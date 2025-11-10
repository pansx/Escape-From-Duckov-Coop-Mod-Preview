# 战利品箱数据库重写需求文档

## 简介

本需求文档描述了将战利品箱同步机制从现有的字典/列表结构重构为基于 `InMemoryDatabase` 的统一数据库架构。目标是实现主机和客户端一视同仁地使用 JSON 消息进行所有战利品箱的增删改查操作，提升代码可维护性、性能和可扩展性。

## 术语表

- **LootBox (战利品箱)**: 游戏中可交互的容器对象，包含物品的 Inventory
- **Inventory (容器)**: 存储物品的数据结构，有固定容量
- **Item (物品)**: 游戏中的物品对象，可以堆叠、有耐久度、可装配件
- **ItemSnapshot (物品快照)**: 物品的序列化数据结构，用于网络传输
- **InMemoryDatabase (内存数据库)**: 高性能的内存数据库，支持 O(1) 索引查询和空间查询
- **LootNet**: 当前的战利品箱同步核心类
- **JsonMessage**: 基于 JSON 的网络消息系统
- **SetId**: 物品或容器的唯一标识符
- **Host (主机)**: 游戏房主，负责权威状态管理
- **Client (客户端)**: 连接到主机的玩家
- **Op Code (操作码)**: 网络消息类型标识符

## 需求

### 需求 1: 战利品箱数据库架构

**用户故事**: 作为开发者，我希望使用统一的数据库来管理所有战利品箱，以便更高效地查询和同步状态。

#### 验收标准

1. THE System SHALL create a LootBoxDatabase class that extends InMemoryDatabase pattern
2. THE LootBoxDatabase SHALL use SetId as primary key for each loot box
3. THE LootBoxDatabase SHALL support index queries by scene name, position, and owner
4. THE LootBoxDatabase SHALL support spatial queries to find nearby loot boxes within radius
5. THE LootBoxDatabase SHALL store complete loot box state including capacity and all items

### 需求 2: JSON 消息统一接口

**用户故事**: 作为开发者，我希望主机和客户端都使用 JSON 消息来操作战利品箱，以便简化网络协议和调试。

#### 验收标准

1. WHEN a player opens a loot box, THE System SHALL send a JSON request message to the host
2. WHEN the host receives an open request, THE System SHALL respond with a JSON snapshot of the loot box contents
3. WHEN a player puts an item into a loot box, THE System SHALL send a JSON put request with item snapshot
4. WHEN a player takes an item from a loot box, THE System SHALL send a JSON take request with position
5. WHEN the host processes any loot box operation, THE System SHALL broadcast a JSON incremental update (item added/removed/modified) to all clients

### 需求 3: 主机端数据库管理

**用户故事**: 作为主机，我希望所有战利品箱状态都存储在数据库中，以便快速查询和验证客户端请求。

#### 验收标准

1. WHEN a scene loads on the host, THE System SHALL scan all loot boxes and read items from their Inventory
2. WHEN items are read from Inventory, THE System SHALL save them to the database and clear the Inventory
3. WHEN a client requests a loot box state, THE System SHALL query the database by SetId in O(1) time
4. WHEN a client modifies a loot box, THE System SHALL only update the database (not the Inventory)
5. WHEN a loot box is destroyed, THE System SHALL remove it from the database

### 需求 4: 客户端状态管理

**用户故事**: 作为客户端，我希望只在打开战利品箱时才加载其内容，以减少内存占用和网络带宽。

#### 验收标准

1. WHEN a client starts the game, THE System SHALL initialize all loot boxes as empty (no items loaded)
2. WHEN a client opens a loot box, THE System SHALL send a request to the host and wait for the response
3. WHEN the host responds with loot box contents, THE System SHALL store it as the current open loot box
4. WHEN a client closes a loot box, THE System SHALL clear the current open loot box data
5. WHEN the host broadcasts an item change, THE System SHALL only apply it if the client has that loot box open

### 需求 5: 物品快照序列化

**用户故事**: 作为开发者，我希望物品快照能够完整序列化为 JSON，以便在网络传输和调试时使用。

#### 验收标准

1. THE System SHALL serialize item basic properties (typeId, stack, durability, inspected) to JSON
2. THE System SHALL recursively serialize item attachments (slots) to JSON
3. THE System SHALL recursively serialize item container contents (inventory) to JSON
4. THE System SHALL deserialize JSON back to ItemSnapshot structure
5. THE System SHALL rebuild complete Item objects from ItemSnapshot including all nested items

### 需求 6: 操作请求验证

**用户故事**: 作为主机，我希望验证所有客户端的战利品箱操作请求，以防止作弊和数据不一致。

#### 验收标准

1. WHEN a client sends a put request, THE System SHALL verify the item snapshot is valid
2. WHEN a client sends a take request, THE System SHALL verify the item exists at the specified position
3. WHEN a client sends a split request, THE System SHALL verify the source item is stackable and has sufficient quantity
4. IF any validation fails, THEN THE System SHALL send a deny message to the client
5. THE System SHALL log all validation failures for debugging

### 需求 7: 性能优化

**用户故事**: 作为玩家，我希望战利品箱操作响应迅速，不会造成游戏卡顿。

#### 验收标准

1. THE System SHALL complete loot box queries in less than 1 millisecond using O(1) index lookups
2. WHEN applying a large loot box snapshot, THE System SHALL yield every 5 items to prevent frame drops
3. THE System SHALL use spatial indexing to find nearby loot boxes in O(1) time
4. THE System SHALL batch multiple loot box updates into a single network message when possible
5. THE System SHALL limit the maximum loot box capacity to 128 slots to prevent memory issues

### 需求 8: 调试和监控

**用户故事**: 作为开发者，我希望能够轻松查看和导出战利品箱数据库状态，以便调试同步问题。

#### 验收标准

1. THE System SHALL provide a debug command to export the entire loot box database to JSON file
2. THE System SHALL log all loot box operations (open, put, take, split) with timestamps
3. THE System SHALL provide statistics on database size, query counts, and operation latency
4. THE System SHALL support filtering database exports by scene, position range, or owner
5. THE System SHALL include database state in the debug panel JSON output

### 需求 9: 向后兼容性

**用户故事**: 作为开发者，我希望新的数据库系统能够与现有代码共存，以便逐步迁移。

#### 验收标准

1. THE System SHALL maintain the existing Op Code definitions for network messages
2. THE System SHALL support both old dictionary-based and new database-based lookups during transition
3. THE System SHALL provide migration utilities to convert existing loot box data to database format
4. THE System SHALL not break existing save files or network protocols
5. THE System SHALL allow feature flags to enable/disable database mode for testing

### 需求 10: 错误处理和恢复

**用户故事**: 作为玩家，我希望即使出现网络错误，游戏也能优雅地恢复。

#### 验收标准

1. WHEN a network request times out, THE System SHALL retry the operation up to 3 times with exponential backoff
2. IF a loot box cannot be found on the host, THEN THE System SHALL close the loot UI and notify the player
3. WHEN a validation error occurs, THE System SHALL log the error and display a user-friendly message
4. IF an operation fails, THEN THE System SHALL not modify the client's UI state
5. THE System SHALL log all errors with timestamps and context for debugging
