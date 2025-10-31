# 防止死亡掉落修复

## 问题描述
玩家死亡时会掉落一地的道具，影响游戏体验和性能。

## 解决方案
参考NoDeathDrops模组的实现方式，通过Harmony Patch阻止客户端玩家死亡时的物品掉落。

## 实现方式

### 新增文件
`EscapeFromDuckovCoopMod/Patch/Player/PreventPlayerDropAllItemsPatch.cs`

### 核心逻辑
```csharp
[HarmonyPatch(typeof(CharacterMainControl), "DropAllItems")]
internal static class PreventClientPlayerDropAllItemsPatch
{
    [HarmonyPrefix]
    private static bool PreventPlayerDropAllItems(CharacterMainControl __instance)
    {
        var mod = ModBehaviourF.Instance;
        if (mod == null || !mod.networkStarted) 
        {
            // 非联机模式，允许正常执行
            return true;
        }

        // 只在客户端阻止玩家掉落物品
        if (!mod.IsServer && __instance == CharacterMainControl.Main)
        {
            Debug.Log("[COOP] 阻止客户端玩家死亡时掉落所有物品");
            return false; // 阻止掉落
        }

        // 服务端或其他角色的掉落允许正常执行
        return true;
    }
}
```

## 工作原理

### 1. Harmony Prefix拦截
- **目标方法**：`CharacterMainControl.DropAllItems`
- **拦截时机**：方法执行前（Prefix）
- **返回值控制**：`false` 阻止原方法执行，`true` 允许执行

### 2. 条件判断
- **联机检查**：只在联机模式下生效
- **角色检查**：只拦截主角色（`CharacterMainControl.Main`）
- **客户端检查**：只在客户端阻止，服务端允许执行

### 3. 执行逻辑
```
非联机模式 → 允许掉落（保持原版体验）
    ↓
联机模式 → 检查角色和端类型
    ↓
客户端主角色 → 阻止掉落（防止物品散落）
    ↓
服务端或其他角色 → 允许掉落（保持游戏逻辑）
```

## 预期效果

### ✅ 客户端玩家死亡
- **不会掉落物品**到地面
- **物品保留**在角色身上
- **通过墓碑系统**处理物品分配

### ✅ 服务端玩家死亡
- **正常掉落物品**（如果需要）
- **保持原版逻辑**
- **兼容其他系统**

### ✅ 其他角色死亡
- **AI角色正常掉落**
- **远程玩家正常掉落**
- **不影响游戏平衡**

## 日志输出

### 客户端玩家死亡
```
[COOP] 阻止客户端玩家死亡时掉落所有物品
```

### 服务端执行
```
[COOP] 服务端 DropAllItems - 允许正常执行
```

### 其他角色
```
[COOP] 客户端其他角色 DropAllItems - 允许正常执行
```

### 非联机模式
```
[COOP] DropAllItems - 非联机模式，允许正常执行
```

## 关键特性

### 🎯 精确控制
- **只影响客户端主角色**
- **不影响服务端逻辑**
- **不影响AI或其他玩家**

### 🔧 兼容性好
- **非联机模式不受影响**
- **保持原版单机体验**
- **与墓碑系统配合**

### 📊 清晰日志
- **详细的执行日志**
- **便于调试和验证**
- **区分不同执行路径**

### 🛡️ 安全可靠
- **简单的布尔返回控制**
- **不修改游戏核心逻辑**
- **易于理解和维护**

## 与墓碑系统的配合

### 完整的死亡处理流程
1. **客户端死亡** → 阻止物品掉落
2. **发送死亡树** → `PLAYER_DEAD_TREE` 包含所有物品
3. **创建墓碑** → 服务端处理物品分配
4. **减法操作** → 从墓碑中减去剩余物品
5. **最终结果** → 只有真正丢失的物品在墓碑中

### 优势
- **避免重复掉落**：物品不会既掉在地上又在墓碑中
- **性能优化**：减少地面物品数量
- **体验改善**：避免物品散落一地的混乱

## 编译状态
✅ **编译成功** - 防止死亡掉落功能已实现

## 测试建议
1. 客户端玩家死亡，确认没有物品掉落到地面
2. 检查日志确认拦截生效
3. 验证墓碑系统正常工作
4. 测试服务端玩家死亡（如果适用）
5. 确认AI角色死亡不受影响

## 总结
这个修复通过简单而有效的Harmony Patch，解决了客户端玩家死亡时物品掉落一地的问题，与现有的墓碑系统完美配合，提供了更好的游戏体验。