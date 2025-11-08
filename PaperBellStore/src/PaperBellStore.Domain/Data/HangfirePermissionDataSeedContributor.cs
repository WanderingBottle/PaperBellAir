using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace PaperBellStore.Data;

/// <summary>
/// Hangfire 权限数据种子贡献者
/// 为 admin 角色自动分配 Hangfire Dashboard 的所有权限
/// </summary>
public class HangfirePermissionDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IPermissionDataSeeder _permissionDataSeeder;
    private readonly IdentityRoleManager _roleManager;

    // Hangfire 权限常量（与 PaperBellStorePermissions 保持一致）
    private const string HangfireDashboard = "PaperBellStore.HangfireDashboard";
    private const string HangfireDashboardView = "PaperBellStore.HangfireDashboard.View";
    private const string HangfireDashboardTrigger = "PaperBellStore.HangfireDashboard.Trigger";
    private const string HangfireDashboardDelete = "PaperBellStore.HangfireDashboard.Delete";
    private const string HangfireDashboardCreate = "PaperBellStore.HangfireDashboard.Create";
    private const string HangfireDashboardEdit = "PaperBellStore.HangfireDashboard.Edit";

    public HangfirePermissionDataSeedContributor(
        IPermissionDataSeeder permissionDataSeeder,
        IdentityRoleManager roleManager)
    {
        _permissionDataSeeder = permissionDataSeeder;
        _roleManager = roleManager;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        // 查找 admin 角色
        var adminRole = await _roleManager.FindByNameAsync("admin");
        if (adminRole != null)
        {
            // 为 admin 角色分配所有 Hangfire 权限
            // 使用父权限（包含所有子权限）
            // "R" 是 ABP 框架中角色权限提供者的标准名称
            await _permissionDataSeeder.SeedAsync(
                "R", // RolePermissionValueProvider.ProviderName 的值
                "admin",
                new[]
                {
                    HangfireDashboard // 父权限，包含所有子权限
                },
                context.TenantId
            );
        }
    }
}

