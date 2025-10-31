# 不可掉落物品过滤修复

## 问题描述
在玩家死亡时，墓碑中会包含一些本不应该掉落的物品，如：
- **近战武器**：刀、剑、斧头、锤子等
- **图腾**：各种图腾和护身符
- **特殊物品**：初始装备、教程物品、任务物品等

这些物品的存在会：
- 破坏游戏平衡性
- 让玩家获得不应该拥有的物品
- 影响游戏体验的公平性

## 解决方案
在 `TombstonePersistence.cs` 的 `AddTombstone` 方法中添加了智能物品过滤系统，在保存墓碑数据时自动排除不应该掉落的物品。

### 修改文件
**文件**: `EscapeFromDuckovCoopMod/Main/SceneService/TombstonePersistence.cs`

### 核心过滤逻辑

#### 1. 主过滤方法 `ShouldItemDrop`
```csharp
private bool ShouldItemDrop(Item item)
{
    if (item == null) return false;
    
    // 检查是否是近战武器
    if (IsMeleeWeapon(item)) return false;
    
    // 检查是否是图腾或特殊物品
    if (IsTotemOrSpecialItem(item)) return false;
    
    // 检查是否包含不可掉落关键词
    if (HasNonDroppableKeywords(item)) return false;
    
    // 默认允许掉落
    return true;
}
```

#### 2. 近战武器检测 `IsMeleeWeapon`
- **组件检测**：检查是否有 `ItemAgent_MeleeWeapon` 组件
- **名称匹配**：检查物品名称是否包含近战武器关键词
  - 英文：knife, sword, axe, hammer, blade, melee
  - 中文：刀, 剑, 斧, 锤

#### 3. 图腾和特殊物品检测 `IsTotemOrSpecialItem`
- **图腾关键词**：totem, 图腾, charm, amulet, talisman
- **TypeID黑名单**：支持添加特定的不可掉落物品ID
- **可扩展设计**：便于添加新的特殊物品类型

#### 4. 关键词过滤 `HasNonDroppableKeywords`
过滤包含以下关键词的物品：
- **英文**：starter, default, basic, tutorial, quest, mission
- **中文**：初始, 默认, 基础, 教程, 任务, 剧情

### 详细日志记录

#### 过滤统计
```
[TOMBSTONE] Filtered 2 non-droppable items out of 8 total items
```

#### 具体物品过滤
```
[TOMBSTONE] Filtering melee weapon: TypeID=1001, Name=BasicKnife
[TOMBSTONE] Filtering totem/special item: TypeID=2001, Name=LuckTotem
[TOMBSTONE] Filtered non-droppable item: TypeID=1001, Name=BasicKnife
```

## 关键特性

### 🛡️ 多层过滤
- **组件检测**：通过Unity组件识别物品类型
- **名称匹配**：支持中英文关键词匹配
- **ID黑名单**：支持精确的TypeID过滤
- **关键词过滤**：过滤特殊用途的物品

### 🔧 可扩展性
- **模块化设计**：每种过滤规则独立实现
- **易于维护**：新增过滤规则只需修改对应方法
- **配置友好**：关键词和ID列表易于调整

### 📊 统计信息
- **过滤计数**：显示过滤了多少物品
- **详细日志**：记录每个被过滤物品的信息
- **调试友好**：便于验证过滤效果

### 🔒 安全处理
- **异常保护**：所有检测方法都有异常处理
- **保守策略**：出错时默认允许掉落，避免丢失重要物品
- **空值检查**：防止空引用异常

## 过滤规则详解

### 近战武器识别
1. **组件检测**：`ItemAgent_MeleeWeapon` 组件
2. **名称关键词**：
   - 英文：knife, sword, axe, hammer, blade, melee
   - 中文：刀, 剑, 斧, 锤

### 图腾和特殊物品
1. **图腾关键词**：totem, 图腾, charm, amulet, talisman
2. **TypeID黑名单**：可配置的特定物品ID列表
3. **扩展性**：支持添加更多特殊物品类型

### 系统物品过滤
1. **初始装备**：starter, default, basic, 初始, 默认, 基础
2. **教程物品**：tutorial, quest, mission, 教程, 任务, 剧情

## 使用示例

### 添加新的不可掉落物品类型
```csharp
// 在 IsTotemOrSpecialItem 方法中添加新的关键词
var specialKeywords = new[] { "artifact", "relic", "神器", "遗物" };

// 或添加特定的TypeID
var nonDroppableTypeIds = new HashSet<int> { 1001, 1002, 2001, 2002 };
```

### 调整过滤严格程度
可以通过修改关键词列表来调整过滤的严格程度，或者添加新的检测逻辑。

## 预期效果

### ✅ 游戏平衡
- 防止玩家获得不应该掉落的近战武器
- 避免图腾等特殊物品的不当流通
- 维护游戏经济平衡

### ✅ 体验优化
- 墓碑中只包含合理的掉落物品
- 减少无意义的物品堆积
- 提高战利品的价值感

### ✅ 系统稳定
- 防止特殊物品导致的游戏问题
- 避免不当物品的复制或传播
- 保持游戏状态的一致性

## 向后兼容性
此修复只影响新创建的墓碑，不会影响已存在的墓碑数据。过滤逻辑是在保存时执行的，不会影响游戏的其他部分。

## 调试和监控
通过日志可以清楚地看到：
- 哪些物品被过滤了
- 过滤的原因（近战武器/图腾/关键词）
- 过滤的统计信息

这有助于验证过滤效果和调整过滤规则。