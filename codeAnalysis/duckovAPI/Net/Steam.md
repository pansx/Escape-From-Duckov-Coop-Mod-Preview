# Net/Steam 模块 API 文档

## 模块概述

Steam 模块实现基于 Steam P2P 的网络通信，提供 NAT 穿透、Lobby 管理、虚拟 IP 映射等功能。该模块是整个联机系统的网络传输层，支持 Steam 好友联机和公开 Lobby。

**核心职责**：

- Steam P2P 网络通信

- Lobby 创建和管理

- 虚拟 IP 与 SteamID 映射

- NAT 穿透和中继

- 连接质量诊断

## 文件列表

| 文件名 | 说明 |

|--------|------|
|---|---|

| `SteamLobbyHelper.cs` | Lobby连接辅助 |
|---|---|

| `SteamLobbyOptions.cs` | Lobby选项配置 |
|---|---|

| `SteamP2PManager.cs` | Steam P2P管理器（核心） |

## 核心类说明

### SteamEndPointMapper

**功能**：Steam ID 与虚拟 IP 的双向映射

**关键特性**：

- **虚拟 IP 格式**：10.255.x.x

- **双向映射**：CSteamID ↔ IPEndPoint

- **会话管理**：注册、注销、查询

- **P2P 握手**：发送 HANDSHAKE 包建立连接

**主要方法**：

#### RegisterSteamID

```csharp
public IPEndPoint RegisterSteamID(CSteamID steamID)

```

- **功能**：注册Steam ID 并生成虚拟端点

- **返回**：虚拟 IPEndPoint（10.255.x.x）

#### WaitForP2PSessionEstablished

```csharp
public async UniTask<bool> WaitForP2PSessionEstablished(CSteamID steamID, float timeout = 10f)

```

- **功能**：等待P2P会话建立

- **超时**：默认10秒

- **返回**：是否成功建立

#### TryGetSteamID / TryGetEndPoint

```csharp
public bool TryGetSteamID(IPEndPoint endPoint, out CSteamID steamID)
public bool TryGetEndPoint(CSteamID steamID, out IPEndPoint endPoint)

```

- **功能**：双向查询映射

#### GenerateVirtualEndPoint

```csharp
private IPEndPoint GenerateVirtualEndPoint()

```

- **功能**：生成虚拟IP（10.255.x.x）

- **范围**：10.255.0.1 - 10.255.255.254

### SteamLobbyHelper

**功能**：Lobby 连接辅助工具

**主要方法**：

#### TriggerMultiplayerConnect

```csharp
public async UniTask TriggerMultiplayerConnect(CSteamID hostSteamID)

```

- **功能**：触发多人连接

- **流程**：

  1. 注册虚拟端点
  2. 等待 P2P 会话
  3. 调用 NetService 连接

#### TriggerMultiplayerHost

```csharp
public void TriggerMultiplayerHost()

```

- **功能**：触发多人主机

- **流程**：

  1. 启动网络服务器
  2. 保持 Steam Lobby

### SteamLobbyManager

**功能**：Steam Lobby 管理器（单例）

**Lobby元数据**：

- `mod_id`：模组标识符

- `name`：Lobby名称

- `host`：主机名称

- `version`：版本号

- `password`：密码哈希（SHA256）

- `password_protected`：是否有密码

**主要方法**：

#### CreateLobby

```csharp
public async UniTask<CSteamID> CreateLobby(SteamLobbyOptions options)

```

- **功能**：创建Lobby

- **参数**：

  - LobbyName：Lobby 名称

  - Password：密码（可选）

  - MaxPlayers：最大玩家数（2-16）

  - Visibility：可见性（公开/仅好友）

- **返回**：Lobby 的 SteamID

#### RequestLobbyList

```csharp
public async UniTask<List<LobbyInfo>> RequestLobbyList()

```

- **功能**：请求Lobby列表

- **范围**：全球

- **限制**：最多50个结果

- **过滤**：按模组 ID 过滤

#### JoinLobby

```csharp
public async UniTask<bool> JoinLobby(CSteamID lobbyID)

```

- **功能**：加入Lobby

- **流程**：

  1. 发送加入请求
  2. 等待回调
  3. 自动连接主机

#### TryJoinLobbyWithPassword

```csharp
public async UniTask<bool> TryJoinLobbyWithPassword(CSteamID lobbyID, string password)

```

- **功能**：使用密码加入Lobby

- **验证**：SHA256哈希比对

**事件回调**：

- `LobbyListUpdated` - Lobby列表更新

- `LobbyJoined` - 加入Lobby成功

- 成员进入/离开/断开/踢出/封禁事件

### SteamLobbyOptions

**功能**：Lobby选项配置

**字段**：

```csharp
public string LobbyName;
public string Password;
public int MaxPlayers;  // 2-16
public ELobbyType Visibility;  // 公开/仅好友

```

**静态方法**：

```csharp
public static SteamLobbyOptions CreateDefault()

```

- **功能**：创建默认配置（使用Steam 用户名）

### SteamP2PLoader

**功能**：Steam P2P 加载器和初始化管理

**初始化检查**：

- 检查 Steam 是否初始化

- 添加 P2P 组件

- UDP 回退支持

**快捷键**：

- **F9**：打开 Steam 邀请界面

- **F10**：显示 P2P 连接统计

**统计信息**：

- 发送/接收包数和字节数

- 队列大小

- 连接数

### SteamP2PManager

**功能**：Steam P2P 管理器（核心类，单例）

**数据包接收**：

