# ClientStatusMessage - 客户端状态上报系统

## 📋 概述

`ClientStatusMessage` 是客户端状态上报消息系统，用于客户端连接时上报 SteamID、EndPoint、Steam 名称和头像等信息，建立正确的玩家映射关系。

**文件路径**: `EscapeFromDuckovCoopMod/Net/ClientStatusMessage.cs`

---

## 🎯 核心功能

### 1. 客户端状态数据结构

```csharp
[System.Serializable]
public class ClientStatusData
{
    public string type = "updateClientStatus";
    public string steamId;          // Steam ID
    public string steamName;        // Steam 用户名
    public string steamAvatarUrl;   // Steam 头像 URL
    public string endPoint;         // 客户端的 EndPoint（虚拟 IP）
    public string playerName;       // 玩家名称
    public string timestamp;        // 时间戳
    public int latency;             // 延迟（毫秒）
    public bool isInGame;           // 是否在游戏中
}
```

### 2. 映射缓存

```csharp
// SteamID -> SteamName 映射
private static Dictionary<string, string> _steamIdToNameMap = new();

// EndPoint -> SteamInfo 映射
private static Dictionary<string, (string steamId, string steamName)> _endPointToSteamInfoMap = new();

// 客户端状态更新冷却时间（防止频繁处理）
private static Dictionary<string, float> _clientStatusCooldown = new();
private const float STATUS_UPDATE_COOLDOWN = 5.0f; // 5秒冷却
```

---

## 🔄 核心流程

### 1. 客户端发送状态更新

```csharp
public static void Client_SendStatusUpdate()
{
    var service = NetService.Instance;
    if (service == null || service.IsServer || service.connectedPeer == null)
    {
        return;
    }
    
    // 1. 获取本地 Steam 信息
    string steamId = "";
    string steamName = "";
    string steamAvatarUrl = "";
    
    if (SteamManager.Initialized)
    {
        try
        {
            var mySteamId = Steamworks.SteamUser.GetSteamID();
            steamId = mySteamId.ToString();
            steamName = Steamworks.SteamFriends.GetPersonaName();
            
            // 获取 Steam 头像 URL（大头像 184x184）
            int avatarHandle = Steamworks.SteamFriends.GetLargeFriendAvatar(mySteamId);
            if (avatarHandle > 0)
            {
                steamAvatarUrl = $"https://avatars.steamstatic.com/{GetSteamId3(mySteamId)}/{avatarHandle:x}_full.jpg";
            }
        }
        catch (System.Exception ex)
        {
            LoggerHelper.LogWarning($"获取 Steam 信息失败: {ex.Message}");
        }
    }
    
    // 2. 获取本地 EndPoint 和延迟
    string endPoint = service.localPlayerStatus?.EndPoint ?? "";
    string playerName = service.localPlayerStatus?.PlayerName ?? steamName ?? "Client";
    int latency = service.connectedPeer?.Ping ?? 0;
    bool isInGame = service.localPlayerStatus?.IsInGame ?? false;
    
    // 3. 构建状态数据
    var data = new ClientStatusData
    {
        steamId = steamId,
        steamName = steamName,
        steamAvatarUrl = steamAvatarUrl,
        endPoint = endPoint,
        playerName = playerName,
        timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        latency = latency,
        isInGame = isInGame,
    };
    
    // 4. 发送 JSON 消息
    JsonMessage.SendToHost(data, DeliveryMethod.ReliableOrdered);
}
```

### 2. 主机处理客户端状态

