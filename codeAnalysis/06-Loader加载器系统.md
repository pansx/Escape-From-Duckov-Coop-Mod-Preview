# Loader 加载器系统分析

## 概览

Loader目录包含了模组的加载器和主要的网络消息处理逻辑，是整个联机模组的入口点和消息分发中心。这个系统负责初始化所有组件、应用Harmony补丁，以及处理所有的网络通信。

## 核心文件

### 1. Loader.cs - 模组加载器
**职责**: 模组的入口点，负责初始化和组件加载

#### 主要功能
```csharp
public class ModBehaviour : Duckov.Modding.ModBehaviour
{
    public Harmony Harmony;
    
    public void OnEnable()
    {
        // 1. 应用Harmony补丁
        Harmony = new Harmony("DETF_COOP");
        Harmony.PatchAll();
        
        // 2. 创建核心GameObject
        var go = new GameObject("COOP_MOD_1");
        DontDestroyOnLoad(go);
        
        // 3. 添加核心组件
        go.AddComponent<NetService>();
        COOPManager.InitManager();
        go.AddComponent<ModBehaviourF>();
        
        // 4. 执行加载流程
        Loader();
    }
}
```

#### 组件初始化流程
```csharp
public void Loader()
{
    // 1. 初始化本地化系统
    CoopLocalization.Initialize();
    
    // 2. 创建辅助GameObject
    var go = new GameObject("COOP_MOD_");
    DontDestroyOnLoad(go);
    
    // 3. 添加所有管理器组件
    go.AddComponent<AIRequest>();
    go.AddComponent<Send_ClientStatus>();
    go.AddComponent<HealthM>();
    go.AddComponent<LocalPlayerManager>();
    go.AddComponent<SendLocalPlayerStatus>();
    go.AddComponent<Spectator>();
    go.AddComponent<DeadLootBox>();
    go.AddComponent<LootManager>();
    go.AddComponent<SceneNet>();
    go.AddComponent<ModUI>();
    
    // 4. 初始化工具类
    CoopTool.Init();
    
    // 5. 延迟初始化
    DeferredInit();
}
```

#### 延迟初始化机制
```csharp
private void DeferredInit()
{
    SafeInit<SceneNet>(sn => sn.Init());
    SafeInit<LootManager>(lm => lm.Init());
    SafeInit<LocalPlayerManager>(lpm => lpm.Init());
    SafeInit<HealthM>(hm => hm.Init());
    SafeInit<SendLocalPlayerStatus>(s => s.Init());
    SafeInit<Spectator>(s => s.Init());
    SafeInit<ModUI>(ui => ui.Init());
    SafeInit<AIRequest>(a => a.Init());
    SafeInit<Send_ClientStatus>(s => s.Init());
    SafeInit<DeadLootBox>(s => s.Init());
}

private void SafeInit<T>(Action<T> init) where T : Component
{
    var c = FindObjectOfType<T>();
    if (c == null) return;
    try
    {
        init(c);
    }
    catch { }
}
```

### 2. Mod.cs - 主要行为控制器
**职责**: 网络消息处理、游戏状态管理和主循环控制

#### 核心类结构
```csharp
public class ModBehaviourF : MonoBehaviour
{
    public static ModBehaviourF Instance;
    
    // 网络相关属性
    public bool IsServer => Service != null && Service.IsServer;
    public NetManager netManager => Service?.netManager;
    public NetPeer connectedPeer => Service?.connectedPeer;
    public bool networkStarted => Service != null && Service.networkStarted;
    
    // 游戏状态
    public bool Pausebool;
    public bool Client_ForceShowAllRemoteAI = true;
    
    // 调试开关
    public static bool LogAiHpDebug = false;
    public static bool LogAiLoadoutDebug = true;
}
```

#### 主循环处理 (Update方法)
```csharp
private void Update()
{
    // 1. 玩家装备监听初始化
    if (CharacterMainControl.Main != null && !isinit)
    {
        // 绑定装备槽位变化事件
        BindEquipmentSlotEvents();
        BindWeaponChangeEvents();
    }
    
    // 2. 暂停状态处理
    if (Pausebool)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    // 3. UI控制
    if (Input.GetKeyDown(KeyCode.Home)) 
        ModUI.Instance.showUI = !ModUI.Instance.showUI;
    
    // 4. 网络处理
    if (networkStarted)
    {
        netManager.PollEvents();
        HandleNetworkTiming();
        HandleServerSpecificTasks();
        HandleClientSpecificTasks();
    }
    
    // 5. 游戏状态更新
    UpdateGameState();
}
```

