using PaperBellStore.Samples;
using Xunit;

namespace PaperBellStore.EntityFrameworkCore.Applications;

[Collection(PaperBellStoreTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<PaperBellStoreEntityFrameworkCoreTestModule>
{

}
