# 设计文档 - 进图全量同步

## 概述

本文档描述进图全量同步系统的详细设计。该系统通过JSON消息机制，在玩家进入场景后执行两阶段同步：第一阶段清空客户端的AI和掉落物，第二阶段从主机全量同步所有实体数据。这确保了所有客户端的游戏状态完全一致。

## 架构

### 系统组件

```
┌─────────────────────────────────────────────────────────┐
│              全量同步系统架构                              │
└─────────────────────────────────────────────────────────┘

主机端 (Host)                          客户端 (Client)
     │                                      │
     ├─ FullSyncManager                    ├─ FullSyncManager
     │  ├─ 场景加载检测                     │  ├─ 场景加载检测
     │  ├─ 数据收集器                       │  ├─ 清空处理器
     │  ├─ 数据序列化                       │  ├─ 数据应用器
     │  └─ 同步状态管理                     │  └─ 同步状态管理
     │                                      │
     ├─ EntityCollector                    ├─ EntityCleaner
     │  ├─ AI数据收集                       │  ├─ AI清空
     │  └─ 掉落物数据收集                   │  └─ 掉落物清空
     │                                      │
     ├─ SyncDataSerializer                 ├─ EntitySpawner
     │  ├─ JSON序列化                       │  ├─ AI生成
     │  ├─ 数据压缩                         │  └─ 掉落物生成
     │  └─ 分包处理                         │
     │                                      │
     └─ SyncValidator                      └─ SyncValidator
        └─ 同步结果验证                        └─ 同步结果验证
```

### 数据流

```
┌─────────────────────────────────────────────────────────┐
│                  全量同步数据流                           │
└─────────────────────────────────────────────────────────┘

客户端                                主机
   │                                   │
   │  1. 场景加载完成                   │
   │  ─> SCENE_GATE_READY              │
   │  ══════════════════════════════>  │
   │  （复用现有机制）                  │
   │                                   │
   │                                   │  2. 等待所有客户端就绪
   │                                   │  ─> 记录客户端状态
   │                                   │
   │                                   │  3. 主机场景加载完成
   │                                   │  ─> 所有AI和掉落物已生成
   │                                   │  ─> 发送SCENE_GATE_RELEASE
   │                                   │
   │  4. 接收放行指令                   │
   │  <══════════════════════════════  │
   │  ─> SCENE_GATE_RELEASE            │
   │                                   │
   │  5. 拦截淡入流程                   │
   │  ─> 暂停进入关卡                   │
   │  ─> 发送同步请求                   │
   │  ─> JSON: {type:"sync_request"}   │
   │  ══════════════════════════════>  │
   │                                   │
   │                                   │  6. 收集实体数据
   │                                   │  ─> 遍历所有AI
   │                                   │  ─> 遍历所有掉落物
   │                                   │  ─> 序列化为JSON
   │                                   │
   │  7. 接收清空指令                   │  7. 发送清空指令
   │  <══════════════════════════════  │  ─> JSON: {type:"clear_entities"}
   │                                   │
   │  8. 清空AI和掉落物                 │
   │  ─> 销毁所有AI实体                 │
   │  ─> 销毁所有掉落物                 │
   │  ─> 保留玩家角色                   │
   │                                   │
   │  9. 发送清空确认                   │
   │  ─> JSON: {type:"clear_complete"} │
   │  ══════════════════════════════>  │
   │                                   │
   │  10. 接收同步数据                  │  10. 发送同步数据
   │  <══════════════════════════════  │  ─> JSON: {type:"full_sync"}
   │  ─> JSON数组（可能分包）           │  ─> ReliableOrdered
   │                                   │
   │  11. 应用同步数据                  │
   │  ─> 解析JSON                       │
   │  ─> 批量生成AI                     │
   │  ─> 批量生成掉落物                 │
   │  ─> 应用状态数据                   │
   │                                   │
   │  12. 发送同步完成                  │
   │  ─> JSON: {type:"sync_complete"}  │
   │  ══════════════════════════════>  │
   │                                   │
   │                                   │  13. 验证同步结果
   │                                   │  ─> 对比实体数量
   │                                   │  ─> 记录日志
   │                                   │
   │  14. 继续淡入流程                  │
   │  ─> 解除AI冻结                     │
   │  ─> 执行淡入动画                   │
   │  ─> 进入游戏                       │
```

## 组件设计

### 1. FullSyncManager

