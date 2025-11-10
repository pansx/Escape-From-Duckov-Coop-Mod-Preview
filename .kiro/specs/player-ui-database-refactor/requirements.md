# 需求文档

## 简介

重构玩家状态UI的玩家信息渲染逻辑，使其严格基于 `PlayerInfoDatabase` 数据库中的数据进行渲染，替代当前基于 `PlayerStatus` 和多种数据源的复杂逻辑。

## 术语表

- **PlayerInfoDatabase**: 玩家信息数据库，存储所有玩家的 Steam 信息、网络信息和元数据
- **PlayerInfoEntity**: 玩家信息实体，包含 SteamId、PlayerName、EndPoint 等字段
- **MModUI**: 联机模组的主UI类，负责渲染玩家列表和状态信息
- **PlayerStatus**: 旧的玩家状态数据结构（将被弃用作为渲染数据源）
- **UI渲染**: 在玩家状态面板中显示玩家信息的过程

## 需求

### 需求 1: 数据库驱动的玩家列表渲染

**用户故事**: 作为开发者，我希望玩家状态UI完全基于 PlayerInfoDatabase 渲染，以便统一数据源并简化逻辑。

#### 验收标准

1. WHEN 玩家状态面板打开时，THE MModUI SHALL 从 PlayerInfoDatabase 获取所有玩家信息
2. THE MModUI SHALL 使用 PlayerInfoEntity 的字段渲染玩家卡片，包括 PlayerName、SteamId、EndPoint
3. THE MModUI SHALL 显示 PlayerInfoEntity 中的 AvatarTexture（如果可用）
4. THE MModUI SHALL 根据 PlayerInfoEntity.IsLocalPlayer 标识本地玩家
5. WHERE PlayerInfoEntity.CustomData 包含额外信息，THE MModUI SHALL 显示相关自定义数据

### 需求 2: 移除旧的数据源依赖和模式区分

**用户故事**: 作为开发者，我希望移除对 PlayerStatus、clientPlayerStatuses、playerStatuses 等旧数据源的依赖，并移除 Steam 模式和直连模式的渲染区分，以便简化代码维护。

#### 验收标准

1. THE UpdatePlayerList 方法 SHALL NOT 访问 playerStatuses 字典
2. THE UpdatePlayerList 方法 SHALL NOT 访问 clientPlayerStatuses 字典
3. THE UpdatePlayerList 方法 SHALL NOT 访问 localPlayerStatus 对象
4. THE UpdatePlayerList 方法 SHALL NOT 检查 TransportMode（Steam 或 Direct）
5. THE UpdatePlayerList 方法 SHALL NOT 使用不同的逻辑处理 Steam 模式和直连模式
6. THE CreatePlayerEntry 方法 SHALL 接收 PlayerInfoEntity 参数而非 PlayerStatus
7. THE MModUI SHALL 移除所有与 Steam API 直接交互的玩家名称获取逻辑
8. THE MModUI SHALL 移除 GetSteamIdFromStatus 方法的调用
9. THE MModUI SHALL 移除 SteamEndPointMapper、SteamLobbyManager 在渲染逻辑中的使用

### 需求 3: 统一的玩家信息显示

**用户故事**: 作为玩家，我希望看到清晰一致的玩家信息显示，无论是 Steam 模式还是直连模式，显示格式都应该相同。

#### 验收标准

1. THE 玩家卡片 SHALL 显示 PlayerInfoEntity.PlayerName 作为主要名称
2. THE 玩家卡片 SHALL 显示 PlayerInfoEntity.SteamId 作为唯一标识
3. THE 玩家卡片 SHALL 显示 PlayerInfoEntity.EndPoint 作为网络地址
4. WHERE PlayerInfoEntity.AvatarTexture 存在，THE 玩家卡片 SHALL 显示头像图片
5. THE 玩家卡片 SHALL 使用 PlayerInfoEntity.LastSeen 显示最后在线时间
6. THE 玩家卡片 SHALL 使用相同的布局和样式，无论传输模式如何
7. THE 玩家卡片 SHALL NOT 根据 TransportMode 显示不同的前缀（如 "HOST_" 或 "CLIENT_"）
8. THE 玩家卡片 SHALL NOT 包含任何特定于传输模式的条件渲染逻辑

### 需求 4: 延迟和状态信息的集成

**用户故事**: 作为玩家，我希望看到其他玩家的延迟和游戏状态信息。

#### 验收标准

1. WHERE PlayerInfoEntity.CustomData 包含 "Latency" 键，THE 玩家卡片 SHALL 显示延迟值
2. WHERE PlayerInfoEntity.CustomData 包含 "IsInGame" 键，THE 玩家卡片 SHALL 显示游戏状态
3. THE MModUI SHALL 使用颜色编码显示延迟等级（绿色<50ms，黄色<100ms，红色>=100ms）
4. WHERE 延迟或状态信息不可用，THE 玩家卡片 SHALL 显示 "未知" 或默认值
5. THE UpdatePlayerPingDisplays 方法 SHALL 从 PlayerInfoDatabase.CustomData 读取延迟信息

### 需求 5: 数据库更新机制

**用户故事**: 作为开发者，我希望确保 PlayerInfoDatabase 在网络事件发生时及时更新，以便UI显示最新信息。

#### 验收标准

1. WHEN 收到 ClientStatusMessage 时，THE NetService SHALL 更新 PlayerInfoDatabase 中的对应玩家信息
2. WHEN 玩家连接时，THE NetService SHALL 在 PlayerInfoDatabase 中创建新的 PlayerInfoEntity
3. WHEN 玩家断开连接时，THE NetService SHALL 更新 PlayerInfoEntity.LastSeen 时间戳
4. THE NetService SHALL 定期将 PlayerStatus 中的延迟和状态信息同步到 PlayerInfoDatabase.CustomData
5. WHERE Steam 信息可用，THE NetService SHALL 更新 PlayerInfoEntity 的 SteamAvatarUrl 和 AvatarTexture

### 需求 6: 向后兼容性

**用户故事**: 作为开发者，我希望在重构期间保持其他功能正常运行，不影响投票、踢人等功能。

#### 验收标准

1. THE 投票面板 SHALL 继续正常显示玩家列表
2. THE 踢人功能 SHALL 能够通过 SteamId 正确识别玩家
3. THE 观战功能 SHALL 不受玩家列表渲染变更的影响
4. THE 主机列表 SHALL 继续正常工作
5. THE Steam Lobby 集成 SHALL 不受影响
