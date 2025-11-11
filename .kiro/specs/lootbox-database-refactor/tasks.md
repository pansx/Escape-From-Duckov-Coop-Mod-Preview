# 战利品箱数据库重写任务列表

## 任务概述

本任务列表描述了将战利品箱同步机制从现有的字典/列表结构重构为基于 `InMemoryDatabase` 的统一数据库架构的实施步骤。

## 任务列表

-   [x] 1. 创建战利品箱数据库基础设施

    -   [x] 1.1 创建实体类

        -   创建 `LootBoxEntity` 类，包含 SetId、GameObject、Inventory、Position、SceneName、Capacity、Items、OwnerId、LastModified、CustomData 字段
        -   创建 `LootItemEntry` 类，包含 Position 和 ItemSnapshot 字段
        -   添加必要的构造函数和属性
        -   _需求: 1.1_

    -   [x] 1.2 创建数据库类

        -   创建 `LootBoxDatabase` 类，继承 `InMemoryDatabase<LootBoxEntity>` 模式
        -   在构造函数中配置主键索引（SetId）
        -   配置二级索引：SceneName、OwnerId
        -   配置空间索引（Position，网格大小 50m）
        -   _需求: 1.2, 1.3, 1.4_

    -   [x] 1.3 实现基础操作方法

        -   实现 `AddLootBox(GameObject go, Inventory inv, string setId)` 方法
        -   实现 `UpdateLootBox(string setId)` 方法
        -   实现 `RemoveLootBox(string setId)` 方法
        -   实现 `Clear()` 方法
        -   _需求: 1.5_

    -   [x] 1.4 实现查询方法

        -   实现 `GetLootBox(string setId)` 方法（O(1) 主键查询）
        -   实现 `GetLootBoxesByScene(string sceneName)` 方法（O(1) 索引查询）
        -   实现 `GetLootBoxesInRadius(Vector3 center, float radius)` 方法（O(1) 空间查询）
        -   实现 `GetPublicLootBoxes()` 方法（过滤 OwnerId == -1）
        -   _需求: 1.5_

    -   [x] 1.5 实现 JSON 导出方法

        -   实现 `ExportToJson(bool indented = true)` 方法
        -   实现 `ExportToJsonWithStats(bool indented = true)` 方法
        -   实现 `ExportBySceneToJson(string sceneName, bool indented = true)` 方法
        -   _需求: 8.1_

-   [x] 2. 定义 JSON 消息类型

    -   [x] 2.1 创建请求消息类

        -   创建 `LootOpenRequest` 类（SetId, RequestVersion）
        -   创建 `LootPutRequest` 类（SetId, PreferredPosition, Token, ItemSnapshot）
        -   创建 `LootTakeRequest` 类（SetId, Position, Token, DestType, DestPosition）
        -   创建 `LootSplitRequest` 类（SetId, SourcePosition, Count, PreferredPosition）
        -   添加 JSON 序列化属性
        -   _需求: 2.1, 2.3, 2.4_

    -   [x] 2.2 创建响应消息类

        -   创建 `LootStateResponse` 类（SetId, Capacity, Items, Timestamp）
        -   创建 `LootOperationResponse` 类（Success, Token, ErrorMessage, ResultItem）
        -   添加 JSON 序列化属性
        -   _需求: 2.2_

    -   [x] 2.3 创建广播消息类

        -   创建 `LootItemAdded` 类（SetId, Position, ItemSnapshot）
        -   创建 `LootItemRemoved` 类（SetId, Position）
        -   创建 `LootItemModified` 类（SetId, Position, ItemSnapshot）
        -   添加 JSON 序列化属性
        -   _需求: 2.5_

    -   [x] 2.4 注册消息类型

        -   在 `JsonMessageRouter` 中注册所有请求消息类型
        -   在 `JsonMessageRouter` 中注册所有响应消息类型
        -   在 `JsonMessageRouter` 中注册所有广播消息类型
        -   配置消息处理器映射
        -   _需求: 2.1, 2.2, 2.3, 2.4, 2.5_

