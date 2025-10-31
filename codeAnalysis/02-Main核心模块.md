# Main 核心模块分析

## 模块概览

Main目录是整个联机模组的核心，包含了所有主要的管理器、服务类和工具类。该模块负责协调各个子系统的工作，处理网络通信，管理游戏状态同步。

## 核心管理器类

### 1. COOPManager.cs - 总管理器
**职责**: 整个模组的中央管理器，负责初始化和协调所有子系统

**主要功能**:
- 初始化所有子系统管理器
- 装备模型同步（护甲、头盔、背包、面罩等）
- 武器模型同步和管理
- 手雷预制体管理
- Buff系统集成
- 远程玩家血条管理

**关键方法**:
- `InitManager()`: 初始化所有子系统
- `ChangeArmorModel()`, `ChangeHelmatModel()`: 装备外观同步
- `ChangeWeaponModel()`: 武器外观同步
- `GetItemAsync()`: 异步获取物品实例
- `ResolveBuffAsync()`: 解析Buff效果

### 2. NetService.cs - 网络服务核心
**职责**: 网络通信的核心服务，处理连接管理和消息分发

**主要功能**:
- 网络连接管理（主机/客户端模式）
- 玩家状态管理
- 自动重连机制
- 场景切换后的连接恢复
- 网络发现和广播

**关键特性**:
- 67Hz高频同步 (`syncInterval = 0.015f`)
- 主机-客户端架构
- 自动重连和连接缓存
- 防抖机制防止频繁重连

**连接管理**:
```csharp
// 手动连接（会更新缓存）
public void ConnectToHost(string ip, int port)

// 自动重连（不更新缓存）
private void AutoReconnectToHost(string ip, int port)

// 场景切换后重连
public async UniTask ReconnectAfterSceneLoad()
```

### 3. Op.cs - 网络操作码
**职责**: 定义所有网络消息类型的枚举

**消息分类**:
- **玩家同步**: `PLAYER_STATUS_UPDATE`, `POSITION_UPDATE`, `ANIM_SYNC`
- **装备同步**: `EQUIPMENT_UPDATE`, `PLAYERWEAPON_UPDATE`
- **战斗系统**: `FIRE_REQUEST`, `FIRE_EVENT`, `MELEE_ATTACK_REQUEST`
- **物品系统**: `ITEM_DROP_REQUEST`, `ITEM_SPAWN`, `ITEM_PICKUP_REQUEST`
- **血量系统**: `PLAYER_HEALTH_REPORT`, `AUTH_HEALTH_SELF`, `AUTH_HEALTH_REMOTE`
- **场景管理**: `SCENE_VOTE_START`, `SCENE_READY_SET`, `SCENE_BEGIN_LOAD`
- **AI同步**: `AI_TRANSFORM_SNAPSHOT`, `AI_HEALTH_SYNC`, `AI_ATTACK_SWING`
- **环境同步**: `DOOR_REQ_SET`, `ENV_HURT_REQUEST`, `LOOT_REQ_OPEN`

## 工具类

### 1. CoopTool.cs - 通用工具类
**职责**: 提供各种通用的工具方法和缓存管理

**主要功能**:
- 网络消息发送封装
- 武器插槽管理
- 射击动画播放
- 血量状态缓存和应用
- 玩家ID构建和管理

**缓存系统**:
- `_cliPendingRemoteHp`: 远程玩家血量缓存
- `_cliPendingProxyBuffs`: 待应用的Buff缓存
- 武器预制体缓存

### 2. CustomFace.cs - 自定义外观管理
**职责**: 处理玩家自定义外观的同步

**主要功能**:
- 外观数据的序列化/反序列化
- 远程玩家外观应用
- 外观缓存管理
- 本地外观数据加载

**关键方法**:
- `Client_ApplyFaceIfAvailable()`: 应用远程玩家外观
- `LoadLocalCustomFaceJson()`: 加载本地外观配置
- `HardApplyCustomFace()`: 强制应用外观设置

### 3. FxManager.cs - 特效管理器
**职责**: 管理游戏中的视觉和音效特效

