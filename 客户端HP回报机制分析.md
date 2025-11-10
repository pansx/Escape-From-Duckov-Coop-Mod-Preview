# 客户端HP回报机制完整分析

## 问题描述
客机进图后血量变少，甚至只有一半不到的问题。需要研究所有客机向主机回报HP的地方。

## 血量同步机制概述

### 操作码定义 (Op.cs)
```csharp
PLAYER_HEALTH_REPORT = 16,  // 客户端 -> 主机：上传自己当前(max,curr)
AUTH_HEALTH_SELF = 17,      // 主机 -> 某个客户端：把"你自己本地人物"的(max,cur)设为权威值
AUTH_HEALTH_REMOTE = 18,    // 主机 -> 所有客户端：某位玩家的(max,cur)用于远端展示（带 playerId）
```

## 客户端向主机回报HP的所有位置

### 1. HealthM.cs - Client_SendSelfHealth() 【主动上报】
**文件**: `EscapeFromDuckovCoopMod/Main/Health/HealthM.cs`
**触发时机**: 客户端血量变化时主动上报

```csharp
public void Client_SendSelfHealth(Health h, bool force)
{
    if (_cliApplyingSelfSnap || Time.time < _cliEchoMuteUntil) return;
    if (!networkStarted || IsServer || connectedPeer == null || h == null) return;

    float max = 0f, cur = 0f;
    try { max = h.MaxHealth; } catch { }
    try { cur = h.CurrentHealth; } catch { }

    // 去抖：值相同直接跳过
    if (!force && Mathf.Approximately(max, _cliLastSentHp.max) && 
        Mathf.Approximately(cur, _cliLastSentHp.cur))
        return;

    // 节流：20Hz
    if (!force && Time.time < _cliNextSendHp) return;

    var w = new NetDataWriter();
    w.Put((byte)Op.PLAYER_HEALTH_REPORT);
    w.Put(max);  // ⚠️ 发送MaxHealth
    w.Put(cur);  // ⚠️ 发送CurrentHealth
    connectedPeer.Send(w, DeliveryMethod.ReliableOrdered);

    _cliLastSentHp = (max, cur);
    _cliNextSendHp = Time.time + 0.05f;
}
```

**特点**:
- 20Hz节流（每0.05秒最多发送一次）
- 值未变不发送（去抖）
- 发送 `MaxHealth` 和 `CurrentHealth`

### 2. HealthM.cs - Client_ReportSelfHealth_IfReadyOnce() 【初始上报】
**文件**: `EscapeFromDuckovCoopMod/Main/Health/HealthM.cs`
**触发时机**: 客户端首次连接或场景加载后的初始血量上报

```csharp
public void Client_ReportSelfHealth_IfReadyOnce()
{
    if (_cliApplyingSelfSnap || Time.time < _cliEchoMuteUntil) return;
    if (IsServer || HealthTool._cliInitHpReported) return;
    if (connectedPeer == null || connectedPeer.ConnectionState != ConnectionState.Connected) return;

    var main = CharacterMainControl.Main;
    var h = main ? main.GetComponentInChildren<Health>(true) : null;
    if (!h) return;

    float max = 0f, cur = 0f;
    try { max = h.MaxHealth; } catch { }
    try { cur = h.CurrentHealth; } catch { }

    var w = new NetDataWriter();
    w.Put((byte)Op.PLAYER_HEALTH_REPORT);
    w.Put(max);  // ⚠️ 发送MaxHealth
    w.Put(cur);  // ⚠️ 发送CurrentHealth
    connectedPeer.Send(w, DeliveryMethod.ReliableOrdered);

    HealthTool._cliInitHpReported = true;  // 标记已上报
}
```

**特点**:
- 只执行一次（通过 `_cliInitHpReported` 标记）
- 在连接建立后首次上报
- 发送 `MaxHealth` 和 `CurrentHealth`

### 3. Mod.cs - Update() 中的定期调用
**文件**: `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs`
**触发时机**: 每帧检查并调用

