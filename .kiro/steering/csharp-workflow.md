---
inclusion: always
---

# C# 项目工作流规范

## 强制要求

### 1. 编译验证

**在写总结之前必须编译项目！**

```bash
dotnet build EscapeFromDuckovCoopMod.sln --configuration Release
```

- ✅ 编译成功后才能写总结
- ❌ 不允许在未编译的情况下声称"完成"
- ⚠️ 如果有编译错误，必须先修复再总结

**编译成功后的部署流程**: 参见 [Mod 部署流程文档](mod-deployment.md)

### 2. 诊断检查

修改代码后使用 `getDiagnostics` 工具检查语法和类型错误：

```
getDiagnostics(["EscapeFromDuckovCoopMod/Main/UI/MModUI.cs"])
```

- 忽略格式化警告（缩进、空格等）
- 关注真正的错误和语义问题

## JSON Debug 调试方法

### 位置

调试输出方法位于：`EscapeFromDuckovCoopMod/Main/UI/MModUI.cs`

方法名：`DebugPrintRemoteCharacters()`

### 如何修改

1. **找到方法**
   - 搜索 `internal void DebugPrintRemoteCharacters()`
   - 该方法约在第 2600 行左右

2. **添加新的调试数据**

```csharp
// 在 debugData 字典中添加新字段
debugData["NewFieldName"] = new Dictionary<string, object>
{
    ["SubField1"] = value1,
    ["SubField2"] = value2
};
```

3. **添加到现有对象的数据**

```csharp
// 在遍历 remoteCharacters 或 clientRemoteCharacters 时
charData["NewProperty"] = someValue;
```

4. **常用的调试信息**

```csharp
// GameObject 信息
charData["GameObjectName"] = go?.name ?? "null";
charData["InstanceId"] = go?.GetInstanceID() ?? 0;
charData["Active"] = go?.activeSelf ?? false;
charData["Position"] = go?.transform.position.ToString() ?? "null";

// 组件检查
charData["HasComponent"] = go?.GetComponent<SomeComponent>() != null;

// 场景路径
var path = "";
var t = go.transform;
while (t != null)
{
    path = t.name + (string.IsNullOrEmpty(path) ? "" : "/" + path);
    t = t.parent;
}
charData["ScenePath"] = path;

// 渲染器状态
var renderers = go.GetComponentsInChildren<Renderer>();
charData["EnabledRenderers"] = renderers.Count(r => r.enabled);
```

### 输出格式

所有数据会被序列化为 JSON 并输出到 Unity 日志：

```csharp
var json = Newtonsoft.Json.JsonConvert.SerializeObject(
    debugData, 
    Newtonsoft.Json.Formatting.Indented
);
Debug.Log(json);
```

### 测试方法

1. 编译项目
2. **部署 DLL** - 使用 API 上传或手动复制（详见 [Mod 部署流程](mod-deployment.md)）
3. 启动游戏
4. 按 `=` 键打开联机面板
5. 点击 "调试信息" 按钮
6. 查看 Unity 日志输出（`%AppData%\..\LocalLow\Duckov\Escape from Duckov\Player.log`）

## 最佳实践

### 添加调试信息时

1. **使用描述性的键名**
   ```csharp
   // ✅ 好
   charData["IsLocalPlayerDuplicate"] = true;
   
   // ❌ 差
   charData["flag"] = true;
   ```

2. **处理空值**
   ```csharp
   // ✅ 好
   charData["Value"] = obj?.property ?? "null";
   
   // ❌ 差
   charData["Value"] = obj.property; // 可能抛出 NullReferenceException
   ```

3. **添加注释标记**
   ```csharp
   // 🔍 新增：检查是否是本地玩家的副本
   charData["IsLocalPlayerDuplicate"] = isLocalPlayerDuplicate;
   ```

4. **分组相关信息**
   ```csharp
   var componentInfo = new Dictionary<string, object>
   {
       ["HasNetInterpolator"] = go.GetComponent<NetInterpolator>() != null,
       ["HasAnimInterpolator"] = go.GetComponent<AnimParamInterpolator>() != null,
       ["HasRemoteReplicaTag"] = go.GetComponent<RemoteReplicaTag>() != null
   };
   charData["Components"] = componentInfo;
   ```

### 修改后的工作流

1. 修改 `DebugPrintRemoteCharacters()` 方法
2. 运行 `getDiagnostics` 检查语法错误
3. 运行 `dotnet build` 编译项目
4. 确认编译成功（无错误）
5. **部署 DLL** - 参见 [Mod 部署流程](mod-deployment.md)
6. 更新相关文档（如 `客户端多余玩家问题分析.md`）
7. 写总结

## 示例：添加新的调试字段

```csharp
// 在 DebugPrintRemoteCharacters() 方法中

// 1. 添加到主数据字典
debugData["TransportMode"] = Service.TransportMode.ToString();

// 2. 添加到本地玩家信息
if (!isServer && Service.connectedPeer != null)
{
    localPlayerData["ConnectedPeerEndPoint"] = Service.connectedPeer.EndPoint?.ToString() ?? "null";
}

// 3. 添加到远程角色信息（客户端）
if (!isServer && Service.clientRemoteCharacters != null)
{
    foreach (var kv in Service.clientRemoteCharacters)
    {
        var playerId = kv.Key;
        var go = kv.Value;
        
        // 检查是否是本地玩家的副本
        var isLocalPlayerDuplicate = false;
        if (Service.connectedPeer != null)
        {
            var myNetworkId = Service.connectedPeer.EndPoint?.ToString();
            isLocalPlayerDuplicate = playerId == myNetworkId;
        }
        charData["IsLocalPlayerDuplicate"] = isLocalPlayerDuplicate;
    }
}
```

## 常见问题

### Q: 为什么要在总结前编译？

A: 确保代码没有语法错误，避免提交无法编译的代码。编译成功是代码质量的基本保证。

### Q: getDiagnostics 显示很多警告怎么办？

A: 格式化警告（缩进、空格）可以忽略。只关注真正的错误（Error）和语义问题。

### Q: 如何快速定位 DebugPrintRemoteCharacters 方法？

A: 使用 `grepSearch` 工具：
```
grepSearch("DebugPrintRemoteCharacters", includePattern="*.cs")
```

### Q: JSON 输出太长怎么办？

A: 可以添加条件过滤，只输出关键信息，或者分批输出不同的数据集。
