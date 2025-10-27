using System.Threading.Tasks;

namespace PaperBellStore.Data;

public interface IPaperBellStoreDbSchemaMigrator
{
    Task MigrateAsync();
}