```csharp
private void Update()
{
    // ... 其他代码 ...
    
    if (networkStarted)
    {
        // 初始化后立即上报一次
        if (!isinit2)
        {
            isinit2 = true;
            if (!IsServer) HealthM.Instance.Client_ReportSelfHealth_IfReadyOnce();
        }
        
        // ... 其他代码 ...
        
        // 主机：每帧确保给所有 Health 打钩
        if (IsServer) HealthM.Instance.Server_EnsureAllHealthHooks();

        // 客户端：本场景里若还没成功上报，就每帧重试直到成功
        if (!IsServer && !HealthTool._cliInitHpReported) 
            HealthM.Instance.Client_ReportSelfHealth_IfReadyOnce();

        // 客户端：给自己的 Health 持续打钩，变化就上报
        if (!IsServer) HealthTool.Client_HookSelfHealth();
    }
}
```

### 4. HealthTool.cs - Client_HookSelfHealth() 【持续监听】
**文件**: `EscapeFromDuckovCoopMod/Main/Health/HealthTool.cs`
**触发时机**: 每帧检查并给本地玩家Health添加监听器

```csharp
public static void Client_HookSelfHealth()
{
    if (_cliHookedSelf) return;
    var main = CharacterMainControl.Main;
    var h = main ? main.GetComponentInChildren<Health>(true) : null;
    if (!h) return;

    // 血量变化监听
    _cbSelfHpChanged = _ => HealthM.Instance.Client_SendSelfHealth(h, false);
    
    // 最大血量变化监听
    _cbSelfMaxChanged = _ => HealthM.Instance.Client_SendSelfHealth(h, true);
    
    // 受击监听（强制上报，跳过节流）
    _cbSelfHurt = di =>
    {
        _cliLastSelfHurtAt = Time.time; // 记录受击时间
        try { _cliLastSelfHpLocal = h.CurrentHealth; } catch { }
        HealthM.Instance.Client_SendSelfHealth(h, true); // ⚠️ 受击当帧强制上报
    };
    
    // 死亡监听（强制上报）
    _cbSelfDead = _ => HealthM.Instance.Client_SendSelfHealth(h, true);

    h.OnHealthChange.AddListener(_cbSelfHpChanged);
    h.OnMaxHealthChange.AddListener(_cbSelfMaxChanged);
    h.OnHurtEvent.AddListener(_cbSelfHurt);
    h.OnDeadEvent.AddListener(_cbSelfDead);

    _cliHookedSelf = true;

    // 初次钩上也主动发一次，作为双保险
    HealthM.Instance.Client_SendSelfHealth(h, true);
}
```

**特点**:
- 监听4个事件：血量变化、最大血量变化、受击、死亡
- 受击和死亡时强制上报（force=true），跳过20Hz节流
- 初次挂钩时立即上报一次
- 记录受击时间用于防回弹

## 主机接收和处理

### Mod.cs - OnNetworkReceive() 处理 PLAYER_HEALTH_REPORT
**文件**: `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs`

```csharp
case Op.PLAYER_HEALTH_REPORT:
{
    if (IsServer)
    {
        var max = reader.GetFloat();  // ⚠️ 读取MaxHealth
        var cur = reader.GetFloat();  // ⚠️ 读取CurrentHealth
        
        if (max <= 0f)
        {
            // 无效数据，缓存起来
            HealthTool._srvPendingHp[peer] = (max, cur);
            break;
        }

        if (remoteCharacters != null && remoteCharacters.TryGetValue(peer, out var go) && go)
        {
            // 主机本地先应用，自己能立刻看到
            HealthM.Instance.ApplyHealthAndEnsureBar(go, max, cur);

            // 再用统一广播流程，发给本人 + 其他客户端
            var h = go.GetComponentInChildren<Health>(true);
            if (h) HealthM.Instance.Server_OnHealthChanged(peer, h);
        }
        else
        {
            // 远端克隆还没创建，缓存起来
            HealthTool._srvPendingHp[peer] = (max, cur);
        }
    }
    break;
}
```

## 主机向客户端下发血量

### HealthM.cs - Server_OnHealthChanged()
**文件**: `EscapeFromDuckovCoopMod/Main/Health/HealthM.cs`

