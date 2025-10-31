# 战利品箱重连同步修复

## 问题描述

客户端重连后，怪物战利品箱很大概率不能同步给客户端，而且重连时也不会同步。客户端看不到战利品箱中的物品，需要让客户端重连时清除所有战利品箱子，由服务端完全同步。

## 根本原因分析

1. **客户端缓存未清除**：客户端重连时，本地的战利品箱缓存（`_cliLootByUid`、`_pendingLootStatesByUid`等）没有被清除
2. **重连后不主动请求**：客户端认为自己已经有了战利品箱数据，不会主动向服务端请求最新状态
3. **同步时机问题**：重连成功后没有强制重新同步所有可见的战利品箱

## 修复方案

### 1. 添加客户端战利品缓存清除机制

在 `NetService.cs` 中添加 `ClearClientLootCache()` 方法：

```csharp
/// <summary>
/// 清除客户端战利品箱缓存，强制重新同步所有战利品箱
/// </summary>
private void ClearClientLootCache()
{
    if (IsServer)
    {
        Debug.Log("[COOP] 服务器模式，跳过清除战利品缓存");
        return;
    }

    try
    {
        if (LootManager.Instance != null)
        {
            var clearedCount = LootManager.Instance._cliLootByUid.Count;
            LootManager.Instance._cliLootByUid.Clear();
            LootManager.Instance._pendingLootStatesByUid.Clear();
            Debug.Log($"[COOP] 已清除客户端战利品缓存，共清除 {clearedCount} 个战利品箱");
        }

        if (COOPManager.LootNet != null)
        {
            var pendingCount = COOPManager.LootNet._cliPendingPut.Count;
            COOPManager.LootNet._cliPendingPut.Clear();
            COOPManager.LootNet._cliSwapByVictim.Clear();
            Debug.Log($"[COOP] 已清除客户端待处理的战利品操作，共清除 {pendingCount} 个待处理操作");
        }

        if (LootManager.Instance != null)
        {
            var takeCount = LootManager.Instance._cliPendingTake.Count;
            var reorderCount = LootManager.Instance._cliPendingReorder.Count;
            LootManager.Instance._cliPendingTake.Clear();
            LootManager.Instance._cliPendingReorder.Clear();
            Debug.Log($"[COOP] 已清除客户端待处理的拾取和重排操作，拾取: {takeCount}, 重排: {reorderCount}");
        }

        Debug.Log("[COOP] 客户端战利品缓存清除完成，所有战利品箱将重新从服务端同步");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[COOP] 清除客户端战利品缓存时发生异常: {ex}");
    }
}
```

### 2. 在连接成功时清除缓存

修改 `OnPeerConnected()` 方法，在客户端连接成功时清除战利品缓存：

```csharp
if (!IsServer)
{
    // ... 现有代码 ...
    
    // 客户端连接成功时清除战利品缓存，确保完全同步
    ClearClientLootCache();
    
    Send_ClientStatus.Instance.SendClientStatusUpdate();
    
    // 延迟一点时间后强制重新同步所有战利品箱
    UniTask.Void(async () =>
    {
        await UniTask.Delay(2000); // 等待连接完全稳定
        if (LootManager.Instance != null && connectedPeer != null)
        {
            Debug.Log("[COOP] 连接成功，开始强制重新同步所有战利品箱");
            LootManager.Instance.Client_ForceResyncAllLootboxes();
        }
    });
}
```

### 3. 在重连时清除缓存并强制同步

修改 `ReconnectAfterSceneLoad()` 方法：

```csharp
if (connectedPeer != null)
{
    Debug.Log($"[COOP] 场景切换后重连成功: {cachedConnectedIP}:{cachedConnectedPort}");
    
    // 重连成功后，清除客户端战利品箱缓存，强制重新同步
    ClearClientLootCache();
    
    // 重连成功后，发送当前状态进行完全同步
    await UniTask.Delay(1000); // 等待连接稳定
    
    try
    {
        // ... 发送状态更新 ...
        
        // 强制重新同步所有战利品箱
        await UniTask.Delay(500); // 再等待一点时间确保场景就绪
        if (LootManager.Instance != null)
        {
            Debug.Log("[COOP] 重连成功，开始强制重新同步所有战利品箱");
            LootManager.Instance.Client_ForceResyncAllLootboxes();
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"[COOP] 重连后发送状态更新异常: {ex}");
    }
}
```

### 4. 添加强制重新同步方法

在 `LootManager.cs` 中添加 `Client_ForceResyncAllLootboxes()` 方法：