#### 网络消息分发系统
**OnNetworkReceive方法**: 处理所有网络消息的中央分发器

```csharp
public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, 
    byte channelNumber, DeliveryMethod deliveryMethod)
{
    if (reader.AvailableBytes <= 0)
    {
        reader.Recycle();
        return;
    }
    
    var op = (Op)reader.GetByte();
    
    switch (op)
    {
        case Op.PLAYER_STATUS_UPDATE:
            HandlePlayerStatusUpdate(reader);
            break;
            
        case Op.CLIENT_STATUS_UPDATE:
            if (IsServer) COOPManager.ClientHandle.HandleClientStatusUpdate(peer, reader);
            break;
            
        case Op.POSITION_UPDATE:
            HandlePositionUpdate(peer, reader);
            break;
            
        case Op.ANIM_SYNC:
            HandleAnimationSync(peer, reader);
            break;
            
        // ... 处理所有其他操作码
        
        default:
            Debug.LogWarning($"Unknown opcode: {(byte)op}");
            break;
    }
    
    reader.Recycle();
}
```

## 网络消息处理详解

### 1. 玩家状态同步
```csharp
case Op.PLAYER_STATUS_UPDATE:
    if (!IsServer)
    {
        var playerCount = reader.GetInt();
        clientPlayerStatuses.Clear();
        
        for (var i = 0; i < playerCount; i++)
        {
            // 读取玩家数据
            var endPoint = reader.GetString();
            var playerName = reader.GetString();
            var latency = reader.GetInt();
            var isInGame = reader.GetBool();
            var position = reader.GetVector3();
            var rotation = reader.GetQuaternion();
            var sceneId = reader.GetString();
            var customFaceJson = reader.GetString();
            
            // 读取装备数据
            var equipmentCount = reader.GetInt();
            var equipmentList = new List<EquipmentSyncData>();
            for (var j = 0; j < equipmentCount; j++)
                equipmentList.Add(EquipmentSyncData.Deserialize(reader));
            
            // 应用状态更新
            ApplyPlayerStatus(endPoint, playerName, position, rotation, 
                sceneId, customFaceJson, equipmentList);
        }
    }
    break;
```

### 2. 位置更新处理
```csharp
case Op.POSITION_UPDATE:
    if (IsServer)
    {
        // 主机转发位置更新
        var endPointC = reader.GetString();
        var posS = reader.GetV3cm();
        var dirS = reader.GetDir();
        var rotS = Quaternion.LookRotation(dirS, Vector3.up);
        
        COOPManager.PublicHandleUpdate.HandlePositionUpdate_Q(peer, endPointC, posS, rotS);
    }
    else
    {
        // 客户端应用位置更新
        var endPointS = reader.GetString();
        var posS = reader.GetV3cm();
        var dirS = reader.GetDir();
        var rotS = Quaternion.LookRotation(dirS, Vector3.up);
        
        if (!NetService.Instance.IsSelfId(endPointS))
        {
            ApplyRemotePlayerPosition(endPointS, posS, rotS);
        }
    }
    break;
```

### 3. AI系统同步
```csharp
case Op.AI_TRANSFORM_SNAPSHOT:
    if (IsServer) break;
    var n = reader.GetInt();
    
    if (!AITool._aiSceneReady)
    {
        // AI场景未就绪，缓存变换数据
        for (var i = 0; i < n; ++i)
        {
            var aiId = reader.GetInt();
            var p = reader.GetV3cm();
            var f = reader.GetDir();
            if (_pendingAiTrans.Count < 512) 
                _pendingAiTrans.Enqueue((aiId, p, f));
        }
    }
    else
    {
        // 直接应用AI变换
        for (var i = 0; i < n; i++)
        {
            var aiId = reader.GetInt();
            var p = reader.GetV3cm();
            var f = reader.GetDir();
            AITool.ApplyAiTransform(aiId, p, f);
        }
    }
    break;
```

### 4. 战利品系统同步
```csharp
case Op.LOOT_REQ_OPEN:
    if (IsServer) LootManager.Instance.Server_HandleLootOpenRequest(peer, reader);
    break;

case Op.LOOT_STATE:
    if (IsServer) break;
    COOPManager.LootNet.Client_ApplyLootboxState(reader);
    break;

case Op.LOOT_REQ_PUT:
    if (!IsServer) break;
    COOPManager.LootNet.Server_HandleLootPutRequest(peer, reader);
    break;

case Op.LOOT_REQ_TAKE:
    if (!IsServer) break;
    COOPManager.LootNet.Server_HandleLootTakeRequest(peer, reader);
    break;
```

