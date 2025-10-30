using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManage.Permissions
{
    /// <summary>
    /// Description:创建权限常量
    /// CreateTime: 2025/10/30 13:50:51
    /// Author: Tang
    /// </summary>
    public static class ProjectManagePermissions
    {
        public const string GroupName = "ProjectManagement";

        public const string Default = GroupName;
        public const string Create = Default+".Create";
        public const string Edit = Default+".Edit";
        public const string Delete = Default+".Delete";
        public const string View = Default+".View";
    }
}
