# 实现计划

-   [x] 1. 准备工作：验证和增强 PlayerInfoDatabase

    -   验证 PlayerInfoDatabase 的基本功能是否正常
    -   确认 CustomData 字典可以正确存储和读取 Latency 和 IsInGame
    -   添加调试日志输出数据库内容
    -   _需求: 1.1, 5.4_

-   [x] 2. 在 NetService 中添加数据库更新逻辑

    -   [x] 2.1 在 ClientStatusMessage 处理逻辑中更新数据库

        -   找到 ClientStatusMessage 的处理代码
        -   添加 AddOrUpdatePlayer 调用，传入 SteamId、PlayerName、EndPoint
        -   添加 SetCustomData 调用，存储 Latency 和 IsInGame
        -   添加日志验证更新成功
        -   _需求: 5.1, 5.4_

    -   [x] 2.2 在玩家连接事件中更新数据库

        -   找到 OnPeerConnected 或类似的连接处理代码
        -   添加 AddOrUpdatePlayer 调用，创建新的 PlayerInfoEntity
        -   设置 IsLocalPlayer 标志（如果是本地玩家）
        -   添加日志验证玩家添加成功
        -   _需求: 5.2_

    -   [x] 2.3 在玩家断开连接事件中更新数据库

        -   找到 OnPeerDisconnected 或类似的断开处理代码
        -   更新 PlayerInfoEntity 的 LastSeen 时间戳
        -   可选：从数据库中移除玩家，或保留一段时间
        -   添加日志验证更新成功
        -   _需求: 5.3_

    -   [x] 2.4 在 OnNetworkLatencyUpdate 中同步延迟到数据库

        -   找到 NetService.OnNetworkLatencyUpdate 方法
        -   在更新 playerStatuses[peer].Latency 后，同步到数据库
        -   获取玩家的 SteamId（从 PlayerInfoDatabase 或其他来源）
        -   调用 PlayerInfoDatabase.Instance.SetCustomData(steamId, "Latency", latency)
        -   添加日志验证同步成功
        -   _需求: 5.4_

    -   [x] 2.5 添加定期同步 IsInGame 状态的逻辑

        -   在 NetService 的 Update 方法中添加定时器
        -   遍历 playerStatuses 和 clientPlayerStatuses
        -   将每个玩家的 IsInGame 同步到 PlayerInfoDatabase.CustomData
        -   添加频率限制（例如每秒更新一次），避免过于频繁的更新
        -   _需求: 5.4_

-   [x] 3. 重构 MModUI.UpdatePlayerList() 方法

    -   [x] 3.1 移除旧的数据源访问逻辑

        -   移除 `isSteamMode` 变量和相关检查
        -   移除 `playerStatuses`、`clientPlayerStatuses`、`localPlayerStatus` 的访问
        -   移除 `displayedSteamIds` 和 `displayedEndPoints` 集合
        -   移除 Steam Lobby 成员列表遍历逻辑
        -   移除虚拟状态创建逻辑
        -   _需求: 2.1, 2.2, 2.3, 2.4, 2.5_

    -   [x] 3.2 实现新的数据库驱动逻辑

        -   调用 PlayerInfoDatabase.Instance.GetAllPlayers() 获取所有玩家
        -   提取 SteamId 集合用于变化检测
        -   使用 \_displayedPlayerIds.SetEquals() 检测变化
        -   只有变化时才重建 UI
        -   移除 Steam 模式下的定期强制刷新逻辑
        -   _需求: 1.1, 1.2_

    -   [x] 3.3 更新 UI 重建逻辑


        -   清空现有列表时使用 SteamId 作为键
        -   遍历数据库中的所有玩家调用 CreatePlayerEntry
        -   更新 \_displayedPlayerIds 缓存
        -   添加日志记录 UI 重建事件
        -   _需求: 1.1, 1.3_

-   [x] 4. 重构 MModUI.CreatePlayerEntry() 方法

    -   [x] 4.1 修改方法签名

        -   将参数从 `PlayerStatus status, bool isLocal` 改为 `PlayerInfoEntity player`
        -   移除 `isLocal` 参数，改用 `player.IsLocalPlayer`
        -   _需求: 2.6_

    -   [x] 4.2 移除模式相关的显示逻辑

        -   移除 `isSteamMode` 检查
        -   移除 `displayName` 和 `displayId` 的复杂计算
        -   移除从投票数据获取名称的逻辑
        -   移除 `GetSteamIdFromStatus()` 调用

        -   移除 Steam API 调用（SteamFriends.GetFriendPersonaName 等）
        -   移除 `isHost` 判断和前缀添加（HOST*、CLIENT*）
        -   _需求: 2.5, 2.7, 2.8, 2.9, 3.6, 3.7, 3.8_

    -   [x] 4.3 实现统一的玩家信息显示


        -   直接使用 player.PlayerName 显示名称
        -   直接使用 player.SteamId 显示 ID
        -   直接使用 player.EndPoint 显示网络地址
        -   从 player.CustomData 读取 Latency 和 IsInGame
        -   使用默认值处理 CustomData 缺失的情况

        -   _需求: 1.3, 3.1, 3.2, 3.3, 4.1, 4.2_


    -   [ ] 4.4 更新延迟文本引用的键

        -   将 \_playerPingTexts 的键从 EndPoint 改为 SteamId

        -   确保 UpdatePlayerPingDisplays 能正确找到文本组件

        -   _需求: 4.3, 5.5_



    -   [ ] 4.5 更新踢人按钮逻辑
        -   使用 player.SteamId 而不是从 status 获取
        -   保持踢人功能的正常工作
        -   _需求: 6.2_

