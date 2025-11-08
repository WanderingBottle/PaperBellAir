namespace PaperBellStore.Permissions;

public static class PaperBellStorePermissions
{
    public const string GroupName = "PaperBellStore";

    // Hangfire Dashboard 权限
    public const string HangfireDashboard = GroupName + ".HangfireDashboard";
    public const string HangfireDashboardView = HangfireDashboard + ".View";           // 查看 Dashboard
    public const string HangfireDashboardTrigger = HangfireDashboard + ".Trigger";   // 立即执行任务
    public const string HangfireDashboardDelete = HangfireDashboard + ".Delete";     // 删除任务
    public const string HangfireDashboardCreate = HangfireDashboard + ".Create";     // 创建任务
    public const string HangfireDashboardEdit = HangfireDashboard + ".Edit";         // 编辑任务

    //Add your own permission names. Example:
    //public const string MyPermission1 = GroupName + ".MyPermission1";
}
