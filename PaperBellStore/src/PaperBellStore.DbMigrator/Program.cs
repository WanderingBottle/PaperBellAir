using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using PaperBellStore.DbMigrator.Extensions;

namespace PaperBellStore.DbMigrator;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // ⭐ 使用专门日志数据库连接字符串（推荐）
        var logConnectionStringName = configuration.GetValue<string>("Serilog:Database:ConnectionStringName", "Logs");
        var logConnectionString = configuration.GetConnectionString(logConnectionStringName)
            ?? configuration.GetConnectionString("Default");

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Volo.Abp", LogEventLevel.Warning)
#if DEBUG
                .MinimumLevel.Override("PaperBellStore", LogEventLevel.Debug)
#else
                .MinimumLevel.Override("PaperBellStore", LogEventLevel.Information)
#endif
                .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console());

        // 添加数据库写入（使用专门日志数据库）
        if (!string.IsNullOrEmpty(logConnectionString))
        {
            var enableDeduplication = configuration.GetValue<bool>("Serilog:Database:EnableDeduplication", true);
            var deduplicationWindowMinutes = configuration.GetValue<int>("Serilog:Database:DeduplicationWindowMinutes", 5);
            var dbMinimumLevel = configuration.GetValue<string>("Serilog:Database:MinimumLevel", "Information");
            var cacheExpirationMinutes = configuration.GetValue<int>("Serilog:Database:CacheExpirationMinutes", 10);

            loggerConfiguration.WriteTo.Async(c =>
                c.WriteToPostgreSQLWithDeduplication(
                    logConnectionString,  // 使用专门日志数据库
                    "AppLogs",
                    enableDeduplication: enableDeduplication,
                    deduplicationWindowMinutes: deduplicationWindowMinutes,
                    minimumLevel: Enum.Parse<LogEventLevel>(dbMinimumLevel),
                    cacheExpirationMinutes: cacheExpirationMinutes));
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        await CreateHostBuilder(args).RunConsoleAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .AddAppSettingsSecretsJson()
            .ConfigureLogging((context, logging) => logging.ClearProviders())
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<DbMigratorHostedService>();
            });
}
