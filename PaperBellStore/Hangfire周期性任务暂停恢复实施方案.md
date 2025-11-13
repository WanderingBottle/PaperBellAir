# Hangfire 周期性任务暂停/恢复功能实施方案

## 📋 方案概述

### 问题描述

Hangfire 默认 Dashboard 不提供周期性任务（RecurringJob）的暂停和恢复按钮，只能通过删除任务来"暂停"，但删除后无法恢复（因为任务配置信息丢失）。

### 解决方案

通过扩展 Hangfire Dashboard，添加自定义的暂停/恢复功能：

1. **任务状态管理服务**：保存任务配置信息，支持暂停/恢复
2. **API 端点**：提供暂停/恢复操作的 HTTP 接口
3. **Dashboard 扩展**：在 Dashboard 的周期性任务页面注入自定义按钮
4. **权限控制**：集成 ABP 权限系统，控制操作权限

---

## 🎯 实施步骤

### 阶段一：任务状态管理服务 ✅

- [x] 创建 `RecurringJobStateService` 服务
- [x] 实现任务配置信息的保存和恢复
- [x] 使用 Hangfire Set 功能存储任务状态（持久化到数据库）
- [x] 创建 `RecurringJobRecoveryService` 服务用于恢复任务

**已创建文件**：

- `src/PaperBellStore.Blazor/Services/RecurringJobStateService.cs`
- `src/PaperBellStore.Blazor/Services/RecurringJobRecoveryService.cs`

### 阶段二：API 端点实现 ✅

- [x] 创建 `HangfireRecurringJobController` 控制器
- [x] 实现暂停任务 API (`POST /api/hangfire/recurring/pause/{jobId}`)
- [x] 实现恢复任务 API (`POST /api/hangfire/recurring/resume/{jobId}`)
- [x] 实现获取任务状态 API (`GET /api/hangfire/recurring/status/{jobId}`)
- [x] 实现获取所有任务列表 API (`GET /api/hangfire/recurring/list`)

**已创建文件**：

- `src/PaperBellStore.Blazor/Controllers/HangfireRecurringJobController.cs`

### 阶段三：Dashboard 扩展 ✅

- [x] 创建 Dashboard 扩展类 (`HangfireDashboardExtensions`)
- [x] 创建自定义 JavaScript 文件 (`hangfire-dashboard-extensions.js`)
- [x] 创建自定义 CSS 文件 (`hangfire-dashboard-extensions.css`)
- [x] 创建中间件注入脚本到 Dashboard (`HangfireDashboardInjectionMiddleware`)
- [x] 在周期性任务列表页面添加暂停/恢复按钮

**已创建文件**：

- `src/PaperBellStore.Blazor/Extensions/HangfireDashboardExtensions.cs`
- `src/PaperBellStore.Blazor/Middleware/HangfireDashboardInjectionMiddleware.cs`
- `src/PaperBellStore.Blazor/wwwroot/hangfire-dashboard-extensions.js`
- `src/PaperBellStore.Blazor/wwwroot/hangfire-dashboard-extensions.css`

### 阶段四：权限集成 ✅

- [x] 使用现有的 `HangfireDashboard.Edit` 权限（暂停/恢复操作需要编辑权限）
- [x] 在 API 端点中集成权限检查
- [x] 权限检查已集成到 `HangfireRecurringJobController`

**权限说明**：

- 暂停/恢复操作需要 `HangfireDashboard.Edit` 权限
- 查看任务状态需要 `HangfireDashboard.View` 权限

### 阶段五：测试验证

- [ ] 功能测试
  - [ ] 测试暂停任务功能
  - [ ] 测试恢复任务功能
  - [ ] 测试 Dashboard 按钮显示
  - [ ] 测试任务状态查询
- [ ] 权限测试
  - [ ] 测试无权限用户无法看到按钮
  - [ ] 测试无权限用户无法调用 API
- [ ] 边界情况测试
  - [ ] 测试暂停已暂停的任务
  - [ ] 测试恢复未暂停的任务
  - [ ] 测试不存在的任务 ID

---

## 📐 技术架构

### 核心组件