-   [x] 3.  实现主机端数据库初始化

            -   [x] 3.1 创建同步管理器类


                -   创建 `LootBoxSyncManager` 类
                -   添加 `_hostDatabase` 字段（LootBox

        Database 类型） - 添加 `_currentLootBox` 字段（客户端用） - 添加 `IsHost` 属性 - 添加单例模式（可选） - _需求: 3.1_ - [x] 3.2 实现扫描战利品箱

                -   实现 `Host_InitializeDatabase()` 方法
                -   使用 `LevelManager.LootBoxInventories` 或 `FindObjectsOfType<LootBoxLoader>()` 扫描场景
                -   为每个战利品箱生成唯一的 SetId（使用场景名 + 位置哈希）
                -   记录日志：扫描到的箱子数量
                -   _需求: 3.1_
            -   [x] 3.3 实现物品读取和清空

                -   遍历每个战利品箱的 Inventory.Content
                -   将每个 Item 转换为 ItemSnapshot（调用 `ItemTool.WriteItemSnapshot()`）
                -   创建 LootItemEntry 并添加到 Items 列表
                -   创建 LootBoxEntity 并插入数据库
                -   清空 Inventory：调用 `inv.RemoveAt()` 并 `Destroy(item.gameObject)`
                -   记录日志：每个箱子的物品数量、总物品数
                -   _需求: 3.2_

-   [x] 4. 实现主机端打开请求处理

    -   [x] 4.1 实现数据库查询

        -   实现 `Host_HandleOpenRequest(NetPeer peer, LootOpenRequest request)` 方法
        -   从数据库查询战利品箱：`entity = _hostDatabase.GetLootBox(request.SetId)`
        -   验证 entity 是否为 null
            -- _需求: 3.3_

    -   [x] 4.2 实现错误处理

        -   如果 entity 为 null，创建错误响应
        -   发送 `LootOperationResponse`（Success=false, ErrorMessage="LootBoxNotFound"）
        -   记录日志：请求者、SetId、错误原因
        -   _需求: 6.1, 10.2_

    -   [x] 4.3 实现成功响应

        -   创建 `LootStateResponse` 对象
        -   设置 SetId、Capacity、Items、Timestamp
        -   通过 `JsonMessageRouter` 发送给请求者
        -   记录日志：请求者、SetId、物品数量、响应大小（JSON 字符串长度）
        -   _需求: 3.3, 2.2_

-   [x] 5. 实现主机端放入请求处理

    -   [x] 5.1 实现请求验证

        -   实现 `Host_HandlePutRequest(NetPeer peer, LootPutRequest request)` 方法
        -   从数据库查询战利品箱
        -   验证战利品箱是否存在
        -   验证物品快照是否有效：typeId 存在、堆叠数量 > 0 且 <= MaxStack
        -   如果验证失败，发送错误响应并返回
        -   _需求: 6.2_

    -   [x] 5.2 实现物品添加

        -   查找合适的位置：优先使用 PreferredPosition，如果已占用则查找第一个空位
        -   如果没有空位，发送错误响应（CapacityExceeded）
        -   创建 LootItemEntry（Position, ItemSnapshot）
        -   添加到 entity.Items 列表
        -   更新数据库：`_hostDatabase.UpdateLootBox(entity.SetId)`
        -   _需求: 3.4_

    -   [x] 5.3 实现响应和广播

        -   发送 `LootOperationResponse` 给请求者（Success=true, Token）
        -   创建 `LootItemAdded` 消息（SetId, Position, ItemSnapshot）
        -   广播给所有客户端（使用 `JsonMessageRouter.Broadcast()`）
        -   记录日志：操作者、SetId、Position、ItemType
        -   _需求: 2.5_

