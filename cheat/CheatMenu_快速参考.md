# CheatMenu 快速参考手册

## 🚀 快速开始

### 功能开关速查表

| 功能 | 默认状态 | 字段名 | 说明 |
|------|---------|--------|------|
| 🎯 自动瞄准 | ✅ ON | `_autoAimEnabled` | 自动锁定敌人 |
| 💣 投掷物追踪 | ✅ ON | `_grenadeAutoAimEnabled` | 手榴弹自动追踪 |
| 🔫 无后坐力 | ✅ ON | `_noRecoilEnabled` | 消除武器后坐力 |
| ∞ 无限弹药 | ✅ ON | `_infiniteAmmoEnabled` | 弹药不消耗 |
| 💥 无限投掷物 | ✅ ON | `_infiniteThrowablesEnabled` | 手榴弹不消耗 |
| 🏃 移动解锁 | ✅ ON | `_movementUnlockEnabled` | 任意状态可射击 |
| 🧱 穿墙射击 | ✅ ON | `_obstaclePenetrationEnabled` | 子弹穿透障碍物 |
| 🛡️ 无敌模式 | ✅ ON | `_invincibilityEnabled` | 不受伤害 |
| 💪 无限耐力 | ❌ OFF | `_infiniteStaminaEnabled` | 耐力不消耗 |
| 🔧 无限耐久 | ✅ ON | `_infiniteDurabilityEnabled` | 装备不损耗 |
| 🍖 无生存需求 | ✅ ON | `_noSurvivalNeedsEnabled` | 不需要进食饮水 |
| 🌀 旋转击杀 | ❌ OFF | `_spinKillEnabled` | 自动旋转击杀 |

---

## ⚙️ 参数调整

### 武器倍率

```csharp
_rangeMultiplier = 1.0f;        // 射程倍率 (1.0 - 10.0)
_fireRateMultiplier = 1.0f;     // 射速倍率 (1.0 - 10.0)
_bulletSpeedMultiplier = 1.0f;  // 弹速倍率 (1.0 - 10.0)
_damageMultiplier = 1.0f;       // 伤害倍率 (1.0 - 10.0)
```

### 自动瞄准

```csharp
_autoAimScreenRadius = 150f;    // 屏幕检测半径（像素）
_rangeVisible = true;           // 显示范围指示器
```

### 旋转击杀

```csharp
_spinKillSearchRadius = 15f;    // 搜索半径（米）
_spinKillAngularSpeed = 360f;   // 旋转速度（度/秒）
```

---

## 🎮 常用操作

### 启用/禁用功能

```csharp
// 在菜单中切换，或通过代码：
ModBehaviour.Instance._autoAimEnabled = true;
ModBehaviour.Instance._spinKillEnabled = false;
```

### 调整武器属性

```csharp
// 设置 2 倍伤害
ModBehaviour.Instance._damageMultiplier = 2.0f;

// 设置 3 倍射速
ModBehaviour.Instance._fireRateMultiplier = 3.0f;
```

### 显示/隐藏范围指示器

```csharp
ModBehaviour.Instance._rangeVisible = true;  // 显示
ModBehaviour.Instance._rangeVisible = false; // 隐藏
```

---

## 🔍 功能组合推荐

### 🎯 精准射手模式
```
✅ 自动瞄准
✅ 无后坐力
✅ 无限弹药
❌ 穿墙射击
❌ 旋转击杀
```

### 💥 暴力突破模式
```
✅ 穿墙射击
✅ 无限弹药
✅ 移动解锁
✅ 无敌模式
伤害倍率: 3.0x
射速倍率: 2.0x
```

### 🌀 清场模式
```
✅ 旋转击杀
✅ 穿墙射击
✅ 无敌模式
✅ 无限弹药
旋转速度: 720°/秒
搜索半径: 30m
```

### 🏃 速通模式
```
✅ 移动解锁
✅ 无限耐力
✅ 无生存需求
✅ 无限耐久
❌ 自动瞄准（手动操作）
```

---

## 🛠️ 故障排查

### 功能不生效

**问题**: 启用功能后没有效果

**解决方案**:
1. 检查 `ModBehaviour.Instance` 是否为 null
2. 确认角色已初始化（`CharacterMainControl.Main` 不为 null）
3. 查看 Unity 日志是否有错误信息
4. 重新进入关卡

### 自动瞄准不工作

**问题**: 启用自动瞄准但不锁定目标

**检查清单**:
- [ ] `_autoAimEnabled = true`
- [ ] 屏幕半径设置合理（推荐 100-300）
- [ ] 场景中有敌人且在范围内
- [ ] 相机和输入管理器正常工作

### 旋转击杀卡顿

**问题**: 启用旋转击杀后游戏卡顿

**优化方案**:
1. 减小搜索半径（15m → 10m）
2. 降低旋转速度（360°/s → 180°/s）
3. 关闭范围可视化
4. 检查是否有大量敌人

### 穿墙射击失效

**问题**: 子弹仍然被墙壁阻挡

**检查**:
1. 确认 `_obstaclePenetrationEnabled = true`
2. 确认 `_penetrationActive = true`
3. 检查弹道类型是否支持
4. 重新射击以应用设置

---

## 📊 性能影响

### 低影响功能 (< 1% CPU)
- 无后坐力
- 无限弹药
- 无限投掷物
- 移动解锁
- 无敌模式
- 无限耐久
- 无生存需求

### 中等影响功能 (1-5% CPU)
- 自动瞄准（多线程优化）
- 投掷物追踪
- 穿墙射击

### 高影响功能 (5-15% CPU)
- 旋转击杀（大量碰撞检测）
- 范围可视化（LineRenderer）

---

## 🔐 安全提示

### ⚠️ 警告

1. **多人游戏**: 使用作弊功能可能导致账号封禁
2. **存档损坏**: 建议定期备份存档
3. **游戏更新**: 更新后可能需要重新编译 Mod

### ✅ 推荐使用场景

- 单人模式测试
- 私人服务器
- 内容创作（视频制作）
- 游戏机制研究

### ❌ 不推荐使用场景

- 公共多人服务器
- 竞技模式
- 排行榜挑战
- 成就解锁

---

## 📞 技术支持

### 日志位置

```
%AppData%\..\LocalLow\Duckov\Escape from Duckov\Player.log
```

### 调试信息

在代码中搜索 `[CheatMenu]` 标签的日志输出：

```csharp
Debug.Log("[CheatMenu] Your debug message");
```

### 常用调试方法

```csharp
// 检查实例状态
if (ModBehaviour.Instance != null)
{
    Debug.Log("CheatMenu is active");
}

// 检查角色状态
var character = CharacterMainControl.Main;
if (character != null)
{
    Debug.Log($"Character: {character.name}");
}

// 检查自动瞄准线程
if (ModBehaviour.Instance._autoAimWorkerThread != null)
{
    Debug.Log("AutoAim thread is running");
}
```

---

## 🔗 相关资源

- [完整功能文档](./CheatMenu_功能文档.md)
- [游戏 API 文档](../codeAnalysis/INDEX.md)
- [Mod 部署指南](../.kiro/steering/mod-deployment.md)

---

*快速参考 v1.0 | 2025-11-09*