**职责**：全量同步的核心管理器，协调整个同步流程

**关键字段**：
```csharp
// 同步状态跟踪
private readonly Dictionary<string, SyncState> _clientSyncStates = new();

// 同步超时时间（秒）
private const float SYNC_TIMEOUT = 30f;

// 清空超时时间（秒）
private const float CLEAR_TIMEOUT = 5f;

// 批量生成大小
private const int SPAWN_BATCH_SIZE = 50;

// 同步状态枚举
public enum SyncState
{
    Idle,           // 空闲
    WaitingClear,   // 等待清空
    Clearing,       // 清空中
    WaitingSync,    // 等待同步
    Syncing,        // 同步中
    Complete,       // 完成
    Failed          // 失败
}
```

**主要方法**：

#### Client_OnSceneGateRelease
```csharp
/// <summary>
/// 客户端：接收到SCENE_GATE_RELEASE后，拦截淡入流程
/// </summary>
public void Client_OnSceneGateRelease()
{
    Debug.Log("[FullSync] 收到SCENE_GATE_RELEASE，拦截淡入流程，请求全量同步");
    
    // 标记正在同步，阻止淡入
    _isSyncing = true;
    
    // 发送同步请求
    var request = new SyncRequestMessage
    {
        timestamp = Time.time
    };
    
    JsonMessage.SendToHost(request);
}

/// <summary>
/// 客户端：同步完成后继续淡入流程
/// </summary>
private void Client_OnSyncComplete()
{
    Debug.Log("[FullSync] 同步完成，继续淡入流程");
    
    // 解除同步标记
    _isSyncing = false;
    
    // 解除AI冻结
    COOPManager.AIHandle.freezeAI = false;
    
    // 继续执行原有的淡入逻辑
    ContinueFadeInProcess();
}
```

#### Host_SendClearRequest
```csharp
/// <summary>
/// 主机端：发送清空指令给客户端
/// </summary>
private void Host_SendClearRequest(string clientId)
{
    var clearData = new ClearEntitiesMessage
    {
        timestamp = Time.time
    };
    
    // 发送JSON消息
    JsonMessage.SendToPeer(GetPeerByClientId(clientId), clearData);
    
    // 更新状态
    _clientSyncStates[clientId] = SyncState.Clearing;
    
    // 启动超时检测
    StartCoroutine(CheckClearTimeout(clientId));
}
```

#### Client_OnJsonMessage
```csharp
/// <summary>
/// 客户端：接收JSON消息并根据type分发
/// </summary>
private void Client_OnJsonMessage(string json)
{
    // 先解析type字段
    var baseMsg = JsonUtility.FromJson<BaseMessage>(json);
    
    switch (baseMsg.type)
    {
        case "clear_entities":
            var clearMsg = JsonUtility.FromJson<ClearEntitiesMessage>(json);
            Client_OnClearRequest(clearMsg);
            break;
            
        case "full_sync":
            var syncMsg = JsonUtility.FromJson<FullSyncMessage>(json);
            Client_OnFullSyncData(syncMsg);
            break;
            
        default:
            Debug.LogWarning($"[FullSync] 未知的消息类型: {baseMsg.type}");
            break;
    }
}

/// <summary>
/// 客户端：接收到清空指令
/// </summary>
private void Client_OnClearRequest(ClearEntitiesMessage message)
{
    Debug.Log("[FullSync] 收到清空指令，开始清空实体");
    
    // 执行清空
    var result = EntityCleaner.ClearAllEntities();
    
    // 发送确认
    var response = new ClearCompleteMessage
    {
        success = result.success,
        aiCount = result.aiCleared,
        lootCount = result.lootCleared,
        timestamp = Time.time
    };
    
    JsonMessage.SendToHost(response);
}
```