-   [x] 6. 实现主机端取出请求处理

    -   [x] 6.1 实现请求验证

        -   实现 `Host_HandleTakeRequest(NetPeer peer, LootTakeRequest request)` 方法

        -   从数据库查询战利品箱
        -   验证战利品箱是否存在
        -   验证位置是否有效：Position >= 0 && Position < Capacity
        -   查找该位置的物品：`item = entity.Items.Find(e => e.Position == request.Position)`
        -   如果验证失败，发送错误响应并返回
        -   _需求: 6.3_

    -   [x] 6.2 实现物品移除

        -   从 entity.Items 列表中移除该物品
        -   更新数据库：`_hostDatabase.UpdateLootBox(entity.SetId)`
        -   _需求: 3.4_

    -   [x] 6.3 实现响应和广播

        -   发送 `LootOperationResponse` 给请求者（Success=true, Token, ResultItem=ItemSnapshot）
        -   创建 `LootItemRemoved` 消息（SetId, Position）
        -   广播给所有客户端
        -   记录日志：操作者、SetId、Position、ItemType
        -   _需求: 2.5_

-   [x] 7. 实现主机端拆分请求处理

    -   [x] 7.1 实现请求验证

        -   实现 `Host_HandleSplitRequest(NetPeer peer, LootSplitRequest request)` 方法
        -   从数据库查询战利品箱
        -   验证战利品箱是否存在
        -   查找源位置的物品
        -   验证物品是否可堆叠（Stackable=true）
        -   验证拆分数量是否合法：Count > 0 && Count < StackCount
        -   如果验证失败，发送错误响应并返回
        -   _需求: 6.4_

    -   [x] 7.2 实现拆分逻辑

        -   修改源物品的堆叠数量：`sourceItem.Snapshot.Stack -= request.Count`
        -   创建新物品快照：复制源物品快照，设置 Stack = request.Count
        -   查找目标位置：优先使用 PreferredPosition，否则查找第一个空位
        -   如果没有空位，发送错误响应
        -   创建新的 LootItemEntry 并添加到 entity.Items
        -   更新数据库
        -   _需求: 3.4_

    -   [x] 7.3 实现广播

        -   创建 `LootItemModified` 消息（源物品）
        -   创建 `LootItemAdded` 消息（新物品）
        -   广播两条消息给所有客户端
        -   记录日志：操作者、SetId、SourcePosition、Count、TargetPosition
        -   _需求: 2.5_

-   [x] 8. 实现客户端打开请求

    -   [x] 8.1 实现请求发送

        -   实现 `Client_RequestOpen(string setId)` 方法

        -   创建 `LootOpenRequest` 消息（SetId, RequestVersion=1）
        -   通过 `JsonMessageRouter.Send()` 发送给主机
        -   记录日志：SetId、请求时间
        -   _需求: 2.1_

    -   [x] 8.2 实现 UI 状态更新

        -   在发送请求前，显示加载中 UI（如果有 LootView）
        -   禁用 UI 交互（防止重复请求）
        -   _需求: 4.2_

-   [x] 9. 实现客户端状态响应处理

    -   [x] 9.1 实现响应接收
        -   实现 `Client_HandleStateResponse(LootStateResponse response)` 方法
        -   保存当前打开的箱子：`_currentLootBox = response`
        -   记录日志：SetId、物品数量、接收时间
        -   _需求: 4.3_
    -   [x] 9.2 实现 UI 更新（协程）
        -   创建协程 `ApplyLootboxStateCoroutine(LootStateResponse response)`
        -   清空 UI 中的旧物品（遍历并销毁）
        -   遍历 response.Items，为每个物品创建 UI 元素
        -   分帧处理：每 5 个物品 yield return null
        -   记录处理耗时
        -   _需求: 7.2_
    -   [x] 9.3 实现 UI 状态恢复
        -   隐藏加载中 UI
        -   启用 UI 交互
        -   刷新容量文本、按钮状态等
        -   _需求: 4.3_

