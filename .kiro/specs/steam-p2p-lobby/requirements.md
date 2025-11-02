# 需求文档

## 介绍

本文档规定了实现Steam P2P联机系统的需求，该系统使玩家能够通过多种连接方式创建和加入游戏房间，包括Steam大厅、直接P2P连接和好友邀请。系统将提供一个综合的大厅界面用于房间管理和玩家协调。

## 术语表

- **Steam大厅系统**: Steam内置的大厅和匹配服务，用于多人游戏
- **P2P服务器**: 运行在9050端口的点对点服务器，用于直接连接
- **房主**: 创建和管理游戏房间的玩家
- **房间客户端**: 加入现有游戏房间的玩家
- **Rich_Presence**: Steam的系统，用于向好友显示玩家状态和游戏信息
- **大厅界面**: 用于显示房间信息和玩家管理的主要UI组件
- **连接管理器**: 负责建立和维护网络连接的系统组件
- **房间状态**: 游戏房间的当前状态和配置，包括玩家、设置和连接信息

## Requirements

### 需求 1

**用户故事:** 作为玩家，我想要创建一个可自定义设置的游戏房间，以便为我的好友和其他玩家主持多人游戏会话。

#### 验收标准

1. WHEN 玩家点击"创建房间"按钮时, THE 大厅界面 SHALL 显示带有可配置选项的房间创建对话框
2. THE 房主 SHALL 能够设置房间名称、最大玩家数(1-4)、地图选择、游戏模式和可选密码
3. WHEN 确认创建房间时, THE P2P服务器 SHALL 在9050端口启动 AND THE Steam大厅系统 SHALL 创建对应的大厅
4. THE Rich_Presence SHALL 更新为向好友显示"可加入游戏"状态
5. THE 大厅界面 SHALL 显示创建的房间，房主作为玩家列表中的第一个玩家

### Requirement 2

**User Story:** As a player, I want to join existing game rooms through multiple methods, so that I can participate in multiplayer sessions with different groups of players.

#### Acceptance Criteria

1. WHEN a player clicks "加入房间" button, THE Lobby_Interface SHALL display connection options including room ID, friend invite code, and direct IP connection
2. WHEN a player selects room ID connection, THE Connection_Manager SHALL attempt to join the specified Steam lobby
3. WHEN a player selects direct IP connection, THE Connection_Manager SHALL establish P2P connection to the specified IP address and port 9050
4. IF a room requires a password, THE Lobby_Interface SHALL prompt for password input before connection attempt
5. WHEN connection is successful, THE Lobby_Interface SHALL display the joined room information and player list

### Requirement 3

**User Story:** As a player, I want to invite friends and have them join my game easily, so that we can play together without complex setup procedures.

#### Acceptance Criteria

1. WHEN a Room_Host has an active room, THE Rich_Presence SHALL display "可加入游戏" status to Steam friends
2. WHEN a friend right-clicks the Room_Host in Steam friends list, THE Steam_Lobby_System SHALL show "邀请加入游戏" and "加入游戏" options
3. WHEN a friend clicks "加入游戏", THE Connection_Manager SHALL automatically obtain connection information and establish P2P connection
4. THE Room_Client SHALL automatically enter the Lobby_Interface showing the joined room
5. THE Room_State SHALL update to reflect the new player in all connected clients

### Requirement 4

**User Story:** As a player in a room, I want to see real-time information about the room and other players, so that I can coordinate gameplay and monitor connection status.

#### Acceptance Criteria

1. THE Lobby_Interface SHALL display current room information including room name, host, map, mode, and password status
2. THE Lobby_Interface SHALL show a real-time player list with player names, host designation, and connection status
3. WHEN players join or leave, THE Room_State SHALL update immediately across all connected clients
4. THE Lobby_Interface SHALL display connection status including P2P port, latency, and connection type
5. IF a player is the Room_Host, THE Lobby_Interface SHALL provide "开始游戏" button and room management controls

### Requirement 5

**User Story:** As a player, I want to browse available public rooms, so that I can find and join games with other players.

#### Acceptance Criteria

1. THE Lobby_Interface SHALL display a room list showing available public rooms with basic information
2. WHEN a player clicks "刷新" button, THE Steam_Lobby_System SHALL update the room list with current available lobbies
3. THE room list SHALL show room name, current player count, maximum players, and connection status for each room
4. WHEN a player double-clicks a room in the list, THE Connection_Manager SHALL attempt to join that room
5. THE room list SHALL automatically refresh every 30 seconds to maintain current information

### Requirement 6

**User Story:** As a player, I want to see my Steam friends' online status and easily interact with them, so that I can coordinate multiplayer sessions.

#### Acceptance Criteria

1. THE Lobby_Interface SHALL display a friends list showing Steam friends with their online status
2. THE friends list SHALL use color coding: green for online and available, yellow for in-game, red for offline
3. WHEN a player right-clicks a friend, THE Lobby_Interface SHALL show context menu with "邀请好友" option
4. WHEN "邀请好友" is selected, THE Steam_Lobby_System SHALL send a game invitation to the selected friend
5. THE friends list SHALL update automatically when friends' status changes

### Requirement 7

**User Story:** As a room host, I want to manage my room and control the game session, so that I can ensure a good multiplayer experience.

#### Acceptance Criteria

1. WHEN a Room_Host wants to start the game, THE Lobby_Interface SHALL provide "开始游戏" button available only to the host
2. THE Room_Host SHALL be able to kick players from the room through player list context menu
3. WHEN the Room_Host leaves, THE Connection_Manager SHALL either transfer host to another player or close the room
4. THE Room_Host SHALL be able to change room settings including map and game mode before starting
5. WHEN "开始游戏" is clicked, THE Room_State SHALL transition all connected players to the game session