# 客户端死亡物品保留功能实现

## 概述

本功能通过5个关键的Harmony补丁，防止客户端玩家在死亡时丢失物品，同时保持观战模式和其他游戏功能的正常运行。

## 实现的补丁

### 1. PreventClientPlayerDropAllItemsPatch
- **目标方法**: `CharacterMainControl.DropAllItems`
- **功能**: 阻止客户端玩家掉落所有物品
- **作用范围**: 仅客户端本地玩家

### 2. PreventClientPlayerDestroyAllItemsPatch
- **目标方法**: `CharacterMainControl.DestroyAllItem`
- **功能**: 阻止客户端玩家销毁所有物品
- **作用范围**: 仅客户端本地玩家

### 3. PreventClientPlayerOnDeadPatch ⭐ 核心补丁
- **目标方法**: `CharacterMainControl.OnDead`
- **功能**: 阻止原始死亡逻辑执行，手动触发观战模式
- **作用范围**: 仅客户端本地玩家
- **特殊处理**: 手动调用 `Spectator.Instance.TryEnterSpectatorOnDeath(dmgInfo)`

### 4. PreventClientEnsureSelfDeathEventPatch ⭐ 关键补丁
- **目标方法**: `LoaclPlayerManager.Client_EnsureSelfDeathEvent`
- **功能**: 阻止联机模组的死亡事件补发机制
- **作用范围**: 仅客户端本地玩家
- **重要性**: 这是联机模组触发死亡的另一个重要路径

### 5. PreventTombSpawnPatch
- **目标方法**: `LevelConfig.get_SpawnTomb`
- **功能**: 阻止墓碑生成，防止物品被转移到墓碑中
- **作用范围**: 联机模式下全局
- **参考**: NoDeathDrops模组的实现

## 技术特点

### 多层防护
- 通过拦截5个不同的方法，确保物品不会通过任何路径被清空
- 从根源上阻止死亡逻辑执行，而不是仅仅阻止物品操作

### 精确作用域
- 只影响客户端的本地玩家
- 服务端和其他角色的死亡逻辑完全不受影响
- 保持多人游戏的平衡性

### 功能完整性
- 保持观战模式正常工作
- 保持死亡状态的视觉效果
- 不影响其他游戏机制

## 调试日志

每个补丁都包含详细的调试日志，便于问题排查：
- `[COOP] 阻止客户端玩家死亡时掉落所有物品`
- `[COOP] 阻止客户端玩家死亡时销毁所有物品`
- `[COOP] 阻止客户端玩家OnDead执行，保留物品`
- `[COOP] 阻止客户端死亡事件补发，保留物品`
- `[COOP] 阻止生成墓碑，防止物品被转移`

## 兼容性

- ✅ 与现有联机功能完全兼容
- ✅ 不影响服务端逻辑
- ✅ 不影响AI和其他玩家
- ✅ 保持观战模式功能
- ✅ 参考成熟的NoDeathDrops模组实现

## 使用场景

此功能主要解决联机模式下客户端玩家死亡时物品意外丢失的问题，确保玩家在死亡后能够保留所有装备和物品，提升多人游戏体验。