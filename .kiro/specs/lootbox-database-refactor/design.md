# 战利品箱数据库重写设计文档

## 概述

本设计文档描述了如何将现有的战利品箱同步机制从字典/列表结构重构为基于 `InMemoryDatabase` 的统一数据库架构。

**核心设计理念**:
- **主机端**: 使用 `LootBoxDatabase` 作为权威数据源，提供 O(1) 查询性能
- **客户端**: 所有战利品箱默认为空，只有打开时才从主机获取内容
- **通信**: 使用 JSON 消息进行战利品箱的增删改查操作

**客户端状态管理**:
1. **初始状态**: 所有战利品箱在客户端都是空的（不显示任何物品）
2. **打开箱子**: 
   - 发送 OPEN 请求到主机
   - 等待主机返回完整的物品列表
   - 收到响应后才显示 UI 和物品
3. **实时更新**:
   - 主机广播物品变更（PUT/TAKE/SPLIT）时，只发送 SetId + 操作类型 + 物品 ID
   - 客户端检查当前打开的箱子是否匹配 SetId
   - 如果匹配，则应用增量更新（添加/移除/修改物品）
   - 如果不匹配，则忽略（因为玩家没有打开那个箱子）

**优势**:
- 客户端不需要维护任何战利品箱状态
- 减少网络带宽（只同步当前打开的箱子）
- 避免数据不一致问题
- 简化代码逻辑

## 架构

### 整体架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                        战利品箱数据库架构                          │
└─────────────────────────────────────────────────────────────────┘

主机端 (Host)                              客户端 (Client)
┌──────────────────────┐                  ┌──────────────────────┐
│  LootBoxDatabase     │                  │  当前打开的箱子       │
│  ┌────────────────┐  │                  │  ┌────────────────┐  │
│  │ Primary Index  │  │                  │  │ CurrentLootBox │  │
│  │ SetId -> Entity│  │                  │  │ - SetId        │  │
│  └────────────────┘  │                  │  │ - Items[]      │  │
│  ┌────────────────┐  │                  │  │ - Capacity     │  │
│  │ Scene Index    │  │                  │  └────────────────┘  │
│  │ Position Index │  │                  │  (仅在打开时有数据)   │
│  │ Spatial Index  │  │                  └──────────────────────┘
│  └────────────────┘  │
└──────────┬───────────┘
           │                                         │
           │  JSON Messages (JsonMessageRouter)     │
           │  ┌──────────────────────────────────┐  │
           │  │ LOOT_OPEN_REQUEST                │  │
           │  │ LOOT_STATE_RESPONSE              │  │
           │  │ LOOT_PUT_REQUEST                 │  │
           │  │ LOOT_TAKE_REQUEST                │  │
           │  │ LOOT_SPLIT_REQUEST               │  │
           │  │ LOOT_STATE_UPDATE (broadcast)    │  │
           │  └──────────────────────────────────┘  │
           └─────────────────┬─────────────────────┘
                             │
                    ┌────────▼────────┐
                    │  Network Layer  │
                    │  (LiteNetLib)   │
                    └─────────────────┘
```

### 数据流图

```
┌─────────────────────────────────────────────────────────────────┐
│                    打开战利品箱数据流                              │
└─────────────────────────────────────────────────────────────────┘

