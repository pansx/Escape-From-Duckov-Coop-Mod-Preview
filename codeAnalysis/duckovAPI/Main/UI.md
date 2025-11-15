# Main/UI 模块 API 文档

## 模块概述

UI 模块负责用户界面，包括联机菜单、玩家列表、聊天系统等。

## 文件列表

| 文件名                        | 说明                    |
| ----------------------------- | ----------------------- |
| `ModUI.cs`                    | 模组 UI 主类            |
| `MModUI.cs`                   | 模组 UI 管理器          |
| `MModUIComponents.cs`         | UI 组件                 |
| `MModUILayoutBuilder.cs`      | UI 布局构建器           |
| `WaitingSynchronizationUI.cs` | 场景加载同步等待界面 ⭐ |

## 核心功能

-   联机菜单界面

-   玩家列表显示

-   聊天系统

-   Steam Lobby 界面

-   连接状态显示

-   场景加载同步等待界面 ⭐ (新增)

## 依赖关系

**依赖模块**：

-   NetService - 网络状态

-   Net/Steam/SteamLobbyManager - Lobby 管理

**Unity 依赖**：

-   UGUI 系统

-   TextMeshPro

---

## WaitingSynchronizationUI - 场景加载同步等待界面 ⭐

### 概述

`WaitingSynchronizationUI` 是场景加载期间显示的全屏同步等待界面，提供可视化进度反馈和玩家信息显示。

**文件**: `EscapeFromDuckovCoopMod/Main/UI/WaitingSynchronizationUI.cs`  
**行数**: 1128 行  
**新增日期**: 2025-11-08  
**作者**: Neko17awa

### 核心特性

#### 1. 全屏 UI 布局

```
┌─────────────────────────────────────────────────────────────┐
│                    正在加载场景...                      [✕] │  ← 关闭按钮
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐                      ┌──────────────────┐ │
│  │ 玩家列表     │                      │ 地图信息         │ │
│  │ HOST_Player1 │                      │ 地图: 农场镇     │ │
│  │ [头像] 96x96 │                      │ 时间: 第1天 08:00│ │
│  │ CLIENT_Plr2  │                      │ 天气: 晴天       │ │
│  └──────────────┘                      └──────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                  [旋转动画] 同步环境数据... 75%              │
└─────────────────────────────────────────────────────────────┘
```

**布局区域**:

-   **顶部**: 标题文字 "正在加载场景..."
-   **右上角**: 关闭按钮（X）⭐ 新增
-   **左侧**: 玩家列表（带 Steam 头像）
-   **右侧**: 地图信息、游戏时间、天气信息
-   **底部**: 同步进度百分比 + 旋转加载动画

#### 2. Steam 集成

**Steam 头像加载**:

```csharp
/// <summary>
/// 创建玩家头像（Steam P2P模式下加载真实头像）
/// </summary>
private GameObject CreatePlayerAvatar(ulong steamId = 0)
{
    bool isSteamMode = NetService.Instance?.TransportMode == NetworkTransportMode.SteamP2P;
    bool canLoadSteamAvatar = steamId > 0 && isSteamMode && SteamManager.Initialized;

    if (canLoadSteamAvatar)
    {
        // 异步加载Steam头像
        StartCoroutine(LoadSteamAvatar(new CSteamID(steamId), image));
    }
    else
    {
        // 创建默认头像（圆形 + 简单人像）
        CreateDefaultAvatar(image);
    }
}
```

**Steam 用户名获取**:

```csharp
// 从 LobbyManager 缓存获取
var lobbyManager = SteamLobbyManager.Instance;
if (lobbyManager != null && lobbyManager.IsInLobby)
{
    steamUsername = lobbyManager.GetCachedMemberName(cSteamId);

    // 判断是否是主机
    var lobbyOwner = SteamMatchmaking.GetLobbyOwner(lobbyManager.CurrentLobbyId);
    bool isHost = (steamId == lobbyOwner.m_SteamID);
    string prefix = isHost ? "HOST" : "CLIENT";
    displayName = $"{prefix}_{steamUsername}";
}
```

**头像缓存机制**:

```csharp
// Steam头像缓存，避免重复加载
private Dictionary<ulong, Sprite> _steamAvatarCache = new Dictionary<ulong, Sprite>();

// 加载时先检查缓存
if (_steamAvatarCache.TryGetValue(steamId, out var cachedSprite))
{
    image.sprite = cachedSprite;
    image.color = Color.white;
}
```

#### 3. 任务追踪系统

**任务状态结构**:

