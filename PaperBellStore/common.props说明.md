# common.props 文件说明

## 📋 什么是 common.props？

`common.props` 是一个 **MSBuild 属性文件**，用于在整个解决方案中**统一管理**多个项目的共同配置。它是 .NET 项目中的一项最佳实践，通过 `<Import>` 指令导入到每个 `.csproj` 项目中。

---

## 🎯 主要用途

### 1. **统一项目配置** 🔧

避免在每个项目中重复配置相同的属性，确保所有项目使用一致的设置。

### 2. **集中管理版本和设置** 📌

在一个地方管理所有项目共享的属性，如：

- C# 语言版本
- 项目版本号
- 警告配置
- 项目类型标识

### 3. **简化维护** 🛠️

当需要修改配置时，只需修改一个文件，所有项目自动应用更改。

---

## 📄 当前项目中的 common.props 内容

```xml
<Project>
  <PropertyGroup>
    <!-- 1. 语言版本设置 -->
    <LangVersion>latest</LangVersion>

    <!-- 2. 项目版本号 -->
    <Version>1.0.0</Version>

    <!-- 3. 警告配置 -->
    <NoWarn>$(NoWarn);CS1591</NoWarn>

    <!-- 4. ABP项目类型标识 -->
    <AbpProjectType>app</AbpProjectType>
  </PropertyGroup>

  <!-- 5. 自定义编译目标 - 处理Razor视图的警告 -->
  <Target Name="NoWarnOnRazorViewImportedTypeConflicts" BeforeTargets="RazorCoreCompile">
    <PropertyGroup>
      <NoWarn>$(NoWarn);0436</NoWarn>
    </PropertyGroup>
  </Target>

  <!-- 6. 排除ABP Studio生成的文件 -->
  <ItemGroup>
    <None Remove="**\*.abppkg" />
    <None Remove="**\*.abppkg.analyze.json" />
    <Content Remove="$(UserProfile)\.nuget\packages\*\*\contentFiles\any\*\*.abppkg*" />
  </ItemGroup>
</Project>
```

---

## 🔍 配置项详解

### 1. `<LangVersion>latest</LangVersion>`

**作用**：设置所有项目使用 **最新的 C# 语言版本**

**好处**：

- ✅ 所有项目都可以使用最新的 C# 特性
- ✅ 统一代码风格和语法
- ✅ 避免因语言版本不一致导致的兼容性问题

**示例**：可以使用 C# 12/13 的最新特性，如：

- `Primary constructors`
- `Collection expressions`
- `ref readonly` 等

---

### 2. `<Version>1.0.0</Version>`

**作用**：定义**项目版本号**

**好处**：

- ✅ 统一管理整个解决方案的版本
- ✅ 便于版本控制和发布管理
- ✅ 所有项目的程序集版本保持一致

**使用场景**：

- NuGet 打包时使用此版本
- 程序集元数据中的版本信息
- 发布和部署时标识版本

---

### 3. `<NoWarn>$(NoWarn);CS1591</NoWarn>`

**作用**：**禁用特定的编译器警告**

- `CS1591`：缺少 XML 注释文档的警告
- `$(NoWarn)`：保留现有的警告配置，追加新的警告忽略

**为什么禁用 CS1591**：

- 不是所有代码都需要 XML 文档注释
- 开发阶段可以更专注于功能实现
- 减少编译输出的噪音

**如果需要生成文档**：可以单独为需要文档的项目移除此配置。

---

### 4. `<AbpProjectType>app</AbpProjectType>`

**作用**：标识项目为 **ABP 应用程序项目**

**用途**：

- ABP Studio 工具识别项目类型
- ABP CLI 工具自动配置
- ABP 框架的某些特性会根据此标识调整行为

**可能的值**：

- `app`：应用程序项目
- `module`：模块项目
- `library`：类库项目

---

### 5. Razor 视图警告处理

```xml
<Target Name="NoWarnOnRazorViewImportedTypeConflicts" BeforeTargets="RazorCoreCompile">
  <PropertyGroup>
    <NoWarn>$(NoWarn);0436</NoWarn>
  </PropertyGroup>
</Target>
```

**作用**：在编译 Razor 视图时禁用类型冲突警告 `0436`