-   [x] 10. 实现客户端放入请求

    -   [x] 10.1 实现 Token 生成和物品序列化

        -   实现 `Client_RequestPut(string setId, Item item, int preferredPosition)` 方法
        -   生成唯一的 Token：`token = _nextToken++`

        -   将物品序列化为 ItemSnapshot：调用 `ItemTool.WriteItemSnapshot()`
        -   _需求: 5.1_

    -   [x] 10.2 实现请求发送

        -   创建 `LootPutRequest` 消息（SetId, PreferredPosition, Token, ItemSnapshot）
        -   保存待处理的物品：`_pendingPutItems[token] = item`

        -   通过 `JsonMessageRouter.Send()` 发送给主机

        -   记录日志：SetId、Token、ItemType、PreferredPosition
        -   _需求: 2.3_

-   [x] 11. 实现客户端取出请求

    -   [x] 11.1 实现 Token 生成和目的地保存

        -   实现 `Client_RequestTake(string setId, int position, DestinationType destType)` 方法
        -   生成唯一的 Token：`token = _nextToken++`
        -   创建目的地信息对象（destType, destInv, destPos, destSlot）
        -   保存目的地信息：`_pendingTakeDestinations[token] = destInfo`
        -   _需求: 2.4_

    -   [x] 11.2 实现请求发送

        -   创建 `LootTakeRequest` 消息（SetId, Position, Token, DestType, DestPosition）
        -   通过 `JsonMessageRouter.Send()` 发送给主机
        -   记录日志：SetId、Token、Position、DestType
        -   _需求: 2.4_

-   [x] 12. 实现客户端拆分请求

            -   实现 `Client_RequestSplit(string setId, int sourcePosition, int count, int preferredPosition)` 方法
            -   创建 `LootSplitRequest` 消息（SetId, Sou

        rcePosition, Count, PreferredPosition） - 通过 `JsonMessageRouter.Send()` 发送给主机 - 记录日志：SetId、SourcePosition、Count、PreferredPosition - _需求: 2.4_

-

-   [x] 13. 实现客户端操作响应处理

    -   [x] 13.1 实现响应接收和验证

        -   实现 `Client_HandleOperationResponse(LootOperationResponse response)` 方法
        -   检查操作是否成功（response.Success）

        -   如果失败，显示错误消息（使用 UI 提示或日志）
        -   记录日志：Token、Success、ErrorMessage
        -   _需求: 10.4_

    -   [x] 13.2 实现 PUT 操作成功处理

        -   从 `_pendingPutItems` 中查找并删除物品：`item = _pendingPutItems[response.Token]`
        -   调用 `item.Detach()` 从背包分离
        -   调用 `Destroy(item.gameObject)` 销毁本地物品
        -   清理重复引用（遍历 \_pendingPutItems，移除已销毁的物品）
        -   _需求: 4.4_

    -   [x] 13.3 实现 TAKE 操作成功处理

        -   从 `_pendingTakeDestinations` 中获取目的地信息
        -   从 response.ResultItem 重建物品：`item = ItemTool.BuildItemFromSnapshot(response.ResultItem)`
        -   根据目的地类型放入：装备槽（slot.Plug）、背包格（inv.AddAt）、背包（inv.AddAndMerge）
        -   如果放入失败，发送 PUT 请求放回容器
        -   _需求: 4.4_

