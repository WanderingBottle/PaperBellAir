using System;
using System.Linq;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
using PaperBellStore.Blazor.Services;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Authorization.Permissions;
using PaperBellStore.Permissions;

namespace PaperBellStore.Blazor.Controllers
{
    /// <summary>
    /// Hangfire 周期性任务管理控制器
    /// 提供暂停、恢复、状态查询等功能
    /// </summary>
    [Route("api/hangfire/recurring")]
    public class HangfireRecurringJobController : AbpControllerBase
    {
        private readonly RecurringJobStateService _stateService;
        private readonly RecurringJobRecoveryService _recoveryService;
        private readonly IPermissionChecker _permissionChecker;

        public HangfireRecurringJobController(
            RecurringJobStateService stateService,
            RecurringJobRecoveryService recoveryService,
            IPermissionChecker permissionChecker)
        {
            _stateService = stateService;
            _recoveryService = recoveryService;
            _permissionChecker = permissionChecker;
        }

        /// <summary>
        /// 暂停周期性任务
        /// </summary>
        /// <param name="jobId">任务ID</param>
        /// <returns>操作结果</returns>
        [HttpPost("pause/{jobId}")]
        public IActionResult Pause(string jobId)
        {
            // 权限检查
            if (!_permissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardEdit).Result)
            {
                return Forbid("没有暂停任务的权限");
            }

            if (string.IsNullOrEmpty(jobId))
            {
                return BadRequest(new { Message = "JobId 不能为空" });
            }

            try
            {
                // 检查任务是否存在
                using var connection = JobStorage.Current.GetConnection();
                var recurringJobs = connection.GetRecurringJobs();
                var job = recurringJobs.FirstOrDefault(j => j.Id == jobId);

                if (job == null)
                {
                    return NotFound(new { Message = "任务不存在", JobId = jobId });
                }

                // 检查任务是否已经暂停
                if (_stateService.IsPaused(jobId))
                {
                    return BadRequest(new { Message = "任务已经处于暂停状态", JobId = jobId });
                }

                // 获取当前任务配置信息
                var config = _recoveryService.GetCurrentJobConfig(jobId);
                if (config == null)
                {
                    return BadRequest(new { Message = "无法获取任务配置信息", JobId = jobId });
                }

                // 保存任务配置信息
                config.PausedBy = CurrentUser?.UserName ?? "System";
                config.PausedAt = DateTime.UtcNow;
                _stateService.SaveJobConfig(jobId, config);

                // 删除任务（暂停）
                RecurringJob.RemoveIfExists(jobId);

                return Ok(new
                {
                    Message = "任务已暂停",
                    JobId = jobId,
                    PausedAt = config.PausedAt,
                    PausedBy = config.PausedBy,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "暂停任务失败",
                    Error = ex.Message,
                    JobId = jobId
                });
            }
        }

        /// <summary>
        /// 恢复周期性任务
        /// </summary>
        /// <param name="jobId">任务ID</param>
        /// <returns>操作结果</returns>
        [HttpPost("resume/{jobId}")]
        public IActionResult Resume(string jobId)
        {
            // 权限检查
            if (!_permissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardEdit).Result)
            {
                return Forbid("没有恢复任务的权限");
            }

            if (string.IsNullOrEmpty(jobId))
            {
                return BadRequest(new { Message = "JobId 不能为空" });
            }

