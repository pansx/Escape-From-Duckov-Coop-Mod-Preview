# Net 模块总览

## 概述

Net 模块是 Escape From Duckov Coop Mod 的网络通信层，负责数据传输、状态同步、插值优化和网络质量管理。该模块包含2个子模块（NetPack 和 Steam）和11个根级核心类，实现了从底层网络传输到高层数据同步的完整网络架构。

## 模块架构

Net模块采用分层网络架构设计：

```

Net/
├── 核心网络层（根级文件）
│   ├── NetDataExtensions.cs      - 数据序列化扩展

│   ├── NetworkExtensions.cs      - 智能发送扩展

│   ├── OpPriority.cs             - 操作码优先级映射

│   └── PacketPriority.cs         - 数据包优先级系统

├── 同步和插值层
│   ├── NetInterpolator.cs        - 位置插值器（Fika 架构）

│   ├── NetAiFollower.cs          - AI 动画跟随器

│   └── NetAiVisibilityGuard.cs   - AI 可见性管理

├── 优化层
│   ├── NetPacketPool.cs          - 数据包对象池

│   └── NetSilenceGuards.cs       - 静默守卫（防止循环触发）

├── 标记和特效层
│   ├── NetAiTag.cs               - AI 网络标记

│   └── LocalHitKillFx.cs         - 本地命中特效

└── 子模块（2个）
    ├── NetPack/                  - 网络数据包定义

    └── Steam/                    - Steam P2P网络

```

## 核心类详解

### 1. NetInterpolator - 位置插值器

**功能**：实现基于 Fika 架构的高级网络插值系统，提供平滑的位置和旋转同步。

**核心特性**：- **时间轴系统**：使用本地时间轴进行插值，支持动态时间缩放

- **追赶/减速机制**：自动调整播放速度，保持同步

- **动态延迟调整**：根据网络质量自动优化缓冲延迟

- **EMA 平滑**：使用指数移动平均减少网络抖动

- **二分查找优化**：大缓冲区时使用 O(log n)查找

- **异常跳变检测**：自动检测并处理瞬移

**关键属性**：```csharp
public float interpolationBackTime = 0.12f          // 渲染回看时间
public float maxExtrapolate = 0.05f                 // 最大预测时间
public float hardSnapDistance = 6f                  // 硬对齐距离
public bool enableEmaSmoothing = true               // 启用 EMA 平滑
public bool enableDynamicDelay = true               // 动态延迟调整
public bool enableCatchupSlowdown = true            // 追赶/减速机制

```

**关键方法**：```csharp
public void Push(Vector3 pos, Quaternion rot, double when = -1)  // 推送快照
public NetworkQualityStats GetNetworkStats()                      // 获取网络统计
private int BinarySearchSnapshot(double targetTime)               // 二分查找快照

```

**网络质量统计**：- 平均延迟和标准差

- 包间隔抖动

- 时间漂移和缩放

- 缓冲区大小

- 质量评分（0-100）

### 2. NetworkExtensions - 智能发送扩展

**功能**：提供智能网络发送方法，根据 Op 操作码自动选择最优传输方式。

**核心职责**：- 自动选择 DeliveryMethod（Reliable/Unreliable/Sequenced）

- 自动分配通道编号（0-3）

- 批量发送优化

- 网络统计监控

**关键方法**：```csharp
public static void SendSmart(this NetManager netManager, NetDataWriter writer, Op op)
public static void SendSmart(this NetPeer peer, NetDataWriter writer, Op op)
public static void SendSmartExcept(this NetManager netManager, NetDataWriter writer, Op op, NetPeer excludedPeer)
public static void SendBatch<T>(this NetManager netManager, Op op, IEnumerable<T> items, Action<NetDataWriter, T> writeItem)
public static string GetNetworkStats(this NetManager netManager)

```

### 3. OpPriority - 操作码优先级映射

