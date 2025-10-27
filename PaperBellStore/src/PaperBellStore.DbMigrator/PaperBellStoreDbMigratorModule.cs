using PaperBellStore.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace PaperBellStore.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(PaperBellStoreEntityFrameworkCoreModule),
    typeof(PaperBellStoreApplicationContractsModule)
)]
public class PaperBellStoreDbMigratorModule : AbpModule
{
}
