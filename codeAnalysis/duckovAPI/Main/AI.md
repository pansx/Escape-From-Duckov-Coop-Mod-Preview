# Main/AI 模块 API 文档

## 模块概述

AI模块负责管理AI角色的网络同步、装备应用、动画同步、生命值管理、名称图标显示等核心功能。该模块确保所有客户端的AI状态保持一致，包括AI的生成种子、装备、动画、血量等关键数据。

**核心职责**：
- AI生成种子同步，确保所有客户端生成相同的AI
- AI装备和武器的网络同步
- AI动画状态的实时同步
- AI生命值的权威同步
- AI名称和图标的显示管理
- AI自动绑定和状态管理

## 文件列表

| 文件名 | 说明 |
|--------|------|
| `AIHandle.cs` | AI处理核心类，管理AI种子、装备、变换和动画同步 |
| `AIHealth.cs` | AI生命值管理，处理血量同步和伤害报告 |
| `AIName.cs` | AI名称与图标管理，处理显示逻辑 |
| `AIRequest.cs` | AI请求处理，单例管理器 |
| `AITool.cs` | AI工具类，提供各种辅助方法 |

## 核心类说明

### 1. AIHandle

**功能**：AI处理核心类，负责AI的种子同步、装备应用、变换同步和动画同步

**关键字段**：
```csharp
// AI种子映射：rootId -> seed
public readonly Dictionary<int, int> aiRootSeeds

// 待应用的装备数据
public readonly Dictionary<int, (...)> pendingAiLoadouts

// 客户端AI血量覆盖
public readonly Dictionary<Health, float> _cliAiMaxOverride

// 是否冻结AI
public bool freezeAI = true

// 场景种子
public int sceneSeed
```

**主要方法**：

#### Server_SendAiSeeds
- **功能**：服务器发送AI生成种子快照
- **说明**：生成场景种子，为每个Root生成稳定ID和种子，支持双映射确保兼容性

#### RegisterAi
- **功能**：注册AI到系统
- **说明**：添加到aiById字典，应用待处理数据，服务器广播装备，客户端添加网络组件

#### Server_BroadcastAiLoadout
- **功能**：服务器广播AI装备快照（v5协议）
- **包含**：装备、武器、脸部JSON、模型名、图标类型、显示名称

#### Client_ApplyAiLoadout
- **功能**：客户端异步应用AI装备
- **流程**：切换模型 → 等待就绪 → 应用名称图标 → 应用装备 → 应用武器 → 应用脸部

#### Server_BroadcastAiTransforms
- **功能**：广播所有AI的位置和朝向
- **优化**：使用压缩格式（PutV3cm, PutDir）

#### Server_BroadcastAiAnimations
- **功能**：广播所有AI的动画状态
- **优化**：Unreliable传输，支持分包

### 2. AIHealth

**功能**：AI生命值管理，处理血量同步和伤害报告

**主要方法**：

#### Server_BroadcastAiHealth
- **功能**：服务器广播AI血量
- **传输**：使用SendSmart自动选择（Critical → ReliableOrdered）

#### Client_ReportAiHealth
- **功能**：客户端报告AI血量
- **节流**：20Hz（50ms冷却）

#### HandleAiHealthReport
- **功能**：服务器处理血量报告
- **流程**：验证 → 应用 → 广播 → 处理死亡

#### Client_ApplyAiHealth
- **功能**：客户端应用AI血量
- **特性**：显示伤害数字，处理超出最大值情况，死亡时隐藏并播放特效

### 3. AIName

**功能**：AI名称与图标管理

**主要方法**：

#### FindCharacterModelByName_Any
- **功能**：通过名称查找角色模型
- **查找顺序**：已加载资源 → Resources目录 → 场景实例

#### ResolveIconSprite
- **功能**：解析图标类型到Sprite
- **支持**：none, elete, pmc, boss, merchant, pet

#### RefreshNameIconWithRetries
- **功能**：多帧重试刷新名称和图标
- **说明**：等待HealthBar生成，反射调用RefreshCharacterIcon

#### Server_BroadcastAiNameIcon
- **功能**：服务器广播AI名称和图标
- **特性**：对boss/elete强制显示名称

### 4. AIRequest

**功能**：AI请求处理单例管理器

**主要方法**：

#### Server_SendRootSeedDelta
- **功能**：发送单个Root的增量种子
- **说明**：包含guid和兼容id两条映射

#### Server_TryRebroadcastIconLater
- **功能**：延迟重播图标
- **说明**：等待0.6秒后重新检查

### 5. AITool

**功能**：AI工具类，提供各种辅助方法

**关键字段**：
```csharp
// AI映射
public static readonly Dictionary<int, CharacterMainControl> aiById

// AI死亡特效已播放集合
public static readonly HashSet<int> _cliAiDeathFxOnce
```

**主要方法**：

#### StableRootId / StableRootId_Alt
- **功能**：生成稳定的Root ID
- **算法**：FNV1a哈希（场景+名称+坐标）

#### IsRealAI
- **功能**：判断是否为真实AI
- **过滤**：主角、玩家队伍、宠物、远程玩家

#### TryAutoBindAi / TryAutoBindAiWithBudget
- **功能**：自动绑定AI
- **优化**：200ms冷却，35米范围，OverlapSphere搜索

#### TryFreezeAI
- **功能**：冻结AI逻辑
- **禁用**：AICharacterController, AI_PathControl, FSMOwner, Blackboard

#### GetLocalAIEquipment
- **功能**：获取AI装备列表
- **槽位**：护甲、头盔、面罩、背包、耳机

#### EnsureMagicBlendBound
- **功能**：确保MagicBlend组件正确绑定

#### TryClientRemoveNearestAICorpse
- **功能**：客户端移除最近的AI尸体
- **优化**：使用OverlapSphereNonAlloc避免GC

## 依赖关系

**依赖模块**：
- NetService - 网络通信
- COOPManager - 资源管理
- NetPack - 数据压缩
- HealthM - 生命值管理
- FxManager - 特效管理

**被依赖模块**：
- Patch/Character - 调用AI注册
- Main/HostService - 使用AI管理

## 关键流程

### AI生成同步
```
服务器 → 生成种子 → 广播快照
客户端 → 接收种子 → 使用相同种子生成AI
```

### AI装备同步
```
服务器 → 收集装备信息 → 广播装备快照
客户端 → 接收快照 → 异步应用装备
```

### AI血量同步
```
客户端 → 20Hz报告血量 → 服务器
服务器 → 验证并应用 → 广播给所有客户端
客户端 → 应用血量 → 显示伤害 → 处理死亡
```

### AI动画同步
```
服务器 → 每帧收集动画参数 → 分包发送（Unreliable）
客户端 → 接收快照 → 应用到NetAiFollower
```

## 性能优化

1. **数据压缩**：位置12字节，方向4字节，节省50-70%带宽
2. **节流限频**：血量20Hz，自动绑定200ms冷却
3. **分包传输**：动画快照根据MTU自动分包
4. **缓存机制**：aiById、pendingAiLoadouts、_cliAiMaxOverride
5. **范围限制**：自动绑定35米，尸体移除使用OverlapSphereNonAlloc
6. **日志限制**：pending警告每200次输出1次

## 注意事项

1. AI默认冻结（freezeAI=true）用于验证一致性
2. 种子使用稳定哈希算法，支持双映射
3. 装备协议当前版本v5，包含名字文本
4. 服务器作为血量权威源
5. 动画使用Unreliable传输，允许丢包
6. 自动绑定有200ms冷却和35米范围限制
