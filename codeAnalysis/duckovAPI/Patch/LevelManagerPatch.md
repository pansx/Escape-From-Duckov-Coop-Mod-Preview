# LevelManagerPatch - 关卡管理器补丁

## 📋 概述

`LevelManagerPatch` 是关卡管理器的性能监控补丁集合，用于跟踪角色死亡流程、定位耗时操作。

**文件路径**: `EscapeFromDuckovCoopMod/Patch/LevelManagerPatch.cs`

---

## 🎯 补丁列表

### 1. 角色死亡流程监控

```csharp
[HarmonyPatch(typeof(LevelManager), "OnMainCharacterDie")]
internal static class Patch_LevelManager_OnMainCharacterDie
{
    private static void Prefix()
    {
        UnityEngine.Debug.Log($"[Death-Monitor] [{DateTime.Now:HH:mm:ss.fff}] ========== 角色死亡流程开始 ==========");
    }
    
    private static void Postfix()
    {
        UnityEngine.Debug.Log($"[Death-Monitor] [{DateTime.Now:HH:mm:ss.fff}] OnMainCharacterDie 完成");
    }
}
```

**功能**：
- 记录角色死亡流程的开始和结束时间
- 用于定位死亡流程中的性能瓶颈

### 2. 存档操作监控

```csharp
[HarmonyPatch(typeof(SavesSystem), "SaveFile")]
internal static class Patch_SavesSystem_SaveFile
{
    private static Stopwatch _stopwatch;
    
    private static void Prefix(bool writeSaveTime)
    {
        _stopwatch = Stopwatch.StartNew();
    }
    
    private static void Postfix()
    {
        _stopwatch?.Stop();
    }
}
```

**功能**：
- 监控存档操作的耗时
- 帮助定位存档性能问题

### 3. 墓碑创建监控

```csharp
[HarmonyPatch(typeof(InteractableLootbox), nameof(InteractableLootbox.CreateFromItem))]
internal static class Patch_InteractableLootbox_CreateFromItem
{
    private static Stopwatch _stopwatch;
    
    private static void Prefix(Item item)
    {
        _stopwatch = Stopwatch.StartNew();
        UnityEngine.Debug.Log($"[Death-Monitor] [{DateTime.Now:HH:mm:ss.fff}] CreateFromItem 开始 (物品类型: {item?.GetType().Name})");
    }
    
    private static void Postfix(InteractableLootbox __result)
    {
        _stopwatch?.Stop();
        UnityEngine.Debug.Log($"[Death-Monitor] [{DateTime.Now:HH:mm:ss.fff}] CreateFromItem 完成，耗时: {_stopwatch?.ElapsedMilliseconds}ms");
    }
}
```

**功能**：
- 监控墓碑创建的耗时
- 记录物品类型信息
- 帮助定位墓碑创建性能问题

---

## 📊 性能监控日志格式

### 日志示例

```
[Death-Monitor] [14:23:45.123] ========== 角色死亡流程开始 ==========
[Death-Monitor] [14:23:45.234] CreateFromItem 开始 (物品类型: ItemAgent_Gun)
[Death-Monitor] [14:23:45.456] CreateFromItem 完成，耗时: 222ms
[Death-Monitor] [14:23:45.567] OnMainCharacterDie 完成
```

### 日志字段说明

- `[Death-Monitor]`: 日志标签
- `[HH:mm:ss.fff]`: 精确到毫秒的时间戳
- 操作名称: 如 "CreateFromItem"、"OnMainCharacterDie"
- 耗时: 以毫秒为单位

---

## 🔍 使用场景

### 1. 定位死亡流程性能瓶颈

通过监控日志，可以快速定位死亡流程中的耗时操作：

```
[Death-Monitor] [14:23:45.123] ========== 角色死亡流程开始 ==========
[Death-Monitor] [14:23:45.234] CreateFromItem 开始 (物品类型: ItemAgent_Gun)
[Death-Monitor] [14:23:45.456] CreateFromItem 完成，耗时: 222ms  ← 耗时较长
[Death-Monitor] [14:23:45.567] OnMainCharacterDie 完成
```

### 2. 分析墓碑创建性能

通过物品类型和耗时信息，可以分析不同物品类型的墓碑创建性能：

```
[Death-Monitor] CreateFromItem 开始 (物品类型: ItemAgent_Gun)
[Death-Monitor] CreateFromItem 完成，耗时: 222ms

[Death-Monitor] CreateFromItem 开始 (物品类型: ItemAgent_Armor)
[Death-Monitor] CreateFromItem 完成，耗时: 45ms
```

### 3. 监控存档操作

虽然当前代码中注释掉了存档日志输出，但可以根据需要启用：

```csharp
private static void Prefix(bool writeSaveTime)
{
    _stopwatch = Stopwatch.StartNew();
    UnityEngine.Debug.Log($"[Death-Monitor] [{DateTime.Now:HH:mm:ss.fff}] SaveFile 开始 (writeSaveTime={writeSaveTime})");
}

private static void Postfix()
{
    _stopwatch?.Stop();
    UnityEngine.Debug.Log($"[Death-Monitor] [{DateTime.Now:HH:mm:ss.fff}] SaveFile 完成，耗时: {_stopwatch?.ElapsedMilliseconds}ms");
}
```

---

## 🔧 补丁技术

### 1. Prefix 补丁

在原方法执行前插入代码：

```csharp
private static void Prefix()
{
    // 在原方法执行前执行
    _stopwatch = Stopwatch.StartNew();
}
```

### 2. Postfix 补丁

在原方法执行后插入代码：

```csharp
private static void Postfix()
{
    // 在原方法执行后执行
    _stopwatch?.Stop();
}
```

### 3. 参数访问

可以访问原方法的参数：

```csharp
private static void Prefix(Item item)
{
    // 访问原方法的 item 参数
    UnityEngine.Debug.Log($"物品类型: {item?.GetType().Name}");
}
```

### 4. 返回值访问

可以访问原方法的返回值（使用 `__result`）：

```csharp
private static void Postfix(InteractableLootbox __result)
{
    // 访问原方法的返回值
    UnityEngine.Debug.Log($"创建的墓碑: {__result}");
}
```

---

## 📝 最近更新

### 2024-11-11
- ✅ 添加角色死亡流程监控
- ✅ 添加存档操作监控
- ✅ 添加墓碑创建监控
- ✅ 使用 Stopwatch 精确测量耗时
- ✅ 添加毫秒级时间戳

---

## 🔗 相关模块

- **LevelManager**: 关卡管理器
- **SavesSystem**: 存档系统
- **InteractableLootbox**: 可交互战利品箱（墓碑）
- **HarmonyLib**: Harmony 补丁库

---

*文档版本: 1.0.0*  
*最后更新: 2024-11-11*
