# 投票用户列表与玩家状态UI差异分析

## 问题描述

投票面板显示的玩家列表与玩家状态UI中显示的玩家不一致。

## 数据来源对比

### 1. 投票面板的玩家列表来源

**位置**: `MModUI.cs` -> `UpdateVotePanel()` 方法

**数据源**: `SceneNet.Instance.sceneParticipantIds`

**构建逻辑** (`CoopTool.cs` -> `BuildParticipantIds_Server()`):

```csharp
public static List<string> BuildParticipantIds_Server()
{
    var list = new List<string>();
    
    // 1. 添加主机自己
    var hostPid = NetService.Instance.GetPlayerId(null);
    if (!string.IsNullOrEmpty(hostPid)) list.Add(hostPid);
    
    // 2. 仅添加"SceneId == 主机SceneId"的客户端
    var statuses = PlayerStatuses;
    foreach (var kv in statuses)
    {
        var peer = kv.Key;
        string peerScene = null;
        
        // 从服务端缓存的场景表获取
        if (!SceneM._srvPeerScene.TryGetValue(peer, out peerScene))
            peerScene = kv.Value?.SceneId;
        
        // 只有在同一场景的玩家才加入投票
        if (!string.IsNullOrEmpty(hostSceneId) && !string.IsNullOrEmpty(peerScene))
        {
            if (peerScene == hostSceneId)
            {
                var pid = NetService.Instance.GetPlayerId(peer);
                if (!string.IsNullOrEmpty(pid)) list.Add(pid);
            }
        }
        else
        {
            // 如果拿不到 SceneId，先加进来
            var pid = NetService.Instance.GetPlayerId(peer);
            if (!string.IsNullOrEmpty(pid)) list.Add(pid);
        }
    }
    
    return list;
}
```

**关键过滤条件**:
- ✅ 主机自己（总是包含）
- ✅ 只包含与主机在**同一场景**的客户端
- ✅ 基于 `SceneM._srvPeerScene` 或 `PlayerStatus.SceneId` 判断

### 2. 玩家状态UI的玩家列表来源

**位置**: `MModUI.cs` -> `UpdatePlayerList()` 方法

**数据源**: `PlayerInfoDatabase.Instance.GetAllPlayers()`

**构建逻辑**:

```csharp
public void UpdatePlayerList(bool forceRebuild = false)
{
    // 从数据库获取所有玩家
    var allPlayers = Utils.Database.PlayerInfoDatabase.Instance.GetAllPlayers().ToList();
    
    // 渲染所有玩家
    foreach (var player in allPlayers)
    {
        CreatePlayerEntry(player);
    }
}
```

**数据库更新来源**:
1. `ClientStatusMessage.cs` - 接收客户端状态消息时更新
2. `NetService.cs` - 网络连接建立时更新
3. `Mod.cs` - 本地玩家初始化时更新
4. `SceneVoteMessage.cs` - 投票消息中更新

**关键特点**:
- ❌ **没有场景过滤**
- ✅ 包含所有曾经连接过的玩家
- ✅ 数据持久化在内存数据库中

## 核心差异

| 特性 | 投票面板 | 玩家状态UI |
|------|---------|-----------|
| 数据源 | `SceneNet.sceneParticipantIds` | `PlayerInfoDatabase` |
| 场景过滤 | ✅ 只显示同场景玩家 | ❌ 显示所有玩家 |
| 实时性 | ✅ 每次投票时重新构建 | ⚠️ 依赖消息更新 |
| 持久化 | ❌ 临时列表 | ✅ 内存数据库 |
| 更新时机 | 投票开始时 | 收到状态消息时 |

## 问题原因

**投票面板显示的玩家少于玩家状态UI的原因**:

1. **场景过滤**: 投票面板只显示与主机在同一场景的玩家
2. **数据库未清理**: 玩家状态UI显示所有曾经连接过的玩家，包括：
   - 已断开连接的玩家
   - 在不同场景的玩家
   - 历史连接记录

## 示例场景

假设有以下情况：

```
主机（Host）: 在场景 "Level_1"
客户端A: 在场景 "Level_1"  ← 会出现在投票面板
客户端B: 在场景 "Level_2"  ← 不会出现在投票面板，但在玩家状态UI
客户端C: 已断开连接      ← 不会出现在投票面板，但可能在玩家状态UI
```

**投票面板**: 显示 Host + 客户端A（2人）
**玩家状态UI**: 显示 Host + 客户端A + 客户端B + 客户端C（4人）

## ✅ 已实施的解决方案

### 为投票面板添加数据库过滤

**修改位置**: `MModUI.cs` -> `UpdateVotePanel()` 方法

**实施内容**:

在投票面板渲染玩家列表时，添加了数据库过滤逻辑：

