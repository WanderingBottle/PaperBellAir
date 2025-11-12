# MudBlazor 到 Blazorise 迁移实施方案

## 📋 项目概述

本项目基于 ABP 框架的 Blazor 应用，使用 LeptonXLite 主题和 Blazorise 组件库。目前项目中同时集成了 MudBlazor 8.0，为了统一组件库，需要将所有 MudBlazor 组件替换为 Blazorise 对应组件。

## 🎯 迁移目标

1. **完全移除 MudBlazor** 相关依赖和引用
2. **保持现有功能不变**，确保用户体验一致
3. **统一使用 Blazorise** 组件库，简化维护

## 📊 组件映射表

### 基础布局组件

| MudBlazor 组件        | Blazorise 组件                 | 说明                       |
| --------------------- | ------------------------------ | -------------------------- |
| `MudContainer`        | `Container`                    | 容器组件                   |
| `MudCard`             | `Card`                         | 卡片组件                   |
| `MudCardContent`      | `CardBody`                     | 卡片内容                   |
| `MudGrid` / `MudItem` | `Row` / `Column`               | 网格布局                   |
| `MudStack`            | `Row` / `Column` + Flex 工具类 | 堆叠布局                   |
| `MudPaper`            | `Card`                         | 纸张效果（可用 Card 替代） |
| `MudDivider`          | `Divider`                      | 分割线                     |

### 文本和图标

| MudBlazor 组件 | Blazorise 组件                                | 说明     |
| -------------- | --------------------------------------------- | -------- |
| `MudText`      | HTML 标签 (`<h1>`, `<p>`, `<span>`) 或 `Text` | 文本组件 |
| `MudIcon`      | `Icon`                                        | 图标组件 |

### 表单组件

| MudBlazor 组件                | Blazorise 组件          | 说明       |
| ----------------------------- | ----------------------- | ---------- |
| `MudButton`                   | `Button`                | 按钮       |
| `MudTextField`                | `TextEdit`              | 文本输入框 |
| `MudSelect` / `MudSelectItem` | `Select` / `SelectItem` | 下拉选择   |
| `MudDatePicker`               | `DateEdit`              | 日期选择器 |
| `MudCheckBox`                 | `Check`                 | 复选框     |

### 数据展示组件

| MudBlazor 组件      | Blazorise 组件      | 说明                 |
| ------------------- | ------------------- | -------------------- |
| `MudDataGrid`       | `DataGrid`          | 数据表格（已在使用） |
| `MudDataGridPager`  | `DataGrid` 内置分页 | 分页器               |
| `MudChip`           | `Badge`             | 标签/徽章            |
| `MudTooltip`        | `Tooltip`           | 工具提示             |
| `MudProgressLinear` | `Progress`          | 进度条               |
| `MudAlert`          | `Alert`             | 警告提示             |
| `MudPagination`     | `Pagination`        | 分页组件             |

### 交互组件

| MudBlazor 组件                             | Blazorise 组件      | 说明          |
| ------------------------------------------ | ------------------- | ------------- |
| `MudDialog`                                | `Modal`             | 对话框/模态框 |
| `MudTabs` / `MudTabPanel`                  | `Tabs` / `TabPanel` | 标签页        |
| `MudExpansionPanels` / `MudExpansionPanel` | `Collapse`          | 折叠面板      |

### 服务接口

| MudBlazor 服务   | Blazorise 服务    | 说明         |
| ---------------- | ----------------- | ------------ |
| `IDialogService` | `IModalService`   | 对话框服务   |
| `ISnackbar`      | `IMessageService` | 消息提示服务 |

## 🔄 详细替换方案

### 1. 配置和依赖移除

#### 1.1 移除 NuGet 包

**文件**: `PaperBellStore.Blazor.csproj`

- 移除 `<PackageReference Include="MudBlazor" Version="8.0.0" />`

#### 1.2 移除服务注册

**文件**: `PaperBellStoreBlazorModule.cs`

- 移除 `using MudBlazor.Services;`
- 移除 `context.Services.AddMudServices();`

#### 1.3 移除引用

**文件**: `_Imports.razor`

- 移除 `@using MudBlazor`

#### 1.4 移除 CSS/JS 引用

**文件**: `App.razor`

- 移除 Google Fonts (Roboto) 引用
- 移除 `_content/MudBlazor/MudBlazor.min.css`
- 移除 `_content/MudBlazor/MudBlazor.min.js`

#### 1.5 移除 Provider 组件

**文件**: `Routes.razor`