```csharp
public static void Host_HandleClientStatus(NetPeer fromPeer, string json)
{
    var service = NetService.Instance;
    if (service == null || !service.IsServer)
    {
        return;
    }
    
    try
    {
        // 1. 反序列化数据
        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<ClientStatusData>(json);
        if (data == null || data.type != "updateClientStatus")
        {
            LoggerHelper.LogWarning("无效的客户端状态数据");
            return;
        }
        
        // 2. 检查冷却时间（5秒内不重复处理同一客户端）
        var currentTime = UnityEngine.Time.time;
        if (_clientStatusCooldown.TryGetValue(data.endPoint, out var lastTime))
        {
            if (currentTime - lastTime < STATUS_UPDATE_COOLDOWN)
            {
                return; // 还在冷却中，跳过处理
            }
        }
        
        // 3. 更新冷却时间
        _clientStatusCooldown[data.endPoint] = currentTime;
        
        // 4. 缓存 SteamID -> SteamName 映射
        if (!string.IsNullOrEmpty(data.steamId) && !string.IsNullOrEmpty(data.steamName))
        {
            _steamIdToNameMap[data.steamId] = data.steamName;
        }
        
        // 5. 缓存 EndPoint -> SteamInfo 映射
        if (!string.IsNullOrEmpty(data.endPoint) && !string.IsNullOrEmpty(data.steamId) && !string.IsNullOrEmpty(data.steamName))
        {
            _endPointToSteamInfoMap[data.endPoint] = (data.steamId, data.steamName);
        }
        
        // 6. 更新玩家信息数据库
        UpdatePlayerDatabase(data);
        
        // 7. 更新投票系统中的玩家信息
        if (SceneVoteMessage.HasActiveVote())
        {
            UpdateVotePlayerInfo(data.endPoint, data.steamId, data.steamName);
        }
        
        // 8. 建立 SteamID 和 EndPoint 的映射
        RegisterSteamEndPointMapping(data);
        
        // 9. 发送玩家信息更新给所有客户端
        SendPlayerInfoUpdateToClients();
    }
    catch (System.Exception ex)
    {
        LoggerHelper.LogError($"处理客户端状态失败: {ex.Message}");
    }
}
```

### 3. 建立 Steam EndPoint 映射

```csharp
private static void RegisterSteamEndPointMapping(ClientStatusData data)
{
    if (string.IsNullOrEmpty(data.steamId) || string.IsNullOrEmpty(data.endPoint))
    {
        return;
    }
    
    if (SteamEndPointMapper.Instance == null)
    {
        return;
    }
    
    // 解析 EndPoint 为 IPEndPoint
    var parts = data.endPoint.Split(':');
    if (parts.Length == 2 && 
        System.Net.IPAddress.TryParse(parts[0], out var ipAddr) && 
        int.TryParse(parts[1], out var port))
    {
        var ipEndPoint = new System.Net.IPEndPoint(ipAddr, port);
        var steamId = new Steamworks.CSteamID(ulong.Parse(data.steamId));
        
        // 使用反射访问私有字典
        var mapperType = typeof(SteamEndPointMapper);
        var steamToEndPointField = mapperType.GetField("_steamToEndPoint", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var endPointToSteamField = mapperType.GetField("_endPointToSteam", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (steamToEndPointField != null && endPointToSteamField != null)
        {
            var steamToEndPoint = steamToEndPointField.GetValue(SteamEndPointMapper.Instance)
                as System.Collections.Generic.Dictionary<Steamworks.CSteamID, System.Net.IPEndPoint>;
            var endPointToSteam = endPointToSteamField.GetValue(SteamEndPointMapper.Instance)
                as System.Collections.Generic.Dictionary<System.Net.IPEndPoint, Steamworks.CSteamID>;
            
            if (steamToEndPoint != null && endPointToSteam != null)
            {
                // 检查是否已存在相同 SteamID 但不同 EndPoint 的映射（端口变化）
                if (steamToEndPoint.TryGetValue(steamId, out var oldEndPoint))
                {
                    if (!oldEndPoint.Equals(ipEndPoint))
                    {
                        // 移除旧的 EndPoint 映射
                        endPointToSteam.Remove(oldEndPoint);
                        
                        // 同时更新 NetService 中的玩家记录
                        UpdatePlayerStatusEndPoint(oldEndPoint.ToString(), data.endPoint, data.steamId, data.steamName);
                    }
                }
                
                // 注册新的映射（或更新现有映射）
                steamToEndPoint[steamId] = ipEndPoint;
                endPointToSteam[ipEndPoint] = steamId;
            }
        }
    }
}
```

---

## 🗄️ 数据库集成

### 1. 更新玩家信息数据库

```csharp
private static void UpdatePlayerDatabase(ClientStatusData data)
{
    try
    {
        if (string.IsNullOrEmpty(data.steamId))
        {
            LoggerHelper.LogWarning("无法更新数据库：SteamID 为空");
            return;
        }
        
        var playerDb = Utils.Database.PlayerInfoDatabase.Instance;
        
        // 添加或更新玩家信息（使用 steamName 作为 playerName）
        bool success = playerDb.AddOrUpdatePlayer(
            steamId: data.steamId,
            playerName: data.steamName ?? data.playerName ?? "Unknown",
            avatarUrl: data.steamAvatarUrl,
            isLocal: false,  // 远程玩家
            endPoint: data.endPoint,
            lastUpdate: data.timestamp
        );
        
        if (success)
        {
            // 更新延迟和游戏状态到 CustomData
            playerDb.SetCustomData(data.steamId, "Latency", data.latency);
            playerDb.SetCustomData(data.steamId, "IsInGame", data.isInGame);
            
            LoggerHelper.Log(
                $"✓ 已更新玩家数据库: {data.steamName} ({data.steamId}), Latency={data.latency}ms, IsInGame={data.isInGame}"
            );
        }
    }
    catch (System.Exception ex)
    {
        LoggerHelper.LogError($"更新玩家数据库异常: {ex.Message}");
    }
}
```

