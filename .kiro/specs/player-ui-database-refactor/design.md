# 设计文档

## 概述

本设计文档描述了如何重构 `MModUI` 的玩家列表渲染逻辑，使其完全基于 `PlayerInfoDatabase` 作为单一数据源，移除对 `PlayerStatus` 和传输模式相关的复杂逻辑。

## 架构

### 当前架构问题

当前的 `UpdatePlayerList()` 方法存在以下问题：

1. **多数据源混乱**: 同时从 `playerStatuses`、`clientPlayerStatuses`、`localPlayerStatus` 读取数据
2. **模式区分复杂**: 大量 `if (isSteamMode)` 条件分支，导致代码难以维护
3. **Steam API 直接调用**: 在 UI 层直接调用 `SteamFriends.GetFriendPersonaName()` 等 API
4. **去重逻辑复杂**: 使用 `displayedSteamIds`、`displayedEndPoints` 等多个集合进行去重
5. **缓存机制混乱**: 从投票数据、ClientStatusMessage、LobbyManager 等多处获取缓存

### 新架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                         MModUI                              │
│  ┌───────────────────────────────────────────────────────┐  │
│  │          UpdatePlayerList()                           │  │
│  │  - 从 PlayerInfoDatabase 获取所有玩家                  │  │
│  │  - 检测变化（基于 SteamId）                           │  │
│  │  - 重建 UI（如果需要）                                │  │
│  └───────────────────────────────────────────────────────┘  │
│                          ↓                                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │          CreatePlayerEntry(PlayerInfoEntity)          │  │
│  │  - 渲染玩家卡片                                        │  │
│  │  - 显示名称、SteamId、EndPoint                        │  │
│  │  - 显示头像（如果有）                                  │  │
│  │  - 显示延迟和状态（从 CustomData）                    │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                          ↑
                          │ 读取
                          │
┌─────────────────────────────────────────────────────────────┐
│                  PlayerInfoDatabase                         │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  GetAllPlayers() → IEnumerable<PlayerInfoEntity>     │  │
│  └───────────────────────────────────────────────────────┘  │
│                          ↑                                  │
│                          │ 更新                             │
│                          │                                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  AddOrUpdatePlayer(steamId, name, ...)               │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                          ↑
                          │ 调用
                          │