**功能**：为每个 Op 操作码分配优先级，实现智能网络传输。

**优先级分类**：- **Critical (关键)**：投票、伤害、拾取、交互 → ReliableOrdered

- **Important (重要)**：血量、装备、弹药 → ReliableSequenced

- **Normal (普通)**：NPC 状态、物品生成 → ReliableUnordered

- **Frequent (高频)**：位置、动画 → Unreliable

**关键方法**：```csharp
public static PacketPriority GetPriority(this Op op)
public static DeliveryMethod GetDeliveryMethod(this Op op)
public static byte GetChannelNumber(this Op op)
public static bool IsReliable(this Op op)
public static string GetPriorityStats()

```

**优化效果**：- 位置更新使用Unreliable，减少90%带宽

- 关键事件使用 ReliableOrdered，确保送达

- 多通道系统避免队头阻塞

### 4. PacketPriority - 数据包优先级系统

**功能**：定义网络数据包的优先级枚举和扩展方法。

**优先级定义**：```csharp
public enum PacketPriority : byte
{
    Critical = 0,    // 关键功能 - ReliableOrdered

    Important = 1,   // 重要状态 - ReliableSequenced

    Normal = 2,      // 普通事件 - ReliableUnordered

    Frequent = 3,    // 高频更新 - Unreliable

    Voice = 4        // 语音数据 - Sequenced

}

```

**扩展方法**：```csharp
public static DeliveryMethod GetDeliveryMethod(this PacketPriority priority)
public static byte GetChannelNumber(this PacketPriority priority)
public static string GetDescription(this PacketPriority priority)
public static bool IsReliable(this PacketPriority priority)

```

### 5. NetDataExtensions - 数据序列化扩展

**功能**：提供 Vector3和 Quaternion 的序列化/反序列化扩展方法，包含安全性检查。

**核心职责**：- Vector3序列化（12字节）

- Quaternion 序列化（16字节）

- NaN/Infinity 检测和修复

- 零四元数保护

**关键方法**：```csharp
public static void PutVector3(this NetDataWriter writer, Vector3 vector)
public static Vector3 GetVector3(this NetPacketReader reader)
public static void PutQuaternion(this NetDataWriter writer, Quaternion q)
public static Quaternion GetQuaternion(this NetPacketReader reader)

```

**安全特性**：- 自动归一化四元数

- 检测并修复非法值（NaN、Infinity）

- 防止零四元数导致的错误

### 6. NetPacketPool - 数据包对象池

**功能**：实现 NetDataWriter 对象池，减少 GC 压力，提高性能。

**核心特性**：- 线程安全（使用 ConcurrentBag）

- 自动限制池大小（最大100个）

- 自动重置对象状态

- 池统计信息

**关键方法**：```csharp
public static NetDataWriter GetWriter()
public static void ReturnWriter(NetDataWriter writer)
public static PoolStats GetStats()
public static void Clear()

```

**使用示例**：```csharp
var writer = NetPacketPool.GetWriter();
try
{
    writer.Put((byte)Op.POSITION_UPDATE);
    writer.PutVector3(position);
    netManager.SendSmart(writer, Op.POSITION_UPDATE);
}
finally
{
    NetPacketPool.ReturnWriter(writer);
}

```

### 7. NetAiFollower - AI 动画跟随器

**功能**：管理远程 AI 的动画同步和平滑播放。

**核心职责**：- 动画参数平滑插值（MoveSpeed、MoveDirX/Y、HandState 等）

- 位置和朝向平滑跟随

- 自动检测和修复 Animator 丢失

- 支持换壳后重新绑定

**关键方法**：```csharp
public void SetTarget(Vector3 pos, Vector3 dir)
public void SetAnim(float speed, float dirX, float dirY, int hand, bool gunReady, bool dashing)
public void PlayAttack()
public void ForceRebindAfterModelSwap()

```

**动画参数**：- MoveSpeed: 移动速度