#### Host_OnJsonMessage
```csharp
/// <summary>
/// 主机端：接收JSON消息并根据type分发
/// </summary>
private void Host_OnJsonMessage(NetPeer peer, string json)
{
    // 先解析type字段
    var baseMsg = JsonUtility.FromJson<BaseMessage>(json);
    var clientId = NetService.Instance.GetPlayerId(peer);
    
    switch (baseMsg.type)
    {
        case "sync_request":
            var requestMsg = JsonUtility.FromJson<SyncRequestMessage>(json);
            Host_OnSyncRequest(clientId, peer);
            break;
            
        case "clear_complete":
            var clearMsg = JsonUtility.FromJson<ClearCompleteMessage>(json);
            Host_OnClearComplete(clientId, clearMsg);
            break;
            
        case "sync_complete":
            var syncMsg = JsonUtility.FromJson<SyncCompleteMessage>(json);
            Host_OnSyncComplete(clientId, syncMsg);
            break;
            
        default:
            Debug.LogWarning($"[FullSync] 未知的消息类型: {baseMsg.type}");
            break;
    }
}

/// <summary>
/// 主机端：客户端请求同步
/// </summary>
private void Host_OnSyncRequest(string clientId, NetPeer peer)
{
    Debug.Log($"[FullSync] 客户端 {clientId} 请求全量同步");
    
    // 更新状态
    _clientSyncStates[clientId] = SyncState.WaitingClear;
    
    // 发送清空指令
    Host_SendClearRequest(peer);
}

/// <summary>
/// 主机端：客户端清空完成
/// </summary>
private void Host_OnClearComplete(string clientId, ClearCompleteMessage message)
{
    if (!message.success)
    {
        Debug.LogWarning($"[FullSync] 客户端 {clientId} 清空失败");
        _clientSyncStates[clientId] = SyncState.Failed;
        return;
    }
    
    Debug.Log($"[FullSync] 客户端 {clientId} 清空完成: AI={message.aiCount}, 掉落物={message.lootCount}");
    
    // 更新状态
    _clientSyncStates[clientId] = SyncState.WaitingSync;
    
    // 开始收集和发送同步数据
    StartCoroutine(Host_CollectAndSendSyncData(clientId));
}
```

#### Host_CollectAndSendSyncData
```csharp
/// <summary>
/// 主机端：收集并发送同步数据
/// </summary>
private IEnumerator Host_CollectAndSendSyncData(string clientId)
{
    // 1. 收集AI数据
    var aiData = EntityCollector.CollectAllAIData();
    yield return null;
    
    // 2. 收集掉落物数据
    var lootData = EntityCollector.CollectAllLootData();
    yield return null;
    
    // 3. 序列化为JSON
    var syncData = new FullSyncMessage
    {
        aiEntities = aiData,
        lootEntities = lootData,
        timestamp = Time.time
    };
    
    var json = JsonUtility.ToJson(syncData);
    
    // 4. 检查是否需要分包
    if (json.Length > 1024 * 1024) // 1MB
    {
        yield return StartCoroutine(Host_SendChunkedData(clientId, json));
    }
    else
    {
        // 直接发送
        JsonMessage.SendToPeer(GetPeerByClientId(clientId), syncData);
    }
    
    // 5. 更新状态
    _clientSyncStates[clientId] = SyncState.Syncing;
    
    // 6. 启动超时检测
    StartCoroutine(CheckSyncTimeout(clientId));
}
```

#### Client_OnFullSyncData
```csharp
/// <summary>
/// 客户端：接收到全量同步数据
/// </summary>
private void Client_OnFullSyncData(FullSyncMessage message)
{
    Debug.Log($"[FullSync] 收到同步数据: AI={message.aiEntities.Length}, 掉落物={message.lootEntities.Length}");
    
    // 启动应用协程
    StartCoroutine(Client_ApplySyncData(message));
}
```

#### Client_ApplySyncData
```csharp
/// <summary>
/// 客户端：应用同步数据
/// </summary>
private IEnumerator Client_ApplySyncData(FullSyncMessage message)
{
    int aiSpawned = 0;
    int lootSpawned = 0;
    int aiFailed = 0;
    int lootFailed = 0;
    
    // 1. 批量生成AI
    for (int i = 0; i < message.aiEntities.Length; i++)
    {
        try
        {
            EntitySpawner.SpawnAI(message.aiEntities[i]);
            aiSpawned++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FullSync] 生成AI失败: {ex.Message}");
            aiFailed++;
        }
        
        // 每批次后yield
        if ((i + 1) % SPAWN_BATCH_SIZE == 0)
        {
            yield return null;
        }
    }
    
    // 2. 批量生成掉落物
    for (int i = 0; i < message.lootEntities.Length; i++)
    {
        try
        {
            EntitySpawner.SpawnLoot(message.lootEntities[i]);
            lootSpawned++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FullSync] 生成掉落物失败: {ex.Message}");
            lootFailed++;
        }
        
        // 每批次后yield
        if ((i + 1) % SPAWN_BATCH_SIZE == 0)
        {
            yield return null;
        }
    }
    
    // 3. 发送完成确认
    var response = new SyncCompleteMessage
    {
        success = true,
        aiCount = aiSpawned,
        lootCount = lootSpawned,
        aiFailed = aiFailed,
        lootFailed = lootFailed,
        timestamp = Time.time
    };
    
    JsonMessage.SendToHost(response);
    
    Debug.Log($"[FullSync] 同步完成: AI={aiSpawned}/{message.aiEntities.Length}, 掉落物={lootSpawned}/{message.lootEntities.Length}");
}
```

