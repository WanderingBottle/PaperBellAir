using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Hangfire;
using Hangfire.Storage;
using Volo.Abp.DependencyInjection;

namespace PaperBellStore.Blazor.Services
{
    /// <summary>
    /// 周期性任务状态管理服务
    /// 用于保存和恢复周期性任务的配置信息，支持暂停/恢复功能
    /// </summary>
    public class RecurringJobStateService : ITransientDependency
    {
        private const string StateSetKey = "recurring-job-states";
        private const string PausedJobSetKey = "recurring-job-paused";

        /// <summary>
        /// 保存任务配置信息
        /// </summary>
        /// <param name="jobId">任务ID</param>
        /// <param name="config">任务配置信息</param>
        public void SaveJobConfig(string jobId, RecurringJobConfig config)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                throw new ArgumentException("JobId 不能为空", nameof(jobId));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            using var connection = JobStorage.Current.GetConnection();
            var key = GetStateKey(jobId);

            // 序列化配置信息
            var json = JsonSerializer.Serialize(config);

            // 使用 Hangfire 的 Set 功能存储（持久化到数据库）
            connection.SetRangeInHash(key, new Dictionary<string, string>
            {
                { "JobId", config.JobId },
                { "JobType", config.JobType },
                { "CronExpression", config.CronExpression },
                { "TimeZoneId", config.TimeZoneId ?? "Local" },
                { "Queue", config.Queue ?? "default" },
                { "ConfigJson", json },  // 保存完整配置的 JSON（用于恢复）
                { "PausedAt", DateTime.UtcNow.ToString("O") },
                { "PausedBy", config.PausedBy ?? "System" }
            });

            using var transaction = connection.CreateWriteTransaction();
            transaction.AddToSet(PausedJobSetKey, jobId);
            transaction.Commit();
        }

        /// <summary>
        /// 获取任务配置信息
        /// </summary>
        /// <param name="jobId">任务ID</param>
        /// <returns>任务配置信息，如果不存在则返回 null</returns>
        public RecurringJobConfig? GetJobConfig(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return null;
            }

            using var connection = JobStorage.Current.GetConnection();
            var key = GetStateKey(jobId);
            var hash = connection.GetAllEntriesFromHash(key);

            if (hash == null || !hash.ContainsKey("ConfigJson"))
            {
                return null;
            }

            try
            {
                // 从 JSON 反序列化配置信息
                var json = hash["ConfigJson"];
                return JsonSerializer.Deserialize<RecurringJobConfig>(json);
            }
            catch
            {
                // 如果反序列化失败，尝试从 hash 中重建配置
                return new RecurringJobConfig
                {
                    JobId = hash.GetValueOrDefault("JobId", jobId),
                    JobType = hash.GetValueOrDefault("JobType", ""),
                    CronExpression = hash.GetValueOrDefault("CronExpression", ""),
                    TimeZoneId = hash.GetValueOrDefault("TimeZoneId", "Local"),
                    Queue = hash.GetValueOrDefault("Queue", "default"),
                    PausedBy = hash.GetValueOrDefault("PausedBy", "System")
                };
            }
        }

        /// <summary>
        /// 删除任务配置信息
        /// </summary>
        /// <param name="jobId">任务ID</param>
        public void RemoveJobConfig(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return;
            }

            using var connection = JobStorage.Current.GetConnection();
            var key = GetStateKey(jobId);

            using var transaction = connection.CreateWriteTransaction();
            transaction.RemoveHash(key);
            transaction.RemoveFromSet(PausedJobSetKey, jobId);
            transaction.Commit();
        }

        /// <summary>
        /// 检查任务是否已暂停
        /// </summary>
        /// <param name="jobId">任务ID</param>
        /// <returns>如果任务已暂停返回 true，否则返回 false</returns>
        public bool IsPaused(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return false;
            }

            using var connection = JobStorage.Current.GetConnection();
            var key = GetStateKey(jobId);
            var hash = connection.GetAllEntriesFromHash(key);

            return hash != null && hash.ContainsKey("ConfigJson");
        }

        /// <summary>
        /// 获取所有已暂停的任务ID列表
        /// </summary>
        /// <returns>已暂停的任务ID列表</returns>
        public List<string> GetPausedJobIds()
        {
            using var connection = JobStorage.Current.GetConnection();
            var pausedJobs = connection.GetAllItemsFromSet(PausedJobSetKey)?.ToList() ?? new List<string>();

            // 回退：扫描当前任务，确保集合里存在的任务未丢失
            var allRecurringJobs = connection.GetRecurringJobs();
            foreach (var job in allRecurringJobs)
            {
                if (IsPaused(job.Id) && !pausedJobs.Contains(job.Id))
                {
                    pausedJobs.Add(job.Id);
                }
            }

            return pausedJobs;
        }

        /// <summary>
        /// 获取状态存储键
        /// </summary>
        private string GetStateKey(string jobId)
        {
            return $"{StateSetKey}:{jobId}";
        }
    }

    /// <summary>
    /// 周期性任务配置信息
    /// </summary>
    public class RecurringJobConfig
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// 任务类型（完整类型名，如 PaperBellStore.Blazor.RecurringJobs.LogCleanupRecurringJob）
        /// </summary>
        public string JobType { get; set; } = string.Empty;

        /// <summary>
        /// Cron 表达式
        /// </summary>
        public string CronExpression { get; set; } = string.Empty;

        /// <summary>
        /// 时区ID（如 "Local", "UTC", "Asia/Shanghai"）
        /// </summary>
        public string? TimeZoneId { get; set; }

        /// <summary>
        /// 队列名称
        /// </summary>
        public string? Queue { get; set; }

        /// <summary>
        /// 暂停操作的用户
        /// </summary>
        public string? PausedBy { get; set; }

        /// <summary>
        /// 暂停时间
        /// </summary>
        public DateTime? PausedAt { get; set; }

        /// <summary>
        /// 其他配置选项（JSON 格式）
        /// </summary>
        public string? AdditionalOptions { get; set; }
    }
}

