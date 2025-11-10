using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using MudBlazor;

using PaperBellStore.Blazor.Menus;
using PaperBellStore.EntityFrameworkCore;

using Volo.Abp.AuditLogging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace PaperBellStore.Blazor.Components.Pages;

public partial class AuditLog : IAsyncDisposable
{
    [Inject]
    protected IDbContextProvider<PaperBellStoreDbContext> DbContextProvider { get; set; } = default!;

    [Inject]
    protected new ILogger<AuditLog> Logger { get; set; } = default!;

    [Inject]
    protected IUnitOfWorkManager UnitOfWorkManager { get; set; } = default!;

    [Inject]
    protected IDialogService DialogService { get; set; } = default!;

    protected override string BreadcrumbId => "audit-log-breadcrumb-container";
    protected override string CurrentPagePath => "/audit-log";
    protected override string[] MenuItemPaths => new[]
    {
        PaperBellStoreMenus.Home,
        PaperBellStoreMenus.RunningLogGroup,
        PaperBellStoreMenus.AuditLog
    };

    // 审计日志列表
    private List<AuditLogDto>? auditLogs;
    private int totalCount = 0;
    private int currentPage = 1;
    private int pageSize = 20;
    private int totalPages => (int)Math.Ceiling((double)totalCount / pageSize);
    private bool isLoading = false;

    // 过滤条件
    private DateTime? startDate;
    private DateTime? endDate;
    private string? userName;
    private string? httpMethod;
    private int? httpStatusCode;
    private string? urlKeyword;
    private string? clientIpAddress;
    private bool onlyExceptions = false;

    // 选中的审计日志ID（用于显示操作详情）
    private Guid? selectedAuditLogId;
    private List<AuditLogActionDto>? selectedAuditLogActions;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        // 默认查询最近7天的日志
        endDate = DateTime.Now;
        startDate = DateTime.Now.AddDays(-7);
        await LoadAuditLogs();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>
    /// 加载审计日志列表
    /// </summary>
    private async Task LoadAuditLogs()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            using var uow = UnitOfWorkManager.Begin(requiresNew: true);
            var dbContext = await DbContextProvider.GetDbContextAsync();

            var query = dbContext.Set<Volo.Abp.AuditLogging.AuditLog>()
                .AsNoTracking()
                .AsQueryable();

            // 时间范围过滤
            if (startDate.HasValue)
            {
                query = query.Where(x => x.ExecutionTime >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(x => x.ExecutionTime <= endDate.Value.AddDays(1));
            }

            // 用户名过滤
            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(x => x.UserName != null && x.UserName.Contains(userName));
            }

            // HTTP方法过滤
            if (!string.IsNullOrEmpty(httpMethod))
            {
                query = query.Where(x => x.HttpMethod == httpMethod);
            }

            // HTTP状态码过滤
            if (httpStatusCode.HasValue)
            {
                query = query.Where(x => x.HttpStatusCode == httpStatusCode.Value);
            }

            // URL关键词搜索
            if (!string.IsNullOrEmpty(urlKeyword))
            {
                query = query.Where(x => x.Url != null && x.Url.Contains(urlKeyword));
            }

            // 客户端IP过滤
            if (!string.IsNullOrEmpty(clientIpAddress))
            {
                query = query.Where(x => x.ClientIpAddress != null && x.ClientIpAddress.Contains(clientIpAddress));
            }

            // 仅显示异常
            if (onlyExceptions)
            {
                query = query.Where(x => !string.IsNullOrEmpty(x.Exceptions));
            }

            // 过滤掉包含 Volo.Abp 的条目（框架相关的审计日志）
            query = query.Where(x => x.Url == null || !x.Url.Contains("Volo.Abp"));

            // 获取总数
            totalCount = await query.CountAsync();

            // 分页查询
            var logs = await query
                .OrderByDescending(x => x.ExecutionTime)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 转换为DTO
            auditLogs = logs.Select(x => new AuditLogDto
            {
                Id = x.Id,
                ExecutionTime = x.ExecutionTime,
                ExecutionDuration = x.ExecutionDuration,
                UserName = x.UserName,
                HttpMethod = x.HttpMethod,
                Url = x.Url,
                HttpStatusCode = x.HttpStatusCode,
                ClientIpAddress = x.ClientIpAddress,
                HasException = !string.IsNullOrEmpty(x.Exceptions),
                Exceptions = x.Exceptions,
                BrowserInfo = x.BrowserInfo,
                CorrelationId = x.CorrelationId,
                Comments = x.Comments,
                IsSystem = IsSystemOperation(x.Url)
            }).ToList();

            // 如果选中了某个审计日志，重新加载其操作
            if (selectedAuditLogId.HasValue && auditLogs.Any(x => x.Id == selectedAuditLogId.Value))
            {
                await LoadAuditLogActions(selectedAuditLogId.Value);
            }
            else
            {
                selectedAuditLogId = null;
                selectedAuditLogActions = null;
            }

            await uow.CompleteAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载审计日志列表失败");
            auditLogs = new List<AuditLogDto>();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// 加载审计日志的操作详情
    /// </summary>
    private async Task LoadAuditLogActions(Guid auditLogId)
    {
        try
        {
            using var uow = UnitOfWorkManager.Begin(requiresNew: true);
            var dbContext = await DbContextProvider.GetDbContextAsync();

            var actions = await dbContext.Set<Volo.Abp.AuditLogging.AuditLogAction>()
                .AsNoTracking()
                .Where(x => x.AuditLogId == auditLogId)
                // 过滤掉包含 Volo.Abp 的操作（框架相关的操作）
                .Where(x => x.ServiceName == null || !x.ServiceName.Contains("Volo.Abp"))
                .OrderBy(x => x.ExecutionTime)
                .ToListAsync();

            selectedAuditLogActions = actions.Select(x => new AuditLogActionDto
            {
                Id = x.Id,
                ServiceName = x.ServiceName,
                MethodName = x.MethodName,
                ExecutionTime = x.ExecutionTime,
                ExecutionDuration = x.ExecutionDuration,
                Parameters = x.Parameters,
                IsSystem = IsSystemOperation(x.ServiceName)
            }).ToList();

            await uow.CompleteAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载审计日志操作详情失败");
            selectedAuditLogActions = new List<AuditLogActionDto>();
        }
        finally
        {
            StateHasChanged();
        }
    }

