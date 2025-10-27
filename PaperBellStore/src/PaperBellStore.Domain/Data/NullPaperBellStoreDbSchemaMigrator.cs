using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PaperBellStore.Data;

/* This is used if database provider does't define
 * IPaperBellStoreDbSchemaMigrator implementation.
 */
public class NullPaperBellStoreDbSchemaMigrator : IPaperBellStoreDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
