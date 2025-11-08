# ItemStatsSystem 模块分析

## 概述

ItemStatsSystem 是游戏的核心物品系统，负责管理物品、库存、属性、效果等核心游戏机制。

## 核心架构

### 主要类结构

#### Item (物品核心类)

- **功能**：游戏中所有物品的基础类

- **关键属性**：- `TypeID`: 物品类型标识符

  - `DisplayName`: 显示名称

  - `StackCount`: 堆叠数量

  - `Durability`: 耐久度系统

  - `Weight`: 重量系统

  - `Quality`: 品质等级

- **核心系统**：- 堆叠系统 (Stackable)

  - 耐久度系统 (UseDurability)

  - 标签系统 (Tags)

  - 变量系统 (Variables/Constants)

  - 父子关系管理

#### Inventory (库存系统)

- **功能**：管理物品容器和存储

- **关键特性**：- 容量管理 (Capacity)

  - 位置锁定 (LockedIndexes)

  - 自动排序功能

  - 重量计算

  - 检查状态管理

#### Stat (属性系统)

- **功能**：管理物品的数值属性

- **修饰符系统**：- Add: 直接加法

  - PercentageAdd: 百分比加法

  - PercentageMultiply: 百分比乘法

- **计算顺序**：按 Order 值排序执行

#### Effect (效果系统)

- **组件化设计**：- EffectTrigger: 触发条件

  - EffectFilter: 过滤条件

  - EffectAction: 执行动作

- **事件驱动**：基于物品树变化触发

#### Slot (插槽系统)

- **功能**：管理物品的装备位置

- **特性**：- 标签过滤 (RequireTags/ExcludeTags)

  - 同 ID 物品限制

  - 自动合并堆叠物品

## 数据结构

### Data 子系统

- `InventoryData`: 库存序列化数据

- `ItemTreeData`: 物品树序列化数据

### Items 子系统

- `Slot`: 插槽实现

- `SlotCollection`: 插槽集合管理

### Stats 子系统

- `Modifier`: 属性修饰符

- `ModifierType`: 修饰符类型枚举

## 设计模式

### 组合模式

- Item 可以包含 Inventory 和 SlotCollection

- 形成复杂的物品树结构

### 观察者模式

- 大量事件系统用于状态同步

- `onItemTreeChanged`, `onContentChanged` 等

### 策略模式

- Effect 系统的 Trigger/Filter/Action 组合

- 不同的使用行为 (UsageUtilities)

### 工厂模式

- ItemAgent 系统用于创建特定行为

## 关键机制

### 物品树系统

- 物品可以嵌套包含其他物品

- 自动计算总重量和价值

- 树状结构的事件传播

### 堆叠与合并

- 相同类型物品自动合并

- 支持部分合并和拆分

- 堆叠限制检查

### 耐久度系统

- 最大耐久度和当前耐久度

- 耐久度损失计算

- 影响物品效果激活

### 标签过滤系统

- 基于标签的物品分类

- 插槽兼容性检查

- 效果触发条件

## 本地化支持

- 使用 LocalizationKey 属性

- 支持多语言显示名称和描述

- 统一的本地化键值管理

## 开发建议

### 扩展性

- 通过继承 EffectAction 添加新效果

- 使用 CustomData 系统存储自定义属性

- 标签系统便于分类管理

### 性能优化

- 缓存系统减少重复计算

- 事件系统避免轮询

- 延迟加载和异步操作

### 数据完整性

- 完善的验证系统 (ISelfValidator)

- 自动修复常见配置错误

- 详细的错误日志
