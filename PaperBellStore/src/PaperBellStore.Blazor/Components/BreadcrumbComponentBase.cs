using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Volo.Abp.UI.Navigation;
using PaperBellStore.Blazor.Services;

namespace PaperBellStore.Blazor.Components;

/// <summary>
/// 带面包屑功能的组件基类
/// </summary>
public abstract class BreadcrumbComponentBase : PaperBellStoreComponentBase, IAsyncDisposable
{
    [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected IMenuManager MenuManager { get; set; } = null!;
    [Inject] protected BreadcrumbService BreadcrumbService { get; set; } = null!;

    protected ApplicationMenuItem? HomeMenu { get; private set; }
    protected ApplicationMenuItem? ParentMenu { get; private set; }
    protected ApplicationMenuItem? CurrentMenu { get; private set; }

    /// <summary>
    /// 获取面包屑ID（子类需要实现）
    /// </summary>
    protected abstract string BreadcrumbId { get; }

    /// <summary>
    /// 获取当前页面路径（用于判断是否离开页面）
    /// </summary>
    protected abstract string CurrentPagePath { get; }

    /// <summary>
    /// 获取菜单项路径数组，例如：["Home", "ProjectManagement", "ProjectManagement.Projects"]
    /// </summary>
    protected abstract string[] MenuItemPaths { get; }

    protected override async Task OnInitializedAsync()
    {
        // 从菜单配置中自动获取菜单项信息
        var menu = await MenuManager.GetAsync(StandardMenus.Main);

        // 获取首页菜单
        HomeMenu = menu.Items.FirstOrDefault(x => x.Name == MenuItemPaths[0]);

        // 获取父菜单
        if (MenuItemPaths.Length > 1)
        {
            ParentMenu = menu.Items.FirstOrDefault(x => x.Name == MenuItemPaths[1]);
        }

        // 获取当前菜单
        if (MenuItemPaths.Length > 2)
        {
            if (ParentMenu != null)
            {
                CurrentMenu = ParentMenu.Items.FirstOrDefault(x => x.Name == MenuItemPaths[2]);
            }
        }
        else if (MenuItemPaths.Length == 2)
        {
            CurrentMenu = ParentMenu;
        }

        // 初始化面包屑服务
        await BreadcrumbService.InitializeAsync(
            BreadcrumbId,
            CurrentPagePath,
            MenuItemPaths);

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 将面包屑移动到顶栏
            await BreadcrumbService.MoveToTopBarAsync();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (BreadcrumbService != null)
        {
            await BreadcrumbService.DisposeAsync();
        }
    }
}