┌─────────────────────────────────────────────────────────────┐
│                      NetService                             │
│  - 收到 ClientStatusMessage 时更新数据库                    │
│  - 玩家连接/断开时更新数据库                                │
│  - 定期同步延迟和状态到 CustomData                          │
└─────────────────────────────────────────────────────────────┘
```

## 组件和接口

### 1. PlayerInfoDatabase（已存在）

**职责**: 存储和管理所有玩家信息

**关键方法**:
- `GetAllPlayers()`: 获取所有玩家
- `AddOrUpdatePlayer()`: 添加或更新玩家信息
- `SetCustomData()`: 设置自定义数据（延迟、状态等）

### 2. MModUI.UpdatePlayerList()（重构）

**职责**: 从数据库获取玩家列表并更新 UI

**新实现逻辑**:

```csharp
private void UpdatePlayerList()
{
    if (_components?.PlayerListContent == null) return;

    // 1. 从数据库获取所有玩家
    var allPlayers = PlayerInfoDatabase.Instance.GetAllPlayers().ToList();
    
    // 2. 提取当前玩家的 SteamId 集合
    var currentPlayerIds = new HashSet<string>(
        allPlayers.Select(p => p.SteamId)
    );
    
    // 3. 检查是否需要重建 UI
    bool needsRebuild = !_displayedPlayerIds.SetEquals(currentPlayerIds);
    
    if (!needsRebuild)
        return;
    
    // 4. 清空现有列表
    foreach (Transform child in _components.PlayerListContent)
        Destroy(child.gameObject);
    _playerEntries.Clear();
    _playerPingTexts.Clear();
    
    // 5. 更新缓存
    _displayedPlayerIds.Clear();
    foreach (var id in currentPlayerIds)
        _displayedPlayerIds.Add(id);
    
    // 6. 渲染所有玩家
    foreach (var player in allPlayers)
    {
        CreatePlayerEntry(player);
    }
}
```

**移除的逻辑**:
- ❌ `isSteamMode` 检查
- ❌ `displayedSteamIds` 和 `displayedEndPoints` 去重
- ❌ `GetSteamIdFromStatus()` 调用
- ❌ Steam Lobby 成员列表遍历
- ❌ 虚拟状态创建
- ❌ `playerStatuses`、`clientPlayerStatuses`、`localPlayerStatus` 访问

### 3. MModUI.CreatePlayerEntry()（重构）

**职责**: 根据 `PlayerInfoEntity` 渲染玩家卡片

**新签名**:
```csharp
private void CreatePlayerEntry(PlayerInfoEntity player)
```

**新实现逻辑**:

```csharp
private void CreatePlayerEntry(PlayerInfoEntity player)
{
    var entry = CreateModernCard(_components.PlayerListContent, $"Player_{player.SteamId}");
    
    // 本地玩家特殊样式
    if (player.IsLocalPlayer)
    {
        var bg = entry.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0.24f, 0.52f, 0.98f, 0.15f);
            var outline = entry.AddComponent<Outline>();
            outline.effectColor = ModernColors.Primary;
            outline.effectDistance = new Vector2(2, -2);
        }
    }
    
    var headerRow = CreateHorizontalGroup(entry.transform, "Header");
    
    // 状态指示器（从 CustomData 读取）
    bool isInGame = player.CustomData.TryGetValue("IsInGame", out var inGameObj) 
        && inGameObj is bool inGameValue && inGameValue;
    
    var statusDot = new GameObject("StatusDot");
    statusDot.transform.SetParent(headerRow.transform, false);
    var dotLayout = statusDot.AddComponent<LayoutElement>();
    dotLayout.preferredWidth = 10;
    dotLayout.preferredHeight = 10;
    var dotImage = statusDot.AddComponent<Image>();
    dotImage.color = isInGame ? ModernColors.Success : ModernColors.Warning;
    
    // 显示玩家名称（直接使用数据库中的名称）
    var nameText = CreateText("Name", headerRow.transform, player.PlayerName, 16, 
        ModernColors.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold);
    
    // 本地玩家标签
    if (player.IsLocalPlayer)
    {
        CreateBadge(headerRow.transform, CoopLocalization.Get("ui.playerStatus.local"), 
            ModernColors.Primary);
    }
    
    CreateDivider(entry.transform);
    
    var infoRow = CreateHorizontalGroup(entry.transform, "Info");
    
    // 显示 SteamId
    CreateText("ID", infoRow.transform, 
        CoopLocalization.Get("ui.playerStatus.id") + ": " + player.SteamId, 
        13, ModernColors.TextSecondary);
    
    // 显示延迟（从 CustomData 读取）
    int latency = 0;
    if (player.CustomData.TryGetValue("Latency", out var latencyObj) && latencyObj is int latencyValue)
    {
        latency = latencyValue;
    }
    
    var pingText = CreateText("Ping", infoRow.transform, $"{latency}ms", 13,
        latency < 50 ? ModernColors.Success :
        latency < 100 ? ModernColors.Warning : ModernColors.Error);
    
    // 保存延迟文本引用（使用 SteamId 作为键）
    _playerPingTexts[player.SteamId] = pingText;
    
    // 显示游戏状态
    var stateText = CreateText("State", infoRow.transform, 
        isInGame ? CoopLocalization.Get("ui.playerStatus.inGameStatus") : 
                   CoopLocalization.Get("ui.playerStatus.idle"), 
        13, isInGame ? ModernColors.Success : ModernColors.TextSecondary);
    
    // 踢人按钮（只有主机且不是本地玩家时显示）
    if (IsServer && !player.IsLocalPlayer && SteamManager.Initialized)
    {
        if (ulong.TryParse(player.SteamId, out ulong targetSteamId) && targetSteamId > 0)
        {
            var kickButton = CreateIconButton("KickBtn", infoRow.transform, "踢", () =>
            {
                LoggerHelper.Log($"[MModUI] 主机踢出玩家: SteamID={targetSteamId}");
                KickMessage.Server_KickPlayer(targetSteamId, "被主机踢出");
            }, 50, ModernColors.Error);
        }
    }
}
```

**移除的逻辑**:
- ❌ `isSteamMode` 检查
- ❌ `displayName` 和 `displayId` 的复杂计算
- ❌ 从投票数据获取名称的逻辑
- ❌ `GetSteamIdFromStatus()` 调用
- ❌ Steam API 调用（`SteamFriends.GetFriendPersonaName()` 等）
- ❌ `isHost` 判断和前缀添加
- ❌ 使用 `EndPoint` 作为键

### 4. MModUI.UpdatePlayerPingDisplays()（重构）

**职责**: 实时更新玩家延迟显示

**新实现逻辑**:

```csharp
private void UpdatePlayerPingDisplays()
{
    if (_playerPingTexts.Count == 0) return;
    
    _pingUpdateTimer += Time.deltaTime;
    if (_pingUpdateTimer < PING_UPDATE_INTERVAL)
        return;
    
    _pingUpdateTimer = 0f;
    
    // 从数据库获取所有玩家
    var allPlayers = PlayerInfoDatabase.Instance.GetAllPlayers();
    
    foreach (var player in allPlayers)
    {
        // 使用 SteamId 作为键查找文本组件
        if (_playerPingTexts.TryGetValue(player.SteamId, out var pingText) && pingText != null)
        {
            // 从 CustomData 读取延迟
            int latency = 0;
            if (player.CustomData.TryGetValue("Latency", out var latencyObj) && latencyObj is int latencyValue)
            {
                latency = latencyValue;
            }
            
            pingText.text = $"{latency}ms";
            
            if (latency < 50)
                pingText.color = ModernColors.Success;
            else if (latency < 100)
                pingText.color = ModernColors.Warning;
            else
                pingText.color = ModernColors.Error;
        }
    }
}
```

**移除的逻辑**:
- ❌ 从 `playerStatuses` 和 `clientPlayerStatuses` 收集状态
- ❌ 使用 `EndPoint` 作为键

### 5. NetService 数据库更新逻辑（新增）

**职责**: 在网络事件发生时更新 `PlayerInfoDatabase`

**需要添加的更新点**:

1. **收到 ClientStatusMessage 时**:
```csharp
// 在 ClientStatusMessage 处理逻辑中
PlayerInfoDatabase.Instance.AddOrUpdatePlayer(
    steamId: message.SteamId,
    playerName: message.SteamName,
    endPoint: message.EndPoint,
    lastUpdate: message.Timestamp
);

