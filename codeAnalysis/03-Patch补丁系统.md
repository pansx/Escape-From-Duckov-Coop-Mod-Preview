# Patch 补丁系统分析

## 概览

Patch目录包含了所有的Harmony补丁，这些补丁通过运行时代码注入的方式修改游戏原有行为，实现多人同步功能。补丁系统是整个联机模组的核心，负责拦截、修改和扩展游戏的各种行为。

## 目录结构

```
Patch/
├── Character/          # 角色相关补丁
├── InventoryAndLootBox/ # 库存和战利品箱补丁
├── Item/               # 物品相关补丁
└── Scene/              # 场景相关补丁
```

## Character/ - 角色系统补丁

### 核心补丁文件

#### 1. CharacterMainControlPatch.cs - 角色主控制器补丁
**主要功能**:
- **装备变更广播**: AI切换装备时自动广播给所有客户端
- **模型设置处理**: 角色模型变更时的同步处理
- **死亡处理**: 玩家死亡时的物品保护和状态同步
- **AI标记管理**: 为AI角色添加网络标记和组件

**关键补丁**:
- `Patch_CMC_OnChangeHold_AIRebroadcast`: AI装备变更广播
- `Patch_CMC_SetCharacterModel_FaceReapply`: 模型变更时重新应用外观
- `Patch_Client_OnDead_ReportCorpseTree`: 客户端死亡时上报装备树
- `Patch_Server_OnDead_Host_UsePlayerTree`: 主机死亡处理

#### 2. HealthPatch.cs - 血量系统补丁
**主要功能**:
- **血量同步**: AI血量变化的网络同步
- **伤害重定向**: 客户端伤害请求重定向到主机
- **可破坏物管理**: 环境可破坏物的网络同步
- **血条显示**: 远程玩家血条的显示和更新

**关键补丁**:
- `Patch_HSB_OnHurt_RedirectNet`: 伤害重定向到主机处理
- `Patch_AIHealth_SetHealth_Broadcast`: AI血量变化广播
- `Patch_HealthBar_RefreshCharacterIcon_Override`: 血条图标覆盖

#### 3. PlayerDeathItemPreservePatch.cs - 玩家死亡物品保护补丁
**主要功能**:
- **物品保护**: 防止客户端玩家死亡时掉落所有物品
- **死亡状态管理**: 在保护物品的同时正确处理死亡状态
- **观战系统集成**: 死亡后自动进入观战模式

**关键补丁**:
- `PreventClientPlayerDropAllItemsPatch`: 阻止客户端掉落物品
- `PreventClientPlayerDestroyAllItemsPatch`: 阻止客户端销毁物品
- `PreventClientPlayerOnDeadPatch`: 阻止客户端OnDead执行
- `PreventClientEnsureSelfDeathEventPatch`: 阻止死亡事件补发

### 其他角色补丁

#### AnimPacth.cs - 动画补丁
- 动画状态同步
- 远程玩家动画插值

#### BuffPatch.cs - Buff效果补丁
- Buff效果的网络同步
- 状态效果的应用和移除

#### CharacterItemControl_Patch.cs - 角色物品控制补丁
- 装备穿戴同步
- 物品使用状态同步

## Item/ - 物品系统补丁

### 核心补丁文件

#### 1. GunPatch.cs - 枪械补丁
**主要功能**:
- **射击拦截**: 客户端射击请求重定向到主机
- **AI射击阻止**: 防止客户端AI本地射击
- **弹丸同步**: 主机弹丸参数广播给客户端
- **近战攻击标记**: 近战攻击的本地处理标记

**关键补丁**:
- `Patch_BlockClientAiShoot`: 阻止客户端AI射击
- `Patch_ShootOneBullet_Client`: 客户端射击重定向
- `Patch_ProjectileInit_Broadcast`: 弹丸参数广播
- `Patch_Melee_FlagLocalDeal`: 近战攻击标记

#### 2. ItemPatch.cs - 物品基础补丁
- 物品创建和销毁同步
- 物品状态变化同步

#### 3. SlotPatch.cs - 插槽补丁
- 装备插槽的网络同步
- 附件安装和拆卸同步

## InventoryAndLootBox/ - 库存和战利品系统补丁

### 核心补丁文件

