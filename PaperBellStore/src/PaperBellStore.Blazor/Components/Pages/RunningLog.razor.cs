using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using MudBlazor;

using PaperBellStore.Blazor.Menus;
using PaperBellStore.Data;
using PaperBellStore.EntityFrameworkCore;

using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace PaperBellStore.Blazor.Components.Pages;

public partial class RunningLog : IAsyncDisposable
{
    [Inject]
    protected IDbContextProvider<LogDbContext> LogDbContextProvider { get; set; } = default!;

    [Inject]
    protected new ILogger<RunningLog> Logger { get; set; } = default!;

    [Inject]
    protected IUnitOfWorkManager UnitOfWorkManager { get; set; } = default!;

    [Inject]
    protected IDialogService DialogService { get; set; } = default!;

    protected override string BreadcrumbId => "log-test-breadcrumb-container";
    protected override string CurrentPagePath => "/running-log";
    protected override string[] MenuItemPaths => new[] { PaperBellStoreMenus.Home, PaperBellStoreMenus.RunningLogGroup,
                PaperBellStoreMenus.RunningLog };

    // 日志列表
    private List<AppLog>? logs;
    private int totalCount = 0;
    private int currentPage = 1;
    private int pageSize = 20;
    private int totalPages => (int)Math.Ceiling((double)totalCount / pageSize);
    private bool isLoading = false;

    // 过滤条件
    private string? selectedLevel = null;
    private DateTime? startDate;
    private DateTime? endDate;
    private string searchText = "";

    // 测试写入日志
    private string testLogLevel = "Information";
    private string testLogMessage = "这是一条测试日志消息";
    private bool includeException = false;
    private string writeResult = "";


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        // 默认查询最近7天的日志
        endDate = DateTime.Now;
        startDate = DateTime.Now.AddDays(-7);
        await LoadLogs();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // 先调用基类方法，让 BreadcrumbService 处理面包屑移动
        await base.OnAfterRenderAsync(firstRender);

        // 如果需要额外的处理，可以在这里添加
        // 注意：基类已经调用了 BreadcrumbService.MoveToTopBarAsync()
    }

    /// <summary>
    /// 加载日志列表
    /// </summary>
    private async Task LoadLogs()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            // 在 Unit of Work 范围内使用 DbContext
            using var uow = UnitOfWorkManager.Begin(requiresNew: true);
            var dbContext = await LogDbContextProvider.GetDbContextAsync();
            var query = dbContext.AppLogs.AsQueryable();

            // 按级别过滤
            if (!string.IsNullOrEmpty(selectedLevel))
            {
                query = query.Where(x => x.Level == selectedLevel);
            }

            // 按时间范围过滤
            if (startDate.HasValue)
            {
                query = query.Where(x => x.Timestamp >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(x => x.Timestamp <= endDate.Value.AddDays(1)); // 包含结束日期当天
            }

            // 搜索消息
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(x => x.Message != null && x.Message.Contains(searchText));
            }

            // 获取总数
            totalCount = await query.CountAsync();

            // 分页查询
            logs = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await uow.CompleteAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载日志列表失败");
            logs = new List<AppLog>();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// 写入测试日志
    /// </summary>
    private async Task WriteTestLog()
    {
        try
        {
            writeResult = "";
            StateHasChanged();

            // 将 Serilog 的级别名称映射到 .NET 的 LogLevel
            // Serilog 的 Fatal 对应 .NET 的 Critical
            // Serilog 的 Verbose 对应 .NET 的 Trace
            var logLevelName = testLogLevel;
            if (logLevelName == "Fatal")
            {
                logLevelName = "Critical";
            }
            else if (logLevelName == "Verbose")
            {
                logLevelName = "Trace";
            }

            var logLevel = Enum.Parse<LogLevel>(logLevelName);
            var message = string.IsNullOrWhiteSpace(testLogMessage)
                ? $"测试日志 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                : testLogMessage;

            if (includeException)
            {
                try
                {
                    throw new Exception("这是一条测试异常信息");
                }
                catch (Exception ex)
                {
                    Logger.Log(logLevel, ex, message);
                }
            }
            else
            {
                Logger.Log(logLevel, message);
            }

            writeResult = $"日志写入成功！级别: {testLogLevel}, 消息: {message}";

            // 等待一下让日志写入数据库
            await Task.Delay(500);

            // 刷新日志列表
            currentPage = 1;
            await LoadLogs();
        }
        catch (Exception ex)
        {
            writeResult = $"日志写入失败: {ex.Message}";
            Logger.LogError(ex, "写入测试日志失败");
        }
        finally
        {
            StateHasChanged();
        }
    }

    /// <summary>
    /// 重置过滤条件
    /// </summary>
    private async Task ResetFilters()
    {
        selectedLevel = null;
        endDate = DateTime.Now;
        startDate = DateTime.Now.AddDays(-7);
        searchText = "";
        currentPage = 1;
        await LoadLogs();
    }

    /// <summary>
    /// 切换页码
    /// </summary>
    private async Task ChangePage(int page)
    {
        if (page >= 1 && page <= totalPages)
        {
            currentPage = page;
            await LoadLogs();
        }
    }

    /// <summary>
    /// 显示日志详情
    /// </summary>
    private async Task ShowLogDetail(AppLog log)
    {
        var parameters = new DialogParameters
        {
            ["Log"] = log
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Large,
            FullWidth = true
        };

        await DialogService.ShowAsync<LogDetailDialog>("日志详情", parameters, options);
    }

    /// <summary>
    /// 获取日志级别的颜色
    /// </summary>
    private Color GetLevelColor(string? level)
    {
        return level switch
        {
            "Verbose" => Color.Secondary,
            "Debug" => Color.Info,
            "Information" => Color.Primary,
            "Warning" => Color.Warning,
            "Error" => Color.Error,
            "Fatal" => Color.Dark,
            _ => Color.Secondary
        };
    }

    /// <summary>
    /// 页面卸载时清理
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        // 调用基类方法，让 BreadcrumbService 清理面包屑
        await base.DisposeAsync();
    }
}

