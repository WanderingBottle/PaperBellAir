# RabbitMQ/MQTT 测试文档

本文档提供详细的测试步骤和 API 说明，用于测试 RabbitMQ 和 MQTT 相关功能。

## 📋 目录

1. [快速开始](#1-快速开始)
2. [测试 API 说明](#2-测试api说明)
3. [测试步骤](#3-测试步骤)
4. [验证方法](#4-验证方法)
5. [常见问题排查](#5-常见问题排查)
6. [测试检查清单](#6-测试检查清单)

---

## 1. 快速开始

### 1.1 前置条件

1. **启动 RabbitMQ 服务**

   ```bash
   docker-compose up -d
   ```

2. **验证 RabbitMQ 运行状态**

   ```bash
   docker ps | grep rabbitmq
   ```

3. **检查配置**

   - 确保 `appsettings.json` 中 `MessageQueue:RabbitMQ:Enabled = true`
   - 确保 `EnablePublish = true` 和 `EnableSubscribe = true`

4. **启动应用程序**
   ```bash
   cd src/PaperBellStore.Blazor
   dotnet run
   ```

### 1.2 快速测试

1. **获取配置状态**

   ```bash
   curl http://localhost:44305/api/mq-test/status
   ```

2. **发布测试事件**

   ```bash
   curl -X POST http://localhost:44305/api/mq-test/publish-order-created \
     -H "Content-Type: application/json" \
     -d '{
       "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
       "UserId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
       "TotalAmount": 99.99,
       "OrderNumber": "TEST-001"
     }'
   ```

3. **查看日志**
   - 在应用程序日志中查找 "处理订单创建事件" 的日志
   - 确认事件处理器被正确调用

---

## 2. 测试 API 说明

### 2.1 获取配置状态

**端点**: `GET /api/mq-test/status`

**说明**: 获取当前 RabbitMQ 和 MQTT 的配置状态

**请求示例**:

```bash
curl -X GET http://localhost:44305/api/mq-test/status
```

**响应示例**:

```json
{
  "rabbitMQ": {
    "enabled": true,
    "enablePublish": true,
    "enableSubscribe": true,
    "hostName": "localhost",
    "port": 5672,
    "virtualHost": "/"
  },
  "mqtt": {
    "enabled": false,
    "enablePublish": true,
    "enableSubscribe": true,
    "brokerHost": "localhost",
    "brokerPort": 1883,
    "topic": "paperbellstore/#"
  },
  "timestamp": "2024-01-01T12:00:00"
}
```

### 2.2 发布订单创建事件

**端点**: `POST /api/mq-test/publish-order-created`

**说明**: 发布一个订单创建事件到 RabbitMQ

**请求体** (可选):

```json
{
  "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "UserId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "TotalAmount": 99.99,
  "OrderNumber": "TEST-001"
}
```

**说明**:

- 如果请求体为空，将使用默认值生成测试事件
- 所有字段都是可选的

**请求示例**:

```bash
curl -X POST http://localhost:44305/api/mq-test/publish-order-created \
  -H "Content-Type: application/json" \
  -d '{
    "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "UserId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "TotalAmount": 99.99,
    "OrderNumber": "TEST-001"
  }'
```

**响应示例**:

```json
{
  "message": "订单创建事件已发布",
  "event": {
    "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "orderNumber": "TEST-001",
    "totalAmount": 99.99,
    "createdAt": "2024-01-01T12:00:00Z"
  },
  "timestamp": "2024-01-01T12:00:00",
  "note": "请查看应用程序日志和 RabbitMQ 管理界面以验证事件处理"
}
```

### 2.3 批量发布事件

**端点**: `POST /api/mq-test/publish-batch?count=10`

**说明**: 批量发布多个订单创建事件

**查询参数**:

- `count`: 要发布的事件数量（1-100，默认 5）

**请求示例**:

```bash
curl -X POST "http://localhost:44305/api/mq-test/publish-batch?count=10"
```

**响应示例**:

```json
{
  "message": "成功发布 10 个事件",
  "count": 10,
  "durationMs": 150.5,
  "averageMs": 15.05,
  "events": [
    {
      "index": 1,
      "orderId": "...",
      "orderNumber": "BATCH-ORD-20240101120000-001",
      "totalAmount": 123.45
    }
  ],
  "timestamp": "2024-01-01T12:00:00"
}
```

### 2.4 测试事件处理器

**端点**: `POST /api/mq-test/test-handler`

**说明**: 发布测试事件并验证事件处理器是否正常工作

**请求示例**:

```bash
curl -X POST http://localhost:44305/api/mq-test/test-handler
```

**响应示例**:

```json
{
  "message": "测试事件已发布",
  "event": {
    "orderId": "...",
    "orderNumber": "TEST-HANDLER-20240101120000",
    "totalAmount": 199.99
  },
  "instructions": [
    "1. 查看应用程序日志，应该看到 '处理订单创建事件' 的日志",
    "2. 如果看到日志，说明事件处理器正常工作",
    "3. 如果未看到日志，请检查：",
    "   - OrderCreatedEventHandler 是否正确实现",
    "   - 是否注册为 ITransientDependency",
    "   - RabbitMQ 连接是否正常",
    "   - EnableSubscribe 配置是否为 true"
  ],
  "timestamp": "2024-01-01T12:00:00"
}
```

### 2.5 获取测试指南

**端点**: `GET /api/mq-test/guide`

**说明**: 获取完整的测试指南和常见问题解决方案

**请求示例**:

```bash
curl -X GET http://localhost:44305/api/mq-test/guide
```

---

## 3. 测试步骤

### 3.1 基础功能测试

#### 步骤 1: 检查配置状态

```bash
curl -X GET http://localhost:44305/api/mq-test/status
```

**预期结果**:

- `RabbitMQ.Enabled = true`
- `RabbitMQ.EnablePublish = true`
- `RabbitMQ.EnableSubscribe = true`

#### 步骤 2: 发布单个事件

```bash
curl -X POST http://localhost:44305/api/mq-test/publish-order-created \
  -H "Content-Type: application/json" \
  -d '{
    "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "UserId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "TotalAmount": 99.99,
    "OrderNumber": "TEST-001"
  }'
```

**预期结果**:

- API 返回 200 状态码
- 响应中包含事件信息
- 无错误日志

#### 步骤 3: 验证事件处理

1. **查看应用程序日志**

   - 应该看到 "处理订单创建事件" 的日志
   - 日志应包含 OrderId、UserId、TotalAmount 等信息

2. **检查 RabbitMQ 管理界面**
   - 访问 http://localhost:15672
   - 登录（用户名: admin，密码: your_password）
   - 查看 "Queues" 标签页
   - 应该看到相关队列和消息

#### 步骤 4: 批量测试

```bash
curl -X POST "http://localhost:44305/api/mq-test/publish-batch?count=10"
```

**验证**:

- 检查日志中是否有 10 条处理日志
- 检查 RabbitMQ 管理界面中的消息数量

### 3.2 事件处理器测试

```bash
curl -X POST http://localhost:44305/api/mq-test/test-handler
```

**验证**:

- 查看响应中的 `instructions` 字段
- 按照说明检查日志和配置

### 3.3 性能测试

1. **小批量测试** (10 个事件)

   ```bash
   curl -X POST "http://localhost:44305/api/mq-test/publish-batch?count=10"
   ```

2. **中批量测试** (50 个事件)

   ```bash
   curl -X POST "http://localhost:44305/api/mq-test/publish-batch?count=50"
   ```

3. **大批量测试** (100 个事件)
   ```bash
   curl -X POST "http://localhost:44305/api/mq-test/publish-batch?count=100"
   ```

**监控指标**:

- 发布耗时（`durationMs`）
- 平均每个事件耗时（`averageMs`）
- RabbitMQ 管理界面中的队列长度
- 应用程序日志中的处理速度

### 3.4 错误处理测试

1. **禁用发布功能测试**

   - 设置 `EnablePublish = false`
   - 调用发布 API
   - 应该返回错误提示

2. **禁用订阅功能测试**

   - 设置 `EnableSubscribe = false`
   - 发布事件
   - 事件处理器不应被调用

3. **RabbitMQ 断开测试**
   - 停止 RabbitMQ 服务
   - 尝试发布事件
   - 应该正确处理错误

---

## 4. 验证方法

### 4.1 日志验证

**查找关键日志**:

1. **事件发布日志**

   ```
   发布订单创建事件: OrderId=xxx, OrderNumber=xxx
   ```

2. **事件处理日志**

   ```
   处理订单创建事件: OrderId=xxx, UserId=xxx, TotalAmount=xxx, OrderNumber=xxx
   ```

3. **错误日志**（如果有）
   ```
   [Error] 发布订单创建事件失败
   [Error] 处理订单创建事件失败
   ```

### 4.2 RabbitMQ 管理界面验证

1. **访问管理界面**

   - URL: http://localhost:15672
   - 用户名: admin
   - 密码: your_password（在 docker-compose.yml 中配置）

2. **检查队列**

   - 进入 "Queues" 标签页
   - 查找与事件相关的队列
   - 检查队列中的消息数量

3. **检查连接**

   - 进入 "Connections" 标签页
   - 确认有应用程序的连接

4. **检查交换机**
   - 进入 "Exchanges" 标签页
   - 查找 ABP 框架创建的交换机

### 4.3 健康检查验证

**访问健康检查端点**:

```bash
curl http://localhost:44305/health-status
```

**预期结果**:

- RabbitMQ 健康检查应该显示为 "Healthy"
- 如果显示 "Unhealthy"，检查 RabbitMQ 连接配置

### 4.4 应用程序行为验证

1. **事件发布成功**

   - API 返回 200 状态码
   - 响应中包含事件信息
   - 无错误日志

2. **事件处理成功**

   - 应用程序日志中有处理日志
   - 事件处理器逻辑正常执行
   - 无异常抛出

3. **消息持久化**
   - RabbitMQ 管理界面中可以看到消息
   - 重启应用程序后，未处理的消息仍然存在

---

## 5. 常见问题排查

### 5.1 事件未处理

**症状**: 发布事件后，事件处理器未被调用

**排查步骤**:

1. **检查配置**

   ```bash
   GET /api/mq-test/status
   ```

   - 确认 `EnableSubscribe = true`
   - 确认 `Enabled = true`

2. **检查事件处理器**

   - 确认 `OrderCreatedEventHandler` 实现了 `IDistributedEventHandler<OrderCreatedEvent>`
   - 确认实现了 `ITransientDependency` 接口
   - 确认命名空间正确

3. **检查 RabbitMQ 连接**

   - 查看应用程序启动日志
   - 检查是否有连接错误
   - 验证连接配置（HostName, Port, UserName, Password）

4. **检查日志**
   - 查看是否有错误日志
   - 查看是否有 "RabbitMQ 订阅功能已禁用" 的日志

### 5.2 发布失败

**症状**: API 返回错误，事件未发布

**排查步骤**:

1. **检查配置**

   - 确认 `EnablePublish = true`
   - 确认 `Enabled = true`

2. **检查 RabbitMQ 服务**

   ```bash
   docker ps | grep rabbitmq
   docker-compose logs rabbitmq
   ```

3. **检查连接配置**

   - 验证 HostName、Port、UserName、Password
   - 测试连接: `telnet localhost 5672`

4. **查看错误日志**
   - 检查应用程序日志中的详细错误信息
   - 检查 RabbitMQ 日志

### 5.3 RabbitMQ 连接失败

**症状**: 健康检查显示 RabbitMQ 为 Unhealthy

**排查步骤**:

1. **检查服务状态**

   ```bash
   docker ps
   docker-compose logs rabbitmq
   ```

2. **检查端口**

   ```bash
   netstat -an | grep 5672
   ```

3. **检查配置**

   - 验证用户名和密码
   - 验证 VirtualHost
   - 检查防火墙设置

4. **重启服务**
   ```bash
   docker-compose restart rabbitmq
   ```

### 5.4 性能问题

**症状**: 批量发布事件时性能较差

**优化建议**:

1. **调整批量大小**

   - 小批量（10-20）适合测试
   - 大批量（100+）可能影响性能

2. **检查 RabbitMQ 配置**

   - 增加 RabbitMQ 内存限制
   - 优化队列设置

3. **监控资源使用**
   - 检查 CPU 和内存使用情况
   - 检查网络带宽

### 5.5 消息丢失

**症状**: 发布事件后，消息在 RabbitMQ 中找不到

**排查步骤**:

1. **检查消息持久化**

   - 确认 RabbitMQ 配置了消息持久化
   - 检查队列是否设置为持久化

2. **检查消费者**

   - 确认事件处理器正在运行
   - 检查是否有多个消费者竞争消息

3. **检查消息确认**
   - 查看 RabbitMQ 管理界面中的消息确认状态

---

## 6. 测试检查清单

### 6.1 基础功能

- [ ] RabbitMQ 服务正常运行
- [ ] 配置状态检查通过
- [ ] 可以发布单个事件
- [ ] 事件处理器被正确调用
- [ ] 日志中可以看到处理记录

### 6.2 批量功能

- [ ] 可以批量发布事件（10 个）
- [ ] 可以批量发布事件（50 个）
- [ ] 可以批量发布事件（100 个）
- [ ] 所有事件都被正确处理

### 6.3 错误处理

- [ ] 禁用发布时返回正确错误
- [ ] 禁用订阅时返回正确错误
- [ ] RabbitMQ 断开时正确处理错误
- [ ] 错误日志清晰明确

### 6.4 性能

- [ ] 单个事件发布耗时 < 100ms
- [ ] 批量发布（100 个）耗时 < 5s
- [ ] 事件处理速度正常
- [ ] 无内存泄漏

### 6.5 可靠性

- [ ] 消息持久化正常
- [ ] 服务重启后未处理消息仍然存在
- [ ] 网络中断后可以恢复
- [ ] 消息不丢失

---

## 7. 使用 Postman/Insomnia 测试

### 7.1 导入请求集合

创建以下请求：

1. **获取状态**

   - Method: GET
   - URL: `http://localhost:44305/api/mq-test/status`

2. **发布事件**

   - Method: POST
   - URL: `http://localhost:44305/api/mq-test/publish-order-created`
   - Headers: `Content-Type: application/json`
   - Body:
     ```json
     {
       "OrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
       "UserId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
       "TotalAmount": 99.99,
       "OrderNumber": "TEST-001"
     }
     ```

3. **批量发布**

   - Method: POST
   - URL: `http://localhost:44305/api/mq-test/publish-batch?count=10`

4. **测试处理器**
   - Method: POST
   - URL: `http://localhost:44305/api/mq-test/test-handler`

---

## 8. 持续集成测试

### 8.1 自动化测试脚本

```bash
#!/bin/bash

BASE_URL="http://localhost:44305/api/mq-test"

# 1. 检查状态
echo "检查配置状态..."
curl -s "$BASE_URL/status" | jq .

# 2. 发布测试事件
echo "发布测试事件..."
curl -s -X POST "$BASE_URL/publish-order-created" \
  -H "Content-Type: application/json" \
  -d '{
    "OrderNumber": "CI-TEST-001",
    "TotalAmount": 99.99
  }' | jq .

# 3. 等待处理
echo "等待事件处理..."
sleep 2

# 4. 批量测试
echo "批量发布测试..."
curl -s -X POST "$BASE_URL/publish-batch?count=5" | jq .

echo "测试完成！"
```

---

**最后更新**: 2024 年
