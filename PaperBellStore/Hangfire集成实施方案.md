# Hangfire 定时任务集成实施方案

## 项目现状分析

当前项目已经使用了 ABP 框架的 BackgroundJobs 模块：

- ✅ `AbpBackgroundJobsDomainModule` - 领域层后台作业模块
- ✅ `AbpBackgroundJobsEntityFrameworkCoreModule` - EF Core 后台作业模块
- ✅ 数据库表 `AbpBackgroundJobs` 已创建

**数据库**：PostgreSQL
**ABP 版本**：9.2.1
**.NET 版本**：9.0

---

## 方案对比

### 方案一：使用 ABP Hangfire 集成模块（推荐）⭐

**优点**：

- ✅ 与 ABP 框架深度集成
- ✅ 可以使用 ABP 的 `IBackgroundJobManager` 接口
- ✅ 支持 ABP 的多租户、权限等功能
- ✅ 统一的作业管理接口
- ✅ 符合 ABP 框架最佳实践

**缺点**：

- ⚠️ 需要替换现有的 ABP BackgroundJobs 实现
- ⚠️ 功能相对受限（受 ABP 抽象层限制）

**适用场景**：

- 需要与 ABP 框架深度集成
- 需要多租户支持
- 需要统一的作业管理接口

---

### 方案二：直接使用 Hangfire（灵活方案）

**优点**：

- ✅ 功能完整，不受 ABP 抽象层限制
- ✅ 可以使用 Hangfire 的所有特性（Dashboard、重试、延迟任务等）
- ✅ 可以与 ABP BackgroundJobs 共存
- ✅ 更灵活的任务调度方式

**缺点**：

- ⚠️ 需要手动处理 ABP 的依赖注入
- ⚠️ 需要手动处理多租户（如果启用）
- ⚠️ 需要单独管理 Hangfire 任务

**适用场景**：

- 需要 Hangfire 的完整功能（Dashboard、复杂调度等）
- 需要与 ABP BackgroundJobs 共存
- 需要更灵活的任务管理

---

### 方案三：混合方案（推荐用于复杂场景）⭐

**优点**：

- ✅ 同时使用 ABP BackgroundJobs 和 Hangfire
- ✅ 简单任务使用 ABP BackgroundJobs
- ✅ 复杂任务使用 Hangfire
- ✅ 充分利用两种方案的优势

**缺点**：

- ⚠️ 需要维护两套任务系统
- ⚠️ 配置相对复杂

**适用场景**：

- 需要同时使用简单后台任务和复杂定时任务
- 需要 Hangfire Dashboard 管理复杂任务
- 需要保留 ABP BackgroundJobs 的简单任务

---

## 推荐方案

**⭐ 推荐使用方案二（直接使用 Hangfire）**，原因：

1. 项目已使用 PostgreSQL，Hangfire 对 PostgreSQL 支持良好
2. 可以保留现有的 ABP BackgroundJobs，两者共存
3. Hangfire Dashboard 提供强大的任务管理界面
4. 支持复杂的 Cron 表达式和任务调度
5. 支持任务重试、延迟执行等高级功能

---

## 实施方案一：使用 ABP Hangfire 集成模块

### 第一步：安装 NuGet 包

在 `PaperBellStore.Blazor.csproj` 中添加：

```xml
<ItemGroup>
  <PackageReference Include="Volo.Abp.BackgroundJobs.Hangfire" Version="9.2.1" />
  <PackageReference Include="Hangfire.PostgreSql" Version="1.20.6" />
</ItemGroup>
```

### 第二步：配置模块

修改 `PaperBellStoreBlazorModule.cs`：