- 移除 `<MudThemeProvider />`
- 移除 `<MudDialogProvider />`
- 移除 `<MudPopoverProvider />`

### 2. 组件替换详细说明

#### 2.1 布局组件替换

**MudContainer → Container**

```razor
<!-- MudBlazor -->
<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">
    ...
</MudContainer>

<!-- Blazorise -->
<Container Class="mt-4">
    ...
</Container>
```

**MudCard → Card**

```razor
<!-- MudBlazor -->
<MudCard Elevation="3">
    <MudCardContent>
        ...
    </MudCardContent>
</MudCard>

<!-- Blazorise -->
<Card>
    <CardBody>
        ...
    </CardBody>
</Card>
```

**MudGrid → Row/Column**

```razor
<!-- MudBlazor -->
<MudGrid Spacing="3">
    <MudItem xs="12" sm="6" md="3">
        ...
    </MudItem>
</MudGrid>

<!-- Blazorise -->
<Row>
    <Column ColumnSize="ColumnSize.Is12.OnMobile.Is6.OnTablet.Is3.OnDesktop">
        ...
    </Column>
</Row>
```

**MudStack → Row/Column**

```razor
<!-- MudBlazor -->
<MudStack Row="true" Spacing="2" Justify="Justify.SpaceBetween">
    ...
</MudStack>

<!-- Blazorise -->
<Row JustifyContent="JustifyContent.Between">
    <Column ColumnSize="ColumnSize.IsAuto">
        ...
    </Column>
</Row>
```

#### 2.2 文本和图标替换

**MudText → HTML 标签或 Text 组件**

```razor
<!-- MudBlazor -->
<MudText Typo="Typo.h5">标题</MudText>
<MudText Class="text-center">居中文本</MudText>
<MudText>普通文本</MudText>

<!-- Blazorise -->
<h5>标题</h5>
<!-- 注意：Text 组件不支持 Class 属性，需要使用 HTML 标签 -->
<div class="text-center">居中文本</div>
<!-- 或使用 -->
<Text>普通文本</Text>
```

**重要说明**：

- ✅ **已验证**：Blazorise 的 `Text` 组件不支持 `Class` 属性，会报编译错误 `RZ1023`
- 如果需要添加 CSS 类，使用 HTML 标签（如 `<div>`, `<span>`, `<p>` 等）而不是 `Text` 组件
- `Text` 组件适用于纯文本内容，不需要样式类的情况

**MudIcon → Icon**

```razor
<!-- MudBlazor -->
<MudIcon Icon="@Icons.Material.Filled.Search" Size="@MudBlazor.Size.Small" />

<!-- Blazorise -->
<Icon Name="IconName.Search" Size="IconSize.Small" />
```

#### 2.3 表单组件替换

**MudButton → Button**

```razor
<!-- MudBlazor -->
<MudButton Variant="Variant.Filled" Color="@MudBlazor.Color.Primary"
           StartIcon="@Icons.Material.Filled.Search" OnClick="LoadLogs">
    搜索
</MudButton>

<!-- Blazorise -->
<Button Color="Color.Primary" Clicked="LoadLogs">
    <Icon Name="IconName.Search" />
    搜索
</Button>
```

**MudTextField → TextEdit**

```razor
<!-- MudBlazor -->
<MudTextField @bind-Value="searchText" Label="搜索消息"
              Variant="Variant.Outlined" Placeholder="搜索日志消息..."
              Adornment="Adornment.Start" AdornmentIcon="@Icons.Material.Filled.Search" />

<!-- Blazorise -->
<Field>
    <FieldLabel>搜索消息</FieldLabel>
    <TextEdit @bind-Text="searchText" Placeholder="搜索日志消息...">
        <Feedback>
            <Icon Name="IconName.Search" />
        </Feedback>
    </TextEdit>
</Field>
```

**MudSelect → Select**

```razor
<!-- MudBlazor -->
<MudSelect T="string" @bind-Value="selectedLevel" Label="日志级别"
           Variant="Variant.Outlined" Clearable>
    <MudSelectItem T="string" Value="@((string?)null)">全部</MudSelectItem>
    <MudSelectItem T="string" Value="@("Error")">Error</MudSelectItem>
</MudSelect>

<!-- Blazorise -->
<Field>
    <FieldLabel>日志级别</FieldLabel>
    <Select TValue="string" SelectedValue="selectedLevel"
            SelectedValueChanged="@((string value) => selectedLevel = value)">
        <SelectItem TValue="string" Value="@((string?)null)">全部</SelectItem>
        <SelectItem TValue="string" Value="@("Error")">Error</SelectItem>
    </Select>
</Field>
```

