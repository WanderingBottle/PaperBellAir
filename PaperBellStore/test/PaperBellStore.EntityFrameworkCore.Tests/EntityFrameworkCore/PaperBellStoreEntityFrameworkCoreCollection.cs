using Xunit;

namespace PaperBellStore.EntityFrameworkCore;

[CollectionDefinition(PaperBellStoreTestConsts.CollectionDefinitionName)]
public class PaperBellStoreEntityFrameworkCoreCollection : ICollectionFixture<PaperBellStoreEntityFrameworkCoreFixture>
{

}