```csharp
public class SyncTaskStatus
{
    public string Name;          // 任务名称
    public bool IsCompleted;     // 是否完成
    public string Details;       // 详细信息
}

private Dictionary<string, SyncTaskStatus> _syncTasks = new Dictionary<string, SyncTaskStatus>();
```

**任务管理方法**:

```csharp
/// <summary>
/// 注册同步任务
/// </summary>
public void RegisterTask(string taskId, string taskName)
{
    if (!_syncTasks.ContainsKey(taskId))
    {
        _syncTasks[taskId] = new SyncTaskStatus
        {
            Name = taskName,
            IsCompleted = false,
            Details = ""
        };
    }
}

/// <summary>
/// 更新任务状态
/// </summary>
public void UpdateTaskStatus(string taskId, bool isCompleted, string details = "")
{
    if (_syncTasks.TryGetValue(taskId, out var task))
    {
        task.IsCompleted = isCompleted;
        task.Details = details;
    }
}

/// <summary>
/// 标记任务完成
/// </summary>
public void CompleteTask(string taskId, string details = "")
{
    UpdateTaskStatus(taskId, true, details);
}
```

**进度计算**:

```csharp
private void UpdateProgressDisplay()
{
    int completed = _syncTasks.Count(t => t.Value.IsCompleted);
    int total = _syncTasks.Count;

    // 计算百分比
    float percent = (float)completed / total * 100f;
    _syncPercentText.text = $"{percent:F0}%";

    // 根据进度改变颜色
    if (percent >= 100f)
        _syncPercentText.color = new Color(0.5f, 1f, 0.5f, 1f); // 绿色
    else if (percent >= 50f)
        _syncPercentText.color = new Color(1f, 1f, 0.5f, 1f); // 黄色
    else
        _syncPercentText.color = new Color(1f, 0.7f, 0.5f, 1f); // 橙色
}
```

#### 4. 地图和天气信息

**实时更新**:

```csharp
private void UpdateMapAndWeatherInfo()
{
    // 更新地图信息
    var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    _mapInfoText.text = $"地图: {GetMapDisplayName(currentScene.name)}";

    // 更新游戏时间 - 使用 GameClock
    var day = GameClock.Day;
    var timeOfDay = GameClock.TimeOfDay;
    var hours = timeOfDay.Hours;
    var minutes = timeOfDay.Minutes;
    _timeInfoText.text = $"时间: 第{day}天 {hours:D2}:{minutes:D2}";

    // 更新天气信息 - 使用 TimeOfDayController
    if (TimeOfDayController.Instance != null)
    {
        var currentWeather = TimeOfDayController.Instance.CurrentWeather;
        var weatherName = TimeOfDayController.GetWeatherNameByWeather(currentWeather);
        _weatherInfoText.text = $"天气: {weatherName}";
    }
}
```

**地图名称映射**:

```csharp
private string GetMapDisplayName(string sceneName)
{
    switch (sceneName)
    {
        case "Level_GroundZero_Main": return "零号地区";
        case "Level_Factory_Main": return "工厂";
        case "Level_Hospital_Main": return "医院";
        case "Level_Suburb_Main": return "郊区";
        case "Level_Downtown_Main": return "市区";
        case "Base": return "避难所";
        default: return sceneName;
    }
}
```

#### 5. 手动关闭按钮 ⭐

**右上角关闭按钮**:

```csharp
/// <summary>
/// 创建右上角关闭按钮
/// </summary>
private void CreateCloseButton()
{
    var closeButtonObj = new GameObject("CloseButton");
    closeButtonObj.transform.SetParent(_panel.transform);

    var buttonRect = closeButtonObj.AddComponent<RectTransform>();
    buttonRect.anchorMin = new Vector2(1, 1);
    buttonRect.anchorMax = new Vector2(1, 1);
    buttonRect.pivot = new Vector2(1, 1);
    buttonRect.anchoredPosition = new Vector2(-30, -30); // 距离右上角 30 像素
    buttonRect.sizeDelta = new Vector2(60, 60); // 60x60 的按钮

    var button = closeButtonObj.AddComponent<Button>();
    var buttonImage = closeButtonObj.AddComponent<Image>();
    buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f); // 半透明红色

    // 创建 X 图标
    var xIconText = xIconObj.AddComponent<TextMeshProUGUI>();
    xIconText.text = "✕";
    xIconText.fontSize = 40;
    xIconText.fontStyle = FontStyles.Bold;
    xIconText.color = Color.white;
    xIconText.alignment = TextAlignmentOptions.Center;

    // 按钮点击事件
    button.onClick.AddListener(() =>
    {
        Debug.Log("[SYNC_UI] 用户点击关闭按钮");
        Hide();
    });

    // 悬停效果
    var colors = button.colors;
    colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
    colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
    colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
    button.colors = colors;
}
```

