# Hangfire 周期性任务暂停/恢复功能测试指南

## 📋 测试概述

本文档提供 Hangfire 周期性任务暂停/恢复功能的完整测试指南，包括功能测试、权限测试、边界情况测试等。

**测试目标**：验证暂停/恢复功能是否按预期工作，确保任务配置信息能够正确保存和恢复。

**⚠️ 重要说明**：本测试指南针对**自定义 Blazor 管理页面**（`/hangfire/recurring-jobs`）进行测试。Hangfire Dashboard 的原始功能保持不变，可通过链接跳转访问。

---

## 🎯 测试前准备

### 1. 环境准备

1. **启动应用程序**

   ```bash
   cd src/PaperBellStore.Blazor
   dotnet run
   ```

2. **确认周期性任务已注册**

   - 访问 Hangfire Dashboard：`https://localhost:44305/hangfire`
   - 点击左侧菜单 "Recurring jobs"
   - 确认能看到以下任务：
     - `log-cleanup-daily` - 日志清理任务（每天凌晨 2:00）
     - `audit-log-cleanup-daily` - 审计日志清理任务（每天凌晨 3:00）

3. **访问自定义管理页面**

   - 访问：`https://localhost:44305/hangfire/recurring-jobs`
   - 确认页面可以正常加载
   - 确认能看到任务列表

4. **准备测试用户**
   - **有权限用户**：拥有 `HangfireDashboard.Edit` 权限
   - **无权限用户**：只有 `HangfireDashboard.View` 权限（或没有权限）

### 2. 测试工具准备

- **浏览器**：Chrome/Edge/Firefox（用于 Dashboard UI 测试）
- **Postman/curl**：用于 API 测试
- **Swagger UI**：`https://localhost:44305/swagger`（可选）

---

## 📝 测试用例

### 测试组 1：功能测试

#### 测试用例 1.1：暂停任务功能（自定义 Blazor 页面）

**测试步骤**：

1. 使用有权限的用户登录系统
2. 访问自定义管理页面：`https://localhost:44305/hangfire/recurring-jobs`
3. 在任务列表中找到 `log-cleanup-daily` 任务
4. 确认任务状态显示为"运行中"（绿色 Badge）
5. 确认任务列表中有"操作"列，且该任务显示"暂停"按钮
6. 点击"暂停"按钮
7. 在确认对话框中点击"确定"
8. 等待操作完成（页面会自动刷新）

**预期结果**：

- ✅ 按钮显示为"暂停中..."（加载状态）
- ✅ 操作成功后显示成功消息（Toast 或 Alert）
- ✅ 页面刷新后，任务状态更新为"已暂停"（黄色 Badge）
- ✅ 操作按钮变为"恢复"按钮
- ✅ 任务配置信息已保存到 Hangfire 存储中

**验证方法**：

```bash
# 通过 API 查询任务状态
GET /api/hangfire/recurring/status/log-cleanup-daily
```

**预期响应**：

```json
{
  "jobId": "log-cleanup-daily",
  "exists": false,
  "isPaused": true,
  "pausedInfo": {
    "pausedAt": "2024-01-01T12:00:00Z",
    "pausedBy": "admin"
  }
}
```

---

#### 测试用例 1.2：恢复任务功能（自定义 Blazor 页面）

**前置条件**：任务 `log-cleanup-daily` 已处于暂停状态

**测试步骤**：

1. 使用有权限的用户登录系统
2. 访问自定义管理页面：`https://localhost:44305/hangfire/recurring-jobs`
3. 在任务列表中找到已暂停的任务（状态显示为"已暂停"）
4. 确认该任务显示"恢复"按钮（绿色按钮）
5. 点击"恢复"按钮
6. 在确认对话框中点击"确定"
7. 等待操作完成（页面会自动刷新）

**预期结果**：

