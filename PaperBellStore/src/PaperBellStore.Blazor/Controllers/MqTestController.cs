using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaperBellStore.Events;
using PaperBellStore.MessageQueue;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.EventBus.Distributed;

namespace PaperBellStore.Blazor.Controllers
{
    /// <summary>
    /// MQ 测试控制器
    /// 用于测试 RabbitMQ 和 MQTT 相关功能
    /// </summary>
    [Route("api/mq-test")]
    public class MqTestController : AbpControllerBase
    {
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IOptions<MessageQueueOptions> _mqOptions;
        private readonly ILogger<MqTestController> _logger;

        public MqTestController(
            IDistributedEventBus distributedEventBus,
            IOptions<MessageQueueOptions> mqOptions,
            ILogger<MqTestController> logger)
        {
            _distributedEventBus = distributedEventBus;
            _mqOptions = mqOptions;
            _logger = logger;
        }

        /// <summary>
        /// 获取 MQ 配置状态
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var status = new
            {
                RabbitMQ = new
                {
                    Enabled = _mqOptions.Value.RabbitMQ.Enabled,
                    EnablePublish = _mqOptions.Value.RabbitMQ.EnablePublish,
                    EnableSubscribe = _mqOptions.Value.RabbitMQ.EnableSubscribe,
                    HostName = _mqOptions.Value.RabbitMQ.HostName,
                    Port = _mqOptions.Value.RabbitMQ.Port,
                    VirtualHost = _mqOptions.Value.RabbitMQ.VirtualHost
                },
                Mqtt = new
                {
                    Enabled = _mqOptions.Value.Mqtt.Enabled,
                    EnablePublish = _mqOptions.Value.Mqtt.EnablePublish,
                    EnableSubscribe = _mqOptions.Value.Mqtt.EnableSubscribe,
                    BrokerHost = _mqOptions.Value.Mqtt.BrokerHost,
                    BrokerPort = _mqOptions.Value.Mqtt.BrokerPort,
                    Topic = _mqOptions.Value.Mqtt.Topic
                },
                Timestamp = DateTime.Now
            };

            return Ok(status);
        }

