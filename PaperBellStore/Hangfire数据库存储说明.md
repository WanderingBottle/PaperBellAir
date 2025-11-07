# Hangfire 数据库存储说明

## ✅ 是的，定时任务会存储到数据库中

Hangfire 使用 PostgreSQL 作为持久化存储，所有定时任务、任务执行历史、任务队列等信息都会存储到数据库中。

---

## 📊 存储位置

根据你的配置，Hangfire 使用：

- **数据库**：PostgreSQL（使用 `Default` 连接字符串）
- **Schema**：`hangfire`（独立的 Schema，与业务数据隔离）
- **自动创建**：`PrepareSchemaIfNecessary = true`，首次运行时会自动创建表结构

---

## 🗄️ 数据库表结构

Hangfire 会在 `hangfire` Schema 下自动创建以下表：

### 核心表

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

---

## 🔍 定时任务存储详解

### 1. 定时任务定义存储

当你调用 `RecurringJob.AddOrUpdate` 注册定时任务时：

```csharp
RecurringJob.AddOrUpdate<SampleRecurringJob>(
    "sample-job-daily",  // 任务ID
    job => job.ExecuteAsync(),
    Cron.Daily(23, 30)  // Cron 表达式
);
```

**存储位置**：`hangfire.set` 表

**存储内容**：

- 任务 ID（`sample-job-daily`）
- Cron 表达式（`0 30 23 * *`）
- 任务类型和方法信息
- 时区设置
- 任务状态（启用/禁用）

### 2. 任务执行记录存储

当定时任务触发执行时：

**存储位置**：

- `hangfire.job` 表：存储任务基本信息
- `hangfire.jobparameter` 表：存储任务参数
- `hangfire.jobstate` 表：存储任务执行状态（Pending、Processing、Succeeded、Failed 等）
- `hangfire.jobqueue` 表：存储任务队列信息

---

## 📝 查看数据库中的定时任务

### 方式一：通过 SQL 查询

```sql
-- 连接到数据库
\c PpbStore;

-- 查看所有定时任务
SELECT * FROM hangfire.set WHERE "key" = 'recurring-jobs';

-- 查看任务执行历史
SELECT * FROM hangfire.job
ORDER BY "createdat" DESC
LIMIT 10;

-- 查看任务状态
SELECT j."id", j."stateid", js."name" as state_name, js."createdat"
FROM hangfire.job j
LEFT JOIN hangfire.jobstate js ON j."stateid" = js."id"
ORDER BY j."createdat" DESC
LIMIT 10;
```

### 方式二：通过 Hangfire Dashboard

访问 `https://localhost:44305/hangfire`，在 Dashboard 中可以：

- 查看所有定时任务（Recurring jobs 页面）
- 查看任务执行历史（Jobs 页面）
- 查看任务统计信息

---

## 🔄 数据持久化机制

### 1. 定时任务定义持久化

```csharp
// 注册定时任务时，立即存储到数据库
RecurringJob.AddOrUpdate<SampleRecurringJob>(
    "sample-job-daily",
    job => job.ExecuteAsync(),
    Cron.Daily(23, 30)
);
```

**存储时机**：调用 `AddOrUpdate` 时立即存储到 `hangfire.set` 表

**持久化特点**：

- ✅ 即使应用重启，定时任务定义仍然存在
- ✅ 多个应用实例共享相同的定时任务定义
- ✅ 可以在 Dashboard 中修改定时任务，修改会立即保存到数据库

### 2. 任务执行记录持久化

**存储时机**：

- 任务创建时：存储到 `hangfire.job` 表
- 任务状态变化时：存储到 `hangfire.jobstate` 表
- 任务加入队列时：存储到 `hangfire.jobqueue` 表

**持久化特点**：

- ✅ 任务执行历史永久保存（除非手动清理）
- ✅ 可以查看任务执行历史
- ✅ 任务失败时可以查看失败原因

---

## 🗑️ 数据清理

### 自动清理

Hangfire 会自动清理过期的任务记录：

```csharp
new PostgreSqlStorageOptions
{
    JobExpirationCheckInterval = TimeSpan.FromHours(1),  // 每小时检查一次过期任务
    // ...
}
```

**清理规则**：

- 成功执行的任务：默认保留 24 小时
- 失败的任务：默认保留 7 天
- 可以通过配置调整保留时间

### 手动清理

```sql
-- 清理过期的任务记录（谨慎操作）
-- DELETE FROM hangfire.job WHERE "createdat" < NOW() - INTERVAL '7 days';
```

---

## 📊 数据存储示例

### 示例：查看定时任务定义

```sql
-- 查看所有定时任务
SELECT
    "key",
    "value",
    "score"
FROM hangfire.set
WHERE "key" = 'recurring-jobs';
```

**结果示例**：

```
key              | value                                    | score
-----------------|------------------------------------------|-------
recurring-jobs   | {"sample-job-daily": {...}}             | 0
```

