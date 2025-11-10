# 同步 UI 无敌功能说明

## 📋 功能概述

在 `WaitingSynchronizationUI`（同步等待界面）显示期间，自动为角色启用无敌状态，并在界面隐藏或关闭时恢复原始状态。

## 🎯 实现目标

- ✅ UI 显示时（`Show()`）：启用角色无敌
- ✅ UI 隐藏时（`Hide()`）：强制解除无敌
- ✅ UI 关闭时（`Close()`）：强制解除无敌
- ✅ 保存并恢复原始无敌状态

## 🔧 技术实现

### 1. 新增字段

```csharp
// 无敌状态管理
private Health _invincibilityTargetHealth = null;
private bool? _originalInvincibleState = null;
```

### 2. 修改的方法

#### Show() - 显示 UI 时启用无敌

```csharp
public void Show()
{
    // ... 原有代码 ...
    
    // ✅ 启用角色无敌
    EnableCharacterInvincibility();
    
    Debug.Log("[SYNC_UI] 显示同步等待界面完成");
}
```

#### Hide() - 隐藏 UI 时解除无敌

```csharp
public void Hide()
{
    // ✅ 强制解除角色无敌
    DisableCharacterInvincibility();
    
    // ... 原有代码 ...
}
```

#### Close() - 关闭 UI 时解除无敌

```csharp
public void Close()
{
    // ✅ 强制解除角色无敌
    DisableCharacterInvincibility();
    
    // ... 原有代码 ...
}
```

### 3. 新增方法

#### EnableCharacterInvincibility() - 启用无敌

```csharp
private void EnableCharacterInvincibility()
{
    try
    {
        var character = CharacterMainControl.Main;
        if (character == null)
        {
            Debug.LogWarning("[SYNC_UI] 无法启用无敌：角色为空");
            return;
        }

        var health = character.Health;
        if (health == null)
        {
            Debug.LogWarning("[SYNC_UI] 无法启用无敌：Health组件为空");
            return;
        }

        // 如果已经在追踪其他Health对象，先恢复
        if (_invincibilityTargetHealth != null && _invincibilityTargetHealth != health)
        {
            DisableCharacterInvincibility();
        }

        // 保存原始状态
        if (_originalInvincibleState == null)
        {
            _originalInvincibleState = health.Invincible;
            Debug.Log($"[SYNC_UI] 保存原始无敌状态: {_originalInvincibleState.Value}");
        }

        // 启用无敌
        if (!health.Invincible)
        {
            health.SetInvincible(true);
            Debug.Log("[SYNC_UI] ✅ 已启用角色无敌");
        }
        else
        {
            Debug.Log("[SYNC_UI] 角色已处于无敌状态");
        }

        _invincibilityTargetHealth = health;
    }
    catch (Exception ex)
    {
        Debug.LogError($"[SYNC_UI] 启用无敌失败: {ex.Message}\n{ex.StackTrace}");
    }
}
```

#### DisableCharacterInvincibility() - 解除无敌

```csharp
private void DisableCharacterInvincibility()
{
    try
    {
        if (_invincibilityTargetHealth != null && _originalInvincibleState != null)
        {
            _invincibilityTargetHealth.SetInvincible(_originalInvincibleState.Value);
            Debug.Log($"[SYNC_UI] ✅ 已恢复角色无敌状态为: {_originalInvincibleState.Value}");
        }
        else if (_invincibilityTargetHealth == null && _originalInvincibleState != null)
        {
            Debug.LogWarning("[SYNC_UI] Health对象已失效，无法恢复无敌状态");
        }

        _invincibilityTargetHealth = null;
        _originalInvincibleState = null;
    }
    catch (Exception ex)
    {
        Debug.LogError($"[SYNC_UI] 解除无敌失败: {ex.Message}\n{ex.StackTrace}");
    }
}
```

## 📊 工作流程

### 场景 1：正常流程

