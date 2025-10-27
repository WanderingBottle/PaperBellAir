using PaperBellStore.Samples;
using Xunit;

namespace PaperBellStore.EntityFrameworkCore.Domains;

[Collection(PaperBellStoreTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<PaperBellStoreEntityFrameworkCoreTestModule>
{

}
