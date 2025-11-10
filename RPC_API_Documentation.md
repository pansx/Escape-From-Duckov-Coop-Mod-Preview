# Escape From Duckov Coop Mod - RPC API 文档

## 概述

本文档详细说明了 Escape From Duckov Coop Mod 中混合P2P RPC系统的使用方法和API接口。

## 目录

- [系统架构](#系统架构)
- [快速开始](#快速开始)
- [API 参考](#api-参考)
- [已注册的RPC方法](#已注册的rpc方法)
- [使用示例](#使用示例)
- [最佳实践](#最佳实践)
- [故障排除](#故障排除)

## 系统架构

### 核心组件

- **HybridRPCManager**: RPC系统的核心管理器
- **SteamNetworkingTransport**: Steam P2P传输层
- **LiteNetLib**: LAN网络传输层

### 传输模式

```csharp
public enum TransportMode
{
    Auto,    // 自动选择（优先Steam）
    Steam,   // 强制使用Steam P2P
    LAN      // 强制使用LAN
}
```

### RPC目标类型

```csharp
public enum RPCTarget : byte
{
    Server = 0,                    // 发送给服务器
    AllClients = 1,               // 发送给所有客户端
    TargetClient = 2,             // 发送给指定客户端
    AllClientsExceptSender = 3    // 发送给除发送者外的所有客户端
}
```

### 传递方式

```csharp
public enum DeliveryMethod
{
    Unreliable,           // 不可靠，快速
    ReliableUnordered,    // 可靠，无序
    ReliableOrdered,      // 可靠，有序（推荐）
    ReliableSequenced     // 可靠，序列化
}
```

## 快速开始

### 1. 获取RPC管理器实例

```csharp
var rpcManager = HybridRPCManager.Instance;
if (rpcManager == null)
{
    Debug.LogError("RPC Manager not available");
    return;
}
```

### 2. 注册RPC处理器

```csharp
private void RegisterRPCs()
{
    var rpcManager = HybridRPCManager.Instance;
    if (rpcManager == null) return;

    // 注册RPC方法
    rpcManager.RegisterRPC("MyRPCMethod", HandleMyRPC);
    
    Debug.Log("RPC registered successfully");
}

// RPC处理器
private void HandleMyRPC(long senderConnectionId, NetDataReader reader)
{
    // 读取参数
    string message = reader.GetString();
    int value = reader.GetInt();
    
    // 处理逻辑
    Debug.Log($"Received RPC from {senderConnectionId}: {message}, {value}");
}
```

### 3. 调用RPC方法

```csharp
public void CallMyRPC(string message, int value)
{
    var rpcManager = HybridRPCManager.Instance;
    if (rpcManager == null) return;

    rpcManager.CallRPC("MyRPCMethod", RPCTarget.Server, 0, (writer) =>
    {
        writer.Put(message);
        writer.Put(value);
    });
}
```

## API 参考

### HybridRPCManager 类

#### 属性

| 属性 | 类型 | 描述 |
|------|------|------|
| `Instance` | `HybridRPCManager` | 单例实例 |
| `Mode` | `TransportMode` | 当前传输模式 |
| `IsServer` | `bool` | 是否为服务器 |
| `IsClient` | `bool` | 是否为客户端 |

#### 方法

##### RegisterRPC

注册RPC处理器

```csharp
public ushort RegisterRPC(string rpcName, RPCHandler handler)
```

**参数:**
- `rpcName`: RPC方法名称（字符串）
- `handler`: 处理器委托

**返回值:**
- `ushort`: RPC ID

**示例:**
```csharp
ushort rpcId = rpcManager.RegisterRPC("PlayerMove", HandlePlayerMove);
```

##### UnregisterRPC

注销RPC处理器

```csharp
public void UnregisterRPC(string rpcName)
```

**参数:**
- `rpcName`: 要注销的RPC方法名称

##### CallRPC

调用RPC方法

```csharp
public void CallRPC(string rpcName, RPCTarget target, long targetConnectionId, 
                   Action<NetDataWriter> writeData, 
                   DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
```

**参数:**
- `rpcName`: RPC方法名称
- `target`: 目标类型
- `targetConnectionId`: 目标连接ID（仅在TargetClient时使用）
- `writeData`: 数据写入委托
- `deliveryMethod`: 传递方式（可选，默认为ReliableOrdered）

### NetDataWriter 数据写入

#### 基本类型

```csharp
writer.Put(bool value);           // 布尔值
writer.Put(byte value);           // 字节
writer.Put(int value);            // 32位整数
writer.Put(long value);           // 64位整数
writer.Put(float value);          // 单精度浮点数
writer.Put(double value);         // 双精度浮点数
writer.Put(string value);         // 字符串
writer.Put(ulong value);          // 无符号64位整数
```

#### 复合类型

```csharp
// Vector3
writer.Put(vector.x);
writer.Put(vector.y);
writer.Put(vector.z);

// 数组
writer.Put(array.Length);
foreach (var item in array)
{
    writer.Put(item);
}

// 字节数组
writer.Put(byteArray, length);
```

### NetDataReader 数据读取

#### 基本类型

```csharp
bool boolValue = reader.GetBool();
byte byteValue = reader.GetByte();
int intValue = reader.GetInt();
long longValue = reader.GetLong();
float floatValue = reader.GetFloat();
double doubleValue = reader.GetDouble();
string stringValue = reader.GetString();
ulong ulongValue = reader.GetULong();
```

#### 复合类型

```csharp
// Vector3
Vector3 vector = new Vector3(
    reader.GetFloat(),
    reader.GetFloat(),
    reader.GetFloat()
);

// 数组
int length = reader.GetInt();
var array = new int[length];
for (int i = 0; i < length; i++)
{
    array[i] = reader.GetInt();
}

// 字节数组
byte[] byteArray = new byte[length];
reader.GetBytes(byteArray, length);
```

## 已注册的RPC方法

### 投票系统 RPC

#### VoteStart
**方向:** Server → Client  
**描述:** 服务器通知客户端开始投票

**参数:**
```csharp
writer.Put(targetSceneId);        // string: 目标场景ID
writer.Put(curtainGuid);          // string: 幕布GUID
writer.Put(notifyEvac);           // bool: 是否通知撤离
writer.Put(saveToFile);           // bool: 是否保存到文件
writer.Put(useLocation);          // bool: 是否使用位置
writer.Put(locationName);         // string: 位置名称
writer.Put(hostSceneId);          // string: 主机场景ID
writer.Put(participantCount);     // int: 参与者数量
// 循环写入参与者Steam ID
foreach (var steamId in participants)
    writer.Put(steamId);          // ulong: Steam ID
```

#### VoteRequest
**方向:** Client → Server  
**描述:** 客户端请求开始投票

**参数:**
```csharp
writer.Put(targetId);             // string: 目标场景ID
writer.Put(curtainGuid);          // string: 幕布GUID
writer.Put(notifyEvac);           // bool: 是否通知撤离
writer.Put(saveToFile);           // bool: 是否保存到文件
writer.Put(useLocation);          // bool: 是否使用位置
writer.Put(locationName);         // string: 位置名称
```

#### VoteCast
**方向:** Client → Server  
**描述:** 客户端投票

**参数:**
```csharp
writer.Put(ready);                // bool: 是否准备就绪
```

#### VoteReadySet
**方向:** Server → Client  
**描述:** 服务器广播玩家准备状态

**参数:**
```csharp
writer.Put(playerId);             // string: 玩家ID
writer.Put(ready);                // bool: 是否准备就绪
```

#### VoteBeginLoad
**方向:** Server → Client  
**描述:** 服务器通知开始加载场景

**参数:**
```csharp
writer.Put(targetSceneId);        // string: 目标场景ID
writer.Put(curtainGuid);          // string: 幕布GUID
writer.Put(notifyEvac);           // bool: 是否通知撤离
writer.Put(saveToFile);           // bool: 是否保存到文件
writer.Put(useLocation);          // bool: 是否使用位置
writer.Put(locationName);         // string: 位置名称
```

#### VoteCancel
**方向:** Server → Client  
**描述:** 服务器取消投票

**参数:** 无

### 示例RPC方法

#### TestRPC
**描述:** 测试用RPC方法

**参数:**
```csharp
writer.Put(message);              // string: 测试消息
writer.Put(value);                // int: 测试数值
```

#### SyncVoteData
**描述:** 同步投票数据

**参数:**
```csharp
writer.Put(voteId);               // string: 投票ID
writer.Put(voteCount);            // int: 投票数量
writer.Put(isReady);              // bool: 是否准备就绪
```

#### RequestPlayerData
**描述:** 请求玩家数据

**参数:**
```csharp
writer.Put(playerId);             // ulong: 玩家ID
```

## 使用示例

### 示例1: 简单的聊天系统

```csharp
public class ChatRPC : MonoBehaviour
{
    private void Start()
    {
        RegisterChatRPCs();
    }
    
    private void RegisterChatRPCs()
    {
        var rpcManager = HybridRPCManager.Instance;
        if (rpcManager == null) return;
        
        rpcManager.RegisterRPC("SendChatMessage", OnReceiveChatMessage);
        rpcManager.RegisterRPC("BroadcastChatMessage", OnBroadcastChatMessage);
    }
    
    // 发送聊天消息（客户端调用）
    public void SendChatMessage(string playerName, string message)
    {
        var rpcManager = HybridRPCManager.Instance;
        if (rpcManager == null || rpcManager.IsServer) return;
        
        rpcManager.CallRPC("SendChatMessage", RPCTarget.Server, 0, (writer) =>
        {
            writer.Put(playerName);
            writer.Put(message);
            writer.Put(Time.time); // 时间戳
        });
    }
    
    // 服务器接收并广播消息
    private void OnReceiveChatMessage(long senderConnectionId, NetDataReader reader)
    {
        if (!NetService.Instance.IsServer) return;
        
        string playerName = reader.GetString();
        string message = reader.GetString();
        float timestamp = reader.GetFloat();
        
        // 验证消息（可选）
        if (string.IsNullOrEmpty(message) || message.Length > 200)
            return;
        
        // 广播给所有客户端
        var rpcManager = HybridRPCManager.Instance;
        rpcManager.CallRPC("BroadcastChatMessage", RPCTarget.AllClients, 0, (writer) =>
        {
            writer.Put(playerName);
            writer.Put(message);
            writer.Put(timestamp);
        });
    }
    
    // 客户端接收广播消息
    private void OnBroadcastChatMessage(long senderConnectionId, NetDataReader reader)
    {
        string playerName = reader.GetString();
        string message = reader.GetString();
        float timestamp = reader.GetFloat();
        
        // 显示聊天消息
        Debug.Log($"[Chat] {playerName}: {message}");
        // 这里可以更新UI显示聊天消息
    }
}
```

### 示例2: 玩家位置同步

```csharp
public class PlayerSyncRPC : MonoBehaviour
{
    private void Start()
    {
        RegisterSyncRPCs();
    }
    
    private void RegisterSyncRPCs()
    {
        var rpcManager = HybridRPCManager.Instance;
        if (rpcManager == null) return;
        
        rpcManager.RegisterRPC("SyncPlayerPosition", OnSyncPlayerPosition);
    }
    
    // 同步玩家位置（频繁调用，使用Unreliable）
    public void SyncPlayerPosition(Vector3 position, Vector3 rotation, float speed)
    {
        var rpcManager = HybridRPCManager.Instance;
        if (rpcManager == null || !rpcManager.IsClient) return;
        
        rpcManager.CallRPC("SyncPlayerPosition", RPCTarget.Server, 0, (writer) =>
        {
            // 位置
            writer.Put(position.x);
            writer.Put(position.y);
            writer.Put(position.z);
            
            // 旋转
            writer.Put(rotation.x);
            writer.Put(rotation.y);
            writer.Put(rotation.z);
            
            // 速度
            writer.Put(speed);
            
            // 时间戳
            writer.Put(Time.time);
            
        }, DeliveryMethod.Unreliable); // 使用不可靠传输提高性能
    }
    
    private void OnSyncPlayerPosition(long senderConnectionId, NetDataReader reader)
    {
        Vector3 position = new Vector3(
            reader.GetFloat(),
            reader.GetFloat(),
            reader.GetFloat()
        );
        
        Vector3 rotation = new Vector3(
            reader.GetFloat(),
            reader.GetFloat(),
            reader.GetFloat()
        );
        
        float speed = reader.GetFloat();
        float timestamp = reader.GetFloat();
        
        // 更新远程玩家位置
        UpdateRemotePlayer(senderConnectionId, position, rotation, speed, timestamp);
    }
    
    private void UpdateRemotePlayer(long playerId, Vector3 pos, Vector3 rot, float speed, float timestamp)
    {
        // 实现玩家位置更新逻辑
        Debug.Log($"Player {playerId} moved to {pos} at {timestamp}");
    }
}
```

### 示例3: 游戏事件系统

```csharp
public class GameEventRPC : MonoBehaviour
{
    public enum GameEventType : byte
    {
        PlayerJoined = 0,
        PlayerLeft = 1,
        ItemPickup = 2,
        EnemyKilled = 3,
        LevelComplete = 4
    }
    
    private void Start()
    {
        RegisterEventRPCs();
    }
    
    private void RegisterEventRPCs()
    {
        var rpcManager = HybridRPCManager.Instance;
        if (rpcManager == null) return;
        
        rpcManager.RegisterRPC("GameEvent", OnGameEvent);
    }
    
    // 触发游戏事件
    public void TriggerGameEvent(GameEventType eventType, params object[] parameters)
    {
        var rpcManager = HybridRPCManager.Instance;
        if (rpcManager == null) return;
        
        RPCTarget target = rpcManager.IsServer ? RPCTarget.AllClients : RPCTarget.Server;
        
        rpcManager.CallRPC("GameEvent", target, 0, (writer) =>
        {
            writer.Put((byte)eventType);
            writer.Put(parameters.Length);
            
            foreach (var param in parameters)
            {
                WriteParameter(writer, param);
            }
        });
    }
    
    private void WriteParameter(NetDataWriter writer, object param)
    {
        switch (param)
        {
            case string str:
                writer.Put((byte)0); // 类型标识
                writer.Put(str);
                break;
            case int i:
                writer.Put((byte)1);
                writer.Put(i);
                break;
            case float f:
                writer.Put((byte)2);
                writer.Put(f);
                break;
            case bool b:
                writer.Put((byte)3);
                writer.Put(b);
                break;
            default:
                writer.Put((byte)255); // 未知类型
                break;
        }
    }
    
    private void OnGameEvent(long senderConnectionId, NetDataReader reader)
    {
        GameEventType eventType = (GameEventType)reader.GetByte();
        int paramCount = reader.GetInt();
        
        var parameters = new object[paramCount];
        for (int i = 0; i < paramCount; i++)
        {
            parameters[i] = ReadParameter(reader);
        }
        
        HandleGameEvent(eventType, parameters);
    }
    
    private object ReadParameter(NetDataReader reader)
    {
        byte type = reader.GetByte();
        return type switch
        {
            0 => reader.GetString(),
            1 => reader.GetInt(),
            2 => reader.GetFloat(),
            3 => reader.GetBool(),
            _ => null
        };
    }
    
    private void HandleGameEvent(GameEventType eventType, object[] parameters)
    {
        switch (eventType)
        {
            case GameEventType.PlayerJoined:
                Debug.Log($"Player {parameters[0]} joined the game");
                break;
            case GameEventType.ItemPickup:
                Debug.Log($"Player {parameters[0]} picked up {parameters[1]}");
                break;
            // 处理其他事件...
        }
    }
}
```

## 最佳实践

### 1. 错误处理

```csharp
public void SafeCallRPC(string rpcName, RPCTarget target, Action<NetDataWriter> writeData)
{
    try
    {
        var rpcManager = HybridRPCManager.Instance;
        if (rpcManager == null)
        {
            Debug.LogWarning("RPC Manager not available");
            return;
        }
        
        rpcManager.CallRPC(rpcName, target, 0, writeData);
    }
    catch (Exception ex)
    {
        Debug.LogError($"Failed to call RPC {rpcName}: {ex.Message}");
    }
}
```

### 2. 参数验证

```csharp
private void OnPlayerMoveRPC(long senderConnectionId, NetDataReader reader)
{
    try
    {
        Vector3 position = new Vector3(
            reader.GetFloat(),
            reader.GetFloat(),
            reader.GetFloat()
        );
        
        // 验证位置合理性
        if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
        {
            Debug.LogWarning($"Invalid position from {senderConnectionId}");
            return;
        }
        
        // 验证位置范围
        if (position.magnitude > 10000f)
        {
            Debug.LogWarning($"Position too far from {senderConnectionId}");
            return;
        }
        
        // 处理有效的位置数据
        UpdatePlayerPosition(senderConnectionId, position);
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error processing player move RPC: {ex.Message}");
    }
}
```

### 3. 性能优化

```csharp
public class OptimizedRPC : MonoBehaviour
{
    private float lastSyncTime;
    private const float SYNC_INTERVAL = 0.1f; // 100ms间隔
    
    private void Update()
    {
        // 限制同步频率
        if (Time.time - lastSyncTime >= SYNC_INTERVAL)
        {
            SyncPlayerData();
            lastSyncTime = Time.time;
        }
    }
    
    private void SyncPlayerData()
    {
        // 只在数据发生变化时同步
        if (HasPlayerDataChanged())
        {
            CallPlayerSyncRPC();
        }
    }
}
```

### 4. 版本兼容性

```csharp
private void RegisterVersionedRPC()
{
    var rpcManager = HybridRPCManager.Instance;
    if (rpcManager == null) return;
    
    rpcManager.RegisterRPC("PlayerData_v2", OnPlayerDataV2);
    rpcManager.RegisterRPC("PlayerData_v1", OnPlayerDataV1); // 向后兼容
}

private void OnPlayerDataV2(long senderConnectionId, NetDataReader reader)
{
    // 新版本处理逻辑
    byte version = reader.GetByte();
    if (version != 2)
    {
        Debug.LogWarning($"Unsupported version {version} from {senderConnectionId}");
        return;
    }
    
    // 处理v2数据...
}
```

## 故障排除

### 常见问题

#### 1. RPC Manager 为 null
**原因:** HybridRPCManager 未正确初始化  
**解决方案:**
```csharp
// 确保在调用前检查
var rpcManager = HybridRPCManager.Instance;
if (rpcManager == null)
{
    Debug.LogError("RPC Manager not initialized");
    return;
}
```

#### 2. RPC 未注册
**原因:** 忘记注册RPC处理器  
**解决方案:**
```csharp
// 在Start()或Awake()中注册
private void Start()
{
    RegisterAllRPCs();
}
```

#### 3. 数据读取错误
**原因:** 读取顺序与写入顺序不匹配  
**解决方案:**
```csharp
// 写入顺序
writer.Put(playerName);  // 1. string
writer.Put(level);       // 2. int
writer.Put(health);      // 3. float

// 读取顺序必须一致
string playerName = reader.GetString();  // 1. string
int level = reader.GetInt();             // 2. int
float health = reader.GetFloat();        // 3. float
```

#### 4. 网络连接问题
**原因:** Steam或LAN连接不稳定  
**解决方案:**
```csharp
// 检查连接状态
if (!rpcManager.IsServer && !rpcManager.IsClient)
{
    Debug.LogWarning("Not connected to network");
    return;
}
```

### 调试技巧

#### 1. 启用详细日志
```csharp
// 在HybridRPCManager中添加调试日志
Debug.Log($"[RPC] Calling {rpcName} to {target}");
Debug.Log($"[RPC] Received {rpcName} from {senderConnectionId}");
```

#### 2. 监控RPC调用
```csharp
public class RPCMonitor : MonoBehaviour
{
    private Dictionary<string, int> rpcCallCounts = new Dictionary<string, int>();
    
    public void LogRPCCall(string rpcName)
    {
        if (!rpcCallCounts.ContainsKey(rpcName))
            rpcCallCounts[rpcName] = 0;
        
        rpcCallCounts[rpcName]++;
        
        if (rpcCallCounts[rpcName] % 100 == 0)
        {
            Debug.Log($"RPC {rpcName} called {rpcCallCounts[rpcName]} times");
        }
    }
}
```

#### 3. 网络延迟测试
```csharp
public void TestNetworkLatency()
{
    var rpcManager = HybridRPCManager.Instance;
    if (rpcManager == null) return;
    
    float sendTime = Time.realtimeSinceStartup;
    
    rpcManager.CallRPC("PingTest", RPCTarget.Server, 0, (writer) =>
    {
        writer.Put(sendTime);
    });
}

private void OnPingTest(long senderConnectionId, NetDataReader reader)
{
    float sendTime = reader.GetFloat();
    float latency = Time.realtimeSinceStartup - sendTime;
    
    Debug.Log($"Network latency: {latency * 1000:F2}ms");
}
```

## 更新日志

### v1.0.0 (2025-01-01)
- 初始版本发布
- 支持Steam P2P和LAN混合传输
- 基础RPC功能实现
- 投票系统RPC集成

---

**注意:** 本文档会随着系统更新而持续维护，请定期查看最新版本。

**联系方式:** 如有问题或建议，请在项目GitHub页面提交Issue。