# JSON消息系统 - 简明总结

## 修改内容

### 1. 操作码更新
- **Op.cs**: `JSON_TEST` → `JSON` (Op = 200)

### 2. 日志简化
所有日志输出已简化为单行：

**发送日志：**
- 广播: `[JSON] 广播到 N 个客户端`
- 点对点: `[JSON] 发送到 {peer.EndPoint}`

**接收日志：**
- 原始JSON: `[JSON] 收到消息`
- 解析对象: `[JSON] 收到 {TypeName}`
- 解析失败: `[JSON] 解析失败: {错误信息}`

### 3. 核心API

```csharp
// 服务器广播
JsonMessage.BroadcastToAllClients(data);

// 发送给指定peer
JsonMessage.SendToPeer(peer, data);

// 客户端发送给主机
JsonMessage.SendToHost(data);

// 接收处理
JsonMessage.HandleReceivedJson<T>(reader, data => {
    // 处理数据
});
```

## 日志示例

### 连接成功后的日志输出

**服务器端：**
```
[08:48:19] 连接成功: 192.168.137.30:64670
[08:48:19] [JSON] 发送到 192.168.137.30:64670
[08:48:19] [JSON] 收到 TestJsonData
```

**客户端：**
```
[08:48:15] 连接成功: 192.168.123.1:9050
[08:48:15] [JSON] 发送到 192.168.123.1:9050
[08:48:15] [JSON] 收到 TestJsonData
```

## 完整文档

详细使用方法请参考：
- **EscapeFromDuckovCoopMod/Net/JsonMessage_Usage.md** - 完整API文档和示例
- **.kiro/steering/json-test-feature.md** - 功能说明