```csharp
public void Server_OnHealthChanged(NetPeer ownerPeer, Health h)
{
    if (!IsServer || !h) return;

    float max = 0f, cur = 0f;
    try { max = h.MaxHealth; } catch { }
    try { cur = h.CurrentHealth; } catch { }

    if (max <= 0f) return;
    
    // 去抖 + 限频
    if (_srvLastSent.TryGetValue(h, out var last))
        if (Mathf.Approximately(max, last.max) && Mathf.Approximately(cur, last.cur))
            return;

    var now = Time.time;
    if (_srvNextSend.TryGetValue(h, out var tNext) && now < tNext)
        return;

    _srvLastSent[h] = (max, cur);
    _srvNextSend[h] = now + SRV_HP_SEND_COOLDOWN;

    var pid = NetService.Instance.GetPlayerId(ownerPeer);

    // ✅ 回传本人快照：AUTH_HEALTH_SELF
    if (ownerPeer != null && ownerPeer.ConnectionState == ConnectionState.Connected)
    {
        var w1 = new NetDataWriter();
        w1.Put((byte)Op.AUTH_HEALTH_SELF);
        w1.Put(max);  // ⚠️ 发送MaxHealth
        w1.Put(cur);  // ⚠️ 发送CurrentHealth
        ownerPeer.Send(w1, DeliveryMethod.ReliableOrdered);
    }

    // ✅ 广播给其他玩家：AUTH_HEALTH_REMOTE
    var w2 = new NetDataWriter();
    w2.Put((byte)Op.AUTH_HEALTH_REMOTE);
    w2.Put(pid);
    w2.Put(max);  // ⚠️ 发送MaxHealth
    w2.Put(cur);  // ⚠️ 发送CurrentHealth

    foreach (var p in netManager.ConnectedPeerList)
    {
        if (p == ownerPeer) continue;
        p.Send(w2, DeliveryMethod.ReliableOrdered);
    }
}
```

## 客户端接收主机下发的血量

### Mod.cs - OnNetworkReceive() 处理 AUTH_HEALTH_SELF
**文件**: `EscapeFromDuckovCoopMod/Main/Loader/Mod.cs`

```csharp
case Op.AUTH_HEALTH_SELF:
{
    var max = reader.GetFloat();  // ⚠️ 读取MaxHealth
    var cur = reader.GetFloat();  // ⚠️ 读取CurrentHealth

    if (max <= 0f)
    {
        CoopTool._cliSelfHpMax = max;
        CoopTool._cliSelfHpCur = cur;
        CoopTool._cliSelfHpPending = true;
        break;
    }

    // 防回弹：受击窗口内不接受"比本地更高"的回显
    var shouldApply = true;
    try
    {
        var main = CharacterMainControl.Main;
        var selfH = main ? main.Health : null;
        if (selfH)
        {
            var localCur = selfH.CurrentHealth;
            // 仅在"刚受击的短时间窗"里做保护
            if (Time.time - HealthTool._cliLastSelfHurtAt <= SELF_ACCEPT_WINDOW)
                if (cur > localCur + 0.0001f)
                {
                    Debug.Log($"[HP][SelfEcho] drop stale echo: local={localCur:F3} srv={cur:F3}");
                    shouldApply = false;
                }
        }
    }
    catch { }

    HealthM.Instance._cliApplyingSelfSnap = true;
    HealthM.Instance._cliEchoMuteUntil = Time.time + SELF_MUTE_SEC;
    try
    {
        if (shouldApply)
        {
            // 应用血量
            // ... 应用逻辑 ...
        }
    }
    finally
    {
        HealthM.Instance._cliApplyingSelfSnap = false;
    }
    break;
}
```

## 潜在问题分析

### 🔴 问题1: 客户端进图时血量可能未初始化
**位置**: `Client_ReportSelfHealth_IfReadyOnce()`

当客户端刚进入场景时，`CharacterMainControl.Main` 的 `Health` 组件可能还未完全初始化，导致：
- `MaxHealth` 可能返回 0 或默认值
- `CurrentHealth` 可能返回 0 或默认值

**解决方案**:
1. 在上报前检查 `MaxHealth` 是否有效（> 0）
2. 延迟上报，等待角色完全初始化
3. 添加重试机制

### 🔴 问题2: 场景切换时血量重置
**位置**: 场景加载流程

客户端在场景切换时，可能会：
1. 先创建新的角色对象（血量为默认值）
2. 然后才收到主机的权威血量
3. 导致短暂显示错误的血量

**解决方案**:
1. 在场景加载完成前禁止血量上报
2. 等待主机下发权威血量后再显示
3. 使用场景门控机制同步

### 🔴 问题3: 主机连接时的初始血量同步
**位置**: `NetService.cs - OnPeerConnected()`

