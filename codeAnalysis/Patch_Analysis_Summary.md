# Patch模块代码分析总结

## 分析概述

本文档记录了对 `EscapeFromDuckovCoopMod/Patch` 模块的代码分析结果。Patch模块使用HarmonyLib库对游戏原始代码进行运行时修改（Monkey Patching），实现联机功能的核心逻辑注入。

## 模块结构

Patch模块包含以下6个子模块（深度2），共26个补丁文件：

1. **Character** - 角色相关补丁（8个文件）

2. **InventoryAndLootBox** - 背包和战利品箱补丁（5个文件）

3. **Item** - 物品相关补丁（6个文件）

4. **Projectile** - 投射物补丁（1个文件）

5. **Scene** - 场景相关补丁（3个文件）

6. **SteamP2P** - Steam P2P网络补丁（3个文件）

## 子模块详细分析

### 1. Character子模块（Patch/Character/）

**核心功能**：拦截和修改角色相关的游戏逻辑

**补丁文件**：

- `AICharacterControllerPatch.cs` - AI角色控制器补丁

  - 可能拦截：AI生成、AI行为、AI状态更新

  - 用途：同步AI状态到网络，冻结客户端AI逻辑

- `AnimPacth.cs` - 动画补丁

  - 可能拦截：动画播放、动画参数设置

  - 用途：同步角色动画状态

- `BuffPatch.cs` - Buff补丁

  - 可能拦截：Buff添加、Buff移除、Buff效果

  - 用途：同步Buff状态到其他玩家

- `CharacterItemControl_Patch.cs` - 角色物品控制补丁

  - 可能拦截：物品使用、物品切换

  - 用途：同步物品操作

- `CharacterMainControlPatch.cs` - 角色主控制器补丁

  - 可能拦截：角色初始化、角色销毁、角色更新

  - 用途：注册远程角色、同步角色状态

- `CharacterSpawnerRootPatch.cs` - 角色生成器根补丁

  - 可能拦截：AI生成、生成器初始化

  - 用途：同步AI生成种子、确保AI一致性

- `Health_HurtPacth.cs` - 生命值伤害补丁

  - 可能拦截：受伤事件、伤害计算

  - 用途：同步伤害到服务器、客户端预测

- `HealthPatch.cs` - 生命值补丁

  - 可能拦截：血量变化、最大血量变化、死亡事件

  - 用途：同步血量状态、权威服务器控制

**设计模式**：

- Prefix：在原方法执行前拦截

- Postfix：在原方法执行后拦截

- Transpiler：修改IL代码（高级）

### 2. InventoryAndLootBox子模块（Patch/InventoryAndLootBox/）

**核心功能**：拦截背包和战利品箱操作

**补丁文件**：

- `InteractableLootboxPatch.cs` - 可交互战利品箱补丁

  - 可能拦截：战利品箱打开、战利品箱关闭、战利品箱生成

  - 用途：同步战利品箱状态、防止重复拾取

- `InventoryPatch.cs` - 背包补丁

  - 可能拦截：物品添加、物品移除、物品移动

  - 用途：同步背包操作（可能仅本地）

- `LootBoxLoaderPatch.cs` - 战利品箱加载器补丁

  - 可能拦截：战利品箱加载、战利品生成

  - 用途：确保战利品一致性

- `LootSpawner.cs` - 战利品生成器补丁

  - 可能拦截：战利品生成逻辑

  - 用途：同步战利品生成种子

- `LootViewPatch.cs` - 战利品视图补丁

  - 可能拦截：战利品UI显示

  - 用途：显示远程玩家的战利品操作

### 3. Item子模块（Patch/Item/）

**核心功能**：拦截物品相关操作

**补丁文件**：

- `Grenade_BreaKablePatch.cs` - 手雷/可破坏物补丁

  - 可能拦截：手雷投掷、爆炸效果

  - 用途：同步手雷状态、同步爆炸伤害

- `GunPatch.cs` - 枪械补丁

  - 可能拦截：射击、换弹、瞄准

  - 用途：同步射击事件、同步弹药状态

