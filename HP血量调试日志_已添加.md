# HP 血量调试日志 - 已添加

## 修改概述

已在所有关键的血量同步位置添加 JSON 格式的调试日志，用于追踪客机进图后血量变少的问题。

## 已添加的日志位置

### 1. 客户端初始血量上报

**文件**: `EscapeFromDuckovCoopMod/Main/Health/HealthM.cs`
**方法**: `Client_ReportSelfHealth_IfReadyOnce()`

**日志标签**: `[HP_REPORT_INIT]`

**日志内容**:

```json
{
    "event": "Client_ReportSelfHealth_IfReadyOnce",
    "maxHealth": 100.0,
    "currentHealth": 100.0,
    "sceneId": "Base",
    "time": 5.23,
    "isValid": true
}
```

**新增功能**:

-   ✅ 添加血量有效性检查（max > 0 && cur > 0）
-   ✅ 无效血量时输出警告并延迟上报
-   ✅ 成功上报时输出确认日志

### 2. 客户端血量变化上报

**文件**: `EscapeFromDuckovCoopMod/Main/Health/HealthM.cs`
**方法**: `Client_SendSelfHealth()`

**日志标签**: `[HP_REPORT]`

**日志内容**:

```json
{
    "event": "Client_SendSelfHealth",
    "maxHealth": 100.0,
    "currentHealth": 85.3,
    "force": true,
    "time": 20.45,
    "lastMax": 100.0,
    "lastCur": 100.0
}
```

**特点**:

-   仅在 force=true 或值变化时输出日志
-   记录上次发送的值用于对比

### 3. 主机接收血量上报

**文件**: `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs`
**消息**: `Op.PLAYER_HEALTH_REPORT`

**日志标签**: `[HP_RECEIVE]`

**日志内容**:

```json
{
    "event": "Server_ReceiveHealthReport",
    "playerId": "192.168.1.100:12345",
    "maxHealth": 100.0,
    "currentHealth": 100.0,
    "hasRemoteCharacter": true,
    "time": 5.25
}
```

**新增功能**:

-   ✅ 记录玩家 ID 和血量值
-   ✅ 检查远程角色是否已创建
-   ✅ 无效血量时输出警告
-   ✅ 成功应用时输出确认日志
-   ✅ 远程角色未创建时输出警告

### 4. 客户端接收权威血量

**文件**: `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs`
**消息**: `Op.AUTH_HEALTH_SELF`

**日志标签**: `[HP_AUTH_SELF]`

**日志内容**:

```json
{
    "event": "Client_ReceiveAuthHealth",
    "maxHealth": 100.0,
    "currentHealth": 85.3,
    "time": 20.47
}
```

**新增功能**:

-   ✅ 记录收到的权威血量
-   ✅ 无效血量时输出警告
-   ✅ 应用成功时输出确认日志
-   ✅ 防回弹时输出跳过日志

### 5. 防回弹机制

**文件**: `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs`
**消息**: `Op.AUTH_HEALTH_SELF`

**日志标签**: `[HP_BOUNCEBACK]`

**日志内容**:

```json
{
    "event": "AntiBounceback",
    "localCurrent": 85.3,
    "serverCurrent": 90.0,
    "timeSinceHurt": 0.15
}
```

**触发条件**:

-   在受击窗口内（0.3 秒）
-   服务器回显的血量比本地更高

## 编译结果

✅ **编译成功**

```
dotnet build EscapeFromDuckovCoopMod.sln --configuration Release
还原完成(1.7)
EscapeFromDuckovCoopMod 成功，出现 19 警告 (7.8 秒)
→ EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll
```

警告都是无关紧要的（nullable 注释、未使用字段等），不影响功能。

## 如何使用这些日志

### 1. 启动游戏并测试

1. 主机启动游戏并进入场景
2. 客机连接到主机
3. 观察 Unity 日志输出

### 2. 查找日志

在 Unity 日志文件中搜索以下标签：

-   `[HP_REPORT_INIT]` - 客户端初始血量上报
-   `[HP_REPORT]` - 客户端血量变化上报
-   `[HP_RECEIVE]` - 主机接收血量上报
-   `[HP_AUTH_SELF]` - 客户端接收权威血量
-   `[HP_BOUNCEBACK]` - 防回弹机制触发

### 3. 分析日志

使用 JSON 解析工具或直接阅读日志，关注：

-   `maxHealth` 和 `currentHealth` 的值是否正确
-   `isValid` 是否为 true
-   是否有 "⚠️" 警告标记
-   时间戳是否合理

### 4. 常见问题排查

#### 问题 1: 客机进图后血量为 0 或很低

**查找日志**:

```
[HP_REPORT_INIT] {"event":"Client_ReportSelfHealth_IfReadyOnce","maxHealth":0.0,...}
[HP_REPORT_INIT] ⚠️ 血量未初始化，延迟上报: max=0, cur=0
```

**原因**: 客机在角色未完全初始化时就尝试上报血量

**解决**: 日志会显示延迟上报，等待下一帧重试

#### 问题 2: 主机收到无效血量

**查找日志**:

```
[HP_RECEIVE] ⚠️ 收到无效血量，缓存: 玩家=xxx, max=0, cur=0
```

**原因**: 客机上报了无效的血量数据

**解决**: 主机会缓存数据，等待远程角色创建后再应用

#### 问题 3: 血量回弹

**查找日志**:

```
[HP_BOUNCEBACK] {"event":"AntiBounceback","localCurrent":85.3,"serverCurrent":90.0,...}
[HP_AUTH_SELF] ✗ 跳过权威血量（防回弹）
```

**原因**: 防回弹机制检测到陈旧的回显数据

**解决**: 这是正常的保护机制，防止血量错误恢复

## 测试场景

### 场景 1: 正常连接

**预期日志顺序**:

1. `[HP_REPORT_INIT]` - 客机上报初始血量
2. `[HP_RECEIVE]` - 主机接收血量
3. `[HP_RECEIVE] ✓ 应用血量到远程角色`
4. `[HP_AUTH_SELF]` - 客机收到权威血量
5. `[HP_AUTH_SELF] ✓ 应用权威血量`

### 场景 2: 角色未初始化

**预期日志顺序**:

1. `[HP_REPORT_INIT] ⚠️ 血量未初始化，延迟上报`
2. （等待下一帧）
3. `[HP_REPORT_INIT]` - 重试上报
4. `[HP_REPORT_INIT] ✓ 初始血量上报成功`

### 场景 3: 受伤同步

**预期日志顺序**:

1. `[HP_REPORT]` - 客机上报血量变化（force=true）
2. `[HP_RECEIVE]` - 主机接收血量
3. `[HP_AUTH_SELF]` - 客机收到权威血量
4. 可能触发 `[HP_BOUNCEBACK]` - 防回弹检查

## 下一步

1. **运行游戏测试**：启动主机和客机，观察日志输出
2. **收集日志**：使用 `get-logs.bat` 脚本获取完整日志
3. **分析问题**：根据日志中的 JSON 数据定位问题
4. **调整参数**：如果需要，可以调整：
    - 血量有效性检查阈值
    - 防回弹窗口时间（当前 0.3 秒）
    - 上报频率（当前 20Hz）

## 相关文档

-   `客户端HP回报机制分析.md` - 完整的血量同步机制分析
-   `get-logs.bat` - 日志获取脚本
-   `.kiro/steering/log-retrieval.md` - 日志获取方法说明
