# 墓碑JSON更新修复

## 问题描述
1. **物品掉落问题**：设置 `_applyingLootState` 导致客机也看到掉落，需要撤销
2. **JSON不更新问题**：主机获取墓碑里的道具不更新JSON，导致重启后物品又出现

## 修复内容

### 1. 撤销双重标志保护
撤销了 `_applyingLootState` 的设置，只保留 `_serverApplyingLoot`：

```csharp
// 只设置服务端标志，避免影响客户端
COOPManager.LootNet._serverApplyingLoot = true;

try
{
    // 清空和重建库存操作
}
finally
{
    COOPManager.LootNet._serverApplyingLoot = false;
}
```

### 2. 添加直接库存操作的墓碑更新
在 `InventoryPatch.cs` 的 `RemoveAt` 后处理中添加墓碑更新：

```csharp
DeferedRunner.EndOfFrame(() =>
{
    // 只处理战利品容器，跳过玩家仓库/宠物包等私有库存
    if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;
    COOPManager.LootNet.Server_SendLootboxState(null, __instance); // 广播给所有客户端
    
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
});
```

## 问题分析

### 物品掉落问题
- `_applyingLootState` 标志会影响客户端的行为
- 设置此标志导致客机也看到物品掉落
- 只使用 `_serverApplyingLoot` 可以避免影响客户端

### JSON更新问题
主机可能通过以下方式操作墓碑：
1. **网络操作**：通过 `LootNet.cs` 的网络请求（已有更新逻辑）
2. **直接操作**：主机直接通过UI操作库存（缺少更新逻辑）

之前只有网络操作会更新JSON，直接操作不会更新，导致：
- 主机拿走物品后JSON文件没有更新
- 重启游戏后物品又出现在墓碑中

## 更新机制

### 现有的更新调用
1. **LootNet.cs - Server_OnLootPut**：放入物品后更新
2. **LootNet.cs - Server_OnLootTake**：网络拿取后更新
3. **新增 - InventoryPatch.cs**：直接库存操作后更新

### UpdateTombstoneByLootUid 方法
```csharp
public void UpdateTombstoneByLootUid(int lootUid, Inventory inventory)
{
    // 遍历所有用户的墓碑数据
    // 找到匹配的lootUid
    // 更新对应的墓碑物品数据
    // 保存到JSON文件
}
```

## 关键特性

### 🔄 完整覆盖
- **网络操作**：通过LootNet的网络请求
- **直接操作**：通过InventoryPatch的库存操作
- **自动检测**：`UpdateTombstoneByLootUid` 自动查找对应用户

### 🛡️ 安全保护
- **异常处理**：所有更新操作都有try-catch保护
- **条件检查**：只处理战利品容器，跳过私有库存
- **延迟执行**：使用 `DeferedRunner.EndOfFrame` 避免冲突

### 📊 详细日志
```
[TOMBSTONE] Updated tombstone after direct inventory RemoveAt
[TOMBSTONE] Updated tombstone after item put: userId=Client:xxx, lootUid=1
[TOMBSTONE] Updated tombstone after item taken: lootUid=1
```

## 预期效果

### ✅ 物品掉落控制
- 只有服务端标志，不影响客户端
- 主机不会看到物品掉落（在大多数情况下）
- 客机正常显示

### ✅ JSON持久化
- 主机直接操作墓碑时也会更新JSON
- 重启游戏后墓碑状态保持正确
- 所有操作路径都有更新覆盖

### ✅ 系统稳定性
- 异常保护确保系统稳定
- 延迟执行避免操作冲突
- 自动检测减少手动配置

## 编译状态
✅ **编译成功** - 修复已实现

## 测试建议
1. 主机直接从墓碑中拿取物品
2. 检查JSON文件是否正确更新
3. 重启游戏验证墓碑状态
4. 确认物品掉落情况
5. 验证网络同步正常

## 总结
这个修复解决了两个关键问题：
1. **撤销了会影响客户端的标志设置**
2. **添加了直接库存操作的墓碑更新机制**

确保了墓碑系统的完整性和持久化的正确性。