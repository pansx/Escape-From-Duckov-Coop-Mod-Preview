# JSON 投票系统实现

## 概述

将场景投票系统从二进制消息改为 JSON 消息，支持每秒广播和中途加入玩家。

## 实现的功能

### 1. 核心特性

- ✅ **JSON 消息格式**：投票状态使用 JSON 格式传输，易于调试和扩展
- ✅ **每秒广播**：主机每秒自动广播投票状态给所有客户端
- ✅ **中途加入支持**：即使玩家不在初始参与者列表中，也能接收和参与投票
- ✅ **状态同步**：所有玩家的准备状态实时同步
- ✅ **自动检测全员准备**：当所有玩家都准备好时，自动开始加载场景

### 2. 消息类型

#### sceneVote - 投票状态广播（主机 → 所有客户端）

```json
{
  "type": "sceneVote",
  "active": true,
  "targetSceneId": "Level_1",
  "curtainGuid": "...",
  "locationName": "...",
  "notifyEvac": false,
  "saveToFile": true,
  "useLocation": false,
  "hostSceneId": "Level_0",
  "players": [
    {
      "playerId": "192.168.1.100:9050",
      "playerName": "Host",
      "ready": false
    },
    {
      "playerId": "192.168.1.101:54321",
      "playerName": "Player1",
      "ready": true
    }
  ],
  "timestamp": "2025-01-08 12:34:56.789"
}
```

#### sceneVoteRequest - 请求发起投票（客户端 → 主机）

```json
{
  "type": "sceneVoteRequest",
  "targetSceneId": "Level_1",
  "curtainGuid": "...",
  "locationName": "...",
  "notifyEvac": false,
  "saveToFile": true,
  "useLocation": false,
  "timestamp": "2025-01-08 12:34:56.789"
}
```

#### sceneVoteReady - 切换准备状态（客户端 → 主机）

```json
{
  "type": "sceneVoteReady",
  "playerId": "192.168.1.101:54321",
  "ready": true,
  "timestamp": "2025-01-08 12:34:56.789"
}
```

## 文件结构

### 新增文件

1. **EscapeFromDuckovCoopMod/Net/SceneVoteMessage.cs**
   - 投票系统的核心实现
   - 包含所有投票相关的数据结构和方法
   - 主机端：发起投票、广播状态、处理准备切换
   - 客户端：接收状态、切换准备、请求投票

2. **EscapeFromDuckovCoopMod/Main/SceneService/SceneNet_VoteHelper.cs**
   - SceneNet 的辅助类
   - 提供便捷的 JSON 投票方法
   - 简化调用接口

### 修改文件

1. **EscapeFromDuckovCoopMod/Net/JsonMessageRouter.cs**
   - 添加了三种新的 JSON 消息类型路由
   - `sceneVote` → `SceneVoteMessage.Client_HandleVoteState`
   - `sceneVoteRequest` → `SceneVoteMessage.Host_HandleVoteRequest`
   - `sceneVoteReady` → `SceneVoteMessage.Host_HandleReadyToggle`

2. **EscapeFromDuckovCoopMod/Main/Loader/Mod.cs**
   - 修复了 switch 语句结构错误（default 分支位置）
   - 更新了投票准备切换逻辑，使用新的 JSON 系统
   - 添加了主机端定期广播投票状态的逻辑

## 使用方法

### 主机发起投票

```csharp
// 方法 1：直接使用 SceneVoteMessage
SceneVoteMessage.Host_StartVote(
    targetSceneId: "Level_1",
    curtainGuid: null,
    notifyEvac: false,
    saveToFile: true,
    useLocation: false,
    locationName: ""
);

// 方法 2：使用辅助类
SceneNetVoteHelper.Host_StartJsonVote(
    targetSceneId: "Level_1",
    curtainGuid: null,
    notifyEvac: false,
    saveToFile: true,
    useLocation: false,
    locationName: ""
);
```

### 客户端请求发起投票

```csharp
SceneVoteMessage.Client_RequestVote(
    targetSceneId: "Level_1",
    curtainGuid: null,
    notifyEvac: false,
    saveToFile: true,
    useLocation: false,
    locationName: ""
);
```

### 切换准备状态

```csharp
// 主机
var myId = NetService.Instance.GetPlayerId(null);
SceneVoteMessage.Host_HandleReadyToggle(myId, true);

// 客户端
SceneVoteMessage.Client_ToggleReady(true);
```

## 工作流程

### 主机端

