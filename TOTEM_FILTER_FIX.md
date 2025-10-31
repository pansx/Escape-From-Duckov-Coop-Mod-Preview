# 图腾过滤修复

## 问题描述
用户反馈：刀成功不爆了（之前的宠物背包修复生效），但图腾还是进了墓碑。

## 根本原因
在 `PLAYER_DEAD_TREE` 消息处理中，服务端直接使用客户端发送的完整物品树创建墓碑，没有任何过滤逻辑。这导致所有物品（包括图腾、近战武器等不应该掉落的物品）都被添加到墓碑中。

## 解决方案
在 `TombstonePersistence.cs` 中添加了完整的物品过滤系统，并在 `Mod.cs` 的 `PLAYER_DEAD_TREE` 处理逻辑中集成了过滤功能。

## 修改文件

### 1. TombstonePersistence.cs
添加了以下过滤方法：

#### `FilterDroppableItems(Inventory inventory)`
- 主过滤方法，遍历库存中的所有物品
- 调用 `ShouldItemDrop` 判断每个物品是否应该掉落
- 返回过滤后的可掉落物品列表
- 提供详细的过滤统计日志

#### `ShouldItemDrop(Item item)`
- 核心判断逻辑，决定物品是否应该掉落
- 调用三个专门的检查方法：
  - `IsMeleeWeapon` - 检查近战武器
  - `IsTotemOrSpecialItem` - 检查图腾和特殊物品
  - `HasNonDroppableKeywords` - 检查系统物品关键词

#### `IsMeleeWeapon(Item item)`
- 检查 `ItemAgent_MeleeWeapon` 组件
- 匹配近战武器关键词：
  - 英文：knife, sword, axe, hammer, blade, melee
  - 中文：刀, 剑, 斧, 锤

#### `IsTotemOrSpecialItem(Item item)`
- 匹配图腾关键词：
  - 英文：totem, charm, amulet, talisman
  - 中文：图腾, 护身符, 符咒
- 支持 TypeID 黑名单（可扩展）

#### `HasNonDroppableKeywords(Item item)`
- 匹配系统物品关键词：
  - 英文：starter, default, basic, tutorial, quest, mission
  - 中文：初始, 默认, 基础, 教程, 任务, 剧情

### 2. Mod.cs
在 `PLAYER_DEAD_TREE` 消息处理中添加过滤逻辑：

```csharp
// 过滤不可掉落的物品
Debug.Log("[TOMBSTONE] Filtering non-droppable items from death inventory");
var droppableItems = TombstonePersistence.FilterDroppableItems(inventory);

// 清空原始库存并只添加可掉落的物品
inventory.Clear();
foreach (var droppableItem in droppableItems)
{
    inventory.Add(droppableItem);
}

Debug.Log($"[TOMBSTONE] After filtering: {inventory.Content.Count} droppable items remain");
```

## 过滤规则

### ❌ 不可掉落物品
1. **近战武器**：所有刀、剑、斧、锤等近战武器
2. **图腾类物品**：所有图腾、护身符、符咒等特殊物品
3. **系统物品**：初始装备、教程物品、任务物品等

### ✅ 可掉落物品
1. **远程武器**：枪械、弓箭等（如果不在黑名单中）
2. **消耗品**：药品、食物、弹药等
3. **材料物品**：制作材料、资源等
4. **装备物品**：护甲、配件等（如果不在黑名单中）

## 日志输出示例

### 过滤开始
```
[TOMBSTONE] Filtering non-droppable items from death inventory
[TOMBSTONE] Starting item filtering for inventory with 8 slots
```

### 物品过滤详情
```
[TOMBSTONE] Filtering totem/special item: TypeID=2001, Name=LuckTotem
[TOMBSTONE] Filtering melee weapon: TypeID=1001, Name=BasicKnife
[TOMBSTONE] Item allowed to drop: TypeID=3001, Name=HealthPotion
[TOMBSTONE] Filtered non-droppable item: TypeID=2001, Name=LuckTotem
```

### 过滤结果
```
[TOMBSTONE] Item filtering complete: 3 droppable items out of 8 total items (5 filtered)
[TOMBSTONE] After filtering: 3 droppable items remain
```

## 特性

### 🛡️ 多层防护
- **组件检测**：通过 Unity 组件识别物品类型
- **名称匹配**：支持中英文关键词匹配
- **ID黑名单**：支持精确的 TypeID 过滤
- **关键词过滤**：过滤特殊用途的物品

### 🔧 可扩展性
- **模块化设计**：每种过滤规则独立实现
- **易于维护**：新增过滤规则只需修改对应方法
- **配置友好**：关键词和ID列表易于调整

### 📊 详细日志
- **过滤统计**：显示过滤了多少物品
- **物品详情**：记录每个被过滤物品的信息
- **调试友好**：便于验证过滤效果

### 🔒 安全处理
- **异常保护**：所有检测方法都有异常处理
- **保守策略**：出错时默认允许掉落，避免丢失重要物品
- **空值检查**：防止空引用异常

## 预期效果
1. **图腾不再掉落**：所有图腾类物品将被过滤，不会出现在墓碑中
2. **近战武器不再掉落**：刀、剑、斧、锤等近战武器将被过滤
3. **系统物品不再掉落**：初始装备、教程物品等将被过滤
4. **普通物品正常掉落**：药品、弹药、材料等仍会正常掉落到墓碑中

## 向后兼容性
- 此修复只影响新创建的墓碑
- 不会影响已存在的墓碑数据
- 过滤逻辑在服务端执行，客户端无需修改

## 扩展方法
如需添加新的不可掉落物品类型：

1. **添加关键词**：在相应的关键词数组中添加新词汇
2. **添加TypeID**：在 `nonDroppableTypeIds` 集合中添加特定ID
3. **添加新检查方法**：创建新的检查方法并在 `ShouldItemDrop` 中调用

## 测试建议
1. 携带图腾、近战武器、普通物品死亡
2. 检查墓碑中只包含普通物品
3. 查看日志确认过滤统计正确
4. 验证没有异常或错误日志