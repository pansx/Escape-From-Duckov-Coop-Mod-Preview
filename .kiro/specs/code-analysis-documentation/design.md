# Design Document - 代码分析与文档生成系统

## Overview

本设计文档描述了如何系统地分析 Escape From Duckov Coop Mod 项目的代码结构，并生成层次化的API文档。该系统采用自底向上的分析策略，从最深层的代码模块开始，逐层向上汇总，最终生成完整的项目架构文档。

### 项目背景

Escape From Duckov Coop Mod 是一个Unity C#游戏模组，为单人游戏《逃离鸭科夫》添加了多人联机合作功能。项目使用以下技术栈：

- **语言**: C# (.NET Standard 2.1)
- **框架**: Unity游戏引擎
- **网络库**: LiteNetLib (UDP), Steam P2P
- **代码注入**: HarmonyLib (运行时代码修改)

### 设计目标

1. **完整性**: 覆盖所有源代码文件和模块
2. **层次性**: 保持代码的层次结构，从底层到顶层
3. **可读性**: 生成清晰易懂的中文文档
4. **实用性**: 突出关键API和核心流程
5. **可维护性**: 文档结构便于后续更新

## Architecture

### 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    代码分析与文档生成系统                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Phase 1: 项目结构扫描                                        │
│  - 递归扫描目录树                                             │
│  - 识别所有.cs文件                                            │
│  - 计算目录深度                                               │
│  - 排除bin/obj目录                                            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Phase 2: 深度优先分析                                        │
│  - 按深度排序目录                                             │
│  - 从最深层开始分析                                           │
│  - 每个目录一个分析任务                                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Phase 3: 代码解析与提取                                      │
│  - 读取C#源代码                                               │
│  - 提取类/接口/枚举定义                                        │
│  - 识别方法、属性、字段                                        │
│  - 提取注释和文档                                             │
│  - 分析依赖关系                                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Phase 4: 模块文档生成                                        │
│  - 为每个目录生成API文档                                       │
│  - 描述模块功能和职责                                          │
│  - 列出主要类和接口                                           │
│  - 说明模块间关系                                             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Phase 5: 层次汇总                                            │
│  - 向上汇总子模块信息                                          │
│  - 生成父模块文档                                             │
│  - 描述模块组织结构                                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Phase 6: 总体架构文档                                        │
│  - 生成项目总览                                               │
│  - 描述整体架构                                               │
│  - 说明核心流程                                               │
│  - 提供使用指南                                               │
└─────────────────────────────────────────────────────────────┘
```

### 分层架构

系统采用分层处理架构：

1. **扫描层**: 文件系统遍历和结构识别
2. **解析层**: C#代码语法分析和信息提取
3. **分析层**: 代码语义理解和关系识别
4. **生成层**: Markdown文档生成和格式化
5. **汇总层**: 跨模块信息整合和架构总结

## Components and Interfaces

### 1. 目录扫描器 (Directory Scanner)

**职责**: 扫描项目目录结构，识别所有需要分析的文件夹和文件

**接口**:
```csharp
interface IDirectoryScanner
{
    // 扫描指定目录，返回目录树结构
    DirectoryTree Scan(string rootPath);
    
    // 获取按深度排序的目录列表
    List<DirectoryInfo> GetDirectoriesByDepth(bool deepestFirst);
    
    // 过滤需要排除的目录
    bool ShouldExclude(string directoryPath);
}
```

**实现细节**:
- 递归遍历 `EscapeFromDuckovCoopMod` 目录
- 排除 `bin`, `obj`, `.git` 等目录
- 记录每个目录的深度和父子关系
- 识别所有 `.cs` 文件

### 2. 代码解析器 (Code Parser)

**职责**: 解析C#源代码，提取类型定义和成员信息

**接口**:
```csharp
interface ICodeParser
{
    // 解析单个C#文件
    CodeFile Parse(string filePath);
    
    // 提取类定义
    List<ClassInfo> ExtractClasses(string sourceCode);
    
    // 提取方法定义
    List<MethodInfo> ExtractMethods(ClassInfo classInfo);
    
    // 提取依赖关系
    List<Dependency> ExtractDependencies(CodeFile file);
}
```

**数据模型**:
```csharp
class CodeFile
{
    string FilePath;
    string Namespace;
    List<ClassInfo> Classes;
    List<string> Usings;
}

class ClassInfo
{
    string Name;
    string Namespace;
    ClassType Type; // Class, Interface, Enum, Struct
    string BaseClass;
    List<string> Interfaces;
    List<MethodInfo> Methods;
    List<PropertyInfo> Properties;
    List<FieldInfo> Fields;
    string Summary; // XML注释
}

