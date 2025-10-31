# 墓碑序列化问题修复报告

## 问题描述

从错误日志中发现了四个主要问题：

1. **目录不存在错误**：墓碑数据保存时出现 `DirectoryNotFoundException`
2. **索引越界错误**：本地化系统中的字符串解析出现 `Index was out of range` 错误
3. **非法文件名字符**：用户ID包含冒号等非法字符，导致文件创建失败
4. **墓碑恢复失败**：第二次进入场景时墓碑没有被正确恢复到游戏世界中

## 修复内容

### 1. Base64文件名编码系统 (TombstonePersistence.cs)

#### 问题
- 用户ID如 `Client:c19ee733` 包含冒号，Windows文件系统不允许
- 字符替换方案会导致反序列化问题（多个ID可能映射到同一文件名）

#### 修复
- 使用Base64编码确保文件名安全且完全可逆
- 添加 `EncodeUserIdForFileName()` 和 `DecodeUserIdFromFileName()` 方法

```csharp
/// <summary>
/// 将用户ID编码为安全的文件名
/// </summary>
private string EncodeUserIdForFileName(string userId)
{
    var bytes = System.Text.Encoding.UTF8.GetBytes(userId);
    var base64 = Convert.ToBase64String(bytes);
    
    // Base64可能包含 '/' 和 '+' 字符，替换为文件名安全的字符
    var safeBase64 = base64.Replace('/', '_')
                           .Replace('+', '-')
                           .TrimEnd('=');
    
    return $"user_{safeBase64}";
}
```

#### 编码示例
- `Client:34cb5fa7` → `user_Q2xpZW50OjM0Y2I1ZmE3_tombstones.json`
- `Host:9050` → `user_SG9zdDo5MDUw_tombstones.json`

### 2. 墓碑恢复机制修复 (LootManager.cs)

#### 问题
- 墓碑数据被正确保存和读取，但第二次进入场景时没有恢复到游戏世界
- 系统认为墓碑"已经在内存中"，但实际游戏对象已被销毁

#### 修复
- 添加 `DoesTombstoneGameObjectExist()` 方法检查墓碑游戏对象是否还存在
- 改进 `LoadSceneTombstones()` 逻辑，即使墓碑在内存中也检查游戏对象状态

```csharp
/// <summary>
/// 检查墓碑对应的游戏对象是否还存在
/// </summary>
private bool DoesTombstoneGameObjectExist(Inventory inventory)
{
    if (inventory == null) return false;
    
    var lootbox = LootboxDetectUtil.TryGetInventoryLootBox(inventory);
    if (lootbox != null && lootbox.gameObject != null)
    {
        return lootbox.gameObject.scene.isLoaded && lootbox.gameObject.activeInHierarchy;
    }
    
    return false;
}
```

### 3. 增强的目录创建机制

```csharp
/// <summary>
/// 确保墓碑数据目录存在
/// </summary>
private void EnsureDirectoryExists()
{
    try
    {
        if (string.IsNullOrEmpty(_streamingAssetsPath))
        {
            _streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "TombstoneData");
        }
        
        if (!Directory.Exists(_streamingAssetsPath))
        {
            Directory.CreateDirectory(_streamingAssetsPath);
        }
    }
    catch (Exception e)
    {
        // 尝试使用备用路径
        var fallbackPath = Path.Combine(Application.persistentDataPath, "TombstoneData");
        if (!Directory.Exists(fallbackPath))
        {
            Directory.CreateDirectory(fallbackPath);
        }
        _streamingAssetsPath = fallbackPath;
    }
}
```

### 4. 本地化系统修复 (LocalizationManager.cs)

#### 修复
- 在所有字符串操作前添加边界检查
- 确保索引计算的安全性

## 验证结果

### 1. Base64编码成功
从日志可以看到：
```
[TOMBSTONE] Saved user data: Host:9050, tombstones: 1
[TOMBSTONE] Saved user data: Client:34cb5fa7, tombstones: 1
```

文件名：
- `user_SG9zdDo5MDUw_tombstones.json` (Host:9050)
- `user_Q2xpZW50OjM0Y2I1ZmE3_tombstones.json` (Client:34cb5fa7)

### 2. 墓碑数据正确读取
```
[TOMBSTONE] Found 1 tombstones for userId=Client:34cb5fa7, sceneId=Level_GroundZero
[TOMBSTONE] Scene tombstone 0: lootUid=3, items=8, position=...
[TOMBSTONE]   Item summary: TypeID:783x1, TypeID:1175x1, TypeID:947x1, TypeID:966x1, TypeID:594x22, TypeID:88x1, TypeID:84x1, TypeID:20x3
```

### 3. 待验证：墓碑恢复
新的修复应该能解决第二次进入场景时墓碑没有物品的问题。

## 预期效果

1. **完全可逆的文件名编码**：确保不同用户ID不会冲突
2. **墓碑正确恢复**：第二次进入场景时墓碑会被重新创建
3. **系统稳定性**：消除所有相关错误和崩溃

## 相关文件

- `EscapeFromDuckovCoopMod/Main/SceneService/TombstonePersistence.cs`
- `EscapeFromDuckovCoopMod/Main/SceneService/LootManager.cs`
- `EscapeFromDuckovCoopMod/Main/Localization/LocalizationManager.cs`