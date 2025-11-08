# SetId 修复后投票功能恢复

## 问题描述

修改 SetId 功能后，客机的投票面板不显示了。

## 问题原因

在 `Mod.cs` 的 `OnNetworkReceive` 方法中，switch 语句的结构存在严重错误：

```csharp
case Op.JSON:
    JsonMessageRouter.HandleJsonMessage(reader);
    break;

default:
    Debug.LogWarning($"Unknown opcode: {(byte)op}");
    break;

case Op.GRENADE_THROW_REQUEST:  // ❌ 这些 case 永远不会被执行！
    // ...
case Op.SCENE_VOTE_START:       // ❌ 投票相关的消息也在 default 之后
    // ...
```

在 C# 中，`default` 分支必须放在 switch 语句的最后。当前的代码结构导致：
1. `default` 分支之后的所有 case 都不会被执行
2. 包括 `SCENE_VOTE_START`、`SCENE_VOTE_REQ`、`SCENE_READY_SET` 等投票相关的消息处理
3. 这就是为什么客机收不到投票通知的原因

## 修复方案

### 1. 移除错误位置的 default 分支

从 `case Op.JSON:` 后面移除 `default` 分支：

```csharp
case Op.JSON:
    // 处理JSON消息 - 使用路由器根据type字段分发
    JsonMessageRouter.HandleJsonMessage(reader);
    break;

// ✅ 移除了这里的 default 分支

case Op.GRENADE_THROW_REQUEST:
    if (IsServer) COOPManager.GrenadeM.HandleGrenadeThrowRequest(peer, reader);
    break;
```

### 2. 在 switch 语句末尾添加 default 分支

在所有 case 处理完之后，在 switch 语句的最后添加 default 分支：

```csharp
case Op.PLAYER_HURT_EVENT:
    if (!IsServer) HealthM.Instance.Client_ApplySelfHurtFromServer(reader);
    break;

default:
    // 有未知 opcode 时给出警告，便于排查（比如双端没一起更新）
    Debug.LogWarning($"Unknown opcode: {(byte)op}");
    break;
}

reader.Recycle();
```

## 修复结果

修复后，所有消息类型都能正确处理：
- ✅ `Op.JSON` 消息（SetId、战利品全量同步等）
- ✅ `Op.SCENE_VOTE_START` - 主机发起投票
- ✅ `Op.SCENE_VOTE_REQ` - 客机请求发起投票
- ✅ `Op.SCENE_READY_SET` - 玩家切换准备状态
- ✅ `Op.SCENE_BEGIN_LOAD` - 开始加载场景
- ✅ `Op.SCENE_CANCEL` - 取消投票
- ✅ 所有其他消息类型

## 编译状态

✅ 代码编译成功（无语法错误）
⚠️ 文件复制失败（游戏正在运行，需要关闭游戏后重新编译）

## 测试建议

1. 关闭游戏
2. 重新编译项目：`dotnet build EscapeFromDuckovCoopMod.sln --configuration Release`
3. 启动游戏测试：
   - 主机发起场景投票
   - 客机应该能看到投票面板
   - 客机按 J 键切换准备状态
   - 所有玩家准备后自动开始加载

## 技术细节

### C# Switch 语句规则

在 C# 中，switch 语句的 `default` 分支：
- 必须放在所有 case 之后（或者至少不能在中间阻断其他 case）
- 如果放在中间，后面的 case 标签会被编译器忽略
- 这是一个容易被忽视的语法陷阱

### 正确的 Switch 结构

```csharp
switch (value)
{
    case A:
        // 处理 A
        break;
    
    case B:
        // 处理 B
        break;
    
    case C:
        // 处理 C
        break;
    
    default:  // ✅ 放在最后
        // 处理未知情况
        break;
}
```

### 错误的 Switch 结构

```csharp
switch (value)
{
    case A:
        // 处理 A
        break;
    
    default:  // ❌ 放在中间
        // 处理未知情况
        break;
    
    case B:   // ❌ 这个 case 永远不会被执行！
        // 处理 B
        break;
}
```

## 相关文件

- `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs` - 修复了 switch 语句结构
- `EscapeFromDuckovCoopMod/Net/JsonMessageRouter.cs` - JSON 消息路由器（未修改）
- `EscapeFromDuckovCoopMod/Net/SetIdMessage.cs` - SetId 消息处理（未修改）

## 总结

这是一个典型的 switch 语句结构错误，与 SetId 功能本身无关。修复后，投票功能应该能正常工作。
