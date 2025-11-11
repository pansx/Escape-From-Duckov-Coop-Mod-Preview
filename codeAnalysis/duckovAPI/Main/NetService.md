# NetService - 网络服务核心

## 📋 概述

`NetService` 是联机模组的网络服务核心类，负责管理网络连接、玩家状态同步、数据传输等核心功能。

**文件路径**: `EscapeFromDuckovCoopMod/Main/NetService.cs`

---

## 🎯 核心功能

### 1. 网络传输模式

支持两种网络传输模式：

```csharp
public enum NetworkTransportMode
{
    Direct,      // 直连模式（LAN/IP）
    SteamP2P     // Steam P2P 模式
}
```

### 2. 玩家管理

#### 主机端玩家管理
```csharp
// 按 NetPeer 管理
public readonly Dictionary<NetPeer, PlayerStatus> playerStatuses;
public readonly Dictionary<NetPeer, GameObject> remoteCharacters;
```

#### 客户端玩家管理
```csharp
// 按 EndPoint(玩家ID) 管理
public readonly Dictionary<string, PlayerStatus> clientPlayerStatuses;
public readonly Dictionary<string, GameObject> clientRemoteCharacters;
```

### 3. 场景切换自动重连

```csharp
// 缓存成功连接的IP和端口
public string cachedConnectedIP = "";
public int cachedConnectedPort = 0;
public bool hasSuccessfulConnection = false;

// 重连防抖机制
private float lastReconnectTime = 0f;
private const float RECONNECT_COOLDOWN = 10f; // 10秒冷却时间
```

**功能说明**：
- 仅在手动连接成功时缓存IP和端口
- 场景切换后自动重连到上次成功的主机
- 防抖机制避免重连触发过于频繁

### 4. P2P加入超时管理

```csharp
// 仅服务端使用
private readonly Dictionary<NetPeer, float> _peerConnectionTime = new();
private const float JOIN_TIMEOUT_SECONDS = 10f;
```

**功能说明**：
- 记录玩家连接时间
- 超时未进入游戏的玩家将被踢出
- 防止恶意连接占用服务器资源

---

## 🔄 核心流程

### 1. 网络启动流程

```csharp
public void StartNetwork(bool isServer, bool keepSteamLobby = false)
{
    // 1. 停止现有网络
    StopNetwork(!keepSteamLobby);
    
    // 2. 设置AI冻结状态
    COOPManager.AIHandle.freezeAI = !isServer;
    
    // 3. 创建NetManager（4通道系统）
    netManager = new NetManager(this)
    {
        BroadcastReceiveEnabled = true,
        ChannelsCount = 4  // 通道0: Critical, 通道1: Important, 通道2: Normal, 通道3: Frequent
    };
    
    // 4. 启动服务器或客户端
    if (isServer)
    {
        netManager.Start(port);
    }
    else
    {
        netManager.Start();
        CoopTool.SendBroadcastDiscovery();  // 发送局域网发现广播
    }
    
    // 5. 初始化本地玩家
    LocalPlayerManager.Instance.InitializeLocalPlayer();
    
    // 6. 注册主机射击事件
    if (isServer)
    {
        ItemAgent_Gun.OnMainCharacterShootEvent += COOPManager.WeaponHandle.Host_OnMainCharacterShoot;
        UpdateLocalPlayerToDatabase();  // 更新主机信息到数据库
    }
    
    // 7. 配置Steam P2P（如果启用）
    if (TransportMode == NetworkTransportMode.SteamP2P && SteamP2PLoader.Instance.UseSteamP2P)
    {
        netManager.UseNativeSockets = false;  // 禁用原生Socket
        // 初始化Steam组件...
    }
}
```

### 2. 玩家连接流程

```csharp
public void OnPeerConnected(NetPeer peer)
{
    // 1. 记录连接信息
    Debug.Log($"连接成功: {peer.EndPoint}");
    connectedPeer = peer;
    
    if (!IsServer)
    {
        // 客户端：发送状态更新
        Send_ClientStatus.Instance.SendClientStatusUpdate();
        ClientStatusMessage.Client_SendStatusUpdate();  // 发送Steam信息
        
        // 缓存连接信息（仅手动连接）
        if (isManualConnection && peer.EndPoint is IPEndPoint ipEndPoint)
        {
            cachedConnectedIP = ipEndPoint.Address.ToString();
            cachedConnectedPort = ipEndPoint.Port;
            hasSuccessfulConnection = true;
        }
        
        // 更新本地玩家到数据库
        UpdateLocalPlayerToDatabase();
    }
    else
    {
        // 主机：发送SetId消息告知客户端真实网络ID
        SetIdMessage.SendSetIdToPeer(peer);
        
        // 记录连接时间，开始超时计时
        _peerConnectionTime[peer] = Time.time;
        
        // 同步血量信息
        SyncHealthToNewPeer(peer);
        
        // 发送玩家信息更新
        ClientStatusMessage.SendPlayerInfoUpdateToClients();
    }
}
```

