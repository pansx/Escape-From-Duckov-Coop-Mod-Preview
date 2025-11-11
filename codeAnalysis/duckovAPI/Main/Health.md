# Main/Health 模块 API 文档

## 模块概述

Health 模块负责生命值系统的网络同步，包括玩家和 AI 的血量管理、Buff 应用、伤害处理等。该模块实现了服务器权威的血量同步机制，确保所有客户端的血量状态一致。

**核心职责**：

- 玩家和 AI 生命值的网络同步

- Buff 状态的同步和应用

- 伤害事件的处理和转发

- 服务器权威的血量控制

## 文件列表

| 文件名 | 说明 |

|--------|------|
|---|---|

| `HealthM.cs` | 生命值管理器核心类 |
|---|---|

| `HurtM.cs` | 伤害管理 |

## 核心类说明

### Buff_

**功能**：处理 Buff 的网络同步

**主要方法**：

- `HandlePlayerBuffSelfApply` - 处理玩家Buff自我应用

- `HandleBuffProxyApply` - 处理Buff代理应用

### HealthM

**功能**：生命值管理器，核心血量同步逻辑

**主要方法**：

- `Client_SendSelfHealth` - 客户端发送自身血量

- `Server_ForceAuthSelf` - 服务器强制权威自身

- `Server_ForwardHurtToOwner` - 服务器转发伤害给拥有者

- `Client_ApplySelfHurtFromServer` - 客户端应用来自服务器的伤害

- `Server_OnHealthChanged` - 服务器血量变化处理

- `ForceSetHealth` - 强制设置生命值

**关键机制**：

- **20Hz 节流**：防止血量更新过于频繁

- **反射访问**：使用 FieldInfo 访问 Health 私有字段

- **事件监听**：使用 UnityAction 监听血量变化

- **权威同步**：服务器作为血量权威源

### HealthTool

**功能**：生命值工具类，提供辅助方法

**主要方法**：

- `Server_HookOneHealth` - 服务器钩子健康

- `Client_HookSelfHealth` - 客户端钩子自身健康

- `BindHealthToCharacter` - 绑定Health到Character

- `TryShowDamageBarUI` - 显示伤害条UI

### HurtM

**功能**：伤害管理

**主要方法**：

- `Server_HandleEnvHurtRequest` - 服务器处理环境伤害请求

- `Client_RequestDestructibleHurt` - 客户端请求可破坏物伤害

## 关键流程

### 血量同步流程

```

客户端 → 20Hz发送血量 → 服务器
服务器 → 验证并应用 → 广播给所有客户端
客户端 → 接收并应用血量

```

### 伤害处理流程

```

客户端 → 发送伤害请求 → 服务器
服务器 → 验证伤害 → 计算并应用
服务器 → 广播血量变化 → 所有客户端

```

## 依赖关系

**依赖模块**：

- NetService - 网络通信

- AIHealth - AI 血量管理

**被依赖模块**：

- Patch/Character/HealthPatch - 血量补丁

- Main/AI/AIHealth - AI 血量同步

## 性能优化

1. **20Hz 节流**：防止血量更新过于频繁
2. **反射缓存**：缓存 FieldInfo 避免重复查找
3. **事件驱动**：使用 UnityAction 监听变化
4. **权威服务器**：减少客户端验证开销
