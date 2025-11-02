# 聊天消息去重和显示修复

## 问题描述

### 问题 1：无限循环导致崩溃

客机向主机发送聊天消息时，会出现无限循环导致游戏崩溃。日志显示同一条消息（相同的 ID）被重复处理多次。

#### 问题根源

1. 客机通过 UDP 发送消息给主机
2. 主机收到消息后调用 `HandleUDPChatMessage` 进行广播
3. 主机广播消息时，自己也会收到这条消息
4. 收到的消息再次触发广播，形成无限循环

### 问题 2：消息不显示到 UI

修复无限循环后，消息虽然不再重复，但没有显示到 UI 上。

#### 问题根源

1. `UnifiedChatTransport` 层面添加了消息去重
2. `MessageConverter` 层面也有消息去重
3. 两层去重机制冲突，导致 `MessageConverter.ConvertNetworkToDisplay` 返回 `null`
4. `ChatManager.ReceiveNetworkMessage` 没有处理 `null` 的情况，导致消息无法显示

## 解决方案

在 `UnifiedChatTransport.cs` 中添加了消息去重机制：

### 1. 添加消息 ID 缓存

```csharp
/// <summary>
/// 已处理的消息 ID 缓存（用于去重）
/// </summary>
private readonly HashSet<string> _processedMessageIds = new HashSet<string>();

/// <summary>
/// 消息 ID 缓存的最大大小
/// </summary>
private const int MAX_MESSAGE_CACHE_SIZE = 1000;
```

### 2. 实现去重检查方法

```csharp
/// <summary>
/// 检查消息是否应该被处理（去重）
/// </summary>
private bool ShouldProcessMessage(string messageJson)
{
    // 从 JSON 中提取消息 ID
    var message = ChatMessage.FromJson(messageJson);

    // 检查消息 ID 是否已处理
    if (_processedMessageIds.Contains(message.Id))
    {
        return false; // 已处理，跳过
    }

    // 添加到已处理集合
    _processedMessageIds.Add(message.Id);

    // 缓存管理：超过限制时清理旧消息
    if (_processedMessageIds.Count > MAX_MESSAGE_CACHE_SIZE)
    {
        // 清空一半旧消息
    }

    return true; // 新消息，允许处理
}
```

### 3. 在消息接收处应用去重

#### Steam P2P 消息接收

```csharp
// 检查消息是否已处理（去重）
if (!ShouldProcessMessage(messageJson))
{
    LogDebug($"忽略重复的 Steam P2P 消息: {senderId}");
    continue;
}
```

#### 直连 UDP 消息接收

```csharp
// 检查消息是否已处理（去重）
if (!ShouldProcessMessage(messageJson))
{
    LogDebug($"忽略重复的 UDP 消息: {senderEndpoint}");
    return;
}
```

## 工作原理

1. **消息唯一性**：每条 `ChatMessage` 都有唯一的 `Id` 字段（GUID）
2. **去重检查**：收到消息时，先检查 ID 是否已处理过
3. **缓存管理**：维护最近 1000 条消息的 ID，超过限制时自动清理
4. **双重保护**：同时在 Steam P2P 和直连 UDP 两个通道应用去重

## 测试建议

1. 启动主机和客机
2. 客机发送聊天消息
3. 观察日志，确认消息只被处理一次
4. 检查主机是否正常广播给其他客机
5. 验证不会出现无限循环或崩溃

### 4. 修复消息显示问题

在 `ChatManager.ReceiveNetworkMessage` 中处理 `MessageConverter` 返回 `null` 的情况：

```csharp
// 转换网络消息为显示消息
var displayMessage = _messageConverter.ConvertNetworkToDisplay(message);

// 如果转换器返回 null（可能是重复消息），直接使用原消息
if (displayMessage == null)
{
    LogDebug($"消息转换器返回 null，使用原消息: {message.Id}");
    displayMessage = message;
}
```

### 5. 连接 UI 显示

在 `ChatUIManager.Initialize` 中订阅 `LocalChatManager.OnMessageReceived` 事件：

```csharp
private void SubscribeToChatEvents()
{
    var localChatManager = Managers.LocalChatManager.Instance;
    if (localChatManager != null)
    {
        localChatManager.OnMessageReceived += HandleChatMessageReceived;
    }
}

private void HandleChatMessageReceived(ChatMessage message)
{
    // 添加消息到 UI
    AddMessage(message);

    // 通知 ModUI 更新聊天消息显示
    ModUI.Instance?.AddChatMessage(message.GetDisplayText());
}
```

在 `ModUI` 中添加 `AddChatMessage` 方法：

```csharp
public void AddChatMessage(string message)
{
    _chatMessages.Add(message);

    // 限制消息数量
    while (_chatMessages.Count > _maxChatMessages)
    {
        _chatMessages.RemoveAt(0);
    }
}
```

## 预期效果

-   ✅ 客机发送的消息只被处理一次
-   ✅ 主机正常广播给所有客机
-   ✅ 不会出现消息重复或无限循环
-   ✅ 游戏不会因聊天消息而崩溃
-   ✅ 消息正常显示到 UI 上
-   ✅ `ChatUIManager` 订阅事件并更新 `ModUI` 的消息列表
