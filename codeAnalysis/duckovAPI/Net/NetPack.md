# Net/NetPack 模块 API 文档

## 模块概述

NetPack 模块提供网络数据的压缩和序列化工具，通过量化和压缩技术大幅减少网络带宽占用。该模块是整个网络层的基础，所有网络数据都通过这里的压缩方法进行优化。

**核心职责**：

- 位置数据压缩（Vector3 → 12字节）

- 方向数据压缩（Vector3 → 4字节）

- 浮点数量化压缩

- 伤害数据打包

- 投射物数据打包

- 标志位压缩

## 文件列表

| 文件名 | 说明 |

|--------|------|
|---|---|

| `NetPackProjectile.cs` | 投射物数据打包 |
|---|---|

## 核心类说明

### NetPack

**功能**：提供各种数据压缩和量化方法

**主要方法**：

#### PutV3cm / GetV3cm

```csharp
public static void PutV3cm(this NetDataWriter w, Vector3 v)
public static Vector3 GetV3cm(this NetDataReader r)

```

- **功能**：位置压缩

- **压缩方式**：100倍缩放，转换为3个 int

- **大小**：12字节（vs 原始24字节）

- **精度**：厘米级（0.01m）

- **节省**：50%带宽

#### PutDir / GetDir

```csharp
public static void PutDir(this NetDataWriter w, Vector3 forward)
public static Vector3 GetDir(this NetDataReader r)

```

- **功能**：方向压缩

- **压缩方式**：yaw/pitch 编码，各2字节

- **大小**：4字节（vs 原始12字节）

- **精度**：65535级（ushort）

- **范围**：yaw 0-360度，pitch -90到90度

- **节省**：67%带宽

#### PutSNorm16 / GetSNorm16

```csharp
public static void PutSNorm16(this NetDataWriter w, float v)
public static float GetSNorm16(this NetDataReader r)

```

- **功能**：小范围浮点压缩

- **范围**：[-8, 8]

- **分辨率**：1/16（0.0625）

- **大小**：1字节（sbyte）

- **节省**：75%带宽

#### PutDamagePayload / GetDamagePayload

```csharp
public static void PutDamagePayload(this NetDataWriter w, DamageInfo di)
public static DamageInfo GetDamagePayload(this NetDataReader r)

```

- **功能**：伤害负载打包

- **包含数据**：

  - 伤害值、护甲穿透、暴击系数、暴击率、暴击标志

  - 伤害点（压缩位置）

  - 伤害法线（压缩方向）

  - 武器 ID、流血几率、爆炸标志、攻击范围

- **优化**：使用压缩位置和方向减少带宽

### NetPackProjectile

**功能**：投射物数据打包

**主要方法**：

#### PutProjectilePayload / TryGetProjectilePayload

```csharp
public static void PutProjectilePayload(this NetDataWriter w, ProjectileData data)
public static bool TryGetProjectilePayload(this NetDataReader r, out ProjectileData data)

```

- **功能**：投射物负载打包

- **包含数据**：

  - 基础属性：伤害、暴击率、暴击系数、护甲穿透、护甲破坏

  - 元素属性：物理、火、毒、电、空间

  - 爆炸属性：爆炸范围、爆炸伤害

  - 状态属性：Buff 几率、流血几率

  - 其他：穿透次数、武器 ID

- **总大小**：约64字节（14个 float + 2个 int）

### PackFlag

**功能**：标志位打包

**主要方法**：

#### PackFlags / UnpackFlags

```csharp
public static byte PackFlags(bool f1, bool f2, bool f3, bool f4)
public static (bool, bool, bool, bool) UnpackFlags(byte packed)

```

- **功能**：将4个bool压缩为1个 byte

- **压缩方式**：位运算

- **节省**：75%带宽（4字节 → 1字节）

## 压缩效果对比

| 数据类型 | 原始大小 | 压缩大小 | 节省 |

|---------|---------|---------|------|
|---|---|---|---|

| Vector3方向 | 12字节 | 4字节 | 67% |
|---|---|---|---|

| 小范围 float | 4字节 | 1字节 | 75% |

**总体带宽节省**：约50-70%

## 设计特点

1. **扩展方法设计**：
   - 所有方法都是 NetDataWriter/Reader 的扩展方法

   - 使用方便，代码简洁

   - 无需创建额外对象

2. **无 GC 分配**：
   - 直接操作字节流

   - 避免装箱拆箱

   - 使用值类型

3. **精度与带宽平衡**：
   - 位置：厘米级精度足够游戏使用

   - 方向：65535级精度足够平滑

   - 浮点：根据范围选择合适精度

4. **可逆压缩**：
   - 所有压缩都是可逆的

   - 解压后数据在精度范围内准确

## 使用示例

### 位置压缩

```csharp
// 发送
Vector3 pos = transform.position;
writer.PutV3cm(pos);  // 12字节

// 接收
Vector3 pos = reader.GetV3cm();

```

### 方向压缩

```csharp
// 发送
Vector3 forward = transform.forward;
writer.PutDir(forward);  // 4字节

// 接收
Vector3 forward = reader.GetDir();

```

### 伤害数据

```csharp
// 发送
DamageInfo di = new DamageInfo { ... };
writer.PutDamagePayload(di);

// 接收
DamageInfo di = reader.GetDamagePayload();

```

### 标志位

```csharp
// 发送
byte flags = PackFlag.PackFlags(true, false, true, false);
writer.Put(flags);  // 1字节

// 接收
byte flags = reader.GetByte();
var (f1, f2, f3, f4) = PackFlag.UnpackFlags(flags);

```

## 依赖关系

**依赖库**：

- LiteNetLib - NetDataWriter/Reader

**被依赖模块**：

- Main/AI - AI 数据压缩

- Main/Health - 伤害数据压缩

- Main/Weapon - 投射物数据压缩

- 所有需要网络传输的模块

## 性能优化

1. **直接字节操作**：避免中间对象创建
2. **位运算**：高效的标志位打包
3. **整数运算**：避免浮点运算开销
4. **缓冲区复用**：配合 NetPacketPool 使用

## 注意事项

1. **精度损失**：
   - 位置精度：±0.5cm

   - 方向精度：±0.0055度

   - 小范围浮点：±0.03125

2. **范围限制**：
   - PutSNorm16只适用于[-8, 8]范围

   - 超出范围会被夹住

3. **字节序**：
   - LiteNetLib 自动处理字节序

   - 跨平台兼容

4. **版本兼容**：
   - 压缩格式变更需要协议版本升级

   - 确保客户端和服务器版本一致