**MudDatePicker → DateEdit（仅日期）**

```razor
<!-- MudBlazor -->
<MudDatePicker @bind-Date="startDate" Label="开始日期"
               Variant="Variant.Outlined" DateFormat="yyyy-MM-dd" />

<!-- Blazorise -->
<Field>
    <FieldLabel>开始日期</FieldLabel>
    <DateEdit @bind-Date="startDate" DisplayFormat="yyyy-MM-dd" />
</Field>
```

**MudDatePicker（带时间）→ DateEdit + TimeEdit（组合方案）**

```razor
<!-- MudBlazor -->
<MudDatePicker @bind-Date="startDateTime" Label="开始时间"
               Variant="Variant.Outlined" DateFormat="yyyy-MM-dd HH:mm"
               TimePicker="true" />

<!-- Blazorise - 方案A：使用两个组件（推荐） -->
<Row>
    <Column ColumnSize="ColumnSize.Is6">
        <Field>
            <FieldLabel>开始日期</FieldLabel>
            <DateEdit @bind-Date="startDate" />
        </Field>
    </Column>
    <Column ColumnSize="ColumnSize.Is6">
        <Field>
            <FieldLabel>开始时间</FieldLabel>
            <TimeEdit @bind-Time="startTime" />
        </Field>
    </Column>
</Row>

@code {
    private DateTime? startDate;
    private TimeSpan? startTime;

    // 合并日期和时间的属性
    private DateTime? StartDateTime
    {
        get => startDate.HasValue && startTime.HasValue
            ? startDate.Value.Date.Add(startTime.Value)
            : null;
        set
        {
            if (value.HasValue)
            {
                startDate = value.Value.Date;
                startTime = value.Value.TimeOfDay;
            }
            else
            {
                startDate = null;
                startTime = null;
            }
        }
    }
}
```

**注意**：Blazorise 的 `DateEdit` 不支持时间选择，如果需要时间选择功能，必须使用 `DateEdit` + `TimeEdit` 组合，或在代码中合并日期和时间。

**MudCheckBox → Check**

```razor
<!-- MudBlazor -->
<MudCheckBox @bind-Value="onlyExceptions" Label="仅显示异常"
             Color="@MudBlazor.Color.Error" />

<!-- Blazorise -->
<Field>
    <Check TValue="bool" @bind-Checked="onlyExceptions" Color="Color.Danger">
        仅显示异常
    </Check>
</Field>
```

#### 2.4 数据展示组件替换

**MudDataGrid → DataGrid**

```razor
<!-- MudBlazor -->
<MudDataGrid T="AppLog" @ref="dataGrid" ServerData="@LoadServerData"
             Hover="true" Striped="true" Dense="true" ReadOnly="true"
             FilterMode="@MudBlazor.DataGridFilterMode.Simple"
             NoDataContent="暂无数据" Loading="@isLoading">
    <Columns>
        <PropertyColumn Property="@(x => x.Timestamp)" Title="时间" />
    </Columns>
</MudDataGrid>

<!-- Blazorise -->
<DataGrid TItem="AppLog" @ref="dataGrid" Data="@logItems" ReadData="@OnReadData"
          Hoverable="true" Striped="true" Responsive="true"
          ShowPager="true" PageSize="20" TotalItems="@totalCount"
          Class="mt-3">
    <EmptyTemplate>
        <Text>暂无数据</Text>
    </EmptyTemplate>
    <DataGridColumns>
        <DataGridColumn TItem="AppLog" Field="@nameof(AppLog.Timestamp)"
                        Caption="时间" Sortable="true"
                        DisplayFormat="yyyy-MM-dd HH:mm:ss" />
        <DataGridColumn TItem="AppLog" Field="@nameof(AppLog.Level)"
                        Caption="级别" Sortable="true">
            <DisplayTemplate>
                <Badge Color="@GetLevelColor(context.Level)">
                    @context.Level
                </Badge>
            </DisplayTemplate>
        </DataGridColumn>
    </DataGridColumns>
</DataGrid>

@code {
    private DataGrid<AppLog>? dataGrid;
    private List<AppLog>? logItems; // 存储数据用于 DataGrid 的 Data 属性
    private bool isLoading = false;
    private int totalCount = 0;

    private async Task OnReadData(DataGridReadDataEventArgs<AppLog> e)
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            // 获取分页信息
            var page = e.Page;
            var pageSize = e.PageSize;

            // 在 Unit of Work 范围内使用 DbContext
            using var uow = UnitOfWorkManager.Begin(requiresNew: true);
            var dbContext = await LogDbContextProvider.GetDbContextAsync();
            var query = dbContext.AppLogs.AsQueryable();

            // 应用过滤条件
            if (!string.IsNullOrEmpty(selectedLevel))
            {
                query = query.Where(x => x.Level == selectedLevel);
            }
            if (startDate.HasValue)
            {
                query = query.Where(x => x.Timestamp >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(x => x.Timestamp <= endDate.Value.AddDays(1));
            }
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(x => x.Message != null && x.Message.Contains(searchText));
            }

            // 获取总数
            totalCount = await query.CountAsync();

            // 应用排序 - 从 e.Columns 获取排序信息
            if (e.Columns != null && e.Columns.Any(c => c.SortDirection != SortDirection.Default))
            {
                var sortColumn = e.Columns.FirstOrDefault(c => c.SortDirection != SortDirection.Default);
                if (sortColumn != null)
                {
                    query = sortColumn.Field switch
                    {
                        nameof(AppLog.Timestamp) => sortColumn.SortDirection == SortDirection.Descending
                            ? query.OrderByDescending(x => x.Timestamp)
                            : query.OrderBy(x => x.Timestamp),
                        nameof(AppLog.Level) => sortColumn.SortDirection == SortDirection.Descending
                            ? query.OrderByDescending(x => x.Level)
                            : query.OrderBy(x => x.Level),
                        _ => query.OrderByDescending(x => x.Timestamp) // 默认按时间倒序
                    };
                }
                else
                {
                    query = query.OrderByDescending(x => x.Timestamp);
                }
            }
            else
            {
                query = query.OrderByDescending(x => x.Timestamp);
            }

            // 分页查询
            var items = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await uow.CompleteAsync();

            // 存储数据到组件状态（用于 DataGrid 的 Data 属性）
            logItems = items;
            totalCount = totalCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载日志列表失败");
            logItems = new List<AppLog>();
            totalCount = 0;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}
```

