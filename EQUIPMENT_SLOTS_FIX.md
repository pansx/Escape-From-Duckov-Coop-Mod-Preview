# 装备槽位检测修复

## 问题描述
用户反馈：图腾的TypeID是966，装备在身上，但上报时没有检测到。

## 根本原因分析
从日志分析发现：
- 死亡时背包里的物品：`PoliceStick(Clone) (TypeID: 772)` 和 `Potato(Clone) (TypeID: 403)`
- 上报的剩余物品：只有 `TypeID=1175`（近战武器）
- **图腾 TypeID=966 没有被检测到**

问题在于 `GetPlayerEquipmentTypeIds` 方法只检查了：
1. ✅ 远程武器 (`GetGun()`)
2. ✅ 近战武器 (`GetMeleeWeapon()`)
3. ✅ 背包物品 (`PlayerStorage.Inventory`)

但**没有检查角色身上的装备槽位**，包括：
- 图腾槽
- 护甲槽
- 头盔槽
- 其他装备槽

## 解决方案
在 `GetPlayerEquipmentTypeIds` 方法中添加对角色身上所有装备槽位的检查。

## 修改内容

### NetService.cs - `GetPlayerEquipmentTypeIds` 方法
在近战武器检查后添加装备槽位检查：

```csharp
// 获取角色身上的所有装备槽位（包括图腾等）
var characterItem = mainControl.CharacterItem;
if (characterItem != null && characterItem.Slots != null)
{
    Debug.Log($"[TOMBSTONE] Checking character equipment slots");
    
    foreach (var slot in characterItem.Slots)
    {
        if (slot != null && slot.Content != null)
        {
            remainingItemTypeIds.Add(slot.Content.TypeID);
            Debug.Log($"[TOMBSTONE] Found equipped item in slot '{slot.Key}': TypeID={slot.Content.TypeID}, Name={slot.Content.name}");
        }
    }
}
else
{
    Debug.LogWarning("[TOMBSTONE] Character equipment slots not found");
}
```

## 完整检查流程

### 1. 远程武器检查
```csharp
var rangedWeapon = mainControl.GetGun();
if (rangedWeapon != null && rangedWeapon.Item != null)
{
    remainingItemTypeIds.Add(rangedWeapon.Item.TypeID);
    Debug.Log($"[TOMBSTONE] Found ranged weapon: TypeID={rangedWeapon.Item.TypeID}");
}
```

### 2. 近战武器检查
```csharp
var meleeWeapon = mainControl.GetMeleeWeapon();
if (meleeWeapon != null && meleeWeapon.Item != null)
{
    remainingItemTypeIds.Add(meleeWeapon.Item.TypeID);
    Debug.Log($"[TOMBSTONE] Found melee weapon: TypeID={meleeWeapon.Item.TypeID}");
}
```

### 3. 装备槽位检查（新增）
```csharp
var characterItem = mainControl.CharacterItem;
if (characterItem != null && characterItem.Slots != null)
{
    foreach (var slot in characterItem.Slots)
    {
        if (slot != null && slot.Content != null)
        {
            remainingItemTypeIds.Add(slot.Content.TypeID);
            Debug.Log($"[TOMBSTONE] Found equipped item in slot '{slot.Key}': TypeID={slot.Content.TypeID}, Name={slot.Content.name}");
        }
    }
}
```

### 4. 背包物品检查
```csharp
var playerInventory = PlayerStorage.Inventory;
if (playerInventory != null)
{
    for (int i = 0; i < playerInventory.Content.Count; i++)
    {
        var item = playerInventory.GetItemAt(i);
        if (item != null)
        {
            remainingItemTypeIds.Add(item.TypeID);
            Debug.Log($"[TOMBSTONE] Found inventory item {i}: TypeID={item.TypeID}, Name={item.name}");
        }
    }
}
```

## 预期日志输出

### 装备槽位检查
```
[TOMBSTONE] Checking character equipment slots
[TOMBSTONE] Found equipped item in slot 'TotemSlot': TypeID=966, Name=LuckTotem(Clone)
[TOMBSTONE] Found equipped item in slot 'ArmorSlot': TypeID=1001, Name=BasicArmor(Clone)
[TOMBSTONE] Found equipped item in slot 'HelmetSlot': TypeID=2001, Name=TacticalHelmet(Clone)
```

### 完整上报结果
```
[TOMBSTONE] Found melee weapon: TypeID=1175
[TOMBSTONE] Found equipped item in slot 'TotemSlot': TypeID=966, Name=LuckTotem(Clone)
[TOMBSTONE] Found inventory item 0: TypeID=403, Name=Potato(Clone)
[TOMBSTONE] Total remaining items found: 3
[TOMBSTONE] Reporting remaining item: TypeID=1175
[TOMBSTONE] Reporting remaining item: TypeID=966
[TOMBSTONE] Reporting remaining item: TypeID=403
```

## 关键特性

### 🔍 全面检查
- **武器检查**：远程武器 + 近战武器
- **装备检查**：所有角色装备槽位
- **背包检查**：背包中的所有物品
- **宠物背包**：记录但不上报（因为不会掉落）

### 📊 详细日志
- 每个检查阶段都有详细日志
- 显示物品的槽位名称、TypeID和名称
- 便于调试和验证

### 🛡️ 错误处理
- 检查每个组件是否存在
- 防止空引用异常
- 提供警告信息

## 预期效果

### ✅ 图腾被正确检测
- 图腾 TypeID=966 将被正确检测到
- 在装备槽位检查中找到图腾
- 上报给服务端进行减法操作

### ✅ 所有装备被检测
- 护甲、头盔、配件等所有装备
- 不遗漏任何装备在身上的物品
- 确保减法操作的完整性

### ✅ 减法操作正确
- 服务端收到完整的剩余物品列表
- 从墓碑中正确减去所有剩余物品
- 墓碑只包含真正掉落的物品

## 编译状态
✅ **编译成功** - 修改已通过编译测试

## 测试建议
1. 装备图腾（TypeID=966）后死亡
2. 检查日志确认图腾被检测到
3. 验证墓碑中不包含图腾
4. 测试其他装备槽位的物品
5. 确认减法操作正确执行

## 总结
这个修复解决了装备槽位物品检测不完整的问题，确保了：
- 所有装备在身上的物品都被正确检测
- 图腾等装备物品能被正确上报
- 减法操作能够完整执行
- 墓碑显示真正的掉落物品