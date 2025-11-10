# 投票系统 Steam 名字实时更新

## 实现目标

客户端收到投票 JSON 消息时自动发送 ClientStatus，主机收到 ClientStatus 后根据 Steam ID 更新投票列表中的玩家信息。

## 实现方案

### 1. 客户端收到投票时发送状态

**位置**: `EscapeFromDuckovCoopMod/Net/SceneVoteMessage.cs`

**方法**: `Client_HandleVoteState()`

```csharp
// 🆕 收到投票消息时，立即上报客户端状态（确保 Steam 名字信息最新）
ClientStatusMessage.Client_SendStatusUpdate();
```

**说明**: 
- 客户端收到投票 JSON 时，立即发送包含 SteamID、Steam 名字、EndPoint 的状态更新
- 确保主机能获取到最新的 Steam 信息

### 2. 主机收到状态时更新投票列表

**位置**: `EscapeFromDuckovCoopMod/Net/ClientStatusMessage.cs`

**方法**: `Host_HandleClientStatus()`

```csharp
// 🆕 更新投票系统中的玩家信息（根据 Steam ID 匹配）
UpdateVotePlayerInfo(data.endPoint, data.steamId, data.steamName);
```

**新增方法**: `UpdateVotePlayerInfo()`

```csharp
/// <summary>
/// 🆕 更新投票系统中的玩家信息（根据 Steam ID 匹配）
/// </summary>
private static void UpdateVotePlayerInfo(string endPoint, string steamId, string steamName)
{
    // 1. 检查是否有活跃的投票
    if (!SceneVoteMessage.HasActiveVote())
        return;

    // 2. 通过反射访问 _hostVoteState
    // 3. 根据 Steam ID 或 EndPoint 查找玩家
    // 4. 更新 Steam 名字和 EndPoint
    // 5. 立即广播新的投票状态
}
```

## 工作流程

```
客户端收到投票 JSON
    ↓
立即发送 ClientStatus (包含 SteamID + Steam名字 + EndPoint)
    ↓
主机收到 ClientStatus
    ↓
根据 Steam ID 匹配投票列表中的玩家
    ↓
更新玩家的 Steam 名字和 EndPoint
    ↓
立即广播更新后的投票状态
    ↓
所有客户端收到最新的投票信息（包含正确的 Steam 名字）
```

## 匹配逻辑

### 优先级 1: Steam ID 匹配

```csharp
if (!string.IsNullOrEmpty(steamId) && player.steamId == steamId)
{
    // 更新 Steam 名字
    player.steamName = steamName;
    // 更新 EndPoint（如果变化）
    player.playerId = endPoint;
}
```

**优势**: Steam ID 是唯一且不变的，最可靠

### 优先级 2: EndPoint 匹配

```csharp
else if (player.playerId == endPoint)
{
    // 更新 Steam ID 和名字
    player.steamId = steamId;
    player.steamName = steamName;
}
```

**用途**: 处理首次连接时还没有 Steam ID 的情况

## 关键特性

### 1. 实时更新

- 客户端收到投票时立即上报状态
- 主机收到状态后立即更新并广播
- 无需等待下一次定时广播

### 2. 可靠匹配

- 优先使用 Steam ID 匹配（唯一标识）
- 备用 EndPoint 匹配（处理边缘情况）
- 支持 EndPoint 变化（端口重新分配）

### 3. 自动同步

- 主机更新后自动广播
- 所有客户端自动收到最新信息
- UI 自动刷新显示

## 测试场景

### 场景 1: 正常流程

1. 主机发起投票
2. 客户端收到投票 JSON
3. 客户端立即发送 ClientStatus
4. 主机更新投票列表
5. 主机广播更新后的投票状态
6. 所有客户端看到正确的 Steam 名字

### 场景 2: 中途加入

1. 投票已经开始
2. 新客户端加入
3. 收到投票 JSON 后立即发送 ClientStatus
4. 主机更新投票列表（添加新玩家）
5. 广播更新后的状态

### 场景 3: 端口变化

1. 客户端的端口被重新分配
2. 发送 ClientStatus 时包含新的 EndPoint
3. 主机根据 Steam ID 匹配到玩家
4. 更新 EndPoint
5. 投票继续正常进行

## 日志输出

### 客户端日志

```
[SceneVote] 客户端收到 JSON: {...}
[ClientStatus] 客户端发送状态更新: {...}
```

### 主机日志

```
[ClientStatus] 收到客户端状态: EndPoint=..., SteamID=..., SteamName=...
[ClientStatus] ✓ 已缓存 Steam 名字映射: ... -> ...
[ClientStatus] 🔄 更新投票玩家 Steam 名字: ... -> ... (SteamID=...)
[ClientStatus] ✓ 投票玩家信息已更新，广播新状态
[SceneVote] 主机广播 JSON: {...}
```

## 编译和部署

### 编译

```bash
dotnet build EscapeFromDuckovCoopMod.sln --configuration Release
```

**结果**: 成功，19 个警告（格式化警告，可忽略）

### 部署

```bash
curl.exe -X POST "http://localhost:8080/api/mods/upload" \
  -H "Authorization: Bearer <TOKEN>" \
  -F "file=@EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll"
```

**结果**: 
```json
{
  "artifactPath": "C:/SteamLibrary/.../EscapeFromDuckovCoopMod.dll",
  "latestVersion": "EscapeFromDuckovCoopMod-20251110-2117"
}
```

## 相关文件

- `EscapeFromDuckovCoopMod/Net/SceneVoteMessage.cs` - 投票消息系统
- `EscapeFromDuckovCoopMod/Net/ClientStatusMessage.cs` - 客户端状态上报
- `EscapeFromDuckovCoopMod/Net/JsonMessageRouter.cs` - 消息路由

## 总结

实现了客户端收到投票时自动上报状态，主机根据 Steam ID 实时更新投票列表的功能。通过优先使用 Steam ID 匹配，确保了玩家信息的准确性和一致性。编译成功，已部署到游戏目录。
