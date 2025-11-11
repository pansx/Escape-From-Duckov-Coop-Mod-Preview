# 战利品箱数据库使用指南

## 概述

`LootBoxDatabase` 是一个高性能的战利品箱管理系统，基于 `InMemoryDatabase` 实现，提供 O(1) 查询性能和空间索引支持。

## 核心类

### ItemSnapshot

物品快照，用于序列化和网络传输：

```csharp
public class ItemSnapshot
{
    public string TypeId { get; set; }
    public int Stack { get; set; }
    public float Durability { get; set; }
    public bool Inspected { get; set; }
    public List<ItemSnapshot> Slots { get; set; }        // 装备槽（递归）
    public List<ItemSnapshot> Inventory { get; set; }    // 容器内容（递归）
    public Dictionary<string, object> CustomData { get; set; }
}
```

### LootItemEntry

战利品箱中的物品条目：

```csharp
public class LootItemEntry
{
    public int Position { get; set; }        // 槽位索引
    public ItemSnapshot Snapshot { get; set; }  // 物品快照
}
```

### LootBoxEntity

战利品箱实体：

```csharp
public class LootBoxEntity
{
    public string SetId { get; set; }                    // 唯一标识
    public GameObject GameObject { get; set; }           // GameObject 引用
    public object Inventory { get; set; }                // Inventory 组件
    public Vector3 Position { get; set; }                // 世界坐标
    public string SceneName { get; set; }                // 场景名称
    public int Capacity { get; set; }                    // 容量
    public List<LootItemEntry> Items { get; set; }       // 物品列表
    public int OwnerId { get; set; }                     // 所有者 ID（-1 = 公共）
    public DateTime LastModified { get; set; }           // 最后修改时间
    public Dictionary<string, object> CustomData { get; set; }  // 自定义数据
}
```

## 快速开始

### 1. 创建数据库

```csharp
// 创建数据库（50米网格大小）
var lootBoxDb = new LootBoxDatabase(spatialCellSize: 50f);
```

### 2. 添加战利品箱

```csharp
// 添加单个战利品箱
var success = lootBoxDb.AddLootBox(
    go: lootBoxGameObject,
    inv: lootBoxInventory,
    setId: "loot_001"
);
```

### 3. 查询战利品箱

```csharp
// 按 SetId 查询（O(1)）
var lootBox = lootBoxDb.GetLootBox("loot_001");

// 按场景查询（O(1)）
var sceneLootBoxes = lootBoxDb.GetLootBoxesByScene("MainScene");

// 空间范围查询（O(1)）
var nearbyLootBoxes = lootBoxDb.GetLootBoxesInRadius(playerPosition, 50f);

// 获取所有公共战利品箱
var publicLootBoxes = lootBoxDb.GetPublicLootBoxes();
```

### 4. 更新和删除

```csharp
// 更新战利品箱
lootBoxDb.UpdateLootBox("loot_001");

// 删除战利品箱
lootBoxDb.RemoveLootBox("loot_001");

// 清空数据库
lootBoxDb.Clear();
```

## 高级功能

### 复杂条件查询

```csharp
// 查找特定条件的战利品箱
var results = lootBoxDb.FindLootBoxes(e => 
    e.SceneName == "MainScene" && 
    e.Items.Count > 5 &&
    e.OwnerId == -1
);
```

### 操作物品列表

```csharp
var lootBox = lootBoxDb.GetLootBox("loot_001");

// 添加物品
lootBox.Items.Add(new LootItemEntry
{
    Position = 0,
    Snapshot = new ItemSnapshot
    {
        TypeId = "weapon_ak47",
        Stack = 1,
        Durability = 100f,
        Inspected = false
    }
});

// 更新数据库
lootBoxDb.UpdateLootBox("loot_001");
```

### 自定义数据

```csharp
var lootBox = lootBoxDb.GetLootBox("loot_001");

// 存储自定义数据
lootBox.CustomData["LastSyncTime"] = DateTime.Now;
lootBox.CustomData["IsSynced"] = true;

// 更新数据库
lootBoxDb.UpdateLootBox("loot_001");
```

## JSON 导出

### 基础导出

```csharp
// 导出整库（简化格式）
var json = lootBoxDb.ExportToJson(indented: true);
Debug.Log(json);

// 导出带统计信息
var jsonWithStats = lootBoxDb.ExportToJsonWithStats(indented: true);
Debug.Log(jsonWithStats);
```

### 选择性导出

```csharp
// 按场景导出
var sceneJson = lootBoxDb.ExportBySceneToJson("MainScene", indented: true);
```

### 保存到文件

```csharp
var json = lootBoxDb.ExportToJsonWithStats(indented: true);
var filePath = Path.Combine(
    Application.persistentDataPath,
    $"lootbox_database_{DateTime.Now:yyyyMMdd_HHmmss}.json"
);

File.WriteAllText(filePath, json);
Debug.Log($"数据库已导出到: {filePath}");
```

