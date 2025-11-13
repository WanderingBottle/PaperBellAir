using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    private List<AppLog>? logItems;
    private int totalCount = 0;
    private bool isLoading = false;
    protected DataGrid<AppLog>? dataGrid; // DataGrid 组件引用

    // 分页大小选项
    //protected int[] pageSizeOptions = new[] { 10, 20, 50, 100 };
    //private int currentPageSize = 20; // 当前分页大小
    private bool isFirstLoad = true; // 标记是否首次加载

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
        endDate=DateTime.Now;
        startDate=DateTime.Now.AddDays(-7);
        StartRefreshCooldown();
        await RefreshLogDataAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // 首次渲染时，触发 DataGrid 加载数据
        if(firstRender&&dataGrid!=null)
        {
            await dataGrid.Reload();
        }
    }

    private async Task RefreshLogDataAsync()
    {
        try
        {

            isLoading=true;
            StateHasChanged();
            // 标记首次加载完成
            if(isFirstLoad)
            {
                isFirstLoad=false;
            }
            // 在 Unit of Work 范围内使用 DbContext
            using var uow = UnitOfWorkManager.Begin(requiresNew: true);
            var dbContext = await LogDbContextProvider.GetDbContextAsync();
            var query = dbContext.AppLogs.AsQueryable();

            // 按级别过滤
            if(!string.IsNullOrEmpty(selectedLevel))
            {
                query=query.Where(x => x.Level==selectedLevel);
            }

            // 按时间范围过滤
            if(startDate.HasValue)
            {
                query=query.Where(x => x.Timestamp>=startDate.Value);
            }
            if(endDate.HasValue)
            {
                query=query.Where(x => x.Timestamp<=endDate.Value.AddDays(1)); // 包含结束日期当天
            }

            // 搜索消息
            if(!string.IsNullOrEmpty(searchText))
            {
                query=query.Where(x => x.Message!=null&&x.Message.Contains(searchText));
            }

            // 获取总数
            totalCount=await query.CountAsync();

            // 默认按时间倒序
            query=query.OrderByDescending(x => x.Timestamp);

            // 分页查询
            var items = await query
                //.Skip(page*pageSize)
                //.Take(pageSize)
                .ToListAsync();

            await uow.CompleteAsync();

            // 存储数据到组件状态
            logItems=items;
        }
        catch(Exception ex)
        {
            Logger.LogError(ex , "加载日志列表失败");
            logItems=new List<AppLog>();
            totalCount=0;
        }
        finally
        {
            isLoading=false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// 加载日志列表（用于搜索按钮）
    /// </summary>
    private async Task LoadLogs()
    {
        // 如果正在冷却中，直接返回
        if(refreshCooldownSeconds>0)
        {
            return;
        }

        // 启动冷却倒计时
        StartRefreshCooldown();

        // 使用 Reload 方法刷新数据，保留分页状态
        if(dataGrid!=null)
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
            writeResult="";
            StateHasChanged();

            // 将 Serilog 的级别名称映射到 .NET 的 LogLevel
            // Serilog 的 Fatal 对应 .NET 的 Critical
            // Serilog 的 Verbose 对应 .NET 的 Trace
            var logLevelName = testLogLevel;
            if(logLevelName=="Fatal")
            {
                logLevelName="Critical";
            }
            else if(logLevelName=="Verbose")
            {
                logLevelName="Trace";
            }

            var logLevel = Enum.Parse<LogLevel>(logLevelName);
            var message = string.IsNullOrWhiteSpace(testLogMessage)
                ? $"测试日志 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                : testLogMessage;

            if(includeException)
            {
                try
                {
                    throw new Exception("这是一条测试异常信息");
                }
                catch(Exception ex)
                {
                    Logger.Log(logLevel , ex , message);
                }
            }
            else
            {
                Logger.Log(logLevel , message);
            }

            writeResult=$"日志写入成功！级别: {testLogLevel}, 消息: {message}";

            // 等待一下让日志写入数据库
            await Task.Delay(500);

            // 刷新日志列表
            await LoadLogs();
        }
        catch(Exception ex)
        {
            writeResult=$"日志写入失败: {ex.Message}";
            Logger.LogError(ex , "写入测试日志失败");
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
        selectedLevel=null;
        endDate=DateTime.Now;
        startDate=DateTime.Now.AddDays(-7);
        searchText="";
        // 重置到第一页
        if(dataGrid!=null)
        {
            await dataGrid.Reload();
        }
        else
        {
            await LoadLogs();
        }
    }

    /// <summary>
    /// 处理行点击事件
    /// </summary>
    private async Task OnRowClick(DataGridRowMouseEventArgs<AppLog> args)
    {
        // 可以在这里添加行点击的处理逻辑，如果需要的话
        await Task.CompletedTask;
    }

    /// <summary>
    /// 显示日志详情
    /// </summary>
    private async Task ShowLogDetail(AppLog log)
    {
        await ModalService.Show<LogDetailDialog>(builder =>
        {
            builder.Add(x => x.Log , log);
            builder.Add(x => x.Size , ModalSize.ExtraLarge);
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
        if(refreshCooldownCts!=null)
        {
            refreshCooldownCts.Cancel();
            refreshCooldownCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        refreshCooldownCts=cts;
        refreshCooldownSeconds=RefreshCooldownDuration;
        _=InvokeAsync(StateHasChanged);

        // 启动倒计时任务
        _=Task.Run(async () =>
        {
            try
            {
                while(refreshCooldownSeconds>0&&!cts.IsCancellationRequested)
                {
                    await Task.Delay(1000 , cts.Token);
                    if(!cts.IsCancellationRequested)
                    {
                        refreshCooldownSeconds--;
                        // 在 UI 线程上更新状态
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
            catch(OperationCanceledException)
            {
                // 倒计时被取消，正常情况
            }
            finally
            {
                if(!cts.IsCancellationRequested)
                {
                    refreshCooldownSeconds=0;
                    await InvokeAsync(StateHasChanged);
                }
                if(ReferenceEquals(refreshCooldownCts , cts))
                {
                    refreshCooldownCts=null;
                }
                cts.Dispose();
            }
        } , cts.Token);
    }

    /// <summary>
    /// 获取刷新按钮文本
    /// </summary>
    private string GetRefreshButtonText()
    {
        if(refreshCooldownSeconds>0)
        {
            return $"刷新 ({refreshCooldownSeconds})";
        }
        return "刷新";
    }

    /// <summary>
    /// 获取截断后的日志消息
    /// </summary>
    private static string GetShortMessage(string? message , int maxLength = 120)
    {
        if(string.IsNullOrWhiteSpace(message))
        {
            return "-";
        }

        return message.Length<=maxLength
            ? message
            : $"{message[..maxLength]}…";
    }

    /// <summary>
    /// 页面卸载时清理
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        // 取消并释放倒计时
        if(refreshCooldownCts!=null)
        {
            refreshCooldownCts.Cancel();
            refreshCooldownCts.Dispose();
            refreshCooldownCts=null;
        }

        // 调用基类方法，让 BreadcrumbService 清理面包屑
        await base.DisposeAsync();
    }
}

