# Main 模块总览

## 模块概述

Main 模块是 Escape From Duckov Coop Mod 的核心业务逻辑层，负责联机游戏的主要功能实现。该模块包含12个子模块和8个根级文件，涵盖了玩家管理、AI 同步、物品系统、场景服务、武器系统等核心功能。

**核心职责**：

- 统一管理所有联机功能的初始化和协调

- 提供网络服务的核心接口和工具类

- 实现玩家、AI、物品、场景等核心系统的同步

- 管理游戏状态和网络通信协议

**技术特点**：

- 采用服务器权威架构，确保游戏状态一致性

- 使用 LiteNetLib 进行高效网络通信

- 支持 Steam P2P 和直连两种网络模式

- 实现了完善的去抖、节流和幂等性控制

- 使用 UniTask 进行异步操作，避免阻塞主线程

## 项目结构

```

Main/
├── 根级文件（8个）
│   ├── COOPManager.cs          # 联机管理器，统一初始化和资源管理
│   ├── CoopTool.cs             # 联机工具类，提供各种辅助方法
│   ├── CustomFace.cs           # 自定义外观管理
│   ├── FxManager.cs            # 特效管理器
│   ├── HarmonyFix.cs           # Harmony 补丁修复
│   ├── NetService.cs           # 网络服务核心类
│   ├── Op.cs                   # 网络操作码定义
│   └── PublicHandleUpdate.cs  # 公共更新处理器
│
└── 子模块（12个）
    ├── AI/                     # AI 同步系统
    ├── ClientService/          # 客户端服务
    ├── Health/                 # 生命值系统
    ├── HostService/            # 主机服务
    ├── Item/                   # 物品系统
    ├── Loader/                 # 模组加载器
    ├── Localization/           # 本地化
    ├── LocalPlayer/            # 本地玩家管理
    ├── SceneService/           # 场景服务
    ├── UI/                     # 用户界面
    ├── Weapon/                 # 武器系统
    └── WeatherAndTime/         # 天气时间同步

```


## 根级文件详解

### 1. COOPManager.cs

**功能**：联机管理器，负责统一初始化和管理所有联机功能模块

**核心职责**：

- 初始化所有子系统（AI、物品、武器、场景等）

- 提供装备模型切换方法（护甲、头盔、面罩、背包等）

- 管理手雷预制体缓存

- 提供 Buff 解析和应用

- 管理远程玩家血条显示

**关键字段**：

```csharp
// 各子系统实例
public static HostPlayerApply HostPlayer_Apply;
public static ClientPlayerApply ClientPlayer_Apply;
public static LootNet LootNet;
public static AIHandle AIHandle;
public static Door Door;
public static WeaponHandle WeaponHandle;
public static ClientHandle ClientHandle;
// ... 等18个子系统

```

**主要方法**：

- `InitManager()` - 初始化所有子系统

- `GetItemAsync(int itemID)` - 异步获取物品实例

- `ChangeArmorModel/ChangeHelmatModel/...` - 切换装备模型

- `ChangeWeaponModel()` - 切换武器模型（支持多插槽）

- `GetGrenadePrefabByItemIdAsync()` - 获取手雷预制体

- `ResolveBuffAsync()` - 解析Buff预制体

- `EnsureRemotePlayersHaveHealthBar()` - 确保远程玩家有血条

- `StripAllHandItems()` - 清除所有手持物品

**设计模式**：

- 静态单例模式 - 所有子系统通过静态字段访问

- 工厂模式 - 提供物品和 Buff 的创建方法

- 策略模式 - 不同装备槽位使用不同的应用策略


### 2. CoopTool.cs

**功能**：联机工具类，提供各种辅助方法和缓存管理

**核心职责**：

- 管理待处理的血量和 Buff 数据

- 提供网络发送的便捷方法

- 实现主机发现机制

- 提供手雷预制体解析

- 管理参与者ID列表

**关键字段**：

```csharp
// 客户端待处理数据缓存
public static bool _cliSelfHpPending;
public static float _cliSelfHpMax, _cliSelfHpCur;
public static readonly Dictionary<string, List<(int, int)>> _cliPendingProxyBuffs;
public static readonly Dictionary<string, (float, float)> _cliPendingRemoteHp;

```

**主要方法**：

- `Init()` - 初始化工具类

- `SafeKillItemAgent()` - 安全销毁物品Agent

- `ClearWeaponSlot()` - 清除武器插槽

- `ResolveSocketOrDefault()` - 解析插槽类型

- `TryPlayShootAnim()` - 尝试播放射击动画

- `BroadcastReliable()` - 广播可靠包

- `SendReliable()` - 发送可靠包（智能选择传输方式）

- `SendBroadcastDiscovery()` - 发送广播发现

- `GetMapSelectionEntrylist()` - 获取地图选择条目

- `CacheGrenadePrefab()` - 缓存手雷预制体

- `TryResolvePrefab()` - 尝试解析预制体

- `Client_ApplyPendingSelfIfReady()` - 应用待处理的自身数据

- `Client_ApplyPendingRemoteIfAny()` - 应用待处理的远程数据

- `BuildParticipantIds_Server()` - 构建参与者ID列表（仅同场景玩家）

**设计特点**：

- 使用静态方法提供全局访问

- 实现了智能发送机制（根据 Op 码选择传输方式）

- 提供了完善的缓存管理

- 支持待处理数据的延迟应用


### 3. CustomFace.cs

**功能**：自定义外观管理，处理玩家脸部外观的同步

**核心职责**：

- 管理远程玩家外观缓存

- 应用自定义脸部数据

- 加载本地玩家外观

