using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PaperBellStore.Blazor.Middleware
{
    /// <summary>
    /// Hangfire Dashboard 脚本注入中间件
    /// 在 Dashboard 页面中注入自定义 JavaScript 和 CSS
    /// </summary>
    public class HangfireDashboardInjectionMiddleware
    {
        private readonly RequestDelegate _next;

        public HangfireDashboardInjectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 只处理 Hangfire Dashboard 路径
            if (!context.Request.Path.StartsWithSegments("/hangfire"))
            {
                await _next(context);
                return;
            }

            // 拦截响应
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // 只处理 HTML 响应
            if (context.Response.ContentType?.Contains("text/html") == true)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();

                // 注入脚本和样式
                var injectionScript = @"
<!-- Hangfire Dashboard Extensions -->
<link rel=""stylesheet"" href=""/hangfire/css/extensions.css"" />
<script src=""/hangfire/js/extensions.js""></script>
";

                // 在 </head> 标签前注入
                if (responseText.Contains("</head>"))
                {
                    responseText = responseText.Replace("</head>", injectionScript + "</head>");
                }
                // 如果没有 </head>，在 <body> 标签后注入
                else if (responseText.Contains("<body>"))
                {
                    responseText = responseText.Replace("<body>", "<body>" + injectionScript);
                }

                var modifiedBytes = Encoding.UTF8.GetBytes(responseText);
                context.Response.ContentLength = modifiedBytes.Length;

                await context.Response.Body.WriteAsync(modifiedBytes, 0, modifiedBytes.Length);
            }

            context.Response.Body = originalBodyStream;
        }
    }
}

