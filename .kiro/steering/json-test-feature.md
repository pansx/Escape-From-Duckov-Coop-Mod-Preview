---
inclusion: manual
---

# JSON消息系统

## 概述

`JsonMessage` 是一个通用的JSON消息发送和接收工具类，提供了便捷的方法在服务器和客户端之间传输JSON数据。

## 实现文件

- **EscapeFromDuckovCoopMod/Net/JsonTestMessage.cs** - JSON消息工具类（已重命名为 JsonMessage）
- **EscapeFromDuckovCoopMod/Net/JsonMessage_Usage.md** - 详细使用文档
- **EscapeFromDuckovCoopMod/Main/Op.cs** - 添加了 `JSON_TEST = 200` 操作码
- **EscapeFromDuckovCoopMod/Main/Loader/Mod.cs** - 在 `OnNetworkReceive` 中处理JSON消息
- **EscapeFromDuckovCoopMod/Main/NetService.cs** - 在 `OnPeerConnected` 中自动发送测试消息

## 核心功能

### 1. 广播消息给所有客户端（服务器端）

```csharp
// 发送对象
var data = new MyData { type = "update", value = 123 };
JsonMessage.BroadcastToAllClients(data);

// 发送JSON字符串
JsonMessage.BroadcastToAllClients("{\"type\":\"update\"}");
```

### 2. 发送消息给指定Peer

```csharp
// 发送对象
var data = new ChatMessage { sender = "Player1", text = "Hello!" };
JsonMessage.SendToPeer(targetPeer, data);

// 发送JSON字符串
JsonMessage.SendToPeer(targetPeer, "{\"message\":\"Hello\"}");
```

### 3. 发送消息给主机（客户端）

```csharp
// 发送对象
var request = new PlayerRequest { action = "pickup", itemId = 42 };
JsonMessage.SendToHost(request);

// 发送JSON字符串
JsonMessage.SendToHost("{\"action\":\"request\"}");
```

### 4. 接收和处理消息

```csharp
// 在 Mod.cs 的 OnNetworkReceive 中
case Op.JSON_TEST:
    // 方法1：接收原始JSON
    JsonMessage.HandleReceivedJson(reader, json =>
    {
        Debug.Log($"收到JSON: {json}");
    });
    
    // 方法2：自动解析为对象
    JsonMessage.HandleReceivedJson<MyData>(reader, data =>
    {
        Debug.Log($"收到数据: {data.type}");
    });
    break;
```

## 使用示例

### 服务器广播游戏状态

```csharp
[System.Serializable]
public class GameStateData
{
    public string eventType;
    public int roundNumber;
    public float timeRemaining;
}

// 服务器发送
var gameState = new GameStateData
{
    eventType = "round_start",
    roundNumber = 3,
    timeRemaining = 300f
};
JsonMessage.BroadcastToAllClients(gameState);

// 客户端接收
JsonMessage.HandleReceivedJson<GameStateData>(reader, data =>
{
    Debug.Log($"第{data.roundNumber}轮开始");
});
```

### 客户端向服务器发送请求

```csharp
[System.Serializable]
public class ItemPickupRequest
{
    public string playerId;
    public int itemId;
}

// 客户端发送
var request = new ItemPickupRequest
{
    playerId = "Player_123",
    itemId = 42
};
JsonMessage.SendToHost(request);

// 服务器接收
JsonMessage.HandleReceivedJson<ItemPickupRequest>(reader, request =>
{
    Debug.Log($"玩家 {request.playerId} 请求拾取物品 {request.itemId}");
});
```

## 测试验证

连接成功后会自动发送测试消息，日志输出示例：

```
[JsonMessage] 发送JSON到 192.168.123.1:9050:
{
    "message": "Hello from Client",
    "timestamp": "2025-11-08 08:48:15",
    "randomValue": 619
}

[JsonMessage] ========== 收到JSON消息 ==========
[JsonMessage] 原始JSON:
{
    "message": "Hello from Server",
    "timestamp": "2025-11-08 08:48:19",
    "randomValue": 338
}
[JsonMessage] 成功解析为 TestJsonData
[JsonMessage] =====================================
```

## 传输方式

- **ReliableOrdered** (默认): 可靠有序
- **ReliableUnordered**: 可靠无序
- **Unreliable**: 不可靠快速
- **ReliableSequenced**: 可靠序列化

## 注意事项

1. 数据结构必须标记 `[System.Serializable]`
2. 字段必须是 public 或标记 `[SerializeField]`
3. Unity的 JsonUtility 不支持字典、多维数组等复杂类型
4. 避免频繁发送大量数据
5. 所有操作都有完整的错误处理和日志输出

## 详细文档

完整的API文档和更多示例请参考：
**EscapeFromDuckovCoopMod/Net/JsonMessage_Usage.md**