```csharp
public void OnPeerConnected(NetPeer peer)
{
    // ... 其他代码 ...
    
    if (IsServer)
    {
        // 1) 主机自己的血量
        var hostMain = CharacterMainControl.Main;
        var hostH = hostMain ? hostMain.GetComponentInChildren<Health>(true) : null;
        if (hostH)
        {
            var w = new NetDataWriter();
            w.Put((byte)Op.AUTH_HEALTH_REMOTE);
            w.Put(GetPlayerId(null)); // Host 的 playerId
            try { w.Put(hostH.MaxHealth); } catch { w.Put(0f); }
            try { w.Put(hostH.CurrentHealth); } catch { w.Put(0f); }
            peer.Send(w, DeliveryMethod.ReliableOrdered);
        }

        // 2) 其他已连接玩家的血量
        if (remoteCharacters != null)
            foreach (var kv in remoteCharacters)
            {
                // ... 发送其他玩家血量 ...
            }
    }
}
```

**问题**: 
- 如果主机的 `Health` 组件未初始化，会发送 0/0
- 客户端收到后会应用错误的血量

### 🔴 问题4: 血量应用时机
**位置**: `HealthM.cs - ApplyHealthAndEnsureBar()`

```csharp
public void ApplyHealthAndEnsureBar(GameObject go, float max, float cur)
{
    if (!go) return;

    var cmc = go.GetComponent<CharacterMainControl>();
    var h = go.GetComponentInChildren<Health>(true);
    if (!cmc || !h) return;

    try { h.autoInit = false; } catch { }

    // 绑定 Health ⇄ Character
    HealthTool.BindHealthToCharacter(h, cmc);

    // 先把数值灌进去
    ForceSetHealth(h, max > 0 ? max : 40f, cur > 0 ? cur : max > 0 ? max : 40f, false);
    
    // ... 血条显示逻辑 ...
}
```

**问题**:
- 如果 `max <= 0`，会使用默认值 40
- 如果 `cur <= 0`，会使用 `max` 或 40
- 这可能导致血量不准确

## 完整的血量同步流程

### 客户端进图流程
```
1. 客户端连接到主机
   ↓
2. 主机发送 AUTH_HEALTH_REMOTE（主机和其他玩家的血量）
   ↓
3. 客户端场景加载完成
   ↓
4. 客户端调用 Client_ReportSelfHealth_IfReadyOnce()
   ↓
5. 客户端发送 PLAYER_HEALTH_REPORT（自己的血量）⚠️ 可能此时血量未初始化
   ↓
6. 主机接收并应用到远程角色
   ↓
7. 主机发送 AUTH_HEALTH_SELF（回传给客户端）
   ↓
8. 主机广播 AUTH_HEALTH_REMOTE（给其他客户端）
   ↓
9. 客户端接收 AUTH_HEALTH_SELF 并应用
```

### 血量变化流程
```
1. 客户端本地血量变化（受伤/治疗）
   ↓
2. HealthTool.Client_HookSelfHealth() 检测到变化
   ↓
3. 调用 Client_SendSelfHealth()
   ↓
4. 发送 PLAYER_HEALTH_REPORT 到主机
   ↓
5. 主机接收并应用
   ↓
6. 主机调用 Server_OnHealthChanged()
   ↓
7. 主机发送 AUTH_HEALTH_SELF（回传）
   ↓
8. 主机广播 AUTH_HEALTH_REMOTE（其他玩家）
```

## 建议的修复方案

### 方案1: 延迟初始血量上报
在 `Client_ReportSelfHealth_IfReadyOnce()` 中添加血量有效性检查：

```csharp
public void Client_ReportSelfHealth_IfReadyOnce()
{
    // ... 现有代码 ...
    
    float max = 0f, cur = 0f;
    try { max = h.MaxHealth; } catch { }
    try { cur = h.CurrentHealth; } catch { }
    
    // ⚠️ 新增：检查血量是否有效
    if (max <= 0f || cur <= 0f)
    {
        Debug.LogWarning($"[HP] 血量未初始化，延迟上报: max={max}, cur={cur}");
        return; // 不上报，等待下一帧重试
    }
    
    // ... 发送逻辑 ...
}
```

### 方案2: 主机连接时等待客户端血量
在 `OnPeerConnected()` 中不立即发送主机血量，而是等待客户端先上报：