- 清理和刷新外观组件

**关键字段**：

```csharp
// 客户端待应用的外观缓存
public static readonly Dictionary<string, string> _cliPendingFace;

```

**主要方法**：

- `Client_ApplyFaceIfAvailable()` - 客户端应用外观（如果可用）

- `HardApplyCustomFace()` - 强制应用自定义外观

- `StripAllCustomFaceParts()` - 清除所有外观部件

- `LoadLocalCustomFaceJSON()` - 加载本地外观JSON

- `ApplyFaceJsonToModel()` - 将JSON应用到模型

**数据格式**：

- 使用 JSON 序列化 CustomFaceSettingData 结构体

- 支持从 LevelManager 或运行时模型加载

- 提供兜底机制确保外观数据可用

**设计特点**：

- 使用 JSON 进行外观数据序列化

- 实现了多级兜底机制

- 支持延迟应用（等待远程角色创建）

- 使用缓存避免重复处理


### 4. FxManager.cs

**功能**：特效管理器，负责射击特效、近战特效、AI 死亡特效等

**核心职责**：

- 管理枪口火光特效

- 处理抛壳粒子效果

- 播放近战挥砍特效

- 播放 AI 死亡特效和音效

- 实现特效池化以减少GC

**关键字段**：

```csharp
// 特效缓存（池化）
private static readonly Dictionary<ItemAgent_Gun, GameObject> _muzzleFxByGun;
private static readonly Dictionary<ItemAgent_Gun, ParticleSystem> _shellPsByGun;

// 默认枪口特效（兜底）
public static GameObject defaultMuzzleFx;

```

**主要方法**：

- `PlayMuzzleFxAndShell()` - 播放枪口特效和抛壳

- `Client_PlayLocalShotFx()` - 客户端播放本地射击特效

- `Client_PlayAiDeathFxAndSfx()` - 客户端播放AI死亡特效和音效

- `TryStartVisualRecoil_NoAlloc()` - 尝试启动视觉后坐力（零GC）

- `MeleeFx.SpawnSlashFx()` - 生成近战挥砍特效

**性能优化**：

- 使用 Dictionary 池化特效对象，避免重复创建

- 缓存枪械和枪口 Transform，减少 GetComponent 调用

- 使用反射缓存 MethodInfo 和 FieldInfo

- 支持零 GC 的后坐力触发

- 实现去抖机制防止同帧重复播放

**设计特点**：

- 支持有枪实例和无枪实例两种模式

- 提供兜底特效确保视觉反馈

- 使用 FMOD 进行音效播放

- 支持延迟播放（UniTask）


### 5. HarmonyFix.cs

**功能**：Harmony 补丁修复，包含多个关键的运行时补丁

**核心职责**：

- 修复客户端近战伤害报告

- 阻止客户端 AI 互相伤害

- 修复远程玩家距离激活

- 修复可破坏物伤害重定向

- 实现观战系统门控

- 修复暂停菜单行为

**关键补丁**：

#### Patch_ClientReportMeleeHit

- **目标**：DamageReceiver.Hurt

- **功能**：客户端近战命中时上报给服务器而非本地结算

- **优先级**：First

- **特点**：包含弹字显示，失败时回退到本地结算

#### Patch_BlockClientAiVsAI_AtReceiver

- **目标**：DamageReceiver.Hurt

- **功能**：阻止客户端 AI 互相伤害（仅服务器权威）

- **优先级**：First

#### Patch_SABPD_FixedUpdate_AllPlayersUnion

- **目标**：SetActiveByPlayerDistance.FixedUpdate

- **功能**：基于所有在线玩家位置激活对象（而非仅本地玩家）

#### Patch_ClientMelee_HurtRedirect_Destructible

- **目标**：DamageReceiver.Hurt

- **功能**：客户端近战攻击可破坏物时重定向到服务器

- **优先级**：First

#### Patch_ClosureView_ShowAndReturnTask_SpectatorGate

- **目标**：ClosureView.ShowAndReturnTask

- **功能**：玩家死亡时进入观战模式而非结算界面

#### Patch_Paused_AlwaysFalse / Patch_PauseMenuShow/Hide

- **目标**：GameManager.get_Paused / PauseMenu.Show/Hide

- **功能**：联机时禁用暂停功能

**辅助类**：

- `LocalMeleeOncePerFrame` - 近战每帧一次限制

- `MeleeLocalGuard` - 近战本地守卫（ThreadStatic）

- `RemoteReplicaTag` - 远程复制标记

- `NcMainRedirector` - 主角重定向器（ThreadStatic）

**设计特点**：

- 使用 HarmonyPriority 控制补丁顺序

- 实现了完善的错误处理和回退机制

- 使用 ThreadStatic 确保线程安全

- 支持联机和单机模式的自动切换


### 6. NetService.cs

**功能**：网络服务核心类，管理网络连接、玩家状态、传输模式等

**核心职责**：

- 管理 LiteNetLib 网络管理器

- 实现 INetEventListener 接口处理网络事件

- 支持 Direct 和 Steam P2P 两种传输模式

- 管理服务器和客户端状态

- 维护玩家列表和远程角色

**关键字段**：

```csharp
// 网络配置
public int port = 9050;
public NetworkTransportMode TransportMode;
public SteamLobbyOptions LobbyOptions;

// 网络状态
public bool networkStarted;
public bool IsServer { get; private set; }
public NetManager netManager;
public NetDataWriter writer;
public NetPeer connectedPeer;

// 玩家管理（服务器端）
public readonly Dictionary<NetPeer, PlayerStatus> playerStatuses;
public readonly Dictionary<NetPeer, GameObject> remoteCharacters;

// 玩家管理（客户端）
public readonly Dictionary<string, PlayerStatus> clientPlayerStatuses;
public readonly Dictionary<string, GameObject> clientRemoteCharacters;

// 本地玩家
public PlayerStatus localPlayerStatus;

// 同步配置
public float syncInterval = 0.015f; // 15ms = 66.7Hz

```

