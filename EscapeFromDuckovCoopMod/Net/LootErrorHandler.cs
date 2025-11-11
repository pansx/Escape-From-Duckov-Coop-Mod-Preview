// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team
//
// This program is not a free software.
// It's distributed under a license based on AGPL-3.0,
// with strict additional restrictions:
//  YOU MUST NOT use this software for commercial purposes.
//  YOU MUST NOT use this software to run a headless game server.
//  YOU MUST include a conspicuous notice of attribution to
//  Mr-sans-and-InitLoader-s-team/Escape-From-Duckov-Coop-Mod-Preview as the original author.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EscapeFromDuckovCoopMod.Utils.Logger.Core;

namespace EscapeFromDuckovCoopMod.Net;

/// <summary>
/// 战利品箱错误类型枚举
/// </summary>
public enum LootErrorType
{
    /// <summary>
    /// 战利品箱不存在（主机端）
    /// </summary>
    LootBoxNotFound,
    
    /// <summary>
    /// 无效的 SetId
    /// </summary>
    InvalidSetId,
    
    /// <summary>
    /// 容量超限
    /// </summary>
    CapacityExceeded,
    
    /// <summary>
    /// 物品不存在
    /// </summary>
    ItemNotFound,
    
    /// <summary>
    /// 无效的位置
    /// </summary>
    InvalidPosition,
    
    /// <summary>
    /// 验证失败（如物品快照无效）
    /// </summary>
    ValidationFailed,
    
    /// <summary>
    /// 网络超时
    /// </summary>
    NetworkTimeout
}

/// <summary>
/// 战利品箱错误处理器
/// 负责处理战利品箱操作中的各种错误情况
/// </summary>
public class LootErrorHandler
{
    // 重试配置
    private const int MaxRetryCount = 3;
    private static readonly int[] RetryDelaysMs = { 100, 200, 400 }; // 指数退避
    
    // 重试状态跟踪
    private readonly Dictionary<string, int> _retryCounters = new Dictionary<string, int>();
    private readonly Dictionary<string, DateTime> _lastRetryTime = new Dictionary<string, DateTime>();
    
    /// <summary>
    /// 处理错误
    /// </summary>
    /// <param name="errorType">错误类型</param>
    /// <param name="setId">战利品箱 SetId</param>
    /// <param name="details">错误详细信息</param>
    public void HandleError(LootErrorType errorType, string setId, string details)
    {
        // 记录错误日志
        LogError(errorType, setId, details);
        
        // 根据错误类型执行不同的处理策略
        switch (errorType)
        {
            case LootErrorType.NetworkTimeout:
                // 网络超时需要重试
                Debug.LogWarning($"[LOOT][ERROR] Network timeout for SetId={setId}, will retry", "LootError");
                break;
                
            case LootErrorType.LootBoxNotFound:
                // 战利品箱不存在，通知用户并关闭 UI
                Debug.LogError($"[LOOT][ERROR] LootBox not found: SetId={setId}", "LootError");
                NotifyPlayer("战利品箱已不存在或已被移除");
                // 注意：实际关闭 UI 的逻辑应该在调用方处理
                break;
                
            case LootErrorType.CapacityExceeded:
                // 容量超限
                Debug.LogWarning($"[LOOT][ERROR] Capacity exceeded for SetId={setId}", "LootError");
                NotifyPlayer("战利品箱已满，无法放入更多物品");
                break;
                
            case LootErrorType.ItemNotFound:
                // 物品不存在
                Debug.LogWarning($"[LOOT][ERROR] Item not found in SetId={setId}, details: {details}", "LootError");
                NotifyPlayer("物品不存在或已被其他玩家取走");
                break;
                
            case LootErrorType.InvalidPosition:
                // 无效的位置
                Debug.LogWarning($"[LOOT][ERROR] Invalid position for SetId={setId}, details: {details}", "LootError");
                NotifyPlayer("无效的物品位置");
                break;
                
            case LootErrorType.ValidationFailed:
                // 验证失败
                Debug.LogWarning($"[LOOT][ERROR] Validation failed for SetId={setId}, details: {details}", "LootError");
                NotifyPlayer("操作验证失败，请重试");
                break;
                
            case LootErrorType.InvalidSetId:
                // 无效的 SetId
                Debug.LogError($"[LOOT][ERROR] Invalid SetId: {setId}, details: {details}", "LootError");
                NotifyPlayer("无效的战利品箱标识");
                break;
                
            default:
                Debug.LogError($"[LOOT][ERROR] Unknown error type: {errorType}, SetId={setId}, details: {details}", "LootError");
                NotifyPlayer("发生未知错误");
                break;
        }
    }
    
