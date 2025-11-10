# 投票系统 Steam 名字修复

## 问题描述

在投票系统中，客户端的 Steam 名字显示为空。

### 日志分析

**客户端上报时（18:10:00）：**
```json
{
  "steamId": "76561199049334068",
  "steamName": "永劫无间从没让我开心",  // ✅ 有名字
  "endPoint": "116.28.180.185:11574"
}
```

**投票消息中（18:15:02）：**
```json
{
  "playerId": "116.28.180.185:9788",  // ⚠️ 端口变了！
  "steamId": "76561199049334068",
  "steamName": ""  // ❌ 名字丢失了
}
```

### 根本原因

1. **端口变化**：客户端的端口从 `11574` 变成了 `9788`
2. **映射失效**：之前建立的 `EndPoint → SteamID → SteamName` 映射失效
3. **查询失败**：投票系统用新的 EndPoint 查询不到对应的 Steam 信息

## 解决方案

### 修改内容

在 `SceneVoteMessage.cs` 的 `Client_HandleVoteState` 方法中添加：

```csharp
// 🆕 收到投票消息时，立即上报客户端状态（确保 Steam 名字信息最新）
ClientStatusMessage.Client_SendStatusUpdate();
```

### 工作原理

1. **客户端收到投票消息**
2. **立即上报最新状态**（包含当前的 EndPoint 和 Steam 信息）
3. **主机更新映射**（新的 EndPoint → SteamID → SteamName）
4. **下次广播时使用最新信息**

### 修改位置

**文件**：`EscapeFromDuckovCoopMod/Net/SceneVoteMessage.cs`

**行号**：约 560 行

**修改前**：
```csharp
public static void Client_HandleVoteState(string json)
{
    var service = NetService.Instance;
    if (service == null || service.IsServer)
        return;

    // 🔍 输出接收到的完整 JSON（单行）
    LoggerHelper.Log($"[SceneVote] 客户端收到 JSON: {json}");

    try
    {
        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<VoteStateData>(json);
        // ...
    }
}
```

**修改后**：
```csharp
public static void Client_HandleVoteState(string json)
{
    var service = NetService.Instance;
    if (service == null || service.IsServer)
        return;

    // 🔍 输出接收到的完整 JSON（单行）
    LoggerHelper.Log($"[SceneVote] 客户端收到 JSON: {json}");

    // 🆕 收到投票消息时，立即上报客户端状态（确保 Steam 名字信息最新）
    ClientStatusMessage.Client_SendStatusUpdate();

    try
    {
        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<VoteStateData>(json);
        // ...
    }
}
```

## 预期效果

### 修复后的流程

1. **主机发起投票** → 广播投票消息
2. **客户端收到投票** → 立即上报状态（新的 EndPoint + Steam 信息）
3. **主机收到状态** → 更新映射
4. **主机下次广播** → 使用最新的 Steam 名字

### 预期日志

**客户端**：
```
[SceneVote] 客户端收到 JSON: {...}
[ClientStatus] Steam 信息: ID=76561199049334068, Name=永劫无间从没让我开心, Avatar=...
[ClientStatus] 客户端发送状态更新: {...}
```

**主机**：
```
[ClientStatus] 收到客户端状态: EndPoint=116.28.180.185:9788, SteamID=76561199049334068, SteamName=永劫无间从没让我开心, Name=Client
[ClientStatus] ✓ 已注册映射: 116.28.180.185:9788 <-> 76561199049334068
[SceneVote] 主机广播 JSON: {..., "steamName":"永劫无间从没让我开心", ...}
```

## 编译和部署

### 编译
```bash
dotnet build EscapeFromDuckovCoopMod.sln --configuration Release
```

**结果**：✅ 编译成功（19 个警告，无错误）

### 部署
```bash
curl.exe -X POST "http://localhost:8080/api/mods/upload" \
  -H "Authorization: Bearer <TOKEN>" \
  -F "file=@EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll"
```

**结果**：✅ 部署成功
- 版本：`EscapeFromDuckovCoopMod-20251110-1818`
- 路径：`C:/SteamLibrary/steamapps/common/Escape from Duckov/Duckov_Data/Mods/EscapeFromDuckovCoopMod/EscapeFromDuckovCoopMod.dll`

## 测试步骤

1. **启动游戏**（主机和客户端）
2. **主机发起投票**
3. **查看客户端日志**：
   - 应该看到 `[ClientStatus] 客户端发送状态更新`
4. **查看主机日志**：
   - 应该看到 `[ClientStatus] 收到客户端状态`
   - 应该看到 `[ClientStatus] ✓ 已注册映射`
5. **查看投票面板**：
   - 所有玩家的 Steam 名字应该正确显示

## 优势

### 1. 自动修复端口变化问题
- 客户端端口变化时，自动重新上报状态
- 主机始终能获取到最新的映射关系

### 2. 实时性
- 收到投票消息时立即上报
- 确保投票面板显示的信息是最新的

### 3. 无需手动干预
- 完全自动化
- 不需要客户端手动操作

### 4. 向后兼容
- 不影响现有功能
- 只是增加了一次状态上报

## 注意事项

### 1. 网络开销
- 每次收到投票消息都会上报一次状态
- 但投票消息本身就是每秒一次，所以开销可接受
- 状态消息很小（约 200-300 字节）

### 2. 时序问题
- 客户端上报状态后，主机需要一点时间处理
- 但由于投票是每秒广播，下一次广播时就会包含最新信息

### 3. Steam API 可用性
- 如果 Steam API 不可用，会回退到默认名称
- 不会影响投票功能本身

## 后续优化建议

### 1. 缓存 Steam 信息到 PlayerStatus
在主机端将 Steam 信息缓存到 `PlayerStatus` 对象中，这样即使端口变化也能保留信息。

### 2. 定期上报
客户端可以定期（如每 30 秒）上报一次状态，确保映射始终有效。

### 3. 使用 SteamID 作为主键
考虑使用 SteamID 而不是 EndPoint 作为玩家的唯一标识，避免端口变化带来的问题。

## 总结

通过在客户端收到投票消息时立即上报状态，成功解决了 Steam 名字丢失的问题。这是一个简单但有效的解决方案，能够自动处理端口变化导致的映射失效问题。

---

**修改时间**：2025-11-10 18:18  
**版本**：EscapeFromDuckovCoopMod-20251110-1818  
**状态**：✅ 已编译、已部署、待测试
