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

using System.Collections;
using ItemStatsSystem;
using UnityEngine.SceneManagement;

namespace EscapeFromDuckovCoopMod;

public class DeadLootBox : MonoBehaviour
{
    public const bool EAGER_BROADCAST_LOOT_STATE_ON_SPAWN = true;
    public static DeadLootBox Instance;

    private NetService Service => NetService.Instance;
    private bool IsServer => Service != null && Service.IsServer;
    private NetManager netManager => Service?.netManager;
    private NetDataWriter writer => Service?.writer;
    private NetPeer connectedPeer => Service?.connectedPeer;
    private PlayerStatus localPlayerStatus => Service?.localPlayerStatus;
    private bool networkStarted => Service != null && Service.networkStarted;


    public void Init()
    {
        Instance = this;
    }

    public void SpawnDeadLootboxAt(int aiId, int lootUid, Vector3 pos, Quaternion rot)
    {
        Debug.Log($"[DEATH-DEBUG] SpawnDeadLootboxAt called - aiId:{aiId}, lootUid:{lootUid}, pos:{pos}, rot:{rot}");
        
        try
        {
            // 检查是否已经存在相同lootUid的墓碑，并且游戏对象仍然有效
            if (lootUid >= 0 && LootManager.Instance._cliLootByUid.ContainsKey(lootUid))
            {
                var existingInv = LootManager.Instance._cliLootByUid[lootUid];
                if (existingInv != null && existingInv.gameObject != null)
                {
                    Debug.Log($"[DEATH-DEBUG] Tombstone with lootUid {lootUid} already exists on client, skipping creation");
                    return;
                }
                else
                {
                    Debug.Log($"[DEATH-DEBUG] Tombstone with lootUid {lootUid} exists in dictionary but gameObject is null, removing and recreating");
                    LootManager.Instance._cliLootByUid.Remove(lootUid);
                }
            }
            
            // 清理同一位置的空墓碑（客户端版本）
            CleanupEmptyLootboxesAtPositionClient(pos, 1.0f);
            
            AITool.TryClientRemoveNearestAICorpse(pos, 3.0f);
            Debug.Log("[DEATH-DEBUG] Removed nearest AI corpse");

            var prefab = GetDeadLootPrefabOnClient(aiId);
            if (!prefab)
            {
                Debug.LogWarning("[DEATH-DEBUG] DeadLoot prefab not found on client, spawn aborted.");
                return;
            }

            Debug.Log($"[DEATH-DEBUG] Using dead loot prefab: {prefab.name}");

            var go = Instantiate(prefab, pos, rot);
            var box = go ? go.GetComponent<InteractableLootbox>() : null;
            if (!box) 
            {
                Debug.LogWarning("[DEATH-DEBUG] Failed to get InteractableLootbox component");
                return;
            }

            Debug.Log($"[DEATH-DEBUG] Created lootbox: {box.name}");

            var inv = box.Inventory;
            if (!inv)
            {
                Debug.LogWarning("[DEATH-DEBUG] Client DeadLootBox Spawn - Inventory is null!");
                return;
            }

            Debug.Log($"[DEATH-DEBUG] Got inventory with {inv.Content.Count} items");

            WorldLootPrime.PrimeIfClient(box);
            Debug.Log("[DEATH-DEBUG] Primed lootbox for client");

            // 用主机广播的 pos 注册 posKey → inv（旧兜底仍保留）
            var dict = InteractableLootbox.Inventories;
            if (dict != null)
            {
                var correctKey = LootManager.ComputeLootKeyFromPos(pos);
                var wrongKey = -1;
                foreach (var kv in dict)
                    if (kv.Value == inv && kv.Key != correctKey)
                    {
                        wrongKey = kv.Key;
                        break;
                    }

                if (wrongKey != -1) 
                {
                    dict.Remove(wrongKey);
                    Debug.Log($"[DEATH-DEBUG] Removed wrong key: {wrongKey}");
                }
                dict[correctKey] = inv;
                Debug.Log($"[DEATH-DEBUG] Registered inventory with correct key: {correctKey}");
            }

            //稳定 ID → inv
            if (lootUid >= 0) 
            {
                LootManager.Instance._cliLootByUid[lootUid] = inv;
                Debug.Log($"[DEATH-DEBUG] Registered inventory with lootUid: {lootUid}");
            }

            // 若快照先到，这里优先吃缓存
            if (lootUid >= 0 && LootManager.Instance._pendingLootStatesByUid.TryGetValue(lootUid, out var pack))
            {
                Debug.Log($"[DEATH-DEBUG] Found pending loot state for lootUid: {lootUid}, applying cached state");
                LootManager.Instance._pendingLootStatesByUid.Remove(lootUid);

                COOPManager.LootNet._applyingLootState = true;
                try
                {
                    var cap = Mathf.Clamp(pack.capacity, 1, 128);
                    Debug.Log($"[DEATH-DEBUG] Applying cached state - capacity: {cap}, items: {pack.Item2.Count}");
                    
                    inv.Loading = true; // ★ 进入批量
                    inv.SetCapacity(cap);

                    var removedCount = 0;
                    for (var i = inv.Content.Count - 1; i >= 0; --i)
                    {
                        Item removed;
                        inv.RemoveAt(i, out removed);
                        try
                        {
                            if (removed) 
                            {
                                Destroy(removed.gameObject);
                                removedCount++;
                            }
                        }
                        catch
                        {
                        }
                    }
                    Debug.Log($"[DEATH-DEBUG] Removed {removedCount} existing items");

                    var addedCount = 0;
                    foreach (var (p, snap) in pack.Item2)
                    {
                        var item = ItemTool.BuildItemFromSnapshot(snap);
                        if (item) 
                        {
                            inv.AddAt(item, p);
                            addedCount++;
                        }
                    }
                    Debug.Log($"[DEATH-DEBUG] Added {addedCount} items from cached state");
                }
                finally
                {
                    inv.Loading = false; // ★ 结束批量
                    COOPManager.LootNet._applyingLootState = false;
                    Debug.Log("[DEATH-DEBUG] Finished applying cached loot state");
                }

                WorldLootPrime.PrimeIfClient(box);
                Debug.Log("[DEATH-DEBUG] Applied cached state, skipping request");
                return; // 吃完缓存就不再发请求
            }

            // 正常路径：请求一次状态 + 超时兜底
            Debug.Log("[DEATH-DEBUG] No cached state found, requesting loot state from server");
            COOPManager.LootNet.Client_RequestLootState(inv);
            StartCoroutine(LootManager.Instance.ClearLootLoadingTimeout(inv, 1.5f));
        }
        catch (Exception e)
        {
            Debug.LogError("[DEATH-DEBUG] SpawnDeadLootboxAt failed: " + e);
        }
    }


