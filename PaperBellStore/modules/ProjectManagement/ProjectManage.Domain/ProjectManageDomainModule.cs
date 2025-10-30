using Volo.Abp.Modularity;

namespace ProjectManage.Domain
{
    [DependsOn(
        typeof(ProjectManageDomainSharedModule))]
    public class ProjectManageDomainModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}