        /// <summary>
        /// 测试发布订单创建事件
        /// </summary>
        [HttpPost("publish-order-created")]
        public async Task<IActionResult> PublishOrderCreatedEvent([FromBody] OrderCreatedTestRequest? request)
        {
            if (!_mqOptions.Value.RabbitMQ.Enabled || !_mqOptions.Value.RabbitMQ.EnablePublish)
            {
                return BadRequest(new
                {
                    Message = "RabbitMQ 发布功能已禁用",
                    Suggestion = "请在 appsettings.json 中设置 MessageQueue:RabbitMQ:Enabled = true 和 EnablePublish = true"
                });
            }

            try
            {
                var orderId = request?.OrderId ?? Guid.NewGuid();
                var userId = request?.UserId ?? Guid.NewGuid();
                var totalAmount = request?.TotalAmount ?? 99.99m;
                var orderNumber = request?.OrderNumber ?? $"TEST-ORD-{DateTime.UtcNow:yyyyMMddHHmmss}";

                var eventData = new OrderCreatedEvent
                {
                    OrderId = orderId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    OrderNumber = orderNumber
                };

                _logger.LogInformation("发布订单创建事件: OrderId={OrderId}, OrderNumber={OrderNumber}",
                    orderId, orderNumber);

                await _distributedEventBus.PublishAsync(eventData);

                return Ok(new
                {
                    Message = "订单创建事件已发布",
                    Event = new
                    {
                        eventData.OrderId,
                        eventData.UserId,
                        eventData.OrderNumber,
                        eventData.TotalAmount,
                        eventData.CreatedAt
                    },
                    Timestamp = DateTime.Now,
                    Note = "请查看应用程序日志和 RabbitMQ 管理界面以验证事件处理"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布订单创建事件失败");
                return BadRequest(new
                {
                    Message = "发布事件失败",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// 批量测试发布多个事件
        /// </summary>
        [HttpPost("publish-batch")]
        public async Task<IActionResult> PublishBatch([FromQuery] int count = 5)
        {
            if (!_mqOptions.Value.RabbitMQ.Enabled || !_mqOptions.Value.RabbitMQ.EnablePublish)
            {
                return BadRequest(new
                {
                    Message = "RabbitMQ 发布功能已禁用"
                });
            }

            if (count < 1 || count > 100)
            {
                return BadRequest(new
                {
                    Message = "批量数量必须在 1-100 之间"
                });
            }

            try
            {
                var results = new List<object>();
                var startTime = DateTime.UtcNow;

                for (int i = 0; i < count; i++)
                {
                    var orderId = Guid.NewGuid();
                    var eventData = new OrderCreatedEvent
                    {
                        OrderId = orderId,
                        UserId = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow,
                        TotalAmount = (decimal)(new Random().NextDouble() * 1000),
                        OrderNumber = $"BATCH-ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{i + 1:D3}"
                    };

                    await _distributedEventBus.PublishAsync(eventData);
                    results.Add(new
                    {
                        Index = i + 1,
                        eventData.OrderId,
                        eventData.OrderNumber,
                        eventData.TotalAmount
                    });
                }

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalMilliseconds;

                return Ok(new
                {
                    Message = $"成功发布 {count} 个事件",
                    Count = count,
                    DurationMs = duration,
                    AverageMs = duration / count,
                    Events = results,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量发布事件失败");
                return BadRequest(new
                {
                    Message = "批量发布事件失败",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 测试事件处理器是否正常工作
        /// 通过发布事件并检查日志来验证
        /// </summary>
        [HttpPost("test-handler")]
        public async Task<IActionResult> TestEventHandler()
        {
            if (!_mqOptions.Value.RabbitMQ.Enabled)
            {
                return BadRequest(new
                {
                    Message = "RabbitMQ 未启用"
                });
            }

            if (!_mqOptions.Value.RabbitMQ.EnableSubscribe)
            {
                return BadRequest(new
                {
                    Message = "RabbitMQ 订阅功能已禁用",
                    Suggestion = "请在 appsettings.json 中设置 MessageQueue:RabbitMQ:EnableSubscribe = true"
                });
            }

            try
            {
                var testEvent = new OrderCreatedEvent
                {
                    OrderId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    TotalAmount = 199.99m,
                    OrderNumber = $"TEST-HANDLER-{DateTime.UtcNow:yyyyMMddHHmmss}"
                };

                _logger.LogInformation("测试事件处理器: 发布测试事件 OrderId={OrderId}", testEvent.OrderId);

                await _distributedEventBus.PublishAsync(testEvent);

                return Ok(new
                {
                    Message = "测试事件已发布",
                    Event = new
                    {
                        testEvent.OrderId,
                        testEvent.OrderNumber,
                        testEvent.TotalAmount
                    },
                    Instructions = new string[]
                    {
                        "1. 查看应用程序日志，应该看到 '处理订单创建事件' 的日志",
                        "2. 如果看到日志，说明事件处理器正常工作",
                        "3. 如果未看到日志，请检查：",
                        "   - OrderCreatedEventHandler 是否正确实现",
                        "   - 是否注册为 ITransientDependency",
                        "   - RabbitMQ 连接是否正常",
                        "   - EnableSubscribe 配置是否为 true"
                    },
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试事件处理器失败");
                return BadRequest(new
                {
                    Message = "测试失败",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 获取测试指南
        /// </summary>
        [HttpGet("guide")]
        public IActionResult GetTestGuide()
        {
            var guide = new
            {
                Title = "MQ 测试指南",
                Steps = new object[]
                {
                    new
                    {
                        Step = 1,
                        Title = "检查配置",
                        Description = "调用 GET /api/mq-test/status 查看当前配置状态",
                        Endpoint = "/api/mq-test/status"
                    },
                    new
                    {
                        Step = 2,
                        Title = "启动 RabbitMQ",
                        Description = "确保 RabbitMQ 服务正在运行（使用 docker-compose up -d）",
                        Command = "docker-compose up -d"
                    },
                    new
                    {
                        Step = 3,
                        Title = "测试事件发布",
                        Description = "调用 POST /api/mq-test/publish-order-created 发布测试事件",
                        Endpoint = "/api/mq-test/publish-order-created",
                        Example = new
                        {
                            Method = "POST",
                            Body = new
                            {
                                OrderId = "guid",
                                UserId = "guid",
                                TotalAmount = 99.99m,
                                OrderNumber = "TEST-001"
                            }
                        }
                    },
                    new
                    {
                        Step = 4,
                        Title = "验证事件处理",
                        Description = "查看应用程序日志，确认 OrderCreatedEventHandler 被调用",
                        LogPattern = "处理订单创建事件"
                    },
                    new
                    {
                        Step = 5,
                        Title = "检查 RabbitMQ 管理界面",
                        Description = "访问 http://localhost:15672 查看队列和消息",
                        Url = "http://localhost:15672",
                        Credentials = new
                        {
                            Username = "admin",
                            Password = "your_password"
                        }
                    },
                    new
                    {
                        Step = 6,
                        Title = "批量测试",
                        Description = "调用 POST /api/mq-test/publish-batch?count=10 批量发布事件",
                        Endpoint = "/api/mq-test/publish-batch?count=10"
                    },
                    new
                    {
                        Step = 7,
                        Title = "测试事件处理器",
                        Description = "调用 POST /api/mq-test/test-handler 验证事件处理器",
                        Endpoint = "/api/mq-test/test-handler"
                    }
                },
                HealthCheck = new
                {
                    Url = "/health-status",
                    Description = "检查 RabbitMQ 连接状态"
                },
                RabbitMQManagement = new
                {
                    Url = "http://localhost:15672",
                    Description = "RabbitMQ 管理界面"
                },
                CommonIssues = new object[]
                {
                    new
                    {
                        Issue = "事件未处理",
                        Solutions = new string[]
                        {
                            "检查 EnableSubscribe 配置是否为 true",
                            "确认 OrderCreatedEventHandler 实现了 IDistributedEventHandler<OrderCreatedEvent>",
                            "确认事件处理器注册为 ITransientDependency",
                            "检查 RabbitMQ 连接是否正常",
                            "查看应用程序日志中的错误信息"
                        }
                    },
                    new
                    {
                        Issue = "发布失败",
                        Solutions = new string[]
                        {
                            "检查 EnablePublish 配置是否为 true",
                            "确认 RabbitMQ 服务正在运行",
                            "检查连接配置（HostName, Port, UserName, Password）",
                            "查看应用程序日志中的错误信息"
                        }
                    },
                    new
                    {
                        Issue = "RabbitMQ 连接失败",
                        Solutions = new string[]
                        {
                            "确认 RabbitMQ 服务已启动：docker ps",
                            "检查端口是否被占用",
                            "验证用户名和密码是否正确",
                            "检查防火墙设置"
                        }
                    }
                },
                Timestamp = DateTime.Now
            };

            return Ok(guide);
        }
    }

    /// <summary>
    /// 订单创建测试请求模型
    /// </summary>
    public class OrderCreatedTestRequest
    {
        public Guid? OrderId { get; set; }
        public Guid? UserId { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? OrderNumber { get; set; }
    }
}


