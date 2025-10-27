using Volo.Abp.Modularity;

namespace PaperBellStore;

[DependsOn(
    typeof(PaperBellStoreDomainModule),
    typeof(PaperBellStoreTestBaseModule)
)]
public class PaperBellStoreDomainTestModule : AbpModule
{

}