- ✅ 按钮显示为"恢复中..."（加载状态）
- ✅ 操作成功后显示成功消息（Toast 或 Alert）
- ✅ 页面刷新后，任务状态更新为"运行中"（绿色 Badge）
- ✅ 操作按钮变为"暂停"按钮
- ✅ 任务配置信息（Cron 表达式、时区等）与暂停前一致
- ✅ 任务的下次执行时间已更新

**验证方法**：

```bash
# 通过 API 查询任务状态
GET /api/hangfire/recurring/status/log-cleanup-daily
```

**预期响应**：

```json
{
  "jobId": "log-cleanup-daily",
  "exists": true,
  "isPaused": false,
  "nextExecution": "2024-01-02T02:00:00Z",
  "cron": "0 2 * * *",
  "timeZone": "Local"
}
```

---

#### 测试用例 1.3：暂停任务功能（API）

**测试步骤**：

```bash
# 使用 curl 测试
curl -X POST "https://localhost:44305/api/hangfire/recurring/pause/audit-log-cleanup-daily" \
  -H "Content-Type: application/json" \
  -H "Cookie: YOUR_AUTH_COOKIE" \
  --cookie-jar cookies.txt
```

**或使用 Postman**：

- Method: `POST`
- URL: `https://localhost:44305/api/hangfire/recurring/pause/audit-log-cleanup-daily`
- Headers: `Content-Type: application/json`
- 添加认证 Cookie 或 Bearer Token

**预期响应**：

```json
{
  "message": "任务已暂停",
  "jobId": "audit-log-cleanup-daily",
  "pausedAt": "2024-01-01T12:00:00Z",
  "pausedBy": "admin",
  "timestamp": "2024-01-01T12:00:00"
}
```

**验证步骤**：

1. 查询任务状态，确认 `isPaused` 为 `true`
2. 在 Dashboard 中确认任务已从列表中消失

---

#### 测试用例 1.4：恢复任务功能（API）

**前置条件**：任务 `audit-log-cleanup-daily` 已处于暂停状态

**测试步骤**：

```bash
# 使用 curl 测试
curl -X POST "https://localhost:44305/api/hangfire/recurring/resume/audit-log-cleanup-daily" \
  -H "Content-Type: application/json" \
  -H "Cookie: YOUR_AUTH_COOKIE" \
  --cookie cookies.txt
```

**预期响应**：

```json
{
  "message": "任务已恢复",
  "jobId": "audit-log-cleanup-daily",
  "cronExpression": "0 3 * * *",
  "timeZone": "Local",
  "timestamp": "2024-01-01T12:00:00"
}
```

**验证步骤**：

1. 查询任务状态，确认 `isPaused` 为 `false`
2. 在 Dashboard 中确认任务重新出现在列表中
3. 确认 Cron 表达式和时区与暂停前一致

---

#### 测试用例 1.5：查询任务状态

**测试步骤**：

```bash
# 查询运行中的任务
GET /api/hangfire/recurring/status/log-cleanup-daily

# 查询已暂停的任务
GET /api/hangfire/recurring/status/audit-log-cleanup-daily
```

**预期响应（运行中的任务）**：

```json
{
  "jobId": "log-cleanup-daily",
  "exists": true,
  "isPaused": false,
  "nextExecution": "2024-01-02T02:00:00Z",
  "lastExecution": "2024-01-01T02:00:00Z",
  "cron": "0 2 * * *",
  "timeZone": "Local",
  "error": "",
  "pausedInfo": null,
  "timestamp": "2024-01-01T12:00:00"
}
```

**预期响应（已暂停的任务）**：

```json
{
  "jobId": "audit-log-cleanup-daily",
  "exists": false,
  "isPaused": true,
  "nextExecution": null,
  "lastExecution": "2024-01-01T03:00:00Z",
  "cron": null,
  "timeZone": null,
  "error": "",
  "pausedInfo": {
    "pausedAt": "2024-01-01T12:00:00Z",
    "pausedBy": "admin"
  },
  "timestamp": "2024-01-01T12:00:00"
}
```

---

#### 测试用例 1.6：获取所有任务列表

**测试步骤**：

