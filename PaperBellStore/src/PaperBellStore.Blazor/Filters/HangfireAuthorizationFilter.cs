using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace PaperBellStore.Blazor.Filters
{
    /// <summary>
    /// Hangfire Dashboard 授权过滤器
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

            // 可选：检查用户角色或权限
            // 例如：只有管理员才能访问
            // return httpContext.User.IsInRole("admin");

            // 默认：已认证的用户都可以访问
            return true;
        }
    }
}

