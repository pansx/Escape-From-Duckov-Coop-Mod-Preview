// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System.Net;

namespace EscapeFromDuckovCoopMod.HttpApi.Endpoints;

/// <summary>
/// 根路径端点 "/"
/// </summary>
public class RootEndpoint : IHttpEndpoint
{
    public string Path => "/";

    public void Handle(HttpListenerContext context)
    {
        var service = NetService.Instance;
        var port = service?.port ?? 9050;

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Escape From Duckov - Coop Mod</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}
        .container {{ 
            background: white; 
            padding: 40px; 
            border-radius: 16px; 
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            max-width: 800px;
            width: 100%;
        }}
        h1 {{ 
            color: #333; 
            margin-bottom: 10px;
            font-size: 2.5em;
        }}
        .status {{ 
            color: #28a745; 
            font-weight: bold; 
            font-size: 1.2em;
            margin-bottom: 20px;
        }}
        .info {{ 
            background: #f8f9fa; 
            padding: 15px; 
            border-radius: 8px; 
            margin-bottom: 30px;
        }}
        .info-item {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #dee2e6;
        }}
        .info-item:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            font-weight: 600;
            color: #495057;
        }}
        .info-value {{
            color: #6c757d;
            font-family: 'Courier New', monospace;
        }}
        h2 {{ 
            color: #495057; 
            margin: 30px 0 20px 0;
            font-size: 1.5em;
            border-bottom: 2px solid #667eea;
            padding-bottom: 10px;
        }}
        .endpoint {{ 
            background: #f8f9fa; 
            padding: 20px; 
            margin: 15px 0; 
            border-left: 4px solid #667eea;
            border-radius: 8px;
            transition: all 0.3s ease;
            cursor: pointer;
        }}
        .endpoint:hover {{
            background: #e9ecef;
            transform: translateX(5px);
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.2);
        }}
        .endpoint strong {{ 
            color: #667eea; 
            font-size: 1.1em;
            display: block;
            margin-bottom: 8px;
        }}
        .endpoint-desc {{
            color: #6c757d;
            line-height: 1.6;
        }}
        .endpoint-url {{
            font-family: 'Courier New', monospace;
            color: #495057;
            background: white;
            padding: 8px 12px;
            border-radius: 4px;
            margin-top: 10px;
            display: inline-block;
            font-size: 0.9em;
        }}
        .footer {{
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #dee2e6;
            text-align: center;
            color: #6c757d;
            font-size: 0.9em;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🦆 Escape From Duckov</h1>
        <p class='status'>✓ HTTP 服务器运行中</p>
        
        <div class='info'>
            <div class='info-item'>
                <span class='info-label'>监听端口</span>
                <span class='info-value'>{port}</span>
            </div>
            <div class='info-item'>
                <span class='info-label'>服务状态</span>
                <span class='info-value'>{(service?.networkStarted == true ? "运行中" : "未启动")}</span>
            </div>
            <div class='info-item'>
                <span class='info-label'>服务器类型</span>
                <span class='info-value'>{(service?.IsServer == true ? "主机" : "客户端")}</span>
            </div>
        </div>
        
        <h2>📡 可用端点</h2>
        
        <div class='endpoint' onclick='window.location.href=""/''>
            <strong>GET /</strong>
            <div class='endpoint-desc'>显示此欢迎页面</div>
            <div class='endpoint-url'>http://localhost:{port}/</div>
        </div>
        
        <div class='endpoint' onclick='window.location.href=""/api/status""'>
            <strong>GET /api/status</strong>
            <div class='endpoint-desc'>获取服务器状态信息（JSON）</div>
            <div class='endpoint-url'>http://localhost:{port}/api/status</div>
        </div>
        
        <div class='endpoint' onclick='window.location.href=""/api/players""'>
            <strong>GET /api/players</strong>
            <div class='endpoint-desc'>获取当前在线玩家列表，包含 Steam 信息（JSON）</div>
            <div class='endpoint-url'>http://localhost:{port}/api/players</div>
        </div>
        
        <div class='endpoint' onclick='window.location.href=""/api/vote""'>
            <strong>GET /api/vote</strong>
            <div class='endpoint-desc'>获取当前场景投票状态（JSON）</div>
            <div class='endpoint-url'>http://localhost:{port}/api/vote</div>
        </div>
        
        <div class='endpoint' onclick='window.location.href=""/api/ais""'>
            <strong>GET /api/ais</strong>
            <div class='endpoint-desc'>获取所有 AI 角色列表，包含位置、血量、状态（JSON）</div>
            <div class='endpoint-url'>http://localhost:{port}/api/ais</div>
        </div>
        
        <div class='endpoint' onclick='window.location.href=""/api/lootboxes""'>
            <strong>GET /api/lootboxes</strong>
            <div class='endpoint-desc'>获取所有战利品箱列表，包含位置、物品清单（JSON）</div>
            <div class='endpoint-url'>http://localhost:{port}/api/lootboxes</div>
        </div>
        
        <div class='footer'>
            <p>Escape From Duckov - Coop Mod v1.0</p>
            <p>© 2025 Mr.sans and InitLoader's team</p>
        </div>
    </div>
</body>
</html>";

        HttpHelper.SendHtml(context.Response, html);
    }
}
