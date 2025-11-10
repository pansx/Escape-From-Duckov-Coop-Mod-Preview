# Main/HostService 模块 API 文档

## 模块概述

HostService 模块负责主机端服务处理，包括玩家死亡处理、装备应用、武器更新等。该模块实现了主机端的特殊逻辑和去抖控制。

## 文件列表

| 文件名 | 说明 |

|--------|------|
|---|---|

| `HostPlayerApply.cs` | 主机玩家应用 |

## 核心类说明

### HostHandle

**主要方法**：

- `Server_HandlePlayerDeadTree` - 处理玩家死亡树

- `Server_HandleHostDeathViaTree` - 处理主机死亡

### HostPlayerApply

**主要方法**：

- `ApplyEquipmentUpdate` - 应用装备更新

- `ApplyWeaponUpdate` - 应用武器更新

- `PlayShootAnimOnServerPeer` - 播放射击动画

**关键机制**：

- 200ms 去抖控制防止重复应用武器

- 缓存枪械和枪口特效

- 使用 UniTask 异步处理
