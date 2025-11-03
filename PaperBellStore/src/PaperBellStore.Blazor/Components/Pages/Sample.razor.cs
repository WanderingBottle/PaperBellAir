using Microsoft.AspNetCore.Components;
using Volo.Abp.UI.Navigation;
using PaperBellStore.Blazor.Menus;
using PaperBellStore.Localization;
using System.Threading.Tasks;

namespace PaperBellStore.Blazor.Components.Pages;

public partial class Sample
{
    [Inject]
    protected IMenuManager MenuManager { get; set; } = default!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    private async Task NavigateToParentMenu()
    {
        // 导航到示例管理菜单的首页（如果有的话）
        NavigationManager.NavigateTo("/");
    }
}
