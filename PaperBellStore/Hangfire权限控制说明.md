# Hangfire Dashboard 精细化权限控制说明

本文档说明如何对 Hangfire Dashboard 中的按钮和操作进行精细化权限控制。

---

## 📋 目录

1. [权限体系](#1-权限体系)
2. [实现原理](#2-实现原理)
3. [权限配置](#3-权限配置)
4. [使用示例](#4-使用示例)
5. [权限说明](#5-权限说明)

---

## 1. 权限体系

### 1.1 权限层级

Hangfire Dashboard 权限采用层级结构：

```
PaperBellStore.HangfireDashboard (父权限)
├── PaperBellStore.HangfireDashboard.View (查看 Dashboard)
├── PaperBellStore.HangfireDashboard.Trigger (立即执行任务)
├── PaperBellStore.HangfireDashboard.Delete (删除任务)
├── PaperBellStore.HangfireDashboard.Create (创建任务)
└── PaperBellStore.HangfireDashboard.Edit (编辑任务)
```

### 1.2 权限说明

| 权限名称           | 权限常量                   | 说明                         | 控制的按钮/操作       |
| ------------------ | -------------------------- | ---------------------------- | --------------------- |
| **查看 Dashboard** | `HangfireDashboardView`    | 基础权限，允许访问 Dashboard | 整个 Dashboard 的访问 |
| **立即执行任务**   | `HangfireDashboardTrigger` | 允许立即触发定时任务         | "立即执行" 按钮       |
| **删除任务**       | `HangfireDashboardDelete`  | 允许删除定时任务             | "删除" 按钮           |
| **创建任务**       | `HangfireDashboardCreate`  | 允许创建新任务               | "添加" 按钮           |
| **编辑任务**       | `HangfireDashboardEdit`    | 允许编辑任务                 | "编辑" 按钮           |

---

## 2. 实现原理

### 2.1 三层权限控制

#### 第一层：Dashboard 访问控制

通过 `HangfireAuthorizationFilter` 控制用户是否可以访问 Dashboard：

```csharp
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // 检查是否有查看 Dashboard 的权限
        return permissionChecker.IsGrantedAsync(
            PaperBellStorePermissions.HangfireDashboardView)
            .GetAwaiter().GetResult();
    }
}
```

**效果**：没有 `View` 权限的用户无法访问 Dashboard。

#### 第二层：只读模式控制

通过 `IsReadOnlyFunc` 动态控制 Dashboard 是否为只读模式：

```csharp
public static bool IsReadOnly(DashboardContext context)
{
    // 检查用户是否有任何操作权限
    var hasTrigger = permissionChecker.IsGrantedAsync(
        PaperBellStorePermissions.HangfireDashboardTrigger)
        .GetAwaiter().GetResult();
    var hasDelete = permissionChecker.IsGrantedAsync(
        PaperBellStorePermissions.HangfireDashboardDelete)
        .GetAwaiter().GetResult();
    // ... 其他权限检查

    // 如果没有任何操作权限，则为只读模式
    return !(hasTrigger || hasDelete || hasCreate || hasEdit);
}
```

**效果**：

- 如果用户没有任何操作权限，Dashboard 会进入只读模式
- 只读模式下，**所有操作按钮会被隐藏**（这是 Hangfire 的内置行为）

**注意**：`IsReadOnlyFunc` 是全局控制，无法针对单个按钮进行控制。如果需要更细粒度的前端控制，需要使用 JavaScript（见下文）。

#### 第三层：操作拦截控制

通过 `HangfireOperationAuthorizationMiddleware` 拦截特定操作请求：

```csharp
public class HangfireOperationAuthorizationMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // 拦截立即执行操作
        if (path.Contains("/recurring/trigger"))
        {
            var hasPermission = await permissionChecker.IsGrantedAsync(
                PaperBellStorePermissions.HangfireDashboardTrigger);

            if (!hasPermission)
            {
                context.Response.StatusCode = 403;
                return;
            }
        }
        // ... 其他操作拦截
    }
}
```

**效果**：

- 即使前端按钮显示，后端也会拦截未授权的操作
- 确保安全性，防止绕过前端限制

### 2.2 前端按钮控制（可选）

由于 Hangfire Dashboard 是内置的，无法直接修改其 Razor 页面。如果需要更细粒度的前端控制，可以使用 JavaScript：

**创建文件：`src/PaperBellStore.Blazor/wwwroot/hangfire-permissions.js`**

```javascript
// Hangfire Dashboard 权限控制脚本
(function () {
  // 等待页面加载完成
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }

  function init() {
    // 从服务器获取用户权限（需要通过 API 或页面数据传递）
    // 这里需要根据实际实现获取权限信息
    // 示例：根据权限隐藏按钮
    // 注意：这需要与后端 API 配合，获取当前用户的权限列表
    // 如果用户没有 Trigger 权限，隐藏"立即执行"按钮
    // if (!hasTriggerPermission) {
    //     document.querySelectorAll('[data-action="trigger"]').forEach(btn => {
    //         btn.style.display = 'none';
    //     });
    // }
    // 如果用户没有 Delete 权限，隐藏"删除"按钮
    // if (!hasDeletePermission) {
    //     document.querySelectorAll('[data-action="delete"]').forEach(btn => {
    //         btn.style.display = 'none';
    //     });
    // }
  }
})();
```

**注意**：JavaScript 控制只是用户体验优化，**不能替代后端权限验证**。

---

## 3. 权限配置

### 3.1 为角色分配权限

在 ABP 框架中，可以通过以下方式分配权限：

#### 方式一：通过代码分配（开发环境）

在 `DbMigrator` 或初始化代码中：

```csharp
// 为管理员角色分配所有权限
await _roleManager.SetRolePermissionsAsync(
    "admin",
    new[]
    {
        PaperBellStorePermissions.HangfireDashboardView,
        PaperBellStorePermissions.HangfireDashboardTrigger,
        PaperBellStorePermissions.HangfireDashboardDelete,
        PaperBellStorePermissions.HangfireDashboardCreate,
        PaperBellStorePermissions.HangfireDashboardEdit
    }
);

// 为普通用户角色只分配查看权限
await _roleManager.SetRolePermissionsAsync(
    "user",
    new[]
    {
        PaperBellStorePermissions.HangfireDashboardView
    }
);
```

#### 方式二：通过 UI 分配（生产环境）

1. 登录系统
2. 进入 **Administration → Identity → Roles**
3. 选择角色（如 `admin`）
4. 在权限标签页中，找到 **PaperBellStore** 权限组
5. 勾选相应的权限：
   - ✅ 任务调度中心（父权限）
   - ✅ 查看任务调度中心
   - ✅ 立即执行任务
   - ✅ 删除任务
   - ✅ 创建任务
   - ✅ 编辑任务

### 3.2 权限组合示例

#### 示例 1：只读用户

**权限**：

- ✅ `HangfireDashboardView` - 可以查看 Dashboard

**效果**：

- ✅ 可以访问 Dashboard
- ✅ 可以查看任务列表
- ✅ 可以查看任务详情
- ❌ 无法执行任何操作（按钮隐藏或禁用）

#### 示例 2：操作员

**权限**：

- ✅ `HangfireDashboardView` - 可以查看 Dashboard
- ✅ `HangfireDashboardTrigger` - 可以立即执行任务

**效果**：

- ✅ 可以访问 Dashboard
- ✅ 可以查看任务列表
- ✅ 可以看到并点击"立即执行"按钮
- ❌ 无法看到"删除"按钮
- ❌ 无法创建或编辑任务

#### 示例 3：管理员

**权限**：

- ✅ `HangfireDashboardView` - 可以查看 Dashboard
- ✅ `HangfireDashboardTrigger` - 可以立即执行任务
- ✅ `HangfireDashboardDelete` - 可以删除任务
- ✅ `HangfireDashboardCreate` - 可以创建任务
- ✅ `HangfireDashboardEdit` - 可以编辑任务

**效果**：

- ✅ 可以访问 Dashboard
- ✅ 可以看到并使用所有按钮
- ✅ 可以执行所有操作

---

## 4. 使用示例

### 4.1 场景：限制普通用户只能查看

**需求**：普通用户只能查看任务，不能执行任何操作。

**配置**：

1. **为普通用户角色分配权限**：

   ```
   ✅ PaperBellStore.HangfireDashboard.View
   ```

2. **效果**：
   - 用户可以访问 Dashboard
   - 用户可以查看任务列表和详情
   - 所有操作按钮会被隐藏（只读模式）

### 4.2 场景：允许操作员执行任务但不能删除

**需求**：操作员可以立即执行任务，但不能删除任务。

**配置**：

1. **为操作员角色分配权限**：

   ```
   ✅ PaperBellStore.HangfireDashboard.View
   ✅ PaperBellStore.HangfireDashboard.Trigger
   ```

2. **效果**：
   - 用户可以访问 Dashboard
   - 用户可以看到并点击"立即执行"按钮
   - 用户无法看到"删除"按钮
   - 即使用户尝试直接调用删除 API，也会被拦截（403 错误）

### 4.3 场景：完全控制（管理员）

**需求**：管理员拥有所有权限。

**配置**：

1. **为管理员角色分配所有权限**：

   ```
   ✅ PaperBellStore.HangfireDashboard.View
   ✅ PaperBellStore.HangfireDashboard.Trigger
   ✅ PaperBellStore.HangfireDashboard.Delete
   ✅ PaperBellStore.HangfireDashboard.Create
   ✅ PaperBellStore.HangfireDashboard.Edit
   ```

2. **效果**：
   - 管理员可以访问 Dashboard
   - 管理员可以看到并使用所有按钮
   - 管理员可以执行所有操作

---

## 5. 权限说明

### 5.1 权限继承关系

- **父权限**：`HangfireDashboard`

  - 如果用户有父权限，会自动拥有所有子权限
  - 建议：为管理员角色分配父权限

- **子权限**：`View`、`Trigger`、`Delete`、`Create`、`Edit`
  - 可以单独分配
  - 建议：根据实际需求灵活组合

### 5.2 权限检查流程

```
用户访问 Dashboard
    ↓
检查 HangfireAuthorizationFilter
    ↓
是否有 View 权限？
    ├─ 否 → 拒绝访问（403）
    └─ 是 → 继续
        ↓
检查 IsReadOnlyFunc
    ↓
是否有任何操作权限？
    ├─ 否 → 只读模式（按钮隐藏）
    └─ 是 → 正常模式
        ↓
用户点击按钮
    ↓
检查 HangfireOperationAuthorizationMiddleware
    ↓
是否有对应操作权限？
    ├─ 否 → 拒绝操作（403）
    └─ 是 → 执行操作
```

### 5.3 安全说明

**三层防护**：

1. **前端控制**：通过 `IsReadOnlyFunc` 隐藏按钮

   - 用户体验更好
   - 但不能完全依赖（可能被绕过）

2. **后端拦截**：通过中间件拦截操作请求

   - 确保安全性
   - 即使前端被绕过，后端也会拒绝

3. **权限验证**：使用 ABP 权限系统
   - 统一的权限管理
   - 支持多租户、角色等高级功能

---

## 6. 测试验证

### 6.1 测试只读用户

1. **创建测试用户**：

   - 用户名：`viewer`
   - 角色：`viewer`（只有 `View` 权限）

2. **登录并访问 Dashboard**：

   - 应该能访问 Dashboard
   - 应该能看到任务列表
   - **不应该**看到"立即执行"和"删除"按钮

3. **尝试直接调用 API**（如果知道 API 路径）：
   ```bash
   curl -X POST "https://localhost:44305/hangfire/recurring/trigger/sample-job-daily"
   ```
   - 应该返回 403 错误

### 6.2 测试操作员

1. **创建测试用户**：

   - 用户名：`operator`
   - 角色：`operator`（有 `View` 和 `Trigger` 权限）

2. **登录并访问 Dashboard**：

   - 应该能访问 Dashboard
   - 应该能看到"立即执行"按钮
   - **不应该**看到"删除"按钮

3. **测试立即执行**：

   - 点击"立即执行"按钮
   - 应该能成功执行

4. **测试删除**（即使按钮不可见）：
   ```bash
   curl -X DELETE "https://localhost:44305/hangfire/recurring/delete/sample-job-daily"
   ```
   - 应该返回 403 错误

### 6.3 测试管理员

1. **使用管理员账户登录**：
   - 应该能看到所有按钮
   - 应该能执行所有操作

---

## 7. 自定义扩展

### 7.1 添加更多权限

如果需要添加更多权限（如"重试失败任务"），可以：

1. **在 `PaperBellStorePermissions.cs` 中添加**：

   ```csharp
   public const string HangfireDashboardRetry = HangfireDashboard + ".Retry";
   ```

2. **在 `PaperBellStorePermissionDefinitionProvider.cs` 中定义**：

   ```csharp
   hangfireGroup.AddChild(
       PaperBellStorePermissions.HangfireDashboardRetry,
       L("Permission:HangfireDashboard.Retry")
   );
   ```

3. **在中间件中添加拦截逻辑**：
   ```csharp
   if (path.Contains("/jobs/retry"))
   {
       var hasPermission = await permissionChecker.IsGrantedAsync(
           PaperBellStorePermissions.HangfireDashboardRetry);
       // ...
   }
   ```

### 7.2 自定义按钮显示逻辑

**方案一：使用 IsReadOnlyFunc（推荐）**

`IsReadOnlyFunc` 可以根据用户权限动态控制 Dashboard 是否为只读模式：

- 如果用户有**任何**操作权限（Trigger、Delete、Create、Edit 中的任意一个），Dashboard 为正常模式
- 如果用户**没有任何**操作权限，Dashboard 为只读模式（所有按钮隐藏）

**方案二：使用 JavaScript（高级）**

如果需要更细粒度的前端控制（如：有 Trigger 权限但无 Delete 权限），可以：

1. 创建 API 端点返回当前用户的权限列表
2. 在 Dashboard 页面中注入 JavaScript
3. 根据权限列表动态隐藏特定按钮

**注意**：JavaScript 控制只是用户体验优化，后端拦截才是真正的安全控制。

---

## 8. 注意事项

### 8.1 权限分配建议

1. **最小权限原则**：

   - 只给用户分配必要的权限
   - 普通用户：只有 `View` 权限
   - 操作员：`View` + `Trigger` 权限
   - 管理员：所有权限

2. **权限继承**：
   - 如果分配了父权限，会自动拥有所有子权限
   - 建议管理员使用父权限，普通用户使用子权限

### 8.2 安全建议

1. **不要依赖前端控制**：

   - 前端隐藏按钮只是用户体验优化
   - 后端拦截才是真正的安全控制

2. **定期审查权限**：

   - 定期检查用户权限分配
   - 确保权限分配符合安全策略

3. **日志记录**：
   - 建议记录权限拒绝的操作
   - 便于安全审计

---

## 9. 故障排查

### 问题 1：按钮仍然显示但没有权限

**可能原因**：

- 中间件未正确注册
- 权限检查逻辑有误

**解决方法**：

1. 检查中间件是否在 `UseHangfireDashboard` 之前注册
2. 检查权限分配是否正确
3. 查看浏览器控制台和服务器日志

### 问题 2：有权限但按钮不显示

**可能原因**：

- `IsReadOnlyFunc` 逻辑有误
- 权限检查失败

**解决方法**：

1. 检查 `HangfireReadOnlyFilter.IsReadOnly` 方法
2. 确认用户确实有相应权限
3. 检查权限检查逻辑

### 问题 3：操作被拦截但应该有权限

**可能原因**：

- 中间件路径匹配有误
- 权限常量不匹配

**解决方法**：

1. 检查中间件中的路径匹配逻辑
2. 确认权限常量正确
3. 添加日志输出调试信息

---

## 📚 相关文件

已修改的文件：

1. `src/PaperBellStore.Application.Contracts/Permissions/PaperBellStorePermissions.cs` - 添加权限常量
2. `src/PaperBellStore.Application.Contracts/Permissions/PaperBellStorePermissionDefinitionProvider.cs` - 定义权限
3. `src/PaperBellStore.Blazor/Filters/HangfireAuthorizationFilter.cs` - 更新授权过滤器
4. `src/PaperBellStore.Blazor/Middleware/HangfireOperationAuthorizationMiddleware.cs` - 创建操作授权中间件
5. `src/PaperBellStore.Blazor/PaperBellStoreBlazorModule.cs` - 注册中间件和配置
6. `src/PaperBellStore.Domain.Shared/Localization/PaperBellStore/zh-Hans.json` - 添加中文权限文本
7. `src/PaperBellStore.Domain.Shared/Localization/PaperBellStore/en.json` - 添加英文权限文本

---

## ✅ 总结

已实现 Hangfire Dashboard 的精细化权限控制：

- ✅ 定义了 5 个细粒度权限（View、Trigger、Delete、Create、Edit）
- ✅ 实现了三层权限控制（访问控制、只读模式、操作拦截）
- ✅ 支持根据用户权限动态显示/隐藏按钮
- ✅ 后端拦截确保安全性
- ✅ 使用 ABP 权限系统，支持角色、多租户等

**使用建议**：

- 普通用户：只分配 `View` 权限（只读）
- 操作员：分配 `View` + `Trigger` 权限（可以执行，不能删除）
- 管理员：分配所有权限（完全控制）

---

**最后更新**：2024 年