### 3. 玩家断开流程

```csharp
public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
{
    // 1. 清理超时记录
    if (IsServer && _peerConnectionTime.ContainsKey(peer))
    {
        _peerConnectionTime.Remove(peer);
    }
    
    // 2. 更新数据库中的 LastSeen 时间戳
    if (playerStatuses.ContainsKey(peer))
    {
        var status = playerStatuses[peer];
        if (status != null && !string.IsNullOrEmpty(status.EndPoint))
        {
            UpdatePlayerLastSeenInDatabase(status.EndPoint);
            SceneNet.Instance._cliLastSceneIdByPlayer.Remove(status.EndPoint);
        }
        playerStatuses.Remove(peer);
    }
    
    // 3. 销毁远程角色
    if (remoteCharacters.ContainsKey(peer) && remoteCharacters[peer] != null)
    {
        Destroy(remoteCharacters[peer]);
        remoteCharacters.Remove(peer);
    }
    
    // 4. 关闭Steam P2P会话
    if (SteamP2PLoader.Instance.UseSteamP2P && SteamEndPointMapper.Instance != null)
    {
        if (SteamEndPointMapper.Instance.TryGetSteamID(peer.EndPoint, out CSteamID remoteSteamID))
        {
            SteamNetworking.CloseP2PSessionWithUser(remoteSteamID);
            SteamEndPointMapper.Instance.UnregisterSteamID(remoteSteamID);
            SteamP2PManager.Instance?.ClearAcceptedSession(remoteSteamID);
        }
    }
}
```

---

## 🗄️ 玩家信息数据库

### 1. 更新本地玩家到数据库

```csharp
private void UpdateLocalPlayerToDatabase()
{
    // 获取 Steam 信息
    string steamId = "";
    string steamName = "";
    string steamAvatarUrl = "";
    
    if (SteamManager.Initialized)
    {
        var mySteamId = Steamworks.SteamUser.GetSteamID();
        steamId = mySteamId.ToString();
        steamName = Steamworks.SteamFriends.GetPersonaName();
        
        // 获取头像 URL
        int avatarHandle = Steamworks.SteamFriends.GetLargeFriendAvatar(mySteamId);
        if (avatarHandle > 0)
        {
            ulong accountId = mySteamId.m_SteamID & 0xFFFFFFFF;
            steamAvatarUrl = $"https://avatars.steamstatic.com/{accountId}/{avatarHandle:x}_full.jpg";
        }
    }
    
    // 添加或更新到数据库
    var playerDb = Utils.Database.PlayerInfoDatabase.Instance;
    playerDb.AddOrUpdatePlayer(
        steamId: steamId,
        playerName: steamName ?? localPlayerStatus.PlayerName ?? "LocalPlayer",
        avatarUrl: steamAvatarUrl,
        isLocal: true,  // 本地玩家
        endPoint: localPlayerStatus.EndPoint,
        lastUpdate: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
    );
    
    // 更新延迟和游戏状态
    playerDb.SetCustomData(steamId, "Latency", localPlayerStatus.Latency);
    playerDb.SetCustomData(steamId, "IsInGame", localPlayerStatus.IsInGame);
}
```

### 2. 定期同步 IsInGame 状态

```csharp
// 每秒同步一次
private float _isInGameSyncTimer = 0f;
private const float IS_IN_GAME_SYNC_INTERVAL = 1.0f;

private void SyncIsInGameStatusToDatabase()
{
    var playerDb = Utils.Database.PlayerInfoDatabase.Instance;
    
    if (IsServer)
    {
        // 同步主机和所有远程玩家
        foreach (var kvp in playerStatuses)
        {
            var status = kvp.Value;
            var player = playerDb.GetPlayerByEndPoint(status.EndPoint);
            if (player != null)
            {
                playerDb.SetCustomData(player.SteamId, "IsInGame", status.IsInGame);
            }
        }
    }
    else
    {
        // 同步客户端看到的所有玩家
        foreach (var kvp in clientPlayerStatuses)
        {
            var status = kvp.Value;
            var player = playerDb.GetPlayerByEndPoint(status.EndPoint);
            if (player != null)
            {
                playerDb.SetCustomData(player.SteamId, "IsInGame", status.IsInGame);
            }
        }
    }
}
```

### 3. 同步延迟到数据库