### 2. 更新投票系统中的玩家信息

```csharp
private static void UpdateVotePlayerInfo(string endPoint, string steamId, string steamName)
{
    var service = NetService.Instance;
    if (service == null || !service.IsServer)
        return;
    
    // 检查是否有活跃的投票
    if (!SceneVoteMessage.HasActiveVote())
        return;
    
    try
    {
        // 通过反射访问 _hostVoteState
        var sceneVoteType = typeof(SceneVoteMessage);
        var hostVoteStateField = sceneVoteType.GetField("_hostVoteState", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        if (hostVoteStateField == null)
        {
            LoggerHelper.LogWarning("无法访问 _hostVoteState 字段");
            return;
        }
        
        var hostVoteState = hostVoteStateField.GetValue(null) as SceneVoteMessage.VoteStateData;
        if (hostVoteState == null || hostVoteState.playerList == null || hostVoteState.playerList.items == null)
            return;
        
        // 根据 Steam ID 或 EndPoint 查找并更新玩家信息
        bool updated = false;
        foreach (var player in hostVoteState.playerList.items)
        {
            // 优先匹配 Steam ID（更可靠）
            if (!string.IsNullOrEmpty(steamId) && player.steamId == steamId)
            {
                // 更新 Steam 名字
                if (!string.IsNullOrEmpty(steamName) && player.steamName != steamName)
                {
                    player.steamName = steamName;
                    updated = true;
                }
                
                // 更新 EndPoint（如果变化）
                if (player.playerId != endPoint)
                {
                    player.playerId = endPoint;
                    updated = true;
                }
                break;
            }
            // 备用：匹配 EndPoint
            else if (player.playerId == endPoint)
            {
                // 更新 Steam ID 和名字
                if (!string.IsNullOrEmpty(steamId) && player.steamId != steamId)
                {
                    player.steamId = steamId;
                    updated = true;
                }
                
                if (!string.IsNullOrEmpty(steamName) && player.steamName != steamName)
                {
                    player.steamName = steamName;
                    updated = true;
                }
                break;
            }
        }
        
        // 如果有更新，立即广播新的投票状态
        if (updated)
        {
            SceneVoteMessage.Host_BroadcastVoteState();
        }
    }
    catch (System.Exception ex)
    {
        LoggerHelper.LogError($"更新投票玩家信息失败: {ex.Message}");
    }
}
```

---

## 📡 玩家信息广播

### 1. 发送玩家信息更新给所有客户端

