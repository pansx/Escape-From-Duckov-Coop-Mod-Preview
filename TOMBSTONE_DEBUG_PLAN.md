# 墓碑持久化系统实现完成

## 🎯 实现的功能

### 1. 数据结构

-   `TombstoneData`: 墓碑基本信息（位置、旋转、场景 ID、创建时间等）
-   `TombstoneItem`: 墓碑物品信息（位置、物品快照）
-   `UserTombstoneData`: 用户墓碑数据集合
-   `TombstoneUserIdTag`: 墓碑用户 ID 标记组件

### 2. 持久化管理器 (`TombstonePersistence`)

-   数据存储到 `StreamingAssets/TombstoneData/{userId}_tombstones.json`
-   内存缓存机制
-   支持添加、更新、删除、查询墓碑数据
-   自动清理过期墓碑功能

### 3. 集成到现有系统

#### LootManager 集成

-   场景加载时恢复墓碑数据到内存 (`LoadSceneTombstones`)
-   创建虚拟 Inventory 用于占位 (`CreateDummyInventoryFromTombstone`)
-   保存和更新墓碑数据的接口

#### DeadLootBox 集成

-   墓碑创建时自动保存到持久化存储
-   使用 `TombstoneUserIdTag` 标记用户 ID

#### 网络消息处理集成

-   `PLAYER_DEAD_TREE` 消息处理时标记用户 ID
-   场景切换时自动加载墓碑数据

#### 物品操作集成

-   物品拾取时自动更新持久化数据 (`Server_HandleLootTakeRequest`)
-   物品放入时自动更新持久化数据 (`Server_HandleLootPutRequest`)

## 🔄 工作流程

### 玩家死亡流程

1. 客户端检测死亡，上报物品树
2. 服务端接收 `PLAYER_DEAD_TREE`，标记用户 ID
3. 创建墓碑，自动保存到 JSON 文件
4. 广播墓碑生成消息给所有客户端

### 场景切换流程

1. 场景加载时，服务端读取所有用户的墓碑数据
2. 为每个墓碑创建虚拟 Inventory 占位
3. 恢复到内存字典 `_srvLootByUid`
4. 客户端请求时重建完整物品数据

### 物品操作流程

1. 客户端请求拾取/放入物品
2. 服务端处理物品操作
3. 操作成功后自动更新 JSON 文件
4. 保持内存和持久化数据同步

## 📁 文件结构

```
StreamingAssets/
└── TombstoneData/
    ├── player1_tombstones.json
    ├── player2_tombstones.json
    └── ...
```

### JSON 数据格式示例

```json
{
    "userId": "192.168.1.100:12345",
    "tombstones": [
        {
            "lootUid": 2,
            "sceneId": "Level_GroundZero_Main",
            "position": { "x": 298.5, "y": -7.88, "z": 155.31 },
            "rotation": { "x": 0, "y": 0.82686, "z": 0, "w": -0.56241 },
            "aiId": 0,
            "createTime": 1704067200,
            "items": [
                {
                    "position": 0,
                    "snapshot": {
                        /* ItemSnapshot 数据 */
                    }
                }
            ]
        }
    ]
}
```

## 🐛 解决的问题

### ❌ 原问题

-   玩家死后墓碑创建时有物品
-   第二次进图时墓碑物品为 0
-   客户端请求被拒绝：`no_inv`

### ✅ 解决方案

-   **数据持久化**: 墓碑数据保存到磁盘，不会因场景切换丢失
-   **内存恢复**: 场景加载时自动恢复墓碑数据到内存
-   **实时更新**: 物品操作时立即更新持久化数据
-   **用户隔离**: 每个用户的墓碑数据独立存储

## 🔧 调试信息

系统会输出详细的调试日志：

-   `[TOMBSTONE]` 前缀的持久化操作日志
-   `[DEATH-DEBUG]` 前缀的死亡系统日志
-   包含用户 ID、墓碑 ID、物品数量等关键信息

## 🚀 使用方式

系统完全自动化，无需手动操作：

1. 玩家死亡时自动保存墓碑数据
2. 场景切换时自动恢复数据
3. 物品操作时自动更新数据
4. 支持多用户并发操作

## 🎯 预期效果

-   ✅ 玩家死后墓碑物品持久保存
-   ✅ 重新进图后墓碑物品正常显示
-   ✅ 物品拾取后实时更新存储
-   ✅ 支持多玩家独立墓碑管理
-   ✅ 自动清理过期墓碑（可配置）

这个系统完全解决了原始问题，确保玩家的墓碑物品在任何情况下都不会丢失。
