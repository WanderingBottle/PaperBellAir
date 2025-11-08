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

namespace PaperBellStore.MessageQueue;

/// <summary>
/// MQTT 服务
/// </summary>
public class MqttService : IHostedService, IDisposable
{
    private readonly IMqttClient _mqttClient;
    private readonly ILogger<MqttService> _logger;
    private readonly MqttOptions _options;

    public MqttService(
        IOptions<MessageQueueOptions> options,
        ILogger<MqttService> logger)
    {
        _options = options.Value.Mqtt;
        _logger = logger;

        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 检查是否启用 MQTT 功能
        if (!_options.Enabled)
        {
            _logger.LogInformation("MQTT 功能已禁用，跳过启动");
            return;
        }

        // 检查是否启用订阅功能
        if (!_options.EnableSubscribe)
        {
            _logger.LogInformation("MQTT 订阅功能已禁用，跳过订阅");
            return;
        }

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.BrokerHost, _options.BrokerPort)
            .WithCredentials(_options.Username, _options.Password)
            .WithClientId(_options.ClientId)
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            _logger.LogInformation("收到 MQTT 消息 - 主题: {Topic}, 内容: {Payload}", topic, payload);

            // 处理消息逻辑
            await HandleMessageAsync(topic, payload);
        };

        try
        {
            await _mqttClient.ConnectAsync(mqttOptions, cancellationToken);
            _logger.LogInformation("MQTT 客户端已连接到 {BrokerHost}:{BrokerPort}", _options.BrokerHost, _options.BrokerPort);

            // 订阅主题
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(_options.Topic)
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
            _logger.LogInformation("已订阅主题: {Topic}", _options.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQTT 客户端连接失败");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync();
            _logger.LogInformation("MQTT 客户端已断开连接");
        }
    }

    private async Task HandleMessageAsync(string topic, string payload)
    {
        try
        {
            // 解析消息并处理
            // 例如：将 MQTT 消息转换为内部事件并发布到 RabbitMQ
            _logger.LogDebug("处理 MQTT 消息: {Topic}", topic);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 MQTT 消息失败");
        }
    }

    public async Task PublishAsync(string topic, object message)
    {
        // 检查是否启用 MQTT 功能
        if (!_options.Enabled)
        {
            _logger.LogWarning("MQTT 功能已禁用，消息未发送: {Topic}", topic);
            return;
        }

        // 检查是否启用发布功能
        if (!_options.EnablePublish)
        {
            _logger.LogWarning("MQTT 发布功能已禁用，消息未发送: {Topic}", topic);
            return;
        }

        if (!_mqttClient.IsConnected)
        {
            _logger.LogWarning("MQTT 客户端未连接，无法发布消息: {Topic}", topic);
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
            _logger.LogInformation("已发布 MQTT 消息到主题 {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布 MQTT 消息失败: {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        _mqttClient?.Dispose();
    }
}

