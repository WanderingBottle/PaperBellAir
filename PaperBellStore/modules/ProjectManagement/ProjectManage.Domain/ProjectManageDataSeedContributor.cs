using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace ProjectManage.Domain
{
    /// <summary>
    /// 为内置管理员角色授予项目管理相关权限的数据种子。
    /// </summary>
    public class ProjectManageDataSeedContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly IPermissionDataSeeder _permissionDataSeeder;

        public ProjectManageDataSeedContributor(IPermissionDataSeeder permissionDataSeeder)
        {
            _permissionDataSeeder = permissionDataSeeder;
        }

        [UnitOfWork]
        public async Task SeedAsync(DataSeedContext context)
        {
            // 默认管理员角色名为 "admin"
            var permissions = new[]
            {
                "ProjectManagement", // Default权限，必须授予
                "ProjectManagement.View",
                "ProjectManagement.Create",
                "ProjectManagement.Edit",
                "ProjectManagement.Delete"
            };

            // 角色权限提供者名称（RolePermissionValueProvider.ProviderName），此处直接使用常量值 "R"
            await _permissionDataSeeder.SeedAsync(
                "R",
                "admin",
                permissions,
                context.TenantId
            );
        }
    }
}