-   [x] 14. 实现客户端增量更新处理

    -   [x] 14.1 实现物品添加广播处理

        -   实现 `Client_HandleItemAdded(LootItemAdded message)` 方法
        -   检查是否是当前打开的箱子：`_currentLootBox?.SetId == message.SetId`
        -   如果不是，忽略并返回

        -   如果是，从 ItemSnapshot 重建物品并添加到 UI（指定位置）
        -   记录日志：SetId、Position、ItemType
        -   _需求: 4.5, 2.5_

    -   [x] 14.2 实现物品移除广播处理

        -   实现 `Client_HandleItemRemoved(LootItemRemoved message)` 方法
        -   检查是否是当前打开的箱子
        -   如果不是，忽略并返回
        -   如果是，从 UI 中移除对应位置的物品（销毁 UI 元素）
        -   记录日志：SetId、Position
        -   _需求: 4.5, 2.5_

    -   [x] 14.3 实现物品修改广播处理

        -   实现 `Client_HandleItemModified(LootItemModified message)` 方法
        -   检查是否是当前打开的箱子
        -   如果不是，忽略并返回
        -   如果是，更新对应位置的物品（如堆叠数量、耐久度）
        -   记录日志：SetId、Position、修改内容
        -   _需求: 4.5, 2.5_

-   [x] 15. 集成到现有的 LootNet 系统

    -   [x] 15.1 添加同步管理器实例

        -   在 `LootNet.cs` 中添加 `LootBoxSyncManager` 字段或属性
        -   在构造函数或初始化方法中创建实例
        -   _需求: 9.1_

    -   [x] 15.2 集成主机端初始化

        -   在场景加载时（如 `LevelManagerPatch` 或 `SceneNet`）调用 `Host_InitializeDatabase()`
        -   确保只在主机端调用（检查 `IsServer`）
        -   添加日志：初始化开始和完成
        -   _需求: 9.2_

    -   [x] 15.3 替换客户端请求方法

        -   找到所有调用 `Client_RequestLootState()` 的地方，替换为 `Client_RequestOpen()`
        -   找到所有调用 `Client_SendLootPutRequest()` 的地方，替换为 `Client_RequestPut()`
        -   找到所有调用 `Client_SendLootTakeRequest()` 的地方，替换为 `Client_RequestTake()`
        -   找到所有调用 `Client_SendLootSplitRequest()` 的地方，替换为 `Client_RequestSplit()`
        -   _需求: 9.3_

    -   [x] 15.4 添加条件编译支持

        -   在文件顶部添加 `#define NEW_LOOT_SYSTEM`
        -   使用 `#if NEW_LOOT_SYSTEM` 包裹新代码
        -   使用 `#else` 保留旧代码作为备用
        -   _需求: 9.4_

-   [x] 16. 实现错误处理和重试机制

    -   [x] 16.1 创建错误处理器类

        -   创建 `LootErrorHandler` 类
        -   定义 `LootErrorType` 枚举（LootBoxNotFound, InvalidSetId, CapacityExceeded, ItemNotFound, InvalidPosition, ValidationFailed, NetworkTimeout）
        -   实现 `HandleError(LootErrorType errorType, string setId, string details)` 方法
        -   _需求: 10.1, 10.5_

    -   [x] 16.2 实现网络超时重试

        -   实现 `RetryRequest(string setId, Action retryAction)` 方法
        -   使用异步循环，最多重试 3 次
        -   指数退避：100ms, 200ms, 400ms（使用 `await Task.Delay()`）
        -   记录每次重试到日志
        -   _需求: 10.1_

    -   [x] 16.3 实现错误通知

        -   实现战利品箱不存在的处理：关闭 LootView UI，显示提示消息
        -   实现验证失败的处理：显示错误消息（使用 UI 提示或 Debug.LogWarning）
        -   实现容量超限的处理：显示提示消息
        -   记录所有错误到日志（包括时间戳、SetId、错误类型、详细信息）
        -   _需求: 10.2, 10.3, 10.4_

