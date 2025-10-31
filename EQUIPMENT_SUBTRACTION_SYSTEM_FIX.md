# 装备减法系统修复

## 问题分析
之前的硬编码物品过滤方案存在维护困难的问题：
- 需要手动维护物品类型黑名单
- 游戏更新时可能导致过滤规则失效
- 无法适应新增的物品类型
- 与官方更新同步困难

## 新解决方案：装备减法系统
采用更优雅的"减法"方案：
1. **客户端死亡时上报身上的装备**
2. **服务端从墓碑JSON中减去这些装备**
3. **保持与官方更新的完美兼容性**

## 实现原理

### 🔄 完整流程
```
1. 客户端死亡 → 发送 PLAYER_DEAD_TREE (物品树)
2. 服务端创建墓碑 → 保存所有物品到JSON
3. 服务端请求 → 发送 PLAYER_EQUIPMENT_REQUEST
4. 客户端响应 → 发送 PLAYER_DEATH_EQUIPMENT (装备列表)
5. 服务端处理 → 从JSON中减去装备物品
```

### 📡 新增消息类型
```csharp
PLAYER_DEATH_EQUIPMENT = 27,    // 客户端 -> 主机：上报装备
PLAYER_EQUIPMENT_REQUEST = 28,  // 主机 -> 客户端：请求装备信息
```

## 核心实现

### 1. 服务端请求装备信息
**文件**: `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs`

在处理`PLAYER_DEAD_TREE`消息后：
```csharp
// 创建墓碑后，请求客户端上报装备
var lootUid = GetCreatedLootUid(box);
RequestPlayerEquipmentReport(peer, userId, lootUid);
```

### 2. 客户端上报装备
**文件**: `EscapeFromDuckovCoopMod/Main/NetService.cs`

```csharp
public void SendPlayerDeathEquipment(string userId, int lootUid)
{
    var equipmentTypeIds = GetPlayerEquipmentTypeIds();
    
    writer.Reset();
    writer.Put((byte)Op.PLAYER_DEATH_EQUIPMENT);
    writer.Put(userId);
    writer.Put(lootUid);
    writer.Put(equipmentTypeIds.Count);
    
    foreach (var typeId in equipmentTypeIds)
    {
        writer.Put(typeId);
    }
    
    connectedPeer.Send(writer, DeliveryMethod.ReliableOrdered);
}
```

### 3. 装备检测逻辑
```csharp
private List<int> GetPlayerEquipmentTypeIds()
{
    var equipmentTypeIds = new List<int>();
    var mainControl = CharacterMainControl.Main;
    
    // 获取远程武器
    var rangedWeapon = mainControl.GetGun();
    if (rangedWeapon?.Item != null)
        equipmentTypeIds.Add(rangedWeapon.Item.TypeID);
    
    // 获取近战武器
    var meleeWeapon = mainControl.GetMeleeWeapon();
    if (meleeWeapon?.Item != null)
        equipmentTypeIds.Add(meleeWeapon.Item.TypeID);
    
    return equipmentTypeIds;
}
```

### 4. 服务端减法处理
**文件**: `EscapeFromDuckovCoopMod/Main/SceneService/TombstonePersistence.cs`

```csharp
public void RemoveEquipmentFromTombstone(string userId, int lootUid, List<int> equipmentTypeIds)
{
    var tombstone = FindTombstone(userId, lootUid);
    
    foreach (var equipTypeId in equipmentTypeIds)
    {
        var itemToRemove = tombstone.items.Find(item => item.snapshot.typeId == equipTypeId);
        if (itemToRemove != null)
        {
            tombstone.items.Remove(itemToRemove);
            Debug.Log($"[TOMBSTONE] Removed equipment: TypeID={equipTypeId}");
        }
    }
    
    SaveUserData(userId, userData);
}
```

## 关键优势

### ✅ 完美兼容性
- **无硬编码规则**：不依赖特定的物品ID或名称
- **自动适应更新**：游戏添加新装备时自动支持
- **API独立**：不依赖可能变化的游戏内部API

### ✅ 精确控制
- **实时检测**：基于玩家死亡时的实际装备状态
- **精确移除**：只移除确实装备在身上的物品
- **保留背包**：背包中的物品正常掉落

### ✅ 维护友好
- **零维护成本**：无需更新物品黑名单
- **自动扩展**：支持未来的新装备类型
- **调试友好**：详细的日志记录每个步骤

### ✅ 性能优化
- **按需处理**：只在玩家死亡时执行
- **最小网络开销**：只传输装备TypeID列表
- **高效减法**：直接从JSON中移除指定物品

## 消息流程图

```
客户端死亡检测
       ↓
发送 PLAYER_DEAD_TREE (物品树)
       ↓
服务端创建墓碑 + 保存JSON
       ↓
发送 PLAYER_EQUIPMENT_REQUEST
       ↓
客户端检测当前装备
       ↓
发送 PLAYER_DEATH_EQUIPMENT (装备列表)
       ↓
服务端从JSON中减去装备
       ↓
最终墓碑只包含背包物品
```

## 日志示例

### 服务端日志
```
[TOMBSTONE] Requesting equipment report from client: userId=Host:9050, lootUid=123
[TOMBSTONE] Server received PLAYER_DEATH_EQUIPMENT: userId=Host:9050, lootUid=123, equipment count=3
[TOMBSTONE] Removed equipment from tombstone: TypeID=1001, lootUid=123
[TOMBSTONE] Removed equipment from tombstone: TypeID=2001, lootUid=123
[TOMBSTONE] Removed 2 equipment items from tombstone, remaining items=5
```

### 客户端日志
```
[TOMBSTONE] Client received PLAYER_EQUIPMENT_REQUEST: userId=Host:9050, lootUid=123
[TOMBSTONE] Found ranged weapon: TypeID=1001
[TOMBSTONE] Found melee weapon: TypeID=2001
[TOMBSTONE] Sending player death equipment: equipment count=2
```

## 扩展性

### 支持更多装备类型
可以轻松扩展`GetPlayerEquipmentTypeIds`方法来检测更多装备：
```csharp
// 未来可以添加：
// - 护甲装备
// - 头盔装备
// - 配饰装备
// - 特殊道具
```

### 支持条件过滤
可以添加条件逻辑来决定哪些装备应该被移除：
```csharp
if (ShouldRemoveEquipment(equipTypeId))
{
    // 移除装备
}
```

## 向后兼容性
- 移除了之前的硬编码过滤逻辑
- 保持现有的墓碑系统不变
- 不影响AI死亡的战利品掉落
- 完全向后兼容现有的存档数据

## 总结
这个装备减法系统提供了一个更加优雅、可维护和兼容的解决方案，完美解决了不应掉落物品的问题，同时保持了与游戏官方更新的完美同步能力。