- MoveDirX/Y: 移动方向

- HandState: 手部状态

- GunReady: 枪械准备状态

- Dashing: 冲刺状态

### 8. NetAiTag - AI 网络标记

**功能**：标记网络同步的 AI 实例，存储 AI 的网络 ID和显示信息。

**核心属性**：```csharp
public int aiID                      // AI 唯一 ID
public string nameOverride           // 主机下发的显示名
public int? iconTypeOverride         // 图标类型覆盖
public bool? showNameOverride        // 是否显示名字

```

**用途**：- 区分本地AI和网络同步 AI

- 存储主机权威的显示信息

- 防止非 AI 对象被误标记

### 9. NetAiVisibilityGuard - AI 可见性管理

**功能**：管理 AI 的渲染器、光源和粒子系统的可见性。

**核心职责**：- 批量控制 Renderer、Light、ParticleSystem

- 缓存组件引用，避免重复查找

- 用于距离激活/停用优化

**关键方法**：```csharp
public void SetVisible(bool v)

```

### 10. NetSilenceGuards - 静默守卫

**功能**：使用线程局部变量防止网络事件的循环触发。

**核心标记**：```csharp
[ThreadStatic] public static bool InPickupItem              // 正在执行拾取物品
[ThreadStatic] public static bool InCapacityShrinkCleanup   // 正在执行容量清理

```

**用途**：- 防止拾取物品时触发网络事件导致循环

- 防止容量调整时的重入问题

### 11. LocalHitKillFx - 本地命中特效

**功能**：在客户端本地播放命中和击杀特效，提供即时反馈。

**核心职责**：- 播放受击可视化（HurtVisual）

- 显示 UI 命中标记（HitMarker）

- 弹出伤害数字（PopText）

- 支持 AI和环境可破坏物

**关键方法**：```csharp
public static void ClientPlayForAI(CharacterMainControl victim, DamageInfo di, bool predictedDead)
public static void ClientPlayForDestructible(HealthSimpleBase hs, DamageInfo di, bool predictedDead)
public static void PopDamageText(Vector3 hintPos, DamageInfo di)
public static void RememberLastBaseDamage(float v)

```

**特效类型**：- 受击动画和粒子效果

- 击杀标记和音效

- 伤害数字弹出

- 暴击特效

## 子模块概览

### NetPack 子模块

定义网络数据包的结构和序列化方法，提供高效的数据打包和解包功能。

**核心类**：NetDataWriter 扩展、数据包结构定义
**详细文档**：[NetPack.md](./NetPack.md)

### Steam 子模块

实现 Steam P2P 网络传输，包括大厅管理、端点映射和 P2P 连接。

**核心类**：SteamP2PManager, SteamLobbyManager, SteamEndPointMapper
**详细文档**：[Steam.md](./Steam.md)

## 核心流程

### 1. 网络初始化流程

```

NetService.StartNetwork()
  ├── 创建 NetManager（LiteNetLib）
  ├── 配置多通道系统（4个通道）
  ├── 初始化 Steam P2P（如果启用）
  │   ├── SteamP2PManager 初始化
  │   ├── SteamLobbyManager 初始化
  │   └── SteamEndPointMapper 初始化
  └── 设置 UseNativeSockets（P2P模式为false）

```

### 2. 智能发送流程

```

发送网络消息
  ├── 调用SendSmart(writer, op)
  ├── OpPriority.GetPriority(op)
  │   └── 返回 PacketPriority 枚举
  ├── PacketPriority.GetDeliveryMethod()
  │   └── 返回 LiteNetLib 的 DeliveryMethod
  ├── PacketPriority.GetChannelNumber()
  │   └── 返回通道编号（0-3）
  └── netManager.SendToAll(data, channel, method)

```

### 3. 位置同步流程