    /// <summary>
    /// 处理行点击事件
    /// </summary>
    private async Task OnRowClick(DataGridRowClickEventArgs<AuditLogDto> args)
    {
        await SelectAuditLog(args.Item);
    }

    /// <summary>
    /// 选择审计日志，显示操作详情
    /// </summary>
    private async Task SelectAuditLog(AuditLogDto auditLog)
    {
        if (selectedAuditLogId == auditLog.Id)
        {
            // 如果已经选中，则取消选中
            selectedAuditLogId = null;
            selectedAuditLogActions = null;
        }
        else
        {
            selectedAuditLogId = auditLog.Id;
            await LoadAuditLogActions(auditLog.Id);
        }
        StateHasChanged();
    }

    /// <summary>
    /// 重置过滤条件
    /// </summary>
    private async Task ResetFilters()
    {
        endDate = DateTime.Now;
        startDate = DateTime.Now.AddDays(-7);
        userName = null;
        httpMethod = null;
        httpStatusCode = null;
        urlKeyword = null;
        clientIpAddress = null;
        onlyExceptions = false;
        selectedAuditLogId = null;
        selectedAuditLogActions = null;
        currentPage = 1;
        await LoadAuditLogs();
    }

    /// <summary>
    /// 切换页码
    /// </summary>
    private async Task ChangePage(int page)
    {
        if (page >= 1 && page <= totalPages)
        {
            currentPage = page;
            await LoadAuditLogs();
        }
    }

    /// <summary>
    /// 显示审计日志详情对话框
    /// </summary>
    private async Task ShowAuditLogDetail(AuditLogDto auditLog)
    {
        // 加载完整的审计日志信息
        using var uow = UnitOfWorkManager.Begin(requiresNew: true);
        var dbContext = await DbContextProvider.GetDbContextAsync();

        var fullAuditLog = await dbContext.Set<Volo.Abp.AuditLogging.AuditLog>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == auditLog.Id);

        if (fullAuditLog == null)
        {
            return;
        }

        // 加载所有操作（过滤掉 Volo.Abp 相关的操作）
        var actions = await dbContext.Set<Volo.Abp.AuditLogging.AuditLogAction>()
            .AsNoTracking()
            .Where(x => x.AuditLogId == auditLog.Id)
            .Where(x => x.ServiceName == null || !x.ServiceName.Contains("Volo.Abp"))
            .OrderBy(x => x.ExecutionTime)
            .ToListAsync();

        await uow.CompleteAsync();

        var parameters = new DialogParameters
        {
            ["AuditLog"] = fullAuditLog,
            ["Actions"] = actions
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraLarge,
            FullWidth = true
        };

        await DialogService.ShowAsync<AuditLogDetailDialog>("审计日志详情", parameters, options);
    }

    /// <summary>
    /// 获取HTTP状态码的颜色
    /// </summary>
    private Color GetStatusCodeColor(int? statusCode)
    {
        if (!statusCode.HasValue)
            return Color.Default;

        return statusCode.Value switch
        {
            >= 200 and < 300 => Color.Success,  // 2xx
            >= 300 and < 400 => Color.Info,     // 3xx
            >= 400 and < 500 => Color.Warning,  // 4xx
            >= 500 => Color.Error,              // 5xx
            _ => Color.Default
        };
    }

    /// <summary>
    /// 判断是否为系统操作
    /// </summary>
    private bool IsSystemOperation(string? serviceNameOrUrl)
    {
        if (string.IsNullOrEmpty(serviceNameOrUrl))
            return false;

        // 判断是否为系统级别的操作
        // 可以根据需要扩展判断逻辑
        var systemKeywords = new[] { "System", "Microsoft", "Internal", "Framework" };
        return systemKeywords.Any(keyword =>
            serviceNameOrUrl.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 格式化JSON字符串
    /// </summary>
    private string FormatJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return string.Empty;

        try
        {
            var jsonObj = System.Text.Json.JsonSerializer.Deserialize<object>(json);
            return System.Text.Json.JsonSerializer.Serialize(jsonObj, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// 页面卸载时清理
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
}

/// <summary>
/// 审计日志DTO
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public DateTime ExecutionTime { get; set; }
    public int ExecutionDuration { get; set; }
    public string? UserName { get; set; }
    public string? HttpMethod { get; set; }
    public string? Url { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ClientIpAddress { get; set; }
    public bool HasException { get; set; }
    public string? Exceptions { get; set; }
    public string? BrowserInfo { get; set; }
    public string? CorrelationId { get; set; }
    public string? Comments { get; set; }
    public bool IsSystem { get; set; }
}

/// <summary>
/// 审计日志操作DTO
/// </summary>
public class AuditLogActionDto
{
    public Guid Id { get; set; }
    public string? ServiceName { get; set; }
    public string? MethodName { get; set; }
    public DateTime ExecutionTime { get; set; }
    public int ExecutionDuration { get; set; }
    public string? Parameters { get; set; }
    public bool IsSystem { get; set; }
}