```csharp
public void OnPeerConnected(NetPeer peer)
{
    // ... 现有代码 ...
    
    if (IsServer)
    {
        // ⚠️ 不立即发送主机血量，等待客户端先上报
        // 客户端上报后，主机会通过 Server_OnHealthChanged() 统一下发
    }
}
```

### 方案3: 场景门控机制
确保所有玩家在场景完全加载后才开始血量同步：

```csharp
// 在场景加载完成回调中
private void OnSceneLoadComplete()
{
    if (!IsServer)
    {
        // 等待一帧，确保所有组件初始化完成
        StartCoroutine(DelayedHealthReport());
    }
}

private IEnumerator DelayedHealthReport()
{
    yield return new WaitForEndOfFrame();
    yield return new WaitForSeconds(0.1f); // 额外等待100ms
    
    HealthM.Instance.Client_ReportSelfHealth_IfReadyOnce();
}
```

## 总结

客机向主机回报HP的所有位置：

1. **HealthM.cs - Client_SendSelfHealth()**: 血量变化时主动上报（20Hz节流）
2. **HealthM.cs - Client_ReportSelfHealth_IfReadyOnce()**: 连接后首次上报
3. **Mod.cs - Update()**: 每帧检查并重试上报
4. **HealthTool.cs - Client_HookSelfHealth()**: 持续监听血量变化

**核心问题**:
- 客户端进图时血量可能未初始化就上报了
- 主机连接时可能发送了无效的血量数据
- 场景切换时血量同步时机不当

**建议修复**:
1. 添加血量有效性检查（max > 0 && cur > 0）
2. 延迟初始血量上报，等待角色完全初始化
3. 使用场景门控机制确保同步时机正确
4. 添加更多日志以便调试


## 调试建议

### 添加详细日志

在关键位置添加日志以追踪血量同步问题：

#### 1. Client_ReportSelfHealth_IfReadyOnce() 添加日志

```csharp
public void Client_ReportSelfHealth_IfReadyOnce()
{
    // ... 现有代码 ...
    
    float max = 0f, cur = 0f;
    try { max = h.MaxHealth; } catch { }
    try { cur = h.CurrentHealth; } catch { }
    
    // 🔍 添加日志
    Debug.Log($"[HP][CLIENT] 初始血量上报: max={max:F2}, cur={cur:F2}, " +
              $"场景={MultiSceneCore.MainSceneID}, " +
              $"时间={Time.time:F2}");
    
    // 检查血量是否有效
    if (max <= 0f)
    {
        Debug.LogWarning($"[HP][CLIENT] ⚠️ MaxHealth无效({max})，跳过上报");
        return;
    }
    
    if (cur <= 0f)
    {
        Debug.LogWarning($"[HP][CLIENT] ⚠️ CurrentHealth无效({cur})，跳过上报");
        return;
    }
    
    var w = new NetDataWriter();
    w.Put((byte)Op.PLAYER_HEALTH_REPORT);
    w.Put(max);
    w.Put(cur);
    connectedPeer.Send(w, DeliveryMethod.ReliableOrdered);

    HealthTool._cliInitHpReported = true;
    Debug.Log($"[HP][CLIENT] ✓ 初始血量上报成功");
}
```

#### 2. Client_SendSelfHealth() 添加日志

```csharp
public void Client_SendSelfHealth(Health h, bool force)
{
    if (_cliApplyingSelfSnap || Time.time < _cliEchoMuteUntil) return;
    if (!networkStarted || IsServer || connectedPeer == null || h == null) return;

    float max = 0f, cur = 0f;
    try { max = h.MaxHealth; } catch { }
    try { cur = h.CurrentHealth; } catch { }

    // 🔍 添加日志（仅在force或值变化时）
    if (force || !Mathf.Approximately(max, _cliLastSentHp.max) || 
        !Mathf.Approximately(cur, _cliLastSentHp.cur))
    {
        Debug.Log($"[HP][CLIENT] 血量上报: max={max:F2}, cur={cur:F2}, " +
                  $"force={force}, 时间={Time.time:F2}");
    }

    // 去抖：值相同直接跳过
    if (!force && Mathf.Approximately(max, _cliLastSentHp.max) && 
        Mathf.Approximately(cur, _cliLastSentHp.cur))
        return;

    // 节流：20Hz
    if (!force && Time.time < _cliNextSendHp) return;

    var w = new NetDataWriter();
    w.Put((byte)Op.PLAYER_HEALTH_REPORT);
    w.Put(max);
    w.Put(cur);
    connectedPeer.Send(w, DeliveryMethod.ReliableOrdered);

    _cliLastSentHp = (max, cur);
    _cliNextSendHp = Time.time + 0.05f;
}
```

