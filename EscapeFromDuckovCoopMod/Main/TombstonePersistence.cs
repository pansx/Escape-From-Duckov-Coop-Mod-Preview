// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team
//
// This program is not a free software.
// It's distributed under a license based on AGPL-3.0,
// with strict additional restrictions:
//  YOU MUST NOT use this software for commercial purposes.
//  YOU MUST NOT use this software to run a headless game server.
//  YOU MUST include a conspicuous notice of attribution to
//  Mr-sans-and-InitLoader-s-team/Escape-From-Duckov-Coop-Mod-Preview as the original author.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace EscapeFromDuckovCoopMod
{
    [Serializable]
    public class TombstoneItem
    {
        public int position;
        public ItemSnapshot snapshot;
    }

    [Serializable]
    public class TombstoneData
    {
        public List<TombstoneItem> items = new List<TombstoneItem>();
        public Vector3 position;
        public Quaternion rotation;
        public int lootUid;
    }

    [Serializable]
    public class UserTombstoneData
    {
        public Dictionary<string, TombstoneData> tombstones = new Dictionary<string, TombstoneData>();
    }

    public class TombstonePersistence : MonoBehaviour
    {
        public static TombstonePersistence Instance;
        private readonly Dictionary<string, UserTombstoneData> _userData = new Dictionary<string, UserTombstoneData>();
        private readonly string _dataPath = Path.Combine(Application.persistentDataPath, "TombstoneData");

        private void Awake()
        {
            Instance = this;
            if (!Directory.Exists(_dataPath))
                Directory.CreateDirectory(_dataPath);
        }

        public void SavePlayerTombstone(string userId, string tombstoneId, Vector3 position, Quaternion rotation, int lootUid, ItemSnapshot itemSnapshot)
        {
            if (!_userData.TryGetValue(userId, out var userData))
            {
                userData = new UserTombstoneData();
                _userData[userId] = userData;
            }

            var tombstone = new TombstoneData
            {
                position = position,
                rotation = rotation,
                lootUid = lootUid,
                items = new List<TombstoneItem>()
            };

            // 递归提取所有物品到平铺列表
            ExtractItemsRecursively(itemSnapshot, tombstone.items, 0);

            userData.tombstones[tombstoneId] = tombstone;
            SaveUserData(userId, userData);

            Debug.Log($"[TOMBSTONE] Saved tombstone for user {userId}: {tombstone.items.Count} items, lootUid={lootUid}");
        }

        private void ExtractItemsRecursively(ItemSnapshot snapshot, List<TombstoneItem> items, int basePosition)
        {
            // 添加当前物品
            items.Add(new TombstoneItem
            {
                position = basePosition,
                snapshot = snapshot
            });

            // 递归处理槽位物品
            if (snapshot.slots != null)
            {
                foreach (var (key, slotSnapshot) in snapshot.slots)
                {
                    ExtractItemsRecursively(slotSnapshot, items, items.Count);
                }
            }

            // 递归处理容器内物品
            if (snapshot.inventory != null)
            {
                foreach (var invSnapshot in snapshot.inventory)
                {
                    ExtractItemsRecursively(invSnapshot, items, items.Count);
                }
            }
        }

        public void RemoveEquipmentFromTombstone(string userId, string tombstoneId, int lootUid, List<ItemSnapshot> remainingEquipment)
        {
            if (!_userData.TryGetValue(userId, out var userData) || 
                !userData.tombstones.TryGetValue(tombstoneId, out var tombstone))
            {
                Debug.LogWarning($"[TOMBSTONE] No tombstone found for user {userId}, tombstone {tombstoneId}");
                return;
            }

            Debug.Log($"[TOMBSTONE] Starting subtraction for user {userId}: tombstone has {tombstone.items.Count} items, removing {remainingEquipment.Count} remaining items");

            var removedCount = 0;
            var initialCount = tombstone.items.Count;

            // 对每个剩余装备，从墓碑中移除匹配的物品
            foreach (var remainingItem in remainingEquipment)
            {
                for (int i = tombstone.items.Count - 1; i >= 0; i--)
                {
                    var tombstoneItem = tombstone.items[i];
                    if (ItemSnapshotsMatch(tombstoneItem.snapshot, remainingItem))
                    {
                        tombstone.items.RemoveAt(i);
                        removedCount++;
                        Debug.Log($"[TOMBSTONE] Removed item: TypeID={remainingItem.typeId}, Stack={remainingItem.stack}");
                        break; // 只移除第一个匹配的物品
                    }
                }
            }

            if (removedCount > 0)
            {
                SaveUserData(userId, userData);
                Debug.Log($"[TOMBSTONE] Subtraction complete: removed {removedCount} items from tombstone, remaining items={tombstone.items.Count}");
                Debug.Log($"[TOMBSTONE] Final tombstone contains {tombstone.items.Count} droppable items");

                // 同步更新游戏中的墓碑Inventory
                UpdateGameTombstoneInventory(lootUid, tombstone);
            }
            else
            {
                Debug.Log($"[TOMBSTONE] No items were removed from tombstone");
            }
        }

        private void UpdateGameTombstoneInventory(int lootUid, TombstoneData tombstoneData)
        {
            // 1. 从LootManager中找到对应的Inventory
            var lootManager = LootManager.Instance;
            if (!lootManager._srvLootByUid.TryGetValue(lootUid, out var inventory) || inventory == null)
            {
                Debug.LogWarning($"[TOMBSTONE] Could not find inventory for lootUid={lootUid}");
                return;
            }

            Debug.Log($"[TOMBSTONE] Found game tombstone inventory with {inventory.Content.Count} items");

            // 2. 清空当前库存 - 逐个移除所有物品
            var itemCount = inventory.Content.Count;
            for (int i = itemCount - 1; i >= 0; i--)
            {
                var item = inventory.GetItemAt(i);
                if (item != null)
                {
                    inventory.RemoveAt(i, out _);
                }
            }

            // 3. 根据JSON数据重建库存
            var rebuiltCount = 0;
            foreach (var tombstoneItem in tombstoneData.items)
            {
                try
                {
                    var item = ItemTool.BuildItemFromSnapshot(tombstoneItem.snapshot);
                    if (item != null)
                    {
                        inventory.AddAt(item, tombstoneItem.position);
                        rebuiltCount++;
                        Debug.Log($"[TOMBSTONE] Rebuilt item in game inventory: TypeID={item.TypeID}, Name={item.name}, Position={tombstoneItem.position}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TOMBSTONE] Error rebuilding item at position {tombstoneItem.position}: {e}");
                }
            }

            Debug.Log($"[TOMBSTONE] Game tombstone inventory updated: {rebuiltCount} items rebuilt");

            // 4. 广播更新给所有客户端
            if (COOPManager.LootNet != null)
            {
                Debug.Log($"[TOMBSTONE] Broadcasting updated tombstone state to all clients: lootUid={lootUid}");
                COOPManager.LootNet.Server_SendLootboxState(null, inventory);
            }
        }

        private bool ItemSnapshotsMatch(ItemSnapshot a, ItemSnapshot b)
        {
            return a.typeId == b.typeId && 
                   a.stack == b.stack && 
                   Math.Abs(a.durability - b.durability) < 0.001f &&
                   Math.Abs(a.durabilityLoss - b.durabilityLoss) < 0.001f;
        }

        private void SaveUserData(string userId, UserTombstoneData userData)
        {
            try
            {
                var filePath = Path.Combine(_dataPath, $"{userId}.json");
                var json = JsonConvert.SerializeObject(userData, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TOMBSTONE] Failed to save user data for {userId}: {e}");
            }
        }

        private UserTombstoneData LoadUserData(string userId)
        {
            try
            {
                var filePath = Path.Combine(_dataPath, $"{userId}.json");
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<UserTombstoneData>(json) ?? new UserTombstoneData();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TOMBSTONE] Failed to load user data for {userId}: {e}");
            }

            return new UserTombstoneData();
        }
    }
}