### 2. EntityCollector

**职责**：收集主机端的所有AI和掉落物数据

**主要方法**：

#### CollectAllAIData
```csharp
/// <summary>
/// 收集所有AI数据
/// </summary>
public static AIEntityData[] CollectAllAIData()
{
    var aiList = new List<AIEntityData>();
    
    // 遍历AITool.aiById
    foreach (var kvp in AITool.aiById)
    {
        var aiId = kvp.Key;
        var ai = kvp.Value;
        
        if (ai == null || !AITool.IsRealAI(ai))
            continue;
        
        try
        {
            var data = new AIEntityData
            {
                id = aiId,
                position = ai.transform.position,
                rotation = ai.transform.rotation.eulerAngles,
                health = GetAIHealth(ai),
                maxHealth = GetAIMaxHealth(ai),
                seed = GetAISeed(aiId),
                equipment = GetAIEquipment(ai),
                animState = GetAIAnimState(ai),
                modelName = GetAIModelName(ai),
                displayName = GetAIDisplayName(ai),
                iconType = GetAIIconType(ai)
            };
            
            aiList.Add(data);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EntityCollector] 收集AI {aiId} 数据失败: {ex.Message}");
        }
    }
    
    Debug.Log($"[EntityCollector] 收集了 {aiList.Count} 个AI");
    return aiList.ToArray();
}
```

#### CollectAllLootData
```csharp
/// <summary>
/// 收集所有掉落物数据
/// </summary>
public static LootEntityData[] CollectAllLootData()
{
    var lootList = new List<LootEntityData>();
    
    // 查找场景中所有掉落物
    var allItems = GameObject.FindObjectsOfType<ItemAgent>();
    
    foreach (var item in allItems)
    {
        if (item == null || IsPlayerItem(item))
            continue;
        
        try
        {
            var data = new LootEntityData
            {
                id = item.GetInstanceID(),
                position = item.transform.position,
                rotation = item.transform.rotation.eulerAngles,
                itemType = item.ItemType,
                itemId = item.ItemID,
                quantity = item.Quantity,
                durability = item.Durability,
                attachments = GetItemAttachments(item)
            };
            
            lootList.Add(data);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EntityCollector] 收集掉落物失败: {ex.Message}");
        }
    }
    
    Debug.Log($"[EntityCollector] 收集了 {lootList.Count} 个掉落物");
    return lootList.ToArray();
}
```

### 3. EntityCleaner

**职责**：清空客户端的所有AI和掉落物

**主要方法**：

#### ClearAllEntities
```csharp
/// <summary>
/// 清空所有实体
/// </summary>
public static ClearResult ClearAllEntities()
{
    var result = new ClearResult();
    
    try
    {
        // 1. 清空AI
        result.aiCleared = ClearAllAI();
        
        // 2. 清空掉落物
        result.lootCleared = ClearAllLoot();
        
        result.success = true;
    }
    catch (Exception ex)
    {
        Debug.LogError($"[EntityCleaner] 清空失败: {ex.Message}");
        result.success = false;
    }
    
    return result;
}
```

#### ClearAllAI
```csharp
/// <summary>
/// 清空所有AI
/// </summary>
private static int ClearAllAI()
{
    int count = 0;
    
    // 复制字典避免迭代时修改
    var aiList = AITool.aiById.Values.ToList();
    
    foreach (var ai in aiList)
    {
        if (ai == null || !AITool.IsRealAI(ai))
            continue;
        
        try
        {
            // 销毁AI对象
            GameObject.Destroy(ai.gameObject);
            count++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EntityCleaner] 销毁AI失败: {ex.Message}");
        }
    }
    
    // 清空字典
    AITool.aiById.Clear();
    
    Debug.Log($"[EntityCleaner] 清空了 {count} 个AI");
    return count;
}
```

