# 战利品缓存清理修复

## 问题描述
进入新关卡时，旧的战利品缓存没有被清理，特别是在Base_SceneV2（出基地）时。这导致：
- 手动连接时跳出几百个同步战利品箱子的请求
- 旧关卡的战利品数据残留在新关卡中
- 客户端和服务端的战利品状态不一致

## 解决方案
在 `LevelManagerPatch.cs` 中添加了场景切换时的战利品缓存清理逻辑：

### 客户端清理（ClearLootCacheOnSceneChange）
在场景初始化前（`StartInit` Prefix）执行：
1. 检测是否是出基地场景（Base_SceneV2）或进入新关卡（Level_开头）
2. 清理客户端战利品缓存：
   - `_cliLootByUid` - 客户端战利品UID映射
   - `_cliPendingReorder` - 待处理重排序请求
   - `_cliPendingTake` - 待处理拾取请求
   - `_pendingLootStatesByUid` - 待处理战利品状态
3. 清理静态字典：
   - `InteractableLootbox.Inventories`
   - `LevelManager.LootBoxInventories`

### 服务端清理（ClearServerLootCacheOnSceneVote）
在场景投票开始时（`OnPointerClick` Prefix）执行：
1. 检测是否是离开基地或进入新关卡
2. 智能清理服务端战利品缓存：
   - 保留墓碑数据（没有LootBoxLoader组件的战利品箱）
   - 清理普通战利品缓存（有LootBoxLoader组件的战利品箱）
   - 清理服务端战利品静音表 `_srvLootMuteUntil`

## 关键特性
1. **智能识别**：区分墓碑和普通战利品，只清理必要的缓存
2. **双端同步**：客户端和服务端都进行相应的清理
3. **时机准确**：在场景切换的关键节点执行清理
4. **安全防护**：包含异常处理，避免清理过程中的错误影响游戏

## 修改文件
- `EscapeFromDuckovCoopMod/Patch/Scene/LevelManagerPatch.cs`

## 预期效果
- 进入新关卡时不再有旧战利品数据残留
- 手动连接时不再出现大量重复的同步请求
- 客户端和服务端的战利品状态保持一致
- 墓碑数据得到正确保留