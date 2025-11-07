using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using PaperBellStore.Data;

namespace PaperBellStore.EntityFrameworkCore
{
    /// <summary>
    /// 专门用于日志数据库的 DbContext
    /// </summary>
    [ConnectionStringName("Logs")]
    public class LogDbContext : AbpDbContext<LogDbContext>
    {
        public DbSet<AppLog> AppLogs { get; set; } = null!;

        public LogDbContext(DbContextOptions<LogDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 配置 AppLog 实体
            builder.Entity<AppLog>(b =>
            {
                b.ToTable("AppLogs");
                b.HasKey(x => x.Id);

                // 基本字段
                b.Property(x => x.Timestamp).IsRequired();
                b.Property(x => x.Level).HasMaxLength(50);
                b.Property(x => x.Message).HasColumnType("TEXT");
                b.Property(x => x.Exception).HasColumnType("TEXT");
                b.Property(x => x.Properties).HasColumnType("JSONB");
                b.Property(x => x.LogEvent).HasColumnType("JSONB");

                // 去重字段（允许为NULL，当去重功能禁用时）
                b.Property(x => x.MessageHash).HasMaxLength(64).IsRequired(false);
                b.Property(x => x.FirstOccurrence).IsRequired(false);
                b.Property(x => x.LastOccurrence).IsRequired(false);
                b.Property(x => x.OccurrenceCount).HasDefaultValue(1);
                b.Property(x => x.DeduplicationWindowMinutes).HasDefaultValue(5);

                // 索引
                b.HasIndex(x => x.Timestamp).HasDatabaseName("IX_AppLogs_Timestamp");
                b.HasIndex(x => x.Level).HasDatabaseName("IX_AppLogs_Level");
                b.HasIndex(x => x.MessageHash).HasDatabaseName("IX_AppLogs_MessageHash");
                b.HasIndex(x => new { x.MessageHash, x.LastOccurrence })
                    .HasDatabaseName("IX_AppLogs_MessageHash_LastOccurrence");
            });
        }
    }
}