**主要方法**：

- `StartNetwork(bool isServer, bool keepSteamLobby)` - 启动网络

- `StopNetwork(bool leaveSteamLobby)` - 停止网络

- `ConnectToHost(string ip, int port)` - 连接到主机

- `SetTransportMode(NetworkTransportMode mode)` - 设置传输模式

- `ConfigureLobbyOptions(SteamLobbyOptions? options)` - 配置Lobby选项

- `IsSelfId(string id)` - 判断是否为自己的ID

- `GetPlayerID(NetPeer peer)` - 获取玩家ID

**INetEventListener实现**：

- `OnPeerConnected()` - 玩家连接

- `OnPeerDisconnected()` - 玩家断开

- `OnNetworkReceive()` - 接收网络数据

- `OnNetworkReceiveUnconnected()` - 接收未连接消息（主机发现）

- `OnNetworkLatencyUpdate()` - 延迟更新

- `OnConnectionRequest()` - 连接请求

- `OnNetworkError()` - 网络错误

**传输模式**：

- **Direct 模式**：使用 UDP 直连，支持局域网和公网

- **Steam P2P 模式**：使用 Steam 网络，自动 NAT 穿透

**设计特点**：

- 单例模式（Instance）

- 支持4通道系统（Critical/Important/Normal/Frequent）

- 实现了完善的连接管理和错误处理

- 支持主机发现（广播）

- 自动管理 Steam P2P 会话


### 7. Op.cs

**功能**：网络操作码定义，枚举所有网络消息类型

**核心职责**：

- 定义所有网络操作的唯一标识符

- 使用 byte 类型节省带宽

- 按功能分组组织操作码

**操作码分类**：

#### 基础同步（1-8）

- `PLAYER_STATUS_UPDATE` = 1 - 玩家状态更新

- `CLIENT_STATUS_UPDATE` = 2 - 客户端状态更新

- `POSITION_UPDATE` = 3 - 位置更新

- `ANIM_SYNC` = 4 - 动画同步

- `EQUIPMENT_UPDATE` = 5 - 装备更新

- `PLAYERWEAPON_UPDATE` = 6 - 玩家武器更新

- `FIRE_REQUEST` = 7 - 射击请求

- `FIRE_EVENT` = 8 - 射击事件

#### 手雷系统（9-11）

- `GRENADE_THROW_REQUEST` = 9 - 手雷投掷请求

- `GRENADE_SPAWN` = 10 - 手雷生成

- `GRENADE_EXPLODE` = 11 - 手雷爆炸

#### 物品系统（12-15）

- `ITEM_DROP_REQUEST` = 12 - 物品掉落请求

- `ITEM_SPAWN` = 13 - 物品生成

- `ITEM_PICKUP_REQUEST` = 14 - 物品拾取请求

- `ITEM_DESPAWN` = 15 - 物品消失

#### 生命值系统（16-18）

- `PLAYER_HEALTH_REPORT` = 16 - 玩家血量报告

- `AUTH_HEALTH_SELF` = 17 - 权威血量（自身）

- `AUTH_HEALTH_REMOTE` = 18 - 权威血量（远程）

#### 场景系统（19-23）

- `SCENE_VOTE_START` = 19 - 场景投票开始

- `SCENE_READY_SET` = 20 - 场景准备设置

- `SCENE_BEGIN_LOAD` = 21 - 开始加载场景

- `SCENE_CANCEL` = 22 - 取消场景切换

- `SCENE_READY` = 23 - 场景准备完成

#### 远程角色（24-26）

- `REMOTE_CREATE` = 24 - 远程角色创建

- `REMOTE_DESPAWN` = 25 - 远程角色销毁

- `PLAYER_APPEARANCE` = 26 - 玩家外观

#### AI系统（226-238）

- `AI_HEALTH_REPORT` = 226 - AI血量报告

- `AI_SEED_PATCH` = 227 - AI种子补丁

- `AI_SEED_SNAPSHOT` = 230 - AI种子快照

- `AI_FREEZE_TOGGLE` = 231 - AI冻结切换

- `AI_LOADOUT_SNAPSHOT` = 232 - AI装备快照

- `AI_TRANSFORM_SNAPSHOT` = 233 - AI变换快照

- `AI_ANIM_SNAPSHOT` = 234 - AI动画快照

- `AI_ATTACK_SWING` = 235 - AI攻击挥砍

- `AI_ATTACK_TELL` = 236 - AI攻击预警

- `AI_HEALTH_SYNC` = 237 - AI血量同步

- `AI_NAME_ICON` = 238 - AI名称图标

#### 环境交互（206-222, 242-246）

- `DOOR_REQ_SET` = 206 - 门状态设置请求

- `DOOR_STATE` = 207 - 门状态

- `ENV_HURT_REQUEST` = 220 - 环境伤害请求

- `ENV_HURT_EVENT` = 221 - 环境伤害事件

- `ENV_DEAD_EVENT` = 222 - 环境死亡事件

- `MELEE_ATTACK_REQUEST` = 242 - 近战攻击请求

- `MELEE_ATTACK_SWING` = 243 - 近战攻击挥砍

- `MELEE_HIT_REPORT` = 244 - 近战命中报告

- `ENV_SYNC_REQUEST` = 245 - 环境同步请求

