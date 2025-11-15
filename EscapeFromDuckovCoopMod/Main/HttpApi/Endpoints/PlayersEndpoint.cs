// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System;
using System.Collections.Generic;
using System.Net;

namespace EscapeFromDuckovCoopMod.HttpApi.Endpoints;

/// <summary>
/// 玩家列表端点 "/api/players"
/// </summary>
public class PlayersEndpoint : IHttpEndpoint
{
    public string Path => "/api/players";

    public void Handle(HttpListenerContext context)
    {
        var service = NetService.Instance;
        var players = new List<object>();
        var playerDb = Utils.Database.PlayerInfoDatabase.Instance;

        if (service != null && playerDb != null)
        {
            // 添加本地玩家
            if (service.localPlayerStatus != null)
            {
                var localEndPoint = service.localPlayerStatus.EndPoint;
                var localPlayer = playerDb.GetPlayerByEndPoint(localEndPoint);

                players.Add(new
                {
                    type = "local",
                    steamId = localPlayer?.SteamId ?? "",
                    name = localPlayer?.PlayerName ?? service.localPlayerStatus.PlayerName,
                    avatarUrl = localPlayer?.SteamAvatarUrl ?? "",
                    endPoint = localEndPoint,
                    isInGame = service.localPlayerStatus.IsInGame,
                    latency = service.localPlayerStatus.Latency,
                    lastUpdate = localPlayer?.LastUpdate ?? ""
                });
            }

            // 添加远程玩家
            if (service.IsServer)
            {
                foreach (var kvp in service.playerStatuses)
                {
                    var status = kvp.Value;
                    var player = playerDb.GetPlayerByEndPoint(status.EndPoint);

                    players.Add(new
                    {
                        type = "remote",
                        steamId = player?.SteamId ?? "",
                        name = player?.PlayerName ?? status.PlayerName,
                        avatarUrl = player?.SteamAvatarUrl ?? "",
                        endPoint = status.EndPoint,
                        isInGame = status.IsInGame,
                        latency = status.Latency,
                        lastUpdate = player?.LastUpdate ?? ""
                    });
                }
            }
            else
            {
                foreach (var kvp in service.clientPlayerStatuses)
                {
                    var status = kvp.Value;
                    var player = playerDb.GetPlayerByEndPoint(status.EndPoint);

                    players.Add(new
                    {
                        type = "remote",
                        steamId = player?.SteamId ?? "",
                        name = player?.PlayerName ?? status.PlayerName,
                        avatarUrl = player?.SteamAvatarUrl ?? "",
                        endPoint = status.EndPoint,
                        isInGame = status.IsInGame,
                        latency = status.Latency,
                        lastUpdate = player?.LastUpdate ?? ""
                    });
                }
            }
        }

        var result = new
        {
            count = players.Count,
            players = players,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        HttpHelper.SendJson(context.Response, result);
    }
}