### 示例：查看任务执行历史

```sql
-- 查看最近执行的任务
SELECT
    j."id",
    j."invocationdata",
    js."name" as state,
    js."createdat" as state_time
FROM hangfire.job j
LEFT JOIN hangfire.jobstate js ON j."stateid" = js."id"
ORDER BY js."createdat" DESC
LIMIT 10;
```

---

## ⚙️ 配置说明

### 当前配置

```csharp
config.UsePostgreSqlStorage(
    configuration.GetConnectionString("Default"),
    new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire",  // 使用独立的 Schema
        PrepareSchemaIfNecessary = true,  // 自动创建表结构
        JobExpirationCheckInterval = TimeSpan.FromHours(1),  // 作业过期检查间隔
        // ...
    });
```

### 配置说明

| 配置项                       | 说明             | 默认值       |
| ---------------------------- | ---------------- | ------------ |
| `SchemaName`                 | Schema 名称      | `"hangfire"` |
| `PrepareSchemaIfNecessary`   | 自动创建表结构   | `true`       |
| `JobExpirationCheckInterval` | 作业过期检查间隔 | `1小时`      |
| `QueuePollInterval`          | 队列轮询间隔     | `15秒`       |

---

## 🔐 数据库权限要求

### 首次运行需要的权限

Hangfire 首次运行时会自动创建 Schema 和表，需要以下权限：

```sql
-- 需要以下权限
GRANT CREATE ON DATABASE PpbStore TO postgres;
GRANT USAGE ON SCHEMA public TO postgres;
GRANT CREATE ON SCHEMA public TO postgres;

-- 如果使用自定义 Schema
CREATE SCHEMA IF NOT EXISTS hangfire;
GRANT ALL ON SCHEMA hangfire TO postgres;
```

---

## 📈 数据量估算

### 定时任务定义

- **存储大小**：每个定时任务约 1-5 KB
- **存储位置**：`hangfire.set` 表
- **数据量**：通常很少（几十到几百个任务）

### 任务执行记录

- **存储大小**：每个任务执行记录约 5-50 KB（取决于参数大小）
- **存储位置**：`hangfire.job`、`hangfire.jobstate`、`hangfire.jobparameter` 表
- **数据量**：取决于任务执行频率和保留时间

**示例**：

- 每天执行 10 个任务
- 保留 7 天
- 每个任务约 10 KB
- 总数据量：10 × 7 × 10 KB = 700 KB

---

## ✅ 总结

### 存储机制

1. **定时任务定义**：存储在 `hangfire.set` 表中

   - ✅ 持久化存储
   - ✅ 应用重启后仍然存在
   - ✅ 可以在 Dashboard 中管理

2. **任务执行记录**：存储在多个表中

   - ✅ 任务基本信息：`hangfire.job`
   - ✅ 任务参数：`hangfire.jobparameter`
   - ✅ 任务状态：`hangfire.jobstate`
   - ✅ 任务队列：`hangfire.jobqueue`

3. **数据持久化**：
   - ✅ 所有数据都存储在 PostgreSQL 数据库中
   - ✅ 使用独立的 Schema（`hangfire`）与业务数据隔离
   - ✅ 自动创建表结构
   - ✅ 支持数据清理和归档

### 优势

- ✅ **持久化**：应用重启后任务仍然存在
- ✅ **可恢复**：可以查看任务执行历史
- ✅ **可管理**：可以在 Dashboard 中管理任务
- ✅ **可扩展**：支持多服务器实例共享任务

---

## 🔍 验证存储

### 1. 运行应用后检查数据库

```sql
-- 连接到数据库
\c PpbStore;

-- 查看 Hangfire Schema 是否存在
SELECT schema_name
FROM information_schema.schemata
WHERE schema_name = 'hangfire';

-- 查看 Hangfire 表是否已创建
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'hangfire'
ORDER BY table_name;
```

### 2. 注册定时任务后检查

```sql
-- 查看定时任务定义
SELECT * FROM hangfire.set WHERE "key" = 'recurring-jobs';
```

### 3. 任务执行后检查

```sql
-- 查看任务执行记录
SELECT COUNT(*) FROM hangfire.job;
SELECT COUNT(*) FROM hangfire.jobstate;
```

---

## 💡 注意事项

1. **数据库连接**：确保数据库连接字符串正确
2. **权限**：确保数据库用户有创建 Schema 和表的权限
3. **备份**：建议定期备份 `hangfire` Schema
4. **清理**：定期清理过期的任务记录，避免数据量过大
5. **监控**：监控 Hangfire 表的数据量，确保不会影响数据库性能

---

## 📚 相关文档

- Hangfire 官方文档：https://docs.hangfire.io/
- PostgreSQL 存储文档：https://docs.hangfire.io/en/latest/configuration/using-postgresql.html
