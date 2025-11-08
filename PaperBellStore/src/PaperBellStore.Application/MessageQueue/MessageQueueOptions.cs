namespace PaperBellStore.MessageQueue;

/// <summary>
/// 消息队列配置选项
/// </summary>
public class MessageQueueOptions
{
    public RabbitMQOptions RabbitMQ { get; set; } = new();
    public MqttOptions Mqtt { get; set; } = new();
}

public class RabbitMQOptions
{
    public bool Enabled { get; set; } = true;
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public bool EnablePublish { get; set; } = true;
    public bool EnableSubscribe { get; set; } = true;
}

public class MqttOptions
{
    public bool Enabled { get; set; } = false;
    public string BrokerHost { get; set; } = "localhost";
    public int BrokerPort { get; set; } = 1883;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "your_password";
    public string ClientId { get; set; } = "PaperBellStore";
    public string Topic { get; set; } = "paperbellstore/#";
    public bool EnablePublish { get; set; } = true;
    public bool EnableSubscribe { get; set; } = true;
}

