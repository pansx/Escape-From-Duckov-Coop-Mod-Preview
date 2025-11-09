# CheatMenu 作弊菜单功能文档

## 📋 概述

CheatMenu 是一个为《Escape from Duckov》游戏开发的功能增强 Mod，提供了多种游戏辅助功能。该 Mod 通过 Unity 的 ModBehaviour 系统实现，支持实时开关和参数调整。

## 🎯 核心功能列表

### 1. 自动瞄准系统 (Auto Aim)

#### 1.1 枪械自动瞄准

-   **功能**: 自动锁定屏幕范围内的敌人
-   **默认状态**: ✅ 启用
-   **配置参数**:
    -   `_autoAimEnabled`: 总开关
    -   `_autoAimScreenRadius`: 屏幕检测半径（像素）
    -   范围可视化显示（可切换）

#### 1.2 投掷物自动追踪

-   **功能**: 手榴弹等投掷物自动追踪敌人
-   **默认状态**: ✅ 启用
-   **配置参数**:
    -   `_grenadeAutoAimEnabled`: 投掷物追踪开关
    -   追踪速度: 140°/秒
    -   最小速度: 12 m/s

#### 1.3 技术实现

-   **多线程处理**: 使用独立工作线程计算目标优先级
-   **目标选择算法**:
    ```
    优先级 = 屏幕距离 + 角度偏差 × 2.25 + 距离 × 0.1
    ```
-   **组件**:
    -   `AutoAimThreadContext`: 线程上下文数据
    -   `AutoAimThreadResult`: 计算结果
    -   `AutoAimCandidate`: 候选目标信息
    -   `GrenadeHomingComponent`: 投掷物追踪组件

---

### 2. 武器增强系统

#### 2.1 无后坐力 (No Recoil)

-   **功能**: 消除所有武器后坐力
-   **默认状态**: ✅ 启用
-   **实现方式**:
    -   修改角色 `RecoilControl` 属性为 9999
    -   重置枪械内部后坐力状态
    -   覆盖物品统计数据（垂直/水平后坐力、扩散等）

#### 2.2 无限弹药 (Infinite Ammo)

-   **功能**: 射击后自动恢复弹药
-   **默认状态**: ✅ 启用
-   **实现方式**:
    -   监听 `OnShootEvent` 事件
    -   射击后立即恢复弹匣和备弹

#### 2.3 无限投掷物 (Infinite Throwables)

-   **功能**: 投掷手榴弹等不消耗数量
-   **默认状态**: ✅ 启用
-   **实现方式**:
    -   修改技能 `onRelease` 行为为 `none`
    -   保持堆叠数量至少为 1

#### 2.4 武器属性倍率调整

-   **射程倍率** (`_rangeMultiplier`): 默认 1.0x
-   **射速倍率** (`_fireRateMultiplier`): 默认 1.0x
-   **弹速倍率** (`_bulletSpeedMultiplier`): 默认 1.0x
-   **伤害倍率** (`_damageMultiplier`): 默认 1.0x

---

### 3. 移动与战斗增强

#### 3.1 移动解锁 (Movement Unlock)

-   **功能**: 允许在任何状态下射击和移动
-   **默认状态**: ✅ 启用
-   **效果**:
    -   冲刺时可以射击
    -   移除射击冷却限制
    -   强制启用冲刺控制

#### 3.2 障碍物穿透 (Obstacle Penetration)

-   **功能**: 子弹穿透墙壁和障碍物
-   **默认状态**: ✅ 启用
-   **实现方式**:
    -   修改弹道碰撞层级掩码
    -   仅检测伤害接收器层
    -   自动管理穿透弹道生命周期

---

### 4. 角色状态增强

#### 4.1 无敌模式 (Invincibility)

-   **功能**: 角色不受伤害
-   **默认状态**: ✅ 启用
-   **实现方式**:
    -   设置角色 `Invincible` 属性为 `true`
    -   自动恢复原始状态

#### 4.2 无限耐力 (Infinite Stamina)

-   **功能**: 耐力永不耗尽
-   **默认状态**: ❌ 禁用
-   **实现方式**:
    -   每帧恢复耐力至最大值

#### 4.3 无限耐久 (Infinite Durability)

-   **功能**: 武器和装备不损耗耐久度
-   **默认状态**: ✅ 启用
-   **实现方式**:
    -   监控手持物品耐久度
    -   自动恢复至最大值

#### 4.4 无生存需求 (No Survival Needs)

-   **功能**: 不需要进食、饮水等
-   **默认状态**: ✅ 启用
-   **实现方式**:
    -   自动恢复饥饿度和口渴度至最大值

---

### 5. 特殊功能

#### 5.1 旋转击杀 (Spin Kill)

-   **功能**: 自动旋转并击杀范围内所有敌人
-   **默认状态**: ❌ 禁用
-   **配置参数**:
    -   `_spinKillSearchRadius`: 搜索半径（默认 15m）
    -   `_spinKillAngularSpeed`: 旋转速度（默认 360°/秒）
-   **特性**:
    -   自动锁定最近敌人
    -   优先选择屏幕内目标
    -   可视化范围指示器（红色圆圈）
    -   自动触发射击
    -   强制启用障碍物穿透

#### 5.2 爆头偏移优化

-   **功能**: 自动调整瞄准点至敌人头部
-   **实现方式**:
    -   缓存每个敌人的头部偏移量
    -   自动清理无效引用
    -   支持不同敌人类型

---

## 🎮 用户界面

### 菜单系统

