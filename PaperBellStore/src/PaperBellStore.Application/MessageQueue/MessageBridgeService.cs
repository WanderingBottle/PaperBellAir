using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MQTTnet;
using MQTTnet.Client;

using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace PaperBellStore.MessageQueue;

/// <summary>
/// 消息桥接服务：在 RabbitMQ（AMQP）和 MQTT 之间桥接消息
/// </summary>
public class MessageBridgeService : IHostedService, ITransientDependency
{
    private readonly IMqttClient _mqttClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<MessageBridgeService> _logger;
    private readonly MessageQueueOptions _options;

    public MessageBridgeService(
        IDistributedEventBus distributedEventBus ,
        IOptions<MessageQueueOptions> options ,
        ILogger<MessageBridgeService> logger)
    {
        _distributedEventBus=distributedEventBus;
        _options=options.Value;
        _logger=logger;

        var factory = new MqttFactory();
        _mqttClient=factory.CreateMqttClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if(!_options.Mqtt.Enabled||!_options.RabbitMQ.Enabled)
        {
            _logger.LogInformation("消息桥接服务未启用（RabbitMQ 或 MQTT 未启用）");
            return;
        }

        // 连接到 MQTT Broker
        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.Mqtt.BrokerHost , _options.Mqtt.BrokerPort)
            .WithCredentials(_options.Mqtt.Username , _options.Mqtt.Password)
            .WithClientId($"{_options.Mqtt.ClientId}-Bridge")
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync+=async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            _logger.LogInformation("从 MQTT 收到消息，桥接到 RabbitMQ: {Topic}" , topic);

            // 将 MQTT 消息转换为 RabbitMQ 事件
            // 这里可以根据业务需求进行消息转换
            // await _distributedEventBus.PublishAsync(...);
            await Task.CompletedTask;
        };

        try
        {
            await _mqttClient.ConnectAsync(mqttOptions , cancellationToken);

            // 订阅 MQTT 主题
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(_options.Mqtt.Topic)
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions , cancellationToken);
            _logger.LogInformation("消息桥接服务已启动");
        }
        catch(Exception ex)
        {
            _logger.LogError(ex , "消息桥接服务启动失败");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if(_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync();
            _logger.LogInformation("消息桥接服务已停止");
        }
    }

    /// <summary>
    /// 将 RabbitMQ 事件发布到 MQTT
    /// </summary>
    public async Task BridgeToMqttAsync(string topic , object message)
    {
        if(!_options.Mqtt.Enabled||!_options.Mqtt.EnablePublish)
        {
            return;
        }

        if(!_mqttClient.IsConnected)
        {
            _logger.LogWarning("MQTT 客户端未连接，无法桥接消息: {Topic}" , topic);
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var payload = Encoding.UTF8.GetBytes(json);

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(mqttMessage);
            _logger.LogInformation("消息已从 RabbitMQ 桥接到 MQTT: {Topic}" , topic);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex , "桥接消息到 MQTT 失败: {Topic}" , topic);
            throw;
        }
    }

    public void Dispose()
    {
        _mqttClient?.Dispose();
    }
}