- **多通道支持**：0-3通道

- **批处理限制**：512包/帧

- **队列管理**：最大512包

- **过大包处理**：动态扩展缓冲区（最大8KB）

- **HANDSHAKE 包过滤**：自动过滤握手包

**主要方法**：

#### SendPacket

```csharp
public bool SendPacket(CSteamID target, byte[] data, int length, EP2PSend sendType, int channel = 0)

```

- **功能**：发送数据包

- **支持类型**：

  - Unreliable：不可靠

  - UnreliableNoDelay：不可靠无延迟

  - Reliable：可靠

  - ReliableWithBuffering：可靠带缓冲

- **支持通道**：0-3

- **自动会话接受**：首次发送自动接受会话

#### TryReceiveDirectFromSteam

```csharp
public bool TryReceiveDirectFromSteam(out IPEndPoint endPoint, byte[] buffer, out int length, int channel = 0)

```

- **功能**：直接从Steam接收数据

- **特性**：

  - 绕过队列直接读取

  - 自动端点映射

  - 大包支持（8KB 缓冲区）

#### DiagnoseP2PQuality

```csharp
public string DiagnoseP2PQuality(CSteamID target)

```

- **功能**：诊断P2P连接质量

- **检查项**：

  - 连接状态

  - 中继/直连判断

  - 发送队列监控

  - 质量评估（优秀/良好/一般/差）

  - 优化建议

**统计信息**：

```csharp
public long PacketsSent { get; }
public long PacketsReceived { get; }
public long BytesSent { get; }
public long BytesReceived { get; }
public long PacketsDropped { get; }
public long SendFailures { get; }
public int MaxQueueDepth { get; }
public float PacketLossRate { get; }
public float SendFailureRate { get; }

```

**会话管理**：

- 接受P2P会话

- 关闭 P2P 会话

- 获取会话状态

- 会话请求回调

- 会话失败回调

## 关键流程

### Steam P2P连接流程

```

1. 客户端点击加入Lobby
2. SteamLobbyManager.JoinLobby
3. Steam 回调：OnLobbyEnter
4. 获取主机 SteamID
5. SteamEndPointMapper.RegisterSteamID（生成虚拟 IP）
6. WaitForP2PSessionEstablished（等待 P2P 握手）
7. NetService.ConnectToHost（使用虚拟 IP 连接）
8. SteamP2PManager 拦截发送/接收
9. 转换虚拟 IP ↔ SteamID
10. 使用 Steam P2P API发送/接收

```

### 数据发送流程

```

1. 应用层调用NetDataWriter.Put
2. NetService.Send
3. 判断是否使用 Steam P2P
4. 如果是：SteamP2PManager.SendPacket
5. 转换虚拟 IP → SteamID
6. 选择通道和发送类型
7. SteamNetworking.SendP2PPacket

```

### 数据接收流程

```

1. SteamP2PManager.Update（每帧）
2. 遍历所有通道（0-3）
3. SteamNetworking.IsP2PPacketAvailable
4. SteamNetworking.ReadP2PPacket
5. 过滤 HANDSHAKE 包
6. 入队到_receivedPackets
7. NetService.PollEvents
8. TryGetReceivedPacket
9. 转换 SteamID → 虚拟IP
10. 应用层处理数据

```

## 性能指标

- **延迟**：

  - 直连：<50ms

  - 中继：50-200ms

- **吞吐量**：

  - 理论：1200字节/包 × 512包/帧 = 600KB/帧

  - 实际：受 Steam P2P 限制

- **丢包率**：<1%（正常情况）

- **队列深度**：最大512包

## 设计特点

1. **虚拟 IP 映射**：
   - 透明的 P2P 通信

   - 无需修改应用层代码

   - 10.255.x.x 格式易于识别

2. **多通道支持**：
   - 通道0：普通数据

   - 通道1：高优先级数据

   - 通道2-3：保留

3. **队列管理**：
   - 最大512包防止内存溢出

   - 队列满时丢弃旧包

   - 批处理限制512包/帧

4. **零 GC**：
   - 使用缓冲区池

   - 避免 GC 分配

   - 直接读取模式

5. **自动 NAT 穿透**：
   - Steam 自动处理 NAT

   - 支持中继服务器

   - 无需手动配置

6. **详细诊断**：
   - 实时统计

   - 质量评估

   - 优化建议

## 依赖关系

**依赖库**：

- Steamworks.NET - Steam API 封装

- LiteNetLib - 底层网络库

**被依赖模块**：

- Main 模块 - 提供网络通信基础

- Patch/SteamP2P - 网络数据拦截

## 错误处理

1. **会话建立失败**：
   - 超时重试

   - 降级到 UDP

2. **发送失败**：
   - 统计失败次数

   - 诊断连接质量

3. **队列溢出**：
   - 丢弃旧包

   - 记录丢包统计

4. **密码错误**：
   - 提示用户

   - 拒绝加入

## 注意事项

1. **Steam 初始化**：
   - 必须先初始化 Steam

   - 检查 SteamManager.Initialized

2. **虚拟 IP 范围**：
   - 10.255.0.0/16保留

   - 不与真实 IP 冲突

3. **密码安全**：
   - 使用 SHA256哈希

   - 不传输明文密码

4. **版本兼容**：
   - 检查 mod_id 和 version

   - 拒绝不兼容版本

5. **Lobby 限制**：
   - 最多16人

   - 最多50个搜索结果

6. **P2P 限制**：
   - 包大小限制（通常1200字节）

   - 带宽限制（Steam 控制）
