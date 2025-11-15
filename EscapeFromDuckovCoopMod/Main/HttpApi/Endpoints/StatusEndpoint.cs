// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System;
using System.Net;

namespace EscapeFromDuckovCoopMod.HttpApi.Endpoints;

/// <summary>
/// 状态端点 "/api/status"
/// </summary>
public class StatusEndpoint : IHttpEndpoint
{
    public string Path => "/api/status";

    public void Handle(HttpListenerContext context)
    {
        var service = NetService.Instance;
        var status = new
        {
            server = new
            {
                running = service?.networkStarted ?? false,
                isHost = service?.IsServer ?? false,
                port = service?.port ?? 0,
                transportMode = service?.TransportMode.ToString() ?? "Unknown"
            },
            connection = new
            {
                connected = service?.connectedPeer != null,
                peerCount = service?.IsServer == true ? service.playerStatuses.Count : 0,
                clientCount = service?.IsServer == false ? service.clientPlayerStatuses.Count : 0
            },
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        HttpHelper.SendJson(context.Response, status);
    }
}
