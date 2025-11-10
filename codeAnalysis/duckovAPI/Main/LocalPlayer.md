# Main/LocalPlayer 模块 API 文档

## 模块概述

LocalPlayer 模块负责本地玩家管理，包括玩家状态、输入处理、位置同步等。

## 文件列表

| 文件名 | 说明 |
|--------|------|
| `LocalPlayerManager.cs` | 本地玩家管理器 |
| `SendLocalPlayerStatus.cs` | 发送本地玩家状态 |
| `Spectator.cs` | 观察者模式 |

## 核心功能

- 本地玩家状态管理

- 玩家输入处理

- 位置和动画同步

- 装备和武器状态同步

## 依赖关系

**依赖模块**：

- NetService - 网络通信

- Main/Health - 生命值管理

**被依赖模块**：

- Main/ClientService - 客户端服务

- Main/HostService - 主机服务
