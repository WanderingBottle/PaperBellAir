using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using PaperBellStore.Blazor.RecurringJobs;
using Volo.Abp.DependencyInjection;

namespace PaperBellStore.Blazor.Services
{
    /// <summary>
    /// 周期性任务恢复服务
    /// 根据任务ID和配置信息恢复周期性任务
    /// </summary>
    public class RecurringJobRecoveryService : ITransientDependency
    {
        /// <summary>
        /// 任务注册表：JobId -> 任务恢复方法
        /// </summary>
        private static readonly Dictionary<string, Func<RecurringJobConfig, bool>> JobRecoveryMap = new()
        {
            {
                "log-cleanup-daily",
                config => RecoverLogCleanupJob(config)
            },
            {
                "audit-log-cleanup-daily",
                config => RecoverAuditLogCleanupJob(config)
            }
        };

        /// <summary>
        /// 恢复任务
        /// </summary>
        /// <param name="config">任务配置信息</param>
        /// <returns>恢复成功返回 true，否则返回 false</returns>
        public bool RecoverJob(RecurringJobConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.JobId))
            {
                return false;
            }

            // 从注册表中查找恢复方法
            if (JobRecoveryMap.TryGetValue(config.JobId, out var recoveryFunc))
            {
                return recoveryFunc(config);
            }

            // 如果注册表中没有，尝试根据任务类型恢复
            return RecoverJobByType(config);
        }

        /// <summary>
        /// 根据任务类型恢复任务
        /// </summary>
        private bool RecoverJobByType(RecurringJobConfig config)
        {
            if (string.IsNullOrEmpty(config.JobType))
            {
                return false;
            }

            // 根据任务类型名称恢复
            var jobTypeName = config.JobType;

            if (jobTypeName.Contains(nameof(LogCleanupRecurringJob)))
            {
                return RecoverLogCleanupJob(config);
            }

            if (jobTypeName.Contains(nameof(AuditLogCleanupRecurringJob)))
            {
                return RecoverAuditLogCleanupJob(config);
            }

            return false;
        }

        /// <summary>
        /// 恢复日志清理任务
        /// </summary>
        private static bool RecoverLogCleanupJob(RecurringJobConfig config)
        {
            try
            {
                var timeZone = string.IsNullOrEmpty(config.TimeZoneId)
                    ? TimeZoneInfo.Local
                    : TimeZoneInfo.FindSystemTimeZoneById(config.TimeZoneId);

                var options = new RecurringJobOptions
                {
                    TimeZone = timeZone
                };

                // 注意：RecurringJobOptions 在某些版本中可能不支持 Queue 属性
                // 如果需要指定队列，可以在方法调用时使用 [Queue] 特性或通过其他方式
                RecurringJob.AddOrUpdate<LogCleanupRecurringJob>(
                    config.JobId,
                    job => job.ExecuteAsync(),
                    config.CronExpression,
                    options);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 恢复审计日志清理任务
        /// </summary>
        private static bool RecoverAuditLogCleanupJob(RecurringJobConfig config)
        {
            try
            {
                var timeZone = string.IsNullOrEmpty(config.TimeZoneId)
                    ? TimeZoneInfo.Local
                    : TimeZoneInfo.FindSystemTimeZoneById(config.TimeZoneId);

                var options = new RecurringJobOptions
                {
                    TimeZone = timeZone
                };

                // 注意：RecurringJobOptions 在某些版本中可能不支持 Queue 属性
                RecurringJob.AddOrUpdate<AuditLogCleanupRecurringJob>(
                    config.JobId,
                    job => job.ExecuteAsync(),
                    config.CronExpression,
                    options);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取任务配置信息（从当前注册的任务中）
        /// </summary>
        /// <param name="jobId">任务ID</param>
        /// <returns>任务配置信息</returns>
        public RecurringJobConfig? GetCurrentJobConfig(string jobId)
        {
            // 使用 IStorageConnection 获取周期性任务列表
            using var connection = JobStorage.Current.GetConnection();
            var recurringJobs = connection.GetRecurringJobs();
            var job = recurringJobs.FirstOrDefault(j => j.Id == jobId);

            if (job == null)
            {
                return null;
            }

            // 根据 JobId 确定任务类型
            string jobType;
            if (jobId == "log-cleanup-daily")
            {
                jobType = typeof(LogCleanupRecurringJob).FullName ?? nameof(LogCleanupRecurringJob);
            }
            else if (jobId == "audit-log-cleanup-daily")
            {
                jobType = typeof(AuditLogCleanupRecurringJob).FullName ?? nameof(AuditLogCleanupRecurringJob);
            }
            else
            {
                jobType = "Unknown";
            }

            return new RecurringJobConfig
            {
                JobId = job.Id,
                JobType = jobType,
                CronExpression = job.Cron ?? "",
                TimeZoneId = job.TimeZoneId,
                Queue = "default"  // Hangfire 的 RecurringJobDto 不直接提供队列信息
            };
        }
    }
}

