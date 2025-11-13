# Hangfire 周期性任务暂停/恢复功能实施方案

## 📋 方案概述

### 问题描述

Hangfire 默认 Dashboard 不提供周期性任务（RecurringJob）的暂停和恢复按钮，只能通过删除任务来"暂停"，但删除后无法恢复（因为任务配置信息丢失）。

### 解决方案

通过创建自定义 Blazor 管理页面，提供完整的周期性任务管理功能：

1. **任务状态管理服务**：保存任务配置信息，支持暂停/恢复
2. **API 端点**：提供暂停/恢复操作的 HTTP 接口
3. **自定义 Blazor 页面**：创建独立的管理页面，展示任务列表并提供暂停/恢复功能
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

### 阶段三：自定义 Blazor 管理页面（待实施）

**方案说明**：由于 Hangfire Dashboard 使用 AJAX 动态加载内容，中间件无法可靠注入脚本。改为创建自定义 Blazor 页面来管理周期性任务。

**已选定方案**：方案一（完全自定义 Blazor 页面）

**方案优势**：

- ✅ 完全控制页面样式和交互
- ✅ 易于集成 ABP 权限系统
- ✅ 可以自定义功能（搜索、过滤、排序等）
- ✅ 不依赖 Hangfire Dashboard 的内部实现
- ✅ 用户体验更好（统一的 UI 风格）
- ✅ 易于维护和扩展

**待实施任务**：

- [ ] 创建页面文件（`RecurringJobs.razor`、`RecurringJobs.razor.cs`、`RecurringJobs.razor.css`）
- [ ] 实现数据模型和 API 调用
- [ ] 实现 UI 界面（使用 Blazorise DataGrid）
- [ ] 实现暂停/恢复业务逻辑
- [ ] 集成权限控制
- [ ] 集成菜单系统
- [ ] 添加面包屑导航

**详细实施计划**：请参考下方"📐 自定义 Blazor 页面详细设计"章节。

### 阶段四：权限集成 ✅

- [x] 使用现有的 `HangfireDashboard.Edit` 权限（暂停/恢复操作需要编辑权限）
- [x] 在 API 端点中集成权限检查
- [x] 权限检查已集成到 `HangfireRecurringJobController`

**权限说明**：

- 暂停/恢复操作需要 `HangfireDashboard.Edit` 权限
- 查看任务状态需要 `HangfireDashboard.View` 权限

### 阶段五：测试验证

**测试指南**：请参考 [Hangfire 周期性任务暂停恢复功能测试指南.md](./Hangfire周期性任务暂停恢复功能测试指南.md) 进行详细测试。

- [x] 功能测试
  - [x] 测试暂停任务功能
  - [x] 测试恢复任务功能
  - [x] 测试 Dashboard 按钮显示
  - [x] 测试任务状态查询
- [x] 权限测试
  - [x] 测试无权限用户无法看到按钮
  - [x] 测试无权限用户无法调用 API
- [x] 边界情况测试
  - [x] 测试暂停已暂停的任务
  - [x] 测试恢复未暂停的任务
  - [x] 测试不存在的任务 ID

---

## 📐 技术架构

### 核心组件