// 更新延迟和状态到 CustomData
PlayerInfoDatabase.Instance.SetCustomData(message.SteamId, "Latency", message.Latency);
PlayerInfoDatabase.Instance.SetCustomData(message.SteamId, "IsInGame", message.IsInGame);
```

2. **玩家连接时**:
```csharp
// 在 OnPeerConnected 中
PlayerInfoDatabase.Instance.AddOrUpdatePlayer(
    steamId: GetSteamIdFromPeer(peer),
    playerName: GetPlayerNameFromPeer(peer),
    endPoint: peer.EndPoint.ToString(),
    isLocal: false
);
```

3. **玩家断开连接时**:
```csharp
// 在 OnPeerDisconnected 中
var steamId = GetSteamIdFromPeer(peer);
var player = PlayerInfoDatabase.Instance.GetPlayerBySteamId(steamId);
if (player != null)
{
    player.LastSeen = DateTime.Now;
    // 可选：从数据库中移除，或保留一段时间
}
```

4. **定期同步延迟和状态**:
```csharp
// 在 Update 或定时器中
foreach (var kvp in playerStatuses)
{
    var peer = kvp.Key;
    var status = kvp.Value;
    var steamId = GetSteamIdFromPeer(peer);
    
    PlayerInfoDatabase.Instance.SetCustomData(steamId, "Latency", status.Latency);
    PlayerInfoDatabase.Instance.SetCustomData(steamId, "IsInGame", status.IsInGame);
}
```

## 数据模型

### PlayerInfoEntity（已存在）

```csharp
public class PlayerInfoEntity
{
    // Steam 信息
    public string SteamId { get; set; }
    public string PlayerName { get; set; }
    public string SteamAvatarUrl { get; set; }
    public Texture2D AvatarTexture { get; set; }
    
    // 网络信息
    public string EndPoint { get; set; }
    public string LastUpdate { get; set; }
    
