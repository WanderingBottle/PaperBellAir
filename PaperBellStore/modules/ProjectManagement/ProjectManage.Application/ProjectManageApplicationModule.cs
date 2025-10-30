using PaperBellStore;

using ProjectManage.Application.Contracts;
using ProjectManage.Domain;

using Volo.Abp.AutoMapper;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;

namespace ProjectManage.Application
{
    [DependsOn(typeof(ProjectManageDomainModule) ,
        typeof(ProjectManageApplicationContractsModule) ,
        typeof(AbpIdentityDomainModule) ,
        typeof(PaperBellStoreApplicationModule))]
    public class ProjectManageApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<ProjectManageApplicationModule>();
            });
        }
    }
}