-   [x] 17. 添加调试和监控功能

    -   [x] 17.1 添加数据库导出按钮

        -   在 `MModUI.cs` 的调试面板中添加 "导出战利品箱数据库" 按钮
        -   实现 `DebugExportLootBoxDatabase()` 方法
        -   调用 `LootBoxSyncManager.ExportDatabaseToJson(indented: true)`
        -   保存到文件：`Application.persistentDataPath/lootbox_database_{timestamp}.json`
        -   显示保存路径到日志

        -   _需求: 8.1_

    -   [x] 17.2 添加操作日志

        -   在所有主机端请求处理方法中添加日志（使用 `LoggerHelper.Log()`）
        -   日志格式：`[LOOT][Host] Operation: {type}, SetId: {setId}, Peer: {peer}, Details: {details}`
        -   在所有客户端请求方法中添加日志
        -   日志格式：`[LOOT][Client] Request: {type}, SetId: {setId}, Details: {details}`
        -   _需求: 8.2_

    -   [x] 17.3 添加统计功能

        -   在 `LootBoxSyncManager` 中添加统计字段：`_queryCount`, `_operationCount`, `_totalLatency`
        -   在每次操作时更新统计数据
        -   实现 `GetStatistics()` 方法，返回统计信息
        -   _需求: 8.3_

    -   [x] 17.4 集成到 Debug 面板

        -   在 `MModUI.DebugPrintRemoteCharacters()` 或类似方法中添加数据库状态
        -   包含：数据库大小、查询次数、操作次数、平均延迟
        -   包含：当前打开的箱子 SetId（如果有）
        -   _需求: 8.5_

-   [ ] 18. 人工测试准备

    -   [ ] 18.1 编译和部署
        -   编译项目：`dotnet build EscapeFromDuckovCoopMod.sln --configuration Release`
        -   检查编译是否成功（无错误）
        -   部署 DLL 到游戏目录（使用 API 上传或手动复制）
        -   _需求: 所有需求_
    -   [ ] 18.2 准备测试环境
        -   准备测试场景：选择包含多个战利品箱的场景（如主城、地下城）
        -   准备测试物品：各种类型的物品（可堆叠、不可堆叠、带附件、大型物品等）
        -   准备测试环境：主机 + 至少 1 个客户端（最好 2 个客户端）
        -   准备日志查看工具：记事本或日志查看器
        -   _需求: 所有需求_

-   [ ] 19. 人工功能测试

    -   [ ] 19.1 主机初始化测试
        -   启动游戏作为主机，进入测试场景
        -   检查日志中的箱子扫描数量：`[LOOT][Host] Initialized database with X loot boxes`
        -   打开一个箱子，验证物品是否正确显示
        -   使用调试命令或工具验证 Inventory 是否被清空（Content.Count == 0）
        -   记录测试结果：成功/失败、箱子数量、物品数量
        -   _需求: 3.1, 3.2_
    -   [ ] 19.2 打开箱子测试
        -   主机玩家打开箱子，验证物品是否正确显示
        -   客户端玩家打开箱子，验证物品是否正确显示
        -   验证加载中 UI 是否正常显示和隐藏
        -   检查日志中的请求和响应消息
        -   记录测试结果：成功/失败、延迟时间、截图
        -   _需求: 4.1, 4.2, 4.3_
    -   [ ] 19.3 放入物品测试
        -   从背包拖拽物品到箱子
        -   验证物品是否正确添加到箱子
        -   验证物品是否从背包消失
        -   在另一个客户端打开同一个箱子，验证物品是否实时出现
        -   检查日志中的 PUT 请求和广播消息
        -   记录测试结果：成功/失败、延迟时间、截图
        -   _需求: 5.1, 5.2, 5.3_
    -   [ ] 19.4 取出物品测试
        -   从箱子拖拽物品到背包
        -   验证物品是否正确添加到背包
        -   验证物品是否从箱子消失
        -   在另一个客户端查看同一个箱子，验证物品是否实时消失
        -   检查日志中的 TAKE 请求和广播消息
        -   记录测试结果：成功/失败、延迟时间、截图
        -   _需求: 6.1, 6.2, 6.3_
    -   [ ] 19.5 拆分物品测试
        -   拆分堆叠物品（如子弹、药品）
        -   验证源物品数量是否减少
        -   验证新物品是否出现在正确位置
        -   在另一个客户端查看，验证是否实时更新
        -   检查日志中的 SPLIT 请求和广播消息
        -   记录测试结果：成功/失败、数量是否正确、截图
        -   _需求: 7.1, 7.2, 7.3_
    -   [ ] 19.6 多客户端并发测试
        -   两个客户端同时打开同一个箱子
        -   两个客户端同时操作（一个放入，一个取出）
        -   验证是否有冲突、丢失或重复
        -   验证最终状态是否一致
        -   记录测试结果：成功/失败、是否有异常、日志
        -   _需求: 所有需求_
    -   [ ] 19.7 错误处理测试
        -   测试断网情况：拔网线或禁用网络，验证重试机制
        -   测试无效操作：尝试取出不存在的物品，验证错误提示
        -   测试容量超限：尝试放入物品到已满的箱子，验证错误提示
        -   测试箱子不存在：尝试打开已销毁的箱子，验证错误提示
        -   记录测试结果：成功/失败、错误消息是否正确、日志
        -   _需求: 10.1, 10.2, 10.3, 10.4_

