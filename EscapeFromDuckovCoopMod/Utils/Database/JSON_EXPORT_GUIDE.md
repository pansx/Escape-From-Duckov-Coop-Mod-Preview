# JSON 导出功能快速指南

## 概述

数据库支持高效的 JSON 导出功能，可以将整库或部分数据导出为 JSON 格式，方便调试、分析和数据持久化。

## 性能特点

- ⚡ **高效序列化**：使用 Newtonsoft.Json 优化序列化
- 📦 **灵活导出**：支持整库、按类型、按场景、按范围导出
- 🎯 **选择性导出**：支持自定义条件过滤
- 📊 **统计信息**：可选包含数据统计信息
- 💾 **文件保存**：支持直接保存到文件

## 基础用法

### 1. 导出整库

```csharp
var itemDb = new GameItemDatabase();

// 简单导出（紧凑格式）
var json = itemDb.ExportToJson(indented: false);

// 格式化导出（易读格式）
var json = itemDb.ExportToJson(indented: true);

// 输出到日志
Debug.Log(json);
```

### 2. 导出带统计信息

```csharp
// 包含导出时间、总数、按类型/场景统计
var jsonWithStats = itemDb.ExportToJsonWithStats(indented: true);
Debug.Log(jsonWithStats);
```

**输出示例**：
```json
{
  "ExportTime": "2025-11-11 01:06:00",
  "TotalCount": 10000,
  "Statistics": {
    "ByType": {
      "LootBox": 1667,
      "Weapon": 1666,
      "Ammo": 1667
    },
    "ByScene": {
      "MainScene": 10000
    }
  },
  "Items": [...]
}
```

## 选择性导出

### 按类型导出

```csharp
// 只导出战利品箱
var lootBoxJson = itemDb.ExportByTypeToJson("LootBox", indented: true);

// 只导出武器
var weaponJson = itemDb.ExportByTypeToJson("Weapon", indented: true);
```

### 按场景导出

```csharp
// 只导出主场景的物品
var sceneJson = itemDb.ExportBySceneToJson("MainScene", indented: true);

// 只导出特定地图的物品
var mapJson = itemDb.ExportBySceneToJson("Factory", indented: true);
```

### 按范围导出

```csharp
// 导出玩家周围50米内的物品
var radiusJson = itemDb.ExportInRadiusToJson(
    playerPosition, 
    radius: 50f, 
    indented: true
);
```

**输出示例**：
```json
{
  "Center": { "x": 0.0, "y": 0.0, "z": 0.0 },
  "Radius": 50.0,
  "Count": 123,
  "Items": [
    {
      "SetId": "item_001",
      "ItemType": "LootBox",
      "Distance": 12.34,
      "Position": { "x": 10.0, "y": 0.0, "z": 5.0 }
    }
  ]
}
```

### 自定义条件导出

```csharp
// 导出特定所有者的武器
var customJson = itemDb.ExportToJson(
    e => e.ItemType == "Weapon" && e.OwnerId == 1,
    indented: true
);

// 导出最近创建的物品
var recentJson = itemDb.ExportToJson(
    e => (DateTime.Now - e.CreatedAt).TotalSeconds < 60,
    indented: true
);

// 导出特定区域的特定类型
var complexJson = itemDb.ExportToJson(
    e => e.ItemType == "LootBox" 
      && e.Position.y > 0 
      && e.SceneName == "MainScene",
    indented: true
);
```

## 保存到文件

### 保存到持久化目录

```csharp
var json = itemDb.ExportToJsonWithStats(indented: true);

// 生成带时间戳的文件名
var fileName = $"item_database_{DateTime.Now:yyyyMMdd_HHmmss}.json";
var filePath = Path.Combine(Application.persistentDataPath, fileName);

// 保存文件
File.WriteAllText(filePath, json);
Debug.Log($"数据库已导出到: {filePath}");
```

**文件路径示例**：
- Windows: `C:/Users/[用户名]/AppData/LocalLow/Duckov/Escape from Duckov/item_database_20251111_010600.json`
- Linux: `~/.config/unity3d/Duckov/Escape from Duckov/item_database_20251111_010600.json`

### 保存到自定义目录

```csharp
var json = itemDb.ExportToJsonWithStats(indented: true);
var filePath = @"C:\Temp\database_export.json";

File.WriteAllText(filePath, json);
Debug.Log($"数据库已导出到: {filePath}");
```

