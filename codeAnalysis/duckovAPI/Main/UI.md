# Main/UI 模块 API 文档

## 模块概述

UI 模块负责用户界面，包括联机菜单、玩家列表、聊天系统等。

## 文件列表

| 文件名 | 说明 |
|--------|------|
| `ModUI.cs` | 模组 UI 主类 |
| `MModUI.cs` | 模组 UI 管理器 |
| `MModUIComponents.cs` | UI 组件 |
| `MModUILayoutBuilder.cs` | UI 布局构建器 |

## 核心功能

- 联机菜单界面

- 玩家列表显示

- 聊天系统

- Steam Lobby 界面

- 连接状态显示

## 依赖关系

**依赖模块**：

- NetService - 网络状态

- Net/Steam/SteamLobbyManager - Lobby 管理

**Unity 依赖**：

- UGUI 系统

- TextMeshPro
