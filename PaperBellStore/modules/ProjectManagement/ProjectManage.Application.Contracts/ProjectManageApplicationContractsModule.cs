using Volo.Abp.Modularity;

namespace ProjectManage.Application.Contracts
{
    [DependsOn(typeof(ProjectManageDomainSharedModule))]
    public class ProjectManageApplicationContractsModule : AbpModule
    {

    }
}