```csharp
public static void SendPlayerInfoUpdateToClients()
{
    var service = NetService.Instance;
    if (service == null || !service.IsServer)
    {
        return;
    }
    
    try
    {
        // 1. 构建玩家列表
        var playerList = new System.Collections.Generic.List<SceneVoteMessage.PlayerInfo>();
        
        // 2. 添加主机自己
        var (hostSteamId, hostSteamName) = GetLocalSteamInfo();
        
        // 如果本地缓存为空，尝试实时获取 Steam 信息
        if (string.IsNullOrEmpty(hostSteamId) && SteamManager.Initialized)
        {
            try
            {
                var mySteamId = Steamworks.SteamUser.GetSteamID();
                hostSteamId = mySteamId.ToString();
                hostSteamName = Steamworks.SteamFriends.GetPersonaName();
            }
            catch (System.Exception ex)
            {
                LoggerHelper.LogWarning($"获取主机 Steam 信息失败: {ex.Message}");
            }
        }
        
        // 始终添加主机，即使没有 Steam 信息
        var hostPlayerId = $"Host:{service.port}";
        var hostPlayerName = service.localPlayerStatus?.PlayerName ?? "Host";
        
        playerList.Add(new SceneVoteMessage.PlayerInfo
        {
            playerId = hostPlayerId,
            playerName = hostPlayerName,
            steamId = hostSteamId ?? "",
            steamName = hostSteamName ?? "",
            ready = false
        });
        
        // 3. 添加所有客户端
        if (service.playerStatuses != null)
        {
            foreach (var kvp in service.playerStatuses)
            {
                var status = kvp.Value;
                var (clientSteamId, clientSteamName) = GetSteamInfoFromEndPoint(status.EndPoint);
                
                playerList.Add(new SceneVoteMessage.PlayerInfo
                {
                    playerId = status.EndPoint,
                    playerName = status.PlayerName,
                    steamId = clientSteamId ?? "",
                    steamName = clientSteamName ?? "",
                    ready = false
                });
            }
        }
        
        // 4. 构建投票数据（active=false，仅用于更新玩家信息）
        var voteData = new SceneVoteMessage.VoteStateData
        {
            type = "sceneVote",
            voteId = 0,  // 特殊ID，表示这不是真正的投票
            active = false,  // 不激活投票UI
            targetSceneId = "",
            targetSceneDisplayName = "",
            playerList = new SceneVoteMessage.PlayerList { items = playerList.ToArray() },
            totalPlayers = playerList.Count,
            readyPlayers = 0,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        };
        
        // 5. 发送给所有客户端
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(voteData);
        JsonMessage.BroadcastToAllClients(json, LiteNetLib.DeliveryMethod.ReliableOrdered);
        
        LoggerHelper.Log($"✓ 已发送玩家信息更新给所有客户端 (共 {playerList.Count} 名玩家)");
    }
    catch (System.Exception ex)
    {
        LoggerHelper.LogError($"发送玩家信息更新失败: {ex.Message}");
    }
}
```

---

## 🔧 辅助方法

### 1. 初始化本地 Steam 信息

```csharp
public static void InitializeLocalSteamInfo()
{
    if (!SteamManager.Initialized)
    {
        return;
    }
    
    try
    {
        var mySteamId = Steamworks.SteamUser.GetSteamID();
        _localSteamId = mySteamId.ToString();
        _localSteamName = Steamworks.SteamFriends.GetPersonaName();
        
        if (!string.IsNullOrEmpty(_localSteamId) && !string.IsNullOrEmpty(_localSteamName))
        {
            _steamIdToNameMap[_localSteamId] = _localSteamName;
            LoggerHelper.Log($"✓ 已初始化本地 Steam 信息: ID={_localSteamId}, Name={_localSteamName}");
        }
    }
    catch (System.Exception ex)
    {
        LoggerHelper.LogWarning($"初始化本地 Steam 信息失败: {ex.Message}");
    }
}
```

### 2. 获取缓存的 Steam 名字

```csharp
public static string GetSteamNameFromSteamId(string steamId)
{
    if (string.IsNullOrEmpty(steamId))
        return "";
    
    if (_steamIdToNameMap.TryGetValue(steamId, out var steamName))
    {
        return steamName;
    }
    return "";
}
```

### 3. 从 EndPoint 获取 Steam 信息

```csharp
public static (string steamId, string steamName) GetSteamInfoFromEndPoint(string endPoint)
{
    if (string.IsNullOrEmpty(endPoint))
        return ("", "");
    
    if (_endPointToSteamInfoMap.TryGetValue(endPoint, out var info))
    {
        return info;
    }
    return ("", "");
}
```

### 4. 将 SteamID 转换为 SteamID3 格式

```csharp
private static string GetSteamId3(Steamworks.CSteamID steamId)
{
    // SteamID3 格式：[U:1:XXXXXXXX]
    // 从 64 位 SteamID 提取账户 ID
    ulong accountId = steamId.m_SteamID & 0xFFFFFFFF;
    return accountId.ToString();
}
```

---

## 🔗 相关模块

- **NetService**: 网络服务核心
- **PlayerInfoDatabase**: 玩家信息数据库
- **SceneVoteMessage**: 场景投票消息
- **SteamEndPointMapper**: Steam 端点映射
- **JsonMessage**: JSON 消息系统

---

## 📝 最近更新

### 2024-11-11
- ✅ 添加 Steam 头像 URL 支持
- ✅ 添加延迟和游戏状态上报
- ✅ 集成 PlayerInfoDatabase
- ✅ 添加状态更新冷却机制（5秒）
- ✅ 添加 EndPoint 变化检测和更新
- ✅ 添加投票系统玩家信息同步
- ✅ 添加玩家信息广播功能

---

*文档版本: 1.0.0*  
*最后更新: 2024-11-11*