-   **打开方式**: 游戏内按键（具体按键未在代码中明确）
-   **菜单类型**: Unity IMGUI 系统
-   **功能分区**:
    ```csharp
    enum MenuSection {
        Main,           // 主功能
        Weapons,        // 武器设置
        Movement,       // 移动设置
        Character,      // 角色状态
        Advanced        // 高级选项
    }
    ```

### 可视化元素

1. **自动瞄准范围指示器**

    - 显示检测范围圆圈
    - 可切换显示/隐藏
    - 使用 LineRenderer 渲染

2. **旋转击杀范围指示器**
    - 红色半透明圆圈
    - 96 段圆弧
    - 跟随角色移动

---

## 🔧 技术架构

### 核心类结构

```
ModBehaviour (主类)
├── 自动瞄准系统
│   ├── AutoAimThreadContext (线程上下文)
│   ├── AutoAimThreadResult (计算结果)
│   ├── AutoAimCandidate (候选目标)
│   └── AutoAimThreadTarget (线程目标数据)
│
├── 投掷物追踪
│   ├── GrenadeTracker (追踪器组件)
│   └── GrenadeHomingComponent (追踪逻辑)
│
├── 旋转击杀
│   └── SpinKillCandidateInfo (候选信息)
│
└── 辅助结构
    └── HeadshotInfo (爆头信息)
```

### 事件钩子系统

```csharp
// 角色事件
CharacterMainControl.OnMainCharacterInventoryChangedEvent
ItemAgentHolder.OnHoldAgentChanged
ItemAgent_Gun.OnShootEvent
CharacterSkillKeeper.OnSkillChanged

// 关卡事件
LevelManager.OnAfterLevelInitialized
```

### 多线程架构

-   **工作线程**: `AutoAimWorkerLoop`
-   **更新频率**: 20ms (50 FPS)
-   **线程安全**: 使用 `CancellationToken` 管理生命周期
-   **数据传递**: 通过结构体快照避免竞态条件

---

## 📊 性能优化

### 缓存机制

1. **伤害接收器缓存**

    - 缓存有效期: 单帧
    - 自动扩容机制
    - 空间查询优化

2. **投掷物引用管理**

    - 使用 `WeakReference<T>` 避免内存泄漏
    - 定期清理无效引用
    - 实例 ID 快速查找

3. **弹道追踪**
    - 延迟清理机制（0.2 秒间隔）
    - 快照缓冲区复用
    - 避免重复注册

### 更新频率控制

```csharp
_nextAutoAimThreadStateUpdateTime    // 0.02s (50 FPS)
_nextGrenadeScanTime                 // 0.1s (10 FPS)
_nextProjectileCleanupTime           // 0.2s (5 FPS)
_spinKillNextSearchTime              // 0.05s (20 FPS)
_nextCharacterRetryTime              // 0.2s (5 FPS)
```

---

## 🛠️ 配置与持久化

### 设置保存

-   **方法**: `SaveSettings()` / `LoadSettings()`
-   **存储位置**: 未在代码中明确（可能使用 PlayerPrefs）
-   **保存内容**:
    -   所有功能开关状态
    -   倍率参数
    -   范围和速度设置

---

## ⚠️ 注意事项

### 兼容性

-   **目标框架**: .NET Standard 2.1
-   **Unity 版本**: 需要支持 IMGUI 和 LineRenderer
-   **依赖项**:
    -   `TeamSoda.Duckov.Core.dll`
    -   `ItemStatsSystem.dll`
    -   `UniTask.dll`

### 已知限制

1. **多人游戏**: 可能导致不公平优势或被检测
2. **性能影响**: 旋转击杀模式下 CPU 占用较高
3. **兼容性**: 可能与其他 Mod 冲突

### 安全建议

-   仅在单人模式或私人服务器使用
-   定期备份游戏存档
-   注意游戏更新可能导致功能失效

---

## 📝 开发信息

### 项目结构

```
cheat/
├── CheatMenu.sln                    # 解决方案文件
└── CheatMenu/
    ├── CheatMenu.csproj             # 项目文件
    ├── ModBehaviour.cs              # 主逻辑（6855 行）
    ├── Properties/
    │   └── AssemblyInfo.cs          # 程序集信息
    ├── Microsoft/CodeAnalysis/
    │   └── EmbeddedAttribute.cs     # 编译器属性
    └── System/Runtime/CompilerServices/
        ├── NullableAttribute.cs     # 可空性标注
        └── NullableContextAttribute.cs
```

### 编译配置

-   **Debug**: 完整符号信息，未优化
-   **Release**: 优化编译，PDB 符号文件
-   **输出路径**: `bin/Debug/` 或 `bin/Release/`

### 引用的游戏 DLL

```xml
ItemStatsSystem.dll
TeamSoda.Duckov.Core.dll
UniTask.dll
Unity.TextMeshPro.dll
UnityEngine.CoreModule.dll
UnityEngine.IMGUIModule.dll
UnityEngine.InputLegacyModule.dll
UnityEngine.PhysicsModule.dll
UnityEngine.UI.dll
UnityEngine.UIModule.dll
```

---

## 🔍 代码统计

-   **总行数**: 6855 行
-   **主类**: `ModBehaviour`
-   **嵌套类**: 2 个（`GrenadeTracker`, `GrenadeHomingComponent`）
-   **结构体**: 5 个
-   **枚举**: 1 个
-   **方法数**: 约 150+ 个
-   **字段数**: 约 120+ 个

---

## 📚 相关文档

-   [游戏 API 文档](../codeAnalysis/INDEX.md)
-   [Mod 部署流程](../.kiro/steering/mod-deployment.md)
-   [C# 工作流规范](../.kiro/steering/csharp-workflow.md)

---

_最后更新: 2025-11-09_
_文档版本: 1.0_
