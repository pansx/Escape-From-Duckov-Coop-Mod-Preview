# Patch/SteamP2P 模块 API 文档

## 模块概述

SteamP2P 补丁模块拦截 LiteNetLib 网络库的底层 Socket 调用，透明地替换为 Steam P2P 通信，实现 NAT 穿透和好友联机功能。

## 补丁目标

- `NetManager` - LiteNetLib网络管理器

- `Socket` - UDP Socket

## 文件列表

| 文件名 | 补丁目标 | 说明 |

|--------|---------|------|
|---|---|---|

| `Patch_LiteNetLib.cs` | NetManager | LiteNetLib补丁 |

| `Patch_Socket.cs` | Socket | Socket补丁 |

## 核心补丁说明

### PacketSignature

**功能**：识别 LiteNetLib 数据包

**用途**：

- 区分 Steam P2P 和 UDP 数据包

- 识别 LiteNetLib 协议头

- 验证数据包合法性

### Patch_LiteNetLib

**补丁目标**：NetManager 类

**可能拦截的方法**：

- `SendTo` - 发送数据

- `ReceiveFrom` - 接收数据

- `PollEvents` - 轮询事件

**用途**：

- 重定向发送/接收到 Steam P2P

- 保持 LiteNetLib API 兼容

- 透明替换底层传输

### Patch_Socket

**补丁目标**：Socket 类

**可能拦截的方法**：

- `SendTo` - UDP发送

- `ReceiveFrom` - UDP接收

- `Bind` - 绑定端口

**用途**：

- 拦截 UDP 调用

- 转发到 Steam P2P

- 虚拟 IP 映射

## 设计思路

### 透明替换

```

应用层（LiteNetLib）
  ↓
Patch_LiteNetLib（拦截）
  ↓
检测虚拟 IP（10.255.x.x）
  ↓
转换为 CSteamID
  ↓
SteamP2PManager.SendPacket
  ↓
Steam P2P API

```

### 虚拟IP映射

```

真实IP: 192.168.1.100
  ↓
虚拟 IP: 10.255.0.1
  ↓
SteamID: 76561198012345678

```

## 关键流程

### 发送数据流程

```

Patch_Socket.SendTo (Prefix)
  ↓
检测目标 IP 是否为虚拟 IP（10.255.x.x）
  ↓
如果是：
  - 转换虚拟 IP → SteamID

  - 调用 SteamP2PManager.SendPacket

  - return false（跳过原 UDP 发送）

如果不是：
  - return true（正常UDP发送）

```

### 接收数据流程

```

Patch_Socket.ReceiveFrom (Prefix)
  ↓
检查 Steam P2P 是否有数据
  ↓
如果有：
  - 从 SteamP2PManager 接收

  - 转换 SteamID → 虚拟 IP

  - 填充 buffer 和 endPoint

  - return false（跳过原 UDP 接收）

如果没有：
  - return true（正常UDP接收）

```

## 双模式支持

### Steam P2P 模式

- 使用虚拟 IP（10.255.x.x）

- 通过 Steam P2P 传输

- 支持 NAT 穿透

### UDP 回退模式

- 使用真实 IP

- 通过 UDP 传输

- 局域网直连

## 优势

1. **NAT 穿透**：
   - 无需端口转发

   - 无需公网 IP

   - Steam 自动处理

2. **好友联机**：
   - 通过 Steam 好友列表

   - 通过 Steam Lobby

   - 无需 IP 地址

3. **透明替换**：
   - 应用层无需修改

   - 保持 LiteNetLib API

   - 兼容现有代码

4. **安全性**：
   - Steam 加密传输

   - Steam 身份验证

   - 防止 IP 泄露

## 性能考虑

1. **延迟**：
   - 直连：与 UDP 相当

   - 中继：增加50-150ms

2. **带宽**：
   - 受 Steam P2P 限制

   - 通常足够游戏使用

3. **可靠性**：
   - Steam 自动重传

   - 支持可靠/不可靠传输

## 注意事项

1. **虚拟 IP 范围**：
   - 10.255.0.0/16保留

   - 不与真实 IP 冲突

2. **包大小限制**：
   - Steam P2P 通常限制1200字节

   - 需要分包处理

3. **初始化顺序**：
   - 必须先初始化 Steam

   - 再初始化网络

4. **错误处理**：
   - Steam P2P 失败时降级到 UDP

   - 记录错误日志

5. **调试**：
   - 使用 F10查看 P2P 统计

   - 检查虚拟 IP 映射
