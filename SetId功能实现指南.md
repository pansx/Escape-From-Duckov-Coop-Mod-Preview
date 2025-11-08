# SetId功能实现指南

## 概述

使用JSON消息让主机告知客户端其真实网络ID，解决客户端为自己创建远程副本的问题。

## 实现步骤

### 1. 添加RealNetworkId字段到PlayerStatus

在PlayerStatus类中添加新字段（如果PlayerStatus是struct或class）：

```csharp
public class PlayerStatus
{
    public string EndPoint;          // 原有：本地ID
    public string RealNetworkId;     // 🔧 新增：主机分配的真实网络ID
    public string PlayerName;
    public int Latency;
    public bool IsInGame;
    public bool LastIsInGame;
    public Vector3 Position;
    public Quaternion Rotation;
    public string SceneId;
    public string CustomFaceJson;
    public List<EquipmentSyncData> EquipmentList;
    public List<WeaponSyncData> WeaponList;
}
```

**位置**：PlayerStatus可能在以下位置定义：
- `EscapeFromDuckovCoopMod/Main/NetService.cs`
- 或单独的 `PlayerStatus.cs` 文件

### 2. 主机端：连接时发送SetId消息

在 `NetService.cs` 的 `OnPeerConnected` 方法中添加：

```csharp
public void OnPeerConnected(NetPeer peer)
{
    Debug.Log(CoopLocalization.Get("net.connectionSuccess", peer.EndPoint.ToString()));
    connectedPeer = peer;

    if (!IsServer)
    {
        status = CoopLocalization.Get("net.connectedTo", peer.EndPoint.ToString());
        isConnecting = false;
        Send_ClientStatus.Instance.SendClientStatusUpdate();
    }
    else
    {
        // 🔧 新增：主机告诉客户端其真实ID
        SetIdMessageHandler.SendSetIdToPeer(peer);
    }

    // ... 其余代码保持不变 ...
}
```

### 3. 客户端：处理JSON消息

需要在处理 `Op.JSON` 的地方添加SetId消息的处理。

**查找位置**：搜索 `case Op.JSON:` 或 `Op.JSON` 的处理代码

**添加处理逻辑**：

```csharp
case Op.JSON:
{
    var json = reader.GetString();
    Debug.Log($"[JSON] 收到消息: {json}");
    
    try
    {
        // 🔧 尝试解析为通用JSON对象以获取type字段
        var jsonObj = JsonUtility.FromJson<JsonMessageBase>(json);
        
        if (jsonObj != null && !string.IsNullOrEmpty(jsonObj.type))
        {
            switch (jsonObj.type)
            {
                case "setId":
                {
                    var setIdMsg = JsonUtility.FromJson<SetIdMessage>(json);
                    SetIdMessageHandler.HandleSetIdMessage(setIdMsg);
                    break;
                }
                
                // 其他type的处理...
                
                default:
                    Debug.LogWarning($"[JSON] 未知的消息类型: {jsonObj.type}");
                    break;
            }
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"[JSON] 处理消息失败: {ex.Message}");
    }
    break;
}
```

**需要添加基础JSON消息类**：

```csharp
[System.Serializable]
public class JsonMessageBase
{
    public string type;
}
```

### 4. 修改IsSelfId方法

在 `NetService.cs` 中修改 `IsSelfId` 方法：

```csharp
public bool IsSelfId(string id)
{
    // 使用SetIdMessageHandler的检查方法
    return SetIdMessageHandler.IsSelfPlayerId(id);
}
```

或者直接在 `IsSelfId` 中实现：

