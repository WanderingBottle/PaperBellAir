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

        // 将示例菜单添加到Administration组下，以生成面包屑导航
        // 注意：ABP框架的面包屑通常需要菜单项位于Administration组下才会显示
        var sampleManagement = new ApplicationMenuItem(
            PaperBellStoreMenus.SampleGroup,
            l["Menu:SampleGroup"],
            icon: "fas fa-folder",
            order: 1
        );

        // 添加示例页面作为子菜单项
        sampleManagement.AddItem(new ApplicationMenuItem(
            PaperBellStoreMenus.Sample,
            l["Menu:Sample"],
            "/sample",
            icon: "fas fa-file-alt"
        ));

        // 将示例菜单添加到Administration组下，这样层级为：管理 > 示例管理 > 示例页面
        administration.AddItem(sampleManagement);

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
