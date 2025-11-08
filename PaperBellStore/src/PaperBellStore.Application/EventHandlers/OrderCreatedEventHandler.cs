using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaperBellStore.Events;
using PaperBellStore.MessageQueue;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace PaperBellStore.EventHandlers;

/// <summary>
/// 订单创建事件处理器
/// </summary>
public class OrderCreatedEventHandler : IDistributedEventHandler<OrderCreatedEvent>, ITransientDependency
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;
    private readonly MessageQueueOptions _options;

    public OrderCreatedEventHandler(
        ILogger<OrderCreatedEventHandler> logger,
        IOptions<MessageQueueOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task HandleEventAsync(OrderCreatedEvent eventData)
    {
        // 检查是否启用订阅功能
        if (!_options.RabbitMQ.Enabled || !_options.RabbitMQ.EnableSubscribe)
        {
            _logger.LogDebug("RabbitMQ 订阅功能已禁用，忽略事件: OrderId={OrderId}", eventData.OrderId);
            return;
        }

        _logger.LogInformation("处理订单创建事件: OrderId={OrderId}, UserId={UserId}, TotalAmount={TotalAmount}, OrderNumber={OrderNumber}",
            eventData.OrderId, eventData.UserId, eventData.TotalAmount, eventData.OrderNumber);

        // 处理逻辑
        // 例如：发送邮件、更新库存、记录日志等

        await Task.CompletedTask;
    }
}