```csharp
using Volo.Abp.BackgroundJobs.Hangfire;

[DependsOn(
    // ... 现有依赖 ...
    typeof(AbpBackgroundJobsHangfireModule)  // 添加此行
)]
public class PaperBellStoreBlazorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // ... 现有配置 ...

        var configuration = context.Services.GetConfiguration();

        // 配置 Hangfire
        context.Services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(
                configuration.GetConnectionString("Default"),
                new Hangfire.PostgreSql.PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire"  // 可选：指定 Schema 名称
                });
        });

        // 添加 Hangfire 服务器
        context.Services.AddHangfireServer();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // ... 现有配置 ...

        var app = context.GetApplicationBuilder();

        // 配置 Hangfire Dashboard（可选：添加授权）
        app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() }  // 需要实现授权过滤器
        });
    }
}
```

### 第三步：创建授权过滤器（可选）

创建 `HangfireAuthorizationFilter.cs`：

```csharp
using Hangfire.Dashboard;

namespace PaperBellStore.Blazor
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // 实现授权逻辑
            // 例如：检查用户是否有权限访问
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated == true;
        }
    }
}
```

### 第四步：使用 ABP BackgroundJobManager

创建任务类：

```csharp
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;

namespace PaperBellStore.Application
{
    public class SampleBackgroundJob : AsyncBackgroundJob<SampleBackgroundJobArgs>, ITransientDependency
    {
        public override async Task ExecuteAsync(SampleBackgroundJobArgs args)
        {
            // 执行任务逻辑
            await Task.CompletedTask;
        }
    }

    public class SampleBackgroundJobArgs
    {
        public string Message { get; set; }
    }
}
```

使用任务：

```csharp
public class MyService
{
    private readonly IBackgroundJobManager _backgroundJobManager;

    public MyService(IBackgroundJobManager backgroundJobManager)
    {
        _backgroundJobManager = backgroundJobManager;
    }

    public async Task EnqueueJobAsync()
    {
        await _backgroundJobManager.EnqueueAsync(
            new SampleBackgroundJobArgs { Message = "Hello" }
        );
    }
}
```

---

## 实施方案二：直接使用 Hangfire（推荐）⭐

### 第一步：安装 NuGet 包

在 `PaperBellStore.Blazor.csproj` 中添加：

```xml
<ItemGroup>
  <PackageReference Include="Hangfire.Core" Version="1.8.17" />
  <PackageReference Include="Hangfire.AspNetCore" Version="1.8.17" />
  <PackageReference Include="Hangfire.PostgreSql" Version="1.20.6" />
</ItemGroup>
```

### 第二步：配置 Hangfire

修改 `PaperBellStoreBlazorModule.cs`：

```csharp
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Dashboard;

namespace PaperBellStore.Blazor
{
    [DependsOn(
        // ... 现有依赖保持不变 ...
    )]
    public class PaperBellStoreBlazorModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // ... 现有配置 ...

            var configuration = context.Services.GetConfiguration();

            // 配置 Hangfire
            context.Services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(
                    configuration.GetConnectionString("Default"),
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire",  // 可选：指定 Schema 名称
                        QueuePollInterval = TimeSpan.FromSeconds(15),  // 轮询间隔
                        JobExpirationCheckInterval = TimeSpan.FromHours(1),  // 作业过期检查间隔
                        DistributedLockLifetime = TimeSpan.FromSeconds(30),  // 分布式锁生命周期
                        PrepareSchemaIfNecessary = true,  // 自动创建表结构
                        EnableTransactionScopeEnlistment = true  // 启用事务范围
                    });

                // 配置序列化器（可选）
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
            });

            // 添加 Hangfire 服务器
            context.Services.AddHangfireServer(options =>
            {
                options.ServerName = "PaperBellStore-Server";  // 服务器名称
                options.WorkerCount = Environment.ProcessorCount * 5;  // 工作线程数
                options.Queues = new[] { "default", "critical", "low" };  // 队列名称
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            // ... 现有配置 ...

            var app = context.GetApplicationBuilder();

            // 配置 Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                DashboardTitle = "PaperBellStore 任务调度中心",
                Authorization = new[] { new HangfireAuthorizationFilter() },
                // 可选：配置统计面板
                StatsPollingInterval = 2000,
                // 可选：配置显示选项
                DisplayStorageConnectionString = false,  // 不显示连接字符串
                IsReadOnlyFunc = (DashboardContext ctx) => false  // 是否只读
            });
        }
    }
}
```

