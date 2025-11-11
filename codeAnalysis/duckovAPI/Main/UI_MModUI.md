# MModUI - 现代化联机UI系统

## 📋 概述

`MModUI` 是联机模组的主UI类，提供现代化的玻璃拟态设计风格，支持多语言、实时状态更新、玩家列表管理等功能。

**文件路径**: `EscapeFromDuckovCoopMod/Main/UI/MModUI.cs`

---

## 🎨 设计风格

### 1. 现代化颜色方案（深色模式）

```csharp
public static class ModernColors
{
    // 主题主色（浅绿色主调）
    public static readonly Color Primary = new Color(0.30f, 0.69f, 0.31f, 1f);      // #4CAF50
    public static readonly Color PrimaryHover = new Color(0.26f, 0.60f, 0.27f, 1f); // #439946
    public static readonly Color PrimaryActive = new Color(0.22f, 0.52f, 0.23f, 1f); // #38853B
    
    // 按钮文字色
    public static readonly Color PrimaryText = new Color(1f, 1f, 1f, 0.95f);        // 亮白文字
    
    // 背景层次（柔和的深灰）
    public static readonly Color BgDark = new Color(0.23f, 0.23f, 0.23f, 1f);       // #3A3A3A
    public static readonly Color BgMedium = new Color(0.27f, 0.27f, 0.27f, 1f);     // #454545
    public static readonly Color BgLight = new Color(0.32f, 0.32f, 0.32f, 1f);      // #525252
    
    // 文字色（白色层次）
    public static readonly Color TextPrimary = new Color(1f, 1f, 1f, 0.95f);        // 主文字
    public static readonly Color TextSecondary = new Color(1f, 1f, 1f, 0.75f);      // 次文字
    public static readonly Color TextTertiary = new Color(1f, 1f, 1f, 0.55f);       // 辅助文字
    
    // 状态色
    public static readonly Color Success = new Color(0.45f, 0.75f, 0.50f, 1f);      // #73BF80
    public static readonly Color Warning = new Color(0.90f, 0.75f, 0.35f, 1f);      // #E6BF59
    public static readonly Color Error = new Color(0.85f, 0.45f, 0.40f, 1f);        // #D86E66
    public static readonly Color Info = new Color(0.55f, 0.65f, 0.80f, 1f);         // #8CA6CC
}
```

### 2. 玻璃拟态主题

```csharp
public static class GlassTheme
{
    public static readonly Color PanelBg = new Color(0.25f, 0.25f, 0.25f, 0.92f);
    public static readonly Color CardBg = new Color(0.28f, 0.28f, 0.28f, 0.9f);
    public static readonly Color ButtonBg = new Color(0.30f, 0.30f, 0.30f, 0.95f);
    public static readonly Color ButtonHover = new Color(0.35f, 0.35f, 0.35f, 0.97f);
    public static readonly Color ButtonActive = new Color(0.20f, 0.20f, 0.20f, 1f);
}
```

---

## 🖼️ UI组件

### 1. 主面板

**功能**：
- 网络模式切换（Direct/Steam P2P）
- 服务器创建/关闭
- 主机列表（LAN发现）
- Steam Lobby列表
- 连接管理

**快捷键**：
- `=` 键：切换主界面显示

### 2. 玩家状态面板

**功能**：
- 实时玩家列表
- 延迟显示（每秒更新）
- 游戏状态（IsInGame）
- 本地玩家标记
- 踢人功能（仅主机）

**快捷键**：
- `P` 键：切换玩家状态窗口

**数据源**：
- 从 `PlayerInfoDatabase` 读取玩家信息
- 支持 Steam 头像、名称、ID
- 实时同步延迟和游戏状态

### 3. 投票面板

**功能**：
- 场景切换投票
- 玩家准备状态
- 实时投票进度
- 取消投票（仅房主）

**快捷键**：
- `J` 键：切换准备状态

