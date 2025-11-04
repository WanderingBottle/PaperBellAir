using Microsoft.Extensions.DependencyInjection;

using PaperBellStore.EntityFrameworkCore;

using ProjectManage.Application;
using ProjectManage.Domain;
using ProjectManage.Projects;
using ProjectManage.Owners;

using Volo.Abp.Modularity;

namespace ProjectManage.EntityFrameworkCore
{
    [DependsOn(typeof(ProjectManageDomainModule),
        typeof(ProjectManageApplicationModule),
        typeof(PaperBellStoreEntityFrameworkCoreModule))]
    public class ProjectManageEntityFrameworkCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAbpDbContext<PaperBellStoreDbContext>(options =>
            {
                // 添加项目管理的仓储
                options.AddRepository<PbpProject, ProjectRepository>();
                // 添加负责人的仓储
                options.AddRepository<PbpOwner, OwnerRepository>();
            });
        }
    }
}
