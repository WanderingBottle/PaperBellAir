using ProjectManage.Localization;

using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace ProjectManage.Permissions
{
    /// <summary>
    /// Description:创建权限定义提供程序
    /// CreateTime: 2025/10/30 13:51:32
    /// Author: Tang
    /// </summary>
    public class ProjectManagePermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            var group = context.AddGroup(
                ProjectManagePermissions.GroupName ,
                L("Permission:ProjectManagement"));

            group.AddPermission(
                ProjectManagePermissions.Default ,
                L("Permission:ProjectManagement"));

            group.AddPermission(
                ProjectManagePermissions.Create ,
                L("Permission:ProjectManagement.Create"));

            group.AddPermission(
                ProjectManagePermissions.Edit ,
                L("Permission:ProjectManagement.Edit"));

            group.AddPermission(
                ProjectManagePermissions.Delete ,
                L("Permission:ProjectManagement.Delete"));

            group.AddPermission(
                ProjectManagePermissions.View ,
                L("Permission:ProjectManagement.View"));
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<ProjectManageResource>(name);
        }
    }
}
