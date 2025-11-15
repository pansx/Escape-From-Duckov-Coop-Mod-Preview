// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

namespace EscapeFromDuckovCoopMod.HttpApi.Endpoints;

/// <summary>
/// 日志文件下载端点 "/api/log"
/// </summary>
public class LogEndpoint : IHttpEndpoint
{
    public string Path => "/api/log";

    public void Handle(HttpListenerContext context)
    {
        try
        {
            // 获取游戏根目录（Application.dataPath 指向 Duckov_Data 目录）
            var dataPath = Application.dataPath;
            var gameRootPath = Directory.GetParent(dataPath)?.FullName;

            if (string.IsNullOrEmpty(gameRootPath))
            {
                HttpHelper.SendError(context.Response, 500, "无法获取游戏根目录");
                return;
            }

            var logFilePath = System.IO.Path.Combine(gameRootPath, "latest.log");

            // 检查文件是否存在
            if (!File.Exists(logFilePath))
            {
                HttpHelper.SendError(context.Response, 404, $"日志文件不存在: {logFilePath}");
                return;
            }

            // 获取文件信息
            var fileInfo = new FileInfo(logFilePath);

            // 设置响应头
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.Headers.Add("Content-Disposition", $"attachment; filename=\"latest.log\"");
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.ContentLength64 = fileInfo.Length;

            // 读取并发送文件内容（允许共享读写，因为游戏可能正在写入日志）
            using (var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.CopyTo(context.Response.OutputStream);
            }

            context.Response.OutputStream.Close();

            Debug.Log($"[LogEndpoint] 日志文件已发送: {logFilePath} ({fileInfo.Length} 字节)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LogEndpoint] 发送日志文件失败: {ex.Message}\n{ex.StackTrace}");
            try
            {
                HttpHelper.SendError(context.Response, 500, $"读取日志文件失败: {ex.Message}");
            }
            catch { }
        }
    }
}
