# Utils 模块

## 概述

Utils 模块提供通用工具类，包括数学统计工具和场景管理工具。该模块包含2个工具类，用于网络插值优化和场景触发器管理。

## 模块结构

```

Utils/
├── ExponentialMovingAverage.cs    - 指数移动平均（EMA）

└── SceneTriggerResetter.cs        - 场景触发器重置工具

```

## 核心类

### 1. ExponentialMovingAverage - 指数移动平均

**功能**：实现指数移动平均算法，用于平滑网络抖动和追踪延迟变化。

**来源**：移植自 Fika-Plugin 项目（MIT License）

**核心属性**：```csharp
public double Value                  // 当前平均值
public double Variance               // 方差
public double StandardDeviation      // 标准差（抖动程度）
public bool IsInitialized           // 是否已初始化
public double SignalToNoiseRatio    // 信噪比

```

**关键方法**：```csharp
public ExponentialMovingAverage(int n)    // 构造函数，n 为平滑窗口大小
public void Add(double newValue)          // 添加新数据点
public void Reset()                       // 重置为初始状态

```

**算法原理**：EMA使用指数衰减权重计算移动平均：

```

α = 2 / (n + 1)                    // 平滑因子
EMA(t) = α * x(t) + (1-α) * EMA(t-1)  // 递归公式

```

**方差计算**：```

δ = x(t) - EMA(t-1)                // 增量

Var(t) = (1-α) * (Var(t-1) + α * δ²)  // 方差更新

StdDev = √Var                      // 标准差

```

**参数选择**：- n越大：越平滑，但响应越慢

- n 越小：响应越快，但越不稳定

- 推荐值：30-120（0.5-2秒窗口）

**使用场景**：- 网络延迟追踪

- 包间隔抖动测量

- 时间漂移计算

- 动态延迟调整

**优点**：- 内存占用小（O(1)）

- 计算速度快（O(1)）

- 自动衰减旧数据

- 提供方差和标准差

### 2. SceneTriggerResetter - 场景触发器重置工具

**功能**：重置场景中的触发器状态，解决场景切换后触发器只能触发一次的问题。

**核心方法**：```csharp
public static void ResetAllSceneTriggers()           // 重置所有触发器
public static void ResetTrigger(GameObject triggerObject)  // 重置特定触发器

```

**重置目标**：1. **OnTriggerEnterEvent组件**
   - 重置`triggered`字段为false

   - 允许触发器再次触发

2. **MultiSceneTeleporter组件**
   - 重置`timeWhenTeleportFinished`为-999f

   - 清除传送冷却时间

3. **InteractableBase 组件**
   - 重新启用组件（如果被禁用）

   - 重置`lastStopTime`为-1f

   - 重新启用碰撞体

   - 重新显示交互标记 UI

**实现技术**：使用反射访问私有字段：

```csharp
private static FieldInfo _triggeredField;
private static FieldInfo _teleportFinishedTimeField;
private static FieldInfo _lastStopTimeField;

// 初始化时查找字段
_triggeredField = _triggerEventType.GetField("triggered",
    BindingFlags.Instance | BindingFlags.NonPublic);

// 使用时设置值
_triggeredField.SetValue(triggerEvent, false);

```

**类型查找**：从所有已加载的程序集中查找类型：

```csharp
foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
{
    _triggerEventType = assembly.GetType("OnTriggerEnterEvent");
    if (_triggerEventType != null) break;
}

```

**使用场景**：- 场景切换后重置触发器

- 重新进入已访问的场景

- 多人游戏中同步触发器状态

## 使用示例

### 使用EMA追踪网络延迟

```csharp
// 创建EMA实例（60帧窗口，约1秒）
var delayEma = new ExponentialMovingAverage(60);

// 每次收到包时添加延迟数据
void OnPacketReceived(double arrivalTime, double sendTime)
{
    double delay = arrivalTime - sendTime;

    delayEma.Add(delay);

    // 获取统计信息
    Debug.Log($"平均延迟: {delayEma.Value:F3}s");
    Debug.Log($"抖动: {delayEma.StandardDeviation:F3}s");
    Debug.Log($"信噪比: {delayEma.SignalToNoiseRatio:F1}");
}

```

