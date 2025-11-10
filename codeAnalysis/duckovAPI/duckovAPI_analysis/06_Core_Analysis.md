# TeamSoda.Duckov.Core 模块分析

## 概述

Core 模块是游戏的核心系统，包含游戏管理、角色控制、生命值系统等关键功能。这是游戏运行时的主要逻辑层。

## 核心组件

### GameManager (游戏管理器)

- **功能**：全局游戏状态管理和系统协调

- **单例模式**：使用静态实例确保全局唯一性

- **生命周期**：DontDestroyOnLoad 确保跨场景持久化

- **管理的系统**：- `AudioManager`: 音频系统

  - `UIInputManager`: UI输入管理

  - `PauseMenu`: 暂停菜单

  - `SceneLoader`: 场景加载

  - `ModManager`: 模组管理

  - `AchievementManager`: 成就系统

#### 关键特性

- 自动资源加载和实例化

- 暂停状态管理

- 全局系统访问接口

- DOTween 配置管理

### Health (生命值系统)

- **功能**：角色生命值、护甲、元素抗性管理

- **核心属性**：- `MaxHealth`: 最大生命值

  - `CurrentHealth`: 当前生命值

  - `BodyArmor/HeadArmor`: 身体/头部护甲

  - `ElementFactor`: 元素抗性系数

#### 伤害计算系统

- **护甲计算**：`伤害减免 = 2 / (护甲值 - 穿甲值 + 2)`

- **暴击系统**：头部暴击使用头部护甲

- **元素伤害**：支持多种元素类型和抗性

- **Buff 系统**：流血、骨折、中毒等状态效果

#### 天气影响

- 雨天降低火焰伤害 15%

- 雨天增加电击伤害 20%

- 基地场景不受天气影响

### CharacterMainControl (角色主控制器)

- **功能**：角色行为的核心控制系统

- **组件架构**：- `Movement`: 移动控制

  - `ItemAgentHolder`: 物品代理持有

  - `CharacterActions`: 各种行为动作

  - `EquipmentController`: 装备控制

#### 行为系统 (Action System)

- **优先级机制**：高优先级动作可以打断低优先级

- **动作类型**：- `AttackAction`: 攻击动作

  - `ReloadAction`: 装弹动作

  - `SkillAction`: 技能动作

  - `InteractAction`: 交互动作

  - `DashAction`: 冲刺动作

#### 瞄准系统

- **瞄准类型**：- `normalAim`: 普通瞄准

  - `characterSkill`: 角色技能瞄准

  - `handheldSkill`: 手持物品技能瞄准

- **ADS系统**：机械瞄具支持

- **射程计算**：根据武器类型动态计算

#### 生存系统

- **体力系统**：跑步消耗，自动恢复

- **饥渴系统**：能量和水分管理

- **负重系统**：根据负重影响移动速度

- **状态效果**：饥饿、口渴、负重等 Buff

## 设计模式

### 单例模式

- `GameManager` 确保全局唯一实例

- 自动资源加载和错误处理

### 组件模式

- `CharacterMainControl` 通过组件组合实现复杂功能

- 各个子系统独立管理，松耦合设计

### 事件驱动

- 大量事件用于系统间通信

- `OnHurt`, `OnDead`, `OnActionStart` 等生命周期事件

### 状态机模式

- 角色动作系统使用状态机管理

- 优先级和转换规则明确定义

## 核心机制

### 伤害系统

```csharp
// 护甲减伤计算
float armorReduction = 2f / (armor - armorPiercing + 2f);

finalDamage = baseDamage * armorReduction;

// 元素伤害计算
foreach(ElementFactor factor in damageInfo.elementFactors)
{
    float elementDamage = baseDamage * factor.factor * ElementFactor(factor.elementType);

    totalDamage += elementDamage;
}

```

### 动作优先级系统

- 每个动作有优先级值

- 高优先级可以打断低优先级

- 确保重要动作（如受伤、死亡）优先执行

### 物品代理系统

- `ItemAgent` 为物品提供行为逻辑

- 枪械、近战武器、技能物品等不同代理

- 统一的触发和更新接口

## 性能优化

### 缓存机制

- 角色引用缓存避免重复查找

- 哈希值缓存加速属性访问

- 统计数据缓存减少计算

### 事件优化

- 事件订阅/取消订阅管理

- 避免内存泄漏和重复订阅

### 更新优化

- 分帧更新不同系统

- 距离检查减少不必要计算

- 主角特殊处理优化

## 扩展性设计

### 模块化架构

- 各系统相对独立

- 通过接口和事件通信

- 便于添加新功能

### 配置驱动

- 大量数值通过配置文件管理

- 支持运行时调整和模组修改

### Buff 系统

- 可扩展的状态效果系统

- 支持自定义效果和触发条件

## 开发建议

### 角色系统扩展

- 继承 `CharacterActionBase` 添加新动作

- 实现 `IUpdatable` 接口添加定期更新逻辑

- 使用事件系统进行系统间通信

### 性能考虑

- 合理使用对象池减少 GC 压力

- 避免在 Update 中进行复杂计算

- 使用协程处理异步操作

### 调试支持

- 完善的日志系统

- 可视化调试信息

- 运行时参数调整
