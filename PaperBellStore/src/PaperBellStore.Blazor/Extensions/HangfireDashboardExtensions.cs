using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Threading.Tasks;

namespace PaperBellStore.Blazor.Extensions
{
    /// <summary>
    /// Hangfire Dashboard 扩展
    /// 用于注入自定义 JavaScript 和 CSS 资源
    /// </summary>
    public static class HangfireDashboardExtensions
    {
        /// <summary>
        /// 添加自定义 Dashboard 资源路由
        /// </summary>
        public static IApplicationBuilder UseHangfireDashboardExtensions(this IApplicationBuilder app)
        {
            var webHostEnvironment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            var fileProvider = new PhysicalFileProvider(webHostEnvironment.WebRootPath);

            // 添加自定义 JavaScript 资源路由
            app.Map("/hangfire/js/extensions.js", builder =>
            {
                builder.Run(async context =>
                {
                    context.Response.ContentType = "application/javascript";
                    var fileInfo = fileProvider.GetFileInfo("hangfire-dashboard-extensions.js");

                    if (fileInfo.Exists)
                    {
                        await context.Response.SendFileAsync(fileInfo);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("// File not found");
                    }
                });
            });

            // 添加自定义 CSS 资源路由
            app.Map("/hangfire/css/extensions.css", builder =>
            {
                builder.Run(async context =>
                {
                    context.Response.ContentType = "text/css";
                    var fileInfo = fileProvider.GetFileInfo("hangfire-dashboard-extensions.css");

                    if (fileInfo.Exists)
                    {
                        await context.Response.SendFileAsync(fileInfo);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("/* File not found */");
                    }
                });
            });

            return app;
        }
    }
}

