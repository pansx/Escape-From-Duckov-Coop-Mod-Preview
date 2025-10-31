# 墓碑恢复专用消息类型修复

## 问题描述
之前的实现中，服务端使用 `DEAD_LOOT_SPAWN` 消息来广播墓碑恢复，导致客户端无法区分真正的AI死亡和墓碑恢复，造成：
- 客户端看到不应该存在的墓碑（与AI尸体重叠）
- 墓碑重复生成和位置冲突
- 客户端错误地移除AI尸体（墓碑恢复时不应该移除尸体）

## 解决方案
添加了专门的 `TOMBSTONE_RESTORE` 消息类型来处理墓碑恢复，完全分离了AI死亡和墓碑恢复的逻辑。

### 1. 新增消息类型
在 `Op.cs` 中添加：
```csharp
TOMBSTONE_RESTORE = 249, // 主机 -> 客户端：墓碑恢复（玩家死亡墓碑的专用消息）
```

### 2. 服务端修改
**文件**: `EscapeFromDuckovCoopMod/Main/SceneService/LootManager.cs`

修改 `BroadcastTombstoneRestored` 方法：
- 使用 `Op.TOMBSTONE_RESTORE` 替代 `Op.DEAD_LOOT_SPAWN`
- 简化消息格式，不包含 `aiId`（因为墓碑恢复不需要AI信息）

```csharp
writer.Put((byte)Op.TOMBSTONE_RESTORE);
writer.Put(scene);
writer.Put(tombstone.lootUid);
writer.PutV3cm(tombstone.position);
writer.PutQuaternion(tombstone.rotation);
```

### 3. 客户端修改
**文件**: `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs`

添加新的消息处理：
```csharp
case Op.TOMBSTONE_RESTORE:
{
    var scene = reader.GetInt();
    var lootUid = reader.GetInt();
    var pos = reader.GetV3cm();
    var rot = reader.GetQuaternion();
    if (SceneManager.GetActiveScene().buildIndex != scene) break;

    Debug.Log($"[TOMBSTONE] Client received TOMBSTONE_RESTORE: lootUid={lootUid}, pos={pos}");
    DeadLootBox.Instance.SpawnTombstoneRestoration(lootUid, pos, rot);
    break;
}
```

### 4. 新增专用恢复方法
**文件**: `EscapeFromDuckovCoopMod/Main/SceneService/DeadLootBox.cs`

添加 `SpawnTombstoneRestoration` 方法：
- **不移除AI尸体**：墓碑恢复不是真正的AI死亡
- **使用墓碑预制体**：优先使用正确的墓碑外观
- **智能位置检查**：避免重复创建相同位置的墓碑
- **完整的状态处理**：支持缓存状态和服务端请求

## 关键特性

### 🎯 逻辑分离
- **AI死亡** (`DEAD_LOOT_SPAWN`): 移除AI尸体，创建战利品箱
- **墓碑恢复** (`TOMBSTONE_RESTORE`): 不移除尸体，创建墓碑

### 🔒 安全检查
- 检查是否已存在相同lootUid的墓碑
- 位置距离检查，避免重复创建
- 清理无效的战利品箱条目

### 🎨 正确外观
- 优先使用墓碑预制体 (`LootBoxPrefab_Tomb`)
- 后备使用默认战利品预制体
- 通过LootManager统一管理预制体选择

### 📦 状态同步
- 支持缓存状态的立即应用
- 自动请求服务端最新状态
- 正确的Loading状态管理

## 修改文件
- `EscapeFromDuckovCoopMod/Main/Op.cs` - 添加新消息类型
- `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs` - 添加消息处理
- `EscapeFromDuckovCoopMod/Main/SceneService/LootManager.cs` - 修改广播方法
- `EscapeFromDuckovCoopMod/Main/SceneService/DeadLootBox.cs` - 添加专用恢复方法

## 预期效果
- ✅ 客户端不再看到与AI尸体重叠的墓碑
- ✅ 墓碑恢复不会错误地移除AI尸体
- ✅ 避免墓碑重复生成和位置冲突
- ✅ 玩家墓碑和AI战利品完全分离
- ✅ 更清晰的代码逻辑和更好的可维护性

## 向后兼容性
此修改不影响现有的AI死亡逻辑，`DEAD_LOOT_SPAWN` 消息继续用于真正的AI死亡处理。新的 `TOMBSTONE_RESTORE` 消息专门用于墓碑恢复，实现了完全的逻辑分离。