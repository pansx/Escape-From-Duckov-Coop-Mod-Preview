# JSON测试消息调试指南

## 当前状态

已实现JSON测试消息功能，但客户端可能收不到服务器发送的消息。

## 调试步骤

### 1. 关闭游戏后重新编译

```bash
dotnet build EscapeFromDuckovCoopMod.sln
```

### 2. 查看日志关键字

启动游戏后，在日志中搜索以下关键字：

**发送端日志：**
```
[JsonTest] 准备发送JSON - 身份: 服务器/客户端
[JsonTest] 已发送JSON到
```

**接收端日志：**
```
[JsonTest] 收到 JSON_TEST 操作码
[JsonTest] ========== 收到JSON消息 ==========
[JsonTest] 原始JSON:
[JsonTest] 解析后数据:
```

### 3. 预期行为

- **服务器端**：连接建立后应该看到"准备发送JSON - 身份: 服务器"
- **客户端**：连接建立后应该看到"准备发送JSON - 身份: 客户端"
- **双方**：都应该收到对方发送的JSON消息

### 4. 可能的问题

#### 问题1：客户端没有收到服务器的消息

**原因**：消息可能在连接完全建立前就发送了

**解决方案**：在 NetService.cs 的 OnPeerConnected 末尾添加延迟发送

```csharp
// 延迟发送，确保连接完全建立
StartCoroutine(DelayedSendTestJson(peer));

private IEnumerator DelayedSendTestJson(NetPeer peer)
{
    yield return new WaitForSeconds(0.5f);
    JsonTestMessage.SendTestJson(peer, writer);
}
```

#### 问题2：消息被其他逻辑拦截

**检查**：在 Mod.cs 的 OnNetworkReceive 开头添加的调试日志是否输出

```csharp
if (op == Op.JSON_TEST)
{
    Debug.Log($"[JsonTest] 收到 JSON_TEST 操作码，可用字节: {reader.AvailableBytes}");
}
```

如果这条日志都没有，说明消息根本没到达接收处理函数。

#### 问题3：操作码不匹配

**检查**：确认 Op.JSON_TEST = 200 在双端都是相同的值

### 5. 手动测试方法

1. 启动两个游戏实例
2. 一个作为服务器（创建主机）
3. 另一个作为客户端（连接到 192.168.123.1:9050）
4. 连接成功后立即查看日志
5. 搜索 `[JsonTest]` 关键字

### 6. 如果还是不行

可以尝试在客户端主动请求服务器发送：

在客户端的 `Send_ClientStatus.SendClientStatusUpdate()` 中添加：
```csharp
// 请求服务器发送测试JSON
JsonTestMessage.SendTestJson(connectedPeer, writer);
```

## 当前代码位置

- **JsonTestMessage.cs**: `EscapeFromDuckovCoopMod/Net/JsonTestMessage.cs`
- **Op.JSON_TEST**: `EscapeFromDuckovCoopMod/Main/Op.cs` 第100行
- **消息处理**: `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs` 第747行
- **发送逻辑**: `EscapeFromDuckovCoopMod/Main/NetService.cs` 第195行