### JSON 格式示例

```json
{
  "ExportTime": "2025-11-11 12:00:00",
  "TotalLootBoxes": 47,
  "TotalItems": 235,
  "Statistics": {
    "ByScene": {
      "MainScene": 30,
      "Dungeon": 17
    },
    "PublicLootBoxes": 47
  },
  "LootBoxes": [
    {
      "SetId": "loot_001",
      "SceneName": "MainScene",
      "OwnerId": -1,
      "Capacity": 20,
      "Position": {
        "x": 123.45,
        "y": 0.0,
        "z": 67.89
      },
      "ItemCount": 5,
      "GameObjectName": "LootBox_001",
      "Active": true,
      "LastModified": "2025-11-11 11:58:00",
      "CustomData": {}
    }
  ]
}
```

## 性能特性

| 操作 | 复杂度 | 说明 |
|------|-------|------|
| `GetLootBox()` | O(1) | 主键查询 |
| `GetLootBoxesByScene()` | O(1) | 索引查询 |
| `GetLootBoxesInRadius()` | O(1) | 空间索引查询 |
| `GetPublicLootBoxes()` | O(1) | 索引查询 |
| `FindLootBoxes()` | O(n) | 复杂条件查询 |
| `AddLootBox()` | O(1) | 插入操作 |
| `UpdateLootBox()` | O(1) | 更新操作 |
| `RemoveLootBox()` | O(1) | 删除操作 |

## 使用场景

### 场景 1：主机端初始化

```csharp
// 扫描场景中的所有战利品箱
var lootBoxes = FindObjectsOfType<LootBoxLoader>();

foreach (var lootBox in lootBoxes)
{
    var setId = GenerateSetId(lootBox);
    lootBoxDb.AddLootBox(
        lootBox.gameObject,
        lootBox.GetComponent<Inventory>(),
        setId
    );
}

Debug.Log($"初始化了 {lootBoxDb.Count} 个战利品箱");
```

### 场景 2：客户端请求战利品箱状态

```csharp
// 客户端发送请求
void Client_RequestLootBoxState(string setId)
{
    var request = new LootOpenRequest
    {
        SetId = setId,
        RequestVersion = 1
    };
    
    JsonMessageRouter.Send(request);
}

// 主机端处理请求
void Host_HandleOpenRequest(LootOpenRequest request)
{
    var lootBox = lootBoxDb.GetLootBox(request.SetId);
    
    if (lootBox == null)
    {
        // 发送错误响应
        return;
    }
    
    // 发送战利品箱状态
    var response = new LootStateResponse
    {
        SetId = lootBox.SetId,
        Capacity = lootBox.Capacity,
        Items = lootBox.Items,
        Timestamp = DateTime.Now
    };
    
    JsonMessageRouter.Send(response);
}
```

### 场景 3：同步附近的战利品箱

```csharp
void SyncNearbyLootBoxes(Vector3 playerPosition)
{
    // 只同步50米内的战利品箱（O(1) 查询）
    var nearbyLootBoxes = lootBoxDb.GetLootBoxesInRadius(playerPosition, 50f);
    
    foreach (var lootBox in nearbyLootBoxes)
    {
        SyncLootBoxToClient(lootBox);
    }
}
```

## 注意事项

1. **GameObject 生命周期**：数据库不会自动监听 GameObject 销毁，需要手动调用 `RemoveLootBox()`

2. **位置更新**：如果战利品箱位置变化，需要调用 `UpdateLootBox()` 更新空间索引

3. **内存管理**：大量战利品箱时注意内存占用，及时清理不需要的数据

4. **线程安全**：当前实现不是线程安全的，仅在主线程使用

5. **Inventory 类型**：`Inventory` 字段使用 `object` 类型，因为它是游戏原生类，使用时需要类型转换

## 与现有系统集成

### 集成到 LootNet

```csharp
public class LootNet
{
    private LootBoxDatabase _lootBoxDb;
    
    public void Initialize()
    {
        _lootBoxDb = new LootBoxDatabase(spatialCellSize: 50f);
        
        if (IsHost)
        {
            Host_InitializeDatabase();
        }
    }
    
    private void Host_InitializeDatabase()
    {
        // 扫描并初始化所有战利品箱
        // ...
    }
}
```

## 调试功能

### 导出数据库到文件

```csharp
// 在 MModUI 中添加调试按钮
public void DebugExportLootBoxDatabase()
{
    var json = lootBoxDb.ExportToJsonWithStats(indented: true);
    var filePath = Path.Combine(
        Application.persistentDataPath,
        $"lootbox_database_{DateTime.Now:yyyyMMdd_HHmmss}.json"
    );
    
    File.WriteAllText(filePath, json);
    Debug.Log($"[DEBUG] 战利品箱数据库已导出到: {filePath}");
}
```

---

*最后更新: 2025-11-11*
