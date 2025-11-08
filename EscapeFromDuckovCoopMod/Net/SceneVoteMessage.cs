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

using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using UnityEngine;

namespace EscapeFromDuckovCoopMod.Net;

/// <summary>
/// 场景投票 JSON 消息系统
/// 支持每秒广播和中途加入
/// </summary>
public static class SceneVoteMessage
{
    /// <summary>
    /// 玩家信息（包含ID、名称、SteamID）
    /// </summary>
    [System.Serializable]
    public class PlayerInfo
    {
        public string playerId; // 玩家网络ID（如 "Host:9050" 或 "192.168.1.1:9050"）
        public string playerName; // 玩家名称
        public string steamId; // Steam ID（如果有）
        public bool ready; // 是否准备
    }

    /// <summary>
    /// 玩家列表包装类（Unity JsonUtility 需要）
    /// </summary>
    [System.Serializable]
    public class PlayerList
    {
        public PlayerInfo[] items;
    }

    /// <summary>
    /// 投票状态数据结构
    /// </summary>
    [System.Serializable]
    public class VoteStateData
    {
        public string type = "sceneVote";
        public bool active; // 投票是否激活
        public string targetSceneId; // 目标场景ID
        public string curtainGuid; // 过场GUID
        public string locationName; // 位置名称
        public bool notifyEvac; // 是否通知撤离
        public bool saveToFile; // 是否保存到文件
        public bool useLocation; // 是否使用位置
        public string hostSceneId; // 主机当前场景ID
        public PlayerList playerList; // 🔧 使用包装类，Unity JsonUtility 才能正确序列化
        public string timestamp; // 时间戳
    }

    /// <summary>
    /// 玩家准备状态（向后兼容，已废弃）
    /// </summary>
    [System.Serializable]
    [System.Obsolete("使用 PlayerInfo 代替")]
    public class PlayerReadyState
    {
        public string playerId; // 玩家ID
        public string playerName; // 玩家名称
        public bool ready; // 是否准备
    }

    /// <summary>
    /// 客户端投票请求数据结构
    /// </summary>
    [System.Serializable]
    public class VoteRequestData
    {
        public string type = "sceneVoteRequest";
        public string targetSceneId;
        public string curtainGuid;
        public string locationName;
        public bool notifyEvac;
        public bool saveToFile;
        public bool useLocation;
        public string timestamp;
    }

    /// <summary>
    /// 客户端准备状态切换数据结构
    /// </summary>
    [System.Serializable]
    public class ReadyToggleData
    {
        public string type = "sceneVoteReady";
        public string playerId;
        public bool ready;
        public string timestamp;
    }

    // 主机端：当前投票状态缓存
    private static VoteStateData _hostVoteState = null;
    private static float _lastBroadcastTime = 0f;
    private const float BROADCAST_INTERVAL = 1.0f; // 每秒广播一次

    /// <summary>
    /// 主机：开始投票
    /// </summary>
    public static void Host_StartVote(
        string targetSceneId,
        string curtainGuid,
        bool notifyEvac,
        bool saveToFile,
        bool useLocation,
        string locationName
    )
    {
        var service = NetService.Instance;
        if (service == null || !service.IsServer)
        {
            Debug.LogWarning("[SceneVote] 只有主机可以发起投票");
            return;
        }

        // 计算主机当前场景ID
        string hostSceneId = null;
        LocalPlayerManager.Instance.ComputeIsInGame(out hostSceneId);
        hostSceneId = hostSceneId ?? string.Empty;

        // 🔧 构建玩家信息列表（包含ID、名称、SteamID）
        var players = new List<PlayerInfo>();

        // 添加主机自己
        var hostId = service.GetPlayerId(null);
        var hostName = service.localPlayerStatus?.PlayerName ?? "Host";
        var hostSteamId = GetSteamId(null); // 主机的SteamID
        players.Add(
            new PlayerInfo
            {
                playerId = hostId,
                playerName = hostName,
                steamId = hostSteamId,
                ready = false,
            }
        );

        // 添加所有客户端
        if (service.playerStatuses != null)
        {
            foreach (var kv in service.playerStatuses)
            {
                var peer = kv.Key;
                var status = kv.Value;
                if (peer == null || status == null)
                    continue;

                var clientSteamId = GetSteamId(peer); // 客户端的SteamID
                players.Add(
                    new PlayerInfo
                    {
                        playerId = status.EndPoint,
                        playerName = status.PlayerName ?? "Player",
                        steamId = clientSteamId,
                        ready = false,
                    }
                );
            }
        }

        // 🔍 详细日志：显示所有玩家信息
        Debug.Log(
            $"[SceneVote] 主机构建玩家列表: {string.Join(", ", players.Select(p => $"{p.playerName}({p.playerId})"))}"
        );

        // 创建投票状态
        _hostVoteState = new VoteStateData
        {
            active = true,
            targetSceneId = targetSceneId,
            curtainGuid = curtainGuid,
            locationName = locationName,
            notifyEvac = notifyEvac,
            saveToFile = saveToFile,
            useLocation = useLocation,
            hostSceneId = hostSceneId,
            playerList = new PlayerList { items = players.ToArray() }, // 🔧 使用包装类
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        };

        // 立即广播一次
        Host_BroadcastVoteState();
        _lastBroadcastTime = Time.time;

        Debug.Log($"[SceneVote] 主机发起投票: {targetSceneId}, 参与者: {players.Count}");
    }

