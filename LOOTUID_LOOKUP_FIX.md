# LootUid查找修复

## 问题描述
主机拿取了多个墓碑，没有和客机一样的log，甚至任何log都没有。

## 根本原因分析
在 `InventoryPatch.cs` 中添加的墓碑更新逻辑没有被触发，原因是：

1. **错误的lootUid参数**：传入了 `lootUid = -1`
2. **被条件检查拦截**：`UpdateTombstoneByLootUid` 方法开头有检查：
   ```csharp
   if (!IsServer || lootUid < 0)
   {
       return; // 直接返回，不执行任何逻辑
   }
   ```
3. **没有日志输出**：因为方法直接返回，所以没有任何日志

## 解决方案
修改 `InventoryPatch.cs` 中的逻辑，从 `LootManager._srvLootByUid` 中查找正确的 `lootUid`。

## 修改内容

### InventoryPatch.cs - RemoveAt后处理

**修改前**：
```csharp
// 更新墓碑持久化数据（如果这是墓碑容器）
try
{
    TombstonePersistence.Instance?.UpdateTombstoneByLootUid(-1, __instance);
    Debug.Log($"[TOMBSTONE] Updated tombstone after direct inventory RemoveAt");
}
catch (Exception e)
{
    Debug.LogError($"[TOMBSTONE] Failed to update tombstone after RemoveAt: {e}");
}
```

**修改后**：
```csharp
// 更新墓碑持久化数据（如果这是墓碑容器）
try
{
    // 从LootManager中查找对应的lootUid
    var lootUid = -1;
    if (LootManager.Instance != null && LootManager.Instance._srvLootByUid != null)
    {
        foreach (var kv in LootManager.Instance._srvLootByUid)
        {
            if (kv.Value == __instance)
            {
                lootUid = kv.Key;
                break;
            }
        }
    }
    
    if (lootUid >= 0)
    {
        TombstonePersistence.Instance?.UpdateTombstoneByLootUid(lootUid, __instance);
        Debug.Log($"[TOMBSTONE] Updated tombstone after direct inventory RemoveAt: lootUid={lootUid}");
    }
    else
    {
        Debug.Log($"[TOMBSTONE] No lootUid found for inventory, skipping tombstone update");
    }
}
catch (Exception e)
{
    Debug.LogError($"[TOMBSTONE] Failed to update tombstone after RemoveAt: {e}");
}
```

## 查找逻辑

### LootUid反向查找
```csharp
// 遍历服务端的lootUid映射表
foreach (var kv in LootManager.Instance._srvLootByUid)
{
    if (kv.Value == __instance) // 找到匹配的Inventory
    {
        lootUid = kv.Key;        // 获取对应的lootUid
        break;
    }
}
```

### 条件检查
- **有效lootUid**：`lootUid >= 0` 时执行更新
- **无效lootUid**：`lootUid < 0` 时跳过更新并记录日志

## 预期日志输出

### 成功找到lootUid
```
[TOMBSTONE] Updated tombstone after direct inventory RemoveAt: lootUid=1
[TOMBSTONE] UpdateTombstoneByLootUid called with lootUid: 1
[TOMBSTONE] Scanning 3 tombstone files for lootUid 1
[TOMBSTONE] Found tombstone with lootUid 1 in user Client:xxx's file: user_xxx_tombstones.json
[TOMBSTONE] Updated tombstone items: userId=Client:xxx, lootUid=1
```

### 未找到lootUid
```
[TOMBSTONE] No lootUid found for inventory, skipping tombstone update
```

### 异常情况
```
[TOMBSTONE] Failed to update tombstone after RemoveAt: [异常信息]
```

## 关键特性

### 🔍 智能查找
- **反向查找**：通过Inventory实例查找对应的lootUid
- **精确匹配**：使用引用相等性确保准确性
- **高效查找**：找到匹配项后立即退出循环

### 🛡️ 安全保护
- **空值检查**：检查LootManager和映射表是否存在
- **条件验证**：只有有效的lootUid才执行更新
- **异常处理**：完整的try-catch保护

### 📊 详细日志
- **成功日志**：显示找到的lootUid
- **跳过日志**：说明为什么跳过更新
- **错误日志**：记录异常信息

## 预期效果

### ✅ 正确的lootUid
- 从映射表中查找真实的lootUid
- 不再使用无效的-1值
- 通过UpdateTombstoneByLootUid的条件检查

### ✅ 完整的日志输出
- 主机操作墓碑时会有日志输出
- 可以追踪墓碑更新过程
- 便于调试和验证

### ✅ JSON正确更新
- 主机直接操作墓碑时JSON会更新
- 重启后墓碑状态保持正确
- 避免物品重复出现

## 编译状态
✅ **编译成功** - lootUid查找逻辑已实现

## 测试建议
1. 主机直接从墓碑中拿取物品
2. 检查控制台是否有更新日志
3. 验证JSON文件是否正确更新
4. 重启游戏确认墓碑状态
5. 对比客机操作的日志输出

## 总结
这个修复解决了lootUid查找的问题，确保了：
- **正确的参数传递**
- **完整的日志输出**
- **有效的墓碑更新**
- **持久化的正确性**

现在主机操作墓碑时应该能看到和客机一样的日志输出了。