using Volo.Abp.Modularity;

namespace PaperBellStore;

[DependsOn(
    typeof(PaperBellStoreApplicationModule),
    typeof(PaperBellStoreDomainTestModule)
)]
public class PaperBellStoreApplicationTestModule : AbpModule
{

}
