using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace PaperBellStore.Blazor.RecurringJobs
{
    /// <summary>
    /// 示例定时任务
    /// 演示如何使用 Hangfire 创建定时任务
    /// </summary>
    public class SampleRecurringJob : ITransientDependency
    {
        private readonly ILogger<SampleRecurringJob> _logger;

        public SampleRecurringJob(ILogger<SampleRecurringJob> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 执行定时任务
        /// </summary>
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("定时任务执行开始：{Time}", DateTime.Now);

            try
            {
                // 执行任务逻辑
                // 例如：发送邮件、生成报表、数据同步等
                await Task.Delay(1000);  // 模拟异步操作

                _logger.LogInformation("定时任务执行完成：{Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时任务执行失败：{Time}", DateTime.Now);
                throw;  // 重新抛出异常，Hangfire 会自动重试
            }
        }

        /// <summary>
        /// 带参数的任务示例
        /// </summary>
        public async Task ExecuteWithParametersAsync(string message, int count)
        {
            _logger.LogInformation("定时任务执行开始：{Message}, {Count}", message, count);

            try
            {
                // 执行任务逻辑
                for (int i = 0; i < count; i++)
                {
                    _logger.LogInformation("执行第 {Index} 次：{Message}", i + 1, message);
                    await Task.Delay(100);
                }

                _logger.LogInformation("定时任务执行完成：{Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定时任务执行失败：{Message}", message);
                throw;
            }
        }
    }
}