```csharp
// 获取玩家数据库实例
var playerDb = Utils.Database.PlayerInfoDatabase.Instance;

foreach (var pid in SceneNet.Instance.sceneParticipantIds)
{
    // 🆕 过滤：必须在玩家数据库中存在
    Utils.Database.PlayerInfoEntity playerInfo = null;
    
    // 1. 尝试直接用 pid 作为 SteamId 查找
    playerInfo = playerDb.GetPlayerBySteamId(pid);
    
    // 2. 如果没找到，尝试用 EndPoint 查找
    if (playerInfo == null)
    {
        playerInfo = playerDb.GetPlayerByEndPoint(pid);
    }
    
    // 3. 如果还是没找到，尝试从投票数据中获取 SteamId 再查找
    if (playerInfo == null && SceneNet.Instance.cachedVoteData?.playerList?.items != null)
    {
        foreach (var votePlayer in SceneNet.Instance.cachedVoteData.playerList.items)
        {
            if (votePlayer.playerId == pid && !string.IsNullOrEmpty(votePlayer.steamId))
            {
                playerInfo = playerDb.GetPlayerBySteamId(votePlayer.steamId);
                break;
            }
        }
    }
    
    // 🆕 如果在数据库中找不到，跳过此玩家
    if (playerInfo == null)
    {
        LoggerHelper.LogWarning($"[MModUI] 投票玩家 {pid} 不在数据库中，已跳过显示");
        continue;
    }
    
    // 使用数据库中的玩家名称
    string displayName = playerInfo.PlayerName;
    string displayId = playerInfo.SteamId;
    // ... 渲染UI
}
```

**效果**:
- ✅ 投票面板只显示在玩家数据库中存在的玩家
- ✅ 确保显示的玩家信息与玩家状态UI一致
- ✅ 优先使用数据库中的玩家名称，避免重复查询
- ✅ 添加了详细的日志，便于调试

**部署信息**:
- 编译时间: 2025-11-11
- 版本: EscapeFromDuckovCoopMod-20251111-2156
- 状态: ✅ 已部署

---

## ✅ 修复投票成功判断逻辑（幽灵玩家问题）

### 问题描述

即使所有在数据库中的玩家都同意投票，如果存在"幽灵玩家"（不在数据库中但在 `sceneParticipantIds` 列表中的玩家），投票仍然不会成功。

### 原因分析

**位置**: `SceneNet.cs` -> `Server_OnSceneReadySet()` 方法

**原始逻辑**:
```csharp
// 检查是否全员准备
foreach (var id in sceneParticipantIds)
    if (!sceneReady.TryGetValue(id, out var r) || !r)
        return;

// 全员就绪 → 开始加载
Server_BroadcastBeginSceneLoad();
```

问题：这个逻辑检查 `sceneParticipantIds` 中的**所有玩家**，包括幽灵玩家。幽灵玩家永远不会准备，导致投票永远无法成功。

### 修复方案

添加数据库过滤，只统计在数据库中存在的有效玩家：

```csharp
// 🆕 检查是否全员准备（只检查在数据库中存在的玩家）
var playerDb = Utils.Database.PlayerInfoDatabase.Instance;
int validPlayerCount = 0;
int readyPlayerCount = 0;

foreach (var id in sceneParticipantIds)
{
    // 🆕 检查玩家是否在数据库中
    bool isInDatabase = false;
    
    // 尝试多种方式查找
    if (playerDb.GetPlayerBySteamId(id) != null)
    {
        isInDatabase = true;
    }
    else if (playerDb.GetPlayerByEndPoint(id) != null)
    {
        isInDatabase = true;
    }
    else if (cachedVoteData?.playerList?.items != null)
    {
        foreach (var votePlayer in cachedVoteData.playerList.items)
        {
            if (votePlayer.playerId == id && !string.IsNullOrEmpty(votePlayer.steamId))
            {
                if (playerDb.GetPlayerBySteamId(votePlayer.steamId) != null)
                {
                    isInDatabase = true;
                    break;
                }
            }
        }
    }
    
    // 🆕 只统计在数据库中的玩家
    if (isInDatabase)
    {
        validPlayerCount++;
        if (sceneReady.TryGetValue(id, out var r) && r)
        {
            readyPlayerCount++;
        }
    }
    else
    {
        Debug.LogWarning($"[SCENE] 投票检查：玩家 {id} 不在数据库中，跳过（可能是幽灵玩家）");
    }
}

Debug.Log($"[SCENE] 投票进度：{readyPlayerCount}/{validPlayerCount} 玩家已准备（总参与者：{sceneParticipantIds.Count}）");

// 🆕 只有所有有效玩家都准备好才开始加载
if (validPlayerCount > 0 && readyPlayerCount >= validPlayerCount)
{
    Debug.Log($"[SCENE] ✅ 所有有效玩家已准备，开始加载场景");
    Server_BroadcastBeginSceneLoad();
}
```