**重要说明**：

- ✅ **已验证**：Blazorise DataGrid 的 `ReadData` 是一个事件，需要使用 `DataGridReadDataEventArgs<T>` 事件参数
- ✅ **已验证**：需要在组件中维护数据列表（如 `logItems`），并在 DataGrid 上绑定 `Data="@logItems"` 和 `TotalItems="@totalCount"`
- ✅ **已验证**：排序信息从 `e.Columns` 获取，检查每个列的 `SortDirection` 是否为 `SortDirection.Default`
- ✅ **已验证**：使用 `SortDirection.Default` 表示无排序，而不是 `SortDirection.None`（不存在）
- 分页信息通过 `e.Page` 和 `e.PageSize` 获取
- 需要添加 `using Blazorise.DataGrid;` 和 `using Blazorise;` 引用

**MudChip → Badge**

```razor
<!-- MudBlazor -->
<MudChip T="string" Size="@MudBlazor.Size.Small" Color="@MudBlazor.Color.Primary">
    共 @totalCount 条
</MudChip>

<!-- Blazorise -->
<Badge Color="Color.Primary" Pill="true">
    共 @totalCount 条
</Badge>
```

**MudTooltip → Tooltip**

```razor
<!-- MudBlazor -->
<MudTooltip Text="@context.Item.Message">
    <MudText>@context.Item.Message</MudText>
</MudTooltip>

<!-- Blazorise -->
<Tooltip Text="@context.Item.Message">
    <Text>@context.Item.Message</Text>
</Tooltip>
```

**MudProgressLinear → Progress**

```razor
<!-- MudBlazor -->
<MudProgressLinear Color="@MudBlazor.Color.Primary" Indeterminate="true" />

<!-- Blazorise -->
<Progress Color="Color.Primary" Animated="true" />
```

**MudAlert → Alert**

```razor
<!-- MudBlazor -->
<MudAlert Severity="Severity.Info">
    <MudIcon Icon="@Icons.Material.Filled.Info" />
    暂无数据
</MudAlert>

<!-- Blazorise -->
<Alert Color="Color.Info">
    <Icon Name="IconName.Info" />
    暂无数据
</Alert>
```

**MudPagination → Pagination**

```razor
<!-- MudBlazor -->
<MudPagination Count="@totalPages" Selected="@currentPage"
               SelectedChanged="@((int page) => ChangePage(page))" />

<!-- Blazorise -->
<Pagination TotalItems="@totalCount" CurrentPage="@currentPage"
            CurrentPageChanged="@((int page) => ChangePage(page))" />
```

#### 2.5 交互组件替换

**MudDialog → Modal**

