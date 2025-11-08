# NetTag 模块

## 概述

NetTag 模块提供网络对象标记组件，用于标识和追踪需要网络同步的游戏对象。该模块包含 3 个标记组件，分别用于可破坏物、掉落物品和手榴弹的网络同步。

## 模块结构

```

NetTag/
├── NetDestructibleTag.cs    - 可破坏物网络标记

├── NetDropTag.cs             - 掉落物品网络标记

└── NetGrenadeTag.cs          - 手榴弹网络标记

```

## 核心类

### 1. NetDestructibleTag - 可破坏物网络标记

**功能**：为场景中的可破坏物（门、箱子、障碍物等）生成稳定的网络 ID。

**核心属性**：

```csharp
public uint id    // 稳定的网络 ID

```

**关键方法**：

```csharp
public static uint ComputeStableID(GameObject go)

```

**ID 生成算法**：
1. 收集场景索引
2. 构建完整的 Transform 层级路径（包含兄弟索引）
3. 添加位置信息（厘米精度）
4. 使用 FNV1a-32哈希算法生成ID

**ID 格式**：

```

{sceneIndex}:/{root}#{siblingIndex}/{parent}#{siblingIndex}/...:{px},{py},{pz}

```

**特点**：

- **确定性**：相同对象总是生成相同 ID

- **稳定性**：场景重新加载后 ID 保持不变

- **唯一性**：不同对象生成不同 ID（碰撞概率极低）

**使用场景**：

- 门的开关状态同步

- 可破坏物的血量同步

- 环境对象的状态同步

### 2. NetDropTag - 掉落物品网络标记

**功能**：标记玩家掉落的物品，用于网络同步和拾取管理。

**核心属性**：

```csharp
public uint id    // 掉落物品的网络 ID

```

**关键方法**：

```csharp
private static void AddNetDropTag(GameObject go, uint id)
private static void AddNetDropTag(Item item, uint id)

```

**使用场景**：- 玩家丢弃物品时添加标记

- 主机广播物品掉落事件

- 客户端接收并生成掉落物品

- 物品拾取时验证 ID

### 3. NetGrenadeTag - 手榴弹网络标记

**功能**：标记投掷的手榴弹，用于网络同步和爆炸管理。

**核心属性**：

```csharp
public uint id    // 手榴弹的网络 ID

```

**使用场景**：

- 玩家投掷手榴弹时添加标记

- 主机广播手榴弹生成事件

- 客户端接收并生成手榴弹

- 爆炸时同步伤害范围

## 设计模式

### 1. 标记模式（Tag Pattern）

使用 MonoBehaviour 组件作为标记：

- **轻量级**：只包含 ID 字段

- **易于查找**：使用 GetComponent

- **自动管理**：随 GameObject 销毁

### 2. 单例组件模式

使用 `[DisallowMultipleComponent]` 确保唯一性：

- 防止重复添加

- 确保 ID 唯一性

## 关键技术点

### 1. 稳定 ID 生成算法

**FNV1a-32 哈希**：

```csharp
unchecked
{
    var hash = 2166136261;  // FNV offset basis
    for (var i = 0; i < key.Length; i++)
    {
        hash ^= key[i];
        hash *= 16777619;   // FNV prime
    }
    return hash == 0 ? 1u : hash;
}

```

**优点**：

- **快速**：O(n) 时间复杂度

- **简单**：易于实现和理解

- **稳定**：相同输入总是相同输出

- **分布均匀**：碰撞概率低

### 2. 层级路径构建

使用栈构建完整路径：

```csharp
var stack = new Stack<Transform>();
while (t != null)
{
    stack.Push(t);
    t = t.parent;
}

var sb = new StringBuilder(256);
while (stack.Count > 0)
{
    var cur = stack.Pop();
    sb.Append('/').Append(cur.name).Append('#').Append(cur.GetSiblingIndex());
}

```

**包含信息**：

- 对象名称

- 兄弟索引（区分同名对象）

- 完整层级路径

### 3. 位置信息

添加位置信息增强唯一性：

```csharp
var px = Mathf.RoundToInt(p.x * 100f);  // 厘米精度

var py = Mathf.RoundToInt(p.y * 100f);

var pz = Mathf.RoundToInt(p.z * 100f);

```

**作用**：

- 区分相同层级路径的对象

- 提供空间位置信息

- 厘米精度足够区分大部分对象

## 使用示例

### 标记可破坏物

```csharp
var destructible = GetComponent<HealthSimpleBase>();
var tag = destructible.gameObject.AddComponent<NetDestructibleTag>();
// tag.id 自动在Awake中生成

```

### 标记掉落物品

```csharp
var dropId = GenerateDropId();
var tag = droppedItem.AddComponent<NetDropTag>();
tag.id = dropId;

```

### 标记手榴弹

```csharp
var grenadeId = GenerateGrenadeId();
var tag = grenade.AddComponent<NetGrenadeTag>();
tag.id = grenadeId;

```

### 查找标记对象

```csharp
var tag = gameObject.GetComponent<NetDestructibleTag>();
if (tag != null)
{
    uint objectId = tag.id;
    // 使用ID进行网络同步
}

```

## 性能优化

### 1. 延迟计算

ID 在 Awake 中计算，避免每帧计算：

```csharp
private void Awake()
{
    id = ComputeStableID(gameObject);
}

```

### 2. StringBuilder 优化

使用 StringBuilder 构建路径，避免字符串拼接：

```csharp
var sb = new StringBuilder(256);  // 预分配容量

```

### 3. 缓存结果

ID 计算后缓存在字段中，避免重复计算。

## 总结

NetTag 模块提供了轻量级的网络对象标记系统，通过稳定的 ID 生成算法确保网络同步的准确性。该模块是实现环境对象和动态对象网络同步的基础。

**核心特性**：

- 稳定的 ID 生成算法

- 轻量级组件设计

- 易于使用和扩展

- 高性能和低开销