#### 3. 主机接收 PLAYER_HEALTH_REPORT 添加日志

```csharp
case Op.PLAYER_HEALTH_REPORT:
{
    if (IsServer)
    {
        var max = reader.GetFloat();
        var cur = reader.GetFloat();
        
        var playerId = Service.GetPlayerId(peer);
        
        // 🔍 添加日志
        Debug.Log($"[HP][SERVER] 收到血量上报: 玩家={playerId}, " +
                  $"max={max:F2}, cur={cur:F2}");
        
        if (max <= 0f)
        {
            Debug.LogWarning($"[HP][SERVER] ⚠️ 收到无效血量，缓存: " +
                           $"玩家={playerId}, max={max}, cur={cur}");
            HealthTool._srvPendingHp[peer] = (max, cur);
            break;
        }

        if (remoteCharacters != null && remoteCharacters.TryGetValue(peer, out var go) && go)
        {
            Debug.Log($"[HP][SERVER] ✓ 应用血量到远程角色: 玩家={playerId}");
            HealthM.Instance.ApplyHealthAndEnsureBar(go, max, cur);

            var h = go.GetComponentInChildren<Health>(true);
            if (h) HealthM.Instance.Server_OnHealthChanged(peer, h);
        }
        else
        {
            Debug.LogWarning($"[HP][SERVER] ⚠️ 远程角色未创建，缓存血量: 玩家={playerId}");
            HealthTool._srvPendingHp[peer] = (max, cur);
        }
    }
    break;
}
```

#### 4. 客户端接收 AUTH_HEALTH_SELF 添加日志

```csharp
case Op.AUTH_HEALTH_SELF:
{
    var max = reader.GetFloat();
    var cur = reader.GetFloat();

    // 🔍 添加日志
    Debug.Log($"[HP][CLIENT] 收到主机权威血量: max={max:F2}, cur={cur:F2}, " +
              $"时间={Time.time:F2}");

    if (max <= 0f)
    {
        Debug.LogWarning($"[HP][CLIENT] ⚠️ 收到无效权威血量: max={max}, cur={cur}");
        CoopTool._cliSelfHpMax = max;
        CoopTool._cliSelfHpCur = cur;
        CoopTool._cliSelfHpPending = true;
        break;
    }

    // 防回弹检查
    var shouldApply = true;
    try
    {
        var main = CharacterMainControl.Main;
        var selfH = main ? main.Health : null;
        if (selfH)
        {
            var localCur = selfH.CurrentHealth;
            if (Time.time - HealthTool._cliLastSelfHurtAt <= SELF_ACCEPT_WINDOW)
            {
                if (cur > localCur + 0.0001f)
                {
                    Debug.Log($"[HP][CLIENT] 🛡️ 防回弹: 丢弃陈旧回显 " +
                             $"local={localCur:F3} srv={cur:F3}");
                    shouldApply = false;
                }
            }
        }
    }
    catch { }

    if (shouldApply)
    {
        Debug.Log($"[HP][CLIENT] ✓ 应用权威血量: max={max:F2}, cur={cur:F2}");
        // ... 应用逻辑 ...
    }
    else
    {
        Debug.Log($"[HP][CLIENT] ✗ 跳过权威血量（防回弹）");
    }
    
    break;
}
```

### 测试步骤

#### 测试1: 初始血量同步
1. 主机启动游戏并进入场景
2. 客机连接到主机
3. 观察日志输出：
   - 客机是否成功上报初始血量？
   - 上报的血量值是否正确？
   - 主机是否正确接收并应用？
   - 主机是否正确回传权威血量？

**预期日志**:
```
[HP][CLIENT] 初始血量上报: max=100.00, cur=100.00, 场景=Base, 时间=5.23
[HP][CLIENT] ✓ 初始血量上报成功
[HP][SERVER] 收到血量上报: 玩家=192.168.1.100:12345, max=100.00, cur=100.00
[HP][SERVER] ✓ 应用血量到远程角色: 玩家=192.168.1.100:12345
[HP][CLIENT] 收到主机权威血量: max=100.00, cur=100.00, 时间=5.25
[HP][CLIENT] ✓ 应用权威血量: max=100.00, cur=100.00
```

