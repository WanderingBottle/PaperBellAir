using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using PaperBellStore.Data;
using Volo.Abp.DependencyInjection;

namespace PaperBellStore.EntityFrameworkCore;

/// <summary>
/// EF Core 日志数据库迁移实现
/// </summary>
public class EntityFrameworkCoreLogDbSchemaMigrator
    : ILogDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EntityFrameworkCoreLogDbSchemaMigrator> _logger;

    public EntityFrameworkCoreLogDbSchemaMigrator(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<EntityFrameworkCoreLogDbSchemaMigrator> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the LogDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string from configuration.
         */

        // 自动创建数据库（如果不存在）
        await EnsureDatabaseCreatedAsync();

        try
        {
            // 执行迁移
            var dbContext = _serviceProvider.GetRequiredService<LogDbContext>();
            _logger.LogInformation("Starting EF Core migrations for log database...");

            await dbContext.Database.MigrateAsync();

            // 验证表是否创建成功
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (canConnect)
            {
                // 检查 AppLogs 表是否存在
                var connection = dbContext.Database.GetDbConnection();
                await connection.OpenAsync();
                try
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT COUNT(*) FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = 'AppLogs'";
                    var tableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

                    _logger.LogInformation("EF Core migrations completed successfully for log database.");
                    _logger.LogInformation("AppLogs table verification: Table exists = {TableExists}", tableExists);

                    if (!tableExists)
                    {
                        _logger.LogWarning("AppLogs table was not created. Migration may have failed silently.");
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            else
            {
                _logger.LogWarning("Cannot connect to log database after migration.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute EF Core migrations for log database.");
            throw; // 重新抛出异常，让调用者知道迁移失败
        }
    }

    /// <summary>
    /// 确保日志数据库存在，如果不存在则自动创建
    /// </summary>
    private async Task EnsureDatabaseCreatedAsync()
    {
        var connectionString = _configuration.GetConnectionString("Logs");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("Logs connection string is not configured. Skipping database creation.");
            return;
        }

        try
        {
            // 解析连接字符串以获取数据库名称
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var databaseName = builder.Database;

            if (string.IsNullOrEmpty(databaseName))
            {
                _logger.LogWarning("Database name is not specified in connection string. Skipping database creation.");
                return;
            }

            // 创建不包含数据库名称的连接字符串（连接到 PostgreSQL 服务器）
            builder.Database = "postgres"; // 连接到默认的 postgres 数据库
            var serverConnectionString = builder.ConnectionString;

            // 检查数据库是否存在，如果不存在则创建
            await using var connection = new NpgsqlConnection(serverConnectionString);
            await connection.OpenAsync();

            // 检查数据库是否存在（使用参数化查询避免 SQL 注入）
            var checkDbExistsCommand = @"
                SELECT 1 FROM pg_database WHERE datname = @databaseName";

            await using var checkCommand = new NpgsqlCommand(checkDbExistsCommand, connection);
            checkCommand.Parameters.AddWithValue("databaseName", databaseName);
            var exists = await checkCommand.ExecuteScalarAsync() != null;

            if (!exists)
            {
                _logger.LogInformation($"Database '{databaseName}' does not exist. Creating...");

                // 创建数据库
                // 注意：CREATE DATABASE 不能在事务中执行，且数据库名称不能参数化
                // 但我们已经从配置中获取了数据库名称，相对安全
                var escapedDatabaseName = databaseName.Replace("\"", "\"\""); // 转义双引号
                var createDbCommand = $@"
                    CREATE DATABASE ""{escapedDatabaseName}""";

                await using var createCommand = new NpgsqlCommand(createDbCommand, connection);
                await createCommand.ExecuteNonQueryAsync();

                _logger.LogInformation($"Database '{databaseName}' created successfully.");
            }
            else
            {
                _logger.LogInformation($"Database '{databaseName}' already exists.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to create database automatically. You may need to create it manually: {ex.Message}");
            // 不抛出异常，让 MigrateAsync 尝试连接，如果数据库不存在会抛出更明确的错误
        }
    }
}

