# 坟墓重叠问题修复

## 问题描述

客机看到重叠的箱子和坟墓，但主机只看到一个箱子。这说明多出来的空坟墓是客机本地逻辑创建的，导致了视觉上的重叠问题。

## 根本原因

1. **客机本地坟墓生成**：客机在死亡处理过程中，本地逻辑仍然会创建坟墓
2. **服务端权威坟墓**：服务端同时也会创建并同步坟墓给客机
3. **重叠显示**：客机最终看到两个坟墓对象（本地创建的空坟墓 + 服务端同步的有物品坟墓）

## 修复方案

### 1. 阻止坟墓生成配置 (PreventTombSpawnPatch.cs)

通过拦截 `LevelConfig.get_SpawnTomb` 属性，在联机模式下强制返回 `false`，阻止游戏原生的坟墓生成逻辑。

```csharp
[HarmonyPatch(typeof(LevelConfig), "get_SpawnTomb")]
internal static class PreventTombSpawnPatch
{
    [HarmonyPrefix]
    private static bool PreventTombSpawn(ref bool __result)
    {
        var mod = ModBehaviourF.Instance;
        if (mod == null || !mod.networkStarted) 
        {
            Debug.Log("[COOP] SpawnTomb - 非联机模式，允许正常执行");
            return true;
        }

        // 在联机模式下阻止生成墓碑，防止客机本地创建空坟墓
        Debug.Log($"[COOP] 阻止生成墓碑，防止物品被转移到墓碑中 - IsServer: {mod.IsServer}");
        __result = false;
        return false; // 阻止原方法执行
    }
}
```

### 2. 空坟墓创建防护 (PreventEmptyTombPatch.cs)

作为额外的防护措施，拦截 `InteractableLootbox.CreateFromItem` 方法，防止创建空的坟墓对象。

```csharp
[HarmonyPatch(typeof(InteractableLootbox), "CreateFromItem")]
[HarmonyPrefix]
private static bool PreventEmptyTombCreation(
    ItemRoot item,
    Vector3 position,
    Quaternion rotation,
    bool moveToMainScene,
    InteractableLootbox prefab,
    bool filterDontDropOnDead,
    ref InteractableLootbox __result)
{
    var mod = ModBehaviourF.Instance;
    if (mod == null || !mod.networkStarted) return true;

    // 检查是否是坟墓预制体
    if (prefab != null && IsTombPrefab(prefab))
    {
        // 检查物品是否为空或无效
        if (item == null || IsEmptyItem(item))
        {
            Debug.Log($"[COOP] 阻止创建空坟墓 - prefab: {prefab.name}, item: {item?.name ?? "null"}");
            __result = null;
            return false;
        }
        
        Debug.Log($"[COOP] 允许创建有物品的坟墓 - prefab: {prefab.name}, item: {item.name}");
    }

    return true;
}
```

## 工作原理

1. **配置级别阻止**：`PreventTombSpawnPatch` 在配置级别阻止坟墓生成，这是最根本的解决方案
2. **创建级别防护**：`PreventEmptyTombPatch` 在对象创建级别提供额外防护，确保即使有其他路径也不会创建空坟墓
3. **保持服务端权威**：只有服务端创建的坟墓会被同步给客机，确保数据一致性

## 预期效果

- **客机**：不再本地创建空坟墓，只显示服务端同步的坟墓
- **主机**：继续正常创建和管理坟墓
- **视觉效果**：消除重叠问题，客机和主机看到相同的坟墓对象

## 调试信息

补丁会输出以下调试信息：
- `[COOP] SpawnTomb - 非联机模式，允许正常执行`：单机模式下的正常行为
- `[COOP] 阻止生成墓碑，防止物品被转移到墓碑中`：联机模式下阻止坟墓生成
- `[COOP] 阻止创建空坟墓`：防护机制阻止了空坟墓创建
- `[COOP] 允许创建有物品的坟墓`：允许创建有效坟墓

## 文件位置

- `EscapeFromDuckovCoopMod/Patch/Scene/PreventTombSpawnPatch.cs`
- `EscapeFromDuckovCoopMod/Patch/Scene/PreventEmptyTombPatch.cs`