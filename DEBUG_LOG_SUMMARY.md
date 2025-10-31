# 客机死亡重新进图墓碑为空问题 - 调试日志总结

## 添加的调试日志位置

### 1. LocalPlayerManager.cs
- **Client_EnsureSelfDeathEvent**: 死亡事件处理的完整流程
- **ComputeIsInGame**: 场景状态检测和切换

### 2. CharacterMainControlPatch.cs  
- **Patch_Client_OnDead_ReportCorpseTree**: 客机死亡时上报尸体树的流程

### 3. SendLocalPlayerStatus.cs
- **Net_ReportPlayerDeadTree**: 发送死亡树数据包到服务端

### 4. Mod.cs
- **PLAYER_DEAD_TREE 消息处理**: 服务端接收和处理客机死亡树

### 5. DeadLootBox.cs
- **Server_OnDeadLootboxSpawned**: 服务端创建墓碑的完整流程
- **SpawnDeadLootboxAt**: 客机端生成墓碑的完整流程

## 关键调试点

### 死亡状态管理
- `_cliSelfDeathFired`: 是否已触发死亡
- `_cliCorpseTreeReported`: 是否已上报尸体树  
- `_cliInEnsureSelfDeathEmit`: 是否在死亡事件发送中

### 墓碑创建流程
- 服务端: whoDied参数 -> aiId计算 -> 物品状态设置 -> 网络广播
- 客机端: 接收DEAD_LOOT_SPAWN -> 创建墓碑 -> 缓存处理 -> 状态请求

### 场景切换检测
- ComputeIsInGame: 检测玩家是否在游戏中
- 场景ID计算和比较

## 预期调试输出

运行游戏后，在客机死亡重新进图的过程中，应该能看到：

1. **死亡时**: 
   - `[DEATH-DEBUG] Client_EnsureSelfDeathEvent called`
   - `[DEATH-DEBUG] Patch_Client_OnDead_ReportCorpseTree called`
   - `[DEATH-DEBUG] Net_ReportPlayerDeadTree called`

2. **服务端处理**:
   - `[DEATH-DEBUG] Received PLAYER_DEAD_TREE from peer`
   - `[DEATH-DEBUG] Server_OnDeadLootboxSpawned called`
   - `[DEATH-DEBUG] Broadcasting DEAD_LOOT_SPAWN`

3. **客机重新进图**:
   - `[DEATH-DEBUG] ComputeIsInGame called`
   - `[DEATH-DEBUG] SpawnDeadLootboxAt called`
   - `[DEATH-DEBUG] Found pending loot state` 或 `[DEATH-DEBUG] No cached state found`

## 问题验证点

通过这些日志，我们可以验证：

1. **状态重置问题**: 客机重新进图时，死亡相关标记是否正确重置
2. **墓碑内容同步**: 服务端创建的墓碑是否包含正确的物品
3. **缓存机制**: 客机端是否正确接收和应用墓碑状态
4. **时序问题**: 各个步骤的执行顺序是否正确

## 使用方法

1. 编译并运行修改后的模组
2. 进行客机死亡重新进图的测试
3. 查看控制台输出，搜索 `[DEATH-DEBUG]` 标签
4. 分析日志输出，定位问题所在

## 注意事项

- 这些调试日志会产生大量输出，仅用于问题诊断
- 问题解决后应移除或注释掉这些调试代码
- 日志中包含敏感的游戏状态信息，注意保护隐私