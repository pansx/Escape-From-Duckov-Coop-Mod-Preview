# 坟墓无法拿物品问题修复

## 问题描述
在修复了坟墓重叠和用户ID获取问题后，出现了新的问题：**坟墓无法拿物品**，客机显示 `[LOOT] 请求被拒绝：no_inv`。

## 问题分析

### 错误原因
`no_inv` 错误表示服务端找不到对应的 Inventory。从代码分析发现：

1. **客机请求物品时**：服务端首先通过 `lootUid` 在 `_srvLootByUid` 字典中查找 Inventory
2. **坟墓恢复时**：只注册到了 `InteractableLootbox.Inventories` 字典（通过 posKey），但没有注册到 `_srvLootByUid` 字典
3. **查找失败**：服务端找不到对应的 Inventory，返回 `no_inv` 错误

### 代码流程
```csharp
// 客机请求物品时的服务端逻辑
Inventory inv = null;
if (lootUid >= 0) LootManager.Instance._srvLootByUid.TryGetValue(lootUid, out inv);
if (inv == null && !LootManager.Instance.TryResolveLootById(scene, posKey, iid, out inv))
{
    Server_SendLootDeny(peer, "no_inv");  // ← 这里失败了
    return;
}
```

## 修复方案

### 在坟墓恢复时同时注册到两个字典

**修复前**：只注册到 posKey 字典
```csharp
// 注册到InteractableLootbox.Inventories字典
var dict = InteractableLootbox.Inventories;
if (dict != null)
{
    var posKey = ComputeLootKey(lootbox.transform);
    dict[posKey] = inv;
    Debug.Log($"[TOMBSTONE] Registered tombstone inventory with posKey: {posKey}");
}
```

**修复后**：同时注册到 lootUid 字典
```csharp
// 注册到InteractableLootbox.Inventories字典
var dict = InteractableLootbox.Inventories;
if (dict != null)
{
    var posKey = ComputeLootKey(lootbox.transform);
    dict[posKey] = inv;
    Debug.Log($"[TOMBSTONE] Registered tombstone inventory with posKey: {posKey}");
}

// 注册到_srvLootByUid字典，这样客机可以通过lootUid直接找到
if (tombstone.lootUid >= 0)
{
    _srvLootByUid[tombstone.lootUid] = inv;
    Debug.Log($"[TOMBSTONE] Registered tombstone inventory with lootUid: {tombstone.lootUid}");
}
```

## 工作原理

### 双重注册机制
1. **posKey 注册**：用于场景位置查找，兼容原有逻辑
2. **lootUid 注册**：用于直接 UID 查找，提高查找效率

### 查找优先级
1. **首选**：通过 `lootUid` 在 `_srvLootByUid` 中直接查找（快速）
2. **备选**：通过 `TryResolveLootById` 使用 posKey 查找（兼容）

## 预期效果
- ✅ 客机可以正常从坟墓中取物品
- ✅ 不再出现 `[LOOT] 请求被拒绝：no_inv` 错误
- ✅ 保持与原有系统的兼容性
- ✅ 提高坟墓物品查找效率

## 调试信息
修复后会看到以下日志：
```
[TOMBSTONE] Registered tombstone inventory with posKey: [数字]
[TOMBSTONE] Registered tombstone inventory with lootUid: [数字]
```

## 文件位置
- `EscapeFromDuckovCoopMod/Main/SceneService/LootManager.cs` (修复)

## 测试验证
- 坟墓恢复后应该能正常取物品
- 不再出现 `no_inv` 错误
- 物品取出后坟墓数据正确更新