# P2P加入超时机制实现

## 功能概述

为P2P联机模式添加了10秒加入超时机制，在服务端实现。如果玩家连接后超过10秒未成功进入游戏，将被自动踢出。

## 问题修复：Steam P2P超时后未从大厅踢出

**问题描述：**
当Steam P2P连接超时时，虽然显示错误提示 `[ERROR] [SteamP2P] 错误原因: Timeout - 连接超时（可能NAT穿透失败）`，但玩家仍然留在Steam大厅中，没有被踢出。

**根本原因：**
`OnP2PSessionFailed` 方法只清理了映射关系，但没有：
1. 断开LiteNetLib的NetPeer连接
2. 关闭Steam P2P会话
3. 通知Steam Lobby系统

**解决方案：**
修改 `SteamEndPointMapper.OnP2PSessionFailed` 方法，添加完整的清理流程。

## 实现细节

### 1. NetService.cs - 核心超时管理

**新增字段：**
```csharp
// 🕐 P2P加入超时管理（仅服务端使用）
private readonly Dictionary<NetPeer, float> _peerConnectionTime = new();
private const float JOIN_TIMEOUT_SECONDS = 10f;
```

**OnPeerConnected - 记录连接时间：**
- 当玩家连接时，记录当前时间到 `_peerConnectionTime` 字典
- 仅在服务端（IsServer）执行
- 输出日志：`[JOIN_TIMEOUT] 玩家 {EndPoint} 开始加入，超时时限: 10秒`

**OnPeerDisconnected - 清理超时记录：**
- 玩家断开连接时，从 `_peerConnectionTime` 中移除记录
- 避免内存泄漏

**MarkPlayerJoinedSuccessfully - 标记成功加入：**
```csharp
public void MarkPlayerJoinedSuccessfully(NetPeer peer)
```
- 当玩家成功进入游戏后调用
- 从 `_peerConnectionTime` 中移除该玩家
- 输出日志：`[JOIN_TIMEOUT] 玩家 {EndPoint} 成功加入游戏，耗时: {elapsed}秒`

**CheckJoinTimeouts - 检查并踢出超时玩家：**
```csharp
public void CheckJoinTimeouts()
```
- 遍历所有待加入的玩家
- 检查是否超过10秒
- 超时则调用 `peer.Disconnect()` 踢出
- 输出警告日志：`[JOIN_TIMEOUT] 玩家 {EndPoint} 加入超时 ({elapsed}秒 > 10秒)，即将踢出`

### 2. Mod.cs - 主循环调用

在 `Update()` 方法的网络轮询后添加超时检查：

```csharp
if (networkStarted)
{
    netManager.PollEvents();
    
    // 🕐 主机端：检查玩家加入超时
    if (IsServer)
    {
        Service.CheckJoinTimeouts();
    }
    
    // ... 其他逻辑
}
```

### 3. CreateRemoteCharacter.cs - 标记成功加入

在 `CreateRemoteCharacterAsync` 方法中，远程角色创建成功后：

```csharp
remoteCharacters[peer] = instance;
cmc.gameObject.SetActive(true);

// 🕐 标记玩家已成功进入游戏，清除加入超时计时
Service.MarkPlayerJoinedSuccessfully(peer);

return instance;
```

## 工作流程

1. **玩家连接** → `OnPeerConnected` 记录连接时间
2. **每帧检查** → `CheckJoinTimeouts` 检查是否超时
3. **成功加入** → `CreateRemoteCharacterAsync` 调用 `MarkPlayerJoinedSuccessfully` 清除计时
4. **超时踢出** → `CheckJoinTimeouts` 调用 `peer.Disconnect()` 踢出玩家
5. **断开连接** → `OnPeerDisconnected` 清理记录

## 日志输出

### 正常流程：
```
[JOIN_TIMEOUT] 玩家 192.168.1.100:9050 开始加入，超时时限: 10秒
[JOIN_TIMEOUT] 玩家 192.168.1.100:9050 成功加入游戏，耗时: 3.45秒
```

### 超时流程：
```
[JOIN_TIMEOUT] 玩家 192.168.1.100:9050 开始加入，超时时限: 10秒
[JOIN_TIMEOUT] 玩家 192.168.1.100:9050 加入超时 (10.02秒 > 10秒)，即将踢出
[JOIN_TIMEOUT] 已踢出超时玩家: 192.168.1.100:9050
```

## 配置参数

可以通过修改 `NetService.cs` 中的常量来调整超时时间：

```csharp
private const float JOIN_TIMEOUT_SECONDS = 10f;  // 默认10秒
```

## 注意事项

1. **仅服务端执行**：所有超时检查逻辑仅在 `IsServer == true` 时执行
2. **性能影响**：每帧检查，但只遍历待加入玩家（通常很少），性能开销可忽略
3. **网络延迟**：10秒的超时时间已经足够宽松，即使在较差的网络环境下也能正常加入
4. **兼容性**：不影响现有的连接和断开逻辑，完全向后兼容

## 测试建议

1. **正常加入测试**：验证玩家能正常加入，不会被误踢
2. **超时测试**：模拟网络延迟或客户端卡死，验证10秒后被踢出
3. **多人测试**：多个玩家同时加入，验证各自独立计时
4. **断线重连**：验证断线重连后计时器正确重置

## Steam P2P超时处理增强

### SteamEndPointMapper.cs - OnP2PSessionFailed 方法

**修改前的问题：**
```csharp
public void OnP2PSessionFailed(CSteamID remoteSteamID)
{
    UnregisterSteamID(remoteSteamID);  // ❌ 只清理映射，不断开连接
}
```

**修改后的完整处理：**
```csharp
public void OnP2PSessionFailed(CSteamID remoteSteamID)
{
    // 1. 断开LiteNetLib连接
    if (_steamToEndPoint.TryGetValue(remoteSteamID, out IPEndPoint endPoint))
    {
        var netService = NetService.Instance;
        if (netService != null && netService.IsServer)
        {
            foreach (var peer in netService.playerStatuses.Keys.ToList())
            {
                if (peer.EndPoint.Equals(endPoint))
                {
                    peer.Disconnect();  // ✅ 断开NetPeer
                    break;
                }
            }
        }
    }
    
    // 2. 关闭Steam P2P会话
    SteamNetworking.CloseP2PSessionWithUser(remoteSteamID);  // ✅ 关闭P2P
    
    // 3. 记录日志（如果在Lobby中）
    if (SteamLobbyManager.Instance?.IsHost == true)
    {
        var memberName = SteamLobbyManager.Instance.GetCachedMemberName(remoteSteamID);
        Debug.LogWarning($"玩家 {memberName} 因P2P超时被移除");
    }
    
    // 4. 清理映射
    UnregisterSteamID(remoteSteamID);  // ✅ 清理映射
}
```

### 工作原理

1. **断开NetPeer连接**：触发 `OnPeerDisconnected` 回调，清理游戏内的玩家状态
2. **关闭Steam P2P会话**：通知Steam网络层停止与该玩家的通信
3. **对方收到断开通知**：客户端会收到连接失败事件，应该主动离开Lobby
4. **清理本地映射**：移除SteamID到EndPoint的映射关系

### 注意事项

- Steam Lobby API没有直接的"踢人"方法
- 通过关闭P2P会话，对方会收到连接失败通知
- 对方应该在收到通知后主动调用 `LeaveLobby()`
- 主机端会在 `OnLobbyChatUpdate` 中收到玩家离开的通知

## 编译状态

✅ 代码编译成功（无语法错误）
✅ 所有修改已完成
