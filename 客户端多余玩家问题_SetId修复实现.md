# 客户端多余玩家问题 - SetId修复实现

## 问题回顾

客户端为自己创建了远程副本，原因是**网络地址不一致**导致 `IsSelfId()` 检查失败：

- **客户端本地ID**: `Client:d9805d89`
- **客户端认为的网络地址**: `192.168.123.1:9050`
- **主机看到的客户端地址**: `192.168.137.30:62825` ← 主机用这个创建 REMOTE_CREATE 包

三个ID都不匹配，所以客户端无法识别出这是自己，就为自己创建了远程副本。

## 修复方案

使用JSON消息让主机告知客户端其真实网络ID，客户端接收后更新本地ID。

## 实现细节

### 1. 创建SetId消息数据结构

**文件**: `EscapeFromDuckovCoopMod/Net/SetIdMessage.cs`

```csharp
[System.Serializable]
public class SetIdData
{
    public string type = "setId";  // 消息类型标识
    public string networkId;        // 主机看到的客户端网络ID
    public string timestamp;        // 时间戳（用于调试）
}
```

### 2. 主机发送SetId消息

**文件**: `EscapeFromDuckovCoopMod/Main/NetService.cs`

在客户端连接时（`OnPeerConnected`方法中）：

```csharp
if (!IsServer)
{
    // 客户端逻辑...
}
else
{
    // 🔧 主机：告诉客户端其真实网络ID
    SetIdMessage.SendSetIdToPeer(peer);
}
```

### 3. 创建JSON消息路由器

**文件**: `EscapeFromDuckovCoopMod/Net/JsonMessageRouter.cs`

根据JSON消息的`type`字段分发到不同的处理器：

```csharp
public static void HandleJsonMessage(NetPacketReader reader)
{
    var json = reader.GetString();
    var baseMsg = JsonUtility.FromJson<BaseJsonMessage>(json);
    
    switch (baseMsg.type)
    {
        case "setId":
            HandleSetIdMessage(json);
            break;
        // 其他消息类型...
    }
}
```

### 4. 客户端处理SetId消息

在`JsonMessageRouter.HandleSetIdMessage`中：

```csharp
private static void HandleSetIdMessage(string json)
{
    var data = JsonUtility.FromJson<SetIdMessage.SetIdData>(json);
    var oldId = service.localPlayerStatus?.EndPoint;
    var newId = data.networkId;
    
    // 更新本地玩家状态的EndPoint
    if (service.localPlayerStatus != null)
    {
        service.localPlayerStatus.EndPoint = newId;
        Debug.Log($"[SetId] ✓ 已更新: {oldId} → {newId}");
    }
    
    // 清理自己的远程副本
    CleanupSelfDuplicate(oldId, newId);
}
```

### 5. 修改消息分发

**文件**: `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs`

在`OnNetworkReceive`方法中：

```csharp
case Op.JSON:
    // 处理JSON消息 - 使用路由器根据type字段分发
    JsonMessageRouter.HandleJsonMessage(reader);
    break;
```

### 6. 优化IsSelfId检查

**文件**: `EscapeFromDuckovCoopMod/Main/NetService.cs`

```csharp
public bool IsSelfId(string id)
{
    if (string.IsNullOrEmpty(id)) return false;
    
    var mine = localPlayerStatus?.EndPoint;
    
    // 1. 检查本地ID（SetId消息会更新这个值为主机告知的真实网络ID）
    if (!string.IsNullOrEmpty(mine) && id == mine)
    {
        Debug.Log($"[IsSelfId] ✓ 匹配本地ID: {id}");
        return true;
    }
    
    // 2. 如果是客户端，检查连接的Peer地址（兜底检查）
    if (!IsServer && connectedPeer != null)
    {
        var myNetworkId = connectedPeer.EndPoint?.ToString();
        if (!string.IsNullOrEmpty(myNetworkId) && id == myNetworkId)
        {
            Debug.Log($"[IsSelfId] ✓ 匹配连接Peer地址: {id}");
            return true;
        }
    }
    
    return false;
}
```

## 工作流程

1. **客户端连接到主机**
   - 客户端本地ID: `Client:xxxxx`
   - 主机看到的客户端地址: `192.168.137.30:62825`

2. **主机发送SetId消息**
   ```json
   {
       "type": "setId",
       "networkId": "192.168.137.30:62825",
       "timestamp": "2025-11-08 11:30:00.123"
   }
   ```

3. **客户端接收并更新本地ID**
   - `localPlayerStatus.EndPoint` 从 `Client:xxxxx` 更新为 `192.168.137.30:62825`

4. **主机广播REMOTE_CREATE**
   - 主机告诉所有客户端创建远程玩家
   - PlayerId: `192.168.137.30:62825`

5. **客户端检查IsSelfId**
   - `IsSelfId("192.168.137.30:62825")` 
   - 检查 `localPlayerStatus.EndPoint` = `192.168.137.30:62825`
   - 匹配！返回 `true`
   - **跳过创建远程副本** ✅

6. **清理已存在的副本**
   - 如果在SetId之前已经创建了副本，会被自动清理

## 优势

1. **不修改Op枚举**: 使用现有的`Op.JSON`，通过type字段区分
2. **不修改游戏变量**: 只修改mod内部的`localPlayerStatus.EndPoint`
3. **向后兼容**: 不影响现有的JSON消息（如测试消息）
4. **可扩展**: JsonMessageRouter可以轻松添加新的消息类型
5. **自动清理**: 如果已经创建了副本，会自动检测并删除

## 测试验证

修复后，客户端应该：
- `ClientRemoteCharacters: 1` (只有主机)
- `ClientPlayerStatuses: 1` (只有主机)
- `AllCharactersInScene: 2` (本地玩家 + 主机)
- 游戏中只看到 2 个角色（自己 + 主机）

## 日志输出

成功的日志应该包含：

```
[SetId] 发送SetId给客户端: 192.168.137.30:62825
[SetId] 收到主机告知的网络ID: 192.168.137.30:62825
[SetId] 旧ID: Client:d9805d89
[SetId] ✓ 已更新 localPlayerStatus.EndPoint: Client:d9805d89 → 192.168.137.30:62825
[IsSelfId] ✓ 匹配本地ID: 192.168.137.30:62825
```

如果有副本被清理：

```
[SetId] 发现自己的远程副本，准备删除: 192.168.137.30:62825
[SetId] ✓ 已删除远程副本GameObject: 192.168.137.30:62825
[SetId] ✓ 已从clientRemoteCharacters移除: 192.168.137.30:62825
[SetId] ✓ 清理完成，共删除 1 个自己的远程副本
```

## 编译状态

✅ 编译成功，无错误
⚠️ 只有警告（nullable引用、未使用字段等，不影响功能）
