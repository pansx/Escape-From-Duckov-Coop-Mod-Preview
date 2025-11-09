# CheatMenu - 游戏增强 Mod

## 📖 文档导航

### 📚 主要文档

1. **[功能文档](./CheatMenu_功能文档.md)** - 完整功能说明和技术架构
   - 所有功能详细介绍
   - 技术实现原理
   - 代码架构分析
   - 性能优化说明

2. **[快速参考](./CheatMenu_快速参考.md)** - 速查手册
   - 功能开关速查表
   - 参数调整指南
   - 常用操作示例
   - 故障排查方案

---

## 🎯 功能概览

### 核心功能

| 类别 | 功能 | 状态 |
|------|------|------|
| 🎯 **瞄准辅助** | 自动瞄准 | ✅ 默认启用 |
| | 投掷物追踪 | ✅ 默认启用 |
| | 旋转击杀 | ❌ 默认禁用 |
| 🔫 **武器增强** | 无后坐力 | ✅ 默认启用 |
| | 无限弹药 | ✅ 默认启用 |
| | 无限投掷物 | ✅ 默认启用 |
| | 穿墙射击 | ✅ 默认启用 |
| | 属性倍率调整 | ⚙️ 可配置 |
| 🏃 **移动增强** | 移动解锁 | ✅ 默认启用 |
| | 无限耐力 | ❌ 默认禁用 |
| 🛡️ **角色状态** | 无敌模式 | ✅ 默认启用 |
| | 无限耐久 | ✅ 默认启用 |
| | 无生存需求 | ✅ 默认启用 |

---

## 🚀 快速开始

### 1. 编译项目

```bash
# 在项目根目录执行
cd cheat
dotnet build CheatMenu.sln --configuration Release
```

### 2. 部署 Mod

将编译生成的 `CheatMenu.dll` 复制到游戏 Mod 目录：

```
<游戏安装目录>/Duckov_Data/Mods/CheatMenu/CheatMenu.dll
```

### 3. 启动游戏

启动游戏后，Mod 会自动加载并应用默认设置。

---

## 📋 项目结构

```
cheat/
├── README.md                        # 本文件
├── CheatMenu_功能文档.md            # 完整功能文档
├── CheatMenu_快速参考.md            # 快速参考手册
├── CheatMenu.sln                    # Visual Studio 解决方案
└── CheatMenu/
    ├── CheatMenu.csproj             # 项目文件
    ├── ModBehaviour.cs              # 主逻辑（6855 行）
    ├── Properties/
    │   └── AssemblyInfo.cs
    ├── Microsoft/CodeAnalysis/
    │   └── EmbeddedAttribute.cs
    └── System/Runtime/CompilerServices/
        ├── NullableAttribute.cs
        └── NullableContextAttribute.cs
```

---

## 🔧 开发环境

### 要求

- **.NET Standard 2.1**
- **Visual Studio 2019+** 或 **Rider**
- **Unity 引擎** (游戏使用的版本)

### 依赖项

项目引用以下游戏 DLL（需要从游戏目录复制）：

```
Duckov_Data/Managed/
├── ItemStatsSystem.dll
├── TeamSoda.Duckov.Core.dll
├── UniTask.dll
├── Unity.TextMeshPro.dll
├── UnityEngine.CoreModule.dll
├── UnityEngine.IMGUIModule.dll
├── UnityEngine.InputLegacyModule.dll
├── UnityEngine.PhysicsModule.dll
├── UnityEngine.UI.dll
└── UnityEngine.UIModule.dll
```

---

## 📊 代码统计

- **总代码行数**: 6,855 行
- **主要类**: `ModBehaviour`
- **嵌套类**: 2 个
- **结构体**: 5 个
- **枚举**: 1 个
- **方法数**: 150+ 个
- **字段数**: 120+ 个

---

## 🎮 使用建议

### ✅ 推荐场景

- **单人模式**: 测试游戏机制
- **私人服务器**: 与朋友娱乐
- **内容创作**: 制作视频或截图
- **开发调试**: 快速测试功能

### ⚠️ 注意事项

- **不要在公共多人服务器使用**，可能导致账号封禁
- **定期备份存档**，避免数据损坏
- **游戏更新后**可能需要重新编译 Mod
- **性能影响**：旋转击杀等功能可能影响帧率

---

## 🔍 功能亮点

### 1. 多线程自动瞄准
- 独立工作线程计算目标优先级
- 50 FPS 更新频率
- 智能目标选择算法

### 2. 投掷物追踪系统
- 自动追踪敌人
- 140°/秒 追踪速度
- 支持所有投掷物类型

### 3. 旋转击杀模式
- 自动旋转并击杀范围内敌人
- 可视化范围指示器
- 智能目标优先级

### 4. 完善的事件钩子
- 监听角色状态变化
- 自动应用增强效果
- 支持热重载

---

## 🛠️ 故障排查

### 常见问题

**Q: Mod 加载失败？**
- 检查 DLL 文件是否在正确位置
- 查看游戏日志文件
- 确认游戏版本兼容性

**Q: 功能不生效？**
- 确认功能已启用
- 重新进入关卡
- 检查是否有冲突的 Mod

**Q: 游戏崩溃？**
- 禁用旋转击杀功能
- 降低武器倍率设置
- 查看崩溃日志

### 日志位置

```
Windows: %AppData%\..\LocalLow\Duckov\Escape from Duckov\Player.log
```

---

## 📚 相关文档

### 项目文档
- [完整功能文档](./CheatMenu_功能文档.md)
- [快速参考手册](./CheatMenu_快速参考.md)

### 游戏开发文档
- [游戏 API 文档](../codeAnalysis/INDEX.md)
- [Mod 部署流程](../.kiro/steering/mod-deployment.md)
- [C# 工作流规范](../.kiro/steering/csharp-workflow.md)

---

## ⚖️ 许可证

本项目仅供学习和研究使用。请遵守游戏的服务条款和用户协议。

---

## 📝 更新日志

### v1.0 (2025-11-09)
- ✅ 初始版本
- ✅ 完整功能实现
- ✅ 文档编写完成

---

*最后更新: 2025-11-09*