客户端/主机玩家                        主机数据库
   │                                   │
   │ 1. 玩家打开容器                    │
   │    LootView.Open()                │
   │    显示加载中...                   │
   │                                   │
   │ 2. 发送 JSON 请求                  │
   │    {                              │
   │      "type": "LOOT_OPEN_REQUEST", │
   │      "setId": "loot_001"          │
   │    }                              │
   │    ──────────────────────────────>│
   │                                   │ 3. 查询数据库
   │                                   │    entity = hostDb.FindByKey(setId)
   │                                   │    O(1) 查询
   │                                   │
   │                                   │ 4. 序列化容器状态
   │                                   │    {
   │                                   │      "setId": "loot_001",
   │                                   │      "capacity": 20,
   │                                   │      "items": [
   │                                   │        {
   │                                   │          "position": 0,
   │                                   │          "snapshot": {...}
   │                                   │        }
   │                                   │      ]
   │                                   │    }
   │                                   │
   │<──────────────────────────────────│ 5. 发送 JSON 响应
   │    {                              │    "type": "LOOT_STATE_RESPONSE"
   │      "type": "LOOT_STATE_RESPONSE"│
   │      "setId": "loot_001",         │
   │      "capacity": 20,              │
   │      "items": [...]               │
   │    }                              │
   │                                   │
   │ 6. 保存当前打开的箱子               │
   │    _currentLootBox = response     │
   │                                   │
   │ 7. 应用到 UI                       │
   │    ApplyLootboxState(items)       │
   │    分帧处理，每5个yield一次         │
   │    显示物品                        │
   │                                   │

注意：
1. 主机玩家打开箱子时，也走相同的流程（自己给自己发消息）
2. 数据库是唯一的数据源，Inventory 在初始化时已被清空
```

```
┌─────────────────────────────────────────────────────────────────┐
│                    物品变更增量更新流程                            │
└─────────────────────────────────────────────────────────────────┘

客户端 A (操作者)                      主机                      客户端 B (观察者)
   │                                   │                                   │
   │ 1. 玩家取出物品                    │                                   │
   │    Client_RequestTake()           │                                   │
   │    ──────────────────────────────>│                                   │
   │                                   │ 2. 验证并执行                      │
   │                                   │    - 从数据库查询                  │
   │                                   │    - 移除物品                      │
   │                                   │    - 更新数据库                    │
   │                                   │                                   │
   │<──────────────────────────────────│ 3. 发送操作结果                    │
   │    {                              │    (仅给操作者)                    │
   │      "success": true,             │                                   │
   │      "token": 123,                │                                   │
   │      "item": {...}                │                                   │
   │    }                              │                                   │
   │                                   │                                   │
   │ 4. 添加物品到背包                  │                                   │
   │    AddItemToInventory(item)       │                                   │
   │                                   │                                   │
   │<══════════════════════════════════╬═══════════════════════════════════│
   │    {                              │ 5. 广播增量更新                    │
   │      "type": "LOOT_ITEM_REMOVED", │    (给所有客户端)                  │
   │      "setId": "loot_001",         │                                   │
   │      "position": 5                │                                   │
   │    }                              │                                   │
   │                                   │                                   │
   │ 6. 检查是否是当前打开的箱子         │                                   │ 6. 检查是否是当前打开的箱子
   │    if (_currentLootBox.SetId      │                                   │    if (_currentLootBox.SetId
   │        == "loot_001")              │                                   │        == "loot_001")
   │    {                              │                                   │    {
   │      RemoveItemAt(position: 5)    │                                   │      RemoveItemAt(position: 5)
   │      RefreshUI()                  │                                   │      RefreshUI()
   │    }                              │                                   │    }
   │                                   │                                   │
```

## 组件和接口

### 1. LootBoxEntity (战利品箱实体)

```csharp
/// <summary>
/// 战利品箱实体 - 存储在数据库中的数据结构
/// </summary>
public class LootBoxEntity
{
    // 唯一标识
    public string SetId { get; set; }
    
    // 游戏对象引用
    public GameObject GameObject { get; set; }
    public Inventory Inventory { get; set; }
    
    // 位置信息
    public Vector3 Position { get; set; }
    public string SceneName { get; set; }
    
    // 容器信息
    public int Capacity { get; set; }
    public List<LootItemEntry> Items { get; set; }
    
    // 元数据
    public int OwnerId { get; set; }  // -1 表示公共容器
    public DateTime LastModified { get; set; }
    
    // 自定义数据
    public Dictionary<string, object> CustomData { get; set; }
}

/// <summary>
/// 战利品箱中的物品条目
/// </summary>
public class LootItemEntry
{
    public int Position { get; set; }
    public ItemSnapshot Snapshot { get; set; }
}
```

### 2. LootBoxDatabase (战利品箱数据库)

```csharp
/// <summary>
/// 战利品箱数据库 - 高性能查询和管理
/// </summary>
public class LootBoxDatabase
{
    private readonly InMemoryDatabase<LootBoxEntity> _db;
    