- `ENV_SYNC_STATE` = 246 - 环境同步状态

#### 战利品系统（208-209, 239, 249-255）

- `LOOT_REQ_SLOT_UNPLUG` = 208 - 拔出附件请求

- `LOOT_REQ_SLOT_PLUG` = 209 - 装入附件请求

- `LOOT_REQ_SPLIT` = 239 - 分割请求

- `LOOT_REQ_OPEN` = 250 - 打开容器请求

- `LOOT_STATE` = 251 - 战利品状态

- `LOOT_REQ_PUT` = 252 - 放入请求

- `LOOT_REQ_TAKE` = 253 - 取出请求

- `LOOT_PUT_OK` = 254 - 放入成功

- `LOOT_TAKE_OK` = 255 - 取出成功

- `LOOT_DENY` = 249 - 拒绝操作

#### 其他（170-173, 210, 228-229, 240-241, 247-248）

- `PLAYER_HURT_EVENT` = 170 - 玩家受伤事件

- `PLAYER_BUFF_SELF_APPLY` = 171 - 玩家Buff自我应用

- `HOST_BUFF_PROXY_APPLY` = 172 - 主机Buff代理应用

- `PLAYER_DEAD_TREE` = 173 - 玩家死亡树

- `SCENE_VOTE_REQ` = 210 - 场景投票请求

- `SCENE_GATE_READY` = 228 - 场景门准备

- `SCENE_GATE_RELEASE` = 229 - 场景门释放

- `DISCOVER_REQUEST` = 240 - 发现请求

- `DISCOVER_RESPONSE` = 241 - 发现响应

- `DEAD_LOOT_DESPAWN` = 247 - 死亡战利品消失

- `DEAD_LOOT_SPAWN` = 248 - 死亡战利品生成

**设计特点**：

- 使用 byte 类型（0-255）节省带宽

- 按功能分组便于维护

- 预留了足够的扩展空间

- 命名清晰表达操作意图


### 8. PublicHandleUpdate.cs

**功能**：公共更新处理器，处理装备、武器、动画、位置等更新

**核心职责**：

- 处理装备更新并转发

- 处理武器更新并转发

- 处理客户端动画状态并转发

- 处理位置更新并转发

- 应用远程动画状态

**主要方法**：

#### HandleEquipmentUpdate

- **功能**：处理装备更新

- **流程**：应用到本地 → 转发给其他客户端

- **参数**：endPoint, slotHash, itemID

#### HandleWeaponUpdate

- **功能**：处理武器更新

- **流程**：应用到本地 → 转发给其他客户端

- **参数**：endPoint, slotHash, itemID

#### HandleClientAnimationStatus

- **功能**：处理客户端动画状态

- **流程**：应用到本地 → 附加 playerID → 转发给其他客户端

- **参数**：moveSpeed, moveDirX, moveDirY, isDashing, isAttacking, handState, gunReady, stateHash, normTime

- **传输**：使用 Sequenced 通道（允许乱序丢弃旧包）

#### HandleRemoteAnimationStatus

- **功能**：主机侧应用远程动画状态

- **实现**：使用 AnimInterpUtil 进行插值

#### HandlePositionUpdate

- **功能**：处理位置更新

- **流程**：转发给其他客户端

- **优化**：使用压缩格式（PutV3cm, PutDir）

- **传输**：使用 Unreliable 通道（高频低重要性）

#### HandlePositionUpdate_Q

- **功能**：处理位置更新（Quaternion 版本）

- **流程**：更新 playerStatuses → 应用插值 → 转发

- **实现**：使用 NetInterpUtil 进行插值

**设计特点**：

- 中心化的更新处理和转发

- 使用插值器平滑远程角色移动和动画

- 根据数据重要性选择不同的传输通道

- 实现了高效的数据压缩


## 子模块概览

### AI 模块

负责 AI 角色的网络同步，包括生成种子、装备、动画、血量等。使用稳定哈希算法确保所有客户端生成相同的 AI。

**关键特性**：

- 种子同步确保 AI 一致性

- 20Hz 血量同步

- 装备和武器异步应用

- 动画状态实时同步

- 支持 AI 冻结用于验证

**详细文档**：[AI.md](./AI.md)

### ClientService 模块

处理客户端状态更新和远程角色管理，实现去抖和幂等性控制。

**关键特性**：

- 200ms 去抖窗口

- 幂等性控制

- 异步装备应用

- 远程角色创建管理

**详细文档**：[ClientService.md](./ClientService.md)

### Health 模块

生命值系统的网络同步，实现服务器权威的血量控制。

**关键特性**：

- 服务器权威血量

- 20Hz 节流

- Buff 同步

- 伤害事件处理

**详细文档**：[Health.md](./Health.md)

### HostService 模块

主机端服务处理，包括玩家死亡、装备应用等。

**关键特性**：

- 主机特殊逻辑

- 200ms 去抖控制

- 射击动画播放

- 死亡处理

**详细文档**：[HostService.md](./HostService.md)

### Item 模块

物品掉落和拾取的网络同步，使用 Token 机制防止重复操作。

**关键特性**：

- Token 机制

- 服务器分配 ID

- 物品快照系统

- 防重复标记

**详细文档**：[Item.md](./Item.md)

### Loader 模块

模组加载器，是整个联机模组的入口点。

**关键特性**：

- 模组初始化

- 依赖库加载

- 服务注册

- 配置加载

**详细文档**：[Loader.md](./Loader.md)

### Localization 模块

本地化管理，支持多语言界面。

**支持语言**：

- 英语（en-US）

- 日语（ja-JP）

