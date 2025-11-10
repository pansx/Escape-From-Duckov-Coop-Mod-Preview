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

using EscapeFromDuckovCoopMod.Utils.Logger.Tools;
using LiteNetLib;

namespace EscapeFromDuckovCoopMod.Net;

/// <summary>
/// 客户端状态上报消息系统
/// 用于客户端连接时上报 SteamID 和 EndPoint，建立正确的映射
/// </summary>
public static class ClientStatusMessage
{
    // 🆕 添加 SteamID -> SteamName 的映射缓存
    private static System.Collections.Generic.Dictionary<string, string> _steamIdToNameMap = 
        new System.Collections.Generic.Dictionary<string, string>();

    /// <summary>
    /// 客户端状态数据结构
    /// </summary>
    [System.Serializable]
    public class ClientStatusData
    {
        public string type = "updateClientStatus";
        public string steamId; // Steam ID
        public string steamName; // 🆕 Steam 用户名
        public string steamAvatarUrl; // 🆕 Steam 头像 URL
        public string endPoint; // 客户端的 EndPoint（虚拟 IP）
        public string playerName; // 玩家名称
        public string timestamp; // 时间戳
    }

    /// <summary>
    /// 客户端：发送状态更新到主机
    /// </summary>
    public static void Client_SendStatusUpdate()
    {
        var service = NetService.Instance;
        if (service == null || service.IsServer || service.connectedPeer == null)
        {
            return;
        }

        // 获取本地 Steam 信息
        string steamId = "";
        string steamName = "";
        string steamAvatarUrl = "";

        if (SteamManager.Initialized)
        {
            try
            {
                var mySteamId = Steamworks.SteamUser.GetSteamID();
                steamId = mySteamId.ToString();

                // 🆕 获取 Steam 用户名
                steamName = Steamworks.SteamFriends.GetPersonaName();

                // 🆕 获取 Steam 头像 URL
                // 获取大头像（184x184）
                int avatarHandle = Steamworks.SteamFriends.GetLargeFriendAvatar(mySteamId);
                if (avatarHandle > 0)
                {
                    // Steam 头像 URL 格式：https://avatars.steamstatic.com/{steamid3}/{hash}_full.jpg
                    // 但我们需要通过 API 获取，这里先记录 handle
                    // 实际上可以直接构造 URL
                    steamAvatarUrl = $"https://avatars.steamstatic.com/{GetSteamId3(mySteamId)}/{avatarHandle:x}_full.jpg";
                }

                LoggerHelper.Log(
                    $"[ClientStatus] Steam 信息: ID={steamId}, Name={steamName}, Avatar={steamAvatarUrl}"
                );
            }
            catch (System.Exception ex)
            {
                LoggerHelper.LogWarning($"[ClientStatus] 获取 Steam 信息失败: {ex.Message}");
            }
        }

        // 获取本地 EndPoint
        string endPoint = service.localPlayerStatus?.EndPoint ?? "";
        string playerName = service.localPlayerStatus?.PlayerName ?? steamName ?? "Client";

        if (string.IsNullOrEmpty(endPoint))
        {
            LoggerHelper.LogWarning("[ClientStatus] 无法获取本地 EndPoint，跳过状态上报");
            return;
        }

        var data = new ClientStatusData
        {
            steamId = steamId,
            steamName = steamName,
            steamAvatarUrl = steamAvatarUrl,
            endPoint = endPoint,
            playerName = playerName,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        };

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(
            data,
            Newtonsoft.Json.Formatting.None
        );
        LoggerHelper.Log($"[ClientStatus] 客户端发送状态更新: {json}");

        JsonMessage.SendToHost(data, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// 🆕 获取缓存的 Steam 名字（供 SceneVoteMessage 调用）
    /// </summary>
    public static string GetSteamNameFromSteamId(string steamId)
    {
        if (string.IsNullOrEmpty(steamId))
            return "";

        if (_steamIdToNameMap.TryGetValue(steamId, out var steamName))
        {
            return steamName;
        }
        return "";
    }

    /// <summary>
    /// 主机：处理客户端状态更新
    /// </summary>
    public static void Host_HandleClientStatus(NetPeer fromPeer, string json)
    {
        var service = NetService.Instance;
        if (service == null || !service.IsServer)
        {
            return;
        }

        try
        {
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<ClientStatusData>(json);
            if (data == null || data.type != "updateClientStatus")
            {
                LoggerHelper.LogWarning("[ClientStatus] 无效的客户端状态数据");
                return;
            }

            LoggerHelper.Log(
                $"[ClientStatus] 收到客户端状态: EndPoint={data.endPoint}, SteamID={data.steamId}, SteamName={data.steamName}, Name={data.playerName}"
            );

            // 🆕 缓存 SteamID -> SteamName 映射
            if (!string.IsNullOrEmpty(data.steamId) && !string.IsNullOrEmpty(data.steamName))
            {
                _steamIdToNameMap[data.steamId] = data.steamName;
                LoggerHelper.Log(
                    $"[ClientStatus] ✓ 已缓存 Steam 名字映射: {data.steamId} -> {data.steamName}"
                );
            }

            // 🆕 更新投票系统中的玩家信息（根据 Steam ID 匹配）
            UpdateVotePlayerInfo(data.endPoint, data.steamId, data.steamName);

            // 🔧 建立 SteamID 和 EndPoint 的映射
            if (
                !string.IsNullOrEmpty(data.steamId)
                && !string.IsNullOrEmpty(data.endPoint)
                && SteamEndPointMapper.Instance != null
            )
            {
                // 解析 EndPoint 为 IPEndPoint
                var parts = data.endPoint.Split(':');
                if (
                    parts.Length == 2
                    && System.Net.IPAddress.TryParse(parts[0], out var ipAddr)
                    && int.TryParse(parts[1], out var port)
                )
                {
                    var ipEndPoint = new System.Net.IPEndPoint(ipAddr, port);
                    var steamId = new Steamworks.CSteamID(ulong.Parse(data.steamId));

                    // 🔧 手动注册映射（直接访问内部字典）
                    // 注意：这需要 SteamEndPointMapper 提供公共方法或者我们使用反射
                    // 暂时使用现有的 RegisterSteamID 方法，它会生成虚拟IP但我们可以忽略返回值
                    // 更好的方案是添加一个新方法来直接注册已有的 EndPoint
                    
                    // 使用反射访问私有字典
                    var mapperType = typeof(SteamEndPointMapper);
                    var steamToEndPointField = mapperType.GetField(
                        "_steamToEndPoint",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                    );
                    var endPointToSteamField = mapperType.GetField(
                        "_endPointToSteam",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                    );

                    if (steamToEndPointField != null && endPointToSteamField != null)
                    {
                        var steamToEndPoint = steamToEndPointField.GetValue(SteamEndPointMapper.Instance) 
                            as System.Collections.Generic.Dictionary<Steamworks.CSteamID, System.Net.IPEndPoint>;
                        var endPointToSteam = endPointToSteamField.GetValue(SteamEndPointMapper.Instance) 
                            as System.Collections.Generic.Dictionary<System.Net.IPEndPoint, Steamworks.CSteamID>;

                        if (steamToEndPoint != null && endPointToSteam != null)
                        {
                            // 🔧 检查是否已存在相同 SteamID 但不同 EndPoint 的映射（端口变化）
                            if (steamToEndPoint.TryGetValue(steamId, out var oldEndPoint))
                            {
                                if (!oldEndPoint.Equals(ipEndPoint))
                                {
                                    // 🔧 移除旧的 EndPoint 映射
                                    endPointToSteam.Remove(oldEndPoint);
                                    LoggerHelper.Log(
                                        $"[ClientStatus] 🔄 检测到端口变化: {oldEndPoint} -> {ipEndPoint} (SteamID={data.steamId})"
                                    );
                                    
                                    // 🔧 同时更新 NetService 中的玩家记录
                                    UpdatePlayerStatusEndPoint(oldEndPoint.ToString(), data.endPoint, data.steamId, data.steamName);
                                }
                            }

                            // 🔧 注册新的映射（或更新现有映射）
                            steamToEndPoint[steamId] = ipEndPoint;
                            endPointToSteam[ipEndPoint] = steamId;
                            LoggerHelper.Log(
                                $"[ClientStatus] ✓ 已注册映射: {data.endPoint} <-> {data.steamId}"
                            );
                        }
                    }
                }
                else
                {
                    LoggerHelper.LogWarning(
                        $"[ClientStatus] 无法解析 EndPoint: {data.endPoint}"
                    );
                }
            }
            else
            {
                if (string.IsNullOrEmpty(data.steamId))
                {
                    LoggerHelper.LogWarning(
                        $"[ClientStatus] 客户端 {data.endPoint} 没有 SteamID"
                    );
                }
            }
        }
        catch (System.Exception ex)
        {
            LoggerHelper.LogError($"[ClientStatus] 处理客户端状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 将 SteamID 转换为 SteamID3 格式（用于构造头像 URL）
    /// </summary>
    private static string GetSteamId3(Steamworks.CSteamID steamId)
    {
        // SteamID3 格式：[U:1:XXXXXXXX]
        // 从 64 位 SteamID 提取账户 ID
        ulong accountId = steamId.m_SteamID & 0xFFFFFFFF;
        return accountId.ToString();
    }

    /// <summary>
    /// 🆕 更新投票系统中的玩家信息（根据 Steam ID 匹配）
    /// </summary>
    private static void UpdateVotePlayerInfo(string endPoint, string steamId, string steamName)
    {
        var service = NetService.Instance;
        if (service == null || !service.IsServer)
            return;

        // 检查是否有活跃的投票
        if (!SceneVoteMessage.HasActiveVote())
            return;

        try
        {
            // 🔧 通过反射访问 _hostVoteState（因为它是私有的）
            var sceneVoteType = typeof(SceneVoteMessage);
            var hostVoteStateField = sceneVoteType.GetField(
                "_hostVoteState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );

            if (hostVoteStateField == null)
            {
                LoggerHelper.LogWarning("[ClientStatus] 无法访问 _hostVoteState 字段");
                return;
            }

            var hostVoteState = hostVoteStateField.GetValue(null) as SceneVoteMessage.VoteStateData;
            if (hostVoteState == null || hostVoteState.playerList == null || hostVoteState.playerList.items == null)
                return;

            // 🔧 根据 Steam ID 或 EndPoint 查找并更新玩家信息
            bool updated = false;
            foreach (var player in hostVoteState.playerList.items)
            {
                // 优先匹配 Steam ID（更可靠）
                if (!string.IsNullOrEmpty(steamId) && player.steamId == steamId)
                {
                    // 更新 Steam 名字
                    if (!string.IsNullOrEmpty(steamName) && player.steamName != steamName)
                    {
                        LoggerHelper.Log(
                            $"[ClientStatus] 🔄 更新投票玩家 Steam 名字: {player.playerName} -> {steamName} (SteamID={steamId})"
                        );
                        player.steamName = steamName;
                        updated = true;
                    }

                    // 更新 EndPoint（如果变化）
                    if (player.playerId != endPoint)
                    {
                        LoggerHelper.Log(
                            $"[ClientStatus] 🔄 更新投票玩家 EndPoint: {player.playerId} -> {endPoint} (SteamID={steamId})"
                        );
                        player.playerId = endPoint;
                        updated = true;
                    }
                    break;
                }
                // 备用：匹配 EndPoint
                else if (player.playerId == endPoint)
                {
                    // 更新 Steam ID 和名字
                    if (!string.IsNullOrEmpty(steamId) && player.steamId != steamId)
                    {
                        LoggerHelper.Log(
                            $"[ClientStatus] 🔄 更新投票玩家 SteamID: {player.playerName} -> {steamId}"
                        );
                        player.steamId = steamId;
                        updated = true;
                    }

                    if (!string.IsNullOrEmpty(steamName) && player.steamName != steamName)
                    {
                        LoggerHelper.Log(
                            $"[ClientStatus] 🔄 更新投票玩家 Steam 名字: {player.playerName} -> {steamName}"
                        );
                        player.steamName = steamName;
                        updated = true;
                    }
                    break;
                }
            }

            // 如果有更新，立即广播新的投票状态
            if (updated)
            {
                LoggerHelper.Log("[ClientStatus] ✓ 投票玩家信息已更新，广播新状态");
                SceneVoteMessage.Host_BroadcastVoteState();
            }
        }
        catch (System.Exception ex)
        {
            LoggerHelper.LogError(
                $"[ClientStatus] 更新投票玩家信息失败: {ex.Message}\n{ex.StackTrace}"
            );
        }
    }

    /// <summary>
    /// 更新 NetService 中的玩家记录（端口变化时）
    /// </summary>
    private static void UpdatePlayerStatusEndPoint(
        string oldEndPoint,
        string newEndPoint,
        string steamId,
        string steamName
    )
    {
        var service = NetService.Instance;
        if (service == null || !service.IsServer)
            return;

        try
        {
            // 🔧 在 clientPlayerStatuses 中查找并更新
            if (service.clientPlayerStatuses.TryGetValue(oldEndPoint, out var oldStatus))
            {
                // 移除旧的记录
                service.clientPlayerStatuses.Remove(oldEndPoint);

                // 更新 EndPoint
                oldStatus.EndPoint = newEndPoint;

                // 添加到新的 EndPoint
                service.clientPlayerStatuses[newEndPoint] = oldStatus;

                LoggerHelper.Log(
                    $"[ClientStatus] ✓ 已更新 clientPlayerStatuses: {oldEndPoint} -> {newEndPoint}"
                );
            }

            // 🔧 在 clientRemoteCharacters 中查找并更新
            if (service.clientRemoteCharacters.TryGetValue(oldEndPoint, out var character))
            {
                // 移除旧的记录
                service.clientRemoteCharacters.Remove(oldEndPoint);

                // 添加到新的 EndPoint
                service.clientRemoteCharacters[newEndPoint] = character;

                LoggerHelper.Log(
                    $"[ClientStatus] ✓ 已更新 clientRemoteCharacters: {oldEndPoint} -> {newEndPoint}"
                );
            }

            // 🔧 更新投票系统中的玩家列表
            var sceneNet = SceneNet.Instance;
            if (sceneNet != null && sceneNet.sceneVoteActive)
            {
                // 更新参与者列表
                if (sceneNet.sceneParticipantIds.Contains(oldEndPoint))
                {
                    sceneNet.sceneParticipantIds.Remove(oldEndPoint);
                    sceneNet.sceneParticipantIds.Add(newEndPoint);
                    LoggerHelper.Log(
                        $"[ClientStatus] ✓ 已更新投票参与者: {oldEndPoint} -> {newEndPoint}"
                    );
                }

                // 更新准备状态
                if (sceneNet.sceneReady.TryGetValue(oldEndPoint, out var readyState))
                {
                    sceneNet.sceneReady.Remove(oldEndPoint);
                    sceneNet.sceneReady[newEndPoint] = readyState;
                    LoggerHelper.Log(
                        $"[ClientStatus] ✓ 已更新投票准备状态: {oldEndPoint} -> {newEndPoint}, ready={readyState}"
                    );
                }

                // 🔧 更新主机缓存的投票状态（如果存在）
                if (SceneVoteMessage.HasActiveVote())
                {
                    SceneVoteMessage.UpdatePlayerEndPoint(oldEndPoint, newEndPoint, steamName);
                }
            }
        }
        catch (System.Exception ex)
        {
            LoggerHelper.LogError(
                $"[ClientStatus] 更新玩家 EndPoint 失败: {ex.Message}\n{ex.StackTrace}"
            );
        }
    }
}
