using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using Blazorise;

using Hangfire;
using Hangfire.Storage;

using PaperBellStore.Blazor.Menus;
using PaperBellStore.Blazor.Services;
using PaperBellStore.Permissions;

using Volo.Abp.Authorization.Permissions;

namespace PaperBellStore.Blazor.Components.Pages;

public partial class RecurringJobs
{
    [Inject] protected new ILogger<RecurringJobs> Logger { get; set; } = default!;
    [Inject] protected IPermissionChecker PermissionChecker { get; set; } = default!;
    [Inject] protected INotificationService NotificationService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected RecurringJobStateService JobStateService { get; set; } = default!;
    [Inject] protected RecurringJobRecoveryService JobRecoveryService { get; set; } = default!;

    protected override string BreadcrumbId => "recurring-jobs-breadcrumb-container";
    protected override string CurrentPagePath => "/hangfire-recurring-jobs";
    protected override string[] MenuItemPaths => new[]
    {
        PaperBellStoreMenus.Home,
        PaperBellStoreMenus.RunningLogGroup,
        PaperBellStoreMenus.HangfireRecurringJobs
    };

    private List<RecurringJobDto> recurringJobs = new();
    private bool isLoading;
    private bool canEdit;
    private string? lastError;
    private int lastCount;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        canEdit = await PermissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardEdit);
        await LoadRecurringJobsAsync();
    }

    private async Task LoadRecurringJobsAsync()
    {
        try
        {
            isLoading = true;
            lastError = null;
            lastCount = 0;

            recurringJobs = await Task.Run(() =>
            {
                var list = new List<RecurringJobDto>();

                using var connection = JobStorage.Current.GetConnection();
                var jobs = connection.GetRecurringJobs();
                var paused = JobStateService.GetPausedJobIds().ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var job in jobs)
                {
                    list.Add(new RecurringJobDto
                    {
                        Id = job.Id,
                        Cron = job.Cron ?? string.Empty,
                        TimeZoneId = job.TimeZoneId ?? string.Empty,
                        NextExecution = job.NextExecution,
                        LastExecution = job.LastExecution,
                        CreatedAt = job.CreatedAt,
                        Error = job.Error ?? string.Empty,
                        IsPaused = paused.Contains(job.Id)
                    });
                }

                foreach (var pausedId in paused)
                {
                    if (list.Any(x => string.Equals(x.Id, pausedId, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var config = JobStateService.GetJobConfig(pausedId);
                    if (config != null)
                    {
                        list.Add(new RecurringJobDto
                        {
                            Id = pausedId,
                            Cron = config.CronExpression ?? string.Empty,
                            TimeZoneId = config.TimeZoneId ?? string.Empty,
                            CreatedAt = config.PausedAt,
                            IsPaused = true
                        });
                    }
                }

                return list.OrderBy(x => x.Id).ToList();
            });

            lastCount = recurringJobs.Count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载周期性任务列表失败");
            recurringJobs = new List<RecurringJobDto>();
            lastError = ex.Message;
            await NotificationService.Error("加载任务列表失败：" + ex.Message);
        }
        finally
        {
            isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task PauseJob(string jobId)
    {
        try
        {
            isLoading = true;
            await InvokeAsync(StateHasChanged);

            var result = await Task.Run(() => PauseJobInternal(jobId));
            if (result != null)
            {
                await NotificationService.Warning($"暂停失败：{result}");
            }
            else
            {
                await NotificationService.Success("任务已暂停");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "暂停任务失败: {JobId}", jobId);
            await NotificationService.Error("暂停任务失败：" + ex.Message);
        }
        finally
        {
            await LoadRecurringJobsAsync();
        }
    }

    private async Task ResumeJob(string jobId)
    {
        try
        {
            isLoading = true;
            await InvokeAsync(StateHasChanged);

            var result = await Task.Run(() => ResumeJobInternal(jobId));
            if (result != null)
            {
                await NotificationService.Warning($"恢复失败：{result}");
            }
            else
            {
                await NotificationService.Success("任务已恢复");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "恢复任务失败: {JobId}", jobId);
            await NotificationService.Error("恢复任务失败：" + ex.Message);
        }
        finally
        {
            await LoadRecurringJobsAsync();
        }
    }

    private Task RefreshJobs() => LoadRecurringJobsAsync();

    private void NavigateToDashboard()
    {
        NavigationManager.NavigateTo("/hangfire", forceLoad: true);
    }

    private string? PauseJobInternal(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return "JobId 不能为空";
        }

        using var connection = JobStorage.Current.GetConnection();
        var job = connection.GetRecurringJobs().FirstOrDefault(j => j.Id == jobId);
        if (job == null)
        {
            return "任务不存在";
        }

        if (JobStateService.IsPaused(jobId))
        {
            return "任务已经处于暂停状态";
        }

        var config = JobRecoveryService.GetCurrentJobConfig(jobId);
        if (config == null)
        {
            return "无法获取任务配置信息";
        }

        config.PausedBy = CurrentUser?.UserName ?? "System";
        config.PausedAt = DateTime.UtcNow;
        JobStateService.SaveJobConfig(jobId, config);

        RecurringJob.RemoveIfExists(jobId);
        return null;
    }

    private string? ResumeJobInternal(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return "JobId 不能为空";
        }

        if (!JobStateService.IsPaused(jobId))
        {
            return "任务未处于暂停状态";
        }

        var config = JobStateService.GetJobConfig(jobId);
        if (config == null)
        {
            return "找不到任务配置信息";
        }

        var success = JobRecoveryService.RecoverJob(config);
        if (!success)
        {
            return "恢复任务失败，可能是任务类型不支持";
        }

        JobStateService.RemoveJobConfig(jobId);
        return null;
    }
}

public class RecurringJobDto
{
    public string Id { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public DateTime? NextExecution { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string Error { get; set; } = string.Empty;
    public bool IsPaused { get; set; }
}
