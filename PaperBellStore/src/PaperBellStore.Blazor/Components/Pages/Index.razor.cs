using Microsoft.AspNetCore.Components;

namespace PaperBellStore.Blazor.Components.Pages;


public partial class Index
{
    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        // 如果用户未登录,直接重定向到登录页面
        if (!CurrentUser.IsAuthenticated)
        {
            Navigation.NavigateTo("/Account/Login", forceLoad: true);
        }
        base.OnInitialized();
    }

    private void Login()
    {
        Navigation.NavigateTo("/Account/Login", true);
    }
}