```
1. 玩家进入场景
2. UI.Show() 被调用
   └─> EnableCharacterInvincibility()
       ├─> 保存原始无敌状态（例如：false）
       └─> 设置无敌为 true
3. 同步等待中...（角色处于无敌状态）
4. 同步完成
5. UI.Hide() 被调用
   └─> DisableCharacterInvincibility()
       └─> 恢复无敌状态为原始值（false）
```

### 场景 2：强制关闭

```
1. UI.Show() 被调用
   └─> 启用无敌
2. 某些原因需要立即关闭
3. UI.Close() 被调用
   └─> DisableCharacterInvincibility()
       └─> 立即恢复原始状态
```

### 场景 3：角色切换

```
1. UI.Show() 被调用（角色 A）
   └─> 为角色 A 启用无敌
2. 角色切换到角色 B
3. UI.Show() 再次被调用
   └─> EnableCharacterInvincibility()
       ├─> 检测到 Health 对象变化
       ├─> 先恢复角色 A 的状态
       └─> 为角色 B 启用无敌
```

## 🔍 调试日志

### 启用无敌时的日志

```
[SYNC_UI] 保存原始无敌状态: False
[SYNC_UI] ✅ 已启用角色无敌
```

### 解除无敌时的日志

```
[SYNC_UI] ✅ 已恢复角色无敌状态为: False
```

### 异常情况日志

```
[SYNC_UI] 无法启用无敌：角色为空
[SYNC_UI] 无法启用无敌：Health组件为空
[SYNC_UI] Health对象已失效，无法恢复无敌状态
```

## ⚠️ 注意事项

### 1. 状态保存机制

- 只在第一次启用无敌时保存原始状态
- 避免重复保存导致状态丢失
- 解除无敌后清空保存的状态

### 2. 异常处理

- 所有操作都包含 try-catch 保护
- 角色或 Health 为空时安全退出
- 记录详细的错误日志便于调试

### 3. 多次调用保护

- 如果角色已经无敌，不会重复设置
- 如果 Health 对象变化，先恢复旧对象再处理新对象
- Hide/Close 可以安全地多次调用

## 🎮 使用场景

### 适用情况

✅ **场景加载同步**：防止玩家在等待时被敌人攻击  
✅ **网络同步等待**：多人游戏中等待其他玩家时保护本地玩家  
✅ **关键操作保护**：任何需要玩家等待且不应受到伤害的场景

### 不适用情况

❌ **战斗中的 UI**：如果 UI 是战斗的一部分，不应启用无敌  
❌ **可选的暂停菜单**：玩家主动打开的菜单通常不需要无敌

## 📝 测试建议

### 测试用例 1：基本功能

1. 进入需要同步的场景
2. 观察日志确认无敌已启用
3. 尝试受到伤害（应该无效）
4. 等待同步完成
5. 观察日志确认无敌已解除
6. 尝试受到伤害（应该正常扣血）

### 测试用例 2：强制关闭

1. 显示同步 UI
2. 在同步完成前调用 Close()
3. 确认无敌状态正确恢复

### 测试用例 3：多次显示/隐藏

1. 连续调用 Show() 多次
2. 调用 Hide()
3. 再次调用 Show()
4. 确认状态管理正确

## 🔗 相关文件

- **修改文件**: `EscapeFromDuckovCoopMod/Main/UI/WaitingSynchronizationUI.cs`
- **参考实现**: `cheat/CheatMenu/ModBehaviour.cs` (无敌功能实现)
- **部署文档**: `.kiro/steering/mod-deployment.md`

## 📅 更新记录

- **2025-11-09**: 初始实现
  - 添加无敌状态管理字段
  - 实现 EnableCharacterInvincibility() 方法
  - 实现 DisableCharacterInvincibility() 方法
  - 在 Show/Hide/Close 中集成无敌控制

---

*最后更新: 2025-11-09*