- 韩语（ko-KR）

- 葡萄牙语（pt-BR）

- 简体中文（zh-CN）

**详细文档**：[Localization.md](./Localization.md)

### LocalPlayer 模块

本地玩家管理，包括状态、输入、同步等。

**关键特性**：

- 本地玩家状态管理

- 输入处理

- 位置和动画同步

- 装备状态同步

**详细文档**：[LocalPlayer.md](./LocalPlayer.md)

### SceneService 模块

场景服务，包括场景切换、投票系统、场景同步等。

**关键特性**：

- 场景切换同步

- 投票系统

- 场景加载管理

- 撤离点管理

**详细文档**：[SceneService.md](./SceneService.md)

### UI 模块

用户界面，包括联机菜单、玩家列表、聊天系统等。

**关键特性**：

- 联机菜单界面

- 玩家列表显示

- 聊天系统

- Steam Lobby 界面

**详细文档**：[UI.md](./UI.md)

### Weapon 模块

武器系统的网络同步，包括射击、换弹、武器切换等。

**关键特性**：

- 射击事件同步

- 换弹同步

- 武器切换同步

- 射击特效同步

**详细文档**：[Weapon.md](./Weapon.md)

### WeatherAndTime 模块

天气和时间系统的网络同步。

**关键特性**：

- 天气状态同步

- 时间同步

- 昼夜循环同步

- 天气变化同步

**详细文档**：[WeatherAndTime.md](./WeatherAndTime.md)


## 整体架构

### 模块关系图

```

                    ┌─────────────────┐
                    │   NetService    │ ◄─── 网络核心
                    │  (网络服务)      │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
      ┌──────────┐   ┌──────────┐   ┌──────────┐
      │   AI     │   │  Player  │   │  Scene   │
      │ (AI 同步) │   │ (玩家管理)│   │ (场景服务)│
      └────┬─────┘   └────┬─────┘   └────┬─────┘
           │              │              │
           │         ┌────┴────┐         │
           │         │         │         │
           ▼         ▼         ▼         ▼
      ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐
      │ Health │ │ Weapon │ │  Item  │ │   UI   │
      │(生命值)│ │ (武器) │ │ (物品) │ │ (界面) │
      └────────┘ └────────┘ └────────┘ └────────┘
           │         │         │         │
           └─────────┴─────────┴─────────┘
                     │
              ┌──────┴──────┐
              │             │
              ▼             ▼
      ┌──────────┐   ┌──────────┐
      │FxManager │   │CoopTool  │
      │ (特效)   │   │ (工具)   │
      └──────────┘   └──────────┘

```

### 数据流向

#### 客户端 → 服务器

```

本地事件 → 请求打包 → 发送 → 服务器验证 → 权威处理

```

#### 服务器 → 客户端

```

权威状态 → 广播打包 → 发送 → 客户端接收 → 应用状态

```

#### 服务器转发

```

客户端A → 服务器 → 验证 → 转发 → 客户端B/C/D

```

### 同步频率

| 数据类型 | 频率 | 传输方式 | 通道 |

|---------|------|---------|------|
|---|---|---|---|

| 动画 | 66.7Hz | Sequenced | 3 |
|---|---|---|---|

| 装备 | 事件驱动 | ReliableOrdered | 1 |
|---|---|---|---|

| 射击 | 事件驱动 | ReliableOrdered | 0 |
|---|---|---|---|

| 场景 | 事件驱动 | ReliableOrdered | 0 |
|---|---|---|---|

| AI 动画 | 10Hz | Unreliable | 2 |
|---|---|---|---|

### 通道系统

Main 模块使用4通道系统优化网络传输：

- **通道0 (Critical)**：投票、伤害、交互 - 最高优先级

- **通道1 (Important)**：血量、装备 - 高优先级

- **通道2 (Normal)**：NPC、物品生成 - 中优先级

- **通道3 (Frequent)**：位置、动画 - 低优先级，高频率


## 核心设计模式

### 1. 服务器权威模式

所有关键游戏状态（血量、物品、AI）由服务器权威控制，客户端仅发送请求和上报状态。

**优点**：

- 防止作弊

- 确保状态一致性

- 简化冲突解决

**实现**：

- 客户端发送请求（REQUEST）

- 服务器验证并处理

- 服务器广播权威状态（AUTH/EVENT）

### 2. 去抖和节流

防止高频操作导致网络拥塞和性能问题。

**去抖**：

- 装备更新：200ms 窗口

- 武器更新：200ms 窗口

- AI 自动绑定：200ms 冷却

**节流**：

- 血量同步：20Hz（50ms）

- AI 血量报告：20Hz（50ms）

- 位置同步：66.7Hz（15ms）

### 3. 幂等性控制

使用字典记录已处理的操作，防止重复处理。

**实现**：

```csharp
// 装备更新幂等性
private static readonly Dictionary<(NetPeer, int), string> _lastAppliedEquip;

// 射击去重
public readonly HashSet<int> _dedupeShotFrame;

```

### 4. 缓存和池化

减少GC压力和提高性能。

**缓存**：

- 枪械和枪口 Transform 缓存

- 特效对象池化

- AI 映射缓存

- 反射 FieldInfo/MethodInfo 缓存

**池化**：

- 枪口火光特效

- 抛壳粒子系统

- NetDataWriter 复用

### 5. 异步处理

使用 UniTask 进行异步操作，避免阻塞主线程。

**应用场景**：

- 物品实例化

- 装备应用

- Buff 解析

- 延迟特效播放

### 6. 数据压缩

减少网络带宽占用。

**压缩方法**：

- `PutV3cm()` - 位置压缩（12字节）