1. **发起投票**
   - 调用 `Host_StartVote` 方法
   - 构建参与者列表（包括主机和所有客户端）
   - 创建投票状态缓存
   - 立即广播一次投票状态

2. **定期广播**
   - 在 `Update` 方法中每秒调用 `Host_Update`
   - 自动广播当前投票状态给所有客户端
   - 更新时间戳

3. **处理准备切换**
   - 接收客户端的准备状态切换请求
   - 更新玩家列表中的准备状态
   - 如果玩家不在列表中，自动添加（支持中途加入）
   - 立即广播更新后的状态
   - 检查是否全员准备，如果是则开始加载场景

4. **开始加载**
   - 全员准备后自动触发
   - 调用原有的场景加载逻辑
   - 清除投票状态

### 客户端端

1. **接收投票状态**
   - 通过 JSON 消息接收投票状态
   - 检查是否在参与者列表中（不在也可以参与）
   - 检查场景是否匹配（不同场景忽略）
   - 更新本地投票状态和准备状态

2. **切换准备**
   - 按 J 键切换准备状态
   - 发送 JSON 消息给主机
   - 本地乐观更新状态

3. **请求发起投票**
   - 发送投票请求给主机
   - 主机收到后发起投票

## 优势

### 相比原有二进制系统

1. **易于调试**
   - JSON 格式可读性强
   - 可以直接查看日志中的消息内容
   - 便于排查问题

2. **易于扩展**
   - 添加新字段不需要修改序列化逻辑
   - 向后兼容性更好

3. **中途加入支持**
   - 玩家列表动态更新
   - 即使不在初始列表中也能参与

4. **状态同步更可靠**
   - 每秒广播确保所有客户端状态一致
   - 网络波动时自动恢复

5. **代码更简洁**
   - 不需要手动序列化/反序列化
   - Unity 的 JsonUtility 自动处理

## 注意事项

### 1. 性能考虑

- 每秒广播一次，带宽消耗较小（约 1-2KB/秒）
- 玩家列表较大时（>10人）可能需要优化
- 可以考虑只在状态变化时广播

### 2. 兼容性

- 新系统与旧系统并存
- 旧的 `Op.SCENE_VOTE_START` 等消息仍然保留
- 可以逐步迁移到新系统

### 3. 中途加入

- 中途加入的玩家会自动添加到列表
- 但需要确保他们在同一场景中
- 场景不匹配的玩家会忽略投票

### 4. 全员准备检测

- 只检查列表中的玩家
- 中途加入的玩家也会被计入
- 确保所有玩家都准备后才开始加载

## 测试建议

### 1. 基本功能测试

- [ ] 主机发起投票，客户端能看到投票面板
- [ ] 客户端切换准备状态，主机能收到
- [ ] 全员准备后自动开始加载场景
- [ ] 投票期间按 J 键能切换准备状态

### 2. 中途加入测试

- [ ] 投票开始后，新玩家加入
- [ ] 新玩家能看到投票面板
- [ ] 新玩家能切换准备状态
- [ ] 新玩家准备后，全员准备检测正常工作

### 3. 网络测试

- [ ] 网络延迟情况下，状态同步正常
- [ ] 网络中断后重连，能恢复投票状态
- [ ] 多个玩家同时切换准备状态，不会冲突

### 4. 场景过滤测试

- [ ] 不同场景的玩家不会收到投票通知
- [ ] 同场景的玩家能正常参与投票

## 后续优化

### 1. 性能优化

- 只在状态变化时广播，而不是每秒广播
- 使用增量更新，只发送变化的部分
- 压缩 JSON 数据

### 2. 功能增强

- 添加投票倒计时
- 添加投票取消功能
- 添加投票历史记录
- 支持多个投票同时进行

### 3. UI 改进

- 显示每个玩家的准备状态
- 显示投票进度条
- 添加音效和动画

## 编译状态

✅ **编译成功**（无错误，仅有警告）

警告主要是：
- nullable 引用类型注释（可忽略）
- 未使用的字段（可忽略）
- 无法访问的代码（可忽略）

## 总结

成功实现了基于 JSON 的投票系统，支持每秒广播和中途加入。系统已编译通过，可以进行测试。关键改进包括：

1. 修复了 switch 语句结构错误，恢复了投票功能
2. 实现了 JSON 投票系统，提供更好的可维护性
3. 支持中途加入玩家，提高了灵活性
4. 每秒自动广播，确保状态同步

下一步建议关闭游戏后重新编译，然后进行完整的功能测试。
