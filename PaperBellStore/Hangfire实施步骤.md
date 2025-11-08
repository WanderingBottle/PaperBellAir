# Hangfire 定时任务集成实施步骤

本文档提供 Hangfire 定时任务集成的完整实施步骤，包括方案选择、安装配置、数据库设置、菜单集成等。

---

## 📋 目录

1. [项目现状分析](#1-项目现状分析)
2. [方案对比与选择](#2-方案对比与选择)
3. [安装与配置](#3-安装与配置)
4. [数据库配置](#4-数据库配置)
5. [菜单集成](#5-菜单集成)
6. [创建定时任务](#6-创建定时任务)
7. [高级功能](#7-高级功能)
8. [性能优化](#8-性能优化)
9. [安全配置](#9-安全配置)

---

## 1. 项目现状分析

### 当前环境

- **数据库**：PostgreSQL
- **ABP 版本**：9.2.1
- **.NET 版本**：9.0
- **现有功能**：已使用 ABP BackgroundJobs 模块

### 现有模块

- ✅ `AbpBackgroundJobsDomainModule` - 领域层后台作业模块
- ✅ `AbpBackgroundJobsEntityFrameworkCoreModule` - EF Core 后台作业模块
- ✅ 数据库表 `AbpBackgroundJobs` 已创建

---

## 2. 方案对比与选择

### 方案一：使用 ABP Hangfire 集成模块

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

### 方案二：直接使用 Hangfire（推荐）⭐

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

### 方案三：混合方案

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

### 推荐方案

**⭐ 推荐使用方案二（直接使用 Hangfire）**

**理由**：

1. 项目已使用 PostgreSQL，Hangfire 对 PostgreSQL 支持良好
2. 可以保留现有的 ABP BackgroundJobs，两者共存
3. Hangfire Dashboard 提供强大的任务管理界面
4. 支持复杂的 Cron 表达式和任务调度
5. 支持任务重试、延迟执行等高级功能

---

## 3. 安装与配置

### 3.1 安装 NuGet 包

在 `PaperBellStore.Blazor.csproj` 中添加：

```xml
<ItemGroup>
  <PackageReference Include="Hangfire.Core" Version="1.8.17" />
  <PackageReference Include="Hangfire.AspNetCore" Version="1.8.17" />
  <PackageReference Include="Hangfire.PostgreSql" Version="1.20.6" />
</ItemGroup>
```

### 3.2 配置 Hangfire

修改 `PaperBellStoreBlazorModule.cs`：

```csharp
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Dashboard;

namespace PaperBellStore.Blazor
{
    public class PaperBellStoreBlazorModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // ... 现有配置 ...

            var configuration = context.Services.GetConfiguration();
            var connectionString = configuration.GetConnectionString("Default");

            // 配置 Hangfire（使用推荐的方法）
            context.Services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(options =>
                {
                    // 使用推荐的方法设置连接字符串
                    options.UseNpgsqlConnection(connectionString);
                });

                // 配置序列化器
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
            });

            // 添加 Hangfire 服务器
            context.Services.AddHangfireServer(options =>
            {
                options.ServerName = "PaperBellStore-Server";
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
                StatsPollingInterval = 2000,  // 统计信息轮询间隔（毫秒）
                DisplayStorageConnectionString = false,  // 不显示连接字符串
                IsReadOnlyFunc = (DashboardContext ctx) => false  // 是否只读
            });

            // 注册定时任务
            RegisterRecurringJobs(context);
        }

        private void RegisterRecurringJobs(ApplicationInitializationContext context)
        {
            // 注册定时任务
            RecurringJob.AddOrUpdate<SampleRecurringJob>(
                "sample-job-daily",
                job => job.ExecuteAsync(),
                Cron.Daily(23, 30),  // 每天 23:30 执行
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Local
                });
        }
    }
}
```

### 3.3 创建授权过滤器

创建 `Filters/HangfireAuthorizationFilter.cs`：

```csharp
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace PaperBellStore.Blazor.Filters
{
    /// <summary>
    /// Hangfire Dashboard 授权过滤器
    /// </summary>
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

            // 可选：检查用户角色或权限
            // 例如：只有管理员才能访问
            // return httpContext.User.IsInRole("admin");

            // 默认：已认证的用户都可以访问
            return true;
        }
    }
}
```

### 3.4 配置更新说明

**当前使用的配置方法（推荐）**：

```csharp
config.UsePostgreSqlStorage(options =>
{
    options.UseNpgsqlConnection(connectionString);
});
```

**说明**：

- 使用 Hangfire.PostgreSql 1.20.6 版本推荐的新方法
- Hangfire 默认使用 `"hangfire"` Schema
- 表自动创建功能默认启用
- 其他配置选项使用默认值

---

## 4. 数据库配置

### 4.1 数据库存储说明

Hangfire 使用 PostgreSQL 作为持久化存储，所有定时任务、任务执行历史、任务队列等信息都会存储到数据库中。

**存储位置**：

- **数据库**：PostgreSQL（使用 `Default` 连接字符串）
- **Schema**：`hangfire`（独立的 Schema，与业务数据隔离）
- **自动创建**：首次运行时会自动创建表结构

### 4.2 数据库表结构

Hangfire 会在 `hangfire` Schema 下自动创建以下表：

| 表名                         | 用途             | 说明                                            |
| ---------------------------- | ---------------- | ----------------------------------------------- |
| **`hangfire.set`**           | **定时任务存储** | ⭐ **存储所有定时任务（Recurring Jobs）的定义** |
| `hangfire.job`               | 任务表           | 存储所有任务（包括定时任务触发的任务）          |
| `hangfire.jobparameter`      | 任务参数表       | 存储任务的参数                                  |
| `hangfire.jobqueue`          | 任务队列表       | 存储待执行的任务队列                            |
| `hangfire.jobstate`          | 任务状态表       | 存储任务执行状态和历史                          |
| `hangfire.server`            | 服务器表         | 存储 Hangfire 服务器信息                        |
| `hangfire.list`              | 列表表           | 存储列表类型的数据                              |
| `hangfire.hash`              | 哈希表           | 存储哈希类型的数据                              |
| `hangfire.counter`           | 计数器表         | 存储计数器数据                                  |
| `hangfire.aggregatedcounter` | 聚合计数器表     | 存储聚合计数器数据                              |
| `hangfire.lock`              | 锁表             | 存储分布式锁信息                                |
| `hangfire.schema`            | Schema 版本表    | 存储 Hangfire Schema 版本信息                   |

### 4.3 表创建机制

**重要说明**：Hangfire 表**不是**通过 EF Core + ABP 迁移生成的。

**创建方式**：

- Hangfire 在首次运行时自动检查并创建表
- 不需要 EF Core 迁移
- 不需要 `DbMigrator`
- 配置项：`PrepareSchemaIfNecessary = true`（默认启用）

**工作原理**：

1. 应用启动时检查 `hangfire` Schema 是否存在
2. 检查所需的表是否存在
3. 如果不存在，自动创建 Schema 和所有必需的表
4. 使用 Hangfire 内置的 SQL 脚本创建表
5. 使用 `hangfire.schema` 表记录 Schema 版本

**与 ABP 表的区别**：

| 对比项       | ABP 框架表           | Hangfire 表                |
| ------------ | -------------------- | -------------------------- |
| **创建方式** | EF Core 迁移         | Hangfire 自动初始化        |
| **管理工具** | `DbMigrator` 项目    | Hangfire 存储提供程序      |
| **迁移文件** | `Migrations` 文件夹  | 无（内置在 Hangfire 中）   |
| **创建时机** | 运行 `DbMigrator` 时 | 应用首次运行时             |
| **配置项**   | `AddDbContext`       | `PrepareSchemaIfNecessary` |
| **依赖**     | EF Core              | Hangfire.PostgreSql        |

### 4.4 独立 Schema 说明

**为什么使用独立的 `hangfire` Schema？**

1. **职责分离**：业务数据与任务数据清晰分离

   - 应用业务表（37 个）在 `public` Schema
   - Hangfire 任务表（12 个）在 `hangfire` Schema

2. **避免命名冲突**：防止表名冲突

3. **更易于管理和维护**：

   - 故障排查更简单
   - 升级更安全
   - 结构更清晰

4. **权限管理更灵活**：可以为 `hangfire` Schema 设置不同的访问权限

**优点**：

- ✅ 职责分离、避免冲突、易于维护、结构清晰、权限灵活

**缺点**：

- ⚠️ 结构稍复杂、查询需要 Schema 前缀（影响很小）

**建议**：保持使用独立的 `hangfire` Schema，这是 Hangfire 官方推荐的做法。

### 4.5 数据持久化机制

**定时任务定义持久化**：

- 存储位置：`hangfire.set` 表
- 存储时机：调用 `AddOrUpdate` 时立即存储
- 特点：即使应用重启，定时任务定义仍然存在

**任务执行记录持久化**：

- 存储位置：`hangfire.job`、`hangfire.jobstate`、`hangfire.jobparameter` 表
- 存储时机：任务创建时、状态变化时、加入队列时
- 特点：任务执行历史永久保存（除非手动清理）

### 4.6 数据库权限要求

**首次运行需要的权限**：

```sql
-- 需要以下权限
GRANT CREATE ON DATABASE PpbStore TO postgres;
GRANT USAGE ON SCHEMA public TO postgres;
GRANT CREATE ON SCHEMA public TO postgres;

-- 如果使用自定义 Schema
CREATE SCHEMA IF NOT EXISTS hangfire;
GRANT ALL ON SCHEMA hangfire TO postgres;
```

### 4.7 数据清理

**自动清理**：

- Hangfire 会自动清理过期的任务记录
- 成功执行的任务：默认保留 24 小时
- 失败的任务：默认保留 7 天

**手动清理**（谨慎操作）：

```sql
-- 清理过期的任务记录
DELETE FROM hangfire.job WHERE "createdat" < NOW() - INTERVAL '7 days';
```

---

## 5. 菜单集成

### 5.1 添加菜单常量

在 `PaperBellStoreMenus.cs` 中添加：

```csharp
public const string HangfireDashboard = Prefix + ".HangfireDashboard";
```

### 5.2 配置菜单项

在 `PaperBellStoreMenuContributor.cs` 中添加：

```csharp
// 添加 Hangfire Dashboard 菜单项
administration.AddItem(new ApplicationMenuItem(
    PaperBellStoreMenus.HangfireDashboard,
    l["Menu:HangfireDashboard"],
    "/hangfire",
    icon: "fas fa-tasks",
    order: 4
));
```

### 5.3 添加本地化文本

**zh-Hans.json**:

```json
"Menu:HangfireDashboard": "任务调度中心"
```

**en.json**:

```json
"Menu:HangfireDashboard": "Job Scheduler"
```

### 5.4 菜单位置

Hangfire Dashboard 菜单项位于 **Administration（管理）** 菜单组下：

- 租户管理（如果启用）
- 身份管理
- 设置管理
- **任务调度中心** ← 新添加的菜单项

### 5.5 可选：添加权限控制

如果需要更细粒度的权限控制：

**步骤 1**：定义权限

在 `PaperBellStorePermissionDefinitionProvider.cs` 中添加：

```csharp
myGroup.AddPermission(
    PaperBellStorePermissions.HangfireDashboard,
    L("Permission:HangfireDashboard")
);
```

**步骤 2**：添加权限常量

在 `PaperBellStorePermissions.cs` 中添加：

```csharp
public const string HangfireDashboard = GroupName + ".HangfireDashboard";
```

**步骤 3**：在菜单中添加权限检查

```csharp
var hangfireMenuItem = new ApplicationMenuItem(
    PaperBellStoreMenus.HangfireDashboard,
    l["Menu:HangfireDashboard"],
    "/hangfire",
    icon: "fas fa-tasks",
    order: 4
);

hangfireMenuItem.RequirePermissions(PaperBellStorePermissions.HangfireDashboard);
administration.AddItem(hangfireMenuItem);
```

**步骤 4**：更新授权过滤器

在 `HangfireAuthorizationFilter.cs` 中添加权限检查：

```csharp
var permissionChecker = httpContext.RequestServices
    .GetRequiredService<IPermissionChecker>();

return permissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboard)
    .GetAwaiter().GetResult();
```

---

## 6. 创建定时任务

### 6.1 创建任务类

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
                throw;  // 重新抛出异常，Hangfire 会自动重试
            }
        }

        /// <summary>
        /// 带参数的任务示例
        /// </summary>
        public async Task ExecuteWithParametersAsync(string message, int count)
        {
            _logger.LogInformation("定时任务执行开始：{Message}, {Count}", message, count);

            try
            {
                // 执行任务逻辑
                for (int i = 0; i < count; i++)
                {
                    _logger.LogInformation("执行第 {Index} 次：{Message}", i + 1, message);
                    await Task.Delay(100);
                }

                _logger.LogInformation("定时任务执行完成：{Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时任务执行失败：{Message}", message);
                throw;
            }
        }
    }
}
```

### 6.2 注册定时任务

在 `PaperBellStoreBlazorModule.cs` 的 `RegisterRecurringJobs` 方法中注册：

```csharp
private void RegisterRecurringJobs(ApplicationInitializationContext context)
{
    // 方式一：使用 Hangfire 预定义的 Cron
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

    // 方式三：带参数的任务
    RecurringJob.AddOrUpdate<SampleRecurringJob>(
        "sample-job-with-parameters",
        job => job.ExecuteWithParametersAsync("Hello", 10),
        Cron.Daily());
}
```

### 6.3 Cron 表达式参考

**Hangfire 预定义 Cron**：

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

**自定义 Cron 表达式**：

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

## 7. 高级功能

### 7.1 延迟任务

```csharp
BackgroundJob.Schedule<SampleRecurringJob>(
    job => job.ExecuteAsync(),
    TimeSpan.FromMinutes(30));  // 30分钟后执行
```

### 7.2 一次性任务

```csharp
BackgroundJob.Enqueue<SampleRecurringJob>(
    job => job.ExecuteAsync());
```

### 7.3 任务重试

在任务类中处理异常，Hangfire 会自动重试（默认重试 3 次）：

```csharp
public async Task ExecuteAsync()
{
    try
    {
        // 任务逻辑
    }
    catch (Exception ex)
    {
        // 记录日志
        _logger.LogError(ex, "任务执行失败");
        throw;  // 重新抛出异常以触发重试
    }
}
```

### 7.4 任务参数

```csharp
// 注册带参数的任务
RecurringJob.AddOrUpdate<ParameterizedJob>(
    "parameterized-job",
    job => job.ExecuteAsync("Hello", 10),
    Cron.Daily());
```

### 7.5 任务队列

```csharp
// 配置多个队列
context.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "default", "critical", "low" };
});

// 指定队列执行任务
var client = new BackgroundJobClient(JobStorage.Current);
var job = Job.FromExpression<SampleRecurringJob>(job => job.ExecuteAsync());
var jobId = client.Create(job, new EnqueuedState("critical"));
```

---

## 8. 性能优化

### 8.1 工作线程数

```csharp
context.Services.AddHangfireServer(options =>
{
    // 根据服务器 CPU 核心数设置
    options.WorkerCount = Environment.ProcessorCount * 5;
});
```

### 8.2 队列分离

```csharp
// 将不同类型的任务分配到不同队列
options.Queues = new[] { "default", "critical", "low" };
```

### 8.3 数据库连接池

确保 PostgreSQL 连接池配置合理：

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=PpbStore;UserName=postgres;Password=123456;Maximum Pool Size=100;"
  }
}
```

---

## 9. 安全配置

### 9.1 Dashboard 授权

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

        // 可选：检查角色
        // if (!httpContext.User.IsInRole("admin"))
        // {
        //     return false;
        // }

        // 可选：检查权限（使用 ABP 权限系统）
        // var permissionChecker = httpContext.RequestServices
        //     .GetRequiredService<IPermissionChecker>();
        // return permissionChecker.IsGrantedAsync("Hangfire.Dashboard")
        //     .GetAwaiter().GetResult();

        return true;
    }
}
```

### 9.2 隐藏连接字符串

```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DisplayStorageConnectionString = false  // 不显示连接字符串
});
```

---

## 📝 实施步骤总结

### 完整实施步骤：

1. ✅ 安装 NuGet 包（Hangfire.Core、Hangfire.AspNetCore、Hangfire.PostgreSql）
2. ✅ 在 `PaperBellStoreBlazorModule` 中配置 Hangfire
3. ✅ 创建授权过滤器
4. ✅ 创建定时任务类
5. ✅ 在模块初始化中注册定时任务
6. ✅ 配置 Dashboard
7. ✅ 集成菜单（可选）
8. ✅ 测试验证

---

## 📚 相关资源

- [Hangfire 官方文档](https://docs.hangfire.io/)
- [Hangfire PostgreSQL 存储文档](https://github.com/frankhommers/Hangfire.PostgreSql)
- [Cron 表达式生成器](https://crontab.guru/)

---

**最后更新**：2024 年

