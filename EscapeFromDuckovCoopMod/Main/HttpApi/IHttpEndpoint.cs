// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System.Net;

namespace EscapeFromDuckovCoopMod.HttpApi;

/// <summary>
/// HTTP 端点接口
/// </summary>
public interface IHttpEndpoint
{
    /// <summary>
    /// 端点路径（例如："/api/status"）
    /// </summary>
    string Path { get; }

    /// <summary>
    /// 处理 HTTP 请求
    /// </summary>
    void Handle(HttpListenerContext context);
}
