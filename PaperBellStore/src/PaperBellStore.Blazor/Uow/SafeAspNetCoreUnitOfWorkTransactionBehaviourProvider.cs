using Microsoft.AspNetCore.Http;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace PaperBellStore.Blazor.Uow
{
    /// <summary>
    /// 安全的 ASP.NET Core UnitOfWork 事务行为提供程序
    /// 修复在 Blazor Server 模式下 HttpContext.Request 可能为 null 的问题
    /// </summary>
    public class SafeAspNetCoreUnitOfWorkTransactionBehaviourProvider : IUnitOfWorkTransactionBehaviourProvider, ITransientDependency
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SafeAspNetCoreUnitOfWorkTransactionBehaviourProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool? IsTransactional
        {
            get
            {
                try
                {
                    var httpContext = _httpContextAccessor.HttpContext;

                    // 如果 HttpContext 为 null，返回 null（让 ABP 使用默认行为）
                    if (httpContext == null)
                    {
                        return null;
                    }

                    // 如果 Request 为 null，返回 null（让 ABP 使用默认行为）
                    if (httpContext.Request == null)
                    {
                        return null;
                    }

                    // 安全地访问 Path 属性
                    var path = httpContext.Request.Path;

                    // 如果 Path 为 null，返回 null（让 ABP 使用默认行为）
                    if (path == null || !path.HasValue)
                    {
                        return null;
                    }

                    // 检查是否为非事务性路径（例如 SignalR、Blazor 等）
                    var pathValue = path.Value?.ToLower() ?? "";

                    // SignalR 连接路径
                    if (pathValue.Contains("/_blazor") ||
                        pathValue.Contains("/signalr") ||
                        pathValue.StartsWith("/_framework"))
                    {
                        return false; // 非事务性
                    }

                    // 默认返回 null，让 ABP 使用默认行为
                    return null;
                }
                catch
                {
                    // 如果发生任何异常，返回 null（让 ABP 使用默认行为）
                    return null;
                }
            }
        }
    }
}

