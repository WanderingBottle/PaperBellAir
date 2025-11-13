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
        PaperBellStoreMenus.TaskSchedulingCenterGroup,
        PaperBellStoreMenus.HangfireRecurringJobs
    };

    private List<RecurringJobDto> recurringJobs = new();
    private bool isLoading;
    private bool canEdit;
    private bool canTrigger;
    private bool canDelete;
    private string? lastError;
    private int lastCount;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        canEdit = await PermissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardEdit);
        canTrigger = await PermissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardTrigger);
        canDelete = await PermissionChecker.IsGrantedAsync(PaperBellStorePermissions.HangfireDashboardDelete);
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
            Logger.LogError(ex, L["RecurringJobs:LogLoadFailed"]);
            recurringJobs = new List<RecurringJobDto>();
            lastError = ex.Message;
            await NotificationService.Error(L["RecurringJobs:LoadJobsFailed", ex.Message]);
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
                await NotificationService.Warning(L["RecurringJobs:PauseFailed", result]);
            }
            else
            {
                await NotificationService.Success(L["RecurringJobs:PauseSuccess"]);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, L["RecurringJobs:LogPauseFailed", jobId]);
            await NotificationService.Error(L["RecurringJobs:PauseJobFailed", ex.Message]);
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
                await NotificationService.Warning(L["RecurringJobs:ResumeFailed", result]);
            }
            else
            {
                await NotificationService.Success(L["RecurringJobs:ResumeSuccess"]);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, L["RecurringJobs:LogResumeFailed", jobId]);
            await NotificationService.Error(L["RecurringJobs:ResumeJobFailed", ex.Message]);
        }
        finally
        {
            await LoadRecurringJobsAsync();
        }
    }

    private async Task TriggerJob(string jobId)
    {
        try
        {
            isLoading = true;
            await InvokeAsync(StateHasChanged);

            var error = await Task.Run(() => TriggerJobInternal(jobId));
            if (error != null)
            {
                await NotificationService.Warning(L["RecurringJobs:TriggerFailed", error]);
            }
            else
            {
                await NotificationService.Success(L["RecurringJobs:TriggerSuccess"]);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, L["RecurringJobs:LogTriggerFailed", jobId]);
            await NotificationService.Error(L["RecurringJobs:TriggerJobFailed", ex.Message]);
        }
        finally
        {
            await LoadRecurringJobsAsync();
        }
    }

    private async Task DeleteJob(string jobId)
    {
        try
        {
            isLoading = true;
            await InvokeAsync(StateHasChanged);

            var error = await Task.Run(() => DeleteJobInternal(jobId));
            if (error != null)
            {
                await NotificationService.Warning(L["RecurringJobs:DeleteFailed", error]);
            }
            else
            {
                await NotificationService.Success(L["RecurringJobs:DeleteSuccess"]);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, L["RecurringJobs:LogDeleteFailed", jobId]);
            await NotificationService.Error(L["RecurringJobs:DeleteJobFailed", ex.Message]);
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
            return L["RecurringJobs:ErrorJobIdEmpty"];
        }

        using var connection = JobStorage.Current.GetConnection();
        var job = connection.GetRecurringJobs().FirstOrDefault(j => j.Id == jobId);
        if (job == null)
        {
            return L["RecurringJobs:ErrorJobNotFound"];
        }

        if (JobStateService.IsPaused(jobId))
        {
            return L["RecurringJobs:ErrorJobAlreadyPaused"];
        }

        var config = JobRecoveryService.GetCurrentJobConfig(jobId);
        if (config == null)
        {
            return L["RecurringJobs:ErrorJobConfigMissing"];
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
            return L["RecurringJobs:ErrorJobIdEmpty"];
        }

        if (!JobStateService.IsPaused(jobId))
        {
            return L["RecurringJobs:ErrorJobNotPaused"];
        }

        var config = JobStateService.GetJobConfig(jobId);
        if (config == null)
        {
            return L["RecurringJobs:ErrorJobConfigNotFound"];
        }

        var success = JobRecoveryService.RecoverJob(config);
        if (!success)
        {
            return L["RecurringJobs:ErrorJobRecoverFailed"];
        }

        JobStateService.RemoveJobConfig(jobId);
        return null;
    }

    private string? TriggerJobInternal(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return L["RecurringJobs:ErrorJobIdEmpty"];
        }

        using var connection = JobStorage.Current.GetConnection();
        var job = connection.GetRecurringJobs().FirstOrDefault(j => j.Id == jobId);
        if (job == null)
        {
            return L["RecurringJobs:ErrorJobNotFound"];
        }

        RecurringJob.TriggerJob(jobId);
        return null;
    }

    private string? DeleteJobInternal(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return L["RecurringJobs:ErrorJobIdEmpty"];
        }

        using var connection = JobStorage.Current.GetConnection();
        var job = connection.GetRecurringJobs().FirstOrDefault(j => j.Id == jobId);
        if (job == null)
        {
            return L["RecurringJobs:ErrorJobRemoved"];
        }

        RecurringJob.RemoveIfExists(jobId);
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
