# 需求文档 - 进图全量同步

## 简介

当前游戏存在进图后不同步的问题，玩家之间看不到对方，怪物和掉落物状态不一致。本功能通过JSON消息实现进图时的全量同步机制，确保所有客户端的游戏状态一致。

## 术语表

- **System**: 指"全量同步系统"，负责在场景加载后同步所有游戏实体
- **Host**: 指游戏主机端，拥有游戏状态的权威数据
- **Client**: 指游戏客户端，需要从主机同步游戏状态
- **AI Entity**: 指游戏中的AI角色（怪物、NPC等）
- **Loot Entity**: 指游戏中的掉落物品
- **Scene Load**: 指玩家进入新场景或地图的事件
- **Full Sync**: 指完整同步所有游戏实体的状态数据

## 需求

### 需求 1：场景加载和同步触发

**用户故事**：作为系统，我需要在客户端收到SCENE_GATE_RELEASE后拦截淡入流程并触发全量同步，以便在进入游戏前完成数据同步

#### 验收标准

1. WHEN 客户端接收到SCENE_GATE_RELEASE消息，THE System SHALL 拦截淡入流程
2. WHEN 客户端拦截淡入流程，THE System SHALL 向主机发送同步请求
3. WHEN 主机接收到同步请求，THE System SHALL 发送清空指令给客户端
4. WHEN 客户端同步完成，THE System SHALL 继续执行淡入流程并进入游戏
5. THE System SHALL 复用现有的SCENE_GATE机制，无需修改现有流程

### 需求 2：客户端清空操作

**用户故事**：作为客户端，我需要在接收全量同步前清空本地的AI和掉落物，以便避免重复和冲突

#### 验收标准

1. WHEN 客户端接收到清空指令，THE System SHALL 销毁所有本地AI实体
2. WHEN 客户端接收到清空指令，THE System SHALL 销毁所有本地掉落物实体
3. THE System SHALL 保留本地玩家角色和远程玩家角色
4. THE System SHALL 在清空完成后向主机发送确认消息
5. THE System SHALL 记录清空操作的日志，包括清空的实体数量

### 需求 3：主机数据收集

**用户故事**：作为主机，我需要收集所有AI和掉落物的完整状态数据，以便发送给客户端

#### 验收标准

1. WHEN 主机收到客户端清空确认，THE System SHALL 遍历场景中所有AI实体
2. WHEN 主机收集AI数据，THE System SHALL 包含AI的ID、位置、旋转、生命值、装备、动画状态
3. WHEN 主机遍历掉落物，THE System SHALL 包含掉落物的ID、位置、旋转、物品类型、数量
4. THE System SHALL 将收集的数据序列化为JSON数组格式
5. THE System SHALL 在数据收集失败时记录错误日志并重试最多3次

### 需求 4：全量同步数据传输

**用户故事**：作为主机，我需要将收集的数据通过可靠的方式发送给客户端，以便确保数据完整到达

#### 验收标准

1. WHEN 主机准备好同步数据，THE System SHALL 使用ReliableOrdered传输方式发送
2. WHEN 数据包大小超过1MB，THE System SHALL 将数据分包发送
3. THE System SHALL 为每个数据包添加序列号和总包数信息
4. WHEN 客户端接收到所有分包，THE System SHALL 重组完整数据
5. THE System SHALL 在传输超时（10秒）后重新发送

### 需求 5：客户端数据应用

**用户故事**：作为客户端，我需要根据接收到的数据重建所有AI和掉落物，以便与主机状态保持一致

#### 验收标准

1. WHEN 客户端接收到AI同步数据，THE System SHALL 使用相同的种子和ID生成AI实体
2. WHEN 客户端生成AI，THE System SHALL 应用位置、旋转、生命值、装备、动画状态
3. WHEN 客户端接收到掉落物数据，THE System SHALL 生成对应的掉落物实体
4. WHEN 客户端生成掉落物，THE System SHALL 应用位置、旋转、物品类型、数量
5. THE System SHALL 在所有实体生成完成后向主机发送同步完成确认
6. THE System SHALL 在应用数据失败时记录错误并跳过该实体

### 需求 6：同步状态验证

**用户故事**：作为系统，我需要验证同步完成后的状态一致性，以便确保同步成功

#### 验收标准

1. WHEN 客户端完成同步，THE System SHALL 统计本地AI和掉落物数量
2. WHEN 客户端发送同步完成确认，THE System SHALL 包含实体数量信息
3. WHEN 主机接收到确认，THE System SHALL 对比客户端和主机的实体数量
4. IF 实体数量不匹配，THEN THE System SHALL 记录警告日志并触发增量同步
5. THE System SHALL 在同步完成后解除AI冻结状态

### 需求 7：错误处理和重试

**用户故事**：作为系统，我需要处理同步过程中的各种错误情况，以便提高同步成功率

#### 验收标准

1. WHEN 客户端清空操作超时（5秒），THE System SHALL 记录错误并继续同步流程
2. WHEN 数据传输失败，THE System SHALL 重试最多3次
3. WHEN 客户端应用数据失败，THE System SHALL 跳过失败的实体并继续处理
4. WHEN 同步流程总时间超过30秒，THE System SHALL 中止并记录超时错误
5. THE System SHALL 为每个错误情况提供清晰的日志信息

### 需求 8：性能优化

**用户故事**：作为系统，我需要优化同步性能，以便减少同步时间和网络开销

#### 验收标准

1. THE System SHALL 使用数据压缩减少传输大小至少30%
2. THE System SHALL 批量生成实体，每批最多50个，避免单帧卡顿
3. THE System SHALL 优先同步可见范围内的实体
4. THE System SHALL 使用对象池复用网络数据包，避免GC压力
5. THE System SHALL 在同步期间显示进度提示给玩家