**特性**:

-   位置：右上角，距离边缘 30 像素
-   尺寸：60x60 像素
-   颜色：半透明红色，悬停时变亮
-   图标：白色 X 符号（✕）
-   功能：点击后调用 `Hide()` 方法关闭 UI

#### 6. 视觉效果

**淡出动画**:

```csharp
/// <summary>
/// 淡出协程
/// </summary>
private IEnumerator FadeOut()
{
    float duration = 0.5f; // 淡出持续时间（秒）
    float elapsed = 0f;
    float startAlpha = _canvasGroup.alpha;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float normalizedTime = elapsed / duration;
        _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalizedTime);
        yield return null;
    }

    _canvasGroup.alpha = 0f;
    _panel.SetActive(false);
}
```

**旋转加载动画**:

```csharp
private void Update()
{
    // 旋转加载动画
    if (_loadingAnimation != null && _loadingAnimation.activeSelf)
    {
        _loadingRotation += 360f * Time.deltaTime; // 每秒旋转360度
        if (_loadingRotation >= 360f) _loadingRotation -= 360f;
        _loadingAnimation.transform.rotation = Quaternion.Euler(0, 0, -_loadingRotation);
    }
}
```

**背景图片加载**:

```csharp
private void LoadBackgroundImage(Image targetImage)
{
    var modPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    var bgPath = Path.Combine(modPath, "Assets", "bg.png");

    if (File.Exists(bgPath))
    {
        var fileData = File.ReadAllBytes(bgPath);
        var texture = new Texture2D(2, 2);
        if (texture.LoadImage(fileData))
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
            targetImage.sprite = sprite;
            targetImage.color = Color.white;
        }
    }
}
```

### 使用方式

#### 显示 UI

```csharp
// 场景开始加载时
WaitingSynchronizationUI.Instance.Show();

// 注册同步任务
WaitingSynchronizationUI.Instance.RegisterTask("env_sync", "同步环境数据");
WaitingSynchronizationUI.Instance.RegisterTask("ai_sync", "同步AI装备");
WaitingSynchronizationUI.Instance.RegisterTask("loot_sync", "同步战利品状态");
WaitingSynchronizationUI.Instance.RegisterTask("player_sync", "同步玩家状态");

// 更新玩家列表
WaitingSynchronizationUI.Instance.UpdatePlayerList();
```

#### 更新进度

```csharp
// 更新任务状态
WaitingSynchronizationUI.Instance.UpdateTaskStatus("env_sync", false, "正在同步...");

// 标记任务完成
WaitingSynchronizationUI.Instance.CompleteTask("env_sync", "完成");
```

#### 隐藏 UI

```csharp
// 方法1: 淡出隐藏（推荐）
// 所有任务完成后自动隐藏（带淡出动画）
WaitingSynchronizationUI.Instance.Hide();

// 方法2: 立即关闭（发送完成消息）
// 用于正常关闭流程，会发送完成同步UI消息并启动无敌计时器
WaitingSynchronizationUI.Instance.Close();

// 方法3: 强制关闭（外部调用）
// 用于场景卸载等外部请求，如果UI可见则调用Close()
WaitingSynchronizationUI.Instance.ForceCloseIfVisible("场景卸载");
```

**关闭方法说明**:

- `Hide()`: 带淡出动画的隐藏，所有任务完成后自动调用
- `Close()`: 立即关闭UI，会发送完成同步UI消息给主机，并启动无敌计时器（延迟解除无敌）
- `ForceCloseIfVisible()`: 供外部调用的强制关闭方法，内部调用`Close()`

**重要**: 超时保护触发时也会调用`Close()`方法，确保即使超时也会发送完成消息，不会破坏传送逻辑。

### 技术亮点

#### 1. 异步 Steam 头像加载

```csharp
private IEnumerator LoadSteamAvatar(CSteamID steamId, Image targetImage)
{
    // 尝试获取头像句柄（大头像）
    int avatarHandle = SteamFriends.GetLargeFriendAvatar(steamId);

    // 如果大头像不可用，尝试中等头像
    if (avatarHandle == -1)
    {
        avatarHandle = SteamFriends.GetMediumFriendAvatar(steamId);
    }

    // 等待头像加载完成
    int maxRetries = 10;
    int retryCount = 0;
    while (avatarHandle == -1 && retryCount < maxRetries)
    {
        yield return new WaitForSeconds(0.1f);
        avatarHandle = SteamFriends.GetLargeFriendAvatar(steamId);
        if (avatarHandle == -1)
        {
            avatarHandle = SteamFriends.GetMediumFriendAvatar(steamId);
        }
        retryCount++;
    }

    // 获取图像数据并创建Sprite
    if (avatarHandle > 0)
    {
        uint width, height;
        if (SteamUtils.GetImageSize(avatarHandle, out width, out height))
        {
            byte[] imageData = new byte[width * height * 4];
            if (SteamUtils.GetImageRGBA(avatarHandle, imageData, (int)(width * height * 4)))
            {
                Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                texture.LoadRawTextureData(imageData);
                texture.Apply();

                // 翻转图像（Steam图像是上下颠倒的）
                FlipTextureVertically(texture);

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                _steamAvatarCache[steamId.m_SteamID] = sprite;

                if (targetImage != null)
                {
                    targetImage.sprite = sprite;
                    targetImage.color = Color.white;
                }
            }
        }
    }
}
```

