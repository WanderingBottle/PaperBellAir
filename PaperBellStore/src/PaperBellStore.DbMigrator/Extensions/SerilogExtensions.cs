using System.Collections.Generic;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using PaperBellStore.DbMigrator.Sinks;

namespace PaperBellStore.DbMigrator.Extensions
{
    public static class SerilogExtensions
    {
        /// <summary>
        /// 配置 PostgreSQL Sink（支持可配置的去重、级别控制和内容屏蔽功能）
        /// </summary>
        /// <param name="sinkConfiguration">Sink配置</param>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="tableName">表名</param>
        /// <param name="enableDeduplication">是否启用去重功能，默认 false</param>
        /// <param name="deduplicationWindowMinutes">去重时间窗口（分钟），仅在启用去重时有效，默认 5 分钟</param>
        /// <param name="minimumLevel">最小日志级别，默认 Verbose（记录所有级别）</param>
        /// <param name="excludedKeywords">需要屏蔽的关键词列表</param>
        /// <param name="excludedPatterns">需要屏蔽的正则表达式列表</param>
        /// <param name="batchPostingLimit">批量写入数量限制，默认 100</param>
        /// <param name="cacheExpirationMinutes">缓存过期时间（分钟），默认 10 分钟</param>
        /// <returns></returns>
        public static LoggerConfiguration WriteToPostgreSQLWithDeduplication(
            this LoggerSinkConfiguration sinkConfiguration,
            string connectionString,
            string tableName = "AppLogs",
            bool enableDeduplication = false,
            int deduplicationWindowMinutes = 5,
            LogEventLevel minimumLevel = LogEventLevel.Verbose,
            IEnumerable<string>? excludedKeywords = null,
            IEnumerable<string>? excludedPatterns = null,
            int batchPostingLimit = 100,
            int cacheExpirationMinutes = 10)
        {
            return sinkConfiguration.Sink(
                new DeduplicatingPostgreSQLSink(
                    connectionString,
                    tableName,
                    enableDeduplication,
                    deduplicationWindowMinutes,
                    minimumLevel,
                    excludedKeywords,
                    excludedPatterns,
                    batchPostingLimit,
                    null,
                    cacheExpirationMinutes));
        }
    }
}
