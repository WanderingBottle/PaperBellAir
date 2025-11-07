using System.Threading.Tasks;

using PaperBellStore.Localization;
using PaperBellStore.MultiTenancy;

using ProjectManage.Localization;

using Volo.Abp.Identity.Blazor;
using Volo.Abp.SettingManagement.Blazor.Menus;
using Volo.Abp.TenantManagement.Blazor.Navigation;
using Volo.Abp.UI.Navigation;

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
        var projectL = context.GetLocalizer<ProjectManageResource>();

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

        // 创建项目管理父菜单
        var projectManagementMenu = new ApplicationMenuItem(
            "ProjectManagement",
            projectL["Menu:ProjectManagement"],
            null, // 父菜单不设置 URL
            icon: "fas fa-project-diagram",
            order: 2
        );

        // 添加子菜单项：项目列表
        projectManagementMenu.AddItem(
            new ApplicationMenuItem(
                "ProjectManagement.Projects",
                projectL["Menu:ProjectManagement.Projects"],
                "/projects",
                icon: "fas fa-list",
                order: 1
            )
        );

        // 添加子菜单项：负责人列表
        projectManagementMenu.AddItem(
            new ApplicationMenuItem(
                "ProjectManagement.Owners",
                projectL["Menu:ProjectManagement.Owners"],
                "/owners",
                icon: "fas fa-users",
                order: 2
            )
        );

        context.Menu.Items.Insert(1, projectManagementMenu);

        //Administration
        var administration = context.Menu.GetAdministration();
        administration.Order = 6;

        // 添加日志测试菜单
        var logTestManagement = new ApplicationMenuItem(
            PaperBellStoreMenus.LogTestGroup,
            l["Menu:LogTestGroup"],
            icon: "fas fa-file-alt",
            order: 3
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