    /// <summary>
    /// 主机：广播投票状态（每秒调用）
    /// </summary>
    public static void Host_BroadcastVoteState()
    {
        if (_hostVoteState == null || !_hostVoteState.active)
            return;

        var service = NetService.Instance;
        if (service == null || !service.IsServer)
            return;

        // 更新时间戳
        _hostVoteState.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        // 🔧 使用 Newtonsoft.Json 序列化，确保 playerList 被正确序列化
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(_hostVoteState);
        Debug.Log($"[SceneVote] 主机广播 JSON:\n{json}");

        // 发送给所有客户端
        JsonMessage.BroadcastToAllClients(json, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// 主机：Update 中调用，定期广播
    /// </summary>
    public static void Host_Update()
    {
        if (_hostVoteState == null || !_hostVoteState.active)
            return;

        if (Time.time - _lastBroadcastTime >= BROADCAST_INTERVAL)
        {
            Host_BroadcastVoteState();
            _lastBroadcastTime = Time.time;
        }
    }

    /// <summary>
    /// 主机：处理客户端的准备状态切换
    /// </summary>
    public static void Host_HandleReadyToggle(string playerId, bool ready)
    {
        if (_hostVoteState == null || !_hostVoteState.active)
            return;

        // 🔧 在 playerList 中查找并更新玩家的准备状态
        bool found = false;
        if (_hostVoteState.playerList != null && _hostVoteState.playerList.items != null)
        {
            foreach (var player in _hostVoteState.playerList.items)
            {
                if (player.playerId == playerId)
                {
                    player.ready = ready;
                    found = true;
                    Debug.Log(
                        $"[SceneVote] 玩家 {player.playerName}({playerId}) 准备状态: {ready}"
                    );
                    break;
                }
            }
        }

        if (!found)
        {
            Debug.LogWarning($"[SceneVote] 未找到玩家: {playerId}");
            return;
        }

        // 🔧 同步更新主机的 SceneNet.sceneReady，让 UI 能读取到
        var sceneNet = SceneNet.Instance;
        if (
            sceneNet != null
            && _hostVoteState.playerList != null
            && _hostVoteState.playerList.items != null
        )
        {
            foreach (var player in _hostVoteState.playerList.items)
            {
                sceneNet.sceneReady[player.playerId] = player.ready;
            }
            Debug.Log($"[SceneVote] 已同步更新 SceneNet.sceneReady");
        }

        // 立即广播更新
        Host_BroadcastVoteState();
        Debug.Log($"[SceneVote] 已广播更新的投票状态");

        // 检查是否全员准备
        bool allReady =
            _hostVoteState.playerList != null
            && _hostVoteState.playerList.items != null
            && _hostVoteState.playerList.items.Length > 0
            && _hostVoteState.playerList.items.All(p => p.ready);

        if (allReady)
        {
            Debug.Log("[SceneVote] 全员准备，开始加载场景");
            Host_StartSceneLoad();
        }
    }

    /// <summary>
    /// 主机：开始加载场景
    /// </summary>
    private static void Host_StartSceneLoad()
    {
        if (_hostVoteState == null)
            return;

        // 调用原有的场景加载逻辑
        var sceneNet = SceneNet.Instance;
        if (sceneNet != null)
        {
            sceneNet.sceneTargetId = _hostVoteState.targetSceneId;
            sceneNet.sceneCurtainGuid = _hostVoteState.curtainGuid;
            sceneNet.sceneNotifyEvac = _hostVoteState.notifyEvac;
            sceneNet.sceneSaveToFile = _hostVoteState.saveToFile;
            sceneNet.sceneUseLocation = _hostVoteState.useLocation;
            sceneNet.sceneLocationName = _hostVoteState.locationName;

            // 使用原有的 Server_BroadcastBeginSceneLoad 方法
            // 通过反射调用私有方法
            var method = typeof(SceneNet).GetMethod(
                "Server_BroadcastBeginSceneLoad",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (method != null)
            {
                method.Invoke(sceneNet, null);
            }
        }

        // 清除投票状态
        _hostVoteState.active = false;
        _hostVoteState = null;
    }

    /// <summary>
    /// 主机：取消投票
    /// </summary>
    public static void Host_CancelVote()
    {
        if (_hostVoteState == null)
            return;

        _hostVoteState.active = false;

        // 广播取消状态
        Host_BroadcastVoteState();

        _hostVoteState = null;

        Debug.Log("[SceneVote] 主机取消投票");
    }

    /// <summary>
    /// 客户端：处理接收到的投票状态
    /// </summary>
    public static void Client_HandleVoteState(string json)
    {
        var service = NetService.Instance;
        if (service == null || service.IsServer)
            return;

        // 🔍 输出接收到的完整 JSON
        Debug.Log($"[SceneVote] 客户端收到 JSON:\n{json}");

        try
        {
            // 🔧 使用 Newtonsoft.Json 反序列化，支持嵌套对象
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<VoteStateData>(json);
            if (data == null || data.type != "sceneVote")
            {
                Debug.LogWarning("[SceneVote] 无效的投票状态数据");
                return;
            }

            var sceneNet = SceneNet.Instance;
            if (sceneNet == null)
                return;

            // 如果投票已取消
            if (!data.active)
            {
                if (sceneNet.sceneVoteActive)
                {
                    Debug.Log("[SceneVote] 收到投票取消通知");
                    sceneNet.sceneVoteActive = false;
                    sceneNet.sceneReady.Clear();
                    sceneNet.localReady = false;
                    sceneNet.sceneParticipantIds.Clear();
                }
                return;
            }

            // 检查场景是否匹配
            string mySceneId = null;
            LocalPlayerManager.Instance.ComputeIsInGame(out mySceneId);
            mySceneId = mySceneId ?? string.Empty;

            if (!string.IsNullOrEmpty(data.hostSceneId) && !string.IsNullOrEmpty(mySceneId))
            {
                if (!string.Equals(data.hostSceneId, mySceneId, System.StringComparison.Ordinal))
                {
                    // 不同场景，忽略
                    Debug.Log(
                        $"[SceneVote] 不同场景，忽略投票: host={data.hostSceneId}, me={mySceneId}"
                    );
                    return;
                }
            }

            // 更新投票状态
            sceneNet.sceneVoteActive = true;
            sceneNet.sceneTargetId = data.targetSceneId;
            sceneNet.sceneCurtainGuid = data.curtainGuid;
            sceneNet.sceneLocationName = data.locationName;
            sceneNet.sceneNotifyEvac = data.notifyEvac;
            sceneNet.sceneSaveToFile = data.saveToFile;
            sceneNet.sceneUseLocation = data.useLocation;

            // 🔧 完全依赖主机发送的 players 数组构建参与者列表
            // 客户端不再自己构建列表，以主机为准
            sceneNet.sceneParticipantIds.Clear();
            sceneNet.sceneReady.Clear();

            // 🔍 详细日志：显示收到的玩家信息
            if (data.playerList != null && data.playerList.items != null)
            {
                Debug.Log(
                    $"[SceneVote] 收到 {data.playerList.items.Length} 个玩家信息: {string.Join(", ", data.playerList.items.Select(p => $"{p.playerName}({p.playerId})"))}"
                );

                // 从主机广播的 playerList 解析玩家列表和准备状态
                foreach (var player in data.playerList.items)
                {
                    if (string.IsNullOrEmpty(player.playerId))
                        continue;

                    Debug.Log(
                        $"[SceneVote] 解析玩家: name='{player.playerName}', id='{player.playerId}', steamId='{player.steamId}', ready={player.ready}"
                    );

                    // 添加到参与者列表
                    if (!sceneNet.sceneParticipantIds.Contains(player.playerId))
                    {
                        sceneNet.sceneParticipantIds.Add(player.playerId);
                        Debug.Log(
                            $"[SceneVote] 添加参与者: {player.playerName}({player.playerId}), IsSelfId={service.IsSelfId(player.playerId)}"
                        );
                    }
                    sceneNet.sceneReady[player.playerId] = player.ready;

                    // 检查是否是自己，更新本地准备状态
                    if (service.IsSelfId(player.playerId))
                    {
                        sceneNet.localReady = player.ready;
                        Debug.Log(
                            $"[SceneVote] 识别到自己: {player.playerName}({player.playerId})"
                        );
                    }
                }
            }
            else
            {
                Debug.LogWarning("[SceneVote] 收到的投票状态没有玩家信息");
            }

            Debug.Log(
                $"[SceneVote] 更新投票状态: {data.targetSceneId}, 参与者: {sceneNet.sceneParticipantIds.Count}"
            );
            Debug.Log($"[SceneVote] 参与者列表: {string.Join(", ", sceneNet.sceneParticipantIds)}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SceneVote] 处理投票状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 客户端：切换准备状态
    /// </summary>
    public static void Client_ToggleReady(bool ready)
    {
        var service = NetService.Instance;
        if (service == null || service.IsServer)
            return;

        var myId = service.localPlayerStatus?.EndPoint ?? "";
        if (string.IsNullOrEmpty(myId))
        {
            Debug.LogWarning("[SceneVote] 无法获取本地玩家ID");
            return;
        }

        var data = new ReadyToggleData
        {
            playerId = myId,
            ready = ready,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        };

        Debug.Log($"[SceneVote] 客户端发送准备状态切换: playerId={myId}, ready={ready}");
        JsonMessage.SendToHost(data, DeliveryMethod.ReliableOrdered);

        // 本地乐观更新
        var sceneNet = SceneNet.Instance;
        if (sceneNet != null && sceneNet.sceneVoteActive)
        {
            sceneNet.localReady = ready;
            if (sceneNet.sceneReady.ContainsKey(myId))
            {
                sceneNet.sceneReady[myId] = ready;
            }
            Debug.Log($"[SceneVote] 本地乐观更新完成");
        }

        Debug.Log($"[SceneVote] 客户端切换准备状态: {ready}");
    }

    /// <summary>
    /// 客户端：请求发起投票
    /// </summary>
    public static void Client_RequestVote(
        string targetSceneId,
        string curtainGuid,
        bool notifyEvac,
        bool saveToFile,
        bool useLocation,
        string locationName
    )
    {
        var service = NetService.Instance;
        if (service == null || service.IsServer)
            return;

        var data = new VoteRequestData
        {
            targetSceneId = targetSceneId,
            curtainGuid = curtainGuid,
            locationName = locationName,
            notifyEvac = notifyEvac,
            saveToFile = saveToFile,
            useLocation = useLocation,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        };

        JsonMessage.SendToHost(data, DeliveryMethod.ReliableOrdered);

        Debug.Log($"[SceneVote] 客户端请求发起投票: {targetSceneId}");
    }

    /// <summary>
    /// 主机：处理客户端的投票请求
    /// </summary>
    public static void Host_HandleVoteRequest(string json)
    {
        var service = NetService.Instance;
        if (service == null || !service.IsServer)
            return;

        try
        {
            var data = JsonUtility.FromJson<VoteRequestData>(json);
            if (data == null || data.type != "sceneVoteRequest")
            {
                Debug.LogWarning("[SceneVote] 无效的投票请求数据");
                return;
            }

            Debug.Log($"[SceneVote] 收到客户端投票请求: {data.targetSceneId}");

            // 发起投票
            Host_StartVote(
                data.targetSceneId,
                data.curtainGuid,
                data.notifyEvac,
                data.saveToFile,
                data.useLocation,
                data.locationName
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SceneVote] 处理投票请求失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 主机：处理客户端的准备状态切换
    /// </summary>
    public static void Host_HandleReadyToggle(string json)
    {
        var service = NetService.Instance;
        if (service == null || !service.IsServer)
            return;

        try
        {
            Debug.Log($"[SceneVote] 主机收到准备状态切换消息: {json}");

            var data = JsonUtility.FromJson<ReadyToggleData>(json);
            if (data == null || data.type != "sceneVoteReady")
            {
                Debug.LogWarning("[SceneVote] 无效的准备状态数据");
                return;
            }

            Debug.Log($"[SceneVote] 解析成功: playerId={data.playerId}, ready={data.ready}");
            Host_HandleReadyToggle(data.playerId, data.ready);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SceneVote] 处理准备状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取玩家的 Steam ID
    /// </summary>
    private static string GetSteamId(NetPeer peer)
    {
        try
        {
            // 如果有 Steam 支持，尝试获取 SteamID
            if (SteamManager.Initialized && SteamEndPointMapper.Instance != null)
            {
                if (peer == null)
                {
                    // 主机自己的 SteamID
                    return Steamworks.SteamUser.GetSteamID().ToString();
                }
                else
                {
                    // 客户端的 SteamID
                    if (SteamEndPointMapper.Instance.TryGetSteamID(peer.EndPoint, out var steamId))
                    {
                        return steamId.ToString();
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SceneVote] 获取SteamID失败: {ex.Message}");
        }

        return ""; // 如果没有 Steam 或获取失败，返回空字符串
    }
}
