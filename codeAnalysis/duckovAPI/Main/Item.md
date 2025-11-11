# Main/Item 模块 API 文档

## 模块概述

Item 模块负责物品掉落和拾取的网络同步，确保所有客户端的物品状态一致。使用 Token 机制和 ID 分配系统防止重复操作。

## 文件列表

| 文件名 | 说明 |

|--------|------|
|---|---|

| `ItemRequest.cs` | 物品请求 |
|---|---|

## 核心类说明

### ItemHandle

**主要方法**：

- `HandleItemDropRequest` - 处理物品掉落请求

- `HandleItemSpawn` - 处理物品生成

- `HandleItemPickupRequest` - 处理物品拾取请求

- `HandleItemDespawn` - 处理物品消失

**关键机制**：

- **Token 机制**：使用 uint token 标识本地掉落

- **ID 分配**：服务器分配唯一掉落 ID

- **标记集合**：防止重复广播和请求

- **快照系统**：使用 ItemSnapshot 序列化物品状态

### ItemRequest

**主要方法**：

- `SendItemDropRequest` - 发送物品掉落请求

- `SendItemPickupRequest` - 发送物品拾取请求

## 关键流程

### 物品掉落流程

```

客户端 → 生成本地物品（token） → 发送掉落请求
服务器 → 验证并分配 ID → 广播物品生成
客户端 → 接收并创建物品

```

### 物品拾取流程

```

客户端 → 发送拾取请求 → 服务器
服务器 → 验证并处理 → 广播物品消失
客户端 → 接收并移除物品

```

## 依赖关系

**依赖模块**：

- NetService - 网络通信

- ItemSnapshot - 物品序列化

**被依赖模块**：

- Patch/Item/ItemPatch - 物品补丁