class MethodInfo
{
    string Name;
    string ReturnType;
    List<ParameterInfo> Parameters;
    AccessModifier Access; // Public, Private, Protected, Internal
    bool IsStatic;
    string Summary;
}
```

### 3. 模块分析器 (Module Analyzer)

**职责**: 分析模块功能，识别设计模式和架构模式

**接口**:
```csharp
interface IModuleAnalyzer
{
    // 分析模块功能
    ModuleAnalysis Analyze(DirectoryInfo directory, List<CodeFile> files);
    
    // 识别模块类型
    ModuleType IdentifyModuleType(ModuleAnalysis analysis);
    
    // 提取核心类
    List<ClassInfo> IdentifyCoreClasses(ModuleAnalysis analysis);
    
    // 分析模块间关系
    List<ModuleRelation> AnalyzeRelations(ModuleAnalysis module, List<ModuleAnalysis> allModules);
}
```

**分析维度**:
- **功能职责**: 模块的主要功能和用途
- **设计模式**: 使用的设计模式（如单例、工厂、观察者等）
- **依赖关系**: 与其他模块的依赖关系
- **核心类**: 模块中最重要的类和接口
- **公共API**: 对外暴露的公共接口

### 4. 文档生成器 (Documentation Generator)

**职责**: 生成Markdown格式的API文档

**接口**:
```csharp
interface IDocumentationGenerator
{
    // 生成模块文档
    string GenerateModuleDoc(ModuleAnalysis module);
    
    // 生成类文档
    string GenerateClassDoc(ClassInfo classInfo);
    
    // 生成总体架构文档
    string GenerateArchitectureDoc(List<ModuleAnalysis> modules);
}
```

**文档模板**:

#### 模块文档模板
```markdown
# [模块名称]

## 概述
[模块功能描述]

## 文件列表
- File1.cs
- File2.cs

## 核心类

### ClassName
**功能**: [类的功能描述]
**继承**: BaseClass
**接口**: IInterface1, IInterface2

#### 主要方法
- `MethodName(params)`: 方法描述

## 依赖关系
- 依赖模块A
- 依赖模块B

## 使用示例
[代码示例]
```

#### 总体架构文档模板
```markdown
# Escape From Duckov Coop Mod - 项目架构文档

## 项目概述
[项目简介]

## 技术栈
- Unity
- C# .NET Standard 2.1
- LiteNetLib
- HarmonyLib

## 架构设计

### 模块划分
[模块列表和职责]

### 核心流程
[关键业务流程]

### 网络架构
[网络通信机制]

## 模块详解
[各模块详细说明]
```

### 5. 汇总引擎 (Aggregation Engine)

**职责**: 将子模块信息汇总到父模块，生成层次化文档

**接口**:
```csharp
interface IAggregationEngine
{
    // 汇总子模块信息
    ModuleAnalysis Aggregate(DirectoryInfo parentDir, List<ModuleAnalysis> childModules);
    
    // 生成层次结构描述
    string GenerateHierarchyDescription(ModuleAnalysis module);
}
```

## Data Models

### 项目结构数据模型

```csharp
// 目录树
class DirectoryTree
{
    DirectoryNode Root;
    Dictionary<string, DirectoryNode> AllNodes;
}

class DirectoryNode
{
    string Path;
    string Name;
    int Depth;
    DirectoryNode Parent;
    List<DirectoryNode> Children;
    List<string> CsFiles;
}

// 模块分析结果
class ModuleAnalysis
{
    string ModulePath;
    string ModuleName;
    int Depth;
    ModuleType Type;
    string FunctionalDescription;
    List<CodeFile> SourceFiles;
    List<ClassInfo> CoreClasses;
    List<ModuleAnalysis> SubModules;
    List<ModuleRelation> Dependencies;
    Dictionary<string, string> KeyConcepts;
}

enum ModuleType
{
    Core,           // 核心模块
    Network,        // 网络模块
    UI,             // 界面模块
    Service,        // 服务模块
    Utility,        // 工具模块
    Patch,          // 补丁模块
    Data            // 数据模块
}

class ModuleRelation
{
    string SourceModule;
    string TargetModule;
    RelationType Type;
    string Description;
}

enum RelationType
{
    DependsOn,      // 依赖
    Uses,           // 使用
    Implements,     // 实现
    Extends         // 扩展
}
```

### 代码分析数据模型

```csharp
// 依赖关系
class Dependency
{
    string SourceClass;
    string TargetClass;
    DependencyType Type;
}

enum DependencyType
{
    Inheritance,    // 继承
    Implementation, // 接口实现
    Association,    // 关联
    Aggregation,    // 聚合
    Composition     // 组合
}

// 设计模式识别
class DesignPattern
{
    PatternType Type;
    List<ClassInfo> ParticipatingClasses;
    string Description;
}

