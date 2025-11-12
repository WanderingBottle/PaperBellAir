using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaperBellStore.EntityFrameworkCore;
using Volo.Abp.AuditLogging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace PaperBellStore.Blazor.RecurringJobs
{
    /// <summary>
    /// 审计日志清理定时任务
    /// 定期删除超过保留期的审计日志数据
    /// </summary>
    public class AuditLogCleanupRecurringJob : ITransientDependency
    {
        private readonly ILogger<AuditLogCleanupRecurringJob> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDbContextProvider<PaperBellStoreDbContext> _dbContextProvider;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public AuditLogCleanupRecurringJob(
            ILogger<AuditLogCleanupRecurringJob> logger,
            IConfiguration configuration,
            IDbContextProvider<PaperBellStoreDbContext> dbContextProvider,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _logger = logger;
            _configuration = configuration;
            _dbContextProvider = dbContextProvider;
            _unitOfWorkManager = unitOfWorkManager;
        }

        /// <summary>
        /// 执行审计日志清理任务
        /// </summary>
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("开始执行审计日志清理任务：{Time}", DateTime.Now);

            try
            {
                var cleanupConfig = _configuration.GetSection("Auditing:Cleanup");
                var enabled = cleanupConfig.GetValue<bool?>("Enabled") ?? false;

                if (!enabled)
                {
                    _logger.LogInformation("审计日志清理功能已禁用，跳过清理任务");
                    return;
                }

                var retentionDays = Math.Max(cleanupConfig.GetValue<int?>("RetentionDays") ?? 180, 1);
                var batchSize = Math.Clamp(cleanupConfig.GetValue<int?>("BatchSize") ?? 500, 100, 5000);
                var maxBatches = Math.Clamp(cleanupConfig.GetValue<int?>("MaxBatchesPerRun") ?? 10, 1, 100);
                var cutoffDateUtc = DateTime.UtcNow.AddDays(-retentionDays);

                var stats = new CleanupStats
                {
                    StartTime = DateTime.UtcNow,
                    CutoffDate = cutoffDateUtc,
                    RetentionDays = retentionDays
                };

                using var uow = _unitOfWorkManager.Begin(requiresNew: true);
                var dbContext = await _dbContextProvider.GetDbContextAsync();

                for (var batchIndex = 0; batchIndex < maxBatches; batchIndex++)
                {
                    var auditLogIds = await dbContext.Set<AuditLog>()
                        .Where(x => x.ExecutionTime < cutoffDateUtc)
                        .OrderBy(x => x.ExecutionTime)
                        .Select(x => x.Id)
                        .Take(batchSize)
                        .ToListAsync();

                    if (auditLogIds.Count == 0)
                    {
                        break;
                    }

                    var deletedCount = await dbContext.Set<AuditLog>()
                        .Where(x => auditLogIds.Contains(x.Id))
                        .ExecuteDeleteAsync();

                    if (deletedCount == 0)
                    {
                        break;
                    }

                    stats.TotalDeleted += deletedCount;
                    stats.BatchesExecuted++;
                }

                await uow.CompleteAsync();

                stats.EndTime = DateTime.UtcNow;
                stats.Duration = stats.EndTime - stats.StartTime;

                if (stats.TotalDeleted > 0)
                {
                    _logger.LogInformation(
                        "审计日志清理任务完成。删除记录数：{DeletedCount}，批次数：{Batches}，保留天数：{RetentionDays}，耗时：{Duration}秒，截止时间（UTC）：{Cutoff}",
                        stats.TotalDeleted,
                        stats.BatchesExecuted,
                        stats.RetentionDays,
                        stats.Duration.TotalSeconds.ToString("F2"),
                        stats.CutoffDate);
                }
                else
                {
                    _logger.LogInformation(
                        "审计日志清理任务完成。本次没有符合条件的记录。保留天数：{RetentionDays}，截止时间（UTC）：{Cutoff}",
                        stats.RetentionDays,
                        stats.CutoffDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "审计日志清理任务执行失败：{Message}", ex.Message);
                throw;
            }
        }

        private class CleanupStats
        {
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public TimeSpan Duration { get; set; }
            public int TotalDeleted { get; set; }
            public int BatchesExecuted { get; set; }
            public int RetentionDays { get; set; }
            public DateTime CutoffDate { get; set; }
        }
    }
}

