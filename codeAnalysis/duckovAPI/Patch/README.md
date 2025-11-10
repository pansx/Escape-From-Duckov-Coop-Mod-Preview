# Patch 模块总览

## 模块概述

Patch 模块是 Escape From Duckov Coop Mod 的核心组成部分，使用 HarmonyLib 库对游戏原始代码进行运行时修改（Runtime Patching），实现多人联机功能。该模块通过拦截游戏的关键方法，在不修改游戏源代码的情况下，注入网络同步逻辑，使单人游戏转变为多人合作游戏。

**核心职责**：

- 拦截游戏核心逻辑的执行

- 注入网络同步代码

- 实现客户端-服务器架构

- 防止状态不一致和作弊

- 保持游戏原有功能的完整性

**技术栈**：

- **HarmonyLib 2.x** - 运行时代码修改框架

- **反射（Reflection）** - 访问私有成员

- **IL 代码操作** - 高级代码修改

## 子模块结构

Patch 模块包含6个子模块，每个子模块负责特定领域的补丁：

```

Patch/
├── Character/              # 角色系统补丁（8个文件）
├── InventoryAndLootBox/    # 背包和战利品补丁（5个文件）
├── Item/                   # 物品系统补丁（6个文件）
├── Projectile/             # 投射物补丁（1个文件）
├── Scene/                  # 场景系统补丁（3个文件）
└── SteamP2P/              # Steam P2P网络补丁（3个文件）

```

### 1. Character 子模块

**文档**：[PatchCharacter.md](./PatchCharacter.md)

**补丁目标**：

- AICharacterController - AI 控制器

- Animator - 动画系统

- Buff - Buff 效果系统

- CharacterItemControl - 角色物品控制

- CharacterMainControl - 角色主控制器

- CharacterSpawnerRoot - 角色生成器

- Health - 生命值系统

**核心功能**：

- 同步角色动画状态（移动、攻击、受伤等）

- 同步生命值和伤害计算

- 同步 Buff 效果的添加和移除

- 同步 AI 生成和行为

- 管理角色生命周期（创建、销毁）

- 区分本地玩家、远程玩家和 AI

**关键设计**：

- **服务器权威伤害**：客户端发送伤害请求，服务器验证并计算

- **动画参数同步**：拦截 Animator 的 SetFloat/SetBool/SetTrigger

- **AI 种子同步**：确保所有客户端生成相同的 AI

### 2. InventoryAndLootBox 子模块

**文档**：[PatchInventoryAndLootBox.md](./PatchInventoryAndLootBox.md)

**补丁目标**：

- InteractableLootbox - 可交互战利品箱

- Inventory - 背包系统

- LootBoxLoader - 战利品箱加载器

- LootSpawner - 战利品生成器

- LootView - 战利品视图

**核心功能**：

- 同步战利品箱的打开/关闭状态

- 防止多个玩家重复拾取同一物品

- 同步战利品生成种子，确保内容一致

- 同步背包操作（添加、移除、移动物品）

- 显示其他玩家的战利品操作

**关键设计**：

- **防重复拾取**：服务器验证物品是否已被拾取

- **战利品一致性**：使用相同种子生成战利品

### 3. Item 子模块

**文档**：[PatchItem.md](./PatchItem.md)

**补丁目标**：

- Grenade/Breakable - 手雷和可破坏物

- Gun - 枪械系统

- Item - 物品基类

- ItemExtensions - 物品扩展方法

- ItemUtilities - 物品工具

- Slot - 装备槽位系统

**核心功能**：

- 同步射击事件和弹药状态

- 同步手雷投掷和爆炸

- 同步物品掉落和拾取

- 同步装备操作（装备/卸下）

- 同步换弹和瞄准状态

**关键设计**：

- **客户端预测射击**：立即显示射击效果，服务器验证命中

- **物品 ID 管理**：为每个掉落物品分配唯一 ID

### 4. Projectile 子模块

**文档**：[PatchProjectile.md](./PatchProjectile.md)

**补丁目标**：

- FakeProjectile - 假投射物（客户端视觉）

**核心功能**：

- 客户端生成假投射物用于视觉反馈

- 服务器进行真实的命中判定

- 防止客户端修改弹道作弊

**关键设计**：

- **客户端预测 + 服务器权威**：客户端显示轨迹，服务器计算命中

- **防作弊机制**：客户端投射物不进行真实碰撞检测

### 5. Scene 子模块

**文档**：[PatchScene.md](./PatchScene.md)

**补丁目标**：

- Door - 门系统