enum PatternType
{
    Singleton,
    Factory,
    Observer,
    Strategy,
    Command,
    State,
    Adapter,
    Facade
}
```

## Error Handling

### 错误类型和处理策略

1. **文件访问错误**
   - 场景: 无法读取源代码文件
   - 处理: 记录错误日志，跳过该文件，继续处理其他文件
   - 恢复: 在文档中标注该文件分析失败

2. **代码解析错误**
   - 场景: C#语法错误或无法解析的代码结构
   - 处理: 使用基础文本分析作为降级方案
   - 恢复: 提取基本信息（类名、方法名），标注为部分解析

3. **依赖关系循环**
   - 场景: 模块间存在循环依赖
   - 处理: 检测并记录循环依赖链
   - 恢复: 在文档中明确标注循环依赖

4. **内存不足**
   - 场景: 处理大量文件时内存不足
   - 处理: 分批处理文件，及时释放内存
   - 恢复: 使用流式处理和增量生成

### 错误日志格式

```
[ERROR] [Phase] [Component] Message
  File: /path/to/file.cs
  Line: 123
  Details: Detailed error information
```

## Testing Strategy

### 测试层次

1. **单元测试**
   - 测试各个组件的独立功能
   - 重点: 代码解析器、文档生成器

2. **集成测试**
   - 测试组件间的协作
   - 重点: 扫描器→解析器→生成器的数据流

3. **端到端测试**
   - 测试完整的分析流程
   - 重点: 从项目根目录到最终文档的完整生成

### 测试用例

#### 代码解析器测试
```csharp
// 测试用例1: 解析简单类
Input: 
  public class SimpleClass { }
Expected:
  ClassInfo { Name="SimpleClass", Type=Class, Methods=[], Properties=[] }

// 测试用例2: 解析带继承的类
Input:
  public class DerivedClass : BaseClass, IInterface { }
Expected:
  ClassInfo { Name="DerivedClass", BaseClass="BaseClass", Interfaces=["IInterface"] }

// 测试用例3: 解析带方法的类
Input:
  public class ClassWithMethods {
      public void Method1() { }
      private int Method2(string param) { return 0; }
  }
Expected:
  ClassInfo {
      Methods=[
          MethodInfo { Name="Method1", Access=Public, ReturnType="void" },
          MethodInfo { Name="Method2", Access=Private, ReturnType="int", Parameters=[...] }
      ]
  }
```

#### 文档生成器测试
```csharp
// 测试用例: 生成类文档
Input:
  ClassInfo { Name="TestClass", Summary="Test class description" }
Expected Output:
  ### TestClass
  **功能**: Test class description
  ...