### 5. 场景管理同步
```csharp
case Op.SCENE_VOTE_START:
    if (!IsServer)
    {
        SceneNet.Instance.Client_OnSceneVoteStart(reader);
        // 观战中收到投票，标记投票结束时结算
        if (Spectator.Instance._spectatorActive) 
            Spectator.Instance._spectatorEndOnVotePending = true;
    }
    break;

case Op.SCENE_BEGIN_LOAD:
    if (!IsServer)
    {
        // 观战玩家直接弹出结算，不参与场景切换
        if (Spectator.Instance._spectatorActive && 
            Spectator.Instance._spectatorEndOnVotePending)
        {
            Spectator.Instance.EndSpectatorAndShowClosure();
            break;
        }
        
        // 普通玩家正常切换场景
        SceneNet.Instance.Client_OnBeginSceneLoad(reader);
    }
    break;
```

## 生命周期管理

### 1. 初始化流程
```csharp
private void OnEnable()
{
    // 注册场景事件
    SceneManager.sceneLoaded += OnSceneLoaded_IndexDestructibles;
    LevelManager.OnAfterLevelInitialized += LevelManager_OnAfterLevelInitialized;
    LevelManager.OnLevelInitialized += OnLevelInitialized_IndexDestructibles;
    
    // 注册其他生命周期事件
    SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    LevelManager.OnLevelInitialized += LevelManager_OnLevelInitialized;
}
```

### 2. 场景切换处理
```csharp
private void OnSceneLoaded_IndexDestructibles(Scene s, LoadSceneMode m)
{
    if (!networkStarted) return;
    
    // 重建可破坏物索引
    COOPManager.destructible.BuildDestructibleIndex();
    
    // 重置客户端状态
    HealthTool._cliHookedSelf = false;
    
    if (!IsServer)
    {
        HealthTool._cliInitHpReported = false;
        HealthM.Instance.Client_ReportSelfHealth_IfReadyOnce();
    }
    
    // 更新玩家场景状态
    UpdatePlayerSceneStatus();
}
```

### 3. 清理和销毁
```csharp
private void OnDestroy()
{
    NetService.Instance.StopNetwork();
}

private void OnDisable()
{
    // 取消注册所有事件
    SceneManager.sceneLoaded -= OnSceneLoaded_IndexDestructibles;
    LevelManager.OnLevelInitialized -= OnLevelInitialized_IndexDestructibles;
    SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
    LevelManager.OnLevelInitialized -= LevelManager_OnLevelInitialized;
}
```

## 性能优化策略

### 1. 消息处理优化
- **批量处理**: 每帧限制处理的AI变换数量
- **缓存机制**: 缓存频繁访问的组件引用
- **早期退出**: 优先检查失败条件

### 2. 内存管理
- **对象池化**: 重用网络消息对象
- **及时清理**: 自动回收NetPacketReader
- **弱引用**: 避免循环引用导致的内存泄漏

### 3. 网络优化
- **消息合并**: 批量发送多个小消息
- **数据压缩**: 使用高效的序列化格式
- **优先级队列**: 重要消息优先处理

## 错误处理和调试

### 1. 异常处理
```csharp
private void SafeInit<T>(Action<T> init) where T : Component
{
    var c = FindObjectOfType<T>();
    if (c == null) return;
    try
    {
        init(c);
    }
    catch
    {
        // 静默处理初始化异常
    }
}
```

### 2. 调试支持
```csharp
public static bool LogAiHpDebug = false;
public static bool LogAiLoadoutDebug = true;

// 条件日志输出
if (LogAiLoadoutDebug)
    Debug.Log($"[AI-RECV] ver={ver} aiId={aiId} model='{modelName}'");
```

### 3. 状态验证
```csharp
// 防御性编程
if (float.IsNaN(posS.x) || float.IsNaN(posS.y) || float.IsNaN(posS.z) ||
    float.IsInfinity(posS.x) || float.IsInfinity(posS.y) || float.IsInfinity(posS.z))
    break; // 跳过无效数据
```

## 总结

Loader系统是整个联机模组的神经中枢，通过精心设计的初始化流程、消息分发机制和生命周期管理，确保了模组的稳定运行。该系统展现了以下特点：

1. **模块化设计**: 清晰的组件分离和职责划分
2. **健壮性**: 完善的错误处理和异常恢复机制
3. **可扩展性**: 易于添加新的网络消息类型和处理逻辑
4. **性能优化**: 多种优化策略确保流畅的游戏体验
5. **调试友好**: 丰富的日志输出和调试开关

这个系统为整个联机模组提供了坚实的基础，是实现稳定多人游戏体验的关键组件。