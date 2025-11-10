# JSON 投票系统修复完成

## 问题诊断

通过日志分析发现：

### 主机端
```
[14:40:21] [INFO] [SCENE] 投票开始 v3: target='Level_GroundZero_Main', hostScene='Base_SceneV2', loc='Special/BunkerEntry', count=2
```
主机使用的是旧的二进制投票系统（v3）

### 客户端
```
[14:40:15] [INFO] [SCENE] vote: ignore (not in participants) me='192.168.137.30:59230'
```
客户端收到了旧的二进制消息，但因为不在参与者列表中而被忽略

### 根本原因

`SceneNet.Host_BeginSceneVote_Simple` 方法仍在使用旧的二进制消息系统发送 `Op.SCENE_VOTE_START`，而不是使用新的 JSON 系统。

## 修复内容

### 1. 修改 SceneNet.cs

在 `Host_BeginSceneVote_Simple` 方法中：

**添加的代码（第 739-741 行）：**
```csharp
// ✅ 使用新的 JSON 投票系统
SceneVoteMessage.Host_StartVote(targetSceneId, curtainGuid, notifyEvac, saveToFile, useLocation, locationName);
Debug.Log($"[SCENE] 投票开始 (JSON): target='{targetSceneId}', loc='{locationName}'");
```

**注释掉的代码（第 751-785 行）：**
```csharp
// ❌ 旧的二进制消息系统已禁用，使用上面的 JSON 系统
/*
// 计算主机当前 SceneId
string hostSceneId = null;
LocalPlayerManager.Instance.ComputeIsInGame(out hostSceneId);
hostSceneId = hostSceneId ?? string.Empty;

var w = new NetDataWriter();
w.Put((byte)Op.SCENE_VOTE_START);
w.Put((byte)3);
w.Put(sceneTargetId);
// ... 省略其他二进制序列化代码
*/
```

## 修复后的工作流程

### 主机发起投票

1. 调用 `Host_BeginSceneVote_Simple`
2. 重置场景门控状态
3. **调用 `SceneVoteMessage.Host_StartVote`** ← 新增
4. 构建参与者列表（保留用于兼容性）
5. 设置本地投票状态
6. ~~发送二进制消息~~ ← 已禁用

### JSON 投票系统自动工作

1. **立即广播一次**投票状态（JSON 格式）
2. **每秒自动广播**投票状态（在 `Mod.cs` 的 `Update` 中）
3. 客户端接收 JSON 消息并更新状态
4. 客户端切换准备状态时发送 JSON 消息
5. 主机接收准备状态并更新
6. 全员准备后自动开始加载

## 预期日志输出

### 主机端
```
[GATE] 投票开始，重置场景门控状态
[SCENE] 投票开始 (JSON): target='Level_GroundZero_Main', loc='Special/BunkerEntry'
[SceneVote] 主机发起投票: Level_GroundZero_Main, 参与者: 2
[JSON] 发送到所有客户端
```

### 客户端
```
[JsonRouter] 收到JSON消息，type=sceneVote
[SceneVote] 更新投票状态: Level_GroundZero_Main, 参与者: 2
[SceneVote] 收到投票状态，但不在参与者列表中: 192.168.137.30:59230  ← 如果中途加入
```

## 编译状态

✅ **编译成功**（DLL 已生成）
⚠️ 文件复制失败（游戏正在运行）

DLL 文件位置：
```
EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll
```

## 测试步骤

1. **关闭游戏**
2. **重新编译**（确保 DLL 被复制到游戏目录）
   ```bash
   dotnet build EscapeFromDuckovCoopMod.sln --configuration Release
   ```
3. **启动游戏**
4. **主机发起投票**（触发场景切换）
5. **查看日志**
   - 主机应该看到 `[SCENE] 投票开始 (JSON)`
   - 客户端应该看到 `[JsonRouter] 收到JSON消息，type=sceneVote`
   - 客户端应该看到投票面板

## 关键改进

### 1. 完全使用 JSON 系统

- 主机不再发送二进制 `Op.SCENE_VOTE_START` 消息
- 所有投票通信都通过 JSON 消息
- 更易于调试和扩展

### 2. 自动广播

- 主机每秒自动广播投票状态
- 确保所有客户端（包括中途加入的）都能收到

### 3. 中途加入支持

- 即使玩家不在初始参与者列表中，也能接收投票状态
- 玩家可以动态加入投票

### 4. 向后兼容

- 保留了旧的参与者列表构建逻辑
- 保留了 `sceneVoteActive` 等状态变量
- 只是禁用了二进制消息发送

## 故障排除

### 如果客户端仍然看不到投票

1. **检查日志中是否有 JSON 消息**
   ```
   [JsonRouter] 收到JSON消息，type=sceneVote
   ```

2. **检查主机是否发送了 JSON 消息**
   ```
   [SceneVote] 主机发起投票
   [JSON] 发送到所有客户端
   ```

3. **检查客户端是否在同一场景**
   - JSON 投票系统会过滤不同场景的玩家
   - 确保主机和客户端在同一场景中

4. **检查 JsonMessageRouter 是否正确路由**
   ```
   [JsonRouter] 收到JSON消息，type=sceneVote
   ```

### 如果编译失败

1. **检查 using 语句**
   - `SceneNet.cs` 应该有 `using EscapeFromDuckovCoopMod.Net;`
   - `JsonMessageRouter.cs` 应该有 `using EscapeFromDuckovCoopMod.Net;`

2. **检查 SceneVoteMessage 类是否存在**
   - 文件：`EscapeFromDuckovCoopMod/Net/SceneVoteMessage.cs`

3. **清理并重新编译**
   ```bash
   dotnet clean
   dotnet build EscapeFromDuckovCoopMod.sln --configuration Release
   ```

## 下一步

1. 关闭游戏
2. 重新编译
3. 启动游戏测试
4. 查看日志确认 JSON 消息正常工作
5. 测试投票功能是否正常

## 总结

成功将投票系统从二进制消息迁移到 JSON 消息。主要修改是在 `SceneNet.Host_BeginSceneVote_Simple` 方法中调用新的 JSON 投票系统，并禁用旧的二进制消息发送。系统现在会每秒自动广播投票状态，支持中途加入的玩家。