```bash
GET /api/hangfire/recurring/list
```

**预期响应**：

```json
{
  "message": "获取任务列表成功",
  "count": 2,
  "jobs": [
    {
      "id": "log-cleanup-daily",
      "cron": "0 2 * * *",
      "timeZoneId": "Local",
      "nextExecution": "2024-01-02T02:00:00Z",
      "lastExecution": "2024-01-01T02:00:00Z",
      "createdAt": "2024-01-01T00:00:00Z",
      "error": "",
      "isPaused": false
    },
    {
      "id": "audit-log-cleanup-daily",
      "cron": "0 3 * * *",
      "timeZoneId": "Local",
      "nextExecution": null,
      "lastExecution": "2024-01-01T03:00:00Z",
      "createdAt": "2024-01-01T00:00:00Z",
      "error": "",
      "isPaused": true
    }
  ],
  "timestamp": "2024-01-01T12:00:00"
}
```

**验证点**：

- ✅ 列表中包含所有任务（包括已暂停的任务）
- ✅ `isPaused` 字段正确标识任务状态
- ✅ 已暂停的任务 `nextExecution` 为 `null`

---

### 测试组 2：权限测试

#### 测试用例 2.1：无权限用户无法看到按钮

**测试步骤**：

1. 使用**无权限用户**登录系统（只有 `HangfireDashboard.View` 权限，没有 `HangfireDashboard.Edit` 权限）
2. 访问自定义管理页面：`https://localhost:44305/hangfire/recurring-jobs`
3. 查看任务列表

**预期结果**：

- ✅ 页面可以正常访问（有 View 权限）
- ✅ 任务列表可以正常显示
- ✅ 任务列表中**不显示**"操作"列
- ✅ 任务列表中**不显示**"暂停"/"恢复"按钮

**验证方法**：

- 检查页面，确认"操作"列不存在
- 打开浏览器开发者工具（F12），检查 DOM 元素，确认没有操作按钮

---

#### 测试用例 2.2：无权限用户无法调用暂停 API

**测试步骤**：

```bash
# 使用无权限用户的 Cookie 或 Token
curl -X POST "https://localhost:44305/api/hangfire/recurring/pause/log-cleanup-daily" \
  -H "Content-Type: application/json" \
  -H "Cookie: UNAUTHORIZED_USER_COOKIE"
```

**预期响应**：

```json
{
  "error": {
    "code": "403",
    "message": "没有暂停任务的权限"
  }
}
```

**HTTP 状态码**：`403 Forbidden`

---

#### 测试用例 2.3：无权限用户无法调用恢复 API

**测试步骤**：

```bash
# 使用无权限用户的 Cookie 或 Token
curl -X POST "https://localhost:44305/api/hangfire/recurring/resume/log-cleanup-daily" \
  -H "Content-Type: application/json" \
  -H "Cookie: UNAUTHORIZED_USER_COOKIE"
```

**预期响应**：

```json
{
  "error": {
    "code": "403",
    "message": "没有恢复任务的权限"
  }
}
```

**HTTP 状态码**：`403 Forbidden`

---

#### 测试用例 2.4：有权限用户可以正常操作

**测试步骤**：

1. 使用**有权限用户**登录系统（拥有 `HangfireDashboard.Edit` 权限）
2. 执行测试用例 1.1 和 1.2

**预期结果**：

- ✅ 可以看到"暂停"/"恢复"按钮
- ✅ 可以成功调用 API
- ✅ 操作成功完成

---

### 测试组 3：边界情况测试

#### 测试用例 3.1：暂停已暂停的任务

**前置条件**：任务 `log-cleanup-daily` 已处于暂停状态

**测试步骤**：

```bash
POST /api/hangfire/recurring/pause/log-cleanup-daily
```

**预期响应**：

```json
{
  "message": "任务已经处于暂停状态",
  "jobId": "log-cleanup-daily"
}
```

**HTTP 状态码**：`400 Bad Request`

---