### 第三步：创建授权过滤器

创建 `HangfireAuthorizationFilter.cs`：

```csharp
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace PaperBellStore.Blazor
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // 检查用户是否已认证
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            // 可选：检查用户权限
            // 例如：只有管理员才能访问
            // return httpContext.User.IsInRole("admin");

            return true;
        }
    }
}
```

### 第四步：创建定时任务类

创建 `RecurringJobs/SampleRecurringJob.cs`：

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace PaperBellStore.Blazor.RecurringJobs
{
    /// <summary>
    /// 示例定时任务
    /// </summary>
    public class SampleRecurringJob : ITransientDependency
    {
        private readonly ILogger<SampleRecurringJob> _logger;

        public SampleRecurringJob(ILogger<SampleRecurringJob> logger)
        {
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("定时任务执行开始：{Time}", DateTime.Now);

            try
            {
                // 执行任务逻辑
                await Task.Delay(1000);  // 模拟异步操作

                _logger.LogInformation("定时任务执行完成：{Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时任务执行失败：{Time}", DateTime.Now);
                throw;
            }
        }
    }
}
```

### 第五步：注册定时任务

在 `PaperBellStoreBlazorModule.cs` 的 `OnApplicationInitialization` 方法中注册：

```csharp
using Hangfire;
using PaperBellStore.Blazor.RecurringJobs;

public override void OnApplicationInitialization(ApplicationInitializationContext context)
{
    // ... 现有配置 ...

    // 注册定时任务
    var recurringJobManager = context.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // 方式一：使用 Cron 表达式
    RecurringJob.AddOrUpdate<SampleRecurringJob>(
        "sample-job-daily",  // 任务ID（唯一标识）
        job => job.ExecuteAsync(),
        Cron.Daily(23, 30),  // 每天 23:30 执行
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local  // 时区
        });

    // 方式二：使用自定义 Cron 表达式
    RecurringJob.AddOrUpdate<SampleRecurringJob>(
        "sample-job-custom",
        job => job.ExecuteAsync(),
        "0 */5 * * *",  // 每5分钟执行一次
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local
        });

    // 方式三：使用 Hangfire 预定义的 Cron
    RecurringJob.AddOrUpdate<SampleRecurringJob>(
        "sample-job-hourly",
        job => job.ExecuteAsync(),
        Cron.Hourly());  // 每小时执行一次
}
```

### 第六步：创建数据库表（自动创建）

Hangfire 会在首次运行时自动创建所需的数据库表。如果使用自定义 Schema，确保数据库用户有创建表的权限。

**手动创建表（可选）**：

如果希望手动创建表，可以执行以下 SQL：

```sql
-- 创建 Schema（如果使用自定义 Schema）
CREATE SCHEMA IF NOT EXISTS hangfire;

-- Hangfire 会自动创建以下表：
-- hangfire.job
-- hangfire.jobparameter
-- hangfire.jobqueue
-- hangfire.jobstate
-- hangfire.server
-- hangfire.set
-- hangfire.list
-- hangfire.hash
-- hangfire.counter
-- hangfire.aggregatedcounter
-- hangfire.lock
-- hangfire.schema
```

---

## 实施方案三：混合方案

### 配置说明

混合方案同时使用 ABP BackgroundJobs 和 Hangfire：

1. **简单后台任务**：使用 ABP `IBackgroundJobManager`
2. **复杂定时任务**：使用 Hangfire `RecurringJob`

### 配置步骤

按照方案二的步骤配置 Hangfire，同时保留现有的 ABP BackgroundJobs 配置。

### 使用示例

```csharp
public class MyService
{
    private readonly IBackgroundJobManager _backgroundJobManager;  // ABP 后台任务
    private readonly IRecurringJobManager _recurringJobManager;    // Hangfire 定时任务

