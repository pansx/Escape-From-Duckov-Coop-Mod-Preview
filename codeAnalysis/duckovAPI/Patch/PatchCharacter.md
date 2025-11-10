# Patch/Character 模块 API 文档

## 模块概述

Character 补丁模块使用 HarmonyLib 对游戏角色相关代码进行运行时修改，实现角色状态、动画、生命值、Buff 等的网络同步。这是联机功能的核心补丁模块之一。

**核心职责**：

- 拦截角色初始化和销毁

- 同步角色动画状态

- 同步生命值和伤害

- 同步 Buff 效果

- 同步 AI 生成和行为

## 补丁目标

本模块对以下游戏类进行补丁：

- `AICharacterController` - AI角色控制器

- `Animator` - 动画控制器

- `Buff` - Buff系统

- `CharacterItemControl` - 角色物品控制

- `CharacterMainControl` - 角色主控制器

- `CharacterSpawnerRoot` - 角色生成器

- `Health` - 生命值系统

## 文件列表

| 文件名 | 补丁目标 | 说明 |

|--------|---------|------|
|---|---|---|

| `AnimPacth.cs` | Animator | 动画补丁 |
|---|---|---|

| `CharacterItemControl_Patch.cs` | CharacterItemControl | 物品控制补丁 |
|---|---|---|

| `CharacterSpawnerRootPatch.cs` | CharacterSpawnerRoot | 生成器补丁 |
|---|---|---|

| `HealthPatch.cs` | Health | 生命值补丁 |

## 核心补丁说明

### AICharacterControllerPatch

**补丁目标**：AI 角色控制器

**可能拦截的方法**：

- AI 生成事件

- AI 行为更新

- AI 状态变化

**用途**：

- 同步 AI 状态到网络

- 冻结客户端 AI 逻辑（由服务器控制）

- 注册 AI 到 AITool.aiByID

### AnimPacth

**补丁目标**：动画控制器

**可能拦截的方法**：

- `SetFloat` - 设置浮点参数

- `SetInteger` - 设置整数参数

- `SetBool` - 设置布尔参数

- `SetTrigger` - 触发动画

**用途**：

- 同步角色动画参数

- 同步 AI 动画状态

- 防止客户端本地动画覆盖网络动画

### BuffPatch

**补丁目标**：Buff 系统

**可能拦截的方法**：

- Buff 添加

- Buff 移除

- Buff 效果应用

**用途**：

- 同步 Buff 状态到其他玩家

- 服务器权威 Buff 管理

- 防止 Buff 重复应用

### CharacterItemControl_Patch

**补丁目标**：角色物品控制

**可能拦截的方法**：

- 物品使用

- 物品切换

- 物品装备

**用途**：

- 同步物品操作

- 同步装备变化

- 同步武器切换

### CharacterMainControlPatch

**补丁目标**：角色主控制器

**可能拦截的方法**：

- `Awake` / `Start` - 初始化

- `OnDestroy` - 销毁

- `Update` - 更新

**用途**：

- 注册远程角色

- 同步角色状态

- 管理角色生命周期

- 区分本地玩家、远程玩家和 AI

### CharacterSpawnerRootPatch

**补丁目标**：角色生成器根

**可能拦截的方法**：

- AI 生成方法

- 生成器初始化

**用途**：

- 同步 AI 生成种子

- 确保所有客户端生成相同 AI

- 使用稳定 ID 算法（StableRootID）

### Health_HurtPacth

**补丁目标**：Health.Hurt 方法

**补丁类型**：Prefix（前置拦截）

**用途**：

- 拦截伤害事件

- 客户端：发送伤害请求到服务器

- 服务器：验证并计算伤害

- 防止客户端作弊

**关键逻辑**：

```csharp
[HarmonyPrefix]
static bool Prefix(Health __instance, DamageInfo damageInfo)
{
    if (IsClient)
    {
        // 发送伤害请求到服务器
        SendDamageRequest(damageInfo);
        return false;  // 阻止本地伤害计算
    }
    return true;  // 服务器正常执行
}

```