    private GameObject GetDeadLootPrefabOnClient(int aiId)
    {
        // 1) 首选：死亡 CMC 上的 private deadLootBoxPrefab
        try
        {
            if (aiId > 0 && AITool.aiById.TryGetValue(aiId, out var cmc) && cmc)
            {
                Debug.LogWarning($"[SpawnDeadloot] AiID:{cmc.GetComponent<NetAiTag>().aiId}");
                if (cmc.deadLootBoxPrefab.gameObject == null) Debug.LogWarning("[SPawnDead] deadLootBoxPrefab.gameObject null!");


                if (cmc != null)
                {
                    var obj = cmc.deadLootBoxPrefab.gameObject;
                    if (obj) return obj;
                }
                else
                {
                    Debug.LogWarning("[SPawnDead] cmc is null!");
                }
            }
        }
        catch
        {
        }

        // 2) 兜底：沿用你现有逻辑（Main 或任意 CMC）
        try
        {
            var main = CharacterMainControl.Main;
            if (main)
            {
                var obj = main.deadLootBoxPrefab.gameObject;
                if (obj) return obj;
            }
        }
        catch
        {
        }

        try
        {
            var any = FindObjectOfType<CharacterMainControl>();
            if (any)
            {
                var obj = any.deadLootBoxPrefab.gameObject;
                if (obj) return obj;
            }
        }
        catch
        {
        }

        // 3) 最后兜底：使用 LootManager 的预制体解析
        try
        {
            var lootManager = LootManager.Instance;
            if (lootManager != null)
            {
                var prefab = lootManager.ResolveDeadLootPrefabOnServer();
                if (prefab != null)
                {
                    Debug.Log($"[DEATH-DEBUG] Using LootManager prefab: {prefab.name}");
                    return prefab.gameObject;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DEATH-DEBUG] Failed to get prefab from LootManager: {e.Message}");
        }

        Debug.LogWarning("[DEATH-DEBUG] No dead loot prefab found on client");
        return null;
    }

    public void Server_OnDeadLootboxSpawned(InteractableLootbox box, CharacterMainControl whoDied)
    {
        Debug.Log($"[DEATH-DEBUG] Server_OnDeadLootboxSpawned called - box:{box?.name}, whoDied:{whoDied?.name}, IsServer:{IsServer}");
        
        if (!IsServer || box == null) 
        {
            Debug.Log("[DEATH-DEBUG] Server_OnDeadLootboxSpawned early return - not server or no box");
            return;
        }
        
        try
        {
            // 清理同一位置的空墓碑，避免重叠
            if (LootManager.Instance != null)
            {
                var position = box.transform.position;
                LootManager.Instance.CleanupEmptyLootboxesAtPosition(position, 1.0f);
                Debug.Log($"[DEATH-DEBUG] Cleaned up empty lootboxes at position: {position}");
            }
            // 生成稳定 ID 并登记
            var lootUid = LootManager.Instance._nextLootUid++;
            Debug.Log($"[DEATH-DEBUG] Generated lootUid: {lootUid}");
            
            var inv = box.Inventory;
            if (inv) 
            {
                LootManager.Instance._srvLootByUid[lootUid] = inv;
                Debug.Log($"[DEATH-DEBUG] Registered inventory with lootUid: {lootUid}, item count: {inv.Content.Count}");
            }
            else
            {
                Debug.LogWarning("[DEATH-DEBUG] Box has no inventory!");
            }

            var aiId = 0;
            if (whoDied)
            {
                var tag = whoDied.GetComponent<NetAiTag>();
                if (tag != null) aiId = tag.aiId;
                if (aiId == 0)
                    foreach (var kv in AITool.aiById)
                        if (kv.Value == whoDied)
                        {
                            aiId = kv.Key;
                            break;
                        }
                Debug.Log($"[DEATH-DEBUG] whoDied provided, resolved aiId: {aiId}");
            }
            else
            {
                Debug.Log("[DEATH-DEBUG] whoDied is null, aiId remains 0 (player grave)");
            }

            // >>> 放在 writer.Reset() 之前 <<<
            if (inv != null)
            {
                inv.NeedInspection = true;
                Debug.Log("[DEATH-DEBUG] Set inventory NeedInspection = true");
                // 尝试把“这个箱子以前被搜过”的标记也清空（有的版本有这个字段）
                try
                {
                    Traverse.Create(inv).Field<bool>("hasBeenInspectedInLootBox").Value = false;
                    Debug.Log("[DEATH-DEBUG] Reset hasBeenInspectedInLootBox = false");
                }
                catch (Exception e)
                {
                    Debug.Log($"[DEATH-DEBUG] Could not reset hasBeenInspectedInLootBox: {e.Message}");
                }

                // 把当前内容全部标记为“未鉴定”
                var uninspectedCount = 0;
                for (var i = 0; i < inv.Content.Count; ++i)
                {
                    var it = inv.GetItemAt(i);
                    if (it) 
                    {
                        it.Inspected = false;
                        uninspectedCount++;
                    }
                }
                Debug.Log($"[DEATH-DEBUG] Set {uninspectedCount} items as uninspected");
            }


            // 保存墓碑数据到持久化存储
            var sceneId = LootManager.Instance.GetCurrentSceneId();
            var userId = "unknown";
            
            // 尝试从墓碑标签获取用户ID
            var userIdTag = box.GetComponent<TombstoneUserIdTag>();
            if (userIdTag != null && !string.IsNullOrEmpty(userIdTag.userId))
            {
                userId = userIdTag.userId;
                Debug.Log($"[DEATH-DEBUG] Got userId from tag: {userId}");
            }
            else if (aiId == 0)
            {
                // 玩家墓碑但没有标签，使用默认标识
                userId = "player_grave_" + lootUid;
                Debug.Log($"[DEATH-DEBUG] Using default player grave userId: {userId}");
            }
            else
            {
                // AI墓碑，不进行持久化保存
                Debug.Log($"[DEATH-DEBUG] AI grave detected (aiId={aiId}), skipping tombstone persistence");
                userId = null; // 标记为不保存
            }
            
            // 只对玩家坟墓进行持久化保存
            if (!string.IsNullOrEmpty(userId))
            {
                LootManager.Instance.SaveTombstoneData(userId, lootUid, sceneId, box.transform.position, box.transform.rotation, aiId, inv);
                Debug.Log($"[DEATH-DEBUG] Saved player tombstone data: userId={userId}, lootUid={lootUid}");
            }
            else
            {
                Debug.Log($"[DEATH-DEBUG] Skipped saving AI loot data: aiId={aiId}, lootUid={lootUid}");
            }

            // 稳定 ID
            var sceneIndex = SceneManager.GetActiveScene().buildIndex;
            Debug.Log($"[DEATH-DEBUG] Broadcasting DEAD_LOOT_SPAWN - scene:{sceneIndex}, aiId:{aiId}, lootUid:{lootUid}, pos:{box.transform.position}");
            
            writer.Reset();
            writer.Put((byte)Op.DEAD_LOOT_SPAWN);
            writer.Put(sceneIndex);
            writer.Put(aiId);
            writer.Put(lootUid); // 稳定 ID
            writer.PutV3cm(box.transform.position);
            writer.PutQuaternion(box.transform.rotation);
            netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
            
            Debug.Log("[DEATH-DEBUG] DEAD_LOOT_SPAWN packet sent to all clients");

            if (EAGER_BROADCAST_LOOT_STATE_ON_SPAWN)
                StartCoroutine(RebroadcastDeadLootStateAfterFill(box));
        }
        catch (Exception e)
        {
            Debug.LogError("[LOOT] Server_OnDeadLootboxSpawned failed: " + e);
        }
    }

    public IEnumerator RebroadcastDeadLootStateAfterFill(InteractableLootbox box)
    {
        if (!EAGER_BROADCAST_LOOT_STATE_ON_SPAWN) yield break;

        yield return null; // 给原版填充时间
        yield return null;
        if (box && box.Inventory) COOPManager.LootNet.Server_SendLootboxState(null, box.Inventory);
    }


    public void Server_OnDeadLootboxSpawned(InteractableLootbox box)
    {
        if (!IsServer || box == null) return;
        try
        {
            var lootUid = LootManager.Instance._nextLootUid++;
            var inv = box.Inventory;
            if (inv) LootManager.Instance._srvLootByUid[lootUid] = inv;

            // ★ 新增：抑制“填充期间”的 AddItem 广播
            if (inv) LootManager.Instance.Server_MuteLoot(inv, 2.0f);

            writer.Reset();
            writer.Put((byte)Op.DEAD_LOOT_SPAWN);
            writer.Put(SceneManager.GetActiveScene().buildIndex);
            writer.PutV3cm(box.transform.position);
            writer.PutQuaternion(box.transform.rotation);
            netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);

            // 2) 可选：是否立刻广播整箱内容（默认不广播，等客户端真正打开时再按需请求）
            if (EAGER_BROADCAST_LOOT_STATE_ON_SPAWN) COOPManager.LootNet.Server_SendLootboxState(null, box.Inventory); // 如需老行为，打开上面的开关即可
        }
        catch (Exception e)
        {
            Debug.LogError("[LOOT] Server_OnDeadLootboxSpawned failed: " + e);
        }
    }

