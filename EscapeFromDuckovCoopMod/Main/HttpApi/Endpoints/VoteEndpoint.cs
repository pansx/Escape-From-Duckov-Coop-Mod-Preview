// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System;
using System.Net;

namespace EscapeFromDuckovCoopMod.HttpApi.Endpoints;

/// <summary>
/// 投票状态端点 "/api/vote"
/// </summary>
public class VoteEndpoint : IHttpEndpoint
{
    public string Path => "/api/vote";

    public void Handle(HttpListenerContext context)
    {
        var service = NetService.Instance;

        // 检查是否是主机
        if (service == null || !service.IsServer)
        {
            var error = new
            {
                active = false,
                message = "只有主机才有投票状态",
                isHost = service?.IsServer ?? false
            };
            HttpHelper.SendJson(context.Response, error);
            return;
        }

        // 检查是否有活跃的投票
        if (!Net.SceneVoteMessage.HasActiveVote())
        {
            var noVote = new
            {
                active = false,
                message = "当前没有活跃的投票",
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            HttpHelper.SendJson(context.Response, noVote);
            return;
        }

        // 获取投票状态（通过反射访问私有字段）
        var voteStateField = typeof(Net.SceneVoteMessage).GetField(
            "_hostVoteState",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );

        if (voteStateField == null)
        {
            HttpHelper.SendError(context.Response, 500, "无法获取投票状态");
            return;
        }

        var voteState = voteStateField.GetValue(null) as Net.SceneVoteMessage.VoteStateData;

        if (voteState == null)
        {
            HttpHelper.SendError(context.Response, 500, "投票状态为空");
            return;
        }

        // 构建响应数据
        var result = new
        {
            active = voteState.active,
            voteId = voteState.voteId,
            targetScene = new
            {
                id = voteState.targetSceneId,
                displayName = voteState.targetSceneDisplayName,
                curtainGuid = voteState.curtainGuid,
                locationName = voteState.locationName
            },
            options = new
            {
                notifyEvac = voteState.notifyEvac,
                saveToFile = voteState.saveToFile,
                useLocation = voteState.useLocation
            },
            hostSceneId = voteState.hostSceneId,
            players = voteState.playerList?.items ?? new Net.SceneVoteMessage.PlayerInfo[0],
            statistics = new
            {
                totalPlayers = voteState.totalPlayers,
                readyPlayers = voteState.readyPlayers,
                readyPercentage = voteState.totalPlayers > 0
                    ? (int)((float)voteState.readyPlayers / voteState.totalPlayers * 100)
                    : 0
            },
            timestamp = voteState.timestamp
        };

        HttpHelper.SendJson(context.Response, result);
    }
}