    public MyService(
        IBackgroundJobManager backgroundJobManager,
        IRecurringJobManager recurringJobManager)
    {
        _backgroundJobManager = backgroundJobManager;
        _recurringJobManager = recurringJobManager;
    }

    // 使用 ABP BackgroundJobs（简单任务）
    public async Task EnqueueSimpleJobAsync()
    {
        await _backgroundJobManager.EnqueueAsync(
            new SampleBackgroundJobArgs { Message = "Hello" }
        );
    }

    // 使用 Hangfire（复杂定时任务）
    public void ScheduleRecurringJob()
    {
        RecurringJob.AddOrUpdate<SampleRecurringJob>(
            "complex-job",
            job => job.ExecuteAsync(),
            Cron.Daily());
    }
}
```

---

## Cron 表达式参考

### Hangfire 预定义 Cron

```csharp
Cron.Minutely()        // 每分钟
Cron.Hourly()          // 每小时
Cron.Daily()           // 每天 00:00
Cron.Weekly()          // 每周一 00:00
Cron.Monthly()         // 每月1号 00:00
Cron.Yearly()          // 每年1月1号 00:00

Cron.Daily(23, 30)     // 每天 23:30
Cron.Weekly(DayOfWeek.Monday, 9, 0)  // 每周一 09:00
```

### 自定义 Cron 表达式

```
格式：分 时 日 月 周

示例：
"0 */5 * * *"          // 每5分钟
"0 0 * * *"            // 每天 00:00
"0 0 * * 1"            // 每周一 00:00
"0 0 1 * *"            // 每月1号 00:00
"0 9-17 * * 1-5"       // 工作日上午9点到下午5点，每小时执行
"0 0 1 1 *"            // 每年1月1号 00:00
"*/30 * * * *"         // 每30秒
```

---

## 高级功能

### 1. 延迟任务

```csharp
BackgroundJob.Schedule<SampleRecurringJob>(
    job => job.ExecuteAsync(),
    TimeSpan.FromMinutes(30));  // 30分钟后执行
```

### 2. 一次性任务

```csharp
BackgroundJob.Enqueue<SampleRecurringJob>(
    job => job.ExecuteAsync());
```

### 3. 任务重试

```csharp
// 在任务类中处理异常，Hangfire 会自动重试
public async Task ExecuteAsync()
{
    try
    {
        // 任务逻辑
    }
    catch (Exception ex)
    {
        // 记录日志
        // Hangfire 会自动重试（默认重试3次）
        throw;  // 重新抛出异常以触发重试
    }
}
```

### 4. 任务参数

```csharp
public class ParameterizedJob : ITransientDependency
{
    public async Task ExecuteAsync(string message, int count)
    {
        // 使用参数
    }
}

// 注册任务
RecurringJob.AddOrUpdate<ParameterizedJob>(
    "parameterized-job",
    job => job.ExecuteAsync("Hello", 10),
    Cron.Daily());
```

### 5. 任务队列

```csharp
// 配置多个队列
context.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "default", "critical", "low" };
});

// 指定队列执行任务
BackgroundJob.Enqueue<SampleRecurringJob>(
    job => job.ExecuteAsync(),
    "critical");  // 使用 critical 队列
```

---

## 配置建议

### appsettings.json 配置

```json
{
  "Hangfire": {
    "ServerName": "PaperBellStore-Server",
    "WorkerCount": 10,
    "Queues": ["default", "critical", "low"],
    "SchemaName": "hangfire",
    "Dashboard": {
      "Path": "/hangfire",
      "Title": "PaperBellStore 任务调度中心",
      "StatsPollingInterval": 2000
    }
  }
}
```

### 读取配置

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    var configuration = context.Services.GetConfiguration();
    var hangfireConfig = configuration.GetSection("Hangfire");

    context.Services.AddHangfireServer(options =>
    {
        options.ServerName = hangfireConfig["ServerName"] ?? "PaperBellStore-Server";
        options.WorkerCount = int.Parse(hangfireConfig["WorkerCount"] ?? "10");
        options.Queues = hangfireConfig.GetSection("Queues").Get<string[]>()
            ?? new[] { "default" };
    });
}
```

