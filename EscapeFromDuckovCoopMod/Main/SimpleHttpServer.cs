// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;
using EscapeFromDuckovCoopMod.HttpApi;
using EscapeFromDuckovCoopMod.HttpApi.Endpoints;

namespace EscapeFromDuckovCoopMod;

/// <summary>
/// 简单的 HTTP 服务器，监听与 UDP 相同的端口
/// 提供基础的 REST API 端点
/// </summary>
public class SimpleHttpServer : MonoBehaviour
{
    private HttpListener _listener;
    private Thread _listenerThread;
    private bool _isRunning;
    private int _port;
    private readonly Dictionary<string, IHttpEndpoint> _endpoints = new();

    public static SimpleHttpServer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 注册所有端点
        RegisterEndpoints();
    }

    /// <summary>
    /// 注册所有 HTTP 端点
    /// </summary>
    private void RegisterEndpoints()
    {
        RegisterEndpoint(new RootEndpoint());
        RegisterEndpoint(new StatusEndpoint());
        RegisterEndpoint(new PlayersEndpoint());
        RegisterEndpoint(new VoteEndpoint());
        RegisterEndpoint(new AIsEndpoint());
        RegisterEndpoint(new LootboxesEndpoint());
        RegisterEndpoint(new DebugEndpoint());
        RegisterEndpoint(new LogEndpoint());

        Debug.Log($"[SimpleHttpServer] 已注册 {_endpoints.Count} 个端点");
    }

    /// <summary>
    /// 注册单个端点
    /// </summary>
    private void RegisterEndpoint(IHttpEndpoint endpoint)
    {
        _endpoints[endpoint.Path] = endpoint;
        Debug.Log($"[SimpleHttpServer] 注册端点: {endpoint.Path}");
    }

    /// <summary>
    /// 启动 HTTP 服务器
    /// </summary>
    public void StartServer(int port)
    {
        if (_isRunning)
        {
            Debug.LogWarning($"[SimpleHttpServer] HTTP 服务器已在运行，端口: {_port}");
            return;
        }

        _port = port;

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            _isRunning = true;

            _listenerThread = new Thread(ListenForRequests) { IsBackground = true };
            _listenerThread.Start();

            Debug.Log($"[SimpleHttpServer] ✓ HTTP 服务器已启动，监听端口: {port}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SimpleHttpServer] 启动 HTTP 服务器失败: {ex.Message}");
            _isRunning = false;
        }
    }

    /// <summary>
    /// 停止 HTTP 服务器
    /// </summary>
    public void StopServer()
    {
        if (!_isRunning)
            return;

        try
        {
            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
            _listenerThread?.Join(1000);

            Debug.Log($"[SimpleHttpServer] ✓ HTTP 服务器已停止");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SimpleHttpServer] 停止 HTTP 服务器失败: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        StopServer();
    }

    /// <summary>
    /// 监听 HTTP 请求的主循环
    /// </summary>
    private void ListenForRequests()
    {
        while (_isRunning)
        {
            try
            {
                var context = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
            }
            catch (HttpListenerException)
            {
                // 服务器停止时会抛出异常，忽略
                if (!_isRunning)
                    break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SimpleHttpServer] 监听请求时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 处理 HTTP 请求
    /// </summary>
    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var path = request.Url.AbsolutePath;

            Debug.Log($"[SimpleHttpServer] 收到请求: {request.HttpMethod} {path}");

            // 查找对应的端点
            if (_endpoints.TryGetValue(path, out var endpoint))
            {
                endpoint.Handle(context);
            }
            else
            {
                HttpHelper.Send404(context.Response, path);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SimpleHttpServer] 处理请求时出错: {ex.Message}\n{ex.StackTrace}");
            try
            {
                HttpHelper.SendError(context.Response, 500, $"Internal Server Error: {ex.Message}");
            }
            catch { }
        }
    }
}
