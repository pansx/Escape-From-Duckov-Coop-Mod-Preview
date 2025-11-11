# LootErrorHandler 使用指南

## 概述

`LootErrorHandler` 是战利品箱系统的错误处理器，负责处理各种错误情况，包括网络超时重试、错误日志记录和用户通知。

## 错误类型

```csharp
public enum LootErrorType
{
    LootBoxNotFound,      // 战利品箱不存在（主机端）
    InvalidSetId,         // 无效的 SetId
    CapacityExceeded,     // 容量超限
    ItemNotFound,         // 物品不存在
    InvalidPosition,      // 无效的位置
    ValidationFailed,     // 验证失败（如物品快照无效）
    NetworkTimeout        // 网络超时
}
```

## 基本使用

### 1. 创建错误处理器实例

```csharp
// 在 LootBoxSyncManager 中创建
private readonly LootErrorHandler _errorHandler = new LootErrorHandler();
```

### 2. 处理错误

```csharp
// 示例：处理战利品箱不存在的错误
if (entity == null)
{
    _errorHandler.HandleError(
        LootErrorType.LootBoxNotFound,
        setId,
        $"Requested by peer {peer.EndPoint}"
    );
    
    // 发送错误响应给客户端
    var errorResponse = new LootOperationResponse(
        success: false,
        token: request.token,
        errorMessage: "LootBoxNotFound"
    );
    JsonMessageRouter.Send(peer, errorResponse);
    return;
}
```

### 3. 网络超时重试

```csharp
// 示例：客户端请求超时后重试
public async void Client_RequestOpen(string setId)
{
    try
    {
        // 发送请求
        var request = new LootOpenRequest(setId);
        JsonMessageRouter.Send(NetService.Instance.connectedPeer, request);
        
        // 等待响应（带超时）
        var response = await WaitForResponse(setId, timeout: 5000);
        
        if (response == null)
        {
            // 超时，触发重试
            _errorHandler.HandleError(
                LootErrorType.NetworkTimeout,
                setId,
                "Request timeout after 5 seconds"
            );
            
            // 重试请求
            await _errorHandler.RetryRequest(setId, () => Client_RequestOpen(setId));
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"[LOOT] Request failed: {ex.Message}");
    }
}
```

### 4. 操作成功后重置重试计数器

```csharp
// 示例：收到成功响应后重置
public void Client_HandleStateResponse(LootStateResponse response)
{
    // 重置重试计数器
    _errorHandler.ResetRetryCounter(response.setId);
    
    // 处理响应...
    _currentLootBox = response;
    ApplyLootboxState(response);
}
```

## 完整示例

### 主机端：处理打开请求

```csharp
public void Host_HandleOpenRequest(NetPeer peer, LootOpenRequest request)
{
    Debug.Log($"[LOOT][Host] Received OPEN request from {peer.EndPoint} for SetId={request.setId}", "LootHost");
    
    // 查询数据库
    var entity = _hostDatabase.GetLootBox(request.setId);
    
    // 验证战利品箱是否存在
    if (entity == null)
    {
        _errorHandler.HandleError(
            LootErrorType.LootBoxNotFound,
            request.setId,
            $"Requested by peer {peer.EndPoint}"
        );
        
        // 发送错误响应
        var errorResponse = new LootOperationResponse(
            success: false,
            token: 0,
            errorMessage: "LootBoxNotFound"
        );
        JsonMessageRouter.Send(peer, errorResponse);
        return;
    }
    
    // 创建响应
    var response = new LootStateResponse(
        request.setId,
        entity.Capacity,
        entity.Items
    );
    
    // 发送响应
    JsonMessageRouter.Send(peer, response);
    
    Debug.Log($"[LOOT][Host] Sent STATE response for SetId={request.setId}, items={entity.Items.Count}", "LootHost");
}
```

### 客户端：处理操作响应