---

## 安全配置

### 1. Dashboard 授权

```csharp
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 检查认证
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        // 检查角色
        if (!httpContext.User.IsInRole("admin"))
        {
            return false;
        }

        // 检查权限（使用 ABP 权限系统）
        var permissionChecker = httpContext.RequestServices
            .GetRequiredService<IPermissionChecker>();

        return permissionChecker.IsGrantedAsync("Hangfire.Dashboard")
            .GetAwaiter().GetResult();

        return true;
    }
}
```

### 2. 隐藏连接字符串

```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DisplayStorageConnectionString = false  // 不显示连接字符串
});
```

---

## 监控和日志

### 1. 集成 Serilog

Hangfire 会自动使用已配置的 Serilog，无需额外配置。

### 2. 任务执行日志

```csharp
public class SampleRecurringJob : ITransientDependency
{
    private readonly ILogger<SampleRecurringJob> _logger;

    public SampleRecurringJob(ILogger<SampleRecurringJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("任务开始执行");

        try
        {
            // 任务逻辑
            await Task.CompletedTask;

            _logger.LogInformation("任务执行成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "任务执行失败");
            throw;
        }
    }
}
```

---

## 性能优化建议

### 1. 工作线程数

```csharp
context.Services.AddHangfireServer(options =>
{
    // 根据服务器 CPU 核心数设置
    options.WorkerCount = Environment.ProcessorCount * 5;
});
```

### 2. 队列分离

```csharp
// 将不同类型的任务分配到不同队列
options.Queues = new[] { "default", "critical", "low" };
```

### 3. 数据库连接池

确保 PostgreSQL 连接池配置合理：

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=PpbStore;UserName=postgres;Password=123456;Maximum Pool Size=100;"
  }
}
```

---

## 常见问题

### 1. 任务不执行

- 检查 Hangfire Server 是否启动
- 检查数据库连接是否正常
- 检查 Cron 表达式是否正确
- 查看 Hangfire Dashboard 中的错误信息

### 2. 任务执行失败

- 查看任务执行日志
- 检查任务代码中的异常处理
- 检查数据库连接和权限

### 3. Dashboard 无法访问

- 检查授权过滤器配置
- 检查路由配置
- 检查用户认证状态

---

## 实施步骤总结

### 方案二（推荐）实施步骤：

1. ✅ 安装 NuGet 包（Hangfire.Core、Hangfire.AspNetCore、Hangfire.PostgreSql）
2. ✅ 在 `PaperBellStoreBlazorModule` 中配置 Hangfire
3. ✅ 创建授权过滤器
4. ✅ 创建定时任务类
5. ✅ 在模块初始化中注册定时任务
6. ✅ 配置 Dashboard
7. ✅ 测试验证

---

## 推荐配置

**针对你的项目（PaperBellStore）**：

⭐ **推荐使用方案二（直接使用 Hangfire）**

**理由**：

1. ✅ 可以保留现有的 ABP BackgroundJobs
2. ✅ Hangfire Dashboard 提供强大的任务管理界面
3. ✅ 支持复杂的 Cron 表达式和任务调度
4. ✅ 支持任务重试、延迟执行等高级功能
5. ✅ 与 PostgreSQL 集成良好

**配置建议**：

- 使用独立的 Schema（`hangfire`）存储 Hangfire 表
- 配置 Dashboard 授权，确保只有授权用户可访问
- 根据实际需求配置工作线程数和队列
- 集成 Serilog 日志系统，记录任务执行日志