**为什么需要**：

- Blazor/MVC 项目的 Razor 视图可能会引用多个命名空间
- 某些情况下会产生误报的类型冲突警告
- 这些警告通常不影响实际功能

---

### 6. 排除 ABP Studio 生成的文件

```xml
<ItemGroup>
  <None Remove="**\*.abppkg" />
  <None Remove="**\*.abppkg.analyze.json" />
  <Content Remove="$(UserProfile)\.nuget\packages\*\*\contentFiles\any\*\*.abppkg*" />
</ItemGroup>
```

**作用**：排除 ABP Studio 工具生成的临时文件

**好处**：

- ✅ 避免这些文件被包含在构建输出中
- ✅ 保持项目目录整洁
- ✅ 防止误提交到源代码控制

---

## 💡 如何在项目中使用

### 在 `.csproj` 文件中导入

**对于 `src/` 目录下的项目**：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\..\common.props" />  <!-- 向上两层到解决方案根目录 -->

  <PropertyGroup>
    <!-- 项目特定的配置 -->
  </PropertyGroup>
</Project>
```

**对于 `modules/` 目录下的项目**：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\..\..\common.props" />  <!-- 向上三层到解决方案根目录 -->

  <PropertyGroup>
    <!-- 项目特定的配置 -->
  </PropertyGroup>
</Project>
```

### 路径说明

```
PaperBellStore/                    ← 解决方案根目录（common.props 所在位置）
├── common.props                   ← 这个文件
├── src/
│   └── PaperBellStore.Domain/
│       └── PaperBellStore.Domain.csproj  ← 使用 ..\..\common.props
└── modules/
    └── ProjectManagement/
        └── ProjectManagement.Domain/
            └── ProjectManagement.Domain.csproj  ← 使用 ..\..\..\common.props
```

---

## ✅ 使用 common.props 的优势

### 1. **DRY 原则** (Don't Repeat Yourself)

- 避免在每个项目中重复相同的配置
- 减少代码冗余和维护成本

### 2. **一致性保证**

- 确保所有项目使用相同的编译器设置
- 统一的版本号和构建配置

### 3. **易于维护**

- 修改一处，全局生效
- 减少配置错误和遗漏

### 4. **团队协作**

- 新成员只需了解一个配置文件
- 减少配置不一致导致的问题

---

## 🔧 常见自定义场景

### 场景 1：修改版本号

只需修改 `common.props` 中的 `<Version>`：

```xml
<Version>2.0.0</Version>
```

所有项目自动使用新版本！

### 场景 2：添加新的警告忽略

```xml
<NoWarn>$(NoWarn);CS1591;CS1701;CS1702</NoWarn>
```

### 场景 3：添加自定义属性

```xml
<PropertyGroup>
  <!-- 统一输出目录 -->
  <OutputPath>$(SolutionDir)bin\$(Configuration)\</OutputPath>

  <!-- 统一符号设置 -->
  <DebugType>portable</DebugType>

  <!-- 统一 NuGet 配置 -->
  <Authors>Your Company</Authors>
  <Company>Your Company Name</Company>
</PropertyGroup>
```

---

## 📝 最佳实践

### ✅ 应该在 common.props 中配置的内容：

- 语言版本
- 版本号
- 编译器警告设置
- 项目类型标识
- 通用的构建配置
- 排除规则（如临时文件）

### ❌ 不应该在 common.props 中配置的内容：

- 项目特定的依赖包（NuGet 包）
- 项目特定的项目引用
- 项目特定的编译设置（除非所有项目都需要）

---

## 🔗 相关概念

- **Directory.Build.props**：.NET SDK 风格的自动导入机制（如果存在会自动导入）
- **Directory.Build.targets**：用于定义构建目标
- **MSBuild 属性**：用于在整个构建过程中传递信息

---

## 📚 总结

`common.props` 是一个**集中管理**项目配置的优秀实践，它：

- ✅ 统一配置，避免重复
- ✅ 简化维护，一处修改全局生效
- ✅ 保证一致性，减少配置错误
- ✅ 提高可维护性，便于团队协作

在模块化开发中特别重要，因为多个模块项目都需要使用相同的配置，`common.props` 确保了所有模块的一致性！🎉