-   [x] 5. 重构 MModUI.UpdatePlayerPingDisplays() 方法


    -   移除从 playerStatuses 和 clientPlayerStatuses 收集状态的逻辑
    -   改为从 PlayerInfoDatabase.Instance.GetAllPlayers() 获取玩家
    -   使用 SteamId 作为键查找 \_playerPingTexts
    -   从 player.CustomData 读取 Latency
    -   使用默认值处理 CustomData 缺失的情况
    -   _需求: 4.3, 4.4, 5.5_

-   [x] 6. 清理和移除未使用的代码

-   [ ] 6. 清理和移除未使用的代码

    -   [x] 6.1 移除 GetSteamIdFromStatus() 方法

        -   删除整个方法定义
        -   确认没有其他地方调用此方法
        -   _需求: 2.8_

    -   [x] 6.2 移除未使用的字段和变量

        -   移除 \_noSteamIdWarningCount 和 NO_STEAMID_WARNING_INTERVAL

        -   检查并移除其他未使用的 Steam 相关字段
        -   _需求: 2.5_

    -   [x] 6.3 清理导入和依赖

        -   检查是否有未使用的 using 语句
        -   移除不再需要的 Steam API 引用
        -   _需求: 2.9_

-   [x] 7. 编译和部署






    -   运行 getDiagnostics 检查语法错误
    -   运行 dotnet build 编译项目
    -   确认编译成功（无错误）
    -   使用 API 上传部署 DLL
    -   _需求: 所有_

-   [x] 8. Git 提交和推送




    -   使用 git add 添加所有修改的文件
    -   使用 git commit 提交更改，提交信息：`重构玩家UI为数据库驱动，移除模式区分逻辑`
    -   使用 git push 推送到远程仓库
    -   _需求: 所有_



-   [x] 9. 备份项目



    -   切换到父目录
    -   压缩整个项目文件夹为 zip 文件
    -   使用当前日期作为文件名（格式：`EscapeFromDuckovCoopMod_YYYYMMDD_HHMMSS.zip`）
    -   保存备份文件
    -   _需求: 所有_

## 人工测试清单

完成上述任务后，请进行以下人工测试：

### 测试 1: 验证数据库更新逻辑

-   [ ] 启动游戏，连接玩家
-   [ ] 检查日志，确认数据库正确更新
-   [ ] 使用 PlayerInfoDatabase.ExportToJson() 导出数据验证
-   _需求: 5.1, 5.2, 5.3, 5.4_

### 测试 2: 验证 UI 渲染

-   [ ] 打开玩家状态面板（按 P 键）
-   [ ] 验证玩家列表正确显示
-   [ ] 验证本地玩家标识正确（蓝色边框）
-   [ ] 验证延迟和状态显示正确
-   _需求: 1.1, 1.2, 1.3, 3.1, 3.2, 3.3, 4.1, 4.2_

### 测试 3: 验证实时更新

-   [ ] 连接新玩家，验证列表自动更新
-   [ ] 断开玩家，验证列表自动更新
-   [ ] 验证延迟显示实时更新（每秒刷新）
-   _需求: 4.3, 4.4, 5.5_

### 测试 4: 验证踢人功能

-   [ ] 作为主机，打开玩家状态面板
-   [ ] 点击其他玩家的"踢"按钮
-   [ ] 验证玩家被成功踢出
-   _需求: 6.2_

### 测试 5: 验证其他功能不受影响

-   [ ] 测试投票面板是否正常（按 J 键发起投票）
-   [ ] 测试主机列表是否正常（按 = 键打开联机面板）
-   [ ] 测试观战功能是否正常
-   _需求: 6.1, 6.3, 6.4, 6.5_

### 测试 6: Steam 模式测试

-   [ ] 创建 Steam Lobby
-   [ ] 邀请其他玩家加入
-   [ ] 验证玩家列表显示正确
-   [ ] 验证 Steam 名称显示正确

### 测试 7: 直连模式测试

-   [ ] 启动直连服务器
-   [ ] 客户端连接服务器
-   [ ] 验证玩家列表显示正确
-   [ ] 验证 IP 地址显示正确
