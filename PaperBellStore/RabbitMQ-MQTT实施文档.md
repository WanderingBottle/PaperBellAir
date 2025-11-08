# RabbitMQ/MQTT 实施文档

本文档提供 RabbitMQ 和 MQTT 在 PaperBellStore 项目中的完整实施指南，包括快速启动、配置、使用示例和最佳实践。

## 📋 目录

1. [快速开始](#1-快速开始)
2. [方案概述](#2-方案概述)
3. [环境准备](#3-环境准备)
4. [项目配置](#4-项目配置)
5. [使用指南](#5-使用指南)
6. [高级功能](#6-高级功能)
7. [常见问题](#7-常见问题)
8. [最佳实践](#8-最佳实践)

---

## 1. 快速开始

### 1.1 5 分钟快速启动

#### 步骤 1：启动 RabbitMQ 服务器

**使用 Docker Compose（推荐）**

```bash
# 在项目根目录执行
docker-compose up -d

# 查看日志
docker-compose logs -f rabbitmq
```

**使用 Docker 直接运行**

```bash
docker run -d \
  --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  -p 1883:1883 \
  -e RABBITMQ_DEFAULT_USER=admin \
  -e RABBITMQ_DEFAULT_PASS=your_password \
  rabbitmq:3-management

# 启用 MQTT 插件
docker exec -it rabbitmq rabbitmq-plugins enable rabbitmq_mqtt
```

#### 步骤 2：配置应用程序

编辑 `src/PaperBellStore.Blazor/appsettings.json`：

```json
{
  "MessageQueue": {
    "RabbitMQ": {
      "Enabled": true,
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "admin",
      "Password": "your_password",
      "VirtualHost": "/",
      "EnablePublish": true,
      "EnableSubscribe": true
    },
    "Mqtt": {
      "Enabled": false,
      "BrokerHost": "localhost",
      "BrokerPort": 1883,
      "Username": "admin",
      "Password": "your_password",
      "ClientId": "PaperBellStore",
      "Topic": "paperbellstore/#",
      "EnablePublish": true,
      "EnableSubscribe": true
    }
  }
}
```

#### 步骤 3：运行应用程序

```bash
cd src/PaperBellStore.Blazor
dotnet run
```

#### 步骤 4：验证集成

1. 访问健康检查：http://localhost:44305/health-status
2. 访问 RabbitMQ 管理界面：http://localhost:15672
3. 查看应用程序日志，确认连接成功

### 1.2 验证安装

**检查 RabbitMQ 连接**

```bash
# 检查容器状态
docker ps | grep rabbitmq

# 检查端口
netstat -an | grep 5672
```

**访问管理界面**

- URL: http://localhost:15672
- 用户名: admin
- 密码: your_password

---

## 2. 方案概述

### 2.1 架构设计

```
┌─────────────────┐         ┌─────────────────┐
│  ABP App        │         │  IoT Devices    │
│  (AMQP Client)  │         │  (MQTT Client)  │
└────────┬────────┘         └────────┬────────┘
         │                           │
         │ AMQP                      │ MQTT
         ▼                           ▼
         ┌───────────────────────────┐
         │   RabbitMQ Broker         │
         │   + MQTT Plugin           │
         └───────────────────────────┘
                  │
                  │ 消息桥接（可选）
                  ▼
         ┌───────────────────────────┐
         │  Message Bridge Service   │
         │  (AMQP ↔ MQTT)            │
         └───────────────────────────┘
```

### 2.2 方案特点

- ✅ **ABP EventBus + RabbitMQ**：用于企业级应用之间的消息通信
- ✅ **MQTT**：用于 IoT 设备接入
- ✅ **同时运行**：两种协议可以同时运行，互不干扰
- ✅ **消息桥接**：可选，可以在两种协议之间转换消息
- ✅ **配置开关**：支持通过配置独立控制 RabbitMQ 和 MQTT 的启用/禁用
- ✅ **发布/订阅控制**：支持分别控制发布和订阅功能

### 2.3 使用场景

| 场景           | 协议     | 说明            |
| -------------- | -------- | --------------- |
| 订单创建事件   | RabbitMQ | 应用层业务事件  |
| 用户注册通知   | RabbitMQ | 跨服务通信      |
| IoT 传感器数据 | MQTT     | 设备数据采集    |
| 设备控制命令   | MQTT     | 设备远程控制    |
| 订单 → 打印机  | 消息桥接 | RabbitMQ → MQTT |
| 传感器 → 告警  | 消息桥接 | MQTT → RabbitMQ |

---

## 3. 环境准备

### 3.1 安装 RabbitMQ 服务器

#### 方式一：Docker Compose（推荐）

项目根目录已包含 `docker-compose.yml`：

```yaml
version: "3.8"
services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq
    ports:
      - "5672:5672" # AMQP 端口
      - "15672:15672" # 管理界面端口
      - "1883:1883" # MQTT 端口
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: your_password
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    restart: unless-stopped
    command: >
      bash -c "rabbitmq-plugins enable rabbitmq_mqtt && 
               rabbitmq-server"

volumes:
  rabbitmq_data:
```

启动命令：

```bash
docker-compose up -d
```

#### 方式二：本地安装

1. 下载并安装 RabbitMQ
2. 启用管理插件：`rabbitmq-plugins enable rabbitmq_management`
3. 启用 MQTT 插件：`rabbitmq-plugins enable rabbitmq_mqtt`
4. 配置用户和权限

#### 方式三：使用现有服务器

如果已有 RabbitMQ 服务器（本地或远程），只需在配置文件中设置正确的连接信息即可。

### 3.2 验证 MQTT 插件

在 RabbitMQ 管理界面中：

1. 访问 http://localhost:15672
2. 登录后，点击 "Admin" -> "Plugins"
3. 确认 `rabbitmq_mqtt` 插件已启用（显示为绿色）

---

## 4. 项目配置

### 4.1 安装 NuGet 包

**已安装的包**：

```xml
<!-- ABP EventBus RabbitMQ 模块 -->
<PackageReference Include="Volo.Abp.EventBus.RabbitMQ" Version="9.2.1" />

<!-- RabbitMQ 健康检查 -->
<PackageReference Include="AspNetCore.HealthChecks.RabbitMQ" Version="8.0.1" />

<!-- MQTT 客户端 -->
<PackageReference Include="MQTTnet" Version="4.3.3.952" />
```

### 4.2 配置文件

**位置**：`src/PaperBellStore.Blazor/appsettings.json`

**完整配置示例**：

```json
{
  "MessageQueue": {
    "RabbitMQ": {
      "Enabled": true,
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "admin",
      "Password": "your_password",
      "VirtualHost": "/",
      "EnablePublish": true,
      "EnableSubscribe": true
    },
    "Mqtt": {
      "Enabled": false,
      "BrokerHost": "localhost",
      "BrokerPort": 1883,
      "Username": "admin",
      "Password": "your_password",
      "ClientId": "PaperBellStore",
      "Topic": "paperbellstore/#",
      "EnablePublish": true,
      "EnableSubscribe": true
    }
  }
}
```

### 4.3 配置项说明

#### RabbitMQ 配置

| 配置项            | 说明              | 默认值      | 必填 |
| ----------------- | ----------------- | ----------- | ---- |
| `Enabled`         | 是否启用 RabbitMQ | `true`      | 是   |
| `HostName`        | RabbitMQ 主机名   | `localhost` | 是   |
| `Port`            | RabbitMQ 端口     | `5672`      | 是   |
| `UserName`        | 用户名            | `guest`     | 是   |
| `Password`        | 密码              | `guest`     | 是   |
| `VirtualHost`     | 虚拟主机          | `/`         | 是   |
| `EnablePublish`   | 是否启用发布功能  | `true`      | 否   |
| `EnableSubscribe` | 是否启用订阅功能  | `true`      | 否   |

#### MQTT 配置

| 配置项            | 说明               | 默认值             | 必填 |
| ----------------- | ------------------ | ------------------ | ---- |
| `Enabled`         | 是否启用 MQTT      | `false`            | 是   |
| `BrokerHost`      | MQTT Broker 主机名 | `localhost`        | 是   |
| `BrokerPort`      | MQTT Broker 端口   | `1883`             | 是   |
| `Username`        | 用户名             | `admin`            | 是   |
| `Password`        | 密码               | `your_password`    | 是   |
| `ClientId`        | 客户端 ID          | `PaperBellStore`   | 是   |
| `Topic`           | 订阅主题           | `paperbellstore/#` | 是   |
| `EnablePublish`   | 是否启用发布功能   | `true`             | 否   |
| `EnableSubscribe` | 是否启用订阅功能   | `true`             | 否   |

### 4.4 模块配置

**已配置的模块**：

- `AbpEventBusRabbitMqModule` - RabbitMQ 事件总线模块
- `MqttService` - MQTT 服务（条件性注册）
- `MessageBridgeService` - 消息桥接服务（条件性注册）

---

## 5. 使用指南

### 5.1 RabbitMQ 事件发布/订阅

#### 5.1.1 定义事件

**位置**：`src/PaperBellStore.Application.Contracts/Events/OrderCreatedEvent.cs`

```csharp
using System;
using Volo.Abp.EventBus;

namespace PaperBellStore.Events;

/// <summary>
/// 订单创建事件
/// </summary>
[EventName("PaperBellStore.Orders.OrderCreated")]
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string? OrderNumber { get; set; }
}
```

#### 5.1.2 发布事件

**在应用服务中发布事件**：

```csharp
using PaperBellStore.Events;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Distributed;

namespace PaperBellStore.Orders
{
    public class OrderAppService : ApplicationService
    {
        private readonly IDistributedEventBus _distributedEventBus;

        public OrderAppService(IDistributedEventBus distributedEventBus)
        {
            _distributedEventBus = distributedEventBus;
        }

        public async Task CreateOrderAsync(CreateOrderDto input)
        {
            // 创建订单逻辑...
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}",
                UserId = CurrentUser.Id ?? Guid.Empty,
                TotalAmount = input.TotalAmount,
                CreatedAt = DateTime.UtcNow
            };

            // 保存订单到数据库...
            // await _orderRepository.InsertAsync(order);

            // 发布事件
            await _distributedEventBus.PublishAsync(new OrderCreatedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                CreatedAt = order.CreatedAt,
                TotalAmount = order.TotalAmount,
                OrderNumber = order.OrderNumber
            });
        }
    }
}
```

**条件性发布事件**：

```csharp
using Microsoft.Extensions.Options;
using PaperBellStore.MessageQueue;

public class OrderAppService : ApplicationService
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IOptions<MessageQueueOptions> _mqOptions;
    private readonly ILogger<OrderAppService> _logger;

    public OrderAppService(
        IDistributedEventBus distributedEventBus,
        IOptions<MessageQueueOptions> mqOptions,
        ILogger<OrderAppService> logger)
    {
        _distributedEventBus = distributedEventBus;
        _mqOptions = mqOptions;
        _logger = logger;
    }

    public async Task CreateOrderAsync(CreateOrderDto input)
    {
        // 创建订单逻辑...
        var order = new Order { /* ... */ };

        // 检查是否启用发布功能
        if (_mqOptions.Value.RabbitMQ.Enabled && _mqOptions.Value.RabbitMQ.EnablePublish)
        {
            await _distributedEventBus.PublishAsync(new OrderCreatedEvent
            {
                OrderId = order.Id,
                // ...
            });
        }
        else
        {
            _logger.LogWarning("RabbitMQ 发布功能已禁用，事件未发送");
        }
    }
}
```

#### 5.1.3 订阅事件（创建事件处理器）

**位置**：`src/PaperBellStore.Application/EventHandlers/OrderCreatedEventHandler.cs`

```csharp
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
```

**多个事件处理器示例**：

```csharp
// 邮件处理器
public class OrderCreatedEmailHandler : IDistributedEventHandler<OrderCreatedEvent>, ITransientDependency
{
    public async Task HandleEventAsync(OrderCreatedEvent eventData)
    {
        // 发送邮件逻辑...
    }
}

// 库存处理器
public class OrderCreatedInventoryHandler : IDistributedEventHandler<OrderCreatedEvent>, ITransientDependency
{
    public async Task HandleEventAsync(OrderCreatedEvent eventData)
    {
        // 更新库存逻辑...
    }
}
```

**注意**：可以为同一个事件创建多个处理器，ABP 框架会自动注册所有处理器。

### 5.2 MQTT 使用

#### 5.2.1 发布 MQTT 消息

```csharp
using PaperBellStore.MessageQueue;
using Microsoft.Extensions.Options;

public class DeviceService : ApplicationService
{
    private readonly MqttService _mqttService;
    private readonly IOptions<MessageQueueOptions> _mqOptions;

    public DeviceService(
        MqttService mqttService,
        IOptions<MessageQueueOptions> mqOptions)
    {
        _mqttService = mqttService;
        _mqOptions = mqOptions;
    }

    public async Task SendCommandToDeviceAsync(string deviceId, object command)
    {
        if (!_mqOptions.Value.Mqtt.Enabled || !_mqOptions.Value.Mqtt.EnablePublish)
        {
            return;
        }

        var topic = $"device/{deviceId}/command";
        await _mqttService.PublishAsync(topic, command);
    }
}
```

#### 5.2.2 订阅 MQTT 消息

MQTT 消息订阅在 `MqttService` 中自动处理。收到消息后，可以在 `HandleMessageAsync` 方法中处理。

### 5.3 消息桥接

#### 5.3.1 RabbitMQ → MQTT

**场景**：订单创建后通知 IoT 设备（如智能打印机）

```csharp
using PaperBellStore.Events;
using PaperBellStore.MessageQueue;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace PaperBellStore.EventHandlers
{
    /// <summary>
    /// 订单创建后通知 IoT 设备（通过 MQTT）
    /// </summary>
    public class OrderCreatedMqttBridgeHandler : IDistributedEventHandler<OrderCreatedEvent>, ITransientDependency
    {
        private readonly MessageBridgeService _bridgeService;
        private readonly ILogger<OrderCreatedMqttBridgeHandler> _logger;

        public OrderCreatedMqttBridgeHandler(
            MessageBridgeService bridgeService,
            ILogger<OrderCreatedMqttBridgeHandler> logger)
        {
            _bridgeService = bridgeService;
            _logger = logger;
        }

        public async Task HandleEventAsync(OrderCreatedEvent eventData)
        {
            _logger.LogInformation("将订单创建事件桥接到 MQTT: OrderId={OrderId}", eventData.OrderId);

            // 将 RabbitMQ 事件转换为 MQTT 消息
            await _bridgeService.BridgeToMqttAsync("printer/order", new
            {
                OrderId = eventData.OrderId,
                OrderNumber = eventData.OrderNumber,
                TotalAmount = eventData.TotalAmount,
                PrintTime = DateTime.UtcNow
            });
        }
    }
}
```

#### 5.3.2 MQTT → RabbitMQ

MQTT 消息转换为 RabbitMQ 事件在 `MessageBridgeService` 的 `StartAsync` 方法中处理。

---

## 6. 高级功能

### 6.1 本地事件 vs 分布式事件

ABP 支持两种事件总线：

| 特性         | 本地事件（ILocalEventBus） | 分布式事件（IDistributedEventBus） |
| ------------ | -------------------------- | ---------------------------------- |
| **作用范围** | 同一进程/应用程序内        | 跨进程/跨服务/跨服务器             |
| **性能**     | 高性能，内存操作           | 相对较慢，需要网络传输             |
| **可靠性**   | 进程崩溃会丢失事件         | 支持消息持久化，更可靠             |
| **事务支持** | 与当前 UoW 在同一事务中    | 异步处理，不在同一事务中           |
| **适用场景** | 单应用内的解耦             | 微服务架构、分布式系统             |

**使用建议**：

- **本地事件**：同一应用内的模块解耦、需要事务一致性、性能敏感的场景
- **分布式事件**：微服务架构、需要消息持久化、跨服务器通信

### 6.2 多租户支持

ABP EventBus 自动处理多租户上下文：

```csharp
// 发布事件时，当前租户信息会自动包含在事件中
await _distributedEventBus.PublishAsync(new OrderCreatedEvent { /* ... */ });

// 在事件处理器中，租户上下文会自动设置
public class OrderCreatedEventHandler : IDistributedEventHandler<OrderCreatedEvent>
{
    private readonly ICurrentTenant _currentTenant;

    public async Task HandleEventAsync(OrderCreatedEvent eventData)
    {
        // 当前租户上下文已自动设置
        var tenantId = _currentTenant.Id; // 自动获取发布事件时的租户ID
    }
}
```

### 6.3 健康检查

**访问健康检查端点**：

- 健康检查 URL: http://localhost:44305/health-status
- 健康检查 UI: http://localhost:44305/health-ui

在健康检查 UI 中，可以看到 RabbitMQ 的连接状态。

### 6.4 配置开关

通过配置文件可以灵活控制功能：

**仅启用 RabbitMQ**：

```json
{
  "MessageQueue": {
    "RabbitMQ": {
      "Enabled": true,
      "EnablePublish": true,
      "EnableSubscribe": true
    },
    "Mqtt": {
      "Enabled": false
    }
  }
}
```

**仅启用 MQTT**：

```json
{
  "MessageQueue": {
    "RabbitMQ": {
      "Enabled": false
    },
    "Mqtt": {
      "Enabled": true,
      "EnablePublish": true,
      "EnableSubscribe": true
    }
  }
}
```

**同时启用两者**：

```json
{
  "MessageQueue": {
    "RabbitMQ": {
      "Enabled": true,
      "EnablePublish": true,
      "EnableSubscribe": true
    },
    "Mqtt": {
      "Enabled": true,
      "EnablePublish": true,
      "EnableSubscribe": true
    }
  }
}
```

**完全禁用 RabbitMQ**：

> ⚠️ **重要提示**：如果完全禁用 RabbitMQ 且不希望出现连接警告，需要从 `PaperBellStoreBlazorModule.cs` 的 `DependsOn` 中移除 `typeof(AbpEventBusRabbitMqModule)`。仅设置 `Enabled: false` 可能仍会导致模块尝试连接。详细说明请参考 [7.5 完全禁用 RabbitMQ 模块](#75-完全禁用-rabbitmq-模块)。

```json
{
  "MessageQueue": {
    "RabbitMQ": {
      "Enabled": false
    },
    "Mqtt": {
      "Enabled": false
    }
  }
}
```

---

## 7. 常见问题

### 7.1 RabbitMQ 连接失败

**症状**：应用程序无法连接到 RabbitMQ

**解决方案**：

1. 确认 RabbitMQ 服务已启动

   ```bash
   docker ps | grep rabbitmq
   ```

2. 检查配置中的主机名、端口、用户名和密码是否正确

3. 检查端口是否被占用

   ```bash
   netstat -an | grep 5672
   ```

4. 如果使用远程服务器，检查防火墙设置

5. 如果使用 Docker，确认容器正在运行

### 7.2 MQTT 连接失败

**症状**：MQTT 客户端无法连接

**解决方案**：

1. 确认 RabbitMQ MQTT 插件已启用

   - 在管理界面中检查插件状态

2. 检查 MQTT 端口（1883）是否开放

3. 检查配置中的用户名和密码是否正确

### 7.3 事件未处理

**症状**：发布事件后，事件处理器未被调用

**解决方案**：

1. 检查 `EnableSubscribe` 配置是否为 `true`

2. 确认事件处理器实现了 `IDistributedEventHandler<T>`

3. 确认事件处理器注册为 `ITransientDependency`

4. 检查 RabbitMQ 连接是否正常

5. 查看应用程序日志中的错误信息

### 7.4 是否需要 Docker？

**不需要**。Docker 只是运行 RabbitMQ 的一种便捷方式。您可以选择：

- 使用 Docker（最简单）
- 本地安装 RabbitMQ
- 使用现有的 RabbitMQ 服务器（本地或远程）
- 使用云服务提供的 RabbitMQ（如 Azure Service Bus、AWS MQ 等）

只要 RabbitMQ 服务器可以访问，并且配置了正确的连接信息，应用程序就可以正常工作。

### 7.5 完全禁用 RabbitMQ 模块

**症状**：即使设置了 `MessageQueue:RabbitMQ:Enabled: false`，应用程序仍然尝试连接 RabbitMQ，并出现连接警告。

**原因**：`AbpEventBusRabbitMqModule` 在 `DependsOn` 中被硬编码，即使禁用了配置，模块仍会被加载并尝试初始化连接。

**解决方案**：完全移除模块依赖

#### 步骤 1：修改模块依赖

编辑 `src/PaperBellStore.Blazor/PaperBellStoreBlazorModule.cs`，从 `DependsOn` 中移除 `AbpEventBusRabbitMqModule`：

```csharp
[DependsOn(
    typeof(PaperBellStoreApplicationModule),
    typeof(AbpStudioClientAspNetCoreModule),
    // ... 其他模块 ...
    typeof(AbpSettingManagementBlazorServerModule)
    // 注意：AbpEventBusRabbitMqModule 已移除
    // 如果需要在启用 RabbitMQ 时使用，请取消下面的注释
    // typeof(AbpEventBusRabbitMqModule)
)]
public class PaperBellStoreBlazorModule : AbpModule
{
    // ...
}
```

#### 步骤 2：配置禁用状态

在 `appsettings.json` 中设置：

```json
{
  "MessageQueue": {
    "RabbitMQ": {
      "Enabled": false
    }
  }
}
```

#### 步骤 3：重新启用 RabbitMQ（可选）

如果将来需要启用 RabbitMQ：

1. 在 `appsettings.json` 中设置 `MessageQueue:RabbitMQ:Enabled` 为 `true`
2. 在 `PaperBellStoreBlazorModule.cs` 的 `DependsOn` 中取消注释 `typeof(AbpEventBusRabbitMqModule)`
3. 配置正确的连接信息

#### 注意事项

- **完全禁用**：移除 `DependsOn` 中的模块后，RabbitMQ 功能将完全不可用，不会尝试连接
- **条件性启用**：如果需要根据配置动态启用/禁用，需要手动管理模块依赖（ABP 框架不支持运行时动态加载模块）
- **事件总线**：禁用 RabbitMQ 后，`IDistributedEventBus` 将无法使用，需要使用本地事件总线 `ILocalEventBus` 或完全移除分布式事件功能

---

## 8. 最佳实践

### 8.1 事件设计

1. **事件命名**：使用清晰的命名，如 `OrderCreatedEvent`、`UserRegisteredEvent`
2. **事件数据**：只包含必要的数据，避免包含大量数据
3. **版本兼容**：考虑事件版本的向后兼容性

### 8.2 错误处理

1. **重试机制**：实现消息重试机制
2. **死信队列**：使用死信队列处理失败消息
3. **日志记录**：记录详细的错误日志

### 8.3 性能优化

1. **批量处理**：对于大量消息，考虑批量处理
2. **连接池**：使用连接池管理 RabbitMQ 连接
3. **异步处理**：使用异步方法处理事件

### 8.4 安全性

1. **TLS/SSL**：生产环境使用 TLS/SSL 加密连接
2. **用户权限**：配置适当的用户权限
3. **虚拟主机**：使用虚拟主机隔离不同环境

### 8.5 监控和日志

1. **健康检查**：定期检查 RabbitMQ 连接状态
2. **监控指标**：监控队列长度、消息处理速度等指标
3. **日志记录**：记录所有关键操作和错误

---

## 9. 文件结构

```
src/
├── PaperBellStore.Application/
│   ├── MessageQueue/
│   │   ├── MessageQueueOptions.cs      # 配置选项类
│   │   ├── MqttService.cs              # MQTT 服务
│   │   └── MessageBridgeService.cs     # 消息桥接服务
│   ├── EventHandlers/
│   │   └── OrderCreatedEventHandler.cs # 示例事件处理器
│   └── PaperBellStoreApplicationModule.cs
├── PaperBellStore.Application.Contracts/
│   └── Events/
│       └── OrderCreatedEvent.cs        # 示例事件
└── PaperBellStore.Blazor/
    ├── Controllers/
    │   └── MqTestController.cs         # MQ 测试控制器
    ├── PaperBellStoreBlazorModule.cs   # Blazor 模块配置
    └── appsettings.json                # 配置文件
```

---

## 10. 相关资源

- [ABP EventBus 官方文档](https://docs.abp.io/en/abp/latest/Event-Bus)
- [ABP Distributed Event Bus 文档](https://docs.abp.io/en/abp/latest/Distributed-Event-Bus)
- [ABP EventBus RabbitMQ 文档](https://docs.abp.io/en/abp/latest/Distributed-Event-Bus-RabbitMQ-Integration)
- [RabbitMQ 官方文档](https://www.rabbitmq.com/documentation.html)
- [RabbitMQ MQTT 插件文档](https://www.rabbitmq.com/mqtt.html)
- [MQTTnet 文档](https://github.com/dotnet/MQTTnet)

---

**最后更新**：2024 年