### 使用EMA计算动态缓冲

```csharp
var intervalEma = new ExponentialMovingAverage(60);

void UpdateInterpolationDelay()
{
    if (!intervalEma.IsInitialized) return;

    // 基于抖动动态调整延迟
    double jitter = intervalEma.StandardDeviation;
    double baseDelay = 1.0 / sendRate;
    double dynamicDelay = baseDelay + jitter * 2.0;


    interpolationBackTime = (float)Math.Clamp(dynamicDelay, 0.05, 0.25);
}

```

### 重置场景触发器

```csharp
// 场景加载完成后重置所有触发器
void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    SceneTriggerResetter.ResetAllSceneTriggers();
}

```

### 重置特定触发器

```csharp
// 重置特定的传送门触发器
var teleporter = GameObject.Find("Teleporter");
if (teleporter != null)
{
    SceneTriggerResetter.ResetTrigger(teleporter);
}

```

## 数学原理

### EMA公式推导

**递归形式**：```

EMA(t) = α * x(t) + (1-α) * EMA(t-1)

```

**展开形式**：```

EMA(t) = α * [x(t) + (1-α)*x(t-1) + (1-α)²*x(t-2) + ...]

```

**权重衰减**：- 当前值权重: α

- 1帧前权重: α(1-α)

- 2帧前权重: α(1-α)²

- n 帧前权重: α(1-α)ⁿ

**半衰期**：```

t_half = -ln(2) / ln(1-α) ≈ 0.693 * n

```

### 方差计算

**Welford算法变体**：```

δ = x(t) - EMA(t-1)

EMA(t) = EMA(t-1) + α * δ

Var(t) = (1-α) * (Var(t-1) + α * δ²)

```

**标准差**：```

StdDev = √Var

```

## 性能优化

### EMA优化

1. **使用 struct**：避免堆分配
2. **O(1)复杂度**：只需一次乘加运算
3. **内存占用小**：只存储3个 double 值

### SceneTriggerResetter 优化

1. **反射缓存**：启动时查找并缓存 FieldInfo
2. **批量处理**：一次性重置所有触发器
3. **条件检查**：只在需要时执行重置

## 调试和监控

### EMA调试信息

```csharp
var stats = $"EMA: {ema.Value:F3}, " +
            $"StdDev: {ema.StandardDeviation:F3}, " +
            $"SNR: {ema.SignalToNoiseRatio:F1}";
Debug.Log(stats);

```

### 触发器重置日志

```csharp
[SceneTriggerResetter] 成功重置 15 个 OnTriggerEnterEvent 触发器
[SceneTriggerResetter] 成功重置 MultiSceneTeleporter 传送冷却时间
[SceneTriggerResetter] 成功重置 8 个 InteractableBase 交互状态
[SceneTriggerResetter] 总共完成 24 项重置操作

```

## 扩展性

### EMA扩展

1. 添加更多统计指标：
   - 最小值/最大值追踪

   - 百分位数计算

   - 趋势检测

2. 支持多维数据：
   - Vector3 EMA

   - Quaternion EMA

### SceneTriggerResetter 扩展

1. 支持更多触发器类型：
   - 自定义触发器

   - 第三方插件触发器

2. 添加选择性重置：
   - 按标签重置

   - 按区域重置

## 总结

Utils 模块提供了两个强大的工具类：

- **ExponentialMovingAverage**：高效的统计工具，用于网络质量监控和动态优化

- **SceneTriggerResetter**：场景管理工具，解决触发器重用问题

**核心特性**：- 高性能算法实现

- 易于使用的 API

- 完善的错误处理

- 详细的调试日志

这些工具类为联机系统提供了重要的基础设施支持。
