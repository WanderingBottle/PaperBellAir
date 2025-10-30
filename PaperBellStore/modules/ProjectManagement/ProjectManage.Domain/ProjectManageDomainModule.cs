using PaperBellStore;

using Volo.Abp.Modularity;

namespace ProjectManage.Domain
{
    [DependsOn(
        typeof(ProjectManageDomainSharedModule) ,
        typeof(PaperBellStoreDomainModule))]
    public class ProjectManageDomainModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}