**显示内容**：
- 目标场景名称（中文）
- 玩家列表（Steam名称）
- 准备状态图标
- 投票进度

### 4. 观战面板

**功能**：
- 观战模式提示
- 自动显示/隐藏

### 5. 光标指示器

**功能**：
- UI打开时显示红色圆点光标
- UI关闭时隐藏
- 不阻挡射线检测

---

## 🔄 核心流程

### 1. UI初始化流程

```csharp
public void Init()
{
    Instance = this;
    
    // 1. 初始化组件容器和布局构建器
    _components = new MModUIComponents();
    _layoutBuilder = new MModUILayoutBuilder(this, _components);
    
    // 2. 同步NetService数据
    var svc = Service;
    if (svc != null)
    {
        _manualIP = svc.manualIP;
        _manualPort = svc.manualPort;
        _status = svc.status;
        _port = svc.port;
        // ...
    }
    
    // 3. 注册Steam Lobby事件
    if (LobbyManager != null)
    {
        LobbyManager.LobbyListUpdated += OnLobbyListUpdated;
        LobbyManager.LobbyJoined += OnLobbyJoined;
    }
    
    // 4. 创建UI
    CreateUI();
}
```

### 2. 玩家列表更新流程

```csharp
public void UpdatePlayerList(bool forceRebuild = false)
{
    // 1. 从数据库获取所有玩家
    var allPlayers = Utils.Database.PlayerInfoDatabase.Instance.GetAllPlayers().ToList();
    
    // 2. 提取当前玩家的 SteamId 集合
    var currentPlayerIds = new HashSet<string>(
        allPlayers.Select(p => p.SteamId)
    );
    
    // 3. 检查是否需要重建 UI
    bool needsRebuild = forceRebuild || !_displayedPlayerIds.SetEquals(currentPlayerIds);
    
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

### 3. 投票面板更新流程

```csharp
private void UpdateVotePanel()
{
    // 1. 检查投票是否激活
    bool active = SceneNet.Instance.sceneVoteActive;
    
    // 2. 显示/隐藏面板
    if (_components?.VotePanel != null && _components.VotePanel.activeSelf != active)
    {
        StartCoroutine(AnimatePanel(_components.VotePanel, active));
    }
    
    if (!active) return;
    
    // 3. 检查是否需要重建UI
    bool needsRebuild = false;
    string rebuildReason = "";
    
    if (_lastVoteActive != active)
    {
        needsRebuild = true;
        rebuildReason = "vote active changed";
    }
    else if (_lastVoteSceneId != SceneNet.Instance.sceneTargetId)
    {
        needsRebuild = true;
        rebuildReason = "target scene changed";
    }
    else if (_lastLocalReady != SceneNet.Instance.localReady)
    {
        needsRebuild = true;
        rebuildReason = "local ready changed";
    }
    else
    {
        // 检查参与者列表是否改变
        var currentParticipants = new HashSet<string>(SceneNet.Instance.sceneParticipantIds);
        if (!_lastVoteParticipants.SetEquals(currentParticipants))
        {
            needsRebuild = true;
            rebuildReason = $"participants changed";
        }
    }
    
    if (!needsRebuild) return;
    
    // 4. 清空并重建投票面板内容
    // ...
}
```

---

## 🎯 特色功能

### 1. 实时延迟更新

```csharp
private void UpdatePlayerPingDisplays()
{
    if (_playerPingTexts.Count == 0) return;
    
    // 计时器控制更新频率（每秒一次）
    _pingUpdateTimer += Time.deltaTime;
    if (_pingUpdateTimer < PING_UPDATE_INTERVAL)
        return;
    
    _pingUpdateTimer = 0f;
    
    // 从数据库获取所有玩家
    var allPlayers = Utils.Database.PlayerInfoDatabase.Instance.GetAllPlayers();
    
    // 更新每个玩家的延迟显示
    foreach (var player in allPlayers)
    {
        if (_playerPingTexts.TryGetValue(player.SteamId, out var pingText) && pingText != null)
        {
            // 从 CustomData 读取延迟
            int latency = 0;
            if (player.CustomData.TryGetValue("Latency", out var latencyObj) && latencyObj is int latencyValue)
            {
                latency = latencyValue;
            }
            
            // 更新延迟文本和颜色
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

### 2. 服务端关卡检查

```csharp
private void CheckServerInGame()
{
    _serverCheckTimer += Time.deltaTime;
    if (_serverCheckTimer < SERVER_CHECK_INTERVAL)
        return;
    
    _serverCheckTimer = 0f;
    
    // 检查服务端玩家状态
    if (playerStatuses != null && playerStatuses.Count > 0)
    {
        foreach (var kvp in playerStatuses)
        {
            var hostStatus = kvp.Value;
            if (hostStatus != null && hostStatus.EndPoint.Contains("Host"))
            {
                // 检查主机是否在游戏中
                if (!hostStatus.IsInGame)
                {
                    LoggerHelper.LogWarning("服务端不在关卡内，断开连接");
                    SetStatusText("[!] 服务端不在关卡内", ModernColors.Warning);
                    
                    // 断开连接
                    if (connectedPeer != null)
                    {
                        connectedPeer.Disconnect();
                    }
                    return;
                }
                break;
            }
        }
    }
}
```

### 3. 玩家条目创建

```csharp
private void CreatePlayerEntry(Utils.Database.PlayerInfoEntity player)
{
    var entry = CreateModernCard(_components.PlayerListContent, $"Player_{player.SteamId}");
    
    // 本地玩家特殊样式
    if (player.IsLocalPlayer)
    {
        var bg = entry.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0.24f, 0.52f, 0.98f, 0.15f); // 蓝色半透明
            var outline = entry.AddComponent<Outline>();
            outline.effectColor = ModernColors.Primary;
            outline.effectDistance = new Vector2(2, -2);
        }
    }
    
    // 状态指示器（从 CustomData 读取）
    bool isInGame = player.CustomData.TryGetValue("IsInGame", out var inGameObj)
        && inGameObj is bool inGameValue && inGameValue;
    
    var statusDot = new GameObject("StatusDot");
    // ...
    dotImage.color = isInGame ? ModernColors.Success : ModernColors.Warning;
    
    // 显示玩家名称
    var nameText = CreateText("Name", headerRow.transform, player.PlayerName, 16,
        ModernColors.TextPrimary, TextAlignmentOptions.Left, FontStyles.Bold);
    
    // 本地玩家标签
    if (player.IsLocalPlayer)
    {
        CreateBadge(headerRow.transform, "本地", ModernColors.Primary);
    }
    
    // 显示延迟
    int latency = 0;
    if (player.CustomData.TryGetValue("Latency", out var latencyObj) && latencyObj is int latencyValue)
    {
        latency = latencyValue;
    }
    
    var pingText = CreateText("Ping", infoRow.transform, $"{latency}ms", 13,
        latency < 50 ? ModernColors.Success :
        latency < 100 ? ModernColors.Warning : ModernColors.Error);
    
    // 保存延迟文本引用
    _playerPingTexts[player.SteamId] = pingText;
    
    // 踢人按钮（只有主机且不是本地玩家时显示）
    if (IsServer && !player.IsLocalPlayer && SteamManager.Initialized)
    {
        if (ulong.TryParse(player.SteamId, out ulong targetSteamId) && targetSteamId > 0)
        {
            var kickButton = CreateIconButton("KickBtn", infoRow.transform, "踢", () =>
            {
                KickMessage.Server_KickPlayer(targetSteamId, "被主机踢出");
            }, 50, ModernColors.Error);
        }
    }
}
```

### 4. 投票玩家名称显示

```csharp
// 优先从投票数据中获取 Steam 名字
if (SceneNet.Instance.cachedVoteData?.playerList?.items != null)
{
    foreach (var player in SceneNet.Instance.cachedVoteData.playerList.items)
    {
        if (player.playerId == pid && !string.IsNullOrEmpty(player.steamName))
        {
            // 判断是否是主机
            bool isHost = player.playerId.StartsWith("Host:");
            string prefix = isHost ? "HOST" : "CLIENT";
            displayName = $"{prefix}_{player.steamName}";
            displayId = player.steamId;
            break;
        }
    }
}
```

---

## 🎨 UI Helper方法

### 1. 面板动画

```csharp
internal IEnumerator AnimatePanel(GameObject panel, bool show)
{
    if (show)
    {
        panel.SetActive(true);
        var canvasGroup = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        
        float time = 0;
        while (time < 0.2f)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, time / 0.2f);
            yield return null;
        }
        canvasGroup.alpha = 1;
    }
    else
    {
        var canvasGroup = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
        
        float time = 0;
        while (time < 0.15f)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, time / 0.15f);
            yield return null;
        }
        panel.SetActive(false);
    }
}
```

### 2. 现代化面板创建

```csharp
internal GameObject CreateModernPanel(string name, Transform parent, Vector2 size, Vector2 anchorPos, TextAnchor pivot = TextAnchor.UpperLeft)
{
    var panel = new GameObject(name);
    panel.transform.SetParent(parent, false);
    
    var rect = panel.AddComponent<RectTransform>();
    rect.sizeDelta = size;
    SetAnchor(rect, anchorPos, pivot);
    
    // 尝试使用真实高斯模糊玻璃效果
    bool useTranslucentImage = false;
    TranslucentImage translucentImage = null;
    
    try
    {
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var source = mainCamera.GetComponent<TranslucentImageSource>();
            if (source != null)
            {
                translucentImage = panel.AddComponent<TranslucentImage>();
                translucentImage.source = source;
                translucentImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
                useTranslucentImage = true;
            }
        }
    }
    catch (System.Exception e)
    {
        // 回退到普通背景
    }
    
    // 如果模糊失败，使用普通背景
    if (!useTranslucentImage)
    {
        var bg = panel.AddComponent<Image>();
        bg.color = GlassTheme.PanelBg;
        bg.sprite = CreateEmbeddedNoiseSprite();
        bg.type = Image.Type.Tiled;
    }
    
    return panel;
}
```

---

## 🔍 调试功能

### 1. 数据库调试

**快捷键**：
- `F9`：输出 PlayerInfoDatabase 内容
- `F10`：测试 CustomData 功能

```csharp
// F9 按下 - 输出 PlayerInfoDatabase 调试信息
if (Input.GetKeyDown(KeyCode.F9))
{
    LoggerHelper.Log("[MModUI] F9 按下 - 输出 PlayerInfoDatabase 调试信息");
    Utils.Database.PlayerInfoDatabase.Instance.DebugPrintDatabase();
}

// F10 按下 - 测试 CustomData 功能
if (Input.GetKeyDown(KeyCode.F10))
{
    LoggerHelper.Log("[MModUI] F10 按下 - 测试 CustomData 功能");
    Utils.Database.PlayerInfoDatabase.Instance.DebugTestCustomData();
}
```

---

## 🔗 相关模块

- **NetService**: 网络服务核心
- **PlayerInfoDatabase**: 玩家信息数据库
- **SceneNet**: 场景网络管理
- **ClientStatusMessage**: 客户端状态上报
- **SteamLobbyManager**: Steam Lobby 管理
- **CoopLocalization**: 多语言支持

---

## 📝 最近更新

### 2024-11-11
- ✅ 集成 PlayerInfoDatabase 数据源
- ✅ 实现实时延迟更新（每秒）
- ✅ 添加服务端关卡检查（每2秒）
- ✅ 优化投票面板玩家名称显示
- ✅ 添加光标指示器（红色圆点）
- ✅ 添加踢人功能（仅主机）
- ✅ 添加调试快捷键（F9/F10）

---

*文档版本: 1.0.0*  
*最后更新: 2024-11-11*
