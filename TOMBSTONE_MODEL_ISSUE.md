# 坟墓模型问题分析

## 问题描述
角色刚死时使用的是错误的烤鸡模型，但重新进图后使用的是正确的墓碑模型。

## 问题分析

### 预制体选择逻辑
`ResolveDeadLootPrefabOnServer()` 方法的选择顺序：
1. **首选**：`GameplayDataSettings.Prefabs.LootBoxPrefab_Tomb`（正确的墓碑预制体）
2. **备选**：`GameplayDataSettings.Prefabs.LootBoxPrefab`（可能是烤鸡或其他物品预制体）

### 可能的原因

#### 1. 时机问题
- **死亡时**：`LootBoxPrefab_Tomb` 可能还没有正确初始化或加载
- **场景恢复时**：所有预制体都已经完全加载和初始化

#### 2. 资源加载状态
- 墓碑预制体可能需要额外的加载时间
- 在死亡瞬间，系统回退到了默认的 `LootBoxPrefab`

#### 3. 场景状态差异
- 不同场景状态下，`GameplayDataSettings.Prefabs` 的内容可能不同
- 某些预制体只在特定场景状态下可用

## 修复方案

### 1. 增强调试信息 ✅
已添加详细的调试日志来追踪预制体选择过程：
```csharp
Debug.Log($"[TOMBSTONE] ResolveDeadLootPrefabOnServer - GameplayDataSettings.Prefabs is null: {any == null}");
Debug.Log($"[TOMBSTONE] Using correct tomb prefab: {any.LootBoxPrefab_Tomb.name}");
Debug.LogWarning($"[TOMBSTONE] LootBoxPrefab_Tomb is null, falling back to LootBoxPrefab: {any.LootBoxPrefab?.name ?? "null"}");
```

### 2. 潜在解决方案

#### 方案A：延迟创建
```csharp
// 如果墓碑预制体不可用，延迟一帧再尝试
if (any.LootBoxPrefab_Tomb == null)
{
    StartCoroutine(DelayedTombstoneCreation(tmpRoot, pos, rot));
    return;
}
```

#### 方案B：强制使用墓碑预制体
```csharp
// 如果没有墓碑预制体，拒绝创建而不是使用错误的预制体
if (any.LootBoxPrefab_Tomb == null)
{
    Debug.LogError("[TOMBSTONE] Tomb prefab not available, skipping creation");
    return null;
}
```

#### 方案C：预制体缓存
```csharp
// 在游戏开始时缓存墓碑预制体
private static InteractableLootbox _cachedTombPrefab;

public void CacheTombPrefab()
{
    var any = GameplayDataSettings.Prefabs;
    if (any?.LootBoxPrefab_Tomb != null)
    {
        _cachedTombPrefab = any.LootBoxPrefab_Tomb;
    }
}
```

## 测试计划

### 1. 观察日志
运行游戏并观察以下日志：
- `[TOMBSTONE] Using correct tomb prefab: [name]`
- `[TOMBSTONE] LootBoxPrefab_Tomb is null, falling back to LootBoxPrefab: [name]`

### 2. 确认问题根源
- 如果看到 "falling back" 消息，说明确实是预制体不可用
- 如果没有看到，可能是其他原因

### 3. 验证修复效果
- 死亡时应该使用正确的墓碑模型
- 场景恢复时应该保持一致

## 当前状态
✅ 已添加详细调试信息
⏳ 等待测试结果确认问题根源
⏳ 根据测试结果选择最佳修复方案

## 预期结果
- 死亡时立即使用正确的墓碑模型
- 场景恢复时保持模型一致性
- 消除烤鸡模型的错误显示