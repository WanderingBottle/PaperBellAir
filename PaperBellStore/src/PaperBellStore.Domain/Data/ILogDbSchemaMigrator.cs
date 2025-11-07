using System.Threading.Tasks;

namespace PaperBellStore.Data;

/// <summary>
/// 日志数据库迁移接口
/// </summary>
public interface ILogDbSchemaMigrator
{
    Task MigrateAsync();
}

