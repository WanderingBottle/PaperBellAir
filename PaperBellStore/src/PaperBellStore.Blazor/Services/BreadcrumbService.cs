using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Volo.Abp.UI.Navigation;

namespace PaperBellStore.Blazor.Services;

/// <summary>
/// 面包屑服务，用于管理页面面包屑导航
/// </summary>
public class BreadcrumbService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigation;
    private readonly IMenuManager _menuManager;
    private string? _breadcrumbId;
    private string? _topbarBreadcrumbId;
    private string? _currentPagePath;
    private Action<LocationChangedEventArgs>? _locationChangedHandler;

    public BreadcrumbService(
        IJSRuntime jsRuntime,
        NavigationManager navigation,
        IMenuManager menuManager)
    {
        _jsRuntime = jsRuntime;
        _navigation = navigation;
        _menuManager = menuManager;
    }

    /// <summary>
    /// 初始化面包屑
    /// </summary>
    /// <param name="breadcrumbId">页面中面包屑元素的ID</param>
    /// <param name="currentPagePath">当前页面路径（用于判断是否离开页面）</param>
    /// <param name="menuItems">菜单项路径数组，例如：["Home", "ProjectManagement", "ProjectManagement.Projects"]</param>
    /// <param name="onLocationChanged">位置变化时的回调</param>
    public async Task InitializeAsync(
        string breadcrumbId,
        string currentPagePath,
        string[] menuItems,
        Action<LocationChangedEventArgs>? onLocationChanged = null)
    {
        _breadcrumbId = breadcrumbId;
        _topbarBreadcrumbId = $"{breadcrumbId}-topbar";
        _currentPagePath = currentPagePath;
        _locationChangedHandler = onLocationChanged;

        // 订阅导航事件
        _navigation.LocationChanged += OnLocationChanged;
    }

    /// <summary>
    /// 将面包屑移动到顶栏
    /// </summary>
    public async Task MoveToTopBarAsync()
    {
        if (string.IsNullOrEmpty(_breadcrumbId))
        {
            return;
        }

        // 等待 DOM 完全渲染
        await Task.Delay(200);

        await _jsRuntime.InvokeVoidAsync("eval", $@"
(function() {{
try {{
const breadcrumb = document.getElementById('{_breadcrumbId}');
if (!breadcrumb) {{
return;
}}

// 查找顶栏容器
let topBar = null;
const wrapper = document.querySelector('.lpx-wrapper') || document.querySelector('[class*=""lpx-wrapper""]');
if (wrapper) {{
topBar = wrapper.querySelector('.lpx-header') ||
wrapper.querySelector('[class*=""lpx-header""]') ||
wrapper.querySelector('.lpx-topbar') ||
wrapper.querySelector('[class*=""header""]');
}}

if (!topBar) {{
topBar = document.querySelector('.lpx-header') ||
document.querySelector('.lpx-topbar') ||
document.querySelector('.lpx-navbar') ||
document.querySelector('[class*=""lpx-header""]') ||
document.querySelector('.navbar') ||
document.querySelector('.header');
}}

if (topBar) {{
// 清理所有自定义面包屑（以 -topbar 结尾的ID），确保不会留下旧页面的面包屑
const allBreadcrumbs = topBar.querySelectorAll('.lpx-breadcrumb');
allBreadcrumbs.forEach(bc => {{
const id = bc.id;
if (id && id.endsWith('-topbar')) {{
bc.remove();
}}
}});

// 克隆面包屑元素（深度克隆以包含所有子元素）
const clonedBreadcrumb = breadcrumb.cloneNode(true);
clonedBreadcrumb.id = '{_topbarBreadcrumbId}';

// 移除克隆元素和所有子元素的内联样式，确保使用原生CSS样式
clonedBreadcrumb.removeAttribute('style');
const allItems = clonedBreadcrumb.querySelectorAll('*');
allItems.forEach(item => {{
item.removeAttribute('style');
}});

// 查找原生面包屑的位置，插入到相同位置
const nativeBreadcrumb = topBar.querySelector('.lpx-breadcrumb');

if (nativeBreadcrumb && nativeBreadcrumb.id !== '{_topbarBreadcrumbId}') {{
// 如果存在原生面包屑，插入到它之前
nativeBreadcrumb.parentNode.insertBefore(clonedBreadcrumb, nativeBreadcrumb);
}} else {{
// 如果没有原生面包屑，查找合适的插入位置
const topBarInner = topBar.querySelector('.lpx-header-inner') ||
topBar.querySelector('[class*=""header-inner""]') ||
topBar.querySelector('.navbar-collapse');

const logo = topBar.querySelector('.lpx-logo') ||
topBar.querySelector('[class*=""logo""]') ||
topBar.querySelector('.navbar-brand');

const nav = topBar.querySelector('.lpx-nav') ||
topBar.querySelector('[class*=""nav""]') ||
topBar.querySelector('.navbar-nav');

if (topBarInner) {{
const insertTarget = topBarInner.firstChild;
if (insertTarget) {{
topBarInner.insertBefore(clonedBreadcrumb, insertTarget);
}} else {{
topBarInner.appendChild(clonedBreadcrumb);
}}
}} else if (logo && logo.parentElement === topBar) {{
topBar.insertBefore(clonedBreadcrumb, logo.nextSibling);
}} else if (nav && nav.parentElement === topBar) {{
topBar.insertBefore(clonedBreadcrumb, nav);
}} else {{
topBar.insertBefore(clonedBreadcrumb, topBar.firstChild);
}}
}}

// 隐藏原始面包屑
breadcrumb.style.display = 'none';
}}
}} catch (error) {{
console.error('Error moving breadcrumb to top bar:', error);
}}
}})();
");
    }

    /// <summary>
    /// 移除顶栏中的面包屑
    /// </summary>
    public async Task RemoveFromTopBarAsync()
    {
        if (string.IsNullOrEmpty(_topbarBreadcrumbId))
        {
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", $@"
(function() {{
try {{
const breadcrumb = document.getElementById('{_topbarBreadcrumbId}');
if (breadcrumb) {{
breadcrumb.remove();
}}
}} catch (error) {{
console.error('Error removing breadcrumb:', error);
}}
}})();
");
        }
        catch
        {
            // 忽略错误，可能页面已经卸载
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // 如果导航到其他页面，立即移除所有自定义面包屑
        if (!string.IsNullOrEmpty(_currentPagePath) && !e.Location.Contains(_currentPagePath))
        {
            _ = Task.Run(async () =>
            {
                // 立即清理所有自定义面包屑，不等待
                try
                {
                    await _jsRuntime.InvokeVoidAsync("eval", @"
(function() {
try {
// 查找顶栏容器
let topBar = null;
const wrapper = document.querySelector('.lpx-wrapper') || document.querySelector('[class*=""lpx-wrapper""]');
if (wrapper) {
topBar = wrapper.querySelector('.lpx-header') ||
wrapper.querySelector('[class*=""lpx-header""]') ||
wrapper.querySelector('.lpx-topbar') ||
wrapper.querySelector('[class*=""header""]');
}

if (!topBar) {
topBar = document.querySelector('.lpx-header') ||
document.querySelector('.lpx-topbar') ||
document.querySelector('.lpx-navbar') ||
document.querySelector('[class*=""lpx-header""]') ||
document.querySelector('.navbar') ||
document.querySelector('.header');
}

if (topBar) {
// 清理所有自定义面包屑（以 -topbar 结尾的ID）
const allBreadcrumbs = topBar.querySelectorAll('.lpx-breadcrumb');
allBreadcrumbs.forEach(bc => {
const id = bc.id;
if (id && id.endsWith('-topbar')) {
bc.remove();
}
});
}
} catch (error) {
console.error('Error removing breadcrumbs:', error);
}
})();
");
                }
                catch
                {
                    // 忽略错误
                }
            });
        }

        _locationChangedHandler?.Invoke(e);
    }

    public async ValueTask DisposeAsync()
    {
        // 取消订阅导航事件
        _navigation.LocationChanged -= OnLocationChanged;

        // 移除面包屑
        await RemoveFromTopBarAsync();
    }
}