-   [ ] 20. 人工性能测试

    -   [ ] 20.1 大量箱子测试
        -   在场景中放置或找到 100+ 个箱子
        -   记录主机初始化时间（从日志中查看）
        -   随机打开 10 个箱子，记录每次打开的延迟
        -   计算平均延迟，目标：< 100ms
        -   记录测试结果：初始化时间、平均延迟、是否有卡顿
        -   _需求: 7.1_
    -   [ ] 20.2 大型容器测试
        -   创建或找到包含 50+ 物品的箱子
        -   打开箱子，观察 UI 加载流畅度
        -   记录打开延迟和 UI 渲染时间
        -   验证分帧处理是否生效（每 5 个物品 yield）
        -   记录测试结果：延迟时间、是否有明显卡顿、帧率
        -   _需求: 7.2_
    -   [ ] 20.3 网络延迟测试
        -   使用网络模拟工具（如 clumsy）添加 200ms 延迟
        -   测试打开箱子、放入、取出操作的响应时间
        -   验证操作是否能正常完成
        -   记录测试结果：各操作的响应时间、是否有超时
        -   _需求: 7.3_
    -   [ ] 20.4 长时间运行测试
        -   连续操作 30 分钟：打开箱子、放入、取出、拆分
        -   每 5 分钟记录一次内存占用（使用任务管理器）
        -   观察是否有性能下降或卡顿
        -   检查日志中是否有异常或警告
        -   记录测试结果：内存占用变化、是否有性能下降、是否有异常
        -   _需求: 7.4, 7.5_
    -   [ ] 20.5 性能优化（如果需要）
        -   如果发现性能问题，分析日志和代码
        -   进行针对性优化（如增加缓存、减少序列化次数等）
        -   重新编译和部署
        -   重新进行性能测试，验证优化效果
        -   _需求: 7.1, 7.2, 7.3, 7.4, 7.5_

-   [ ] 21. 文档和清理
    -   [ ] 21.1 更新文档
        -   更新 `README.md`，添加新的战利品箱同步机制说明
        -   创建或更新 API 文档，说明 `LootBoxDatabase` 和 `LootBoxSyncManager` 的使用方法
        -   添加使用示例和最佳实践
        -   _需求: 9.4_
    -   [ ] 21.2 添加代码注释
        -   为所有公共方法添加 XML 文档注释
        -   为关键逻辑添加行内注释（如数据库初始化、物品序列化等）
        -   为复杂算法添加说明（如空间查询、分帧处理等）
        -   _需求: 9.4_
    -   [ ] 21.3 清理废弃代码
        -   如果确认新系统稳定，移除 `#if OLD_LOOT_SYSTEM` 中的旧代码
        -   移除未使用的字段和方法
        -   移除调试用的临时代码
        -   _需求: 9.4_
    -   [ ] 21.4 更新变更日志
        -   在 CHANGELOG.md 中添加新版本条目
        -   列出主要变更：数据库架构、JSON 消息、性能提升等
        -   列出已知问题和限制（如果有）
        -   _需求: 9.4_