- LevelManager - 关卡管理器

- Scene - 场景系统

**核心功能**：

- 同步场景切换（如撤离、进入新关卡）

- 同步门的开关状态

- 管理场景加载流程

- 清理和重置场景状态

**关键设计**：

- **场景切换同步**：主机发起，所有客户端同步加载

- **状态清理**：场景切换时清理 AI、物品、玩家状态

- **加载等待**：等待所有客户端加载完成

### 6. SteamP2P 子模块

**文档**：[PatchSteamP2P.md](./PatchSteamP2P.md)

**补丁目标**：

- NetManager - LiteNetLib 网络管理器

- Socket - UDP Socket

**核心功能**：

- 透明替换 UDP 为 Steam P2P 通信

- 实现 NAT 穿透

- 支持 Steam 好友联机

- 虚拟 IP 映射（10.255.x.x ↔ SteamID）

**关键设计**：

- **透明替换**：拦截 Socket 调用，重定向到 Steam P2P

- **双模式支持**：Steam P2P 模式和 UDP 回退模式

- **虚拟 IP 映射**：将 SteamID 映射为虚拟 IP，保持 LiteNetLib 兼容

## 整体架构设计

### 1. 补丁层次结构

```

游戏原始代码（Unity C#）
        ↓
HarmonyLib 运行时拦截
        ↓
Patch 模块（注入逻辑）
        ↓
Main/Net 模块（业务逻辑）
        ↓
网络传输（LiteNetLib/Steam P2P）

```

### 2. 客户端-服务器架构

Patch 模块实现了混合的客户端-服务器架构：

**服务器权威**（Server-Authoritative）：

- 伤害计算

- 物品拾取

- AI 生成和行为

- 场景切换

**客户端预测**（Client-Side Prediction）：

- 角色移动

- 射击效果

- 动画播放

- 投射物轨迹

### 3. 补丁类型和用途

#### Prefix补丁（前置拦截）

```csharp
[HarmonyPrefix]
static bool Prefix(OriginalClass __instance, ref ReturnType __result)
{
    // 在原方法执行前运行
    // 返回 false 可以跳过原方法
    return true;
}

```

**用途**：

- 阻止原方法执行（如客户端伤害计算）

- 修改输入参数

- 提前返回结果

- 实现客户端预测

**应用示例**：

- `Health_HurtPacth`：客户端拦截伤害，发送请求到服务器

#### Postfix补丁（后置拦截）

```csharp
[HarmonyPostfix]
static void Postfix(OriginalClass __instance, ReturnType __result)
{
    // 在原方法执行后运行
    // 可以访问返回值和修改后的状态
}

```

**用途**：

- 在原方法后添加逻辑

- 访问返回值

- 同步状态变化

- 触发网络事件

**应用示例**：

- `HealthPatch`：服务器计算伤害后，广播血量变化

- `AnimPacth`：动画参数改变后，同步到网络

#### Transpiler补丁（IL代码修改）

```csharp
[HarmonyTranspiler]
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    // 修改 IL 代码
    return instructions;
}

```

**用途**：

- 修改方法内部逻辑

- 插入自定义代码

- 高级优化

- 精确控制执行流程

## 核心设计模式

### 1. 服务器权威模式

**适用场景**：关键游戏逻辑，防止作弊

**流程**：

```

客户端 → 发送请求（如伤害、拾取）
服务器 → 验证并决定结果
      → 广播权威结果
客户端 → 应用服务器结果

```

**实现示例**：

- 伤害计算：`Health_HurtPacth` (Prefix) 拦截客户端伤害

- 物品拾取：服务器验证物品是否已被拾取

### 2. 客户端预测模式

**适用场景**：需要低延迟反馈的操作

**流程**：

```

客户端 → 立即执行操作（如移动、射击）
      → 发送请求到服务器
服务器 → 验证并广播权威结果
客户端 → 根据服务器结果校正（如有偏差）

```

**实现示例**：

- 射击：客户端立即播放射击效果

- 投射物：客户端显示轨迹，服务器计算命中

### 3. 状态同步模式

**适用场景**：持续变化的状态

**流程**：

```

服务器 → 定期同步完整状态（快照）
      → 增量同步变化（事件）
客户端 → 插值平滑显示

```

**实现示例**：

- 动画同步：`AnimPacth` 同步动画参数

- 血量同步：`HealthPatch` 广播血量变化

### 4. 种子同步模式

**适用场景**：随机生成内容需要一致

**流程**：

