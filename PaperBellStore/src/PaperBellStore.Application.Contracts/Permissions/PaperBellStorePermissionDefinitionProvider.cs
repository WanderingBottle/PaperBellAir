using PaperBellStore.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace PaperBellStore.Permissions;

public class PaperBellStorePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(PaperBellStorePermissions.GroupName);

        // Hangfire Dashboard 权限组
        var hangfireGroup = myGroup.AddPermission(
            PaperBellStorePermissions.HangfireDashboard,
            L("Permission:HangfireDashboard")
        );

        // 查看 Dashboard（基础权限）
        hangfireGroup.AddChild(
            PaperBellStorePermissions.HangfireDashboardView,
            L("Permission:HangfireDashboard.View")
        );

        // 立即执行任务
        hangfireGroup.AddChild(
            PaperBellStorePermissions.HangfireDashboardTrigger,
            L("Permission:HangfireDashboard.Trigger")
        );

        // 删除任务
        hangfireGroup.AddChild(
            PaperBellStorePermissions.HangfireDashboardDelete,
            L("Permission:HangfireDashboard.Delete")
        );

        // 创建任务
        hangfireGroup.AddChild(
            PaperBellStorePermissions.HangfireDashboardCreate,
            L("Permission:HangfireDashboard.Create")
        );

        // 编辑任务
        hangfireGroup.AddChild(
            PaperBellStorePermissions.HangfireDashboardEdit,
            L("Permission:HangfireDashboard.Edit")
        );

        //Define your own permissions here. Example:
        //myGroup.AddPermission(PaperBellStorePermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PaperBellStoreResource>(name);
    }
}
