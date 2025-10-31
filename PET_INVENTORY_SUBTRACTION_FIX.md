# Pet Inventory Subtraction Fix

## Problem
The tombstone subtraction system was incorrectly trying to subtract pet inventory items from tombstones, causing "item not found" warnings in the logs.

## Root Cause
In the `GetPlayerEquipmentTypeIds()` method in `NetService.cs`, the system was collecting ALL remaining items on the player, including:
- Equipped weapons (correct)
- Player inventory items (correct) 
- **Pet inventory items (incorrect)**

Pet inventory items should NOT be included in the subtraction because:
1. Pet inventory items don't drop on death
2. Pet inventory items are never added to tombstones in the first place
3. Trying to subtract them causes "item not found" errors

## Log Evidence
```
[TOMBSTONE] Found pet inventory item 0: TypeID=1096, Name=FishingRod_LV3(Clone)
[TOMBSTONE] Found pet inventory item 1: TypeID=95, Name=HammerL(Clone)
[TOMBSTONE] Found pet inventory item 2: TypeID=451, Name=Cash(Clone)
...
[TOMBSTONE] Remaining item not found in tombstone (already dropped?): TypeID=1096, lootUid=3
[TOMBSTONE] Remaining item not found in tombstone (already dropped?): TypeID=95, lootUid=3
[TOMBSTONE] Remaining item not found in tombstone (already dropped?): TypeID=451, lootUid=3
```

## Solution
Modified `GetPlayerEquipmentTypeIds()` in `NetService.cs`:

1. **Keep collecting pet inventory items for logging** - but don't add them to the `remainingItemTypeIds` list
2. **Added explanatory comments** - clarifying why pet inventory items are excluded
3. **Improved error messages** - better context when items aren't found in tombstones

## Code Changes

### NetService.cs
```csharp
// 获取宠物背包中的物品 - 但不包含在剩余物品中，因为宠物背包物品不会掉落
var petInventory = PetProxy.PetInventory;
if (petInventory != null)
{
    Debug.Log($"[TOMBSTONE] Checking pet inventory with {petInventory.Content.Count} slots");
    
    for (int i = 0; i < petInventory.Content.Count; i++)
    {
        var item = petInventory.GetItemAt(i);
        if (item != null)
        {
            // 不添加到remainingItemTypeIds，因为宠物背包物品不会掉落，也不应该从墓碑中减去
            Debug.Log($"[TOMBSTONE] Found pet inventory item {i}: TypeID={item.TypeID}, Name={item.name}");
        }
    }
}

Debug.Log($"[TOMBSTONE] Total remaining items found: {remainingItemTypeIds.Count}");
Debug.Log("[TOMBSTONE] 宠物背包不需要减去,因为宠物背包里的物品本来就不会爆也本就不会进墓碑json");
```

### TombstonePersistence.cs
```csharp
else
{
    Debug.LogWarning($"[TOMBSTONE] Remaining item not found in tombstone (may be pet inventory item that shouldn't be subtracted): TypeID={remainingTypeId}, lootUid={lootUid}");
}
```

## Expected Result
After this fix:
1. Pet inventory items will no longer be included in subtraction operations
2. No more "item not found" warnings for pet inventory items
3. Only droppable items (weapons, player inventory) will be subtracted from tombstones
4. Clearer log messages when legitimate items aren't found

## Testing
The fix should eliminate the following log warnings:
```
[TOMBSTONE] Remaining item not found in tombstone (already dropped?): TypeID=1096
[TOMBSTONE] Remaining item not found in tombstone (already dropped?): TypeID=95  
[TOMBSTONE] Remaining item not found in tombstone (already dropped?): TypeID=451
```

While still properly subtracting legitimate droppable items like:
```
[TOMBSTONE] Subtracted remaining item from tombstone: TypeID=1175, lootUid=3
```