#### 1. LootBoxLoaderPatch.cs - 战利品箱加载补丁
**主要功能**:
- **客户端初始化保护**: 防止客户端重复初始化战利品箱
- **随机激活权威**: 主机决定战利品箱的激活状态
- **状态广播**: 战利品箱状态的网络同步

**关键补丁**:
- `Patch_LootBoxLoader_Setup_GuardClientInit`: 客户端初始化保护
- `Patch_LootBoxLoader_RandomActive_NetAuthority`: 网络权威随机激活

#### 2. InteractableLootboxPatch.cs - 可交互战利品箱补丁
- 战利品箱交互的网络同步
- 物品拾取和放置的权限控制

#### 3. InventoryPatch.cs - 库存补丁
- 库存变化的网络同步
- 物品移动和整理同步

## Scene/ - 场景系统补丁

### 核心补丁文件

#### 1. LevelManagerPatch.cs - 关卡管理器补丁
**主要功能**:
- **场景加载门控**: 客户端场景加载的同步控制
- **地图选择拦截**: 地图选择的网络投票机制

**关键补丁**:
- `Patch_Level_StartInit_Gate`: 场景初始化门控
- `Patch_Mapen_OnPointerClick`: 地图选择拦截

#### 2. DoorPatch.cs - 门系统补丁
- 门开关状态的网络同步
- 门交互的权限控制

#### 3. ScenePatch.cs - 场景基础补丁
- 场景对象的网络同步
- 场景状态的一致性保证

## 补丁设计模式

### 1. 权威模式 (Authority Pattern)
```csharp
// 主机权威，客户端请求
private static bool Prefix(...)
{
    if (!IsServer)
    {
        // 客户端：发送请求给主机
        SendRequestToHost();
        return false; // 阻止本地执行
    }
    return true; // 主机正常执行
}
```

### 2. 重定向模式 (Redirect Pattern)
```csharp
// 将客户端操作重定向到网络处理
private static bool Prefix(...)
{
    if (IsClient && IsLocalPlayer)
    {
        NetworkHandler.HandleOperation();
        return false; // 阻止原始操作
    }
    return true;
}
```

### 3. 广播模式 (Broadcast Pattern)
```csharp
// 主机执行后广播给所有客户端
private static void Postfix(...)
{
    if (IsServer)
    {
        BroadcastToAllClients();
    }
}
```

### 4. 拦截保护模式 (Intercept Protection Pattern)
```csharp
// 保护特定条件下的操作
private static bool Prefix(...)
{
    if (ShouldProtect())
    {
        return false; // 阻止执行
    }
    return true;
}
```

## 补丁优先级管理

### HarmonyPriority 使用
- `Priority.First`: 最高优先级，用于关键拦截
- `Priority.Normal`: 默认优先级
- `Priority.Last`: 最低优先级，用于清理工作

### 补丁执行顺序
1. **Prefix**: 方法执行前的拦截和修改
2. **Postfix**: 方法执行后的处理和广播
3. **Finalizer**: 异常处理和资源清理

## 错误处理策略

### 1. 防守式编程
```csharp
private static Exception Finalizer(Exception __exception)
{
    if (__exception != null)
    {
        Debug.LogWarning($"Suppressed exception: {__exception}");
        return null; // 吞掉异常
    }
    return null;
}
```

### 2. 兜底机制
```csharp
try
{
    // 网络操作
}
catch
{
    return true; // 回退到原始逻辑
}
```

### 3. 状态验证
```csharp
var mod = ModBehaviourF.Instance;
if (mod == null || !mod.networkStarted) 
    return true; // 非联机模式正常执行
```

## 性能优化

### 1. 条件检查优化
- 早期退出：优先检查最可能失败的条件
- 缓存结果：避免重复的反射调用
- 批量处理：合并多个小操作

### 2. 内存管理
- 对象池化：重用频繁创建的对象
- 及时清理：避免内存泄漏
- 弱引用：防止循环引用

### 3. 网络优化
- 消息合并：减少网络包数量
- 压缩数据：使用高效的序列化
- 优先级队列：重要消息优先发送

这个补丁系统展现了一个成熟的多人游戏同步方案的设计思路，通过精心设计的拦截和重定向机制，实现了单人游戏到多人游戏的无缝转换。