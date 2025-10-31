# 坟墓重叠问题修复总结

## 问题描述
1. **重叠问题**：客机看到重叠的箱子和坟墓，主机只看到一个箱子 ✅
2. **客机看不到自己坟墓**：客机死后重生看不到自己的坟墓，但主机可以看到 ✅
3. **坟墓模型问题**：角色刚死时使用错误的烤鸡模型，重新进图后使用正确的墓碑模型 ✅
4. **其他用户取物品更新问题**：其他用户从坟墓中取物品时，无法正确更新坟墓数据 ✅

## 根本原因
1. 客机本地逻辑仍会创建空坟墓，与服务端同步的坟墓重叠
2. 客机坟墓同步检查过于严格，认为坟墓"已存在"但实际游戏对象已被销毁

## 解决方案

### 1. PreventTombSpawnPatch.cs
- 拦截 `LevelConfig.get_SpawnTomb` 属性
- 在联机模式下强制返回 `false`
- 阻止游戏原生的坟墓生成逻辑

### 2. PreventEmptyTombPatch.cs  
- 拦截 `InteractableLootbox.CreateFromItem` 方法
- 防护机制，阻止创建空的坟墓对象
- 检查坟墓预制体和物品有效性

### 3. DeadLootBox.cs 修复
- 改进坟墓存在性检查逻辑
- 不仅检查字典中是否存在记录，还检查游戏对象是否有效
- 自动清理无效的字典记录
- 增强客机预制体获取逻辑，添加 LootManager 备用方案

### 4. LootManager.cs 修复
- 添加详细的预制体选择调试信息
- 确保场景恢复时使用正确的墓碑预制体

### 5. LootNet.cs 修复
- 修复其他用户取物品时的用户ID获取逻辑
- 从坟墓本身获取原始用户ID，而不是从当前操作用户获取
- 添加 `GetTombstoneUserId` 方法通过 `TombstoneUserIdTag` 获取正确的用户ID

```csharp
// 修复1：改进存在性检查
if (lootUid >= 0 && LootManager.Instance._cliLootByUid.ContainsKey(lootUid))
{
    var existingInv = LootManager.Instance._cliLootByUid[lootUid];
    if (existingInv != null && existingInv.gameObject != null)
    {
        // 真正存在，跳过创建
        return;
    }
    else
    {
        // 字典记录无效，清理并重新创建
        LootManager.Instance._cliLootByUid.Remove(lootUid);
    }
}

// 修复2：增强预制体获取
// 添加 LootManager 作为备用预制体来源
var lootManager = LootManager.Instance;
if (lootManager != null)
{
    var prefab = lootManager.ResolveDeadLootPrefabOnServer();
    if (prefab != null)
    {
        return prefab.gameObject;
    }
}
```

## 工作原理
- **配置级别阻止**：从根本上阻止坟墓生成配置
- **创建级别防护**：在对象创建时提供额外防护
- **智能存在性检查**：确保坟墓同步时正确判断是否需要创建
- **保持服务端权威**：只有服务端创建的坟墓会被同步

## 预期效果
- ✅ 客机不再本地创建空坟墓
- ✅ 消除重叠显示问题
- ✅ 客机死后重生能正确看到自己的坟墓
- ✅ 客机和主机看到相同的坟墓对象
- ✅ 场景恢复时使用正确的墓碑模型
- ✅ 其他用户从坟墓取物品时正确更新坟墓数据

## 文件位置
- `EscapeFromDuckovCoopMod/Patch/Scene/PreventTombSpawnPatch.cs` (新增)
- `EscapeFromDuckovCoopMod/Patch/Scene/PreventEmptyTombPatch.cs` (新增)
- `EscapeFromDuckovCoopMod/Main/SceneService/DeadLootBox.cs` (修复)
- `EscapeFromDuckovCoopMod/Main/SceneService/LootManager.cs` (修复)
- `EscapeFromDuckovCoopMod/Main/SceneService/LootNet.cs` (修复)

## 构建状态
✅ 构建成功，无编译错误

## 测试验证

### 单机模式 ✅
```
[COOP] SpawnTomb - 非联机模式，允许正常执行
```

### 联机模式 ✅
```
[COOP] 阻止生成墓碑，防止物品被转移到墓碑中 - IsServer: False
[COOP] 阻止生成墓碑，防止物品被转移到墓碑中 - IsServer: True
```

### 客机坟墓同步改进 ✅
```
[DEATH-DEBUG] SpawnDeadLootboxAt called - aiId:0, lootUid:3
[DEATH-DEBUG] SpawnDeadLootboxAt called - aiId:0, lootUid:2  
[DEATH-DEBUG] SpawnDeadLootboxAt called - aiId:0, lootUid:1
```
不再出现"already exists on client, skipping creation"的跳过消息。

### 预制体获取增强 ✅
添加了 LootManager 备用方案，解决客机预制体找不到的问题。