```

远程玩家位置更新
  ├── 接收POSITION_UPDATE 包（Unreliable）
  ├── NetInterpolator.Push(pos, rot, timestamp)
  │   ├── 检测异常跳变（>6米）
  │   ├── 插入 SortedList 缓冲区
  │   ├── 计算 deliveryTime 和 jitter
  │   ├── 动态调整 interpolationBackTime
  │   ├── 计算 drift（时间漂移）
  │   └── 调整 localTimeScale（追赶/减速）
  └── LateUpdate()插值应用
      ├── 前进本地时间轴
      ├── 二分查找/线性查找快照
      ├── Lerp/Slerp 插值
      └── 应用到Transform

```

### 4. AI同步流程

```

远程AI同步
  ├── 接收AI_TRANSFORM_SNAPSHOT（Unreliable）
  ├── NetAiFollower.SetTarget(pos, dir)
  ├── 接收 AI_ANIM_SNAPSHOT（Unreliable）
  ├── NetAiFollower.SetAnim(speed, dirX, dirY, ...)
  └── Update()平滑应用
      ├── 位置 Lerp
      ├── 旋转 Slerp
      ├── 动画参数 Lerp
      └── Animator.SetFloat/SetBool

```

### 5. 对象池使用流程

```

使用对象池发送消息
  ├── writer = NetPacketPool.GetWriter()
  ├── writer.Put(data)
  ├── netManager.SendSmart(writer, op)
  └── NetPacketPool.ReturnWriter(writer)
      ├── writer.Reset()
      └── 归还到ConcurrentBag

```

## 设计模式

### 1. 对象池模式

NetPacketPool 实现对象池，减少 GC 压力：

- 使用 ConcurrentBag 实现线程安全

- 自动限制池大小

- 自动重置对象状态

### 2. 扩展方法模式

大量使用扩展方法增强现有类型：

- NetDataExtensions 扩展 NetDataWriter/NetPacketReader

- NetworkExtensions 扩展 NetManager/NetPeer

- OpPriority 扩展 Op 枚举

### 3. 策略模式

PacketPriority 系统实现策略模式：

- 不同优先级对应不同传输策略

- 自动选择 DeliveryMethod

- 自动分配通道编号

### 4. 组件模式

使用 MonoBehaviour 组件实现功能：

- NetInterpolator: 位置插值

- NetAiFollower: AI 动画跟随

- NetAiTag: AI 标记

- NetAiVisibilityGuard: 可见性管理

### 5. 单例模式

NetPacketPool 使用静态类实现全局对象池。

## 依赖关系

### 内部依赖

- NetworkExtensions 依赖 OpPriority 和 PacketPriority

- NetInterpolator 依赖 ExponentialMovingAverage（Utils 模块）

- LocalHitKillFx 依赖 Main 模块的特效系统

### 外部依赖

- **LiteNetLib**：底层网络传输库

- **Steamworks.NET**：Steam P2P 功能

- **Unity 引擎**：Transform、Animator、MonoBehaviour 等

- **游戏原始代码**：CharacterMainControl、Health 等

## 关键技术点

### 1. Fika 架构插值系统

NetInterpolator 完全参考 Fika 的实现：

- 时间轴系统（Timeline）

- 动态时间缩放（TimeScale）

- EMA 平滑（Exponential Moving Average）

- 追赶/减速机制（Catchup/Slowdown）

- 动态延迟调整（Dynamic Delay）

### 2. 多通道系统

使用 LiteNetLib 的多通道功能避免队头阻塞：

- 通道0: Critical（投票、伤害）

- 通道1: Important（血量、装备）

- 通道2: Normal（NPC、物品）

- 通道3: Frequent（位置、动画）

### 3. 智能传输选择

根据 Op 操作码自动选择最优传输方式：

- 位置更新: Unreliable（可丢弃）

- 伤害事件: ReliableOrdered（必须送达且有序）

- 装备更新: ReliableSequenced（必须送达，只保留最新）

