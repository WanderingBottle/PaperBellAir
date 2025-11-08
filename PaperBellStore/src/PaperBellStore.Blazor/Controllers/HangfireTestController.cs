using System;
using System.Linq;
using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
// using PaperBellStore.Blazor.RecurringJobs; // SampleRecurringJob 已移除
using Volo.Abp.AspNetCore.Mvc;

namespace PaperBellStore.Blazor.Controllers
{
    /// <summary>
    /// Hangfire 测试控制器
    /// 用于测试 Hangfire 相关功能
    /// </summary>
    [Route("api/hangfire-test")]
    public class HangfireTestController : AbpControllerBase
    {
        /// <summary>
        /// 立即执行示例任务
        /// 注意：SampleRecurringJob 已移除，此方法已禁用
        /// </summary>
        // [HttpPost("execute-sample-job")]
        // public IActionResult ExecuteSampleJob()
        // {
        //     var jobId = BackgroundJob.Enqueue<SampleRecurringJob>(
        //         job => job.ExecuteAsync());
        //
        //     return Ok(new
        //     {
        //         Message = "任务已加入队列",
        //         JobId = jobId,
        //         Status = "Enqueued",
        //         Timestamp = DateTime.Now
        //     });
        // }

        /// <summary>
        /// 延迟执行任务（默认30秒后）
        /// 注意：SampleRecurringJob 已移除，此方法已禁用
        /// </summary>
        // [HttpPost("schedule-sample-job")]
        // public IActionResult ScheduleSampleJob([FromQuery] int delaySeconds = 30)
        // {
        //     var jobId = BackgroundJob.Schedule<SampleRecurringJob>(
        //         job => job.ExecuteAsync(),
        //         TimeSpan.FromSeconds(delaySeconds));
        //
        //     return Ok(new
        //     {
        //         Message = $"任务已计划在 {delaySeconds} 秒后执行",
        //         JobId = jobId,
        //         Status = "Scheduled",
        //         ScheduledTime = DateTime.Now.AddSeconds(delaySeconds),
        //         Timestamp = DateTime.Now
        //     });
        // }

        /// <summary>
        /// 执行带参数的任务
        /// 注意：SampleRecurringJob 已移除，此方法已禁用
        /// </summary>
        // [HttpPost("execute-with-parameters")]
        // public IActionResult ExecuteWithParameters([FromBody] JobParameters parameters)
        // {
        //     if (parameters == null || string.IsNullOrEmpty(parameters.Message))
        //     {
        //         return BadRequest(new { Message = "参数不能为空" });
        //     }
        //
        //     var jobId = BackgroundJob.Enqueue<SampleRecurringJob>(
        //         job => job.ExecuteWithParametersAsync(parameters.Message, parameters.Count));
        //
        //     return Ok(new
        //     {
        //         Message = "带参数的任务已加入队列",
        //         JobId = jobId,
        //         Status = "Enqueued",
        //         Parameters = parameters,
        //         Timestamp = DateTime.Now
        //     });
        // }

        /// <summary>
        /// 在指定队列中执行任务
        /// 注意：SampleRecurringJob 已移除，此方法已禁用
        /// </summary>
        // [HttpPost("execute-in-queue")]
        // public IActionResult ExecuteInQueue([FromQuery] string queue = "critical")
        // {
        //     var validQueues = new[] { "default", "critical", "low" };
        //     if (!Array.Exists(validQueues, q => q == queue))
        //     {
        //         return BadRequest(new
        //         {
        //             Message = "无效的队列名称",
        //             ValidQueues = validQueues
        //         });
        //     }
        //
        //     // 使用 BackgroundJobClient 来指定队列
        //     var client = new BackgroundJobClient(JobStorage.Current);
        //     var job = Job.FromExpression<SampleRecurringJob>(job => job.ExecuteAsync());
        //     var jobId = client.Create(job, new EnqueuedState(queue));
        //
        //     return Ok(new
        //     {
        //         Message = $"任务已加入 {queue} 队列",
        //         JobId = jobId,
        //         Queue = queue,
        //         Status = "Enqueued",
        //         Timestamp = DateTime.Now
        //     });
        // }

        /// <summary>
        /// 立即触发定时任务
        /// </summary>
        [HttpPost("trigger-recurring-job")]
        public IActionResult TriggerRecurringJob([FromQuery] string jobId = "sample-job-daily")
        {
            try
            {
                // 使用新的 API（推荐）
                var recurringJobManager = new RecurringJobManager(JobStorage.Current);
                recurringJobManager.TriggerJob(jobId);

                return Ok(new
                {
                    Message = "定时任务已触发",
                    RecurringJobId = jobId,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "触发任务失败",
                    Error = ex.Message,
                    RecurringJobId = jobId
                });
            }
        }

