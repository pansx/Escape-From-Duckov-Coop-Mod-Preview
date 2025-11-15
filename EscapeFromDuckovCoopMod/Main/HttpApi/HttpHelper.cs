// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System;
using System.Net;
using System.Text;
using UnityEngine;

namespace EscapeFromDuckovCoopMod.HttpApi;

/// <summary>
/// HTTP 辅助工具类
/// </summary>
public static class HttpHelper
{
    /// <summary>
    /// 发送 JSON 响应
    /// </summary>
    public static void SendJson(HttpListenerResponse response, object data, int statusCode = 200)
    {
        try
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            SendResponse(response, statusCode, json, "application/json");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HttpHelper] 发送 JSON 响应失败: {ex.Message}");
            SendError(response, 500, "Internal Server Error");
        }
    }

    /// <summary>
    /// 发送 HTML 响应
    /// </summary>
    public static void SendHtml(HttpListenerResponse response, string html, int statusCode = 200)
    {
        SendResponse(response, statusCode, html, "text/html");
    }

    /// <summary>
    /// 发送文本响应
    /// </summary>
    public static void SendText(HttpListenerResponse response, string text, int statusCode = 200)
    {
        SendResponse(response, statusCode, text, "text/plain");
    }

    /// <summary>
    /// 发送错误响应
    /// </summary>
    public static void SendError(HttpListenerResponse response, int statusCode, string message)
    {
        var error = new
        {
            error = GetStatusText(statusCode),
            message = message,
            statusCode = statusCode
        };
        SendJson(response, error, statusCode);
    }

    /// <summary>
    /// 发送 404 错误
    /// </summary>
    public static void Send404(HttpListenerResponse response, string path)
    {
        var error = new
        {
            error = "Not Found",
            message = $"端点 '{path}' 不存在",
            statusCode = 404
        };
        SendJson(response, error, 404);
    }

    /// <summary>
    /// 发送响应
    /// </summary>
    private static void SendResponse(HttpListenerResponse response, int statusCode, string content, string contentType)
    {
        try
        {
            response.StatusCode = statusCode;
            response.ContentType = contentType + "; charset=utf-8";
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Server", "EscapeFromDuckov-CoopMod/1.0");

            var buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HttpHelper] 发送响应失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取状态码文本
    /// </summary>
    private static string GetStatusText(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            400 => "Bad Request",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "Unknown"
        };
    }
}
