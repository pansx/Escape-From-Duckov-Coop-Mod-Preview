# SetId 功能 - 当前实现状态

## 已完成 ✅

### 1. 核心代码实现

-   ✅ 创建了 `SetIdMessage.cs` - SetId 消息和处理器
-   ✅ 修改了 `PlayerStatus` 类 - 添加 `RealNetworkId` 字段
-   ✅ 修改了 `NetService.cs` - 主机发送 SetId，改进 IsSelfId 检查
-   ✅ 编译成功 - 无错误

### 2. 工作原理

**主机端**：

```csharp
// 在 OnPeerConnected 中
SetIdMessageHandler.SendSetIdToPeer(peer);
// 发送: {"type":"setId","realNetworkId":"192.168.137.30:63825"}
```

**客户端**：

```csharp
// 接收JSON消息后
SetIdMessageHandler.HandleSetIdMessage(message);
// 保存: localPlayerStatus.RealNetworkId = "192.168.137.30:63825"
```

**ID 检查**：

```csharp
// IsSelfId 现在会检查3个ID
1. EndPoint (本地ID)
2. RealNetworkId (主机分配的)  ← 新增
3. connectedPeer.EndPoint (连接地址)
```

## 还需要完成 ⚠️

### 关键：添加 JSON 消息处理

**问题**：客户端还不能处理 SetId 消息，因为 JSON 消息处理代码还没有添加 SetId 的分支。

**需要做的**：

1. **查找 JSON 消息处理位置**

    - 搜索 `case Op.JSON:` 或处理 `Op.JSON` 的代码
    - 可能在 `ModBehaviourF.cs` 或 `Mod.cs` 的 `OnNetworkReceive` 方法中

2. **添加 SetId 消息处理**

```csharp
case Op.JSON:
{
    var json = reader.GetString();
    Debug.Log($"[JSON] 收到消息: {json}");

    try
    {
        // 先尝试解析type字段
        var jsonObj = JsonUtility.FromJson<JsonMessageBase>(json);

        if (jsonObj != null && !string.IsNullOrEmpty(jsonObj.type))
        {
            switch (jsonObj.type)
            {
                case "setId":  // ← 新增
                {
                    var setIdMsg = JsonUtility.FromJson<SetIdMessage>(json);
                    SetIdMessageHandler.HandleSetIdMessage(setIdMsg);
                    break;
                }

                // 其他type的处理...
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

3. **添加基础 JSON 消息类**（如果还没有）

```csharp
[System.Serializable]
public class JsonMessageBase
{
    public string type;
}
```

## 测试计划

### 测试步骤

1. **添加 JSON 消息处理代码**（上面的代码）
2. **重新编译** - `dotnet build`
3. **启动游戏** - 2 人联机（1 主机 + 1 客户端）
4. **查看日志** - 应该看到：

**主机端日志**：

```
[SetId] 已向客户端 192.168.137.30:63825 发送真实ID
[JSON] 发送到 192.168.137.30:63825
```

**客户端日志**：

```
[JSON] 收到消息: {"type":"setId","realNetworkId":"192.168.137.30:63825"}
[SetId] 已设置真实网络ID: 192.168.137.30:63825
[SetId] 本地ID: Client:xxx, 真实ID: 192.168.137.30:63825
```

5. **点击 Debug 按钮** - 验证：

```json
{
    "LocalPlayer": {
        "RealNetworkId": "192.168.137.30:63825" // ← 应该有值
    },
    "ClientRemoteCharacters": {
        "Count": 1 // ← 应该只有1个（主机）
    },
    "CreateRemoteInfo": {
        "HasSelfDuplicate": false // ← 应该是false
    }
}
```

## 当前日志状态

从日志服务器查询结果：

-   ❌ 主机端：没有 SetId 相关日志
-   ❌ 客户端：没有 SetId 相关日志

**原因**：还没有测试新编译的代码，或者 JSON 消息处理还没有添加。

## 下一步行动

### 选项 1：查找并修改 JSON 消息处理代码

1. 搜索 `Op.JSON` 的处理位置
2. 添加 SetId 消息的处理分支
3. 重新编译和测试

### 选项 2：先测试主机是否发送了 SetId 消息

1. 启动游戏，2 人联机
2. 查看日志，确认主机是否发送了 SetId 消息
3. 如果发送了但客户端没有处理，说明需要添加处理代码
4. 如果没有发送，说明 `OnPeerConnected` 没有被调用或有其他问题

## 预期效果

修复完成后：

-   ✅ 客户端只看到 1 个远程角色（主机）
-   ✅ 不再为自己创建远程副本
-   ✅ `HasSelfDuplicate` = false
-   ✅ `ClientRemoteCharacters.Count` = 1

## 相关文件

-   ✅ `EscapeFromDuckovCoopMod/Net/SetIdMessage.cs`
-   ✅ `EscapeFromDuckovCoopMod/Main/LocalPlayer/LocalPlayerManager.cs`
-   ✅ `EscapeFromDuckovCoopMod/Main/NetService.cs`
-   ⚠️ JSON 消息处理文件（需要查找和修改）