```csharp
public bool IsSelfId(string id)
{
    if (string.IsNullOrEmpty(id)) return false;
    
    var mine = localPlayerStatus?.EndPoint;
    
    // 1. 检查本地ID
    if (!string.IsNullOrEmpty(mine) && id == mine)
        return true;
    
    // 2. 检查真实网络ID
    var realId = localPlayerStatus?.RealNetworkId;
    if (!string.IsNullOrEmpty(realId) && id == realId)
        return true;
    
    // 3. 如果是客户端，检查连接的Peer地址
    if (!IsServer && connectedPeer != null)
    {
        var myNetworkId = connectedPeer.EndPoint?.ToString();
        if (!string.IsNullOrEmpty(myNetworkId) && id == myNetworkId)
            return true;
    }
    
    return false;
}
```

### 5. 在创建远程角色时使用IsSelfId检查

在创建远程角色的代码中（可能在处理 `REMOTE_CREATE` 的地方）：

```csharp
// 处理 REMOTE_CREATE 消息
case Op.REMOTE_CREATE:
{
    var playerId = reader.GetString();
    // ... 读取其他数据 ...
    
    // 🔧 检查是否是自己
    if (NetService.Instance.IsSelfId(playerId))
    {
        Debug.LogWarning($"[REMOTE_CREATE] 阻止为自己创建远程副本: {playerId}");
        return;
    }
    
    // 继续创建远程角色...
    break;
}
```

## 文件清单

需要修改/创建的文件：

1. ✅ **已创建** `EscapeFromDuckovCoopMod/Net/SetIdMessage.cs` - SetId消息和处理器
2. **需要修改** `EscapeFromDuckovCoopMod/Main/NetService.cs`
   - 在 `OnPeerConnected` 中调用 `SetIdMessageHandler.SendSetIdToPeer(peer)`
   - 修改 `IsSelfId` 方法
   - 确认PlayerStatus类中有 `RealNetworkId` 字段
3. **需要修改** 处理 `Op.JSON` 的文件（可能是 `ModBehaviourF.cs` 或 `Mod.cs`）
   - 添加SetId消息的处理
4. **需要修改** 处理 `Op.REMOTE_CREATE` 的文件
   - 添加 `IsSelfId` 检查

## 测试验证

修复后，使用Debug按钮验证：

```json
{
  "LocalPlayer": {
    "EndPoint": "Client:eb2083c3",
    "RealNetworkId": "192.168.137.30:63825"  // 🔧 新增字段
  },
  "CreateRemoteInfo": {
    "HasSelfDuplicate": false,  // ✅ 应该是false
    "MyNetworkId": "192.168.123.1:9050",
    "MyLocalPlayerId": "Client:eb2083c3",
    "MyRealNetworkId": "192.168.137.30:63825",  // 🔧 新增
    "AllRemotePlayerIds": "Host:9050"  // ✅ 应该只有主机
  },
  "ClientRemoteCharacters": {
    "Count": 1,  // ✅ 应该只有1个
    "Data": [
      {
        "PlayerId": "Host:9050",
        "IsLocalPlayerDuplicate": false
      }
    ]
  }
}
```

## 调试日志

成功实施后，应该看到以下日志：

```
[SetId] 已向客户端 192.168.137.30:63825 发送真实ID
[JSON] 发送到 192.168.137.30:63825
[JSON] 收到消息: {"type":"setId","realNetworkId":"192.168.137.30:63825"}
[SetId] 已设置真实网络ID: 192.168.137.30:63825
[SetId] 本地ID: Client:eb2083c3, 真实ID: 192.168.137.30:63825
[REMOTE_CREATE] 阻止为自己创建远程副本: 192.168.137.30:63825
[SetId] IsSelfPlayerId: 匹配真实网络ID - 192.168.137.30:63825
```

## 注意事项

1. **PlayerStatus定义位置**：需要确认PlayerStatus类的定义位置，可能需要搜索整个项目
2. **JSON消息处理位置**：需要找到处理 `Op.JSON` 的代码位置
3. **REMOTE_CREATE处理位置**：需要找到创建远程角色的代码位置
4. **编译验证**：每次修改后都要运行 `dotnet build` 确保编译成功
