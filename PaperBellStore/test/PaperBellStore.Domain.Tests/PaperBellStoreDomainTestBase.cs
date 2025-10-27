using Volo.Abp.Modularity;

namespace PaperBellStore;

/* Inherit from this class for your domain layer tests. */
public abstract class PaperBellStoreDomainTestBase<TStartupModule> : PaperBellStoreTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