        /// <summary>
        /// 添加或更新定时任务
        /// </summary>
        [HttpPost("add-or-update-recurring")]
        public IActionResult AddOrUpdateRecurring([FromBody] RecurringJobRequest? request)
        {
            if (request == null || string.IsNullOrEmpty(request.JobId) || string.IsNullOrEmpty(request.CronExpression))
            {
                return BadRequest(new { Message = "JobId 和 CronExpression 不能为空" });
            }

            // 注意：SampleRecurringJob 已移除，此功能已禁用
            return BadRequest(new
            {
                Message = "此功能已禁用：SampleRecurringJob 已移除",
                Suggestion = "请创建新的任务类并更新此方法"
            });

            // 原始代码已注释（SampleRecurringJob 已移除）
            // try
            // {
            //     RecurringJob.AddOrUpdate<SampleRecurringJob>(
            //         request.JobId,
            //         job => job.ExecuteAsync(),
            //         request.CronExpression,
            //         new RecurringJobOptions
            //         {
            //             TimeZone = TimeZoneInfo.Local
            //         });
            //
            //     return Ok(new
            //     {
            //         Message = "定时任务已添加或更新",
            //         JobId = request.JobId,
            //         CronExpression = request.CronExpression,
            //         TimeZone = TimeZoneInfo.Local.Id,
            //         Timestamp = DateTime.Now
            //     });
            // }
            // catch (Exception ex)
            // {
            //     return BadRequest(new
            //     {
            //         Message = "添加或更新任务失败",
            //         Error = ex.Message
            //     });
            // }
        }

        /// <summary>
        /// 删除定时任务
        /// </summary>
        [HttpDelete("remove-recurring")]
        public IActionResult RemoveRecurring([FromQuery] string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return BadRequest(new { Message = "JobId 不能为空" });
            }

            // 先检查任务是否存在
            bool exists;
            using (var connection = JobStorage.Current.GetConnection())
            {
                var recurringJobs = connection.GetRecurringJobs();
                exists = recurringJobs.Any(job => job.Id == jobId);
            }

            if (exists)
            {
                RecurringJob.RemoveIfExists(jobId);
            }

            return Ok(new
            {
                Message = exists ? "定时任务已删除" : "任务不存在或已被删除",
                JobId = jobId,
                Removed = exists,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 获取任务状态
        /// </summary>
        [HttpGet("job-status/{jobId}")]
        public IActionResult GetJobStatus(string jobId)
        {
            try
            {
                var connection = JobStorage.Current.GetConnection();
                var jobData = connection.GetJobData(jobId);

                if (jobData == null)
                {
                    return NotFound(new
                    {
                        Message = "任务不存在",
                        JobId = jobId
                    });
                }

                return Ok(new
                {
                    JobId = jobId,
                    State = jobData.State,
                    CreatedAt = jobData.CreatedAt,
                    LoadException = jobData.LoadException?.Message,
                    Job = jobData.Job?.ToString(),
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "获取任务状态失败",
                    Error = ex.Message,
                    JobId = jobId
                });
            }
        }

        /// <summary>
        /// 获取所有定时任务
        /// </summary>
        [HttpGet("recurring-jobs")]
        public IActionResult GetRecurringJobs()
        {
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    var recurringJobs = connection.GetRecurringJobs();

                    var jobs = recurringJobs.Select(job => new
                    {
                        job.Id,
                        job.Cron,
                        job.TimeZoneId,
                        job.NextExecution,
                        job.LastExecution,
                        job.CreatedAt,
                        job.Error
                    }).ToList();

                    return Ok(new
                    {
                        Message = "获取定时任务列表成功",
                        Count = jobs.Count,
                        Jobs = jobs,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "获取定时任务列表失败",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// 测试任务执行（快速测试）
        /// 注意：SampleRecurringJob 已移除，此方法已禁用
        /// </summary>
        // [HttpPost("quick-test")]
        // public IActionResult QuickTest()
        // {
        //     var results = new
        //     {
        //         Message = "快速测试 Hangfire 功能",
        //         Tests = new[]
        //         {
        //             new { Test = "立即执行任务", Status = "执行中" },
        //             new { Test = "延迟执行任务", Status = "已计划" },
        //             new { Test = "带参数任务", Status = "已计划" }
        //         },
        //         Timestamp = DateTime.Now
        //     };
        //
        //     // 执行多个测试任务
        //     var jobId1 = BackgroundJob.Enqueue<SampleRecurringJob>(
        //         job => job.ExecuteAsync());
        //
        //     var jobId2 = BackgroundJob.Schedule<SampleRecurringJob>(
        //         job => job.ExecuteAsync(),
        //         TimeSpan.FromSeconds(5));
        //
        //     var jobId3 = BackgroundJob.Enqueue<SampleRecurringJob>(
        //         job => job.ExecuteWithParametersAsync("快速测试", 3));
        //
        //     return Ok(new
        //     {
        //         Message = "快速测试任务已创建",
        //         Jobs = new[]
        //         {
        //             new { Type = "立即执行", JobId = jobId1 },
        //             new { Type = "延迟执行（5秒）", JobId = jobId2 },
        //             new { Type = "带参数执行", JobId = jobId3 }
        //         },
        //         Timestamp = DateTime.Now
        //     });
        // }
    }

    /// <summary>
    /// 任务参数模型
    /// </summary>
    public class JobParameters
    {
        public string? Message { get; set; }
        public int Count { get; set; } = 1;
    }

    /// <summary>
    /// 定时任务请求模型
    /// </summary>
    public class RecurringJobRequest
    {
        public string? JobId { get; set; }
        public string? CronExpression { get; set; }
    }
}

