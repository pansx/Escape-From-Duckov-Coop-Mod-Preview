# 空墓碑自动清理修复

## 问题描述
在读取墓碑数据时，如果墓碑中的物品数量为0，这些空墓碑会继续占用存储空间和处理资源，导致：
- JSON文件中存储无用的空墓碑数据
- 服务端尝试恢复空墓碑，浪费资源
- 日志中显示大量空墓碑信息，影响调试

## 解决方案
在 `TombstonePersistence.cs` 的 `GetSceneTombstones` 方法中添加了空墓碑检测和自动清理逻辑。

### 修改文件
**文件**: `EscapeFromDuckovCoopMod/Main/SceneService/TombstonePersistence.cs`

### 核心逻辑

#### 1. 空墓碑检测
在读取场景墓碑时，检查每个墓碑的物品数量：
```csharp
var totalItems = tombstone.items?.Count ?? 0;
if (totalItems == 0)
{
    Debug.LogWarning($"[TOMBSTONE] Found empty tombstone: userId={userId}, lootUid={tombstone.lootUid}, sceneId={sceneId}");
    emptyTombstones.Add(tombstone);
}
```

#### 2. 自动清理逻辑
- **分类处理**：将墓碑分为空墓碑和有效墓碑两类
- **从数据中移除**：从用户数据中移除所有空墓碑
- **文件管理**：
  - 如果用户没有任何墓碑了，删除整个JSON文件
  - 如果还有其他有效墓碑，保存更新后的数据

#### 3. 完整的清理流程
```csharp
// 从用户数据中移除空墓碑
foreach (var emptyTombstone in emptyTombstones)
{
    userData.tombstones.RemoveAll(t => t.lootUid == emptyTombstone.lootUid);
    Debug.Log($"[TOMBSTONE] Removed empty tombstone: lootUid={emptyTombstone.lootUid}");
}

// 检查用户是否还有其他墓碑
if (userData.tombstones.Count == 0)
{
    // 删除整个JSON文件
    var filePath = GetUserDataPath(userId);
    if (File.Exists(filePath))
    {
        File.Delete(filePath);
        Debug.Log($"[TOMBSTONE] Deleted empty tombstone file: {filePath}");
    }
    
    // 从缓存中移除
    if (_userDataCache.ContainsKey(userId))
    {
        _userDataCache.Remove(userId);
        Debug.Log($"[TOMBSTONE] Removed user data from cache: {userId}");
    }
}
else
{
    // 保存更新后的数据
    SaveUserData(userId, userData);
    Debug.Log($"[TOMBSTONE] Updated tombstone file after removing empty tombstones: userId={userId}, remaining tombstones={userData.tombstones.Count}");
}
```

## 关键特性

### 🧹 自动清理
- **实时检测**：每次读取墓碑时自动检测空墓碑
- **智能删除**：区分完全空的文件和部分空的文件
- **缓存同步**：同时清理内存缓存，保持数据一致性

### 📝 详细日志
- **检测日志**：记录发现的每个空墓碑
- **清理日志**：记录移除的墓碑和文件操作
- **统计信息**：显示清理前后的墓碑数量

### 🔒 安全处理
- **异常保护**：完整的try-catch包装
- **数据验证**：检查items是否为null
- **文件安全**：确保文件存在才删除

## 日志示例

### 发现空墓碑
```
[TOMBSTONE] Found empty tombstone: userId=player_grave_5, lootUid=5, sceneId=Level_GroundZero
```

### 清理过程
```
[TOMBSTONE] Removing 1 empty tombstones for userId=player_grave_5
[TOMBSTONE] Removed empty tombstone: lootUid=5
[TOMBSTONE] Deleted empty tombstone file: C:/Path/To/user_cGxheWVyX2dyYXZlXzU_tombstones.json
[TOMBSTONE] Removed user data from cache: player_grave_5
```

### 部分清理
```
[TOMBSTONE] Updated tombstone file after removing empty tombstones: userId=Host:9050, remaining tombstones=6
```

## 预期效果

### ✅ 存储优化
- 自动删除空的JSON文件
- 减少磁盘空间占用
- 清理无用的缓存数据

### ✅ 性能提升
- 减少服务端处理空墓碑的开销
- 避免创建空的游戏对象
- 提高墓碑恢复效率

### ✅ 调试友好
- 清晰的日志记录清理过程
- 区分有效墓碑和空墓碑
- 便于追踪数据变化

## 向后兼容性
此修复不影响现有的有效墓碑数据，只清理确实为空的墓碑。清理过程是安全的，不会影响正常的墓碑功能。

## 触发时机
- 每次调用 `GetSceneTombstones` 方法时
- 场景加载时恢复墓碑数据时
- 客户端请求场景墓碑信息时

这确保了空墓碑能够及时被发现和清理，保持数据的整洁性。