#### 2. 自动完成检测

```csharp
private void CheckAndHideIfComplete()
{
    if (_syncTasks.Count == 0) return;

    bool allComplete = _syncTasks.All(t => t.Value.IsCompleted);
    if (allComplete)
    {
        _allTasksCompleted = true;
        StartCoroutine(HideAfterDelay(1f)); // 1秒后隐藏
    }
}

private IEnumerator HideAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    Hide();
}
```

#### 3. 协程管理

```csharp
// 淡出协程引用
private Coroutine _fadeOutCoroutine = null;

public void Show()
{
    // 停止正在进行的淡出协程（如果有）
    if (_fadeOutCoroutine != null)
    {
        StopCoroutine(_fadeOutCoroutine);
        _fadeOutCoroutine = null;
    }

    // 重置 alpha 为 1，确保完全显示
    if (_canvasGroup != null)
    {
        _canvasGroup.alpha = 1f;
    }

    _panel.SetActive(true);
}

public void Hide()
{
    if (_panel != null && _panel.activeSelf)
    {
        // 停止之前的淡出协程（如果有）
        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
        }

        // 启动淡出协程
        _fadeOutCoroutine = StartCoroutine(FadeOut());
    }
}
```

### 性能考虑

#### 1. 头像缓存

-   使用 `Dictionary<ulong, Sprite>` 缓存已加载的 Steam 头像
-   避免重复加载相同玩家的头像
-   减少 Steam API 调用次数

#### 2. 更新频率控制

-   地图和天气信息每帧更新（轻量级）
-   玩家列表仅在需要时更新（调用 `UpdatePlayerList()`）
-   进度显示每帧更新（轻量级）

#### 3. 协程优化

-   使用协程进行异步操作（Steam 头像加载、淡出动画）
-   避免阻塞主线程
-   合理控制协程数量

### 集成示例

#### 在场景加载流程中集成

```csharp
// Mod.cs 或 SceneNet.cs

public void OnSceneBeginLoad()
{
    // 显示同步UI
    WaitingSynchronizationUI.Instance.Show();

    // 注册所有同步任务
    WaitingSynchronizationUI.Instance.RegisterTask("env_sync", "同步环境数据");
    WaitingSynchronizationUI.Instance.RegisterTask("ai_sync", "同步AI装备");
    WaitingSynchronizationUI.Instance.RegisterTask("loot_sync", "同步战利品状态");
    WaitingSynchronizationUI.Instance.RegisterTask("player_sync", "同步玩家状态");
    WaitingSynchronizationUI.Instance.RegisterTask("destructible_sync", "同步可破坏物");

    // 更新玩家列表
    WaitingSynchronizationUI.Instance.UpdatePlayerList();
}

public void OnEnvironmentSyncComplete()
{
    WaitingSynchronizationUI.Instance.CompleteTask("env_sync", "完成");
}

public void OnAISyncComplete()
{
    WaitingSynchronizationUI.Instance.CompleteTask("ai_sync", "完成");
}

public void OnLootSyncComplete()
{
    WaitingSynchronizationUI.Instance.CompleteTask("loot_sync", "完成");
}

public void OnPlayerSyncComplete()
{
    WaitingSynchronizationUI.Instance.CompleteTask("player_sync", "完成");
}

public void OnDestructibleSyncComplete()
{
    WaitingSynchronizationUI.Instance.CompleteTask("destructible_sync", "完成");
}

public void OnAllSyncComplete()
{
    // UI会自动检测所有任务完成并隐藏
    // 或手动隐藏
    WaitingSynchronizationUI.Instance.Hide();
}
```

### 相关文档

-   [Performance_Optimization_PR.md](../../Performance_Optimization_PR.md) - 性能优化 PR 总览
-   [Main/SceneService.md](SceneService.md) - 场景服务
-   [Net/Steam.md](../Net/Steam.md) - Steam 集成

---

_最后更新: 2025-11-08_