- `PutDir()` - 方向压缩（4字节）

- 使用byte 存储枚举和标志

- 使用 short 存储小范围整数

### 7. 智能传输

根据操作类型自动选择传输方式。

**实现**：

```csharp
public static void SendSmart(this NetManager manager, NetDataWriter w, Op op)
{
    var method = OpPriority.GetDeliveryMethod(op);
    var channel = OpPriority.GetChannel(op);
    manager.SendToAll(w, method, channel);
}

```


## 关键流程

### 1. 网络启动流程

```

StartNetwork(isServer)
    ↓
初始化 NetManager（4通道）
    ↓
设置传输模式（Direct/Steam P2P）
    ↓
启动监听（服务器）或客户端
    ↓
初始化 COOPManager（18个子系统）
    ↓
初始化 LocalPlayerManager
    ↓
注册事件监听器
    ↓
网络就绪

```

### 2. 玩家连接流程

#### 服务器端

```

OnPeerConnected
    ↓
创建 PlayerStatus
    ↓
发送主机血量（AUTH_HEALTH_REMOTE）
    ↓
发送所有远程玩家血量
    ↓
广播玩家状态更新

```

#### 客户端

```

ConnectToHost(ip, port)
    ↓
发送连接请求（gameKey）
    ↓
OnPeerConnected
    ↓
发送客户端状态（CLIENT_STATUS_UPDATE）
    ↓
接收主机和其他玩家状态
    ↓
创建远程角色

```

### 3. 远程角色创建流程

```

接收REMOTE_CREATE
    ↓
实例化角色预制体
    ↓
设置位置和旋转
    ↓
应用外观（CustomFace）
    ↓
应用装备
    ↓
应用武器
    ↓
添加网络组件（NetInterpUtil, AnimInterpUtil）
    ↓
添加血条组件（AutoRequestHealthBar）
    ↓
注册到 clientRemoteCharacters

```

### 4. 射击同步流程

#### 客户端

```

本地射击
    ↓
播放本地特效和音效
    ↓
发送 FIRE_REQUEST
    ↓
等待服务器确认

```

#### 服务器

```

接收FIRE_REQUEST
    ↓
验证射击合法性
    ↓
计算命中和伤害
    ↓
应用伤害到目标
    ↓
广播FIRE_EVENT

```

#### 其他客户端

```

接收FIRE_EVENT
    ↓
播放射击特效
    ↓
播放射击音效
    ↓
播放射击动画

```

### 5. 伤害处理流程

#### 客户端（近战）

```

本地近战攻击
    ↓
Patch拦截 Hurt
    ↓
发送 MELEE_HIT_REPORT
    ↓
显示本地弹字
    ↓
等待服务器确认

```

#### 服务器

```

接收MELEE_HIT_REPORT
    ↓
验证伤害合法性
    ↓
在范围内搜索目标
    ↓
应用伤害
    ↓
广播血量变化

```

### 6. AI同步流程

#### 服务器初始化

```

场景加载完成
    ↓
收集所有AI Root
    ↓
生成场景种子
    ↓
为每个 Root 生成稳定 ID 和种子
    ↓
广播AI_SEED_SNAPSHOT

```

#### 客户端

```

接收AI_SEED_SNAPSHOT
    ↓
使用相同种子生成 AI
    ↓
等待 AI 生成完成
    ↓
接收 AI_LOADOUT_SNAPSHOT
    ↓
异步应用装备和武器
    ↓
接收 AI_TRANSFORM_SNAPSHOT（10Hz）
    ↓
应用位置和旋转（插值）
    ↓
接收 AI_ANIM_SNAPSHOT（10Hz）
    ↓
应用动画状态

```

### 7. 场景切换流程

```

玩家请求场景切换
    ↓
发送SCENE_VOTE_REQ
    ↓
服务器广播 SCENE_VOTE_START
    ↓
所有客户端显示投票 UI
    ↓
客户端发送 SCENE_READY_SET
    ↓
服务器统计准备状态
    ↓
所有人准备完成
    ↓
服务器广播 SCENE_BEGIN_LOAD
    ↓
所有客户端开始加载
    ↓
客户端加载完成发送 SCENE_GATE_READY
    ↓
服务器等待所有人就绪
    ↓
服务器广播 SCENE_GATE_RELEASE
    ↓
所有客户端退出加载界面
    ↓
场景切换完成

```


## 性能优化

### 1. 网络优化

**带宽优化**：

- 位置压缩：Vector3 (12字节) vs 原始 (12字节，但精度优化)

- 方向压缩：Vector3 (4字节) vs 原始 (12字节)

- 使用 byte 存储枚举和标志

- 分包传输大数据（AI 动画快照）

**频率控制**：

- 位置同步：66.7Hz（15ms 间隔）

- 血量同步：20Hz（50ms 间隔）

- AI 变换：10Hz（100ms 间隔）

- 装备更新：事件驱动

**智能传输**：

- Critical 数据：ReliableOrdered + 通道0

- Important 数据：ReliableOrdered + 通道1

- Normal 数据：ReliableOrdered + 通道2

- Frequent 数据：Unreliable/Sequenced + 通道3

### 2. 内存优化

**对象池化**：

- 枪口火光特效池

- 抛壳粒子系统池

- NetDataWriter 复用

**缓存机制**：

- 枪械和枪口 Transform 缓存

- AI 映射缓存（aiByID）

- 反射 FieldInfo/MethodInfo 缓存

- 物品预制体缓存

**及时清理**：

- 远程角色断开时销毁

- 物品拾取后销毁

- AI 死亡后移除尸体

### 3. CPU 优化