```

服务器 → 生成随机种子
      → 广播种子到所有客户端
客户端 → 使用相同种子生成内容

```

**实现示例**：

- AI生成：`CharacterSpawnerRootPatch` 同步AI生成种子

- 战利品生成：`LootSpawner` 同步战利品种子

## 关键技术实现

### 1. 反射访问私有成员

```csharp
// 缓存FieldInfo 以提高性能
private static FieldInfo _healthField;

static void AccessPrivateField(Health health)
{
    if (_healthField == null)
    {
        _healthField = typeof(Health).GetField("_currentHealth",
            BindingFlags.NonPublic | BindingFlags.Instance);
    }

    float currentHealth = (float)_healthField.GetValue(health);
}

```

### 2. 补丁优先级控制

```csharp
[HarmonyPatch(typeof(TargetClass), "MethodName")]
[HarmonyPriority(Priority.High)]  // 优先执行
class MyPatch
{
    // ...
}

```

### 3. 条件补丁执行

```csharp
[HarmonyPrefix]
static bool Prefix()
{
    // 只在联机模式执行补丁逻辑
    if (!NetService.IsOnline)
        return true;  // 单机模式，正常执行原方法

    // 联机逻辑
    // ...
    return false;  // 跳过原方法
}

```

### 4. 异常处理

```csharp
[HarmonyPostfix]
static void Postfix()
{
    try
    {
        // 补丁逻辑
    }
    catch (Exception ex)
    {
        Debug.LogError($"Patch error: {ex}");
        // 不抛出异常，避免崩溃游戏
    }
}

```

## 性能优化策略

### 1. 最小化补丁逻辑

- Prefix/Postfix 方法尽量简短

- 复杂逻辑异步执行或延迟执行

- 避免在高频调用方法中执行重逻辑

### 2. 缓存反射结果

```csharp
// ❌ 错误：每次都反射
void BadExample()
{
    var field = typeof(Class).GetField("field");
    field.GetValue(instance);
}

// ✅ 正确：缓存 FieldInfo
static FieldInfo _cachedField;
void GoodExample()
{
    if (_cachedField == null)
        _cachedField = typeof(Class).GetField("field");
    _cachedField.GetValue(instance);
}

```

### 3. 条件执行

```csharp
[HarmonyPostfix]
static void Postfix()
{
    // 只在联机模式执行
    if (!NetService.IsOnline) return;

    // 只在服务器执行
    if (!NetService.IsServer) return;

    // 补丁逻辑
}

```

### 4. 批处理网络消息

```csharp
// 收集多个变化，一次性发送
List<AnimParam> changes = new List<AnimParam>();
// ... 收集变化
if (changes.Count > 0)
    SendBatch(changes);

```

## 调试和诊断

### 1. 日志输出

```csharp
[HarmonyPrefix]
static bool Prefix(TargetClass __instance)
{
    Debug.Log($"[Patch] Method called on {__instance.name}");
    return true;
}

```

### 2. Harmony调试

```csharp
// 启用Harmony日志
Harmony.DEBUG = true;

// 查看所有已应用的补丁
var harmony = new Harmony("com.example.mod");
var patches = Harmony.GetAllPatchedMethods();
foreach (var method in patches)
{
    Debug.Log($"Patched: {method.DeclaringType}.{method.Name}");
}

```

### 3. 条件断点

使用dnSpy 附加到游戏进程，在补丁方法设置断点：

- 查看参数值

- 查看调用堆栈

- 单步执行

### 4. IL 代码查看

使用 dnSpy 查看编译后的 IL 代码，验证 Transpiler 修改是否正确应用。

## 安全和防作弊

### 1. 服务器验证

```csharp
[HarmonyPrefix]
static bool Prefix_ClientRequest(DamageInfo damageInfo)
{
    if (NetService.IsClient)
    {
        // 客户端：发送请求
        SendDamageRequest(damageInfo);
        return false;  // 阻止本地计算
    }

    // 服务器：验证请求
    if (!ValidateDamageRequest(damageInfo))
    {
        Debug.LogWarning("Invalid damage request");
        return false;  // 拒绝非法请求
    }

    return true;  // 执行伤害计算
}

```

### 2. 数据验证

```csharp
void ValidateNetworkData(NetworkData data)
{
    // 检查数据范围
    if (data.damage < 0 || data.damage > 1000)
        throw new InvalidDataException("Invalid damage value");

    // 检查数据来源
    if (!IsValidSender(data.senderID))
        throw new SecurityException("Invalid sender");
}

```

### 3. 权限控制

