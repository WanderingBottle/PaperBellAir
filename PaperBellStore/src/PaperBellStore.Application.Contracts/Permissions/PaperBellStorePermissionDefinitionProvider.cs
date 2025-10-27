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

        //Define your own permissions here. Example:
        //myGroup.AddPermission(PaperBellStorePermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PaperBellStoreResource>(name);
    }
}