    /// <summary>
    /// 客户端版本：清理指定位置附近的空墓碑
    /// </summary>
    private void CleanupEmptyLootboxesAtPositionClient(Vector3 position, float radius)
    {
        try
        {
            var lootboxes = UnityEngine.Object.FindObjectsOfType<InteractableLootbox>();
            var cleanedCount = 0;

            foreach (var lootbox in lootboxes)
            {
                if (lootbox == null || lootbox.gameObject == null)
                {
                    continue;
                }

                // 检查距离
                var distance = Vector3.Distance(lootbox.transform.position, position);
                if (distance > radius)
                {
                    continue;
                }

                // 检查是否为空
                var inventory = lootbox.Inventory;
                if (inventory != null && IsInventoryEmptyClient(inventory))
                {
                    Debug.Log($"[DEATH-DEBUG] Client cleaning up empty lootbox at position: {lootbox.transform.position}, distance: {distance:F2}");
                    
                    // 从客户端字典中移除
                    var dict = InteractableLootbox.Inventories;
                    if (dict != null)
                    {
                        var keysToRemove = new List<int>();
                        foreach (var kv in dict)
                        {
                            if (kv.Value == inventory)
                            {
                                keysToRemove.Add(kv.Key);
                            }
                        }
                        foreach (var key in keysToRemove)
                        {
                            dict.Remove(key);
                        }
                    }
                    
                    // 从客户端墓碑字典中移除
                    if (LootManager.Instance != null)
                    {
                        var uidsToRemove = new List<int>();
                        foreach (var kv in LootManager.Instance._cliLootByUid)
                        {
                            if (kv.Value == inventory)
                            {
                                uidsToRemove.Add(kv.Key);
                            }
                        }
                        foreach (var uid in uidsToRemove)
                        {
                            LootManager.Instance._cliLootByUid.Remove(uid);
                        }
                    }
                    
                    // 销毁游戏对象
                    UnityEngine.Object.Destroy(lootbox.gameObject);
                    cleanedCount++;
                }
            }

            if (cleanedCount > 0)
            {
                Debug.Log($"[DEATH-DEBUG] Client cleaned up {cleanedCount} empty lootboxes at position {position}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DEATH-DEBUG] Client failed to cleanup empty lootboxes at position: {e}");
        }
    }

    /// <summary>
    /// 客户端版本：检查Inventory是否为空
    /// </summary>
    private bool IsInventoryEmptyClient(Inventory inventory)
    {
        if (inventory == null)
        {
            return true;
        }

        try
        {
            // 检查Content列表
            if (inventory.Content == null || inventory.Content.Count == 0)
            {
                return true;
            }

            // 检查是否所有位置都是null
            var hasItems = false;
            for (int i = 0; i < inventory.Content.Count; i++)
            {
                var item = inventory.GetItemAt(i);
                if (item != null)
                {
                    hasItems = true;
                    break;
                }
            }

            return !hasItems;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DEATH-DEBUG] Client error checking if inventory is empty: {e.Message}");
            return true; // 出错时认为是空的，需要清理
        }
    }
}