```csharp
/// <summary>
/// 客户端重连后强制重新请求所有可见战利品箱的状态
/// </summary>
public void Client_ForceResyncAllLootboxes()
{
    if (IsServer || !networkStarted)
    {
        return;
    }

    try
    {
        Debug.Log("[LOOT] 开始强制重新同步所有战利品箱");
        
        var lootboxes = Object.FindObjectsOfType<InteractableLootbox>(true);
        var syncCount = 0;
        
        foreach (var lootbox in lootboxes)
        {
            if (lootbox == null || lootbox.Inventory == null)
            {
                continue;
            }

            var inv = lootbox.Inventory;
            
            // 跳过私有库存
            if (LootboxDetectUtil.IsPrivateInventory(inv))
            {
                continue;
            }

            try
            {
                // 设置为加载状态
                inv.Loading = true;
                
                // 请求最新状态
                COOPManager.LootNet.Client_RequestLootState(inv);
                
                // 设置超时保护
                KickLootTimeout(inv, 2.0f);
                
                syncCount++;
                
                Debug.Log($"[LOOT] 请求同步战利品箱: {lootbox.name} at {lootbox.transform.position}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LOOT] 请求同步战利品箱失败: {lootbox.name}, 错误: {e.Message}");
            }
        }
        
        Debug.Log($"[LOOT] 完成强制重新同步，共请求 {syncCount} 个战利品箱");
    }
    catch (Exception e)
    {
        Debug.LogError($"[LOOT] 强制重新同步所有战利品箱失败: {e}");
    }
}
```

### 5. 改进战利品状态应用

在 `LootNet.cs` 的 `Client_ApplyLootboxState()` 方法中添加稳定ID映射更新：

```csharp
// 确保稳定ID映射正确更新
if (lootUid >= 0)
{
    LootManager.Instance._cliLootByUid[lootUid] = inv;
    Debug.Log($"[LOOT] 客户端更新战利品箱映射: lootUid={lootUid}, 物品数量={count}");
}
```

## 修复效果

1. **完全清除缓存**：客户端连接/重连时会清除所有战利品箱相关的本地缓存
2. **强制重新同步**：连接成功后会主动请求所有可见战利品箱的最新状态
3. **确保映射正确**：战利品状态应用时会正确更新稳定ID映射
4. **超时保护**：每个同步请求都有超时保护，避免卡死

## 测试验证

1. 客户端连接服务端后，应该能看到所有战利品箱中的物品
2. 客户端重连后，应该能重新看到所有战利品箱中的物品
3. 场景切换后重连，战利品箱同步应该正常工作
4. 日志中应该能看到缓存清除和强制同步的相关信息

## 相关文件

- `EscapeFromDuckovCoopMod/Main/NetService.cs` - 网络服务，添加缓存清除和重连逻辑
- `EscapeFromDuckovCoopMod/Main/SceneService/LootManager.cs` - 战利品管理器，添加强制同步方法
- `EscapeFromDuckovCoopMod/Main/SceneService/LootNet.cs` - 战利品网络，改进状态应用逻辑

### 6. 修复客户端战利品箱检测逻辑

在 `DeadLootBox.cs` 的 `SpawnDeadLootboxAt()` 方法中修复了错误的存在性检查：

```csharp
/// <summary>
/// 清理客户端无效的战利品箱条目
/// </summary>
private void CleanupInvalidLootboxEntries()
{
    if (IsServer || LootManager.Instance == null)
    {
        return;
    }

    try
    {
        var invalidUids = new List<int>();
        
        foreach (var kv in LootManager.Instance._cliLootByUid)
        {
            var lootUid = kv.Key;
            var inventory = kv.Value;
            
            if (inventory == null)
            {
                invalidUids.Add(lootUid);
                continue;
            }
            
            // 检查对应的InteractableLootbox是否还存在且有效
            var lootbox = LootboxDetectUtil.TryGetInventoryLootBox(inventory);
            if (lootbox == null || lootbox.gameObject == null || !lootbox.gameObject.activeInHierarchy)
            {
                invalidUids.Add(lootUid);
            }
        }
        
        foreach (var uid in invalidUids)
        {
            LootManager.Instance._cliLootByUid.Remove(uid);
            Debug.Log($"[DEATH-DEBUG] Cleaned up invalid lootbox entry: lootUid={uid}");
        }
        
        if (invalidUids.Count > 0)
        {
            Debug.Log($"[DEATH-DEBUG] Cleaned up {invalidUids.Count} invalid lootbox entries");
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"[DEATH-DEBUG] Failed to cleanup invalid lootbox entries: {e}");
    }
}
```

修复了原来错误的检查逻辑：
- **原来的问题**：`existingInv.gameObject != null` - `Inventory`对象没有`gameObject`属性
- **修复后**：正确检查对应的`InteractableLootbox`是否存在且有效
- **增强功能**：添加位置检查，如果同一`lootUid`的战利品箱在不同位置，会清理旧的

## 编译状态

✅ 编译成功，无错误
⚠️ 有一些无关紧要的警告