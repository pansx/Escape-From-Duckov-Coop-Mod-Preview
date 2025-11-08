# Properties 模块

## 概述

Properties 模块包含程序集元数据定义，用于配置模组的版本信息、版权信息和程序集属性。

## 模块结构

```

Properties/
└── AssemblyInfo.cs    - 程序集信息定义

```

## 核心内容

### AssemblyInfo.cs

**功能**：定义程序集的元数据属性。

**程序集属性**：```csharp
[assembly: AssemblyTitle(Name)]                    // 程序集标题
[assembly: AssemblyDescription("")]                // 程序集描述
[assembly: AssemblyConfiguration("")]              // 配置信息
[assembly: AssemblyCompany("")]                    // 公司名称
[assembly: AssemblyProduct(Name)]                  // 产品名称
[assembly: AssemblyCopyright(Copyright)]           // 版权信息
[assembly: AssemblyTrademark("")]                  // 商标信息
[assembly: AssemblyCulture("")]                    // 文化信息
[assembly: ComVisible(false)]                      // COM 可见性
[assembly: AssemblyVersion(ModVersion)]            // 程序集版本
[assembly: AssemblyFileVersion(ModVersion)]        // 文件版本

```

**依赖的常量**：- `BuildInfo.Name`: 模组名称

- `BuildInfo.Copyright`: 版权信息

- `BuildInfo.ModVersion`: 模组版本号

## 版本信息

版本信息从`BuildInfo`类获取，该类定义在`BuildInfo.cs`中：

```csharp
public static class BuildInfo
{
    public const string Name = "Escape From Duckov Coop Mod";
    public const string ModVersion = "1.0.0";
    public const string Copyright = "Copyright (C) 2025 Mr.sans and InitLoader's team";
}

```

## 用途

### 1. 版本管理

- 标识模组版本

- 用于兼容性检查

- 用于更新检测

### 2. 法律信息

- 版权声明

- 许可证信息

- 商标信息

### 3. 程序集标识

- 唯一标识程序集

- 用于反射和加载

- 用于依赖管理

## 总结

Properties 模块虽然简单，但提供了重要的程序集元数据，用于版本管理和法律声明。这是.NET 程序集的标准配置模块。
