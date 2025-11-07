using System;
using Volo.Abp.Domain.Entities;

namespace PaperBellStore.Data
{
    public class AppLog : Entity<Guid>
    {
        /// <summary>
        /// 日志时间戳（当前记录的时间）
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public string? Level { get; set; }

        /// <summary>
        /// 日志消息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// 属性（JSON格式）
        /// </summary>
        public string? Properties { get; set; }

        /// <summary>
        /// 日志事件（JSON格式）
        /// </summary>
        public string? LogEvent { get; set; }

        // ========== 去重相关字段 ==========

        /// <summary>
        /// 消息哈希（用于判断重复日志）
        /// 基于 Level + Message + Exception 计算
        /// </summary>
        public string? MessageHash { get; set; }

        /// <summary>
        /// 首次出现时间（该重复日志第一次出现的时间）
        /// </summary>
        public DateTime? FirstOccurrence { get; set; }

        /// <summary>
        /// 最后一次出现时间（该重复日志最后一次出现的时间）
        /// </summary>
        public DateTime? LastOccurrence { get; set; }

        /// <summary>
        /// 出现次数（该重复日志出现的总次数）
        /// </summary>
        public int OccurrenceCount { get; set; } = 1;

        /// <summary>
        /// 去重时间窗口（分钟），超过此时间窗口后相同日志视为新记录
        /// </summary>
        public int DeduplicationWindowMinutes { get; set; } = 5;
    }
}