## 注意事项

1. **分帧处理**: 在应用大型容器状态时，每 5 个物品 yield 一次，避免卡顿
2. **日志记录**: 所有关键操作都要记录日志，包括时间戳、操作者、SetId、操作类型
3. **错误处理**: 所有网络操作都要有超时和重试机制
4. **向后兼容**: 保留旧代码作为备用，使用条件编译
5. **测试**: 每个任务完成后都要进行基本的功能测试

## 依赖关系

-   任务 1.2-1.5 依赖任务 1.1（需要先定义实体类）
-   任务 2.2-2.4 依赖任务 2.1（需要先定义请求消息）
-   任务 3.2-3.3 依赖任务 3.1（需要先创建管理器类）
-   任务 4.2-4.3 依赖任务 4.1（需要先实现查询）
-   任务 5.2-5.3 依赖任务 5.1（需要先验证请求）
-   任务 6.2-6.3 依赖任务 6.1（需要先验证请求）
-   任务 7.2-7.3 依赖任务 7.1（需要先验证请求）
-   任务 8.2 依赖任务 8.1（需要先发送请求）
-   任务 9.2-9.3 依赖任务 9.1（需要先接收响应）
-   任务 10.2 依赖任务 10.1（需要先序列化物品）
-   任务 11.2 依赖任务 11.1（需要先生成 Token）
-   任务 13.2-13.3 依赖任务 13.1（需要先验证响应）
-   任务 3-14 依赖任务 1 和 2（需要数据库和消息类型）
-   任务 15 依赖任务 3-14（需要所有核心功能）
-   任务 16-17 依赖任务 15（需要集成完成）
-   任务 18 依赖任务 15-17（需要集成和调试功能完成）
-   任务 19-20 依赖任务 18（需要测试环境准备完成）
-   任务 21 依赖任务 19-20（需要测试完成）

## 预估工作量

-   任务 1: 2-3 小时（数据库基础设施，5 个子任务）
-   任务 2: 1-2 小时（JSON 消息定义，4 个子任务）
-   任务 3: 2-3 小时（主机端初始化，3 个子任务）
-   任务 4: 1-2 小时（主机端打开请求，3 个子任务）
-   任务 5: 2-3 小时（主机端放入请求，3 个子任务）
-   任务 6: 1-2 小时（主机端取出请求，3 个子任务）
-   任务 7: 2-3 小时（主机端拆分请求，3 个子任务）
-   任务 8: 1 小时（客户端打开请求，2 个子任务）
-   任务 9: 2 小时（客户端状态响应，3 个子任务）
-   任务 10: 1 小时（客户端放入请求，2 个子任务）
-   任务 11: 1 小时（客户端取出请求，2 个子任务）
-   任务 12: 0.5 小时（客户端拆分请求，1 个任务）
-   任务 13: 2 小时（客户端操作响应，3 个子任务）
-   任务 14: 1.5 小时（客户端增量更新，3 个子任务）
-   任务 15: 2-3 小时（集成，4 个子任务）
-   任务 16: 2 小时（错误处理，3 个子任务）
-   任务 17: 2 小时（调试功能，4 个子任务）
-   任务 18: 1 小时（测试准备，2 个子任务）
-   任务 19: 3-4 小时（功能测试，7 个子任务）
-   任务 20: 2-3 小时（性能测试，5 个子任务）
-   任务 21: 1-2 小时（文档，4 个子任务）

**总计**: 约 30-40 小时（包含 73 个子任务）

---

_任务列表版本: 1.0_
_最后更新: 2025-11-11_