```
┌─────────────────────────────────────────┐
│      Hangfire Dashboard (扩展)          │
│  ┌───────────────────────────────────┐  │
│  │  自定义 JavaScript 注入按钮       │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│      API 控制器 (暂停/恢复端点)         │
│  ┌───────────────────────────────────┐  │
│  │  POST /api/hangfire/pause         │  │
│  │  POST /api/hangfire/resume        │  │
│  │  GET  /api/hangfire/job-status    │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│   任务状态管理服务                        │
│  ┌───────────────────────────────────┐  │
│  │  RecurringJobStateService         │  │
│  │  - SaveJobConfig()                │  │
│  │  - GetJobConfig()                 │  │
│  │  - RemoveJobConfig()              │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│      Hangfire API                        │
│  ┌───────────────────────────────────┐  │
│  │  RecurringJob.RemoveIfExists()    │  │
│  │  RecurringJob.AddOrUpdate()       │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

---

## 🔑 关键技术点

### 1. 任务配置信息保存

需要保存的信息：

- JobId（任务 ID）
- JobType（任务类型，如 `LogCleanupRecurringJob`）
- MethodCall（方法调用表达式）
- CronExpression（Cron 表达式）
- TimeZone（时区）
- Options（其他选项）

### 2. Dashboard 扩展方式

Hangfire Dashboard 支持通过以下方式扩展：

- **DashboardRoutes**：添加自定义路由
- **DashboardOptions**：注入自定义资源（JS/CSS）
- **JavaScript 注入**：在现有页面注入自定义脚本

### 3. 权限控制

- 使用 ABP 权限系统
- 添加 `HangfireDashboard.Pause` 和 `HangfireDashboard.Resume` 权限
- 在 API 端点和 Dashboard 中检查权限

---

## 📝 实施细节

### 1. 任务状态管理服务

**文件位置**：`src/PaperBellStore.Blazor/Services/RecurringJobStateService.cs`

**功能**：

- 保存任务配置信息（使用内存字典或数据库）
- 获取任务配置信息
- 删除任务配置信息

**存储方案**：

- **方案 A**：使用内存字典（简单，但重启后丢失）
- **方案 B**：使用数据库表（持久化，推荐）
- **方案 C**：使用 Hangfire 的 Set 功能（利用 Hangfire 存储）

### 2. API 端点

**文件位置**：`src/PaperBellStore.Blazor/Controllers/HangfireRecurringJobController.cs`

**端点设计**：

```
POST   /api/hangfire/recurring/pause/{jobId}    - 暂停任务
POST   /api/hangfire/recurring/resume/{jobId}   - 恢复任务
GET    /api/hangfire/recurring/status/{jobId}   - 获取任务状态
GET    /api/hangfire/recurring/list             - 获取所有任务状态
```

### 3. Dashboard 扩展

**文件位置**：`src/PaperBellStore.Blazor/Extensions/HangfireDashboardExtensions.cs`

**实现方式**：

- 创建自定义 Dashboard 路由
- 注入 JavaScript 文件到 Dashboard
- 在周期性任务列表页面添加按钮

---

## ⚠️ 注意事项

1. **任务配置信息丢失问题**：

   - 暂停时删除任务会丢失配置信息
   - 必须在删除前保存完整配置信息
   - 恢复时需要能够重新创建任务

2. **任务类型识别**：

   - 需要能够识别任务类型（如 `LogCleanupRecurringJob`）
   - 恢复时需要能够重新创建相同类型的任务

3. **权限控制**：

   - 暂停/恢复操作需要权限检查
   - Dashboard 按钮显示需要权限控制

4. **并发安全**：
   - 多个用户同时操作同一任务时的并发控制
   - 使用锁机制防止冲突

---

## 📚 参考资源

- [Hangfire Dashboard 扩展文档](https://docs.hangfire.io/en/latest/configuration/using-dashboard.html)
- [Hangfire RecurringJob API](https://docs.hangfire.io/en/latest/background-methods/calling-methods-with-delay.html#recurring-jobs)
- [ABP 权限系统文档](https://docs.abp.io/en/abp/latest/Authorization)

---

## ✅ 验收标准

1. ✅ 在 Hangfire Dashboard 的周期性任务列表页面可以看到"暂停"按钮
2. ✅ 点击暂停后，任务从列表中消失（或显示为已暂停状态）
3. ✅ 已暂停的任务可以显示"恢复"按钮
4. ✅ 点击恢复后，任务重新开始执行
5. ✅ 权限控制正常工作，无权限用户无法看到按钮
6. ✅ 任务配置信息在暂停后能够完整恢复

---

---

## 📦 已实现文件清单

### 服务层

1. `src/PaperBellStore.Blazor/Services/RecurringJobStateService.cs` - 任务状态管理服务
2. `src/PaperBellStore.Blazor/Services/RecurringJobRecoveryService.cs` - 任务恢复服务

### 控制器层

3. `src/PaperBellStore.Blazor/Controllers/HangfireRecurringJobController.cs` - API 控制器

### 扩展和中间件

4. `src/PaperBellStore.Blazor/Extensions/HangfireDashboardExtensions.cs` - Dashboard 扩展
5. `src/PaperBellStore.Blazor/Middleware/HangfireDashboardInjectionMiddleware.cs` - 脚本注入中间件

### 前端资源

6. `src/PaperBellStore.Blazor/wwwroot/hangfire-dashboard-extensions.js` - JavaScript 扩展脚本
7. `src/PaperBellStore.Blazor/wwwroot/hangfire-dashboard-extensions.css` - CSS 样式文件

### 配置更新

8. `src/PaperBellStore.Blazor/PaperBellStoreBlazorModule.cs` - 已更新，注册扩展和中间件

---

## 🚀 使用说明

### 1. 访问 Dashboard

访问 `/hangfire` 路径，进入 Hangfire Dashboard。

### 2. 查看周期性任务

点击左侧菜单 "Recurring jobs"，查看所有周期性任务。

### 3. 暂停任务

- 在任务列表的"操作"列中，点击"暂停"按钮
- 确认后，任务将被暂停（从 Hangfire 中删除，但配置信息已保存）

### 4. 恢复任务

- 已暂停的任务会显示"恢复"按钮
- 点击"恢复"按钮，任务将重新注册并开始执行

### 5. API 调用示例

**暂停任务**：

```bash
POST /api/hangfire/recurring/pause/{jobId}
```

**恢复任务**：

```bash
POST /api/hangfire/recurring/resume/{jobId}
```

**查询任务状态**：

```bash
GET /api/hangfire/recurring/status/{jobId}
```

**获取所有任务列表**：

```bash
GET /api/hangfire/recurring/list
```

---

## ⚠️ 注意事项

1. **任务配置保存**：暂停任务时，配置信息会保存到 Hangfire 的存储中（使用 Set 功能）
2. **任务类型识别**：恢复任务时，系统会根据 JobId 或任务类型自动识别并恢复
3. **权限要求**：暂停/恢复操作需要 `HangfireDashboard.Edit` 权限
4. **浏览器兼容性**：JavaScript 扩展需要现代浏览器支持（支持 ES6+ 和 MutationObserver）

---

**文档版本**：v1.1  
**创建日期**：2024 年  
**最后更新**：2024 年（阶段一至四已完成）