```

### 验证标准

1. **完整性验证**
   - 所有源代码文件都被处理
   - 所有模块都生成了文档
   - 没有遗漏的目录

2. **准确性验证**
   - 类名、方法名提取正确
   - 继承关系识别准确
   - 依赖关系分析正确

3. **可读性验证**
   - Markdown格式正确
   - 章节结构清晰
   - 代码示例可运行

## Implementation Details

### 分析顺序

基于项目结构，分析顺序如下（从深到浅）：

#### 第1层（最深层 - 深度3）
1. `Main/AI/` - AI系统
2. `Main/ClientService/` - 客户端服务
3. `Main/Health/` - 生命值系统
4. `Main/HostService/` - 主机服务
5. `Main/Item/` - 物品系统
6. `Main/Loader/` - 加载器
7. `Main/Localization/` - 本地化
8. `Main/LocalPlayer/` - 本地玩家
9. `Main/SceneService/` - 场景服务
10. `Main/UI/` - 用户界面
11. `Main/Weapon/` - 武器系统
12. `Main/WeatherAndTime/` - 天气和时间
13. `Net/NetPack/` - 网络数据包
14. `Net/Steam/` - Steam网络
15. `Patch/Character/` - 角色补丁
16. `Patch/InventoryAndLootBox/` - 背包和战利品补丁
17. `Patch/Item/` - 物品补丁
18. `Patch/Projectile/` - 投射物补丁
19. `Patch/Scene/` - 场景补丁
20. `Patch/SteamP2P/` - Steam P2P补丁

#### 第2层（深度2）
1. `Main/` - 主要业务逻辑（汇总上述12个子模块）
2. `Net/` - 网络层（汇总NetPack和Steam）
3. `Patch/` - 补丁层（汇总6个补丁子模块）
4. `NetTag/` - 网络标签
5. `SyncData/` - 同步数据
6. `Utils/` - 工具类

#### 第3层（深度1 - 根目录）
1. `EscapeFromDuckovCoopMod/` - 项目根（汇总所有模块）

### 关键技术决策

#### 1. 代码解析方法

**决策**: 使用正则表达式和文本分析，而非完整的C#编译器

**理由**:
- 无需完整编译项目
- 处理速度快
- 可以处理有语法错误的代码
- 足以提取API级别的信息

**权衡**:
- 无法进行深度语义分析
- 可能遗漏复杂的代码结构
- 需要处理各种代码风格

#### 2. 文档组织结构

**决策**: 采用层次化的多文件结构，而非单一大文件

**理由**:
- 便于导航和查找
- 支持增量更新
- 文件大小适中，易于阅读
- 符合模块化原则

**结构**:
```
codeAnalysis/
├── README.md                    # 总体架构文档
├── Main/
│   ├── README.md               # Main模块总览
│   ├── AI.md                   # AI子模块
│   ├── ClientService.md        # 客户端服务子模块
│   └── ...
├── Net/
│   ├── README.md               # Net模块总览
│   ├── NetPack.md              # 网络数据包
│   └── Steam.md                # Steam网络
├── Patch/
│   ├── README.md               # Patch模块总览
│   └── ...
└── ...
```

#### 3. 信息提取深度

**决策**: 提取公共API和关键私有方法，忽略实现细节

**理由**:
- 文档聚焦于接口和用法
- 避免文档过于冗长
- 实现细节可以查看源代码

**提取内容**:
- ✅ 公共类、接口、枚举
- ✅ 公共方法和属性
- ✅ 重要的私有方法（如事件处理器）
- ✅ 类和方法的注释
- ❌ 方法实现代码
- ❌ 私有字段的详细信息
- ❌ 局部变量

### 性能优化

1. **并行处理**: 独立模块可以并行分析
2. **缓存机制**: 缓存已解析的文件，避免重复处理
3. **增量更新**: 只重新分析修改过的文件
4. **流式处理**: 大文件使用流式读取，避免一次性加载到内存

### 扩展性设计

1. **插件化解析器**: 支持添加新的代码解析器（如支持其他语言）
2. **自定义模板**: 支持自定义文档模板
3. **多格式输出**: 支持输出HTML、PDF等格式
4. **API导出**: 提供JSON格式的API数据导出

## Design Patterns Used

### 1. 策略模式 (Strategy Pattern)
- **应用**: 代码解析器
- **目的**: 支持不同的解析策略（正则表达式、AST解析等）

### 2. 访问者模式 (Visitor Pattern)
- **应用**: 目录树遍历
- **目的**: 分离遍历逻辑和处理逻辑

### 3. 建造者模式 (Builder Pattern)
- **应用**: 文档生成
- **目的**: 逐步构建复杂的Markdown文档

### 4. 模板方法模式 (Template Method Pattern)
- **应用**: 模块分析流程
- **目的**: 定义分析流程框架，子类实现具体步骤

### 5. 单例模式 (Singleton Pattern)
- **应用**: 全局配置管理
- **目的**: 确保配置的一致性

## Project-Specific Considerations

### Unity和C#特性

1. **MonoBehaviour类**: 识别Unity组件类
2. **协程方法**: 识别IEnumerator返回类型的方法
3. **特性标注**: 提取[HarmonyPatch]等特性信息
4. **序列化字段**: 识别[SerializeField]标注的字段

### HarmonyLib补丁

1. **补丁类识别**: 识别包含[HarmonyPatch]的类
2. **补丁方法**: 识别Prefix、Postfix、Transpiler方法
3. **目标方法**: 提取被补丁的原始方法信息

### 网络通信

1. **RPC方法**: 识别网络RPC调用
2. **数据包结构**: 分析NetDataWriter/Reader的使用
3. **同步机制**: 识别状态同步相关代码

### 关键流程识别

需要在文档中重点说明的流程：

1. **模组加载流程**: Loader → Mod → 各服务初始化
2. **网络连接流程**: Steam/LAN连接建立
3. **玩家同步流程**: 本地玩家 → 网络传输 → 远程玩家
4. **AI同步流程**: AI状态 → 网络同步 → 客户端表现
5. **物品同步流程**: 物品操作 → 同步 → 其他玩家
6. **场景切换流程**: 投票 → 加载 → 同步

## Documentation Quality Criteria

### 必须满足的标准

1. **准确性**: 所有类名、方法名必须准确无误
2. **完整性**: 覆盖所有主要模块和核心类
3. **清晰性**: 使用清晰的中文描述，避免歧义
4. **实用性**: 提供足够的信息帮助理解代码
5. **可维护性**: 文档结构便于后续更新

### 文档评审检查点

- [ ] 所有模块都有文档
- [ ] 核心类都有详细说明
- [ ] 关键流程有清晰描述
- [ ] 代码示例正确可用
- [ ] Markdown格式正确
- [ ] 链接和引用有效
- [ ] 术语使用一致
- [ ] 中英文混排规范