### HealthPatch

**补丁目标**：Health 类

**可能拦截的方法**：

- `SetHealth` - 设置血量

- `get_MaxHealth` - 获取最大血量

- `OnHealthChange` - 血量变化事件

- `OnDead` - 死亡事件

**补丁类型**：Postfix（后置拦截）

**用途**：

- 同步血量变化

- 服务器权威血量控制

- 广播血量到所有客户端

- 处理死亡事件

**关键逻辑**：

```csharp
[HarmonyPostfix]
static void Postfix_SetHealth(Health __instance)
{
    if (IsServer)
    {
        // 广播血量变化
        BroadcastHealthChange(__instance);
    }
}

```

## Harmony补丁技术

### 1. Prefix补丁

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

- 阻止原方法执行

- 修改输入参数

- 提前返回结果

- 实现客户端预测

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

- 触发网络事件

### 3. Transpiler补丁

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

## 补丁策略

### 1. 客户端预测

```

客户端 → 立即执行操作（如移动、射击）
      → 发送请求到服务器
服务器 → 验证并广播权威结果
客户端 → 根据服务器结果校正

```

### 2. 服务器权威

```

客户端 → 只发送请求（如伤害、拾取）
服务器 → 验证并决定结果
      → 广播权威结果
客户端 → 应用服务器结果

```

### 3. 状态同步

```

服务器 → 定期同步完整状态（快照）
      → 增量同步变化（事件）
客户端 → 插值平滑显示

```

## 关键流程

### 角色生成流程

```

CharacterSpawnerRootPatch (Postfix)
  ↓
服务器：生成 AI → 计算种子 → 广播种子
客户端：接收种子 → 使用相同种子生成 AI
  ↓
CharacterMainControlPatch (Postfix)
  ↓
注册AI到AITool.aiByID

```

### 伤害处理流程

```

Health_HurtPacth (Prefix)
  ↓
客户端：拦截 → 发送伤害请求 → return false
服务器：正常执行 → 计算伤害 → return true
  ↓
HealthPatch (Postfix)
  ↓
服务器：广播血量变化
客户端：接收并应用血量

```

### 动画同步流程

```

AnimPacth (Postfix)
  ↓
服务器：收集动画参数 → 广播
客户端：接收 → 应用到Animator

```

## 依赖关系

**依赖库**：

- HarmonyLib - 运行时代码修改

**依赖模块**：

- Main/AI - AI 管理

- Main/Health - 生命值管理

- Main/NetService - 网络通信

**被依赖模块**：

- 游戏原始代码（被补丁的目标）

## 性能优化

1. **最小化补丁逻辑**：
   - Prefix/Postfix 尽量简短

   - 复杂逻辑异步执行

2. **缓存反射结果**：
   - 使用 static 字段缓存 FieldInfo

   - 避免重复反射查找

3. **条件执行**：
   - 只在联机模式执行补丁逻辑

   - 使用 if 判断跳过单机

4. **批处理**：
   - 合并多个网络消息

   - 减少发送频率

## 调试技巧

1. **日志输出**：
   - 在补丁中添加 Debug.Log

   - 记录方法调用和参数

2. **条件断点**：
   - 使用 dnSpy 附加调试

   - 在补丁方法设置断点

3. **Harmony 调试**：
   - 启用 Harmony 日志

   - 查看补丁是否成功应用

4. **IL 查看**：
   - 使用 dnSpy 查看 IL 代码

   - 验证 Transpiler 修改

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

## 注意事项

1. **补丁顺序**：
   - 使用 HarmonyPriority 控制顺序

   - 避免补丁冲突

2. **游戏更新**：
   - 游戏更新可能导致补丁失效

   - 需要重新适配

3. **其他 Mod 兼容**：
   - 可能与其他 Mod 的补丁冲突

   - 使用 Prefix 返回值控制执行

4. **反射开销**：
   - 缓存 FieldInfo/MethodInfo

   - 避免每帧反射

5. **异常处理**：
   - 补丁中使用 Try-Catch

   - 避免崩溃游戏