    public int Count => _db.Count;
    
    public LootBoxDatabase(float spatialCellSize = 50f)
    {
        _db = new InMemoryDatabase<LootBoxEntity>()
            .WithPrimaryKey(e => e.SetId)
            .WithIndex("SceneName", e => e.SceneName)
            .WithIndex("OwnerId", e => e.OwnerId)
            .WithSpatialIndex(e => e.Position, spatialCellSize);
    }
    
    // === 基础操作 ===
    
    /// <summary>
    /// 添加战利品箱到数据库
    /// </summary>
    public bool AddLootBox(GameObject go, Inventory inv, string setId);
    
    /// <summary>
    /// 更新战利品箱状态（从 Inventory 读取最新数据）
    /// </summary>
    public bool UpdateLootBox(string setId);
    
    /// <summary>
    /// 删除战利品箱
    /// </summary>
    public bool RemoveLootBox(string setId);
    
    /// <summary>
    /// 清空数据库
    /// </summary>
    public void Clear();
    
    // === 查询操作 ===
    
    /// <summary>
    /// 按 SetId 查询（O(1)）
    /// </summary>
    public LootBoxEntity GetLootBox(string setId);
    
    /// <summary>
    /// 按场景查询（O(1)）
    /// </summary>
    public IEnumerable<LootBoxEntity> GetLootBoxesByScene(string sceneName);
    
    /// <summary>
    /// 空间范围查询（O(1)）
    /// </summary>
    public IEnumerable<LootBoxEntity> GetLootBoxesInRadius(Vector3 center, float radius);
    
    /// <summary>
    /// 获取所有公共战利品箱
    /// </summary>
    public IEnumerable<LootBoxEntity> GetPublicLootBoxes();
    
    // === JSON 导出 ===
    
    /// <summary>
    /// 导出整库为 JSON
    /// </summary>
    public string ExportToJson(bool indented = true);
    
    /// <summary>
    /// 导出带统计信息的 JSON
    /// </summary>
    public string ExportToJsonWithStats(bool indented = true);
    
    /// <summary>
    /// 按场景导出 JSON
    /// </summary>
    public string ExportBySceneToJson(string sceneName, bool indented = true);
}
```

### 3. LootBoxJsonMessages (JSON 消息定义)

```csharp
/// <summary>
/// 打开战利品箱请求
/// </summary>
public class LootOpenRequest
{
    public string SetId { get; set; }
    public int RequestVersion { get; set; }
}