#### 测试2: 场景切换血量同步
1. 主机和客机都在场景A
2. 主机发起场景投票切换到场景B
3. 观察日志输出：
   - 客机在新场景是否重新上报血量？
   - 血量值是否保持正确？
   - 是否有无效血量（0/0）的上报？

**预期日志**:
```
[SCENE] 开始加载场景: SceneB
[HP][CLIENT] 初始血量上报: max=75.50, cur=75.50, 场景=SceneB, 时间=15.67
[HP][CLIENT] ✓ 初始血量上报成功
[HP][SERVER] 收到血量上报: 玩家=192.168.1.100:12345, max=75.50, cur=75.50
```

#### 测试3: 受伤血量同步
1. 客机受到伤害
2. 观察日志输出：
   - 客机是否立即上报血量变化？
   - 主机是否正确接收并广播？
   - 客机是否收到权威血量回显？
   - 防回弹机制是否正常工作？

**预期日志**:
```
[HP][CLIENT] 血量上报: max=100.00, cur=85.30, force=true, 时间=20.45
[HP][SERVER] 收到血量上报: 玩家=192.168.1.100:12345, max=100.00, cur=85.30
[HP][CLIENT] 收到主机权威血量: max=100.00, cur=85.30, 时间=20.47
[HP][CLIENT] 🛡️ 防回弹: 丢弃陈旧回显 local=85.30 srv=90.00
```

### 常见问题排查

#### 问题1: 客机进图后血量为0或很低
**可能原因**:
1. 客机在角色未完全初始化时就上报了血量
2. 主机在连接时发送了无效的血量数据
3. 场景切换时血量同步时机不当

**排查方法**:
1. 查看日志中 `[HP][CLIENT] 初始血量上报` 的值
2. 检查是否有 `⚠️ MaxHealth无效` 或 `⚠️ CurrentHealth无效` 的警告
3. 确认 `_cliInitHpReported` 标记是否正确设置

**解决方案**:
- 在 `Client_ReportSelfHealth_IfReadyOnce()` 中添加血量有效性检查
- 延迟初始血量上报，等待角色完全初始化

#### 问题2: 血量回弹（受伤后又恢复）
**可能原因**:
1. 主机的权威血量回显比客机本地结算慢
2. 防回弹机制未正常工作
3. 网络延迟导致旧数据后到

**排查方法**:
1. 查看日志中是否有 `🛡️ 防回弹` 的提示
2. 检查 `_cliLastSelfHurtAt` 时间戳是否正确记录
3. 确认 `SELF_ACCEPT_WINDOW` 窗口时间是否合适（当前0.3秒）

**解决方案**:
- 调整 `SELF_ACCEPT_WINDOW` 窗口时间
- 确保受击时立即上报（force=true）

#### 问题3: 血量不同步
**可能原因**:
1. 客机未成功上报血量
2. 主机未正确接收或应用血量
3. 网络连接问题

**排查方法**:
1. 查看客机是否有 `✓ 初始血量上报成功` 的日志
2. 查看主机是否有 `收到血量上报` 的日志
3. 检查网络连接状态

**解决方案**:
- 添加重试机制
- 确保网络连接稳定
- 使用可靠传输（ReliableOrdered）

## 推荐的修复代码

### 修复1: 添加血量有效性检查

在 `HealthM.cs` 的 `Client_ReportSelfHealth_IfReadyOnce()` 方法中：

```csharp
public void Client_ReportSelfHealth_IfReadyOnce()
{
    if (_cliApplyingSelfSnap || Time.time < _cliEchoMuteUntil) return;
    if (IsServer || HealthTool._cliInitHpReported) return;
    if (connectedPeer == null || connectedPeer.ConnectionState != ConnectionState.Connected) return;

    var main = CharacterMainControl.Main;
    var h = main ? main.GetComponentInChildren<Health>(true) : null;
    if (!h) return;

    float max = 0f, cur = 0f;
    try { max = h.MaxHealth; } catch { }
    try { cur = h.CurrentHealth; } catch { }

    // ⚠️ 新增：检查血量是否有效
    if (max <= 0f || cur <= 0f)
    {
        Debug.LogWarning($"[HP][CLIENT] 血量未初始化，延迟上报: max={max}, cur={cur}");
        return; // 不上报，等待下一帧重试
    }

    Debug.Log($"[HP][CLIENT] 初始血量上报: max={max:F2}, cur={cur:F2}");

    var w = new NetDataWriter();
    w.Put((byte)Op.PLAYER_HEALTH_REPORT);
    w.Put(max);
    w.Put(cur);
    connectedPeer.Send(w, DeliveryMethod.ReliableOrdered);

    HealthTool._cliInitHpReported = true;
    Debug.Log($"[HP][CLIENT] ✓ 初始血量上报成功");
}
```

