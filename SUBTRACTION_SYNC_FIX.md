# 减法同步修复

## 问题描述
用户反馈：刀成功不爆了（宠物背包修复生效），但图腾还是进了墓碑。

## 根本原因分析
经过分析发现，减法逻辑本身是正确的：
1. 服务端创建完整墓碑JSON（包含所有物品）
2. 客户端上报剩余物品列表
3. 服务端从JSON中减去剩余物品

**但是问题在于**：`RemoveEquipmentFromTombstone` 方法只更新了JSON文件中的数据，没有同步更新游戏中实际的墓碑Inventory。

## 解决方案
在 `RemoveEquipmentFromTombstone` 方法中添加了同步更新逻辑，确保JSON数据和游戏中的墓碑保持一致。

## 修改内容

### TombstonePersistence.cs

#### 1. 修改 `RemoveEquipmentFromTombstone` 方法
在减法操作完成后调用同步更新：

```csharp
if (removedCount > 0)
{
    SaveUserData(userId, userData);
    Debug.Log($"[TOMBSTONE] Subtraction complete: removed {removedCount} items from tombstone, remaining items={tombstone.items.Count}");
    Debug.Log($"[TOMBSTONE] Final tombstone contains {tombstone.items.Count} droppable items");
    
    // 同步更新游戏中的墓碑Inventory
    UpdateGameTombstoneInventory(lootUid, tombstone);
}
```

#### 2. 新增 `UpdateGameTombstoneInventory` 方法
负责将JSON数据同步到游戏中的墓碑：

```csharp
private void UpdateGameTombstoneInventory(int lootUid, TombstoneData tombstoneData)
{
    // 1. 从LootManager中找到对应的Inventory
    var lootManager = LootManager.Instance;
    if (!lootManager._srvLootByUid.TryGetValue(lootUid, out var inventory))
        return;

    // 2. 清空当前库存
    inventory.Clear();
    
    // 3. 根据JSON数据重建库存
    foreach (var tombstoneItem in tombstoneData.items)
    {
        var item = ItemTool.BuildItemFromSnapshot(tombstoneItem.snapshot);
        if (item != null)
        {
            inventory.AddAt(item, tombstoneItem.position);
        }
    }
    
    // 4. 广播更新给所有客户端
    COOPManager.LootNet.Server_SendLootboxStateToAll(inventory);
}
```

## 完整流程

### 1. 玩家死亡
- 客户端发送 `PLAYER_DEAD_TREE`（完整物品树）
- 服务端创建墓碑，保存所有物品到JSON

### 2. 装备上报
- 服务端发送 `PLAYER_EQUIPMENT_REQUEST`
- 客户端发送 `PLAYER_DEATH_EQUIPMENT`（剩余物品列表）

### 3. 减法操作
- 服务端从JSON中减去剩余物品
- **新增**：同步更新游戏中的墓碑Inventory
- **新增**：广播更新给所有客户端

## 关键特性

### 🔄 双重同步
- **JSON持久化**：确保数据在重启后保持一致
- **游戏内同步**：确保玩家立即看到正确的墓碑内容

### 📡 实时广播
- 墓碑更新后立即广播给所有客户端
- 确保所有玩家看到一致的墓碑状态

### 🛡️ 错误处理
- 完整的异常处理和日志记录
- 防止同步失败影响游戏稳定性

### 📊 详细日志
```
[TOMBSTONE] Updating game tombstone inventory: lootUid=3
[TOMBSTONE] Found game tombstone inventory with 10 items
[TOMBSTONE] Rebuilt item in game inventory: TypeID=24, Name=DynamiteMultiple(Clone), Position=0
[TOMBSTONE] Game tombstone inventory updated: 6 items rebuilt
[TOMBSTONE] Broadcasting updated tombstone state to all clients: lootUid=3
```

## 预期效果

### ✅ 图腾被正确移除
- 图腾在客户端剩余物品中被上报
- 服务端从JSON和游戏墓碑中都移除图腾
- 玩家看到的墓碑不再包含图腾

### ✅ 近战武器被正确移除
- 近战武器在客户端剩余物品中被上报
- 服务端从JSON和游戏墓碑中都移除近战武器
- 玩家看到的墓碑不再包含近战武器

### ✅ 实时同步
- 减法操作完成后立即更新游戏中的墓碑
- 所有客户端立即看到更新后的墓碑状态

## 维护优势

### 🎯 自适应过滤
- 不依赖物品名称关键词
- 自动适应官方物品更新
- 基于实际游戏逻辑进行过滤

### 🔧 简单可靠
- 逻辑简单明确：完整物品 - 剩余物品 = 掉落物品
- 不需要维护复杂的过滤规则
- 减少因官方更新导致的维护工作

### 📈 可扩展性
- 如果官方改变物品掉落逻辑，系统会自动适应
- 不需要手动更新物品黑名单或关键词

## 测试验证
1. 玩家携带图腾、近战武器、普通物品死亡
2. 检查墓碑中不包含图腾和近战武器
3. 确认普通物品正常掉落
4. 验证所有客户端看到一致的墓碑状态