    /// <summary>
    /// 重试请求（带指数退避）
    /// </summary>
    /// <param name="setId">战利品箱 SetId</param>
    /// <param name="retryAction">重试操作</param>
    /// <returns>异步任务</returns>
    public async Task RetryRequest(string setId, Action retryAction)
    {
        // 检查重试次数
        if (!_retryCounters.TryGetValue(setId, out var retryCount))
        {
            retryCount = 0;
        }
        
        // 如果已经达到最大重试次数，不再重试
        if (retryCount >= MaxRetryCount)
        {
            Debug.LogError($"[LOOT][RETRY] Max retry count reached for SetId={setId}, giving up", "LootRetry");
            _retryCounters.Remove(setId);
            _lastRetryTime.Remove(setId);
            NotifyPlayer("网络连接失败，请检查网络后重试");
            return;
        }
        
        // 记录重试信息
        _retryCounters[setId] = retryCount + 1;
        _lastRetryTime[setId] = DateTime.Now;
        
        // 计算延迟时间（指数退避）
        var delayMs = retryCount < RetryDelaysMs.Length 
            ? RetryDelaysMs[retryCount] 
            : RetryDelaysMs[RetryDelaysMs.Length - 1];
        
        Debug.Log($"[LOOT][RETRY] Retrying request for SetId={setId}, attempt {retryCount + 1}/{MaxRetryCount}, delay={delayMs}ms", "LootRetry");
        
        // 等待指定时间
        await Task.Delay(delayMs);
        
        // 执行重试操作
        try
        {
            retryAction?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LOOT][RETRY] Retry action failed for SetId={setId}: {ex.Message}", "LootRetry");
            // 继续重试
            await RetryRequest(setId, retryAction);
        }
    }
    
    /// <summary>
    /// 重置重试计数器（操作成功后调用）
    /// </summary>
    /// <param name="setId">战利品箱 SetId</param>
    public void ResetRetryCounter(string setId)
    {
        if (_retryCounters.ContainsKey(setId))
        {
            Debug.LogDebug($"[LOOT][RETRY] Reset retry counter for SetId={setId}", "LootRetry");
            _retryCounters.Remove(setId);
            _lastRetryTime.Remove(setId);
        }
    }
    
    /// <summary>
    /// 获取当前重试次数
    /// </summary>
    /// <param name="setId">战利品箱 SetId</param>
    /// <returns>重试次数</returns>
    public int GetRetryCount(string setId)
    {
        return _retryCounters.TryGetValue(setId, out var count) ? count : 0;
    }
    
    /// <summary>
    /// 记录错误日志
    /// </summary>
    /// <param name="errorType">错误类型</param>
    /// <param name="setId">战利品箱 SetId</param>
    /// <param name="details">错误详细信息</param>
    private void LogError(LootErrorType errorType, string setId, string details)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"[LOOT][ERROR] Timestamp={timestamp}, ErrorType={errorType}, SetId={setId}, Details={details ?? "N/A"}";
        
        // 根据错误严重程度选择日志级别
        switch (errorType)
        {
            case LootErrorType.NetworkTimeout:
            case LootErrorType.ItemNotFound:
            case LootErrorType.InvalidPosition:
            case LootErrorType.CapacityExceeded:
                Debug.LogWarning(logMessage, "LootError");
                break;
                
            case LootErrorType.LootBoxNotFound:
            case LootErrorType.InvalidSetId:
            case LootErrorType.ValidationFailed:
                Debug.LogError(logMessage, "LootError");
                break;
                
            default:
                Debug.LogError(logMessage, "LootError");
                break;
        }
    }
    
    /// <summary>
    /// 通知玩家错误信息
    /// </summary>
    /// <param name="message">错误消息</param>
    private void NotifyPlayer(string message)
    {
        // TODO: 集成到游戏的 UI 提示系统
        // 目前使用 Debug.Log 作为临时方案
        Debug.LogWarning($"[LOOT][NOTIFY] {message}", "LootNotify");
        
        // 未来可以集成到游戏的提示系统，例如：
        // UIManager.ShowNotification(message);
        // 或者使用游戏内的聊天系统显示消息
    }
    
    /// <summary>
    /// 清理过期的重试记录（可选的维护方法）
    /// </summary>
    /// <param name="maxAge">最大保留时间（秒）</param>
    public void CleanupExpiredRetries(int maxAge = 300)
    {
        var now = DateTime.Now;
        var expiredKeys = new List<string>();
        
        foreach (var kv in _lastRetryTime)
        {
            if ((now - kv.Value).TotalSeconds > maxAge)
            {
                expiredKeys.Add(kv.Key);
            }
        }
        
        foreach (var key in expiredKeys)
        {
            _retryCounters.Remove(key);
            _lastRetryTime.Remove(key);
            Debug.LogDebug($"[LOOT][RETRY] Cleaned up expired retry record for SetId={key}", "LootRetry");
        }
    }
}
