# JsonMessage 使用指南

## 概述

`JsonMessage` 是一个通用的JSON消息发送和接收工具类，提供了便捷的方法来在服务器和客户端之间传输JSON数据。

## 主要功能

### 1. 广播消息给所有客户端（服务器端）

```csharp
// 方法1：发送JSON字符串
string json = "{\"type\":\"update\",\"value\":123}";
JsonMessage.BroadcastToAllClients(json);

// 方法2：发送对象（自动序列化）
var data = new MyData { type = "update", value = 123 };
JsonMessage.BroadcastToAllClients(data);

// 指定传输方式
JsonMessage.BroadcastToAllClients(data, DeliveryMethod.Unreliable);
```

### 2. 发送消息给指定Peer

```csharp
// 方法1：发送JSON字符串
NetPeer targetPeer = ...; // 获取目标peer
string json = "{\"message\":\"Hello\"}";
JsonMessage.SendToPeer(targetPeer, json);

// 方法2：发送对象（自动序列化）
var data = new ChatMessage { sender = "Player1", text = "Hello!" };
JsonMessage.SendToPeer(targetPeer, data);

// 指定传输方式
JsonMessage.SendToPeer(targetPeer, data, DeliveryMethod.ReliableUnordered);
```

### 3. 发送消息给主机（客户端）

```csharp
// 方法1：发送JSON字符串
string json = "{\"action\":\"request\",\"id\":1}";
JsonMessage.SendToHost(json);

// 方法2：发送对象（自动序列化）
var request = new PlayerRequest { action = "pickup", itemId = 42 };
JsonMessage.SendToHost(request);

// 指定传输方式
JsonMessage.SendToHost(request, DeliveryMethod.ReliableOrdered);
```

### 4. 接收和处理JSON消息

在 `Mod.cs` 的 `OnNetworkReceive` 中已经处理了 `Op.JSON_TEST` 操作码。

#### 方法1：接收原始JSON字符串

```csharp
case Op.JSON_TEST:
    JsonMessage.HandleReceivedJson(reader, json =>
    {
        Debug.Log($"收到JSON: {json}");
        // 自定义处理逻辑
    });
    break;
```

#### 方法2：接收并自动解析为对象

```csharp
case Op.JSON_TEST:
    JsonMessage.HandleReceivedJson<MyData>(reader, data =>
    {
        Debug.Log($"收到数据: type={data.type}, value={data.value}");
        // 使用解析后的对象
    });
    break;
```

## 完整使用示例

### 示例1：服务器广播游戏状态

```csharp
// 定义数据结构
[System.Serializable]
public class GameStateData
{
    public string eventType;
    public int roundNumber;
    public float timeRemaining;
}

// 服务器端发送
if (NetService.Instance.IsServer)
{
    var gameState = new GameStateData
    {
        eventType = "round_start",
        roundNumber = 3,
        timeRemaining = 300f
    };
    
    JsonMessage.BroadcastToAllClients(gameState);
}

// 客户端接收（在Mod.cs的OnNetworkReceive中）
case Op.JSON_TEST:
    JsonMessage.HandleReceivedJson<GameStateData>(reader, data =>
    {
        Debug.Log($"游戏状态更新: 第{data.roundNumber}轮, 剩余{data.timeRemaining}秒");
        // 更新UI或游戏逻辑
    });
    break;
```

### 示例2：客户端向服务器发送请求

```csharp
// 定义请求数据结构
[System.Serializable]
public class ItemPickupRequest
{
    public string playerId;
    public int itemId;
    public string itemName;
}

// 客户端发送请求
if (!NetService.Instance.IsServer)
{
    var request = new ItemPickupRequest
    {
        playerId = "Player_123",
        itemId = 42,
        itemName = "AK-47"
    };
    
    JsonMessage.SendToHost(request);
}

// 服务器接收（在Mod.cs的OnNetworkReceive中）
case Op.JSON_TEST:
    if (IsServer)
    {
        JsonMessage.HandleReceivedJson<ItemPickupRequest>(reader, request =>
        {
            Debug.Log($"玩家 {request.playerId} 请求拾取 {request.itemName}");
            // 处理拾取逻辑
            // 验证并执行拾取
        });
    }
    break;
```

### 示例3：点对点消息（服务器转发）

```csharp
// 定义私聊消息结构
[System.Serializable]
public class PrivateMessage
{
    public string fromPlayerId;
    public string toPlayerId;
    public string message;
}

// 客户端A发送给服务器
var pm = new PrivateMessage
{
    fromPlayerId = "PlayerA",
    toPlayerId = "PlayerB",
    message = "Hello PlayerB!"
};
JsonMessage.SendToHost(pm);

// 服务器接收并转发给目标客户端
case Op.JSON_TEST:
    if (IsServer)
    {
        JsonMessage.HandleReceivedJson<PrivateMessage>(reader, pm =>
        {
            // 查找目标玩家的peer
            NetPeer targetPeer = FindPeerByPlayerId(pm.toPlayerId);
            if (targetPeer != null)
            {
                JsonMessage.SendToPeer(targetPeer, pm);
            }
        });
    }
    else
    {
        // 客户端B接收
        JsonMessage.HandleReceivedJson<PrivateMessage>(reader, pm =>
        {
            Debug.Log($"来自 {pm.fromPlayerId} 的消息: {pm.message}");
        });
    }
    break;
```

## 传输方式说明

- **ReliableOrdered** (默认): 可靠有序，保证消息按顺序到达
- **ReliableUnordered**: 可靠无序，保证到达但不保证顺序
- **Unreliable**: 不可靠，快速但可能丢失
- **ReliableSequenced**: 可靠序列化，只保留最新的消息

## 注意事项

1. **数据结构要求**：
   - 必须标记 `[System.Serializable]` 属性
   - 字段必须是 public 或标记 `[SerializeField]`
   - Unity的 JsonUtility 不支持字典、多维数组等复杂类型

2. **性能考虑**：
   - 避免频繁发送大量数据
   - 对于高频更新（如位置同步），考虑使用专用的二进制协议
   - 广播消息会发送给所有客户端，注意带宽消耗

3. **错误处理**：
   - 所有方法都包含空值检查和错误日志
   - JSON解析失败会记录错误但不会崩溃

4. **调试**：
   - 所有发送和接收都会在日志中输出 `[JsonMessage]` 标记
   - 可以通过日志服务器查看完整的消息流

## 扩展操作码

如果需要区分不同类型的JSON消息，可以在 `Op.cs` 中添加新的操作码：

```csharp
public enum Op : byte
{
    // ... 现有操作码
    JSON_TEST = 200,
    JSON_GAME_STATE = 201,
    JSON_CHAT_MESSAGE = 202,
    JSON_PLAYER_REQUEST = 203,
    // ...
}
```

然后在 `Mod.cs` 中为每个操作码添加对应的处理逻辑。