**去抖和节流**：

- 装备更新200ms 去抖

- 血量同步20Hz 节流

- AI 自动绑定200ms 冷却

**异步处理**：

- 使用 UniTask 避免阻塞

- 物品实例化异步

- 装备应用异步

**范围限制**：

- AI 自动绑定35米范围

- 使用 OverlapSphereNonAlloc 避免 GC

**日志限制**：

- pending 警告每200次输出1次

- 使用条件编译减少日志

### 4. 渲染优化

**距离激活**：

- SetActiveByPlayerDistance 基于所有玩家位置

- 远程 AI 强制显示选项

**特效优化**：

- 特效对象池化

- 延迟播放特效

- 去抖防止重复播放


## 依赖关系

### 外部依赖

**Unity 引擎**：

- UnityEngine.CoreModule - 核心功能

- UnityEngine.PhysicsModule - 物理系统

- UnityEngine.AnimationModule - 动画系统

- UnityEngine.UIModule - UI 系统

**第三方库**：

- LiteNetLib - UDP 网络库

- HarmonyLib - 运行时代码修改

- Steamworks.NET - Steam 集成

- UniTask - 异步任务

- FMOD - 音频系统

**游戏原生**：

- TeamSoda.Duckov.Core.dll - 游戏核心库

- 各种游戏系统（Health, Item, Character 等）

### 内部依赖

**Main模块内部**：

```

NetService (核心)
    ↓
COOPManager (管理器)
    ↓
各子系统 (AI, Player, Item 等)
    ↓
工具类 (CoopTool, FxManager等)

```

**跨模块依赖**：

- Main → Net (网络通信)

- Main → Patch (游戏逻辑修改)

- Main → NetTag (网络标签)

- Main → SyncData (同步数据)

- Main → Utils (工具类)

### 被依赖关系

**被 Patch 模块依赖**：

- Patch/Character → Main/AI, Main/Health

- Patch/Item → Main/Item

- Patch/Scene → Main/SceneService

**被 Net 模块依赖**：

- Net/NetPack → Main/NetService

- Net/Steam → Main/NetService


## 使用示例

### 1. 启动网络服务

```csharp
// 启动服务器
NetService.Instance.StartNetwork(isServer: true);

// 启动客户端并连接
NetService.Instance.StartNetwork(isServer: false);
NetService.Instance.ConnectToHost("192.168.1.100", 9050);

```

### 2. 发送网络消息

```csharp
// 使用智能发送（自动选择传输方式）
var writer = new NetDataWriter();
writer.Put((byte)Op.PLAYER_STATUS_UPDATE);
writer.Put(playerId);
writer.PutV3cm(position);
writer.PutDir(forward);

// 服务器广播
NetService.Instance.netManager.SendSmart(writer, Op.PLAYER_STATUS_UPDATE);

// 客户端发送
NetService.Instance.connectedPeer.SendSmart(writer, Op.PLAYER_STATUS_UPDATE);

```

### 3. 切换装备

```csharp
// 切换护甲
await COOPManager.ChangeArmorModel(characterModel, armorItem);

// 切换武器
COOPManager.ChangeWeaponModel(characterModel, weaponItem, HandheldSocketTypes.normalHandheld);

```

### 4. 应用Buff

```csharp
// 解析并应用Buff
var buff = await COOPManager.ResolveBuffAsync(weaponTypeId, buffId);
if (buff != null)
{
    characterMainControl.AddBuff(buff, null, weaponTypeId);
}

```

### 5. 播放特效

```csharp
// 播放射击特效
FxManager.PlayMuzzleFxAndShell(shooterId, weaponType, muzzlePos, finalDir);

// 播放近战特效
MeleeFx.SpawnSlashFx(characterModel);

// 播放 AI 死亡特效
FxManager.Client_PlayAiDeathFxAndSfx(aiCharacterMainControl);

```

### 6. 管理AI

```csharp
// 注册AI
AIHandle.RegisterAI(aiId, characterMainControl);

// 服务器广播 AI 种子
AIHandle.Server_SendAiSeeds();

// 客户端报告 AI 血量
AIHealth.Client_ReportAiHealth(aiId, maxHealth, currentHealth);

```

### 7. 处理物品

```csharp
// 请求掉落物品
ItemRequest.SendItemDropRequest(item, position, rotation);

// 请求拾取物品
ItemRequest.SendItemPickupRequest(dropId);

```

### 8. 场景切换

```csharp
// 请求场景投票
SceneNet.Instance.Client_RequestSceneVote(targetSceneID);

// 设置准备状态
SceneNet.Instance.Client_SetReady(isReady);

```


## 注意事项

### 1. 网络安全

**服务器权威**：

- 所有关键操作必须经过服务器验证

- 客户端不能直接修改其他玩家状态

- 使用连接密钥（gameKey）防止未授权连接

**数据验证**：

- 验证玩家 ID 合法性

- 验证操作范围（如近战攻击距离）

- 验证物品存在性

### 2. 性能考虑

**频率控制**：

- 不要超过推荐的同步频率

- 使用去抖和节流防止网络拥塞

- 事件驱动优于轮询

**内存管理**：

- 及时清理断开连接的玩家数据

- 使用对象池减少 GC

- 避免在 Update 中创建对象

**异步操作**：

- 使用 UniTask 进行异步操作

- 避免阻塞主线程

- 注意异步操作的取消和异常处理

### 3. 兼容性

**单机模式**：

- 所有补丁必须检查 networkStarted

- 单机模式下跳过网络逻辑

- 保持原版游戏功能完整

**版本兼容**：

- 协议版本号管理

- 向后兼容考虑