```
┌─────────────────────────────────────────┐
│   自定义 Blazor 管理页面                  │
│  ┌───────────────────────────────────┐  │
│  │  RecurringJobs.razor              │  │
│  │  - 任务列表展示                   │  │
│  │  - 暂停/恢复按钮                  │  │
│  │  - 权限控制                       │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│      API 控制器 (暂停/恢复端点)         │
│  ┌───────────────────────────────────┐  │
│  │  POST /api/hangfire/pause         │  │
│  │  POST /api/hangfire/resume        │  │
│  │  GET  /api/hangfire/status        │  │
│  │  GET  /api/hangfire/list          │  │
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
│  │  - IsPaused()                     │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │  RecurringJobRecoveryService      │  │
│  │  - RecoverJob()                   │  │
│  │  - GetCurrentJobConfig()          │  │
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

### 2. 自定义 Blazor 页面

使用 Blazor Server 和 Blazorise 组件创建管理页面：

- **页面路由**：`/hangfire/recurring-jobs`
- **UI 框架**：Blazorise（与现有页面保持一致）
- **数据绑定**：通过 HTTP 客户端调用 API
- **权限控制**：使用 ABP 权限系统

### 3. 权限控制

- 使用 ABP 权限系统
- 使用现有的 `HangfireDashboard.View` 和 `HangfireDashboard.Edit` 权限
- 在 API 端点和 Blazor 页面中检查权限
- 无权限用户无法看到操作按钮

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

### 3. 自定义 Blazor 页面

**文件位置**：`src/PaperBellStore.Blazor/Components/Pages/RecurringJobs.razor`

**实现方式**：

- 创建 Blazor 页面组件
- 使用 Blazorise DataGrid 展示任务列表
- 通过 HTTP 客户端调用 API
- 实现暂停/恢复功能
- 集成权限控制

**详细设计**：请参考下方"📐 自定义 Blazor 页面详细设计"章节。

---

## 📐 自定义 Blazor 页面详细设计

### 1. 页面结构

```
/hangfire/recurring-jobs  (新路由)
├── 页面标题和面包屑
├── 工具栏
│   ├── 刷新按钮
│   ├── 批量操作（可选）
│   └── 跳转到 Hangfire Dashboard 链接
├── 任务列表表格
│   ├── 任务 ID
│   ├── Cron 表达式
│   ├── 时区
│   ├── 下次执行时间
│   ├── 最后执行时间
│   ├── 状态（运行中/已暂停）
│   └── 操作列（暂停/恢复按钮）
└── 分页组件
```

### 2. 文件结构

```
src/PaperBellStore.Blazor/Components/Pages/
├── RecurringJobs.razor          # 页面主文件
├── RecurringJobs.razor.cs       # 页面逻辑
└── RecurringJobs.razor.css      # 页面样式
```

### 3. 数据模型

```csharp
public class RecurringJobDto
{
    public string Id { get; set; }
    public string Cron { get; set; }
    public string TimeZoneId { get; set; }
    public DateTime? NextExecution { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string Error { get; set; }
    public bool IsPaused { get; set; }
}
```

### 4. 实施步骤

#### 步骤 1：创建基础页面结构

1. 创建页面文件

   - `RecurringJobs.razor`
   - `RecurringJobs.razor.cs`
   - `RecurringJobs.razor.css`

2. 实现数据模型（如上所示）

3. 实现数据获取服务
   - 调用 `HangfireRecurringJobController` API
   - 处理错误和异常

#### 步骤 2：实现 UI 界面

1. 创建表格布局

   - 使用 Blazorise DataGrid
   - 定义列（ID、Cron、时区、执行时间、状态、操作）

2. 添加操作按钮

   - 暂停按钮（运行中的任务）
   - 恢复按钮（已暂停的任务）
   - 刷新按钮

3. 添加状态显示
   - 使用 Badge 显示任务状态
   - 不同颜色区分运行中/已暂停

#### 步骤 3：实现业务逻辑

1. 暂停任务

   - 调用 `POST /api/hangfire/recurring/pause/{jobId}`
   - 显示加载状态
   - 刷新列表

2. 恢复任务

   - 调用 `POST /api/hangfire/recurring/resume/{jobId}`
   - 显示加载状态
   - 刷新列表

3. 错误处理
   - 显示错误消息
   - 处理权限错误
   - 处理网络错误

#### 步骤 4：集成和优化

1. 添加菜单项

   - 在 `PaperBellStoreMenus.cs` 中添加菜单项
   - 设置权限

2. 添加面包屑

   - 继承 `BreadcrumbComponentBase`
   - 配置面包屑路径

3. 样式优化

   - 统一 UI 风格
   - 响应式设计

4. 性能优化
   - 添加加载状态
   - 防抖处理
   - 缓存数据（可选）

### 5. 页面布局示例

```razor
@page "/hangfire/recurring-jobs"
@inherits BreadcrumbComponentBase

<PageTitle>周期性任务管理</PageTitle>

<PageBreadcrumb ... />

<div class="recurring-jobs-page mt-4">
    <Row>
        <Column>
            <Card>
                <CardHeader>
                    <CardTitle>
                        <Icon Name="IconName.Clock" />
                        周期性任务管理
                    </CardTitle>
                    <CardActions>
                        <Button Color="Color.Primary" Clicked="RefreshJobs">
                            <Icon Name="IconName.Refresh" />
                            刷新
                        </Button>
                        <Button Color="Color.Info" Clicked="NavigateToDashboard">
                            <Icon Name="IconName.ExternalLink" />
                            打开 Hangfire Dashboard
                        </Button>
                    </CardActions>
                </CardHeader>
                <CardBody>
                    <DataGrid TItem="RecurringJobDto"
                              Data="@recurringJobs"
                              TotalItems="@totalCount"
                              ReadData="@OnReadData"
                              Loading="@isLoading">
                        <DataGridColumns>
                            <DataGridColumn TItem="RecurringJobDto" Field="@nameof(RecurringJobDto.Id)">
                                任务 ID
                            </DataGridColumn>
                            <DataGridColumn TItem="RecurringJobDto" Field="@nameof(RecurringJobDto.Cron)">
                                Cron 表达式
                            </DataGridColumn>
                            <DataGridColumn TItem="RecurringJobDto" Field="@nameof(RecurringJobDto.TimeZoneId)">
                                时区
                            </DataGridColumn>
                            <DataGridColumn TItem="RecurringJobDto" Field="@nameof(RecurringJobDto.NextExecution)">
                                下次执行
                            </DataGridColumn>
                            <DataGridColumn TItem="RecurringJobDto" Field="@nameof(RecurringJobDto.LastExecution)">
                                最后执行
                            </DataGridColumn>
                            <DataGridColumn TItem="RecurringJobDto">
                                状态
                                <DisplayTemplate>
                                    <Badge Color="@(context.IsPaused ? Color.Warning : Color.Success)">
                                        @(context.IsPaused ? "已暂停" : "运行中")
                                    </Badge>
                                </DisplayTemplate>
                            </DataGridColumn>
                            <DataGridColumn TItem="RecurringJobDto">
                                操作
                                <DisplayTemplate>
                                    @if (CanEdit)
                                    {
                                        @if (context.IsPaused)
                                        {
                                            <Button Color="Color.Success" Size="Size.Small"
                                                    Clicked="@(() => ResumeJob(context.Id))">
                                                <Icon Name="IconName.Play" />
                                                恢复
                                            </Button>
                                        }
                                        else
                                        {
                                            <Button Color="Color.Warning" Size="Size.Small"
                                                    Clicked="@(() => PauseJob(context.Id))">
                                                <Icon Name="IconName.Pause" />
                                                暂停
                                            </Button>
                                        }
                                    }
                                </DisplayTemplate>
                            </DataGridColumn>
                        </DataGridColumns>
                    </DataGrid>
                </CardBody>
            </Card>
        </Column>
    </Row>
</div>
```

### 6. 技术实现细节

#### HTTP 客户端配置

在 `RecurringJobs.razor.cs` 中注入 `HttpClient`：

```csharp
[Inject]
protected HttpClient HttpClient { get; set; } = default!;
```

#### API 调用示例

```csharp
private async Task LoadRecurringJobsAsync()
{
    try
    {
        isLoading = true;
        var response = await HttpClient.GetFromJsonAsync<ApiResponse<List<RecurringJobDto>>>(
            "/api/hangfire/recurring/list");

        if (response?.Jobs != null)
        {
            recurringJobs = response.Jobs;
            totalCount = response.Count;
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载周期性任务列表失败");
        await ModalService.Alert("错误", "加载任务列表失败：" + ex.Message);
    }
    finally
    {
        isLoading = false;
    }
}
```

#### 权限检查

```csharp
[Inject]
protected IPermissionChecker PermissionChecker { get; set; } = default!;

private bool CanEdit => await PermissionChecker.IsGrantedAsync(
    PaperBellStorePermissions.HangfireDashboardEdit);
```

### 7. UI/UX 设计建议

#### 颜色方案

- **运行中**：绿色 Badge
- **已暂停**：黄色 Badge
- **暂停按钮**：黄色/警告色
- **恢复按钮**：绿色/成功色

#### 图标使用

- 时钟图标：周期性任务
- 播放图标：恢复
- 暂停图标：暂停
- 刷新图标：刷新

#### 交互反馈

- 按钮点击后显示加载状态
- 操作成功后显示提示消息
- 操作失败后显示错误消息
- 自动刷新列表

### 8. 参考资源

#### 现有页面参考

- `AuditLog.razor` - 审计日志页面（数据表格、过滤、分页）
- `RunningLog.razor` - 运行日志页面（实时刷新、详情查看）

#### API 参考

- `HangfireRecurringJobController.cs` - 已实现的 API 端点

### 9. 实施时间估算

- **步骤 1**：2-3 小时（页面结构、数据模型）
- **步骤 2**：2-3 小时（UI 界面）
- **步骤 3**：1-2 小时（业务逻辑）
- **步骤 4**：1-2 小时（集成和优化）

**总计**：6-10 小时

---

## ⚠️ 注意事项

1. **任务配置信息丢失问题**：

   - 暂停时删除任务会丢失配置信息
   - 必须在删除前保存完整配置信息
   - 恢复时需要能够重新创建任务

2. **任务类型识别**：

   - 需要能够识别任务类型（如 `LogCleanupRecurringJob`）
   - 恢复时需要能够重新创建相同类型的任务
   - 在 `RecurringJobRecoveryService` 中注册任务恢复方法

3. **权限控制**：

   - 暂停/恢复操作需要 `HangfireDashboard.Edit` 权限
   - 查看任务列表需要 `HangfireDashboard.View` 权限
   - 在 Blazor 页面中根据权限显示/隐藏操作按钮

4. **并发安全**：

   - 多个用户同时操作同一任务时的并发控制
   - API 端点已实现状态检查，防止重复操作

5. **Hangfire Dashboard 保留**：
   - Hangfire Dashboard 的原始功能完全保留
   - 可以通过链接跳转到 Dashboard 查看详细信息
   - 两个页面可以并存使用

---

## 📚 参考资源

- [Hangfire RecurringJob API](https://docs.hangfire.io/en/latest/background-methods/calling-methods-with-delay.html#recurring-jobs)
- [ABP 权限系统文档](https://docs.abp.io/en/abp/latest/Authorization)
- [Blazorise 文档](https://blazorise.com/)
- [Blazor Server 文档](https://learn.microsoft.com/zh-cn/aspnet/core/blazor/)

---

## ✅ 验收标准

1. ✅ 可以访问自定义的周期性任务管理页面（`/hangfire/recurring-jobs`）
2. ✅ 页面能够显示所有周期性任务列表
3. ✅ 运行中的任务显示"暂停"按钮
4. ✅ 已暂停的任务显示"恢复"按钮
5. ✅ 点击暂停后，任务状态正确更新为已暂停
6. ✅ 点击恢复后，任务重新开始执行
7. ✅ 权限控制正常工作，无权限用户无法看到操作按钮
8. ✅ 任务配置信息在暂停后能够完整恢复
9. ✅ 可以跳转到 Hangfire Dashboard 查看详细信息

---

---

## 📦 已实现文件清单

### 服务层

1. `src/PaperBellStore.Blazor/Services/RecurringJobStateService.cs` - 任务状态管理服务
2. `src/PaperBellStore.Blazor/Services/RecurringJobRecoveryService.cs` - 任务恢复服务

### 控制器层

3. `src/PaperBellStore.Blazor/Controllers/HangfireRecurringJobController.cs` - API 控制器

### 页面层（待实施）

4. `src/PaperBellStore.Blazor/Components/Pages/RecurringJobs.razor` - 周期性任务管理页面（待创建）
5. `src/PaperBellStore.Blazor/Components/Pages/RecurringJobs.razor.cs` - 页面逻辑（待创建）
6. `src/PaperBellStore.Blazor/Components/Pages/RecurringJobs.razor.css` - 页面样式（待创建）

**详细实施计划**：请参考上方"📐 自定义 Blazor 页面详细设计"章节。

### 配置更新

7. `src/PaperBellStore.Blazor/PaperBellStoreBlazorModule.cs` - Hangfire Dashboard 配置
8. `src/PaperBellStore.Blazor/Menus/PaperBellStoreMenus.cs` - 菜单配置（待更新）

---

## 🚀 使用说明

### 1. 访问管理页面

访问 `/hangfire/recurring-jobs` 路径，进入周期性任务管理页面。

### 2. 查看周期性任务

页面会自动加载并显示所有周期性任务列表，包括：

- 运行中的任务
- 已暂停的任务

### 3. 暂停任务

- 在任务列表的"操作"列中，找到运行中的任务
- 点击"暂停"按钮
- 确认后，任务将被暂停（从 Hangfire 中删除，但配置信息已保存）
- 页面会自动刷新，显示更新后的状态

### 4. 恢复任务

- 在任务列表中找到已暂停的任务（状态显示为"已暂停"）
- 点击"恢复"按钮
- 确认后，任务将重新注册并开始执行
- 页面会自动刷新，显示更新后的状态

### 5. 查看详细信息

- 点击"打开 Hangfire Dashboard"按钮，跳转到 Hangfire Dashboard 查看任务的详细执行信息

### 6. API 调用示例

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
3. **权限要求**：暂停/恢复操作需要 `HangfireDashboard.Edit` 权限，查看列表需要 `HangfireDashboard.View` 权限
4. **页面路由**：自定义管理页面路由为 `/hangfire/recurring-jobs`，与 Hangfire Dashboard 的 `/hangfire` 路径不冲突
5. **Hangfire Dashboard**：原始 Dashboard 功能完全保留，可通过链接跳转访问

---

---

## 🧹 已清理的无效代码

由于方案变更为自定义 Blazor 页面，以下 Dashboard 扩展相关文件已删除：

### 已删除文件

1. ❌ `src/PaperBellStore.Blazor/wwwroot/hangfire-dashboard-extensions.js`
2. ❌ `src/PaperBellStore.Blazor/wwwroot/hangfire-dashboard-extensions.css`
3. ❌ `src/PaperBellStore.Blazor/wwwroot/hangfire-dashboard-loader.js`
4. ❌ `src/PaperBellStore.Blazor/Extensions/HangfireDashboardExtensions.cs`
5. ❌ `src/PaperBellStore.Blazor/Middleware/HangfireDashboardInjectionMiddleware.cs`

### 已移除的配置

- ❌ `app.UseHangfireDashboardExtensions()` - 已从 `PaperBellStoreBlazorModule.cs` 移除
- ❌ `app.UseMiddleware<HangfireDashboardInjectionMiddleware>()` - 已从 `PaperBellStoreBlazorModule.cs` 移除

### 保留的功能

✅ **Hangfire Dashboard**：原始 Dashboard 功能完全保留，可通过 `/hangfire` 访问  
✅ **API 端点**：所有 API 端点保持不变，可供 Blazor 页面调用  
✅ **服务层**：任务状态管理和恢复服务保持不变

---

**文档版本**：v2.1  
**创建日期**：2024 年  
**最后更新**：2024 年（已合并详细实施计划，方案变更为自定义 Blazor 页面）

**变更说明**：

- v1.0：尝试通过 Dashboard 扩展注入脚本（因 AJAX 加载问题未成功）
- v2.0：改为创建自定义 Blazor 管理页面（方案一）
- v2.1：合并详细实施计划到主文档，删除独立的管理页面实施方案文档