#### ClearAllLoot
```csharp
/// <summary>
/// 清空所有掉落物
/// </summary>
private static int ClearAllLoot()
{
    int count = 0;
    
    // 查找所有掉落物
    var allItems = GameObject.FindObjectsOfType<ItemAgent>();
    
    foreach (var item in allItems)
    {
        if (item == null || IsPlayerItem(item))
            continue;
        
        try
        {
            // 销毁掉落物对象
            GameObject.Destroy(item.gameObject);
            count++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EntityCleaner] 销毁掉落物失败: {ex.Message}");
        }
    }
    
    Debug.Log($"[EntityCleaner] 清空了 {count} 个掉落物");
    return count;
}
```

### 4. EntitySpawner

**职责**：根据同步数据生成AI和掉落物

**主要方法**：

#### SpawnAI
```csharp
/// <summary>
/// 生成AI实体
/// </summary>
public static void SpawnAI(AIEntityData data)
{
    // 1. 使用种子生成AI
    var ai = AITool.SpawnAIWithSeed(data.seed, data.position, Quaternion.Euler(data.rotation));
    
    if (ai == null)
    {
        Debug.LogError($"[EntitySpawner] 生成AI失败: seed={data.seed}");
        return;
    }
    
    // 2. 应用生命值
    var health = ai.GetComponentInChildren<Health>();
    if (health != null)
    {
        health.MaxHealth = data.maxHealth;
        health.CurrentHealth = data.health;
    }
    
    // 3. 应用装备
    if (data.equipment != null && data.equipment.Length > 0)
    {
        ApplyAIEquipment(ai, data.equipment);
    }
    
    // 4. 应用动画状态
    if (data.animState != null)
    {
        ApplyAIAnimState(ai, data.animState);
    }
    
    // 5. 应用名称和图标
    ApplyAINameIcon(ai, data.displayName, data.iconType);
    
    // 6. 注册到系统
    AITool.aiById[data.id] = ai;
    
    Debug.Log($"[EntitySpawner] 生成AI: id={data.id}, pos={data.position}");
}
```

#### SpawnLoot
```csharp
/// <summary>
/// 生成掉落物实体
/// </summary>
public static void SpawnLoot(LootEntityData data)
{
    // 1. 创建物品实例
    var item = ItemFactory.CreateItem(data.itemType, data.itemId);
    
    if (item == null)
    {
        Debug.LogError($"[EntitySpawner] 创建物品失败: type={data.itemType}, id={data.itemId}");
        return;
    }
    
    // 2. 设置位置和旋转
    item.transform.position = data.position;
    item.transform.rotation = Quaternion.Euler(data.rotation);
    
    // 3. 设置数量和耐久度
    item.Quantity = data.quantity;
    item.Durability = data.durability;
    
    // 4. 应用附件
    if (data.attachments != null && data.attachments.Length > 0)
    {
        ApplyItemAttachments(item, data.attachments);
    }
    
    Debug.Log($"[EntitySpawner] 生成掉落物: type={data.itemType}, pos={data.position}");
}
```

## 数据结构

### JSON消息格式

#### SyncRequestMessage
```csharp
[Serializable]
public class SyncRequestMessage
{
    public string type = "sync_request";
    public float timestamp;
}
```

#### ClearEntitiesMessage
```csharp
[Serializable]
public class ClearEntitiesMessage
{
    public string type = "clear_entities";
    public float timestamp;
}
```

#### ClearCompleteMessage
```csharp
[Serializable]
public class ClearCompleteMessage
{
    public string type = "clear_complete";
    public bool success;
    public int aiCount;
    public int lootCount;
    public float timestamp;
}
```

#### FullSyncMessage
```csharp
[Serializable]
public class FullSyncMessage
{
    public string type = "full_sync";
    public AIEntityData[] aiEntities;
    public LootEntityData[] lootEntities;
    public float timestamp;
}
```

#### SyncCompleteMessage
```csharp
[Serializable]
public class SyncCompleteMessage
{
    public string type = "sync_complete";
    public bool success;
    public int aiCount;
    public int lootCount;
    public int aiFailed;
    public int lootFailed;
    public float timestamp;
}
```

#### BaseMessage
```csharp
[Serializable]
public class BaseMessage
{
    public string type;
}
```