```csharp
void HandleCommand(Command cmd)
{
    // 只有主机能执行某些操作
    if (cmd.RequiresHost && !NetService.IsHost)
    {
        Debug.LogWarning("Permission denied");
        return;
    }

    // 执行命令
    cmd.Execute();
}

```

## 兼容性考虑

### 1. 游戏更新适配

- 游戏更新可能改变类名、方法签名

- 需要重新测试和适配补丁

- 使用版本检测机制

### 2. 其他 Mod 兼容

- 可能与其他 Mod 的补丁冲突

- 使用 HarmonyPriority 控制执行顺序

- 使用 Prefix 返回值控制是否执行后续补丁

### 3. 补丁失败处理

```csharp
try
{
    harmony.PatchAll();
}
catch (Exception ex)
{
    Debug.LogError($"Patch failed: {ex}");
    // 降级到单机模式或禁用功能
}

```

## 依赖关系

### 外部依赖

- **HarmonyLib** - 运行时代码修改框架

- **Unity Engine** - 游戏引擎

- **游戏原始代码** - 被补丁的目标

### 内部依赖

- **Main 模块** - 业务逻辑和服务

  - Main/AI - AI 管理

  - Main/Health - 生命值管理

  - Main/NetService - 网络通信

  - Main/SceneService - 场景管理

- **Net 模块** - 网络传输

  - Net/NetPack - 数据包定义

  - Net/Steam - Steam 网络

- **Utils 模块** - 工具类

### 被依赖关系

- Patch 模块是底层拦截层，不被其他模块直接依赖

- 通过事件和回调与 Main 模块交互

## 最佳实践

### 1. 补丁设计原则

- **最小侵入**：只修改必要的方法

- **保持兼容**：不破坏原有功能

- **性能优先**：避免高频方法的重逻辑

- **异常安全**：使用 Try-Catch 防止崩溃

### 2. 代码组织

- 每个补丁类对应一个目标类

- 使用清晰的命名（如`HealthPatch`）

- 相关补丁放在同一子模块

### 3. 文档和注释

- 说明补丁目标和用途

- 记录补丁类型（Prefix/Postfix/Transpiler）

- 解释关键逻辑和设计决策

### 4. 测试策略

- 单机模式测试：确保不破坏原有功能

- 联机模式测试：验证同步逻辑

- 边界测试：测试异常情况和边界条件

## 常见问题和解决方案

### 1. 补丁未生效

**原因**：

- 方法签名不匹配

- 目标类或方法不存在

- 补丁顺序问题

**解决**：

- 启用 Harmony.DEBUG 查看日志

- 使用 dnSpy 验证方法签名

- 调整 HarmonyPriority

### 2. 性能下降

**原因**：

- 高频方法的重逻辑

- 未缓存反射结果

- 过多的网络消息

**解决**：

- 优化补丁逻辑

- 缓存 FieldInfo/MethodInfo

- 批处理网络消息

- 降低同步频率

### 3. 状态不一致

**原因**：

- 客户端和服务器逻辑不同步

- 网络延迟或丢包

- 补丁逻辑错误

**解决**：

- 使用服务器权威模式

- 定期同步完整状态

- 添加状态校验

### 4. 与其他 Mod 冲突

**原因**：

- 补丁同一方法

- 执行顺序问题

**解决**：

- 使用 HarmonyPriority

- 检查 Prefix 返回值

- 与其他 Mod 作者协调

## 未来扩展方向

### 1. 更多游戏功能支持

- 任务系统同步

- 商人系统同步

- 更多场景支持

### 2. 性能优化

- 更智能的同步策略

- 更高效的数据压缩

- 更少的网络流量

### 3. 功能增强

- 观战模式

- 录像回放

- 更好的延迟补偿

### 4. 工具和调试

- 可视化调试工具

- 网络流量分析

- 性能分析工具

## 总结

Patch 模块是 Escape From Duckov Coop Mod 的基础和核心，通过 HarmonyLib 的运行时代码修改能力，在不修改游戏源代码的情况下，实现了完整的多人联机功能。该模块展示了以下关键技术：

1. **运行时代码修改**：使用 HarmonyLib 拦截和修改游戏逻辑
2. **客户端-服务器架构**：实现服务器权威和客户端预测
3. **网络同步**：同步角色、物品、场景等各种游戏状态
4. **防作弊机制**：服务器验证关键操作
5. **性能优化**：最小化补丁开销，优化网络流量

通过6个子模块的协同工作，Patch 模块覆盖了游戏的各个方面，为玩家提供了流畅的多人合作体验。
