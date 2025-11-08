using System.Threading.Tasks;
using PaperBellStore.Localization;
using PaperBellStore.Permissions;
using PaperBellStore.MultiTenancy;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.UI.Navigation;
using Volo.Abp.SettingManagement.Blazor.Menus;
using Volo.Abp.TenantManagement.Blazor.Navigation;
using Volo.Abp.Identity.Blazor;

namespace PaperBellStore.Blazor.Menus;

public class PaperBellStoreMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<PaperBellStoreResource>();

        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                PaperBellStoreMenus.Home,
                l["Menu:Home"],
                "/",
                icon: "fas fa-home",
                order: 1
            )
        );

        //Administration
        var administration = context.Menu.GetAdministration();
        administration.Order = 6;

        // 添加日志测试菜单
        var logTestManagement = new ApplicationMenuItem(
            PaperBellStoreMenus.LogTestGroup,
            l["Menu:LogTestGroup"],
            icon: "fas fa-file-alt",
            order: 2
        );

        // 添加日志测试页面作为子菜单项
        logTestManagement.AddItem(new ApplicationMenuItem(
            PaperBellStoreMenus.LogTest,
            l["Menu:LogTest"],
            "/log-test",
            icon: "fas fa-bug"
        ));

        // 将日志测试菜单添加到Administration组下
        administration.AddItem(logTestManagement);

        // 添加 Hangfire Dashboard 菜单项
        administration.AddItem(new ApplicationMenuItem(
            PaperBellStoreMenus.HangfireDashboard,
            l["Menu:HangfireDashboard"],
            "/hangfire",
            icon: "fas fa-tasks",
            order: 4
        ));

        if (MultiTenancyConsts.IsEnabled)
        {
            administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        }
        else
        {
            administration.TryRemoveMenuItem(TenantManagementMenuNames.GroupName);
        }

        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenus.GroupName, 3);

        return Task.CompletedTask;
    }
}
