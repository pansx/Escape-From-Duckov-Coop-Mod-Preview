# PlayerInfoDatabase 调试功能说明

## 概述

已为 `PlayerInfoDatabase` 添加了调试功能，用于验证数据库的基本功能和 CustomData 的存储/读取能力。

## 新增功能

### 1. DebugPrintDatabase()

输出数据库中所有玩家的完整信息到日志。

**输出内容**：
- 总玩家数
- 每个玩家的详细信息：
  - SteamId
  - PlayerName
  - EndPoint
  - IsLocalPlayer
  - LastUpdate
  - LastSeen
  - HasAvatar
  - CustomData（包括 Latency 和 IsInGame）

### 2. DebugTestCustomData()

自动测试 CustomData 功能，验证 Latency 和 IsInGame 的存储和读取。

**测试步骤**：
1. 创建测试玩家
2. 设置 Latency = 50
3. 设置 IsInGame = true
4. 读取并验证数据
5. 清理测试数据

## 使用方法

### 方法一：快捷键（推荐）

在游戏中按下快捷键即可触发调试功能：

- **F9 键**：输出 PlayerInfoDatabase 内容
- **F10 键**：测试 CustomData 功能

### 方法二：代码调用

```csharp
// 输出数据库内容
PlayerInfoDatabase.Instance.DebugPrintDatabase();

// 测试 CustomData 功能
PlayerInfoDatabase.Instance.DebugTestCustomData();
```

## 日志输出示例

### DebugPrintDatabase() 输出示例

```
[PlayerInfoDatabase] ========== 数据库内容 ==========
[PlayerInfoDatabase] 总玩家数: 2
[PlayerInfoDatabase] --- 玩家: Player1 ---
[PlayerInfoDatabase]   SteamId: 76561198012345678
[PlayerInfoDatabase]   EndPoint: 192.168.1.100:9050
[PlayerInfoDatabase]   IsLocalPlayer: True
[PlayerInfoDatabase]   LastUpdate: 2025-01-09 15:30:45
[PlayerInfoDatabase]   LastSeen: 2025-01-09 15:30:45
[PlayerInfoDatabase]   HasAvatar: False
[PlayerInfoDatabase]   CustomData:
[PlayerInfoDatabase]     Latency: 25
[PlayerInfoDatabase]     IsInGame: True
[PlayerInfoDatabase] --- 玩家: Player2 ---
[PlayerInfoDatabase]   SteamId: 76561198087654321
[PlayerInfoDatabase]   EndPoint: 192.168.1.101:9050
[PlayerInfoDatabase]   IsLocalPlayer: False
[PlayerInfoDatabase]   LastUpdate: 2025-01-09 15:30:40
[PlayerInfoDatabase]   LastSeen: 2025-01-09 15:30:40
[PlayerInfoDatabase]   HasAvatar: False
[PlayerInfoDatabase]   CustomData:
[PlayerInfoDatabase]     Latency: 50
[PlayerInfoDatabase]     IsInGame: True
[PlayerInfoDatabase] ========================================
```

### DebugTestCustomData() 输出示例

```
[PlayerInfoDatabase] ========== 测试 CustomData 功能 ==========
[PlayerInfoDatabase] 添加测试玩家: 成功
[PlayerInfoDatabase] 设置 Latency=50: 成功
[PlayerInfoDatabase] 设置 IsInGame=true: 成功
[PlayerInfoDatabase] 读取测试玩家成功
[PlayerInfoDatabase] 读取 Latency: 50 (类型: Int32)
[PlayerInfoDatabase] 读取 IsInGame: True (类型: Boolean)
[PlayerInfoDatabase] 清理测试数据完成
[PlayerInfoDatabase] ==========================================
```

## 验证要点

### 1. 基本功能验证

- ✅ 数据库可以正常添加玩家
- ✅ 数据库可以正常查询玩家
- ✅ 数据库可以正常更新玩家信息

### 2. CustomData 功能验证

- ✅ CustomData 可以存储 int 类型（Latency）
- ✅ CustomData 可以存储 bool 类型（IsInGame）
- ✅ CustomData 可以正确读取存储的值
- ✅ CustomData 的类型信息正确

### 3. 日志输出验证

- ✅ 日志格式清晰易读
- ✅ 所有关键信息都有输出
- ✅ 空值和缺失数据有正确处理

## 测试建议

1. **启动游戏后立即测试**：
   - 按 F10 测试 CustomData 功能
   - 确认所有测试步骤都成功

2. **连接玩家后测试**：
   - 按 F9 查看数据库内容
   - 确认玩家信息正确存储
   - 确认 CustomData 包含 Latency 和 IsInGame

3. **多人游戏测试**：
   - 主机和客户端都按 F9
   - 对比两端的数据库内容
   - 确认数据同步正确

## 注意事项

1. 调试日志会输出到 Unity 日志文件：
   - 位置：`%AppData%\..\LocalLow\Duckov\Escape from Duckov\Player.log`

2. F9 和 F10 快捷键只在游戏运行时有效

3. 测试数据会自动清理，不会影响实际游戏数据

## 下一步

完成验证后，可以继续执行任务 2：在 NetService 中添加数据库更新逻辑。

---

*创建时间：2025-01-09*
*任务：player-ui-database-refactor - 任务 1*
