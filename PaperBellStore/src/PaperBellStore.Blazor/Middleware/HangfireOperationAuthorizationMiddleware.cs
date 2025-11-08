using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Authorization.Permissions;
using PaperBellStore.Permissions;

namespace PaperBellStore.Blazor.Middleware
{
    /// <summary>
    /// Hangfire 操作授权中间件
    /// 拦截 Hangfire Dashboard 的特定操作请求，进行权限验证
    /// </summary>
    public class HangfireOperationAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public HangfireOperationAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 只处理 Hangfire Dashboard 路径下的请求
            if (context.Request.Path.StartsWithSegments("/hangfire"))
            {
                var path = context.Request.Path.Value?.ToLower() ?? "";
                var method = context.Request.Method;
                var queryString = context.Request.QueryString.Value?.ToLower() ?? "";

                // 检查是否为需要权限验证的操作
                if (method == "POST" || method == "DELETE")
                {
                    // 检查用户是否已认证
                    if (!context.User.Identity?.IsAuthenticated ?? true)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("未授权：请先登录");
                        return;
                    }

                    var permissionChecker = context.RequestServices
                        .GetRequiredService<IPermissionChecker>();

                    // 检查立即执行任务（Trigger）权限
                    // Hangfire Dashboard 使用 SignalR 或特定的 API 端点
                    if (path.Contains("/recurring/trigger") ||
                        (path.Contains("/recurring") && queryString.Contains("trigger")) ||
                        (path.Contains("/jobs/") && method == "POST" && queryString.Contains("trigger")))
                    {
                        var hasPermission = await permissionChecker.IsGrantedAsync(
                            PaperBellStorePermissions.HangfireDashboardTrigger);

                        if (!hasPermission)
                        {
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("{\"error\":\"没有权限执行此操作：立即执行任务\"}");
                            return;
                        }
                    }

                    // 检查删除任务权限
                    if (path.Contains("/recurring/delete") ||
                        (path.Contains("/recurring") && method == "DELETE") ||
                        (path.Contains("/recurring") && queryString.Contains("delete")))
                    {
                        var hasPermission = await permissionChecker.IsGrantedAsync(
                            PaperBellStorePermissions.HangfireDashboardDelete);

                        if (!hasPermission)
                        {
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("{\"error\":\"没有权限执行此操作：删除任务\"}");
                            return;
                        }
                    }

                    // 检查创建任务权限
                    if (path.Contains("/recurring/add") ||
                        (path.Contains("/recurring") && queryString.Contains("add")) ||
                        (path.Contains("/recurring") && method == "POST" && !queryString.Contains("trigger") && !queryString.Contains("delete")))
                    {
                        var hasPermission = await permissionChecker.IsGrantedAsync(
                            PaperBellStorePermissions.HangfireDashboardCreate);

                        if (!hasPermission)
                        {
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("{\"error\":\"没有权限执行此操作：创建任务\"}");
                            return;
                        }
                    }

                    // 检查编辑任务权限
                    if (path.Contains("/recurring/update") ||
                        (path.Contains("/recurring") && queryString.Contains("update")) ||
                        (path.Contains("/recurring") && method == "POST" && queryString.Contains("edit")))
                    {
                        var hasPermission = await permissionChecker.IsGrantedAsync(
                            PaperBellStorePermissions.HangfireDashboardEdit);

                        if (!hasPermission)
                        {
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("{\"error\":\"没有权限执行此操作：编辑任务\"}");
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}