            try
            {
                // 检查任务是否已暂停
                if (!_stateService.IsPaused(jobId))
                {
                    return BadRequest(new { Message = "任务未处于暂停状态", JobId = jobId });
                }

                // 获取保存的任务配置信息
                var config = _stateService.GetJobConfig(jobId);
                if (config == null)
                {
                    return NotFound(new { Message = "找不到任务配置信息，无法恢复", JobId = jobId });
                }

                // 恢复任务
                var success = _recoveryService.RecoverJob(config);
                if (!success)
                {
                    return BadRequest(new { Message = "恢复任务失败，可能是任务类型不支持", JobId = jobId });
                }

                // 删除保存的状态信息
                _stateService.RemoveJobConfig(jobId);

                return Ok(new
                {
                    Message = "任务已恢复",
                    JobId = jobId,
                    CronExpression = config.CronExpression,
                    TimeZone = config.TimeZoneId,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "恢复任务失败",
                    Error = ex.Message,
                    JobId = jobId
                });
            }
        }

        /// <summary>
        /// 立即执行一次任务
        /// </summary>
        [HttpPost("trigger/{jobId}")]
        public IActionResult Trigger(string jobId)
        {
            if (!_permissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardTrigger).Result)
            {
                return Forbid("没有立即执行任务的权限");
            }

            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest(new { Message = "JobId 不能为空" });
            }

            try
            {
                using var connection = JobStorage.Current.GetConnection();
                var recurringJobs = connection.GetRecurringJobs();
                var job = recurringJobs.FirstOrDefault(j => j.Id == jobId);

                if (job == null)
                {
                    return NotFound(new { Message = "任务不存在", JobId = jobId });
                }

                RecurringJob.TriggerJob(jobId);

                return Ok(new
                {
                    Message = "任务已触发执行",
                    JobId = jobId,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "触发任务执行失败",
                    Error = ex.Message,
                    JobId = jobId
                });
            }
        }

        /// <summary>
        /// 删除周期性任务
        /// </summary>
        [HttpPost("delete/{jobId}")]
        public IActionResult Delete(string jobId)
        {
            if (!_permissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardDelete).Result)
            {
                return Forbid("没有删除任务的权限");
            }

            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest(new { Message = "JobId 不能为空" });
            }

            try
            {
                using var connection = JobStorage.Current.GetConnection();
                var recurringJobs = connection.GetRecurringJobs();
                var job = recurringJobs.FirstOrDefault(j => j.Id == jobId);

                if (job == null)
                {
                    return NotFound(new { Message = "任务不存在", JobId = jobId });
                }

                RecurringJob.RemoveIfExists(jobId);
                _stateService.RemoveJobConfig(jobId);

                return Ok(new
                {
                    Message = "任务已删除",
                    JobId = jobId,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "删除任务失败",
                    Error = ex.Message,
                    JobId = jobId
                });
            }
        }

        /// <summary>
        /// 获取任务状态
        /// </summary>
        /// <param name="jobId">任务ID</param>
        /// <returns>任务状态信息</returns>
        [HttpGet("status/{jobId}")]
        public IActionResult GetStatus(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return BadRequest(new { Message = "JobId 不能为空" });
            }

            try
            {
                using var connection = JobStorage.Current.GetConnection();
                var recurringJobs = connection.GetRecurringJobs();
                var job = recurringJobs.FirstOrDefault(j => j.Id == jobId);

                var isPaused = _stateService.IsPaused(jobId);
                var pausedConfig = isPaused ? _stateService.GetJobConfig(jobId) : null;

                return Ok(new
                {
                    JobId = jobId,
                    Exists = job != null,
                    IsPaused = isPaused,
                    NextExecution = job?.NextExecution,
                    LastExecution = job?.LastExecution,
                    Cron = job?.Cron,
                    TimeZone = job?.TimeZoneId,
                    Error = job?.Error,
                    PausedInfo = pausedConfig != null ? new
                    {
                        PausedAt = pausedConfig.PausedAt,
                        PausedBy = pausedConfig.PausedBy
                    } : null,
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
        /// 获取所有周期性任务的状态列表
        /// </summary>
        /// <returns>任务状态列表</returns>
        [HttpGet("list")]
        public IActionResult GetList()
        {
            try
            {
                using var connection = JobStorage.Current.GetConnection();
                var recurringJobs = connection.GetRecurringJobs();
                var pausedJobIds = _stateService.GetPausedJobIds().ToHashSet();

                var jobs = recurringJobs.Select(job => new
                {
                    job.Id,
                    job.Cron,
                    TimeZoneId = job.TimeZoneId ?? string.Empty,
                    job.NextExecution,
                    job.LastExecution,
                    job.CreatedAt,
                    Error = job.Error ?? string.Empty,
                    IsPaused = pausedJobIds.Contains(job.Id)
                }).ToList();

                // 添加已暂停但不在当前列表中的任务
                foreach (var pausedJobId in pausedJobIds)
                {
                    if (!jobs.Any(j => j.Id == pausedJobId))
                    {
                        var pausedConfig = _stateService.GetJobConfig(pausedJobId);
                        if (pausedConfig != null)
                        {
                            jobs.Add(new
                            {
                                Id = pausedJobId,
                                Cron = pausedConfig.CronExpression ?? string.Empty,
                                TimeZoneId = pausedConfig.TimeZoneId ?? string.Empty,
                                NextExecution = (DateTime?)null,
                                LastExecution = (DateTime?)null,
                                CreatedAt = (DateTime?)(pausedConfig.PausedAt ?? DateTime.UtcNow),
                                Error = string.Empty,
                                IsPaused = true
                            });
                        }
                    }
                }

                return Ok(new
                {
                    Message = "获取任务列表成功",
                    Count = jobs.Count,
                    Jobs = jobs,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "获取任务列表失败",
                    Error = ex.Message
                });
            }
        }
    }
}