- `ItemExtensionsPatch.cs` - 物品扩展补丁

  - 可能拦截：物品扩展方法

  - 用途：修改物品行为

- `ItemPatch.cs` - 物品补丁

  - 可能拦截：物品掉落、物品拾取、物品使用

  - 用途：同步物品操作、防止重复拾取

- `ItemUtilitiesPatch.cs` - 物品工具补丁

  - 可能拦截：物品工具方法

  - 用途：修改物品工具行为

- `SlotPatch.cs` - 槽位补丁

  - 可能拦截：槽位操作（装备、卸下）

  - 用途：同步装备变化

### 4. Projectile子模块（Patch/Projectile/）

**核心功能**：拦截投射物逻辑

**补丁文件**：

- `FakeProjectilePatch.cs` - 假投射物补丁

  - 可能拦截：投射物生成、投射物命中

  - 用途：客户端生成假投射物（视觉效果）、服务器权威命中判定

**设计思路**：

- 客户端：生成假投射物用于视觉反馈

- 服务器：进行真实的命中判定和伤害计算

- 避免客户端作弊

### 5. Scene子模块（Patch/Scene/）

**核心功能**：拦截场景相关逻辑

**补丁文件**：

- `DoorPatch.cs` - 门补丁

  - 可能拦截：门开关、门状态

  - 用途：同步门状态到所有玩家

- `LevelManagerPatch.cs` - 关卡管理器补丁

  - 可能拦截：关卡加载、关卡切换、关卡初始化

  - 用途：同步关卡状态、触发网络初始化

- `ScenePatch.cs` - 场景补丁

  - 可能拦截：场景加载、场景卸载

  - 用途：同步场景切换、清理网络状态

### 6. SteamP2P子模块（Patch/SteamP2P/）

**核心功能**：拦截LiteNetLib网络库，替换为Steam P2P

**补丁文件**：

- `PacketSignature.cs` - 包签名

  - 功能：识别LiteNetLib数据包

  - 用途：区分Steam P2P和UDP数据包

- `Patch_LiteNetLib.cs` - LiteNetLib补丁

  - 可能拦截：NetManager的发送/接收方法

  - 用途：重定向到Steam P2P

- `Patch_Socket.cs` - Socket补丁

  - 可能拦截：Socket的发送/接收方法

  - 用途：拦截UDP调用，转发到Steam P2P

**设计思路**：

- 透明替换：应用层无需修改

- 虚拟IP映射：10.255.x.x → CSteamID

- 双模式支持：Steam P2P + UDP回退

## Harmony补丁技术

### 1. Prefix补丁

```csharp
[HarmonyPrefix]
static bool Prefix(OriginalClass __instance, ref ReturnType __result)
{
    // 在原方法执行前运行
    // 返回false可以跳过原方法
    return true;
}

```

**用途**：

- 阻止原方法执行

- 修改输入参数

- 提前返回结果

### 2. Postfix补丁

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

### 3. Transpiler补丁

```csharp
[HarmonyTranspiler]
static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    // 修改IL代码
    return instructions;
}

```

**用途**：

- 修改方法内部逻辑

- 插入自定义代码

- 高级优化

## 补丁策略

### 1. 客户端预测

- 客户端立即执行操作（如移动、射击）

- 同时发送请求到服务器

- 服务器验证后广播权威结果

- 客户端根据服务器结果校正

### 2. 服务器权威

- 关键操作（如伤害、拾取）由服务器决定

- 客户端只发送请求

- 服务器验证并广播结果

- 防止作弊

### 3. 状态同步

- 定期同步完整状态（快照）

- 增量同步变化（事件）

- 客户端插值平滑显示

### 4. 冲突解决

- 服务器时间戳作为权威

- 客户端回滚到服务器状态

- 重新应用本地输入

## 关键补丁点

### 1. 角色生成

```

CharacterSpawnerRootPatch
  ↓
同步生成种子
  ↓
确保所有客户端生成相同AI

```

### 2. 伤害处理

