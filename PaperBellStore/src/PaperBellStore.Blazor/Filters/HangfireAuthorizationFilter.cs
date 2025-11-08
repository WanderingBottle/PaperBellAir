using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Authorization.Permissions;
using PaperBellStore.Permissions;

namespace PaperBellStore.Blazor.Filters
{
    /// <summary>
    /// Hangfire Dashboard 授权过滤器
    /// 控制 Dashboard 的访问权限
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // 检查用户是否已认证
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            // 检查是否有查看 Dashboard 的权限
            var permissionChecker = httpContext.RequestServices
                .GetRequiredService<IPermissionChecker>();

            return permissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardView)
                .GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Hangfire Dashboard 只读模式过滤器
    /// 根据用户权限动态控制 Dashboard 是否为只读模式
    /// </summary>
    public class HangfireReadOnlyFilter
    {
        public static bool IsReadOnly(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            {
                return true;  // 未认证用户只能只读
            }

            var permissionChecker = httpContext.RequestServices
                .GetRequiredService<IPermissionChecker>();

            // 如果没有任何操作权限，则设置为只读
            var hasTrigger = permissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardTrigger)
                .GetAwaiter().GetResult();
            var hasDelete = permissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardDelete)
                .GetAwaiter().GetResult();
            var hasCreate = permissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardCreate)
                .GetAwaiter().GetResult();
            var hasEdit = permissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardEdit)
                .GetAwaiter().GetResult();

            // 如果没有任何操作权限，则为只读模式
            return !(hasTrigger || hasDelete || hasCreate || hasEdit);
        }
    }
}