#### 测试用例 3.2：恢复未暂停的任务

**前置条件**：任务 `log-cleanup-daily` 处于运行状态

**测试步骤**：

```bash
POST /api/hangfire/recurring/resume/log-cleanup-daily
```

**预期响应**：

```json
{
  "message": "任务未处于暂停状态",
  "jobId": "log-cleanup-daily"
}
```

**HTTP 状态码**：`400 Bad Request`

---

#### 测试用例 3.3：暂停不存在的任务

**测试步骤**：

```bash
POST /api/hangfire/recurring/pause/non-existent-job
```

**预期响应**：

```json
{
  "message": "任务不存在",
  "jobId": "non-existent-job"
}
```

**HTTP 状态码**：`404 Not Found`

---

#### 测试用例 3.4：恢复不存在的任务（但配置信息存在）

**前置条件**：手动在 Hangfire 存储中创建暂停状态记录（模拟异常情况）

**测试步骤**：

```bash
POST /api/hangfire/recurring/resume/non-existent-job
```

**预期响应**：

- 如果配置信息存在，应该尝试恢复任务
- 如果恢复失败，返回错误信息

---

#### 测试用例 3.5：查询不存在的任务状态

**测试步骤**：

```bash
GET /api/hangfire/recurring/status/non-existent-job
```

**预期响应**：

```json
{
  "jobId": "non-existent-job",
  "exists": false,
  "isPaused": false,
  "nextExecution": null,
  "lastExecution": null,
  "cron": null,
  "timeZone": null,
  "error": "",
  "pausedInfo": null,
  "timestamp": "2024-01-01T12:00:00"
}
```

**HTTP 状态码**：`200 OK`（查询操作不返回错误，只是返回不存在状态）

---

#### 测试用例 3.6：空 JobId 参数

**测试步骤**：

```bash
POST /api/hangfire/recurring/pause/
POST /api/hangfire/recurring/pause/%20
```

**预期响应**：

```json
{
  "message": "JobId 不能为空"
}
```

**HTTP 状态码**：`400 Bad Request`

---

### 测试组 4：任务配置信息验证

#### 测试用例 4.1：验证暂停后配置信息保存

**测试步骤**：

1. 暂停任务 `log-cleanup-daily`
2. 查询任务状态，获取 `pausedInfo`
3. 验证配置信息完整性

**验证点**：

- ✅ `pausedAt` 时间戳正确
- ✅ `pausedBy` 用户名正确
- ✅ 配置信息已保存到 Hangfire 存储中

**验证 SQL**（可选，直接查询 Hangfire 数据库）：

```sql
-- 查询 Hangfire 存储中的任务状态信息
SELECT * FROM hangfire.hash
WHERE key LIKE 'recurring-job-states:log-cleanup-daily%';
```

---

#### 测试用例 4.2：验证恢复后配置信息正确

**测试步骤**：

1. 记录任务暂停前的配置信息（Cron 表达式、时区等）
2. 暂停任务
3. 恢复任务
4. 验证恢复后的配置信息与暂停前一致

**验证点**：

- ✅ Cron 表达式一致
- ✅ 时区设置一致
- ✅ 任务类型正确
- ✅ 任务可以正常执行

---

#### 测试用例 4.3：验证恢复后任务可以正常执行

**测试步骤**：

1. 恢复任务 `log-cleanup-daily`
2. 在 Dashboard 中手动触发任务（Trigger now）
3. 查看任务执行状态

**预期结果**：

- ✅ 任务可以正常触发
- ✅ 任务执行成功
- ✅ 任务日志正常

---

### 测试组 5：并发测试

#### 测试用例 5.1：同时暂停同一任务

**测试步骤**：

1. 使用两个不同的浏览器标签页（或两个不同的 API 请求）
2. 同时点击"暂停"按钮（或同时发送暂停 API 请求）

**预期结果**：

- ✅ 只有一个请求成功
- ✅ 另一个请求返回适当的错误信息（如"任务已经处于暂停状态"）
- ✅ 任务状态正确（已暂停）