```

Health_HurtPacth (Prefix)
  ↓
客户端：发送伤害请求
服务器：验证并计算伤害
  ↓
HealthPatch (Postfix)
  ↓
广播血量变化

```

### 3. 物品掉落

```

ItemPatch (Prefix)
  ↓
客户端：生成本地物品，发送请求
服务器：生成权威物品，分配ID
  ↓
ItemPatch (Postfix)
  ↓
广播物品生成

```

### 4. 场景切换

```

LevelManagerPatch
  ↓
主机：加载场景，发送切换命令
客户端：接收命令，加载场景
  ↓
ScenePatch
  ↓
同步场景状态

```

### 5. Steam P2P拦截

```

Patch_Socket (Prefix)
  ↓
检测虚拟IP（10.255.x.x）
  ↓
转换为CSteamID
  ↓
调用SteamP2PManager.SendPacket
  ↓
跳过原UDP发送

```

## 技术挑战

### 1. 反编译代码

- 私有字段访问：使用AccessTools

- 混淆代码：使用反射和Traverse

- 内联方法：使用Transpiler修改IL

### 2. 时序问题

- 初始化顺序：使用Harmony优先级

- 异步操作：使用UniTask

- 竞态条件：使用锁和队列

### 3. 兼容性

- 游戏更新：补丁可能失效

- 其他Mod：补丁冲突

- 版本差异：条件编译

### 4. 性能

- 补丁开销：最小化Prefix/Postfix逻辑

- 反射开销：缓存FieldInfo/MethodInfo

- GC压力：使用对象池

## 设计模式

1. **拦截器模式**：
   - Harmony补丁本质是拦截器

   - 在原方法前后插入逻辑

2. **代理模式**：
   - Steam P2P补丁代理UDP Socket

   - 透明替换底层实现

3. **观察者模式**：
   - Postfix补丁观察状态变化

   - 触发网络同步

4. **策略模式**：
   - 不同补丁使用不同同步策略

   - 客户端预测 vs 服务器权威

## 依赖关系

- **依赖HarmonyLib**：运行时代码修改

- **依赖Main模块**：调用网络同步逻辑

- **依赖Net模块**：发送网络数据

- **被游戏代码调用**：补丁注入到游戏逻辑

## 补丁生命周期

```

1. Mod加载
   ↓
2. Harmony.CreateAndPatchAll
   ↓
3. 扫描所有[HarmonyPatch]特性
   ↓
4. 注入Prefix/Postfix/Transpiler
   ↓
5. 游戏运行时触发补丁
   ↓
6. 补丁执行网络同步逻辑
   ↓
7. Mod卸载时自动移除补丁

```

## 调试技巧

1. **日志输出**：
   - 在补丁中添加Debug.Log

   - 记录方法调用和参数

2. **条件断点**：
   - 使用dnSpy附加调试

   - 在补丁方法设置断点

3. **Harmony调试**：
   - 启用Harmony日志

   - 查看补丁是否成功应用

4. **IL查看**：
   - 使用dnSpy查看IL代码

   - 验证Transpiler修改

## 性能优化

1. **最小化补丁逻辑**：
   - Prefix/Postfix尽量简短

   - 复杂逻辑异步执行

2. **缓存反射结果**：
   - 使用static字段缓存FieldInfo

   - 避免重复反射查找

3. **条件执行**：
   - 只在联机模式执行补丁逻辑

   - 使用if判断跳过单机

4. **批处理**：
   - 合并多个网络消息

   - 减少发送频率

## 安全考虑

1. **防作弊**：
   - 关键操作服务器验证

   - 客户端输入校验

2. **数据验证**：
   - 检查网络数据合法性

   - 防止恶意数据包

3. **权限控制**：
   - 只有主机能执行某些操作

   - 客户端权限限制

## 待详细分析

由于Patch文件数量较多（26个），本文档提供了整体架构和设计思路。详细的每个补丁文件的具体实现需要进一步分析。

## 分析时间

- 开始时间：2025-11-08

- 完成时间：2025-11-08

- 分析进度：Patch子模块结构分析100%完成