- 优雅降级处理

### 4. 调试技巧

**日志输出**：

- 使用 Debug.Log 输出关键信息

- 限制高频日志输出

- 使用条件编译控制日志级别

**网络调试**：

- 监控网络延迟和丢包

- 检查 playerStatuses 状态

- 验证远程角色创建

**状态检查**：

- 检查 networkStarted 状态

- 验证 IsServer 标志

- 确认 connectedPeer 不为 null

### 5. 常见问题

**远程角色不显示**：

- 检查 REMOTE_CREATE 是否发送

- 验证外观数据是否正确

- 确认装备应用是否完成

**血量不同步**：

- 检查20Hz 节流是否正常

- 验证服务器权威逻辑

- 确认 AUTH_HEALTH 消息接收

**AI 不一致**：

- 检查种子是否相同

- 验证装备快照是否完整

- 确认 AI 冻结状态

**射击不同步**：

- 检查 FIRE_EVENT 是否广播

- 验证特效缓存是否正确

- 确认动画播放是否触发

**场景切换卡住**：

- 检查所有玩家准备状态

- 验证 SCENE_GATE 消息

- 确认场景加载完成


## 扩展指南

### 1. 添加新的网络操作

**步骤**：
1. 在 Op.cs 中添加新的操作码
2. 在 OpPriority 中配置传输方式和通道
3. 实现发送方法
4. 实现接收处理方法
5. 在 ModBehaviourF.OnNetworkReceive中添加分发逻辑

**示例**：

```csharp
// 1. 添加操作码
public enum Op : byte
{
    // ...
    NEW_FEATURE = 100
}

// 2. 配置优先级
OpPriority.SetPriority(Op.NEW_FEATURE, DeliveryMethod.ReliableOrdered, channel: 1);

// 3. 发送方法
public void SendNewFeature(string data)
{
    var w = new NetDataWriter();
    w.Put((byte)Op.NEW_FEATURE);
    w.Put(data);
    NetService.Instance.netManager.SendSmart(w, Op.NEW_FEATURE);
}

// 4. 接收处理
public void HandleNewFeature(NetPeer sender, NetPacketReader reader)
{
    var data = reader.GetString();
    // 处理逻辑
}

```

### 2. 添加新的子系统

**步骤**：
1. 创建子系统类
2. 在 COOPManager 中添加静态字段
3. 在 InitManager 中初始化
4. 实现必要的网络同步逻辑

**示例**：

```csharp
// 1. 创建子系统
public class NewSystem
{
    public void Initialize() { }
    public void Update() { }
}

// 2. 添加到 COOPManager
public static NewSystem NewSystem;

// 3. 初始化
public static void InitManager()
{
    // ...
    NewSystem = new NewSystem();
}

```

### 3. 优化网络性能

**压缩数据**：

```csharp
// 使用压缩方法
writer.PutV3cm(position);  // 位置压缩
writer.PutDir(direction);  // 方向压缩
writer.Put((byte)value);   // 使用byte存储小值

```

**批量发送**：

```csharp
// 将多个更新打包到一个消息
var w = new NetDataWriter();
w.Put((byte)Op.BATCH_UPDATE);
w.Put(count);
for (int i = 0; i < count; i++)
{
    w.Put(data[i]);
}

```

**选择性同步**：

```csharp
// 只同步变化的数据
if (currentValue != lastValue)
{
    SendUpdate(currentValue);
    lastValue = currentValue;
}

```

### 4. 添加新的Harmony补丁

**步骤**：
1. 在 HarmonyFix.cs 中添加补丁类
2. 使用[HarmonyPatch]特性标记
3. 实现 Prefix/Postfix/Transpiler 方法
4. 添加必要的检查（networkStarted, IsServer等）

**示例**：

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
internal static class Patch_NewFeature
{
    [HarmonyPrefix]
    private static bool Prefix(TargetClass __instance)
    {
        var mod = ModBehaviourF.Instance;
        if (mod == null || !mod.networkStarted) return true;

        // 联机逻辑
        return false; // 阻止原方法执行
    }
}

```

### 5. 调试和测试

**本地测试**：

```csharp
// 启动两个游戏实例
// 实例1：服务器
NetService.Instance.StartNetwork(isServer: true);

// 实例2：客户端
NetService.Instance.StartNetwork(isServer: false);
NetService.Instance.ConnectToHost("127.0.0.1", 9050);

```

**日志调试**：

```csharp
#if DEBUG
Debug.Log($"[COOP] Feature: {data}");
#endif

```

**性能分析**：

```csharp
// 使用Stopwatch 测量性能
var sw = System.Diagnostics.Stopwatch.StartNew();
// 执行操作
sw.Stop();
Debug.Log($"Operation took {sw.ElapsedMilliseconds}ms");

```

## 总结

Main模块是Escape From Duckov Coop Mod 的核心，实现了完整的联机功能。通过服务器权威架构、高效的网络同步、完善的性能优化和灵活的扩展机制，为玩家提供了流畅的联机体验。

**核心特点**：

- 服务器权威确保游戏公平性

- 多通道系统优化网络传输

- 去抖节流控制网络频率

- 对象池化减少 GC 压力

- 异步处理避免阻塞

- 数据压缩节省带宽

- 完善的错误处理和兜底机制

**技术亮点**：

- 支持 Direct 和 Steam P2P 两种网络模式

- 实现了完整的 AI 同步系统

- 提供了灵活的场景切换机制

- 使用 Harmony 实现无侵入式修改

- 支持多语言本地化

Main 模块为整个联机系统提供了坚实的基础，是理解和扩展联机功能的关键。
