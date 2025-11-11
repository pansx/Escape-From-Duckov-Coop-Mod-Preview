# SyncData 模块

## 概述

SyncData 模块定义网络同步数据结构，用于序列化和反序列化需要在网络上传输的游戏状态数据。

## 模块结构

```

SyncData/
└── SyncDataManger.cs    - 同步数据定义

```

## 核心类

### 1. EquipmentSyncData - 装备同步数据

**功能**：封装装备槽位和物品ID的同步数据。

**属性**：```csharp
public string ItemID      // 物品 ID（如"Armor_Vest_01"）
public int SlotHash       // 装备槽位哈希值

```

**方法**：```csharp
public void Serialize(NetDataWriter writer)                      // 序列化到网络包
public static EquipmentSyncData Deserialize(NetPacketReader reader)  // 从网络包反序列化

```

**序列化格式**：```

[SlotHash: int][ItemID: string]

```

**使用场景**：- 玩家装备护甲

- 玩家装备头盔

- 玩家装备背包

- 玩家装备面罩

- 玩家装备耳机

### 2. WeaponSyncData - 武器同步数据

**功能**：封装武器槽位和物品ID的同步数据。

**属性**：```csharp
public string ItemID      // 武器 ID（如"Weapon_AK47"）
public int SlotHash       // 武器槽位哈希值

```

**方法**：```csharp
public void Serialize(NetDataWriter writer)                    // 序列化到网络包
public static WeaponSyncData Deserialize(NetPacketReader reader)  // 从网络包反序列化

```

**序列化格式**：```

[SlotHash: int][ItemID: string]

```

**使用场景**：- 玩家切换主武器

- 玩家切换副武器

- 玩家切换近战武器

- 玩家装备手榴弹

### 3. SyncDataManger - 同步数据管理器

**功能**：静态类，用于管理同步数据（当前为空，预留扩展）。

**潜在用途**：- 数据池管理

- 序列化工具方法

- 数据验证方法

## 设计模式

### 1. 数据传输对象（DTO）模式

EquipmentSyncData 和 WeaponSyncData 是典型的 DTO：

- 只包含数据，无业务逻辑

- 提供序列化/反序列化方法

- 用于网络传输

### 2. 静态工厂方法模式

使用静态 Deserialize方法创建实例：

```csharp
var data = EquipmentSyncData.Deserialize(reader);

```

## 使用示例

### 序列化装备数据

```csharp
var equipData = new EquipmentSyncData
{
    SlotHash = Animator.StringToHash("Armor"),
    ItemId = "Armor_Vest_01"
};

var writer = new NetDataWriter();
writer.Put((byte)Op.EQUIPMENT_UPDATE);
equipData.Serialize(writer);
netManager.SendSmart(writer, Op.EQUIPMENT_UPDATE);

```

### 反序列化装备数据

```csharp
void HandleEquipmentUpdate(NetPacketReader reader)
{
    var equipData = EquipmentSyncData.Deserialize(reader);
    ApplyEquipment(equipData.SlotHash, equipData.ItemId);
}

```

### 序列化武器数据

```csharp
var weaponData = new WeaponSyncData
{
    SlotHash = (int)HandheldSocketTypes.normalHandheld,
    ItemId = "Weapon_AK47"
};

var writer = new NetDataWriter();
writer.Put((byte)Op.PLAYERWEAPON_UPDATE);
weaponData.Serialize(writer);
netManager.SendSmart(writer, Op.PLAYERWEAPON_UPDATE);

```

### 反序列化武器数据

```csharp
void HandleWeaponUpdate(NetPacketReader reader)
{
    var weaponData = WeaponSyncData.Deserialize(reader);
    ApplyWeapon(weaponData.SlotHash, weaponData.ItemId);
}

```

## 数据格式

### 装备槽位哈希值

使用 Animator.StringToHash生成：

```csharp
int armorSlot = Animator.StringToHash("Armor");
int helmatSlot = Animator.StringToHash("Helmat");
int backpackSlot = Animator.StringToHash("Backpack");
int faceMaskSlot = Animator.StringToHash("FaceMask");
int headsetSlot = Animator.StringToHash("Headset");

```

### 武器槽位类型

使用HandheldSocketTypes枚举：

```csharp
public enum HandheldSocketTypes
{
    normalHandheld = 0,    // 右手（主武器）
    meleeWeapon = 1,       // 近战武器
    leftHandSocket = 2     // 左手（副武器）
}

```

### 物品ID格式

物品ID是字符串，格式示例：

- 武器: "Weapon_AK47", "Weapon_M4A1"

- 护甲: "Armor_Vest_01", "Armor_Vest_02"

- 头盔: "Helmat_Military_01"

- 背包: "Backpack_Large_01"

## 性能优化

### 1. 紧凑的序列化格式

- 使用 int 而非 string 存储槽位（4字节 vs 可变长度）

- 只传输必要的数据字段

### 2. 静态方法

Deserialize 使用静态方法，避免创建临时对象。

### 3. 空字符串处理

序列化时处理null：

```csharp
writer.Put(ItemId ?? "");

```

## 扩展性

### 潜在扩展

1. 添加更多同步数据类型：
   - BuffSyncData: Buff 状态同步

   - HealthSyncData: 生命值同步

   - InventorySyncData: 背包同步

2. 添加数据验证：
   - 验证 ItemId 是否存在

   - 验证 SlotHash 是否合法

3. 添加数据压缩：
   - 使用字典压缩 ItemID

   - 使用位字段压缩多个 bool 值

## 总结

SyncData 模块提供了简洁的数据同步结构，用于网络传输装备和武器状态。该模块采用 DTO 模式，提供了高效的序列化/反序列化方法。

**核心特性**：- 简洁的数据结构

- 高效的序列化方法

- 易于扩展

- 类型安全