    // 元数据
    public DateTime LastSeen { get; set; }
    public bool IsLocalPlayer { get; set; }
    public Dictionary<string, object> CustomData { get; set; }
}
```

**CustomData 约定的键**:
- `"Latency"` (int): 玩家延迟（毫秒）
- `"IsInGame"` (bool): 是否在游戏中
- 未来可扩展其他键

## 错误处理

### 1. 数据库为空

**场景**: `PlayerInfoDatabase.Instance.GetAllPlayers()` 返回空列表

**处理**: 显示空列表提示，不抛出异常

### 2. CustomData 缺失

**场景**: `player.CustomData` 不包含 `"Latency"` 或 `"IsInGame"` 键

**处理**: 使用默认值（延迟=0，状态=false），不抛出异常

### 3. SteamId 解析失败

**场景**: `ulong.TryParse(player.SteamId, out ...)` 失败

**处理**: 不显示踢人按钮，记录警告日志

### 4. 数据库更新失败

**场景**: `AddOrUpdatePlayer()` 返回 false

**处理**: 记录错误日志，继续执行，不影响 UI 渲染

## 测试策略

### 单元测试

1. **UpdatePlayerList 测试**:
   - 测试空数据库时的行为
   - 测试玩家列表变化时的 UI 重建
   - 测试玩家列表不变时不重建 UI

2. **CreatePlayerEntry 测试**:
   - 测试本地玩家的特殊样式
   - 测试 CustomData 缺失时的默认值
   - 测试踢人按钮的显示条件

3. **UpdatePlayerPingDisplays 测试**:
   - 测试延迟更新的频率限制
   - 测试延迟颜色编码
   - 测试 CustomData 缺失时的行为

### 集成测试

1. **Steam 模式测试**:
   - 创建 Steam Lobby，加入多个玩家
   - 验证玩家列表正确显示
   - 验证延迟和状态实时更新

2. **直连模式测试**:
   - 启动服务器，连接多个客户端
   - 验证玩家列表正确显示
   - 验证延迟和状态实时更新

3. **模式切换测试**:
   - 从 Steam 模式切换到直连模式
   - 验证玩家列表正确清空和重建
   - 验证数据库正确更新

### 手动测试

1. 打开玩家状态面板，验证显示正确
2. 连接/断开玩家，验证列表实时更新
3. 检查延迟显示的颜色编码
4. 测试踢人功能
5. 测试本地玩家标识

## 性能考虑

### 1. 减少 UI 重建频率

**问题**: 频繁的 UI 重建会导致性能下降

**解决方案**:
- 使用 `_displayedPlayerIds` 缓存检测变化
- 只有玩家列表真正变化时才重建 UI
- 移除 Steam 模式下的定期强制刷新逻辑

### 2. 数据库查询优化

**问题**: 每帧调用 `GetAllPlayers()` 可能影响性能

**解决方案**:
- `GetAllPlayers()` 返回的是内存中的集合，性能开销很小
- 如果需要，可以在 `PlayerInfoDatabase` 中添加缓存机制

### 3. 延迟更新频率限制

**问题**: 每帧更新延迟显示会浪费资源

**解决方案**:
- 使用 `_pingUpdateTimer` 限制更新频率为每秒一次
- 只更新已显示的玩家的延迟文本

## 向后兼容性

### 1. 投票面板

**影响**: 投票面板仍然使用 `SceneNet.Instance.sceneParticipantIds`

**解决方案**: 投票面板保持不变，不受此重构影响

### 2. 踢人功能

**影响**: 踢人功能需要 SteamId

**解决方案**: 从 `PlayerInfoEntity.SteamId` 获取，功能保持不变

### 3. 其他 UI 组件

**影响**: 主机列表、观战面板等不受影响

**解决方案**: 这些组件不依赖玩家列表渲染逻辑

## 迁移计划

### 阶段 1: 准备工作

1. 确保 `PlayerInfoDatabase` 在所有网络事件中正确更新
2. 添加日志验证数据库内容的正确性

### 阶段 2: 重构 UI 渲染

1. 重构 `UpdatePlayerList()` 方法
2. 重构 `CreatePlayerEntry()` 方法
3. 重构 `UpdatePlayerPingDisplays()` 方法

### 阶段 3: 清理旧代码

1. 移除 `GetSteamIdFromStatus()` 方法
2. 移除 Steam 模式相关的条件分支
3. 移除未使用的变量和字段

### 阶段 4: 测试和验证

1. 运行单元测试
2. 运行集成测试
3. 手动测试所有场景

## 风险和缓解措施

### 风险 1: 数据库未及时更新

**描述**: 如果 `PlayerInfoDatabase` 在网络事件发生时未及时更新，UI 可能显示过时信息

**缓解措施**:
- 在所有网络事件处理逻辑中添加数据库更新调用
- 添加日志验证更新是否成功
- 添加单元测试验证更新逻辑

### 风险 2: 性能下降

**描述**: 频繁的数据库查询可能影响性能

**缓解措施**:
- 使用缓存机制减少查询频率
- 性能测试验证帧率不受影响
- 如果需要，添加对象池优化 UI 创建

### 风险 3: 功能回归

**描述**: 重构可能导致现有功能失效

**缓解措施**:
- 完整的测试覆盖
- 分阶段迁移，每个阶段都进行验证
- 保留旧代码作为备份，直到新代码稳定

## 总结

本设计通过以下方式简化了玩家列表渲染逻辑：

1. **单一数据源**: 只从 `PlayerInfoDatabase` 读取数据
2. **移除模式区分**: 不再区分 Steam 模式和直连模式
3. **简化去重逻辑**: 使用 `SteamId` 作为唯一标识
4. **移除 Steam API 调用**: 所有 Steam 信息从数据库获取
5. **统一的渲染逻辑**: 所有玩家使用相同的渲染代码

这将大大提高代码的可维护性和可读性，同时保持功能的完整性。