### 修复2: 场景切换时重置上报标记

在 `Mod.cs` 的 `LevelManager_OnLevelInitialized()` 方法中：

```csharp
private void LevelManager_OnLevelInitialized()
{
    AITool.ResetAiSerials();

    // ... 现有代码 ...

    // ⚠️ 新增：场景切换时重置血量上报标记
    if (!IsServer)
    {
        HealthTool._cliInitHpReported = false;
        Debug.Log($"[HP][CLIENT] 场景切换，重置血量上报标记");
    }

    if (!IsServer) HealthM.Instance.Client_ReportSelfHealth_IfReadyOnce();
    SceneNet.Instance.TrySendSceneReadyOnce();
    if (!IsServer) COOPManager.Weather.Client_RequestEnvSync();

    // ... 其他代码 ...
}
```

### 修复3: 主机连接时不立即发送血量

在 `NetService.cs` 的 `OnPeerConnected()` 方法中：

```csharp
public void OnPeerConnected(NetPeer peer)
{
    Debug.Log(CoopLocalization.Get("net.connectionSuccess", peer.EndPoint.ToString()));
    connectedPeer = peer;

    if (!IsServer)
    {
        status = CoopLocalization.Get("net.connectedTo", peer.EndPoint.ToString());
        isConnecting = false;
        Send_ClientStatus.Instance.SendClientStatusUpdate();
    }
    else
    {
        // 🔧 主机：告诉客户端其真实网络ID
        SetIdMessage.SendSetIdToPeer(peer);
        
        // 🕐 记录连接时间，开始超时计时
        _peerConnectionTime[peer] = Time.time;
        Debug.Log($"[JOIN_TIMEOUT] 玩家 {peer.EndPoint} 开始加入，超时时限: {JOIN_TIMEOUT_SECONDS}秒");
        
        // ⚠️ 修改：不立即发送血量，等待客户端先上报
        // 客户端上报后，主机会通过 Server_OnHealthChanged() 统一下发
        Debug.Log($"[HP][SERVER] 等待客户端上报血量: {peer.EndPoint}");
    }

    if (!playerStatuses.ContainsKey(peer))
        playerStatuses[peer] = new PlayerStatus
        {
            EndPoint = peer.EndPoint.ToString(),
            PlayerName = IsServer ? $"Player_{peer.Id}" : "Host",
            Latency = peer.Ping,
            IsInGame = false,
            LastIsInGame = false,
            Position = Vector3.zero,
            Rotation = Quaternion.identity,
            CustomFaceJson = null
        };

    if (IsServer) SendLocalPlayerStatus.Instance.SendPlayerStatusUpdate();

    // ⚠️ 注释掉原有的立即发送血量逻辑
    /*
    if (IsServer)
    {
        // 1) 主机自己
        var hostMain = CharacterMainControl.Main;
        var hostH = hostMain ? hostMain.GetComponentInChildren<Health>(true) : null;
        if (hostH)
        {
            // ... 发送血量 ...
        }

        // 2) 其他已连接玩家的血量
        if (remoteCharacters != null)
            foreach (var kv in remoteCharacters)
            {
                // ... 发送血量 ...
            }
    }
    */

    // 🧪 发送JSON测试消息（双方都发送）
    JsonMessage.SendTestJson(peer, writer);
}
```

## 总结

通过以上分析，我们找到了所有客机向主机回报HP的位置，并识别出了可能导致血量问题的原因：

1. **初始血量上报时机过早**：客机在角色未完全初始化时就上报了血量
2. **主机连接时发送无效数据**：主机在客户端连接时立即发送血量，但此时可能还未初始化
3. **场景切换时同步时机不当**：场景切换时血量上报标记未重置

建议按照上述修复方案进行修改，并添加详细的日志以便调试。