## JSON 格式说明

### 标准格式

```json
[
  {
    "SetId": "item_001",
    "ItemType": "LootBox",
    "SceneName": "MainScene",
    "OwnerId": 1,
    "Position": {
      "x": 123.45,
      "y": 0.0,
      "z": 67.89
    },
    "GameObjectName": "LootBox_001",
    "Active": true,
    "CreatedAt": "2025-11-11 01:06:00",
    "CustomData": {
      "LastSyncTime": "2025-11-11 01:05:55",
      "IsSynced": true
    }
  }
]
```

### 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| SetId | string | 物品唯一标识符 |
| ItemType | string | 物品类型（LootBox, Weapon 等） |
| SceneName | string | 所在场景名称 |
| OwnerId | int | 所有者 ID（-1 表示无主） |
| Position | object | 3D 坐标 (x, y, z) |
| GameObjectName | string | Unity GameObject 名称 |
| Active | bool | GameObject 是否激活 |
| CreatedAt | string | 创建时间 |
| CustomData | object | 自定义数据字典 |

## 性能优化建议

### 1. 选择合适的格式

```csharp
// ✅ 生产环境：紧凑格式（更小的文件）
var json = itemDb.ExportToJson(indented: false);

// ✅ 调试环境：格式化（易读）
var json = itemDb.ExportToJson(indented: true);
```

### 2. 避免导出大量数据

```csharp
// ❌ 慢：导出整库（10000+ 物品）
var json = itemDb.ExportToJson();

// ✅ 快：只导出需要的部分
var json = itemDb.ExportByTypeToJson("LootBox");
var json = itemDb.ExportInRadiusToJson(playerPos, 50f);
```

### 3. 异步导出大数据

```csharp
IEnumerator ExportLargeDatabase()
{
    Debug.Log("开始导出...");
    
    // 在后台线程执行
    var json = "";
    var task = Task.Run(() => itemDb.ExportToJsonWithStats(indented: false));
    
    while (!task.IsCompleted)
        yield return null;
    
    json = task.Result;
    
    // 保存文件
    File.WriteAllText("export.json", json);
    Debug.Log("导出完成！");
}

// 使用
StartCoroutine(ExportLargeDatabase());
```

## 实际应用场景

### 场景1：调试物品同步问题

```csharp
// 导出当前场景所有物品，检查同步状态
var json = itemDb.ExportBySceneToJson(SceneManager.GetActiveScene().name, true);
Debug.Log("当前场景物品:\n" + json);
```

### 场景2：分析物品分布

```csharp
// 导出带统计信息，分析物品类型分布
var json = itemDb.ExportToJsonWithStats(true);
File.WriteAllText("item_distribution.json", json);
```

### 场景3：备份玩家附近物品

```csharp
// 定期备份玩家周围物品
void BackupNearbyItems()
{
    var json = itemDb.ExportInRadiusToJson(playerPosition, 100f, false);
    var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
    File.WriteAllText(fileName, json);
}
```

### 场景4：导出特定类型统计

```csharp
// 导出所有战利品箱，分析掉落位置
var lootBoxes = itemDb.ExportByTypeToJson("LootBox", true);
File.WriteAllText("lootbox_analysis.json", lootBoxes);
```

## 常见问题

### Q: 导出 10000 条数据需要多久？

A: 约 50-100ms（取决于数据复杂度和格式化选项）

### Q: JSON 文件太大怎么办？

A: 
1. 使用 `indented: false` 减小文件大小（约减少 30-40%）
2. 使用选择性导出（按类型、场景、范围）
3. 使用 gzip 压缩文件

### Q: 如何压缩 JSON 文件？

```csharp
using System.IO.Compression;

void ExportCompressed()
{
    var json = itemDb.ExportToJson(indented: false);
    var bytes = Encoding.UTF8.GetBytes(json);
    
    using (var fs = File.Create("export.json.gz"))
    using (var gz = new GZipStream(fs, CompressionMode.Compress))
    {
        gz.Write(bytes, 0, bytes.Length);
    }
    
    Debug.Log("压缩导出完成！");
}
```

### Q: 可以导出为其他格式吗？

A: 可以，JSON 导出后可以转换为：
- CSV（使用 JSON 转 CSV 工具）
- XML（使用 JsonConvert.DeserializeObject + XmlSerializer）
- SQLite（使用 JSON 导入 SQLite）

---

*最后更新: 2025-11-11*