/// <summary>
/// 战利品箱状态响应
/// </summary>
public class LootStateResponse
{
    public string SetId { get; set; }
    public int Capacity { get; set; }
    public List<LootItemEntry> Items { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 放入物品请求
/// </summary>
public class LootPutRequest
{
    public string SetId { get; set; }
    public int PreferredPosition { get; set; }
    public uint Token { get; set; }
    public ItemSnapshot ItemSnapshot { get; set; }
}

/// <summary>
/// 取出物品请求
/// </summary>
public class LootTakeRequest
{
    public string SetId { get; set; }
    public int Position { get; set; }
    public uint Token { get; set; }
    public DestinationType DestType { get; set; }  // Backpack, Slot, Specific
    public int DestPosition { get; set; }  // 用于 Specific
}

/// <summary>
/// 拆分物品请求
/// </summary>
public class LootSplitRequest
{
    public string SetId { get; set; }
    public int SourcePosition { get; set; }
    public int Count { get; set; }
    public int PreferredPosition { get; set; }
}

/// <summary>
/// 操作确认响应
/// </summary>
public class LootOperationResponse
{
    public bool Success { get; set; }
    public uint Token { get; set; }
    public string ErrorMessage { get; set; }
    public ItemSnapshot ResultItem { get; set; }  // 用于 TAKE 操作
}

/// <summary>
/// 物品添加广播（增量更新）
/// </summary>
public class LootItemAdded
{
    public string SetId { get; set; }
    public int Position { get; set; }
    public ItemSnapshot ItemSnapshot { get; set; }
}

/// <summary>
/// 物品移除广播（增量更新）
/// </summary>
public class LootItemRemoved
{
    public string SetId { get; set; }
    public int Position { get; set; }
}

/// <summary>
/// 物品修改广播（增量更新，如拆分后数量变化）
/// </summary>
public class LootItemModified
{
    public string SetId { get; set; }
    public int Position { get; set; }
    public ItemSnapshot ItemSnapshot { get; set; }
}
```

### 4. LootBoxSyncManager (同步管理器)

```csharp
/// <summary>
/// 战利品箱同步管理器 - 协调主机和客户端的数据库操作
/// </summary>
public class LootBoxSyncManager
{
    private LootBoxDatabase _hostDatabase;    // 主机端数据库（仅主机使用）
    
    // 客户端当前打开的箱子
    private LootStateResponse _currentLootBox;  // 仅在打开箱子时有数据
    
    private bool IsHost => NetService.Instance?.IsServer ?? false;
    
    // === 主机端方法 ===
    
    /// <summary>
    /// 主机：初始化数据库，扫描场景中的所有战利品箱
    /// 1. 扫描所有战利品箱
    /// 2. 从 Inventory 读取物品并保存到数据库
    /// 3. 清空 Inventory（避免数据不一致）
    /// 4. 数据库成为唯一的数据源
    /// </summary>
    public void Host_InitializeDatabase();
    
    /// <summary>
    /// 主机：处理打开请求
    /// 1. 从数据库查询箱子（O(1)）
    /// 2. 返回完整物品列表
    /// 注意：不需要从 Inventory 读取，因为数据库已经是唯一数据源
    /// </summary>
    public void Host_HandleOpenRequest(NetPeer peer, LootOpenRequest request);
    
    /// <summary>
    /// 主机：处理放入请求
    /// 1. 验证请求
    /// 2. 更新数据库（添加物品）
    /// 3. 广播增量更新
    /// 注意：不更新 Inventory，因为 Inventory 已经是空的
    /// </summary>
    public void Host_HandlePutRequest(NetPeer peer, LootPutRequest request);
    
    /// <summary>
    /// 主机：处理取出请求
    /// 1. 验证请求
    /// 2. 更新数据库（移除物品）
    /// 3. 广播增量更新
    /// 注意：不更新 Inventory，因为 Inventory 已经是空的
    /// </summary>
    public void Host_HandleTakeRequest(NetPeer peer, LootTakeRequest request);
    
    /// <summary>
    /// 主机：处理拆分请求
    /// 1. 验证请求
    /// 2. 更新数据库（修改源物品数量，添加新物品）
    /// 3. 广播增量更新
    /// 注意：不更新 Inventory，因为 Inventory 已经是空的
    /// </summary>
    public void Host_HandleSplitRequest(NetPeer peer, LootSplitRequest request);
    
    /// <summary>
    /// 主机：广播状态更新
    /// </summary>
    public void Host_BroadcastStateUpdate(string setId);
    
    // === 客户端方法 ===
    
    /// <summary>
    /// 客户端：请求打开战利品箱
    /// </summary>
    public void Client_RequestOpen(string setId);
    
    /// <summary>
    /// 客户端：请求放入物品
    /// </summary>
    public uint Client_RequestPut(string setId, Item item, int preferredPosition);
    
    /// <summary>
    /// 客户端：请求取出物品
    /// </summary>
    public uint Client_RequestTake(string setId, int position, DestinationType destType);
    
    /// <summary>
    /// 客户端：请求拆分物品
    /// </summary>
    public void Client_RequestSplit(string setId, int sourcePosition, int count, int preferredPosition);
    
    /// <summary>
    /// 客户端：处理状态响应
    /// </summary>
    public void Client_HandleStateResponse(LootStateResponse response);
    
    /// <summary>
    /// 客户端：处理操作响应
    /// </summary>
    public void Client_HandleOperationResponse(LootOperationResponse response);
    
    /// <summary>
    /// 客户端：处理物品添加广播
    /// </summary>
    public void Client_HandleItemAdded(LootItemAdded message);
    
    /// <summary>
    /// 客户端：处理物品移除广播
    /// </summary>
    public void Client_HandleItemRemoved(LootItemRemoved message);
    
    /// <summary>
    /// 客户端：处理物品修改广播
    /// </summary>
    public void Client_HandleItemModified(LootItemModified message);
    
    // === 通用方法 ===
    
    /// <summary>
    /// 导出数据库为 JSON（调试用）
    /// </summary>
    public string ExportDatabaseToJson(bool indented = true);
    
    /// <summary>
    /// 检查本地缓存是否有效
    /// </summary>
    public bool IsCacheValid(string setId, TimeSpan maxAge);
}
```

## 数据模型

### 战利品箱实体关系图

```
LootBoxEntity
├── SetId (PK)
├── GameObject (引用)
├── Inventory (引用)
├── Position (空间索引)
├── SceneName (二级索引)
├── Capacity
├── Items (List<LootItemEntry>)
│   └── LootItemEntry
│       ├── Position
│       └── Snapshot (ItemSnapshot)
│           ├── TypeId
│           ├── Stack
│           ├── Durability
│           ├── Slots (递归)
│           └── Inventory (递归)
├── OwnerId (二级索引)
├── LastModified
├── LastSyncTime
├── Version
└── CustomData
```

### 数据库索引策略

```
InMemoryDatabase<LootBoxEntity>
├── Primary Index: SetId -> Entity (O(1))
├── Secondary Indexes:
│   ├── SceneName -> HashSet<Entity> (O(1))
│   └── OwnerId -> HashSet<Entity> (O(1))
└── Spatial Index: Grid(50m) -> HashSet<Entity> (O(1))
```

## 错误处理

### 错误类型和处理策略

由于客户端每次操作都从主机获取最新状态，错误处理变得非常简单：

```csharp
public enum LootErrorType
{
    LootBoxNotFound,      // 战利品箱不存在（主机端）
    InvalidSetId,         // 无效的 SetId
    CapacityExceeded,     // 容量超限
    ItemNotFound,         // 物品不存在
    InvalidPosition,      // 无效的位置
    ValidationFailed,     // 验证失败（如物品快照无效）
    NetworkTimeout        // 网络超时
}

public class LootErrorHandler
{
    /// <summary>
    /// 处理错误 - 简化版本
    /// </summary>
    public void HandleError(LootErrorType errorType, string setId, string details)
    {
        switch (errorType)
        {
            case LootErrorType.NetworkTimeout:
                // 重试请求（最多3次）
                RetryRequest(setId);
                break;
                
            case LootErrorType.LootBoxNotFound:
                // 战利品箱不存在，关闭 UI 并通知玩家
                CloseLootUI();
                NotifyPlayer("战利品箱已不存在");
                break;
                
            default:
                // 记录日志并通知玩家
                LogError(errorType, setId, details);
                NotifyPlayer($"操作失败: {errorType}");
                break;
        }
    }
    
    /// <summary>
    /// 重试请求（指数退避）
    /// </summary>
    private async void RetryRequest(string setId)
    {
        for (int i = 0; i < 3; i++)
        {
            await Task.Delay((int)Math.Pow(2, i) * 100);  // 100ms, 200ms, 400ms
            
            // 重新请求
            Client_RequestOpen(setId);
            
            // 等待响应...
        }
    }
}
```

### 简化的错误恢复流程

```
┌─────────────────────────────────────────────────────────────────┐
│                    简化的错误处理流程                              │
└─────────────────────────────────────────────────────────────────┘

客户端                                主机
   │                                   │
   │ 1. 发送操作请求                    │
   │    (OPEN/PUT/TAKE/SPLIT)          │
   │    ──────────────────────────────>│
   │                                   │
   │                                   │ 2. 验证请求
   │                                   │    - 战利品箱是否存在
   │                                   │    - 参数是否有效
   │                                   │    - 物品快照是否合法
   │                                   │
   │<──────────────────────────────────│ 3a. 成功：返回结果
   │    {                              │     或广播状态更新
   │      "success": true,             │
   │      "data": {...}                │
   │    }                              │
   │                                   │
   │<──────────────────────────────────│ 3b. 失败：返回错误
   │    {                              │
   │      "success": false,            │
   │      "error": "LootBoxNotFound",  │
   │      "message": "战利品箱不存在"    │
   │    }                              │
   │                                   │
   │ 4. 处理错误                        │
   │    - 网络超时：重试                 │
   │    - 其他错误：通知玩家             │
   │                                   │
```

**关键点**:
- 不需要版本号和冲突检测
- 不需要重同步机制
- 主机的响应就是最新的权威状态
- 客户端只需要简单的重试逻辑

## 测试策略

### 单元测试

```csharp
[TestFixture]
public class LootBoxDatabaseTests
{
    [Test]
    public void AddLootBox_ValidData_ReturnsTrue()
    {
        // Arrange
        var db = new LootBoxDatabase();
        var go = new GameObject("TestLootBox");
        var inv = go.AddComponent<Inventory>();
        
        // Act
        var result = db.AddLootBox(go, inv, "test_001");
        
        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(1, db.Count);
    }
    
    [Test]
    public void GetLootBox_ExistingSetId_ReturnsEntity()
    {
        // Arrange
        var db = new LootBoxDatabase();
        db.AddLootBox(go, inv, "test_001");
        
        // Act
        var entity = db.GetLootBox("test_001");
        
        // Assert
        Assert.IsNotNull(entity);
        Assert.AreEqual("test_001", entity.SetId);
    }
    
    [Test]
    public void GetLootBoxesInRadius_WithinRange_ReturnsEntities()
    {
        // Arrange
        var db = new LootBoxDatabase();
        db.AddLootBox(go1, inv1, "test_001");  // Position (0, 0, 0)
        db.AddLootBox(go2, inv2, "test_002");  // Position (10, 0, 0)
        db.AddLootBox(go3, inv3, "test_003");  // Position (100, 0, 0)
        
        // Act
        var results = db.GetLootBoxesInRadius(Vector3.zero, 20f).ToList();
        
        // Assert
        Assert.AreEqual(2, results.Count);
    }
}
```

### 集成测试

```csharp
[TestFixture]
public class LootBoxSyncIntegrationTests
{
    [Test]
    public async Task Client_RequestOpen_ReceivesStateResponse()
    {
        // Arrange
        var host = SetupHost();
        var client = SetupClient();
        host.InitializeDatabase();
        
        // Act
        client.RequestOpen("loot_001");
        await Task.Delay(100);  // 等待网络传输
        
        // Assert
        var entity = client.GetLocalEntity("loot_001");
        Assert.IsNotNull(entity);
        Assert.AreEqual(20, entity.Capacity);
    }
    
    [Test]
    public async Task Client_PutItem_UpdatesHostDatabase()
    {
        // Arrange
        var host = SetupHost();
        var client = SetupClient();
        var item = CreateTestItem();
        
        // Act
        var token = client.RequestPut("loot_001", item, 0);
        await Task.Delay(100);
        
        // Assert
        var hostEntity = host.GetLootBox("loot_001");
        Assert.AreEqual(1, hostEntity.Items.Count);
        Assert.AreEqual(0, hostEntity.Items[0].Position);
    }
}
```

### 性能测试

```csharp
[TestFixture]
public class LootBoxPerformanceTests
{
    [Test]
    public void QueryPerformance_10000LootBoxes_CompletesInUnder10ms()
    {
        // Arrange
        var db = new LootBoxDatabase();
        for (int i = 0; i < 10000; i++)
        {
            db.AddLootBox(CreateTestLootBox($"loot_{i}"));
        }
        
        // Act
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            var entity = db.GetLootBox($"loot_{i}");
        }
        sw.Stop();
        
        // Assert
        Assert.Less(sw.ElapsedMilliseconds, 10);
    }
    
    [Test]
    public void SpatialQuery_10000LootBoxes_CompletesInUnder50ms()
    {
        // Arrange
        var db = new LootBoxDatabase();
        for (int i = 0; i < 10000; i++)
        {
            db.AddLootBox(CreateTestLootBoxAtPosition(
                new Vector3(i * 10, 0, 0), $"loot_{i}"));
        }
        
        // Act
        var sw = Stopwatch.StartNew();
        var results = db.GetLootBoxesInRadius(Vector3.zero, 100f).ToList();
        sw.Stop();
        
        // Assert
        Assert.Less(sw.ElapsedMilliseconds, 50);
    }
}
```

## 性能考虑

### 内存占用估算

```
单个 LootBoxEntity 内存占用:
- 基础字段: ~200 bytes
- SetId (string): ~50 bytes
- Position (Vector3): 12 bytes
- Items (List): 每个物品 ~500 bytes (包含快照)

假设场景中有 100 个战利品箱，每个平均 10 个物品:
总内存 = 100 * (200 + 50 + 12 + 10 * 500)
       = 100 * 5262
       = 526 KB

可接受范围内。
```

### 网络带宽估算

```
单个 LootStateResponse 消息大小:
- 基础字段: ~100 bytes
- 每个物品快照: ~300 bytes (JSON 格式)

假设容器有 20 个物品:
消息大小 = 100 + 20 * 300 = 6100 bytes ≈ 6 KB

使用 LiteNetLib 的可靠有序传输，
在 100ms 延迟下，带宽占用约 60 KB/s，可接受。
```

### 优化策略

1. **增量更新**: 只发送变化的物品，而不是整个容器
2. **压缩**: 对大型快照使用 GZip 压缩
3. **批处理**: 合并多个小更新为一个大更新
4. **缓存**: 客户端缓存最近访问的战利品箱状态
5. **分帧**: 大型容器的应用分多帧处理，避免卡顿

## 迁移计划

### 阶段 1: 数据库基础设施

- 创建 `LootBoxEntity` 和 `LootBoxDatabase` 类
- 实现基础的增删改查操作
- 编写单元测试

### 阶段 2: JSON 消息系统

- 定义所有 JSON 消息类型
- 实现消息序列化/反序列化
- 集成到 `JsonMessageRouter`

### 阶段 3: 主机端实现

- 实现 `Host_InitializeDatabase()`
- 实现所有 `Host_Handle*Request()` 方法
- 实现状态广播机制

### 阶段 4: 客户端实现

- 实现所有 `Client_Request*()` 方法
- 实现响应处理逻辑
- 实现本地缓存机制

### 阶段 5: 集成和测试

- 替换现有的 `LootNet` 调用
- 进行集成测试
- 性能测试和优化

### 阶段 6: 向后兼容和清理

- 保留旧代码作为备用
- 添加功能开关
- 清理废弃代码

## 调试工具

### JSON 导出命令

```csharp
// 在 MModUI 中添加调试按钮
public void DebugExportLootBoxDatabase()
{
    var json = LootBoxSyncManager.Instance.ExportDatabaseToJson(indented: true);
    var filePath = Path.Combine(
        Application.persistentDataPath,
        $"lootbox_database_{DateTime.Now:yyyyMMdd_HHmmss}.json"
    );
    File.WriteAllText(filePath, json);
    Debug.Log($"[DEBUG] LootBox database exported to: {filePath}");
}
```

### 日志格式

```
[LOOT] [Host] Initialized database with 47 loot boxes
[LOOT] [Host] Received OPEN request from peer 192.168.1.100:5000 for setId=loot_001
[LOOT] [Host] Sent STATE response for setId=loot_001 (20 items, 6.2 KB)
[LOOT] [Client] Received STATE response for setId=loot_001 (version=5)
[LOOT] [Client] Applied state update for setId=loot_001 (took 15ms, 20 items)
[LOOT] [Client] Cache hit for setId=loot_002 (age=3.5s)
[LOOT] [ERROR] Failed to find loot box setId=loot_999, requesting resync
```

---

*设计文档版本: 1.0*
*最后更新: 2025-11-11*
