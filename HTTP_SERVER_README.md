# HTTP 服务器集成说明

## 概述

已成功集成简单的 HTTP 服务器到联机 Mod 中，监听与 UDP 相同的端口（默认 9050）。

## 功能特性

### 自动启动/停止
- HTTP 服务器会在启动网络服务时自动启动
- 在停止网络服务时自动停止
- 与 UDP 服务器共享相同的端口号

### 可用端点

#### 1. 根路径 `/`
**访问方式**: `http://localhost:9050/`

返回一个友好的 HTML 欢迎页面，显示：
- 服务器运行状态
- 当前监听端口
- 所有可用的 API 端点列表

**示例**:
```bash
# 在浏览器中打开
http://localhost:9050/
```

#### 2. 服务器状态 `/api/status`
**访问方式**: `http://localhost:9050/api/status`

返回 JSON 格式的服务器状态信息：
```json
{
  "server": {
    "running": true,
    "isHost": true,
    "port": 9050,
    "transportMode": "Direct"
  },
  "connection": {
    "connected": true,
    "peerCount": 2
  },
  "timestamp": "2025-11-15 12:34:56"
}
```

**示例**:
```bash
# 使用 curl
curl http://localhost:9050/api/status

# 使用 PowerShell
Invoke-RestMethod -Uri "http://localhost:9050/api/status"
```

#### 3. 玩家列表 `/api/players`
**访问方式**: `http://localhost:9050/api/players`

返回 JSON 格式的当前在线玩家列表（包含完整的 Steam 信息）：
```json
{
  "count": 3,
  "players": [
    {
      "type": "local",
      "steamId": "76561198012345678",
      "name": "MyPlayerName",
      "avatarUrl": "https://avatars.steamstatic.com/abc123_full.jpg",
      "endPoint": "Host:9050",
      "isInGame": true,
      "latency": 0,
      "lastUpdate": "2025-11-15 12:34:56.789"
    },
    {
      "type": "remote",
      "steamId": "76561198087654321",
      "name": "FriendName",
      "avatarUrl": "https://avatars.steamstatic.com/def456_full.jpg",
      "endPoint": "192.168.1.100:54321",
      "isInGame": true,
      "latency": 45,
      "lastUpdate": "2025-11-15 12:34:55.123"
    }
  ],
  "timestamp": "2025-11-15 12:34:56"
}
```

**字段说明**:
- `steamId`: Steam 64位 ID
- `name`: Steam 昵称
- `avatarUrl`: Steam 头像 URL（大图）
- `endPoint`: 网络端点地址
- `isInGame`: 是否在游戏中
- `latency`: 延迟（毫秒）
- `lastUpdate`: 最后更新时间戳

**示例**:
```bash
# 使用 curl
curl http://localhost:9050/api/players

# 使用 PowerShell
Invoke-RestMethod -Uri "http://localhost:9050/api/players"
```

## 使用场景

### 1. 监控服务器状态
可以通过 HTTP API 实时监控服务器运行状态，无需进入游戏。

### 2. 外部工具集成
可以开发外部工具（如 Discord Bot、Web 面板）来查询服务器信息。

### 3. 调试和测试
在开发过程中快速检查服务器状态和玩家连接情况。

## 技术细节

### 实现方式
- 使用 .NET 的 `HttpListener` 类实现
- 运行在独立的后台线程中，不阻塞游戏主线程
- 支持 CORS（跨域资源共享）

### 端口说明
- HTTP 服务器监听 `localhost` 和 `127.0.0.1`
- 端口号与 UDP 服务器相同（默认 9050）
- 注意：HTTP 和 UDP 可以共享同一端口，因为它们使用不同的协议

### 安全性
- 仅监听本地地址（localhost/127.0.0.1）
- 不对外网开放
- 适合本地调试和监控

## 测试步骤

### 1. 启动游戏和 Mod
1. 启动游戏
2. 按 `=` 键打开联机面板
3. 点击"启动主机"或"启动客户端"

### 2. 测试 HTTP 端点

#### 方法一：浏览器
直接在浏览器中访问：
```
http://localhost:9050/
http://localhost:9050/api/status
http://localhost:9050/api/players
```

#### 方法二：PowerShell
```powershell
# 测试根路径
Invoke-WebRequest -Uri "http://localhost:9050/"

# 测试状态端点
Invoke-RestMethod -Uri "http://localhost:9050/api/status" | ConvertTo-Json -Depth 10

# 测试玩家列表
Invoke-RestMethod -Uri "http://localhost:9050/api/players" | ConvertTo-Json -Depth 10
```

#### 方法三：curl
```bash
# 测试根路径
curl http://localhost:9050/

# 测试状态端点（格式化输出）
curl http://localhost:9050/api/status | jq

# 测试玩家列表（格式化输出）
curl http://localhost:9050/api/players | jq
```

## 日志输出

HTTP 服务器会在 Unity 日志中输出以下信息：

### 启动时
```
[SimpleHttpServer] ✓ HTTP 服务器已启动，监听端口: 9050
```

### 收到请求时
```
[SimpleHttpServer] 收到请求: GET /api/status
```

### 停止时
```
[SimpleHttpServer] ✓ HTTP 服务器已停止
```

## 故障排查

### 问题：无法访问 HTTP 端点

**可能原因**:
1. 网络服务未启动
2. 端口被占用
3. 防火墙阻止

**解决方法**:
1. 确认已启动主机或客户端
2. 检查 Unity 日志中是否有 HTTP 服务器启动的消息
3. 尝试更换端口号

### 问题：返回 404 错误

**可能原因**:
访问了不存在的端点

**解决方法**:
检查 URL 是否正确，参考上面的可用端点列表

## 未来扩展

可以轻松添加更多端点，例如：
- `/api/config` - 获取/修改服务器配置
- `/api/kick/{playerId}` - 踢出玩家
- `/api/broadcast` - 发送广播消息
- `/api/logs` - 获取服务器日志

## 注意事项

1. **仅本地访问**: HTTP 服务器仅监听本地地址，不对外网开放
2. **无认证**: 当前版本没有实现认证机制，仅用于本地调试
3. **性能影响**: HTTP 服务器运行在独立线程，对游戏性能影响极小
4. **端口冲突**: 如果端口被占用，HTTP 服务器会启动失败，但不影响 UDP 服务器

---

*创建时间: 2025-11-15*
*版本: 1.0.0*
