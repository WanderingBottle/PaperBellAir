using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.File;
using PaperBellStore.Blazor.Extensions;

namespace PaperBellStore.Blazor;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting web host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog((context, services, loggerConfiguration) =>
                {
                    // 全局日志级别配置
                    loggerConfiguration
#if DEBUG
                        .MinimumLevel.Debug()
#else
                        .MinimumLevel.Information()
#endif
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                        .Enrich.FromLogContext();

                    // 配置控制台输出（完整输出，不受级别和屏蔽影响）
                    loggerConfiguration.WriteTo.Async(c => c.Console());

                    // 配置文件输出（支持级别控制和内容屏蔽）
                    var fileMinimumLevel = context.Configuration.GetValue<string>("Serilog:File:MinimumLevel", "Verbose");
                    var fileExcludedKeywords = context.Configuration.GetSection("Serilog:File:ExcludedKeywords").Get<List<string>>() ?? new List<string>();
                    var fileExcludedPatterns = context.Configuration.GetSection("Serilog:File:ExcludedPatterns").Get<List<string>>() ?? new List<string>();

                    // 文件大小限制配置（单位：MB，默认 25MB）
                    var fileSizeLimitMB = context.Configuration.GetValue<long>("Serilog:File:FileSizeLimitMB", 25);
                    var fileSizeLimitBytes = fileSizeLimitMB * 1024 * 1024; // 转换为字节

                    // 文件路径格式：Logs/logs-2024-01-15.txt（包含年月日）
                    var filePath = "Logs/logs-.txt";

                    // 配置文件输出（支持级别控制和内容屏蔽）
                    var fileSinkConfiguration = loggerConfiguration.WriteTo.Async(c => c.File(
                        filePath,
                        restrictedToMinimumLevel: Enum.Parse<LogEventLevel>(fileMinimumLevel),
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                        retainedFileCountLimit: 30,
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: fileSizeLimitBytes,
                        rollOnFileSizeLimit: true,
                        shared: false));

                    // 如果配置了内容屏蔽，则添加过滤
                    if (fileExcludedKeywords.Any() || fileExcludedPatterns.Any())
                    {
                        fileSinkConfiguration.Filter.ByExcluding(logEvent =>
                        {
                            var message = logEvent.RenderMessage();
                            var exception = logEvent.Exception?.ToString() ?? "";

                            // 检查关键词屏蔽
                            if (fileExcludedKeywords.Any(keyword =>
                                message.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                exception.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                            {
                                return true; // 排除此日志
                            }

                            // 检查正则表达式屏蔽
                            if (fileExcludedPatterns.Any(pattern =>
                            {
                                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                return regex.IsMatch(message) || regex.IsMatch(exception);
                            }))
                            {
                                return true; // 排除此日志
                            }

                            return false; // 允许写入
                        });
                    }

                    // 配置 ABP Studio 输出
                    loggerConfiguration.WriteTo.Async(c => c.AbpStudio(services));

                    // 添加数据库写入（支持可配置的去重、级别控制和内容屏蔽）
                    // 注意：去重、级别控制和内容屏蔽仅影响数据库日志，不影响控制台和文件日志

                    // 性能优化：支持使用专门的日志数据库连接字符串
                    // ⭐ 默认使用 "Logs" 连接字符串（专门日志数据库），如果不存在则回退到 "Default"（业务数据库）
                    // 推荐使用专门日志数据库，实现性能隔离，确保 EFCore 操作不受影响
                    var logConnectionStringName = context.Configuration.GetValue<string>("Serilog:Database:ConnectionStringName", "Logs");
                    var logConnectionString = context.Configuration.GetConnectionString(logConnectionStringName)
                        ?? context.Configuration.GetConnectionString("Default");

                    if (!string.IsNullOrEmpty(logConnectionString))
                    {
                        // 从配置中读取数据库日志相关配置
                        // ⭐ 默认配置已优化为专门日志数据库方案
                        var enableDeduplication = context.Configuration.GetValue<bool>("Serilog:Database:EnableDeduplication", true);  // 默认启用去重
                        var deduplicationWindowMinutes = context.Configuration.GetValue<int>("Serilog:Database:DeduplicationWindowMinutes", 5);
                        var dbMinimumLevel = context.Configuration.GetValue<string>("Serilog:Database:MinimumLevel", "Information");  // 默认记录 Information 及以上
                        var dbExcludedKeywords = context.Configuration.GetSection("Serilog:Database:ExcludedKeywords").Get<List<string>>() ?? new List<string>();
                        var dbExcludedPatterns = context.Configuration.GetSection("Serilog:Database:ExcludedPatterns").Get<List<string>>() ?? new List<string>();
                        var cacheExpirationMinutes = context.Configuration.GetValue<int>("Serilog:Database:CacheExpirationMinutes", 10);

                        loggerConfiguration.WriteTo.Async(c =>
                            c.WriteToPostgreSQLWithDeduplication(
                                logConnectionString,  // 使用配置的日志数据库连接字符串（可以是专门的日志数据库）
                                "AppLogs",
                                enableDeduplication: enableDeduplication,  // 是否启用去重（仅影响数据库）
                                deduplicationWindowMinutes: deduplicationWindowMinutes,  // 去重时间窗口（分钟）
                                minimumLevel: Enum.Parse<LogEventLevel>(dbMinimumLevel),  // 数据库日志最小级别
                                excludedKeywords: dbExcludedKeywords,  // 数据库日志屏蔽关键词
                                excludedPatterns: dbExcludedPatterns,  // 数据库日志屏蔽正则表达式
                                batchPostingLimit: 100,
                                cacheExpirationMinutes: cacheExpirationMinutes));  // 缓存过期时间（分钟）
                    }

                    // 注意：
                    // 1. 控制台日志始终完整输出，不受级别控制和内容屏蔽影响
                    // 2. 文件日志支持级别控制和内容屏蔽
                    // 3. 数据库日志支持级别控制、内容屏蔽和去重功能
                });
            await builder.AddApplicationAsync<PaperBellStoreBlazorModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
