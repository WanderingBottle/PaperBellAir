using Volo.Abp.Modularity;

namespace PaperBellStore;

public abstract class PaperBellStoreApplicationTestBase<TStartupModule> : PaperBellStoreTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