### 4. 数据压缩

虽然当前使用 float（4字节），但架构支持未来压缩：

- Vector3: 12字节（可压缩到6-9字节）

- Quaternion: 16字节（可压缩到4-8字节）

- 方向向量: 可使用球坐标压缩

### 5. 安全性保护

多层安全检查防止崩溃：

- NaN/Infinity 检测

- 零四元数保护

- 异常跳变检测

- 自动归一化

## 性能优化

### 1. 对象池

NetPacketPool 减少 GC 压力：

- 复用 NetDataWriter 对象

- 线程安全的 ConcurrentBag

- 自动限制池大小

### 2. 二分查找

NetInterpolator 在大缓冲区时使用二分查找：

- 小缓冲区（<10）: O(n)线性查找

- 大缓冲区（>=10）: O(log n)二分查找

### 3. 组件缓存

NetAiFollower 缓存组件引用：

- 缓存 Animator、MagicBlend、AnimationControl

- 避免每帧 GetComponent

### 4. 去抖和限流

- 射击去抖（_dedupeShotFrame）

- 近战攻击去抖（LocalMeleeOncePerFrame）

- 同步间隔控制（syncInterval）

### 5. 延迟优化

动态调整插值延迟：

- 根据网络质量自动调整

- 平衡延迟和稳定性

- 追赶/减速机制保持同步

## 网络质量监控

### 网络统计信息

NetInterpolator 提供详细的网络质量统计：

- 平均延迟和标准差

- 包间隔抖动

- 时间漂移

- 缓冲区大小

- 质量评分（0-100）

### 质量评分算法

```csharp
int score = (latencyScore + jitterScore + driftScore) / 3

- latencyScore = max(0, 100 - avgDelay * 1000)

- jitterScore = max(0, 100 - jitter * 2000)

- driftScore = max(0, 100 - abs(drift) * 500)

```

### 状态描述

- 优秀: 80-100分

- 良好: 60-79分

- 一般: 40-59分

- 较差: 20-39分

- 很差: 0-19分

## 使用示例

### 智能发送消息

```csharp
var writer = NetPacketPool.GetWriter();
try
{
    writer.Put((byte)Op.POSITION_UPDATE);
    writer.Put(playerId);
    writer.PutVector3(position);
    writer.PutQuaternion(rotation);
    netManager.SendSmart(writer, Op.POSITION_UPDATE);
}
finally
{
    NetPacketPool.ReturnWriter(writer);
}

```

### 使用插值器

```csharp
var interpolator = NetInterpUtil.Attach(remotePlayerObject);
interpolator.Push(position, rotation, timestamp);

```

### 配置插值参数

```csharp
interpolator.interpolationBackTime = 0.12f;  // 120ms 回看
interpolator.enableEmaSmoothing = true;      // 启用 EMA 平滑
interpolator.enableDynamicDelay = true;      // 动态延迟
interpolator.enableCatchupSlowdown = true;   // 追赶/减速

```

### 获取网络统计

```csharp
var stats = interpolator.GetNetworkStats();
Debug.Log($"质量: {stats.GetStatusDescription()}, 评分: {stats.GetQualityScore()}");
Debug.Log($"延迟: {stats.AverageDelay:F3}s, 抖动: {stats.PacketIntervalJitter:F3}s");

```

## 总结

Net模块是整个联机系统的网络通信基础，通过精心设计的架构实现了：

- **高效的数据传输**：智能选择传输方式，减少90%带宽

- **平滑的状态同步**：Fika 架构插值系统，提供流畅体验

- **强大的网络优化**：对象池、多通道、动态调整等技术

- **完善的质量监控**：实时统计网络质量，自动优化参数

- **灵活的扩展性**：扩展方法和组件化设计，易于维护

该模块为上层业务逻辑提供了高性能、低延迟的网络通信基础设施，是实现流畅多人合作游戏体验的关键。