---

#### 测试用例 5.2：同时恢复同一任务

**前置条件**：任务已暂停

**测试步骤**：

1. 使用两个不同的浏览器标签页
2. 同时点击"恢复"按钮

**预期结果**：

- ✅ 只有一个请求成功
- ✅ 另一个请求返回适当的错误信息
- ✅ 任务状态正确（已恢复）

---

## 📊 测试检查清单

### 功能测试

- [ ] 测试用例 1.1：暂停任务功能（Dashboard UI）
- [ ] 测试用例 1.2：恢复任务功能（Dashboard UI）
- [ ] 测试用例 1.3：暂停任务功能（API）
- [ ] 测试用例 1.4：恢复任务功能（API）
- [ ] 测试用例 1.5：查询任务状态
- [ ] 测试用例 1.6：获取所有任务列表

### 权限测试

- [ ] 测试用例 2.1：无权限用户无法看到按钮
- [ ] 测试用例 2.2：无权限用户无法调用暂停 API
- [ ] 测试用例 2.3：无权限用户无法调用恢复 API
- [ ] 测试用例 2.4：有权限用户可以正常操作

### 边界情况测试

- [ ] 测试用例 3.1：暂停已暂停的任务
- [ ] 测试用例 3.2：恢复未暂停的任务
- [ ] 测试用例 3.3：暂停不存在的任务
- [ ] 测试用例 3.4：恢复不存在的任务
- [ ] 测试用例 3.5：查询不存在的任务状态
- [ ] 测试用例 3.6：空 JobId 参数

### 配置信息验证

- [ ] 测试用例 4.1：验证暂停后配置信息保存
- [ ] 测试用例 4.2：验证恢复后配置信息正确
- [ ] 测试用例 4.3：验证恢复后任务可以正常执行

### 并发测试

- [ ] 测试用例 5.1：同时暂停同一任务
- [ ] 测试用例 5.2：同时恢复同一任务

---

## 🔧 快速测试脚本

### 使用 curl 进行快速测试

创建测试脚本 `test-pause-resume.sh`：

```bash
#!/bin/bash

BASE_URL="https://localhost:44305"
JOB_ID="log-cleanup-daily"

echo "=== 测试暂停/恢复功能 ==="

# 1. 查询任务状态（暂停前）
echo -e "\n1. 查询任务状态（暂停前）"
curl -s -X GET "$BASE_URL/api/hangfire/recurring/status/$JOB_ID" | jq .

# 2. 暂停任务
echo -e "\n2. 暂停任务"
curl -s -X POST "$BASE_URL/api/hangfire/recurring/pause/$JOB_ID" | jq .

# 3. 查询任务状态（暂停后）
echo -e "\n3. 查询任务状态（暂停后）"
curl -s -X GET "$BASE_URL/api/hangfire/recurring/status/$JOB_ID" | jq .

# 4. 恢复任务
echo -e "\n4. 恢复任务"
curl -s -X POST "$BASE_URL/api/hangfire/recurring/resume/$JOB_ID" | jq .

# 5. 查询任务状态（恢复后）
echo -e "\n5. 查询任务状态（恢复后）"
curl -s -X GET "$BASE_URL/api/hangfire/recurring/status/$JOB_ID" | jq .

echo -e "\n=== 测试完成 ==="
```

**使用方法**：

```bash
chmod +x test-pause-resume.sh
./test-pause-resume.sh
```

**注意**：需要先登录并获取认证 Cookie，或使用 Bearer Token。

---

## 🐛 常见问题排查

### 问题 1：页面无法访问

**可能原因**：

1. 页面文件未创建
2. 路由配置错误
3. 权限检查失败

**排查步骤**：

1. 确认页面文件已创建：`RecurringJobs.razor`
2. 检查路由配置：`@page "/hangfire/recurring-jobs"`
3. 检查用户权限（需要 `HangfireDashboard.View` 权限）
4. 查看浏览器控制台是否有错误信息
5. 查看应用日志是否有异常

