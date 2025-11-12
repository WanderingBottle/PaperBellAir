using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Blazorise;
using Blazorise.DataGrid;

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
    protected IModalService ModalService { get; set; } = default!;

    protected override string BreadcrumbId => "log-test-breadcrumb-container";
    protected override string CurrentPagePath => "/running-log";
    protected override string[] MenuItemPaths => new[] { PaperBellStoreMenus.Home, PaperBellStoreMenus.RunningLogGroup,
                PaperBellStoreMenus.RunningLog };

    // 日志列表
    private int totalCount = 0;
    private bool isLoading = false;
    private List<AppLog>? logItems; // 存储当前页的数据
    protected DataGrid<AppLog>? dataGrid; // DataGrid 组件引用

    // 分页状态（用于自定义分页器）
    private int currentPageIndex = 0;
    private int currentPageSize = 20;

    // 刷新按钮倒计时
    private int refreshCooldownSeconds = 0;
    private const int RefreshCooldownDuration = 3; // 3秒冷却时间
    private CancellationTokenSource? refreshCooldownCts;

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
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // 先调用基类方法，让 BreadcrumbService 处理面包屑移动
        await base.OnAfterRenderAsync(firstRender);

        // 如果需要额外的处理，可以在这里添加
        // 注意：基类已经调用了 BreadcrumbService.MoveToTopBarAsync()

        // 本地化分页器文本（每次渲染后都执行，因为分页器可能在数据加载后才显示）
        await LocalizePager();
    }

    /// <summary>
    /// 本地化分页器文本
    /// </summary>
    private async Task LocalizePager()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("localizeDataGridPager");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "本地化分页器文本失败: {Message}", ex.Message);
            // 如果 JS 函数不存在，使用 eval 方式
            try
            {
                await JSRuntime.InvokeVoidAsync("eval", @"
                    (function() {
                        function localizeText() {
                            var pager = document.querySelector('.mud-data-grid-pager');
                            if (!pager) return;
                            
                            // 替换 'Rows per page' 文本
                            var allElements = pager.querySelectorAll('*');
                            allElements.forEach(function(el) {
                                var text = el.textContent || el.innerText || '';
                                if (text.includes('Rows per page')) {
                                    el.textContent = text.replace('Rows per page', '每页行数');
                                }
                                // 替换 '1-10 of 100' 格式为 '1-10 共 100 条'
                                if (text.match(/\d+-\d+\s+of\s+\d+/)) {
                                    el.textContent = text.replace(/(\d+)-(\d+)\s+of\s+(\d+)/, '$1-$2 共 $3 条');
                                }
                            });
                        }
                        localizeText();
                        // 使用 MutationObserver 监听 DOM 变化
                        var observer = new MutationObserver(localizeText);
                        var pager = document.querySelector('.mud-data-grid-pager');
                        if (pager) {
                            observer.observe(pager, { childList: true, subtree: true, characterData: true });
                        }
                    })();
                ");
            }
            catch (Exception ex2)
            {
                Logger.LogWarning(ex2, "使用 eval 方式本地化分页器文本也失败");
            }
        }
    }

    /// <summary>
    /// 服务器端数据加载（用于 Blazorise DataGrid）
    /// </summary>
    private async Task OnReadData(DataGridReadDataEventArgs<AppLog> e)
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            // 获取分页信息
            var page = e.Page;
            var pageSize = e.PageSize;

            // 同步分页状态
            currentPageIndex = page;
            currentPageSize = pageSize;

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

            // 应用排序
            if (e.Columns != null && e.Columns.Any(c => c.SortDirection != SortDirection.Default))
            {
                var sortColumn = e.Columns.FirstOrDefault(c => c.SortDirection != SortDirection.Default);
                if (sortColumn != null)
                {
                    query = sortColumn.Field switch
                    {
                        nameof(AppLog.Timestamp) => sortColumn.SortDirection == SortDirection.Descending
                            ? query.OrderByDescending(x => x.Timestamp)
                            : query.OrderBy(x => x.Timestamp),
                        nameof(AppLog.Level) => sortColumn.SortDirection == SortDirection.Descending
                            ? query.OrderByDescending(x => x.Level)
                            : query.OrderBy(x => x.Level),
                        nameof(AppLog.OccurrenceCount) => sortColumn.SortDirection == SortDirection.Descending
                            ? query.OrderByDescending(x => x.OccurrenceCount)
                            : query.OrderBy(x => x.OccurrenceCount),
                        _ => query.OrderByDescending(x => x.Timestamp) // 默认按时间倒序
                    };
                }
                else
                {
                    // 默认按时间倒序
                    query = query.OrderByDescending(x => x.Timestamp);
                }
            }
            else
            {
                // 默认按时间倒序
                query = query.OrderByDescending(x => x.Timestamp);
            }

            // 分页查询
            var items = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await uow.CompleteAsync();

            // 存储数据到组件状态
            logItems = items;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载日志列表失败");
            logItems = new List<AppLog>();
            totalCount = 0;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// 加载日志列表（用于搜索按钮）
    /// </summary>
    private async Task LoadLogs()
    {
        // 如果正在冷却中，直接返回
        if (refreshCooldownSeconds > 0)
        {
            return;
        }

        // 启动冷却倒计时
        StartRefreshCooldown();

        // 使用 Reload 方法刷新数据，保留分页状态
        if (dataGrid != null)
        {
            await dataGrid.Reload();
        }
        else
        {
            await InvokeAsync(StateHasChanged);
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
        await LoadLogs();
    }

    /// <summary>
    /// 处理页码变化
    /// </summary>
    private async Task OnPageChanged(int page)
    {
        // DataGrid 会自动处理分页，我们只需要触发重新加载
        if (dataGrid != null)
        {
            await dataGrid.Reload();
        }
    }

    /// <summary>
    /// 处理每页大小变化
    /// </summary>
    private async Task OnPageSizeChanged(int pageSize)
    {
        // DataGrid 会自动处理每页大小变化
        if (dataGrid != null)
        {
            await dataGrid.Reload();
        }
    }

    /// <summary>
    /// 显示日志详情
    /// </summary>
    private async Task ShowLogDetail(AppLog log)
    {
        await ModalService.Show<LogDetailDialog>(builder =>
        {
            builder.Add(x => x.Log, log);
            builder.Add(x => x.Size, ModalSize.ExtraLarge);
        });
    }

    /// <summary>
    /// 获取日志级别的颜色
    /// </summary>
    private Blazorise.Color GetLevelColor(string? level)
    {
        return level switch
        {
            "Verbose" => Blazorise.Color.Secondary,
            "Debug" => Blazorise.Color.Info,
            "Information" => Blazorise.Color.Primary,
            "Warning" => Blazorise.Color.Warning,
            "Error" => Blazorise.Color.Danger,
            "Fatal" => Blazorise.Color.Dark,
            _ => Blazorise.Color.Secondary
        };
    }

    /// <summary>
    /// 启动刷新按钮冷却倒计时
    /// </summary>
    private void StartRefreshCooldown()
    {
        // 取消之前的倒计时
        if (refreshCooldownCts != null)
        {
            refreshCooldownCts.Cancel();
            refreshCooldownCts.Dispose();
        }

        refreshCooldownCts = new CancellationTokenSource();
        refreshCooldownSeconds = RefreshCooldownDuration;

        // 启动倒计时任务
        _ = Task.Run(async () =>
        {
            try
            {
                while (refreshCooldownSeconds > 0 && !refreshCooldownCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, refreshCooldownCts.Token);
                    if (!refreshCooldownCts.Token.IsCancellationRequested)
                    {
                        refreshCooldownSeconds--;
                        // 在 UI 线程上更新状态
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 倒计时被取消，正常情况
            }
            finally
            {
                if (!refreshCooldownCts.Token.IsCancellationRequested)
                {
                    refreshCooldownSeconds = 0;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }, refreshCooldownCts.Token);
    }

    /// <summary>
    /// 获取刷新按钮文本
    /// </summary>
    private string GetRefreshButtonText()
    {
        if (refreshCooldownSeconds > 0)
        {
            return $"刷新 ({refreshCooldownSeconds})";
        }
        return "刷新";
    }

    /// <summary>
    /// 页面卸载时清理
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        // 取消并释放倒计时
        if (refreshCooldownCts != null)
        {
            refreshCooldownCts.Cancel();
            refreshCooldownCts.Dispose();
            refreshCooldownCts = null;
        }

        // 调用基类方法，让 BreadcrumbService 清理面包屑
        await base.DisposeAsync();
    }
}

