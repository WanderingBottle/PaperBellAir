using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaperBellStore.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace PaperBellStore.Blazor.RecurringJobs
{
    /// <summary>
    /// 日志清理定时任务
    /// 定期清理过期的数据库日志记录
    /// </summary>
    public class LogCleanupRecurringJob : ITransientDependency
    {
        private readonly ILogger<LogCleanupRecurringJob> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDbContextProvider<LogDbContext> _logDbContextProvider;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public LogCleanupRecurringJob(
            ILogger<LogCleanupRecurringJob> logger,
            IConfiguration configuration,
            IDbContextProvider<LogDbContext> logDbContextProvider,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _logger = logger;
            _configuration = configuration;
            _logDbContextProvider = logDbContextProvider;
            _unitOfWorkManager = unitOfWorkManager;
        }

        /// <summary>
        /// 执行日志清理任务
        /// </summary>
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("开始执行日志清理任务：{Time}", DateTime.Now);

            try
            {
                // 从配置中读取清理策略
                var cleanupConfig = _configuration.GetSection("Serilog:Database:Cleanup");
                var defaultRetentionDays = cleanupConfig.GetValue<int>("DefaultRetentionDays", 30);
                var enabled = cleanupConfig.GetValue<bool>("Enabled", true);

                if (!enabled)
                {
                    _logger.LogInformation("日志清理功能已禁用，跳过清理任务");
                    return;
                }

                // 读取不同日志级别的保留天数配置
                var errorRetentionDays = cleanupConfig.GetValue<int>("ErrorRetentionDays", 90);  // Error级别保留90天
                var warningRetentionDays = cleanupConfig.GetValue<int>("WarningRetentionDays", 60);  // Warning级别保留60天
                var informationRetentionDays = cleanupConfig.GetValue<int>("InformationRetentionDays", 30);  // Information级别保留30天
                var debugRetentionDays = cleanupConfig.GetValue<int>("DebugRetentionDays", 7);  // Debug级别保留7天

                using var uow = _unitOfWorkManager.Begin(requiresNew: true);
                var dbContext = await _logDbContextProvider.GetDbContextAsync();

                var cleanupStats = new CleanupStats
                {
                    StartTime = DateTime.UtcNow
                };

                // 清理不同级别的日志
                await CleanupLogsByLevel(dbContext, "Error", errorRetentionDays, cleanupStats);
                await CleanupLogsByLevel(dbContext, "Warning", warningRetentionDays, cleanupStats);
                await CleanupLogsByLevel(dbContext, "Information", informationRetentionDays, cleanupStats);
                await CleanupLogsByLevel(dbContext, "Debug", debugRetentionDays, cleanupStats);
                await CleanupLogsByLevel(dbContext, "Verbose", debugRetentionDays, cleanupStats);  // Verbose和Debug使用相同策略
                
                // 清理其他级别或级别为空的日志（使用默认保留天数）
                await CleanupLogsByLevel(dbContext, null, defaultRetentionDays, cleanupStats);

                await uow.CompleteAsync();

                cleanupStats.EndTime = DateTime.UtcNow;
                cleanupStats.Duration = cleanupStats.EndTime - cleanupStats.StartTime;

                _logger.LogInformation(
                    "日志清理任务完成。删除记录数：{DeletedCount}，耗时：{Duration}秒",
                    cleanupStats.TotalDeleted,
                    cleanupStats.Duration.TotalSeconds.ToString("F2"));

                // 记录详细统计信息
                if (cleanupStats.DeletedByLevel.Any())
                {
                    foreach (var stat in cleanupStats.DeletedByLevel)
                    {
                        _logger.LogInformation(
                            "级别 {Level}：删除 {Count} 条记录（保留天数：{RetentionDays}）",
                            stat.Level ?? "其他",
                            stat.Count,
                            stat.RetentionDays);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日志清理任务执行失败：{Message}", ex.Message);
                throw;  // 重新抛出异常，Hangfire 会自动重试
            }
        }

        /// <summary>
        /// 按日志级别清理日志
        /// </summary>
        private async Task CleanupLogsByLevel(
            LogDbContext dbContext,
            string? level,
            int retentionDays,
            CleanupStats stats)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var query = dbContext.AppLogs.AsQueryable();

                // 根据级别过滤
                if (string.IsNullOrWhiteSpace(level))
                {
                    // 清理级别为空或不在常见级别列表中的日志
                    var commonLevels = new[] { "Error", "Warning", "Information", "Debug", "Verbose", "Fatal" };
                    query = query.Where(x => x.Level == null || !commonLevels.Contains(x.Level));
                }
                else
                {
                    query = query.Where(x => x.Level == level);
                }

                // 根据时间戳过滤（使用LastOccurrence或Timestamp）
                // 优先使用LastOccurrence（如果存在），否则使用Timestamp
                var logsToDelete = await query
                    .Where(x => (x.LastOccurrence ?? x.Timestamp) < cutoffDate)
                    .ToListAsync();

                if (logsToDelete.Any())
                {
                    dbContext.AppLogs.RemoveRange(logsToDelete);
                    await dbContext.SaveChangesAsync();

                    var deletedCount = logsToDelete.Count;
                    stats.TotalDeleted += deletedCount;
                    stats.DeletedByLevel.Add(new LevelCleanupStat
                    {
                        Level = level,
                        Count = deletedCount,
                        RetentionDays = retentionDays
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理级别 {Level} 的日志时发生错误：{Message}", level ?? "其他", ex.Message);
                // 不抛出异常，继续清理其他级别
            }
        }

        /// <summary>
        /// 清理统计信息
        /// </summary>
        private class CleanupStats
        {
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public TimeSpan Duration { get; set; }
            public int TotalDeleted { get; set; }
            public List<LevelCleanupStat> DeletedByLevel { get; set; } = new List<LevelCleanupStat>();
        }

        /// <summary>
        /// 按级别清理统计
        /// </summary>
        private class LevelCleanupStat
        {
            public string? Level { get; set; }
            public int Count { get; set; }
            public int RetentionDays { get; set; }
        }
    }
}

