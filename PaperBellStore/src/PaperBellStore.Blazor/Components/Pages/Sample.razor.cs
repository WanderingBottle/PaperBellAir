using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using System;

namespace PaperBellStore.Blazor.Components.Pages;

public partial class Sample : IAsyncDisposable
{
    // IJSRuntime已在.razor文件中通过@inject注入，这里不需要重复定义

    /// <summary>
    /// 在页面渲染后，通过JS将面包屑插入到顶栏
    /// 原因：顶栏在布局中（MainLayout），页面组件无法直接修改布局结构
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 等待DOM加载完成
            await Task.Delay(100);

            await JSRuntime.InvokeVoidAsync("eval", @"
                (function() {
                    function insertBreadcrumb() {
                        // 查找顶栏
                        const topbar = document.querySelector('.lpx-topbar') || 
                                     document.querySelector('.lpx-header');
                        
                        if (!topbar) {
                            setTimeout(insertBreadcrumb, 200);
                            return;
                        }

                        // 查找面包屑源容器
                        const sourceBreadcrumb = document.getElementById('sample-breadcrumb-container');
                        if (!sourceBreadcrumb) return;

                        // 如果已存在，先移除
                        const existing = document.getElementById('breadcrumb-in-topbar');
                        if (existing) existing.remove();

                        // 查找插入位置：汉堡菜单和右侧菜单之间
                        const hamburger = topbar.querySelector('button[aria-label*=""menu""]') || 
                                         topbar.querySelector('.lpx-navbar-toggle');
                        const rightMenu = topbar.querySelector('.lpx-navbar-end') ||
                                         topbar.querySelector('[class*=""navbar-end""]');

                        // 克隆面包屑内容
                        const breadcrumbContent = sourceBreadcrumb.cloneNode(true);
                        breadcrumbContent.id = 'breadcrumb-in-topbar';
                        breadcrumbContent.style.display = 'block';
                        const breadcrumbOl = breadcrumbContent.querySelector('.lpx-breadcrumb');

                        if (hamburger && rightMenu && hamburger.parentElement === rightMenu.parentElement) {
                            // 在汉堡菜单和右侧菜单之间插入
                            const wrapper = document.createElement('div');
                            wrapper.style.cssText = 'display: flex; align-items: center; flex: 1; margin: 0 20px;';
                            wrapper.appendChild(breadcrumbOl);
                            hamburger.parentNode.insertBefore(wrapper, rightMenu);
                        } else {
                            // 备用方案：插入到顶栏中间
                            topbar.insertBefore(breadcrumbOl, topbar.childNodes[Math.floor(topbar.childNodes.length / 2)]);
                        }
                    }
                    
                    insertBreadcrumb();
                    
                    // 监听DOM变化（顶栏可能是动态加载的）
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
    }

    /// <summary>
    /// 页面卸载时清理插入的面包屑
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("eval", @"
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