```razor
<!-- MudBlazor -->
@inject IDialogService DialogService

private async Task ShowDialog()
{
    var parameters = new DialogParameters { ["Log"] = log };
    var options = new DialogOptions { MaxWidth = MaxWidth.Large };
    await DialogService.ShowAsync<LogDetailDialog>("日志详情", parameters, options);
}

<!-- Blazorise - 正确方式：使用 builder.Add 表达式语法 -->
@inject IModalService ModalService

private async Task ShowLogDetail(AppLog log)
{
    await ModalService.Show<LogDetailDialog>(builder =>
    {
        builder.Add(x => x.Log, log);
    });
}
```

**重要说明**：

- ✅ **已验证**：Blazorise 1.7.6 使用 `builder.Add(x => x.Property, value)` 表达式语法传递参数
- ❌ **错误方式**：不要使用 `AddParameter` 方法（不存在）
- ❌ **错误方式**：不要使用字典方式传参（API 不支持）
- Modal 组件中需要使用 `[Parameter]` 属性接收参数
- 需要添加 `using Blazorise;` 引用

**对话框组件本身**

```razor
<!-- MudBlazor -->
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">日志详情</MudText>
    </TitleContent>
    <DialogContent>
        @if (Log != null)
        {
            <MudText><strong>时间:</strong> @Log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")</MudText>
            <MudText><strong>级别:</strong> @Log.Level</MudText>
            <MudText><strong>消息:</strong> @Log.Message</MudText>
        }
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">关闭</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance? MudDialog { get; set; }
    [Parameter] public AppLog? Log { get; set; }
    private void Cancel() => MudDialog?.Cancel();
}

<!-- Blazorise -->
<ModalContent>
    <ModalHeader>
        <ModalTitle>日志详情</ModalTitle>
    </ModalHeader>
    <ModalBody>
        @if (Log != null)
        {
            <Row>
                <Column ColumnSize="ColumnSize.Is12">
                    <Text><strong>时间:</strong> @Log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")</Text>
                </Column>
                <Column ColumnSize="ColumnSize.Is12">
                    <Text><strong>级别:</strong></Text>
                    <Badge Color="@GetLevelColor(Log.Level)">@Log.Level</Badge>
                </Column>
                <Column ColumnSize="ColumnSize.Is12">
                    <Text><strong>消息:</strong></Text>
                    <Card>
                        <CardBody>
                            <Text>@Log.Message</Text>
                        </CardBody>
                    </Card>
                </Column>
            </Row>
        }
    </ModalBody>
    <ModalFooter>
        <Button Color="Color.Secondary" Clicked="Cancel">关闭</Button>
    </ModalFooter>
</ModalContent>

@code {
    [CascadingParameter] protected ModalInstance ModalInstance { get; set; } = default!;

    [Inject] protected IModalService ModalService { get; set; } = default!;

    [Parameter] public AppLog Log { get; set; } = default!;

    private async Task Cancel()
    {
        await ModalService.Hide(ModalInstance);
    }

    private Color GetLevelColor(string? level)
    {
        return level switch
        {
            "Verbose" => Color.Secondary,
            "Debug" => Color.Info,
            "Information" => Color.Primary,
            "Warning" => Color.Warning,
            "Error" => Color.Danger,
            "Fatal" => Color.Dark,
            _ => Color.Secondary
        };
    }
}
```

**重要说明**：

- ✅ **已验证**：关闭 Modal 需要使用 `await ModalService.Hide(ModalInstance)`，而不是 `ModalInstance.Hide()`
- Modal 组件中需要同时注入 `IModalService` 和接收 `ModalInstance` 作为 CascadingParameter
- 需要添加 `using Blazorise;` 引用

````

**MudTabs → Tabs**

```razor
<!-- MudBlazor -->
<MudTabs Elevation="0" Rounded="true">
    <MudTabPanel Text="基本信息">
        ...
    </MudTabPanel>
    <MudTabPanel Text="HTTP请求信息">
        ...
    </MudTabPanel>
</MudTabs>

<!-- Blazorise -->
<Tabs>
    <Tab Name="basic" Title="基本信息">
        ...
    </Tab>
    <Tab Name="http" Title="HTTP请求信息">
        ...
    </Tab>
</Tabs>
````

**MudExpansionPanels → Collapse**

```razor
<!-- MudBlazor -->
<MudExpansionPanels Elevation="0" MultiExpansion="false">
    <MudExpansionPanel Text="查看参数" Icon="@Icons.Material.Filled.Code">
        ...
    </MudExpansionPanel>
</MudExpansionPanels>