### 修复效果

- ✅ 幽灵玩家不再阻止投票成功
- ✅ 只统计在数据库中的有效玩家
- ✅ 添加详细日志，显示投票进度（例如：`2/2 玩家已准备（总参与者：3）`）
- ✅ 当所有有效玩家都准备好时，立即开始加载场景

### 测试建议

1. 启动游戏，创建房间
2. 有客户端加入后，触发投票
3. 观察日志输出：
   - 应该看到 `[SCENE] 投票进度：X/Y 玩家已准备（总参与者：Z）`
   - 如果有幽灵玩家，应该看到警告：`玩家 XXX 不在数据库中，跳过（可能是幽灵玩家）`
4. 所有有效玩家准备后，应该立即看到 `✅ 所有有效玩家已准备，开始加载场景`

**部署信息**:
- 编译时间: 2025-11-11
- 版本: EscapeFromDuckovCoopMod-20251111-2156
- 状态: ✅ 已部署

---

## 其他可选方案

### 方案1: 为玩家状态UI添加场景过滤

修改 `UpdatePlayerList()` 方法，只显示在线且在同一场景的玩家：

```csharp
public void UpdatePlayerList(bool forceRebuild = false)
{
    var allPlayers = PlayerInfoDatabase.Instance.GetAllPlayers().ToList();
    
    // 🆕 过滤：只显示在线且在同一场景的玩家
    var activePlayers = allPlayers.Where(p => 
    {
        // 本地玩家总是显示
        if (p.IsLocalPlayer) return true;
        
        // 检查是否在线（最近有更新）
        var timeSinceUpdate = DateTime.Now - p.LastSeen;
        if (timeSinceUpdate.TotalSeconds > 30) return false;
        
        // 检查是否在同一场景
        if (p.CustomData.TryGetValue("SceneId", out var sceneIdObj))
        {
            var playerScene = sceneIdObj as string;
            var hostScene = LocalPlayerManager.Instance.GetCurrentSceneId();
            return playerScene == hostScene;
        }
        
        return false;
    }).ToList();
    
    foreach (var player in activePlayers)
    {
        CreatePlayerEntry(player);
    }
}
```

### 方案2: 添加"显示所有玩家"开关

在UI中添加一个切换按钮，让用户选择：
- "仅显示同场景玩家"（默认）
- "显示所有玩家"

### 方案3: 定期清理数据库

添加定时任务，清理长时间未更新的玩家记录：

```csharp
// 在 Update() 中定期清理
if (Time.time - _lastCleanupTime > 60f) // 每60秒清理一次
{
    var staleThreshold = DateTime.Now.AddMinutes(-5);
    var stalePlayers = PlayerInfoDatabase.Instance
        .FindPlayers(p => !p.IsLocalPlayer && p.LastSeen < staleThreshold)
        .ToList();
    
    foreach (var player in stalePlayers)
    {
        PlayerInfoDatabase.Instance.RemovePlayer(player.SteamId);
    }
    
    _lastCleanupTime = Time.time;
}
```

### 方案4: 在数据库中存储场景信息

修改 `ClientStatusMessage` 更新逻辑，将场景信息存入 CustomData：

```csharp
playerDb.SetCustomData(data.steamId, "SceneId", data.sceneId);
playerDb.SetCustomData(data.steamId, "IsOnline", true);
```

## 调试建议

1. **添加日志对比**:
```csharp
LoggerHelper.Log($"[DEBUG] 投票参与者: {string.Join(", ", SceneNet.Instance.sceneParticipantIds)}");
LoggerHelper.Log($"[DEBUG] 数据库玩家: {string.Join(", ", PlayerInfoDatabase.Instance.GetAllPlayers().Select(p => p.SteamId))}");
```

2. **输出场景信息**:
```csharp
foreach (var player in allPlayers)
{
    var sceneId = player.CustomData.GetValueOrDefault("SceneId", "Unknown");
    LoggerHelper.Log($"[DEBUG] 玩家 {player.PlayerName}: SceneId={sceneId}");
}
```

3. **检查投票数据**:
```csharp
if (SceneNet.Instance.cachedVoteData?.playerList?.items != null)
{
    foreach (var p in SceneNet.Instance.cachedVoteData.playerList.items)
    {
        LoggerHelper.Log($"[DEBUG] 投票数据玩家: {p.playerId}, {p.steamName}, Scene={p.sceneId}");
    }
}
```

## 总结

**核心问题**: 投票面板使用**场景过滤**的实时列表，而玩家状态UI使用**未过滤**的持久化数据库。

**推荐方案**: 为玩家状态UI添加场景过滤和在线状态检查，使其与投票面板的逻辑保持一致。
