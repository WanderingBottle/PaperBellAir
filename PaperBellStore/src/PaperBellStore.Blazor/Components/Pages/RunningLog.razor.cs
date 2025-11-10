using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

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
    protected IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    protected IUnitOfWorkManager UnitOfWorkManager { get; set; } = default!;

    // 日志列表
    private List<AppLog>? logs;
    private int totalCount = 0;
    private int currentPage = 1;
    private int pageSize = 20;
    private int totalPages => (int)Math.Ceiling((double)totalCount/pageSize);
    private bool isLoading = false;

    // 过滤条件
    private string selectedLevel = "";
    private DateTime? startDate;
    private DateTime? endDate;
    private string searchText = "";

    // 测试写入日志
    private string testLogLevel = "Information";
    private string testLogMessage = "这是一条测试日志消息";
    private bool includeException = false;
    private bool isWriting = false;
    private string writeResult = "";

    // 日志详情
    private AppLog? selectedLog = null;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        // 默认查询最近7天的日志
        endDate=DateTime.Now;
        startDate=DateTime.Now.AddDays(-7);
        await LoadLogs();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(firstRender)
        {
            await Task.Delay(100);
            await InsertBreadcrumb();
        }
    }

    /// <summary>
    /// 加载日志列表
    /// </summary>
    private async Task LoadLogs()
    {
        try
        {
            isLoading=true;
            StateHasChanged();

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

            // 分页查询
            logs=await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((currentPage-1)*pageSize)
                .Take(pageSize)
                .ToListAsync();

            await uow.CompleteAsync();
        }
        catch(Exception ex)
        {
            Logger.LogError(ex , "加载日志列表失败");
            logs=new List<AppLog>();
        }
        finally
        {
            isLoading=false;
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
            isWriting=true;
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
            currentPage=1;
            await LoadLogs();
        }
        catch(Exception ex)
        {
            writeResult=$"日志写入失败: {ex.Message}";
            Logger.LogError(ex , "写入测试日志失败");
        }
        finally
        {
            isWriting=false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// 重置过滤条件
    /// </summary>
    private async Task ResetFilters()
    {
        selectedLevel="";
        endDate=DateTime.Now;
        startDate=DateTime.Now.AddDays(-7);
        searchText="";
        currentPage=1;
        await LoadLogs();
    }

    /// <summary>
    /// 切换页码
    /// </summary>
    private async Task ChangePage(int page)
    {
        if(page>=1&&page<=totalPages)
        {
            currentPage=page;
            await LoadLogs();
        }
    }

    /// <summary>
    /// 显示日志详情
    /// </summary>
    private void ShowLogDetail(AppLog log)
    {
        selectedLog=log;
        StateHasChanged();
    }

    /// <summary>
    /// 关闭日志详情
    /// </summary>
    private void CloseLogDetail()
    {
        selectedLog=null;
        StateHasChanged();
    }

    /// <summary>
    /// 获取日志级别的Badge样式类
    /// </summary>
    private string GetLevelBadgeClass(string? level)
    {
        return level switch
        {
            "Verbose" => "bg-secondary",
            "Debug" => "bg-info",
            "Information" => "bg-primary",
            "Warning" => "bg-warning",
            "Error" => "bg-danger",
            "Fatal" => "bg-dark",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// 插入面包屑到顶栏
    /// </summary>
    private async Task InsertBreadcrumb()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("eval" , @"
                (function() {
                    function insertBreadcrumb() {
                        const topbar = document.querySelector('.lpx-topbar') || 
                                     document.querySelector('.lpx-header');
                        
                        if (!topbar) {
                            setTimeout(insertBreadcrumb, 200);
                            return;
                        }

                        const sourceBreadcrumb = document.getElementById('log-test-breadcrumb-container');
                        if (!sourceBreadcrumb) return;

                        const existing = document.getElementById('breadcrumb-in-topbar');
                        if (existing) existing.remove();

                        const hamburger = topbar.querySelector('button[aria-label*=""menu""]') || 
                                         topbar.querySelector('.lpx-navbar-toggle');
                        const rightMenu = topbar.querySelector('.lpx-navbar-end') ||
                                         topbar.querySelector('[class*=""navbar-end""]');

                        const breadcrumbContent = sourceBreadcrumb.cloneNode(true);
                        breadcrumbContent.id = 'breadcrumb-in-topbar';
                        breadcrumbContent.style.display = 'block';
                        const breadcrumbOl = breadcrumbContent.querySelector('.lpx-breadcrumb');

                        if (hamburger && rightMenu && hamburger.parentElement === rightMenu.parentElement) {
                            const wrapper = document.createElement('div');
                            wrapper.style.cssText = 'display: flex; align-items: center; flex: 1; margin: 0 20px;';
                            wrapper.appendChild(breadcrumbOl);
                            hamburger.parentNode.insertBefore(wrapper, rightMenu);
                        } else {
                            topbar.insertBefore(breadcrumbOl, topbar.childNodes[Math.floor(topbar.childNodes.length / 2)]);
                        }
                    }
                    
                    insertBreadcrumb();
                    
                    const observer = new MutationObserver(function() {
                        if (!document.getElementById('breadcrumb-in-topbar')) {
                            insertBreadcrumb();
                        }
                    });
                    observer.observe(document.body, { childList: true, subtree: true });
                    setTimeout(() => observer.disconnect(), 3000);
                })();
            ");
        }
        catch
        {
            // 忽略错误
        }
    }

    /// <summary>
    /// 页面卸载时清理
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("eval" , @"
                const breadcrumb = document.getElementById('breadcrumb-in-topbar');
                if (breadcrumb) breadcrumb.remove();
            ");
        }
        catch
        {
            // 忽略清理错误
        }
    }
}