<!-- Blazorise -->
<Collapse>
    <CollapseHeader>
        <Icon Name="IconName.Code" />
        查看参数
    </CollapseHeader>
    <CollapseBody>
        ...
    </CollapseBody>
</Collapse>
```

#### 2.6 服务替换

**ISnackbar → IMessageService（或 IToastService）**

```razor
<!-- MudBlazor -->
@inject ISnackbar Snackbar
Snackbar.Add("操作成功", Severity.Success);
Snackbar.Add("操作失败", Severity.Error);
Snackbar.Add("提示信息", Severity.Info);
Snackbar.Add("警告信息", Severity.Warning);

<!-- Blazorise - 方式1：使用 IMessageService（如果可用） -->
@inject IMessageService MessageService
await MessageService.Success("操作成功");
await MessageService.Error("操作失败");
await MessageService.Info("提示信息");
await MessageService.Warning("警告信息");

<!-- Blazorise - 方式2：使用 IToastService（如果 IMessageService 不可用） -->
@inject IToastService ToastService
await ToastService.Success("操作成功");
await ToastService.Error("操作失败");
await ToastService.Information("提示信息");
await ToastService.Warning("警告信息");
```

**重要说明**：

- 需要确认 Blazorise 1.7.6 版本使用的是 `IMessageService` 还是 `IToastService`
- 建议在项目中搜索或查阅官方文档确认正确的服务接口
- 如果两者都不可用，可能需要使用 ABP 框架提供的消息服务

## 📝 需要修改的文件清单

### 核心配置文件

1. ✅ `PaperBellStore.Blazor.csproj` - 移除 MudBlazor 包引用
2. ✅ `PaperBellStoreBlazorModule.cs` - 移除服务注册
3. ✅ `_Imports.razor` - 移除 MudBlazor 引用
4. ✅ `App.razor` - 移除 CSS/JS 引用
5. ✅ `Routes.razor` - 移除 Provider 组件

### 页面组件文件

6. ✅ `RunningLog.razor` - 替换所有 MudBlazor 组件
7. ✅ `RunningLog.razor.cs` - 替换服务接口和类型，修正 DataGrid ReadData 事件处理
8. ✅ `AuditLog.razor` - 替换所有 MudBlazor 组件
9. ✅ `AuditLog.razor.cs` - 替换服务接口和类型
10. ✅ `LogDetailDialog.razor` - 替换为 Blazorise Modal
11. ✅ `AuditLogDetailDialog.razor` - 替换为 Blazorise Modal
12. ✅ `SimpleDialog.razor` - 替换为 Blazorise Modal（或删除示例文件）
13. ✅ `MudBlazorExample.razor` - 删除或替换为 Blazorise 示例
14. ✅ `CustomPager.razor` - 检查是否需要替换（如果依赖 MudBlazor）

## 🚀 实施步骤

### 阶段一：准备工作（1-2 小时）

1. 创建功能分支 `feature/remove-mudblazor`
2. 备份当前代码
3. 创建测试清单，确保所有功能点都有测试覆盖

### 阶段二：移除配置和依赖（30 分钟）

1. 移除 NuGet 包引用
2. 移除服务注册
3. 移除 CSS/JS 引用
4. 移除 Provider 组件
5. 编译项目，确保无编译错误

### 阶段三：替换组件（4-6 小时）

1. **简单组件替换**（1-2 小时）

   - 布局组件（Container, Card, Row, Column）
   - 文本和图标组件
   - 基础表单组件

2. **复杂组件替换**（2-3 小时）

   - DataGrid 替换（注意服务器端数据加载）
   - Modal/Dialog 替换（注意参数传递）
   - Tabs 和 Collapse 替换

3. **服务接口替换**（1 小时）
   - IDialogService → IModalService
   - ISnackbar → IMessageService
   - 更新所有调用代码

### 阶段四：测试和修复（2-3 小时）

1. 功能测试

   - 运行日志页面功能
   - 审计日志页面功能
   - 对话框功能
   - 表单验证
   - 分页功能

2. UI/UX 检查

   - 样式一致性
   - 响应式布局
   - 交互体验

3. 修复问题
   - 样式调整
   - 功能修复
   - 性能优化

### 阶段五：清理和文档（1 小时）

1. 删除示例文件（如 MudBlazorExample.razor）
2. 更新文档
3. 代码审查
4. 合并到主分支

## ⚠️ 注意事项

### 1. 样式差异

- Blazorise 和 MudBlazor 的样式系统不同，可能需要额外的 CSS 调整
- 建议使用 Bootstrap 工具类来补充样式

### 2. 功能差异

- **日期时间选择器**：Blazorise 的 DateEdit 可能需要额外配置来支持时间选择
- **对话框参数传递**：Blazorise Modal 的参数传递方式与 MudBlazor 不同
- **数据表格**：Blazorise DataGrid 的 API 与 MudDataGrid 有差异，需要仔细调整

### 3. 图标系统

- MudBlazor 使用 Material Icons
- Blazorise 使用 FontAwesome（已配置）
- 需要将 Material Icons 映射到 FontAwesome 图标

### 4. 枚举类型

- MudBlazor 的 `Color`、`Size`、`Variant` 等枚举需要替换为 Blazorise 对应枚举
- 注意命名空间冲突已解决

### 5. 服务器端数据加载

- MudDataGrid 的 `ServerData` 是方法委托：`Task<GridData<T>> LoadServerData(GridState<T> state)`
- Blazorise DataGrid 的 `ReadData` 是事件：`EventCallback<DataGridReadDataEventArgs<T>> ReadData`
- 需要将方法改为事件处理程序，使用 `DataGridReadDataEventArgs<T>` 参数
- 通过 `e.Data` 设置数据，`e.TotalItems` 设置总数
- 分页信息通过 `e.Page` 和 `e.PageSize` 获取
- 排序信息通过 `e.SortBy` 和 `e.SortDirection` 获取

### 6. CustomPager 组件

- 项目中存在 `CustomPager.razor` 组件
- 需要检查该组件是否依赖 MudBlazor
- 如果依赖，需要替换为 Blazorise 的实现或使用 DataGrid 内置分页器

### 7. 样式调整

- Blazorise 和 MudBlazor 的样式系统不同
- 可能需要额外的 CSS 调整来保持视觉一致性
- 建议使用 Bootstrap 工具类补充样式
- 示例：使用 `class="mt-4"` 替代 `Class="mt-4"`（如果 Blazorise 组件支持）

## 📚 参考资源

- [Blazorise 官方文档](https://blazorise.com/docs/)
- [Blazorise DataGrid 文档](https://blazorise.com/docs/components/datagrid)
- [Blazorise Modal 文档](https://blazorise.com/docs/components/modal)
- [ABP Blazorise 集成文档](https://docs.abp.io/en/abp/latest/UI/Blazor/Blazorise)

## ✅ 验收标准

1. ✅ 项目可以成功编译，无错误
2. ✅ 所有页面功能正常，用户体验一致
3. ✅ 无 MudBlazor 相关代码残留
4. ✅ 代码通过代码审查
5. ✅ 所有测试用例通过

## 🎯 预期收益

1. **简化依赖**：减少一个组件库依赖，降低维护成本
2. **统一体验**：所有组件使用同一套设计系统
3. **性能提升**：减少 CSS/JS 加载量，提升页面加载速度
4. **易于维护**：统一的组件库，降低学习成本

---

**预计总耗时**：10-15 小时（已根据审查报告调整）
**风险等级**：中高风险（DataGrid 和 Modal API 差异较大）
**建议**：分阶段实施，每个阶段完成后进行测试，确保功能正常后再继续

## 🔧 关键修正说明

根据实际迁移过程中遇到的问题和修复方案，以下内容已修正：

1. ✅ **DataGrid ReadData 事件处理**：

   - 排序信息从 `e.Columns` 获取，而不是 `e.SortBy` 和 `e.SortDirection`
   - 使用 `SortDirection.Default` 表示无排序，而不是 `SortDirection.None`（不存在）
   - 需要在组件中维护数据列表（如 `logItems`），并在 DataGrid 上绑定 `Data="@logItems"` 和 `TotalItems="@totalCount"`

2. ✅ **Modal 参数传递**：

   - ✅ **已验证**：使用 `builder.Add(x => x.Property, value)` 表达式语法
   - ❌ **错误方式**：不要使用 `AddParameter` 方法（不存在）
   - ❌ **错误方式**：不要使用字典方式传参（API 不支持）

3. ✅ **ModalInstance 关闭方式**：

   - ✅ **已验证**：使用 `await ModalService.Hide(ModalInstance)`，而不是 `ModalInstance.Hide()`
   - Modal 组件中需要同时注入 `IModalService` 和接收 `ModalInstance` 作为 CascadingParameter

4. ✅ **Text 组件限制**：

   - ✅ **已验证**：Blazorise 的 `Text` 组件不支持 `Class` 属性，会报编译错误 `RZ1023`
   - 如果需要添加 CSS 类，使用 HTML 标签（如 `<div>`, `<span>`, `<p>` 等）

5. ✅ **日期时间选择器**：已补充完整的 DateEdit + TimeEdit 组合方案

6. ✅ **消息服务**：已提供 IMessageService 和 IToastService 两种可能

7. ✅ **CustomPager**：已添加到文件清单

8. ✅ **样式调整**：已补充说明

9. ✅ **必需的 using 引用**：
   - `using Blazorise;` - 用于 IModalService、Color 枚举等
   - `using Blazorise.DataGrid;` - 用于 DataGridReadDataEventArgs、SortDirection 等

## ⚠️ 实施前必读

1. **API 确认**：✅ **已完成验证**，以下 API 使用方式已在实际迁移中验证：

   - ✅ Modal 参数传递：使用 `builder.Add(x => x.Property, value)` 表达式语法
   - ✅ Modal 关闭：使用 `await ModalService.Hide(ModalInstance)`
   - ✅ DataGrid 排序：从 `e.Columns` 获取，使用 `SortDirection.Default` 表示无排序
   - ✅ DataGrid 数据绑定：需要在组件中维护数据列表，绑定 `Data` 和 `TotalItems` 属性
   - ⚠️ 消息服务：需要确认项目中使用的是 `IMessageService` 还是 `IToastService`

2. **常见错误避免**：

   - ❌ 不要使用 `builder.AddParameter()` - 方法不存在
   - ❌ 不要使用 `ModalInstance.Hide()` - 应该使用 `ModalService.Hide(ModalInstance)`
   - ❌ 不要使用 `e.SortBy` 和 `e.SortDirection` - 应该从 `e.Columns` 获取
   - ❌ 不要使用 `SortDirection.None` - 应该使用 `SortDirection.Default`
   - ❌ 不要在 `Text` 组件上使用 `Class` 属性 - 使用 HTML 标签代替

3. **备份代码**：在开始替换前，确保已创建功能分支并备份关键文件

## 💡 实际迁移经验总结

基于本次完整迁移的经验，以下是关键要点：

### ✅ 已验证的正确做法

1. **Modal 参数传递**：

   ```csharp
   await ModalService.Show<LogDetailDialog>(builder =>
   {
       builder.Add(x => x.Log, log);
   });
   ```

2. **Modal 关闭**：

   ```csharp
   [Inject] protected IModalService ModalService { get; set; } = default!;
   [CascadingParameter] protected ModalInstance ModalInstance { get; set; } = default!;

   private async Task Cancel()
   {
       await ModalService.Hide(ModalInstance);
   }
   ```

3. **DataGrid 服务器端数据加载**：

   ```csharp
   private List<AppLog>? logItems;
   private int totalCount = 0;

   private async Task OnReadData(DataGridReadDataEventArgs<AppLog> e)
   {
       // ... 查询逻辑 ...
       logItems = items;
       totalCount = totalCount;
   }
   ```

   ```razor
   <DataGrid TItem="AppLog" Data="@logItems" TotalItems="@totalCount" ReadData="@OnReadData">
   ```

4. **DataGrid 排序处理**：

   ```csharp
   if (e.Columns != null && e.Columns.Any(c => c.SortDirection != SortDirection.Default))
   {
       var sortColumn = e.Columns.FirstOrDefault(c => c.SortDirection != SortDirection.Default);
       // 使用 sortColumn.Field 和 sortColumn.SortDirection
   }
   ```

5. **Text 组件使用**：
   - 纯文本：使用 `<Text>...</Text>`
   - 需要样式类：使用 `<div class="...">` 或 `<span class="...">`

### ❌ 常见错误

1. `builder.AddParameter()` - 方法不存在
2. `ModalInstance.Hide()` - 应该使用 `ModalService.Hide(ModalInstance)`
3. `e.SortBy` 和 `e.SortDirection` - 应该从 `e.Columns` 获取
4. `SortDirection.None` - 应该使用 `SortDirection.Default`
5. `<Text Class="...">` - Text 组件不支持 Class 属性

### 📦 必需的引用

```csharp
using Blazorise;
using Blazorise.DataGrid;
```

### 🔍 调试技巧

1. 遇到编译错误时，优先检查：

   - 是否正确添加了 `using` 引用
   - API 方法名是否正确（如 `Add` vs `AddParameter`）
   - 枚举值是否正确（如 `Default` vs `None`）

2. 运行时问题排查：
   - 检查 Modal 是否正确注入 `IModalService`
   - 检查 DataGrid 是否正确绑定 `Data` 和 `TotalItems`
   - 检查事件处理程序签名是否正确