```csharp
public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
{
    if (playerStatuses.ContainsKey(peer))
    {
        playerStatuses[peer].Latency = latency;
        
        // 同步延迟到数据库
        SyncLatencyToDatabase(peer, latency);
    }
}

private void SyncLatencyToDatabase(NetPeer peer, int latency)
{
    var playerDb = Utils.Database.PlayerInfoDatabase.Instance;
    string endPoint = GetPlayerId(peer);
    
    var player = playerDb.GetPlayerByEndPoint(endPoint);
    if (player != null)
    {
        playerDb.SetCustomData(player.SteamId, "Latency", latency);
    }
}
```

---

## 🔧 辅助方法

### 1. 获取玩家ID

```csharp
public string GetPlayerId(NetPeer peer)
{
    if (peer == null)
    {
        // 本地玩家
        if (localPlayerStatus != null && !string.IsNullOrEmpty(localPlayerStatus.EndPoint))
            return localPlayerStatus.EndPoint;
        return $"Host:{port}";
    }
    
    // 远程玩家
    if (playerStatuses != null && playerStatuses.TryGetValue(peer, out var st) && !string.IsNullOrEmpty(st.EndPoint))
        return st.EndPoint;
    return peer.EndPoint.ToString();
}
```

### 2. 判断是否是自己

```csharp
public bool IsSelfId(string id)
{
    if (string.IsNullOrEmpty(id)) return false;
    
    // 1. 检查本地ID（SetId消息会更新这个值为主机告知的真实网络ID）
    var mine = localPlayerStatus?.EndPoint;
    if (!string.IsNullOrEmpty(mine) && id == mine)
    {
        return true;
    }
    
    // 2. 如果是客户端，检查连接的Peer地址（兜底检查）
    if (!IsServer && connectedPeer != null)
    {
        var myNetworkId = connectedPeer.EndPoint?.ToString();
        if (!string.IsNullOrEmpty(myNetworkId) && id == myNetworkId)
        {
            return true;
        }
    }
    
    return false;
}
```

### 3. 场景切换自动重连

```csharp
public void TryAutoReconnect()
{
    // 防抖检查：距离上次重连必须超过冷却时间
    float timeSinceLastReconnect = Time.time - lastReconnectTime;
    if (timeSinceLastReconnect < RECONNECT_COOLDOWN)
    {
        Debug.LogWarning($"重连请求被拒绝：冷却中 (剩余 {RECONNECT_COOLDOWN - timeSinceLastReconnect:F1} 秒)");
        return;
    }
    
    // 检查是否有缓存的连接信息
    if (!hasSuccessfulConnection || string.IsNullOrEmpty(cachedConnectedIP) || cachedConnectedPort == 0)
    {
        Debug.LogWarning("无缓存的连接信息，跳过自动重连");
        return;
    }
    
    // 检查当前是否已经连接
    if (connectedPeer != null && connectedPeer.ConnectionState == ConnectionState.Connected)
    {
        Debug.Log("已经连接，跳过自动重连");
        return;
    }
    
    // 更新重连时间
    lastReconnectTime = Time.time;
    
    // 执行连接（不设置 isManualConnection，因为这是自动重连）
    Debug.Log($"尝试自动重连到: {cachedConnectedIP}:{cachedConnectedPort}");
    ConnectToHost(cachedConnectedIP, cachedConnectedPort);
}
```

---

## 📊 性能优化

### 1. 多通道系统

```csharp
netManager = new NetManager(this)
{
    ChannelsCount = 4
};

// 通道分配：
// 通道0: Critical (投票、伤害、交互)
// 通道1: Important (血量、装备)
// 通道2: Normal (NPC、物品生成)
// 通道3: Frequent (位置、动画)
```

**优势**：
- 避免队头阻塞（HOL Blocking）
- 关键数据优先传输
- 提高整体网络性能

### 2. 同步间隔控制

```csharp
public float syncInterval = 0.015f; // 15ms = 66.7Hz（满血版无同步延迟）
public float broadcastInterval = 5f; // 5秒广播一次局域网发现
```

### 3. 数据库同步优化

```csharp
// IsInGame 状态：每秒同步一次
private const float IS_IN_GAME_SYNC_INTERVAL = 1.0f;

// 延迟：实时同步（OnNetworkLatencyUpdate 回调）
```

---

## 🔗 相关模块

- **LocalPlayerManager**: 本地玩家管理
- **ClientStatusMessage**: 客户端状态上报
- **SetIdMessage**: 网络ID分配
- **SteamP2PLoader**: Steam P2P 加载器
- **SteamEndPointMapper**: Steam 端点映射
- **PlayerInfoDatabase**: 玩家信息数据库

---

## 📝 最近更新

### 2024-11-11
- ✅ 添加玩家信息数据库集成
- ✅ 实现 IsInGame 状态定期同步
- ✅ 实现延迟实时同步到数据库
- ✅ 优化场景切换自动重连逻辑
- ✅ 添加 P2P 加入超时管理

---

*文档版本: 1.0.0*  
*最后更新: 2024-11-11*
