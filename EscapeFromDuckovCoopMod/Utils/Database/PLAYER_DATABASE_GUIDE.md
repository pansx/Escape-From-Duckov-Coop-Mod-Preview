# 玩家信息数据库使用指南

## 概述

玩家信息数据库用于缓存和管理玩家的 Steam 信息，包括 SteamID、Steam 名字和头像。

## 自动初始化

Mod 启动时会自动：
1. 创建玩家信息数据库
2. 获取当前玩家的 Steam 信息
3. 缓存到数据库中
4. 输出 JSON 到日志

## 基础使用

### 获取数据库实例

```csharp
var playerDb = PlayerInfoDatabase.Instance;
```

### 添加或更新玩家

```csharp
// 添加本地玩家
playerDb.AddOrUpdatePlayer(
    steamId: "76561198012345678",
    steamName: "PlayerName",
    avatarUrl: "https://...",
    isLocal: true
);

// 添加远程玩家
playerDb.AddOrUpdatePlayer(
    steamId: "76561198087654321",
    steamName: "RemotePlayer",
    avatarUrl: "https://...",
    isLocal: false
);
```

### 查询玩家

```csharp
// 按 SteamID 查询
var player = playerDb.GetPlayerBySteamId("76561198012345678");

// 获取本地玩家
var localPlayer = playerDb.GetLocalPlayer();

// 按名字查询
var players = playerDb.GetPlayersByName("PlayerName");

// 获取所有玩家
var allPlayers = playerDb.GetAllPlayers();
```

### 头像管理

```csharp
// 设置头像纹理
playerDb.SetPlayerAvatar(steamId, avatarTexture);

// 获取头像纹理
var avatar = playerDb.GetPlayerAvatar(steamId);
```

### 自定义数据

```csharp
// 存储自定义数据
playerDb.SetCustomData(steamId, "LastLoginTime", DateTime.Now);
playerDb.SetCustomData(steamId, "Level", 42);

// 读取自定义数据
var lastLogin = playerDb.GetCustomData(steamId, "LastLoginTime");
var level = playerDb.GetCustomData(steamId, "Level");
```

## JSON 导出

### 简单导出

```csharp
var json = playerDb.ExportToJson(indented: true);
Debug.Log(json);
```

### 带统计信息导出

```csharp
var json = playerDb.ExportToJsonWithStats(indented: true);
Debug.Log(json);
```

**输出示例**：
```json
{
  "ExportTime": "2025-11-11 01:23:00",
  "TotalPlayers": 1,
  "LocalPlayer": "YourSteamName",
  "Players": [
    {
      "SteamId": "76561198012345678",
      "SteamName": "YourSteamName",
      "SteamAvatarUrl": "https://steamcdn-a.akamaihd.net/...",
      "HasAvatar": false,
      "IsLocalPlayer": true,
      "LastSeen": "2025-11-11 01:23:00",
      "CustomData": {}
    }
  ]
}
```

## 实际应用场景

### 场景1：显示玩家列表

```csharp
void ShowPlayerList()
{
    var playerDb = PlayerInfoDatabase.Instance;
    
    foreach (var player in playerDb.GetAllPlayers())
    {
        Debug.Log($"玩家: {player.SteamName} ({player.SteamId})");
        
        if (player.IsLocalPlayer)
            Debug.Log("  [本地玩家]");
        
        if (player.AvatarTexture != null)
            Debug.Log("  头像已加载");
    }
}
```

### 场景2：加载玩家头像

```csharp
IEnumerator LoadPlayerAvatar(string steamId, string avatarUrl)
{
    var www = UnityWebRequestTexture.GetTexture(avatarUrl);
    yield return www.SendWebRequest();
    
    if (www.result == UnityWebRequest.Result.Success)
    {
        var texture = DownloadHandlerTexture.GetContent(www);
        PlayerInfoDatabase.Instance.SetPlayerAvatar(steamId, texture);
        Debug.Log($"头像加载成功: {steamId}");
    }
}
```

### 场景3：记录玩家活动

```csharp
void RecordPlayerActivity(string steamId, string activity)
{
    var playerDb = PlayerInfoDatabase.Instance;
    
    // 更新最后活动时间
    var player = playerDb.GetPlayerBySteamId(steamId);
    if (player != null)
    {
        player.LastSeen = DateTime.Now;
        playerDb.SetCustomData(steamId, "LastActivity", activity);
    }
}
```

### 场景4：导出玩家统计

```csharp
void ExportPlayerStats()
{
    var playerDb = PlayerInfoDatabase.Instance;
    var json = playerDb.ExportToJsonWithStats(indented: true);
    
    var filePath = Path.Combine(
        Application.persistentDataPath,
        $"players_{DateTime.Now:yyyyMMdd_HHmmss}.json"
    );
    
    File.WriteAllText(filePath, json);
    Debug.Log($"玩家数据已导出: {filePath}");
}
```

## API 参考

### PlayerInfoEntity

| 属性 | 类型 | 说明 |
|------|------|------|
| SteamId | string | Steam ID |
| SteamName | string | Steam 名字 |
| SteamAvatarUrl | string | Steam 头像 URL |
| AvatarTexture | Texture2D | 头像纹理 |
| LastSeen | DateTime | 最后活动时间 |
| IsLocalPlayer | bool | 是否是本地玩家 |
| CustomData | Dictionary | 自定义数据 |

### PlayerInfoDatabase

| 方法 | 说明 |
|------|------|
| `AddOrUpdatePlayer()` | 添加或更新玩家信息 |
| `RemovePlayer()` | 删除玩家信息 |
| `GetPlayerBySteamId()` | 按 SteamID 查询 |
| `GetPlayersByName()` | 按名字查询 |
| `GetLocalPlayer()` | 获取本地玩家 |
| `GetAllPlayers()` | 获取所有玩家 |
| `SetPlayerAvatar()` | 设置头像纹理 |
| `GetPlayerAvatar()` | 获取头像纹理 |
| `SetCustomData()` | 设置自定义数据 |
| `GetCustomData()` | 获取自定义数据 |
| `ExportToJson()` | 导出为 JSON |
| `ExportToJsonWithStats()` | 导出带统计信息的 JSON |

## 注意事项

1. **单例模式**：数据库使用单例模式，全局只有一个实例
2. **自动初始化**：Mod 启动时自动初始化本地玩家信息
3. **Steam API**：需要 Steam 运行才能获取玩家信息
4. **头像加载**：头像 URL 需要手动下载并设置纹理
5. **线程安全**：当前实现不是线程安全的，仅在主线程使用

## 日志输出示例

Mod 启动时会输出：

```
[PlayerDB] 本地玩家信息已缓存:
  SteamID: 76561198012345678
  名字: YourSteamName
  头像URL: https://steamcdn-a.akamaihd.net/...
[PlayerDB] 玩家数据库:
{
  "ExportTime": "2025-11-11 01:23:00",
  "TotalPlayers": 1,
  "LocalPlayer": "YourSteamName",
  "Players": [...]
}
```

---

*最后更新: 2025-11-11*