**主要功能**:
- 枪口火光特效池化管理
- 抛壳特效同步
- 武器后坐力视觉效果
- 近战攻击特效
- AI死亡特效和音效

**性能优化**:
- 特效对象池化，减少GC压力
- 缓存机制避免重复查找组件
- 临时特效自动销毁

### 4. HarmonyFix.cs - Harmony补丁集合
**职责**: 包含各种Harmony补丁，修改游戏原有行为

**主要补丁**:
- **近战攻击重定向**: 客户端近战攻击上报给主机处理
- **AI伤害拦截**: 防止客户端AI互相伤害
- **距离激活修正**: 考虑所有在线玩家的距离
- **暂停菜单处理**: 联机模式下的暂停逻辑
- **观战系统**: 死亡后的观战功能

**关键补丁类**:
- `Patch_ClientReportMeleeHit`: 近战攻击上报
- `Patch_SABPD_FixedUpdate_AllPlayersUnion`: 多玩家距离计算
- `Patch_ClosureView_ShowAndReturnTask_SpectatorGate`: 观战门控

### 5. PublicHandleUpdate.cs - 公共消息处理器
**职责**: 处理各种网络消息的公共逻辑

**主要功能**:
- 装备更新消息处理和转发
- 武器更新消息处理和转发
- 动画状态同步处理
- 位置更新消息转发

## 子模块目录结构

### AI/ - AI同步系统
- `AIHandle.cs`: AI行为同步管理
- `AIHealth.cs`: AI血量同步
- `AIName.cs`: AI名称和图标管理
- `AIRequest.cs`: AI相关请求处理
- `AITool.cs`: AI工具类

### ClientService/ - 客户端服务
- `ClientHandle.cs`: 客户端消息处理
- `ClientPlayerApply.cs`: 客户端玩家状态应用
- `SnedClientStatus.cs`: 客户端状态发送

### HostService/ - 主机服务
- `HostHandle.cs`: 主机消息处理
- `HostPlayerApply.cs`: 主机玩家状态应用

### Health/ - 血量系统
- `Buff.cs`: Buff效果管理
- `HealthM.cs`: 血量管理器
- `HealthTool.cs`: 血量工具类
- `HurtM.cs`: 伤害处理管理

### Item/ - 物品系统
- `ItemHandle.cs`: 物品同步处理
- `ItemRequest.cs`: 物品请求管理
- `ItemTool.cs`: 物品工具类

### Weapon/ - 武器系统
- `WeaponHandle.cs`: 武器同步处理
- `WeaponRequest.cs`: 武器请求管理
- `WeaponTool.cs`: 武器工具类
- `GrenadeM.cs`: 手雷管理

### SceneService/ - 场景服务
- `SceneNet.cs`: 场景网络同步
- `SceneM.cs`: 场景管理器
- `LootNet.cs`: 战利品网络同步
- `Door.cs`: 门状态同步
- `Destructible.cs`: 可破坏物同步

### LocalPlayer/ - 本地玩家
- `LocalPlayerManager.cs`: 本地玩家管理
- `SendLocalPlayerStatus.cs`: 本地状态发送
- `Spectator.cs`: 观战系统

### UI/ - 用户界面
- `ModUI.cs`: 模组UI管理

### Localization/ - 多语言
- `LocalizationManager.cs`: 本地化管理器

### WeatherAndTime/ - 天气时间
- `Weather.cs`: 天气同步

## 架构特点

### 1. 模块化设计
每个功能都有独立的管理器类，通过COOPManager统一初始化和协调。

### 2. 异步编程
大量使用UniTask进行异步操作，避免阻塞主线程。

### 3. 缓存机制
各种缓存系统减少重复计算和网络查询，提升性能。

### 4. 容错设计
大量的try-catch块和null检查，确保网络异常不会导致游戏崩溃。

### 5. 性能优化
- 对象池化管理特效
- 智能的组件缓存
- 高频数据的压缩传输

这个Main模块展现了一个成熟的多人游戏同步系统的设计思路，通过清晰的职责分离和完善的错误处理，实现了稳定可靠的联机功能。