#### AIEntityData
```csharp
[Serializable]
public class AIEntityData
{
    public int id;
    public Vector3 position;
    public Vector3 rotation;
    public float health;
    public float maxHealth;
    public int seed;
    public string[] equipment;
    public AnimStateData animState;
    public string modelName;
    public string displayName;
    public string iconType;
}
```

#### LootEntityData
```csharp
[Serializable]
public class LootEntityData
{
    public int id;
    public Vector3 position;
    public Vector3 rotation;
    public string itemType;
    public int itemId;
    public int quantity;
    public float durability;
    public AttachmentData[] attachments;
}
```

## 错误处理

### 超时处理

```csharp
private IEnumerator CheckClearTimeout(string clientId)
{
    float startTime = Time.time;
    
    while (Time.time - startTime < CLEAR_TIMEOUT)
    {
        if (_clientSyncStates[clientId] != SyncState.Clearing)
            yield break;
        
        yield return new WaitForSeconds(0.5f);
    }
    
    // 超时
    Debug.LogWarning($"[FullSync] 客户端 {clientId} 清空超时");
    _clientSyncStates[clientId] = SyncState.Failed;
}
```

### 重试机制

```csharp
private IEnumerator Host_SendSyncDataWithRetry(string clientId, FullSyncMessage data, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            JsonMessage.SendToPeer(GetPeerByClientId(clientId), data);
            yield break; // 成功
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FullSync] 发送同步数据失败 (尝试 {i+1}/{maxRetries}): {ex.Message}");
            
            if (i < maxRetries - 1)
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }
    
    // 所有重试都失败
    Debug.LogError($"[FullSync] 发送同步数据失败，已达最大重试次数");
    _clientSyncStates[clientId] = SyncState.Failed;
}
```

## 性能优化

### 数据压缩

```csharp
/// <summary>
/// 压缩同步数据
/// </summary>
private static string CompressSyncData(string json)
{
    // 使用GZip压缩
    byte[] bytes = Encoding.UTF8.GetBytes(json);
    using (var ms = new MemoryStream())
    {
        using (var gzip = new GZipStream(ms, CompressionMode.Compress))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }
        byte[] compressed = ms.ToArray();
        return Convert.ToBase64String(compressed);
    }
}
```

### 批量生成

```csharp
/// <summary>
/// 批量生成实体，避免单帧卡顿
/// </summary>
private IEnumerator SpawnEntitiesInBatches<T>(T[] entities, Action<T> spawnFunc, int batchSize)
{
    for (int i = 0; i < entities.Length; i++)
    {
        spawnFunc(entities[i]);
        
        if ((i + 1) % batchSize == 0)
        {
            yield return null; // 下一帧继续
        }
    }
}
```

### 对象池

```csharp
/// <summary>
/// 网络数据包对象池
/// </summary>
public class NetDataWriterPool
{
    private static readonly Stack<NetDataWriter> _pool = new Stack<NetDataWriter>();
    
    public static NetDataWriter Get()
    {
        if (_pool.Count > 0)
        {
            var writer = _pool.Pop();
            writer.Reset();
            return writer;
        }
        return new NetDataWriter();
    }
    
    public static void Return(NetDataWriter writer)
    {
        if (_pool.Count < 10)
        {
            _pool.Push(writer);
        }
    }
}
```

## 测试策略

### 单元测试

1. **EntityCollector测试**
   - 测试AI数据收集的完整性
   - 测试掉落物数据收集的完整性
   - 测试异常情况处理

2. **EntityCleaner测试**
   - 测试AI清空功能
   - 测试掉落物清空功能
   - 测试玩家角色保留

3. **EntitySpawner测试**
   - 测试AI生成功能
   - 测试掉落物生成功能
   - 测试数据应用正确性

### 集成测试

1. **完整同步流程测试**
   - 测试清空→同步→验证的完整流程
   - 测试多客户端同时同步
   - 测试大量实体同步

2. **错误处理测试**
   - 测试超时处理
   - 测试重试机制
   - 测试网络中断恢复

3. **性能测试**
   - 测试同步时间
   - 测试内存占用
   - 测试帧率影响

## 注意事项

1. **数据一致性**：确保主机和客户端使用相同的种子生成AI
2. **性能影响**：批量生成避免单帧卡顿，建议每批50个实体
3. **错误恢复**：单个实体失败不应影响整体同步
4. **日志记录**：详细记录同步过程，便于调试
5. **版本兼容**：JSON格式变更需要考虑向后兼容性
