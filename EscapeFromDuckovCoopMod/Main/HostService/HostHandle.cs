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

using static EscapeFromDuckovCoopMod.LootNet;
using Object = UnityEngine.Object;

namespace EscapeFromDuckovCoopMod;

public class HostHandle
{
    private NetService Service => NetService.Instance;

    private bool IsServer => Service != null && Service.IsServer;
    private NetManager netManager => Service?.netManager;
    private NetDataWriter writer => Service?.writer;
    private NetPeer connectedPeer => Service?.connectedPeer;
    private PlayerStatus localPlayerStatus => Service?.localPlayerStatus;
    private bool networkStarted => Service != null && Service.networkStarted;
    private Dictionary<NetPeer, GameObject> remoteCharacters => Service?.remoteCharacters;
    private Dictionary<NetPeer, PlayerStatus> playerStatuses => Service?.playerStatuses;
    private Dictionary<string, GameObject> clientRemoteCharacters => Service?.clientRemoteCharacters;

    public void Server_HandlePlayerDeadTree(Vector3 pos, Quaternion rot, ItemSnapshot snap)
    {
        if (!IsServer) return;

        var tmpRoot = ItemTool.BuildItemFromSnapshot(snap);
        if (!tmpRoot)
        {
            Debug.LogWarning("[LOOT] HostDeath BuildItemFromSnapshot failed.");
            return;
        }

        var deadPfb = LootManager.Instance.ResolveDeadLootPrefabOnServer(); // → LootBoxPrefab_Tomb
        var box = InteractableLootbox.CreateFromItem(tmpRoot, pos + Vector3.up * 0.10f, rot, true, deadPfb);
        if (box) DeadLootBox.Instance.Server_OnDeadLootboxSpawned(box, null); // whoDied=null → aiId=0 → 客户端走“玩家坟碑盒”

        if (tmpRoot && tmpRoot.gameObject) Object.Destroy(tmpRoot.gameObject);
    }

    //  主机专用入口：本地构造一份与客户端打包一致的“物品树”
    public void Server_HandleHostDeathViaTree(CharacterMainControl who)
    {
        if (!networkStarted || !IsServer || !who) return;
        var item = who.CharacterItem;
        if (!item) return;

        var pos = who.transform.position;
        var rot = who.characterModel ? who.characterModel.transform.rotation : who.transform.rotation;

        var snap = ItemTool.MakeSnapshot(item); // 本地版“WriteItemSnapshot”
        Server_HandlePlayerDeadTree(pos, rot, snap);
    }

    public void Server_HandlePlayerDeadTreeWithPeer(NetPeer peer, Vector3 pos, Quaternion rot, ItemSnapshot snap)
    {
        if (!IsServer) return;

        var tmpRoot = ItemTool.BuildItemFromSnapshot(snap);
        if (!tmpRoot)
        {
            Debug.LogWarning("[LOOT] HostDeath BuildItemFromSnapshot failed.");
            return;
        }

        var deadPfb = LootManager.Instance.ResolveDeadLootPrefabOnServer(); // → LootBoxPrefab_Tomb
        var box = InteractableLootbox.CreateFromItem(tmpRoot, pos + Vector3.up * 0.10f, rot, true, deadPfb);
        if (box) 
        {
            DeadLootBox.Instance.Server_OnDeadLootboxSpawned(box, null); // whoDied=null → aiId=0 → 客户端走"玩家坟碑盒"
            
            // 获取lootUid并保存墓碑数据
            var lootUid = box.GetComponent<LootboxTag>()?.lootUid ?? 0;
            if (lootUid > 0 && peer != null)
            {
                var userId = GetPlayerId(peer);
                var tombstoneId = $"{userId}_{System.DateTime.Now.Ticks}";
                
                // 保存完整的墓碑数据
                TombstonePersistence.Instance.SavePlayerTombstone(userId, tombstoneId, pos, rot, lootUid, snap);
                
                // 请求客户端上报剩余装备
                RequestPlayerEquipment(peer, userId, tombstoneId, lootUid);
            }
        }

        if (tmpRoot && tmpRoot.gameObject) Object.Destroy(tmpRoot.gameObject);
    }

    private void RequestPlayerEquipment(NetPeer peer, string userId, string tombstoneId, int lootUid)
    {
        if (peer == null) return;

        Debug.Log($"[TOMBSTONE] Requesting equipment from player {userId} for tombstone {tombstoneId}");

        var w = writer;
        w.Reset();
        w.Put((byte)Op.PLAYER_EQUIPMENT_REQUEST);
        w.Put(userId);
        w.Put(tombstoneId);
        w.Put(lootUid);

        peer.Send(w, DeliveryMethod.ReliableOrdered);
    }

    public void Server_HandlePlayerDeathEquipment(NetPeer peer, NetPacketReader reader)
    {
        if (!IsServer) return;

        var userId = GetPlayerId(peer);
        var equipmentCount = reader.GetInt();
        var remainingEquipment = new List<ItemSnapshot>();

        Debug.Log($"[TOMBSTONE] Received equipment report from {userId}: {equipmentCount} items");

        for (int i = 0; i < equipmentCount; i++)
        {
            var itemSnapshot = ItemTool.ReadItemSnapshot(reader);
            remainingEquipment.Add(itemSnapshot);
        }

        // 从所有墓碑中减去剩余装备（使用最新的墓碑）
        if (TombstonePersistence.Instance != null)
        {
            // 这里简化处理，实际应该根据tombstoneId来处理
            // 由于当前架构限制，我们使用一个简化的方法
            var tombstoneId = $"latest_{userId}";
            TombstonePersistence.Instance.RemoveEquipmentFromTombstone(userId, tombstoneId, 0, remainingEquipment);
        }
    }

    private string GetPlayerId(NetPeer peer)
    {
        if (peer == null)
        {
            if (localPlayerStatus != null && !string.IsNullOrEmpty(localPlayerStatus.EndPoint))
                return localPlayerStatus.EndPoint;
            return $"Host:{Service?.port ?? 9050}";
        }

        if (playerStatuses != null && playerStatuses.TryGetValue(peer, out var st) &&
            !string.IsNullOrEmpty(st.EndPoint))
            return st.EndPoint;
        return peer.EndPoint.ToString();
    }
}