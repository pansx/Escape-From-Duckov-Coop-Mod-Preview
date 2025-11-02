# ModUI API 参考文档

本文档记录了 `ModUI` 类中所有现有的方法，以避免重复定义。

## 公共方法 (Public Methods)

### 初始化和生命周期

- `public void Init()` - 初始化 ModUI

### 聊天相关

- `public void AddChatMessage(string displayMessage)` - 添加聊天消息到显示列表（供网络接收使用）
  - **参数**: `displayMessage` - 格式化的显示消息
  - **功能**: 将消息添加到 `_chatMessages` 列表，限制最大消息数量
  - **注意**: 此方法已存在，不要重复定义！

## 私有方法 (Private Methods)

### UI 状态管理

- `private void ChangeUIState(UIState newState)` - 切换 UI 状态
- `private void OnUIStateChanged(UIState previousState, UIState newState)` - 状态变化时的处理逻辑
- `private void OnEnterMainLobby(UIState previousState)` - 进入主大厅状态的处理
- `private void OnEnterRoom(UIState previousState)` - 进入房间状态的处理
- `private void InitializeUIState()` - 初始化 UI 状态

### Steam 大厅相关

- `private void InitializeSteamLobbyMemberTracking()` - 初始化 Steam 大厅成员跟踪
- `private void UpdateRoomData()` - 更新房间数据
- `private void MonitorSteamLobbyMembers()` - 监听 Steam 大厅成员变化
- `private void OnLobbyListUpdated(IReadOnlyList<SteamLobbyManager.LobbyInfo> lobbies)` - 大厅列表更新回调
- `private void UpdateLobbyOptionsFromUI()` - 从 UI 更新大厅选项
- `private void AttemptSteamLobbyJoin(SteamLobbyManager.LobbyInfo lobby)` - 尝试加入 Steam 大厅

### 聊天系统

- `private void InitializeChatSystem()` - 初始化聊天系统
- `private void HandleChatInput()` - 处理聊天输入
- `private void HandleChatMessageSent(string messageContent)` - 处理聊天消息发送
- `private void SendChatMessageToNetwork(EscapeFromDuckovCoopMod.Chat.Models.ChatMessage message)` - 发送聊天消息到网络

### 连接状态监控

- `private void MonitorConnectionStatus()` - 连接状态监听
- `private void CheckRoomEntryStatus()` - 检查是否成功进入房间
- `private void CheckConnectionFailureStatus()` - 检查连接失败状态
- `private void HandleConnectionLoss(string reason)` - 处理连接丢失
- `private void ShowConnectionError(string errorMessage)` - 显示连接错误

### UI 绘制方法

- `private void OnGUI()` - Unity OnGUI 回调
- `private void DrawMainWindow(int windowID)` - 绘制主窗口
- `private void DrawMainLobbyInterface()` - 绘制主大厅界面
- `private void DrawRoomInterface()` - 绘制房间界面
- `private void DrawRoomCreationSection()` - 绘制房间创建区域
- `private void DrawRoomListSection()` - 绘制房间列表区域
- `private void DrawDirectConnectSection()` - 绘制直连区域
- `private void DrawRoomHeaderSection()` - 绘制房间头部区域
- `private void DrawRoomSettingsSection()` - 绘制房间设置区域
- `private void DrawPlayerListSection()` - 绘制玩家列表区域
- `private void DrawPlayerEntry(string playerName, int latency, bool isHost, bool canKick, bool showKickButton)` - 绘制玩家条目
- `private void DrawTransportModeSelector()` - 绘制传输模式选择器
- `private void DrawDirectClientSection()` - 绘制直连客户端区域
- `private void DrawDirectServerSection()` - 绘制直连服务器区域
- `private void DrawSteamClientSection()` - 绘制 Steam 客户端区域
- `private void DrawSteamServerSection()` - 绘制 Steam 服务器区域
- `private void DrawSteamMode()` - 绘制 Steam 模式

### 房间管理

- `private void CreateRoom()` - 创建房间
- `private void RefreshRoomList()` - 刷新房间列表
- `private void ConnectDirect()` - 直连连接
- `private string GetCurrentRoomName()` - 获取当前房间名称
- `private void LeaveRoom()` - 离开房间
- `private void PerformRoomCleanup()` - 执行房间清理
- `private void ClearRoomData()` - 清理房间数据
- `private void EnsureStateReset()` - 确保状态重置

### 辅助方法

- `private string GetLatencyDisplayText(int latency)` - 获取延迟显示文本

### 生命周期

- `private void OnDestroy()` - Unity OnDestroy 回调

## 字段 (Fields)

### 聊天相关字段

- `private readonly List<string> _chatMessages` - 聊天消息存储列表
- `private readonly int _maxChatMessages = 10` - 最大聊天消息数量

### UI 状态字段

- `private UIState _currentUIState = UIState.MainLobby` - 当前 UI 状态

### 其他字段

- `public static ModUI Instance` - 单例实例
- `public bool showUI` - 是否显示 UI
- `public bool showPlayerStatusWindow` - 是否显示玩家状态窗口
- `public KeyCode toggleWindowKey` - 切换窗口按键

## 使用注意事项

1. **不要重复定义 `AddChatMessage` 方法** - 此方法已存在于 ModUI 中
2. **聊天消息通过 `AddChatMessage(string)` 添加** - 接受格式化的字符串
3. **消息自动限制数量** - 超过 `_maxChatMessages` 时自动删除旧消息
4. **UI 状态管理** - 使用 `ChangeUIState` 切换状态，不要直接修改 `_currentUIState`

## 集成示例

### 从 ChatUIManager 调用

```csharp
// 正确的方式
ModUI.Instance?.AddChatMessage(message.GetDisplayText());

// 错误的方式 - 不要尝试添加新的 AddChatMessage 方法
```

### 消息格式

```csharp
// 消息应该是格式化的字符串
string displayMessage = $"{userName}: {content} {timestamp}";
ModUI.Instance?.AddChatMessage(displayMessage);
```