---

### 问题 2：API 返回 403 错误

**可能原因**：

1. 用户没有 `HangfireDashboard.Edit` 权限
2. 认证 Cookie/Token 无效

**排查步骤**：

1. 确认用户已登录
2. 检查用户权限配置
3. 确认 Cookie/Token 有效

---

### 问题 3：任务列表无法加载

**可能原因**：

1. API 调用失败
2. 网络连接问题
3. 权限问题

**排查步骤**：

1. 打开浏览器开发者工具（F12），查看 Network 标签
2. 检查 `/api/hangfire/recurring/list` 请求是否成功
3. 查看 API 响应内容
4. 检查用户权限
5. 查看应用日志

### 问题 4：任务恢复后无法执行

**可能原因**：

1. 任务类型识别失败
2. 任务恢复方法未注册
3. Cron 表达式错误

**排查步骤**：

1. 检查 `RecurringJobRecoveryService` 中的任务注册表
2. 查看应用日志，确认是否有错误信息
3. 验证任务配置信息是否正确

---

### 问题 4：任务状态查询不准确

**可能原因**：

1. Hangfire 存储连接问题
2. 状态信息未正确保存

**排查步骤**：

1. 检查 Hangfire 数据库连接
2. 直接查询 Hangfire 存储中的状态信息
3. 查看应用日志

---

## 📝 测试报告模板

### 测试环境

- **测试日期**：2024-XX-XX
- **测试人员**：XXX
- **应用版本**：vX.X.X
- **浏览器**：Chrome XX / Edge XX
- **操作系统**：Windows XX / Linux XX

### 测试结果汇总

| 测试组       | 测试用例数 | 通过   | 失败  | 跳过  | 通过率   |
| ------------ | ---------- | ------ | ----- | ----- | -------- |
| 功能测试     | 6          | 6      | 0     | 0     | 100%     |
| 权限测试     | 4          | 4      | 0     | 0     | 100%     |
| 边界情况测试 | 6          | 6      | 0     | 0     | 100%     |
| 配置信息验证 | 3          | 3      | 0     | 0     | 100%     |
| 并发测试     | 2          | 2      | 0     | 0     | 100%     |
| **总计**     | **21**     | **21** | **0** | **0** | **100%** |

### 详细测试结果

#### 功能测试

- ✅ 测试用例 1.1：暂停任务功能（Dashboard UI） - **通过**
- ✅ 测试用例 1.2：恢复任务功能（Dashboard UI） - **通过**
- ...

#### 发现的问题

1. **问题描述**：XXX
   - **严重程度**：高/中/低
   - **状态**：已修复/待修复
   - **备注**：XXX

---

## 🎯 验收标准

根据实施方案，以下验收标准必须全部满足：

1. ✅ 可以访问自定义的周期性任务管理页面（`/hangfire/recurring-jobs`）
2. ✅ 页面能够显示所有周期性任务列表（包括运行中和已暂停的任务）
3. ✅ 运行中的任务显示"暂停"按钮
4. ✅ 已暂停的任务显示"恢复"按钮
5. ✅ 点击暂停后，任务状态正确更新为已暂停
6. ✅ 点击恢复后，任务重新开始执行，状态更新为运行中
7. ✅ 权限控制正常工作，无权限用户无法看到操作按钮
8. ✅ 任务配置信息在暂停后能够完整恢复
9. ✅ 可以跳转到 Hangfire Dashboard 查看详细信息
10. ✅ 页面 UI 风格与现有页面（AuditLog、RunningLog）保持一致

---

## 📚 相关文档

- [Hangfire 周期性任务暂停恢复实施方案.md](./Hangfire周期性任务暂停恢复实施方案.md)
- [Hangfire 测试验证.md](./Hangfire测试验证.md)
- [Hangfire 权限控制说明.md](./Hangfire权限控制说明.md)

---

**文档版本**：v1.0  
**创建日期**：2024 年  
**最后更新**：2024 年