```csharp
public void Client_HandleOperationResponse(LootOperationResponse response)
{
    Debug.Log($"[LOOT][Client] Received operation response: success={response.success}, token={response.token}", "LootClient");
    
    // 检查操作是否成功
    if (!response.success)
    {
        // 解析错误类型
        var errorType = ParseErrorType(response.errorMessage);
        
        // 处理错误
        _errorHandler.HandleError(
            errorType,
            _currentLootBox?.setId ?? "unknown",
            response.errorMessage
        );
        
        // 如果是网络超时，可以选择重试
        if (errorType == LootErrorType.NetworkTimeout)
        {
            // 重试逻辑已经在 HandleError 中触发
        }
        
        return;
    }
    
    // 重置重试计数器
    _errorHandler.ResetRetryCounter(_currentLootBox?.setId ?? "unknown");
    
    // 处理成功响应...
    if (_pendingPutItems.TryGetValue(response.token, out var item))
    {
        // PUT 操作成功
        item.Detach();
        Destroy(item.gameObject);
        _pendingPutItems.Remove(response.token);
    }
    else if (_pendingTakeDestinations.TryGetValue(response.token, out var destInfo))
    {
        // TAKE 操作成功
        var newItem = ItemTool.BuildItemFromSnapshot(response.resultItem);
        // 添加到目标位置...
        _pendingTakeDestinations.Remove(response.token);
    }
}

private LootErrorType ParseErrorType(string errorMessage)
{
    return errorMessage switch
    {
        "LootBoxNotFound" => LootErrorType.LootBoxNotFound,
        "CapacityExceeded" => LootErrorType.CapacityExceeded,
        "ItemNotFound" => LootErrorType.ItemNotFound,
        "InvalidPosition" => LootErrorType.InvalidPosition,
        "ValidationFailed" => LootErrorType.ValidationFailed,
        "NetworkTimeout" => LootErrorType.NetworkTimeout,
        _ => LootErrorType.ValidationFailed
    };
}
```

## 日志输出示例

### 正常操作

```
[LOOT][Host] Received OPEN request from 192.168.1.100:5000 for SetId=loot_001
[LOOT][Host] Sent STATE response for SetId=loot_001, items=15
[LOOT][Client] Received STATE response for SetId=loot_001
```

### 错误处理

```
[LOOT][Host] Received OPEN request from 192.168.1.100:5000 for SetId=loot_999
[LOOT][ERROR] Timestamp=2025-11-11 07:30:15.123, ErrorType=LootBoxNotFound, SetId=loot_999, Details=Requested by peer 192.168.1.100:5000
[LOOT][ERROR] LootBox not found: SetId=loot_999
[LOOT][NOTIFY] 战利品箱已不存在或已被移除
```

### 网络超时重试

```
[LOOT][Client] Request timeout for SetId=loot_001
[LOOT][ERROR] Timestamp=2025-11-11 07:30:20.456, ErrorType=NetworkTimeout, SetId=loot_001, Details=Request timeout after 5 seconds
[LOOT][RETRY] Retrying request for SetId=loot_001, attempt 1/3, delay=100ms
[LOOT][Client] Received STATE response for SetId=loot_001
[LOOT][RETRY] Reset retry counter for SetId=loot_001
```

## 最佳实践

1. **总是记录错误上下文**
   - 包含 SetId、操作类型、请求者信息等

2. **区分错误严重程度**
   - 使用 `Debug.LogWarning` 处理可恢复的错误
   - 使用 `Debug.LogError` 处理严重错误

3. **操作成功后重置重试计数器**
   - 避免累积重试次数影响后续操作

4. **定期清理过期的重试记录**
   ```csharp
   // 在 Update 或定时器中调用
   _errorHandler.CleanupExpiredRetries(maxAge: 300); // 5 分钟
   ```

5. **提供友好的用户提示**
   - 错误消息应该简洁明了
   - 避免暴露技术细节给玩家

## 注意事项

1. **重试机制**
   - 最多重试 3 次
   - 使用指数退避（100ms, 200ms, 400ms）
   - 达到最大重试次数后会通知用户

2. **线程安全**
   - `RetryRequest` 使用 `async/await`，注意在 Unity 主线程调用

3. **内存管理**
   - 定期调用 `CleanupExpiredRetries` 清理过期记录
   - 避免内存泄漏

4. **UI 集成**
   - 当前 `NotifyPlayer` 使用 Debug.Log
   - 未来应该集成到游戏的 UI 提示系统

## 扩展

### 自定义错误处理

```csharp
// 继承 LootErrorHandler 并重写方法
public class CustomLootErrorHandler : LootErrorHandler
{
    protected override void NotifyPlayer(string message)
    {
        // 使用游戏的 UI 系统
        UIManager.ShowNotification(message, NotificationType.Error);
    }
}
```

### 添加统计功能

```csharp
// 在 LootErrorHandler 中添加
private int _totalErrors = 0;
private Dictionary<LootErrorType, int> _errorCounts = new Dictionary<LootErrorType, int>();

public void HandleError(LootErrorType errorType, string setId, string details)
{
    _totalErrors++;
    if (!_errorCounts.ContainsKey(errorType))
        _errorCounts[errorType] = 0;
    _errorCounts[errorType]++;
    
    // 原有逻辑...
}

public Dictionary<LootErrorType, int> GetErrorStatistics()
{
    return new Dictionary<LootErrorType, int>(_errorCounts);
}
```

---

*最后更新: 2025-11-11*
