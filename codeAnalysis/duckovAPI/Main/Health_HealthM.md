# HealthM - 生命值管理系统

## 📋 概述

`HealthM` 是生命值管理系统的核心类，负责主机权威的血量同步、伤害转发、血量上报等功能。

**文件路径**: `EscapeFromDuckovCoopMod/Main/Health/HealthM.cs`

---

## 🎯 核心功能

### 1. 血量同步机制

#### 主机端：节流去抖
```csharp
private const float SRV_HP_SEND_COOLDOWN = 0.05f; // 20Hz

// 节流去抖数据结构
private readonly Dictionary<Health, (float max, float cur)> _srvLastSent = new();
private readonly Dictionary<Health, float> _srvNextSend = new();
```

#### 客户端：上报机制
```csharp
private static (float max, float cur) _cliLastSentHp;
private static float _cliNextSendHp;
```

### 2. 伤害转发系统

主机端将伤害信息转发给客户端，由客户端本地执行 `Hurt` 方法：

```csharp
public void Server_ForwardHurtToOwner(NetPeer owner, DamageInfo di)
{
    if (!IsServer || owner == null) return;
    
    var w = new NetDataWriter();
    w.Put((byte)Op.PLAYER_HURT_EVENT);
    
    // 序列化 DamageInfo
    w.Put(di.damageValue);
    w.Put(di.armorPiercing);
    w.Put(di.critDamageFactor);
    w.Put(di.critRate);
    w.Put(di.crit);
    w.PutV3cm(di.damagePoint);
    w.PutDir(di.damageNormal.sqrMagnitude < 1e-6f ? Vector3.up : di.damageNormal.normalized);
    w.Put(di.fromWeaponItemID);
    w.Put(di.bleedChance);
    w.Put(di.isExplosion);
    
    owner.Send(w, DeliveryMethod.ReliableOrdered);
}
```

---

## 🔄 核心流程

### 1. 客户端血量上报流程

```csharp
public void Client_SendSelfHealth(Health h, bool force)
{
    // 1. 检查是否正在应用快照或在静音期
    if (_cliApplyingSelfSnap || Time.time < _cliEchoMuteUntil) return;
    
    // 2. 检查网络状态
    if (!networkStarted || IsServer || connectedPeer == null || h == null) return;
    
    // 3. 读取血量
    float max = 0f, cur = 0f;
    try
    {
        max = h.MaxHealth;
        cur = h.CurrentHealth;
    }
    catch { }
    
    // 4. 去抖：值相同直接跳过
    if (!force && Mathf.Approximately(max, _cliLastSentHp.max) && Mathf.Approximately(cur, _cliLastSentHp.cur))
        return;
    
    // 5. 节流：20Hz
    if (!force && Time.time < _cliNextSendHp) return;
    
    // 6. 发送血量上报
    var w = new NetDataWriter();
    w.Put((byte)Op.PLAYER_HEALTH_REPORT);
    w.Put(max);
    w.Put(cur);
    connectedPeer.Send(w, DeliveryMethod.ReliableOrdered);
    
    // 7. 更新缓存
    _cliLastSentHp = (max, cur);
    _cliNextSendHp = Time.time + 0.05f;
}
```

### 2. 客户端初始血量上报

```csharp
public void Client_ReportSelfHealth_IfReadyOnce()
{
    // 1. 检查是否正在应用快照或在静音期
    if (_cliApplyingSelfSnap || Time.time < _cliEchoMuteUntil) return;
    
    // 2. 检查是否已上报
    if (IsServer || HealthTool._cliInitHpReported) return;
    
    // 3. 检查连接状态
    if (connectedPeer == null || connectedPeer.ConnectionState != ConnectionState.Connected) return;
    
    // 4. 获取本地玩家 Health
    var main = CharacterMainControl.Main;
    var h = main ? main.GetComponentInChildren<Health>(true) : null;
    if (!h) return;
    
    // 5. 读取血量
    float max = 0f, cur = 0f;
    try
    {
        max = h.MaxHealth;
        cur = h.CurrentHealth;
    }
    catch { }
    
    // 6. 发送初始血量上报
    var w = new NetDataWriter();
    w.Put((byte)Op.PLAYER_HEALTH_REPORT);
    w.Put(max);
    w.Put(cur);
    connectedPeer.Send(w, DeliveryMethod.ReliableOrdered);
    
    // 7. 标记已上报
    HealthTool._cliInitHpReported = true;
    LoggerHelper.Log($"✓ 初始血量上报成功");
}
```

### 3. 主机端血量变化处理

```csharp
public void Server_OnHealthChanged(NetPeer ownerPeer, Health h)
{
    if (!IsServer || !h) return;
    
    // 1. 读取血量
    float max = 0f, cur = 0f;
    try
    {
        max = h.MaxHealth;
        cur = h.CurrentHealth;
    }
    catch { }
    
    if (max <= 0f) return;
    
    // 2. 去抖：值相同直接跳过
    if (_srvLastSent.TryGetValue(h, out var last))
        if (Mathf.Approximately(max, last.max) && Mathf.Approximately(cur, last.cur))
            return;
    
    // 3. 节流：20Hz
    var now = Time.time;
    if (_srvNextSend.TryGetValue(h, out var tNext) && now < tNext)
        return;
    
    // 4. 更新缓存
    _srvLastSent[h] = (max, cur);
    _srvNextSend[h] = now + SRV_HP_SEND_COOLDOWN;
    
    // 5. 计算 playerId
    var pid = NetService.Instance.GetPlayerId(ownerPeer);
    
    // 6. 回传本人快照：AUTH_HEALTH_SELF
    if (ownerPeer != null && ownerPeer.ConnectionState == ConnectionState.Connected)
    {
        var w1 = new NetDataWriter();
        w1.Put((byte)Op.AUTH_HEALTH_SELF);
        w1.Put(max);
        w1.Put(cur);
        ownerPeer.Send(w1, DeliveryMethod.ReliableOrdered);
    }
    
    // 7. 广播给其他玩家：AUTH_HEALTH_REMOTE
    var w2 = new NetDataWriter();
    w2.Put((byte)Op.AUTH_HEALTH_REMOTE);
    w2.Put(pid);
    w2.Put(max);
    w2.Put(cur);
    
    foreach (var p in netManager.ConnectedPeerList)
    {
        if (p == ownerPeer) continue; // 跳过本人，避免重复
        p.Send(w2, DeliveryMethod.ReliableOrdered);
    }
}
```

### 4. 客户端应用伤害

```csharp
public void Client_ApplySelfHurtFromServer(NetPacketReader r)
{
    try
    {
        // 1. 反序列化 DamageInfo
        var dmg = r.GetFloat();
        var ap = r.GetFloat();
        var cdf = r.GetFloat();
        var cr = r.GetFloat();
        var crit = r.GetInt();
        var hit = r.GetV3cm();
        var nrm = r.GetDir();
        var wid = r.GetInt();
        var bleed = r.GetFloat();
        var boom = r.GetBool();
        
        // 2. 获取本地玩家
        var main = LevelManager.Instance ? LevelManager.Instance.MainCharacter : null;
        if (!main || main.Health == null) return;
        
        // 3. 构造 DamageInfo
        var di = new DamageInfo(main)
        {
            damageValue = dmg,
            armorPiercing = ap,
            critDamageFactor = cdf,
            critRate = cr,
            crit = crit,
            damagePoint = hit,
            damageNormal = nrm,
            fromWeaponItemID = wid,
            bleedChance = bleed,
            isExplosion = boom
        };
        
        // 4. 记录最近一次本地受击时间（用于 echo 抑制）
        HealthTool._cliLastSelfHurtAt = Time.time;
        
        // 5. 执行伤害
        main.Health.Hurt(di);
        
        // 6. 上报血量
        Client_ReportSelfHealth_IfReadyOnce();
    }
    catch (Exception e)
    {
        LoggerHelper.LogWarning("[CLIENT] apply self hurt from server failed: " + e);
    }
}
```

---

## 🔧 辅助方法

### 1. 强制设置血量

```csharp
public void ForceSetHealth(Health h, float max, float cur, bool ensureBar = true)
{
    if (!h) return;
    
    // 1. 读取当前最大血量
    var nowMax = 0f;
    try
    {
        nowMax = h.MaxHealth;
    }
    catch { }
    
    // 2. 读取 defaultMaxHealth
    var defMax = 0;
    try
    {
        defMax = (int)(HealthTool.FI_defaultMax?.GetValue(h) ?? 0);
    }
    catch { }
    
    // 3. 如果传入的 max 更大，更新 defaultMaxHealth
    if (max > 0f && (nowMax <= 0f || max > nowMax + 0.0001f || defMax <= 0))
        try
        {
            HealthTool.FI_defaultMax?.SetValue(h, Mathf.RoundToInt(max));
            HealthTool.FI_lastMax?.SetValue(h, -12345f);
            h.OnMaxHealthChange?.Invoke(h);
        }
        catch { }
    
    // 4. 设置当前血量
    var effMax = 0f;
    try
    {
        effMax = h.MaxHealth;
    }
    catch { }
    
    if (effMax > 0f && cur > effMax + 0.0001f)
    {
        // 当前血量超过最大血量，直接设置 _current 字段
        try
        {
            HealthTool.FI__current?.SetValue(h, cur);
        }
        catch { }
        
        try
        {
            h.OnHealthChange?.Invoke(h);
        }
        catch { }
    }
    else
    {
        // 正常设置血量
        try
        {
            h.SetHealth(cur);
        }
        catch
        {
            try
            {
                HealthTool.FI__current?.SetValue(h, cur);
            }
            catch { }
        }
        
        try
        {
            h.OnHealthChange?.Invoke(h);
        }
        catch { }
    }
    
    // 5. 确保血条显示
    if (ensureBar)
    {
        try
        {
            h.showHealthBar = true;
        }
        catch { }
        
        try
        {
            h.RequestHealthBar();
        }
        catch { }
        
        StartCoroutine(EnsureBarRoutine(h, 30, 0.1f));
    }
}
```

### 2. 应用血量并确保血条

```csharp
public void ApplyHealthAndEnsureBar(GameObject go, float max, float cur)
{
    if (!go) return;
    
    // 1. 获取组件
    var cmc = go.GetComponent<CharacterMainControl>();
    var h = go.GetComponentInChildren<Health>(true);
    if (!cmc || !h) return;
    
    // 2. 禁用自动初始化
    try
    {
        h.autoInit = false;
    }
    catch { }
    
    // 3. 绑定 Health ⇄ Character
    HealthTool.BindHealthToCharacter(h, cmc);
    
    // 4. 设置血量
    ForceSetHealth(h, max > 0 ? max : 40f, cur > 0 ? cur : max > 0 ? max : 40f, false);
    
    // 5. 立刻起条
    try
    {
        h.showHealthBar = true;
    }
    catch { }
    
    try
    {
        h.RequestHealthBar();
    }
    catch { }
    
    // 6. 触发事件
    try
    {
        h.OnMaxHealthChange?.Invoke(h);
    }
    catch { }
    
    try
    {
        h.OnHealthChange?.Invoke(h);
    }
    catch { }
    
    // 7. 多帧重试：8 次、每 0.25s 一次
    StartCoroutine(EnsureBarRoutine(h, 8, 0.25f));
}
```

### 3. 血条显示兜底

```csharp
private static IEnumerator EnsureBarRoutine(Health h, int attempts, float interval)
{
    for (var i = 0; i < attempts; i++)
    {
        if (h == null) yield break;
        
        try
        {
            h.showHealthBar = true;
        }
        catch { }
        
        try
        {
            h.RequestHealthBar();
        }
        catch { }
        
        try
        {
            h.OnMaxHealthChange?.Invoke(h);
        }
        catch { }
        
        try
        {
            h.OnHealthChange?.Invoke(h);
        }
        catch { }
        
        yield return new WaitForSeconds(interval);
    }
}
```

### 4. 服务器兜底：确保所有 Health 都已挂监听

```csharp
public void Server_EnsureAllHealthHooks()
{
    if (!IsServer || !networkStarted) return;
    
    // 1. 主机自己
    var hostMain = CharacterMainControl.Main;
    if (hostMain) HealthTool.Server_HookOneHealth(null, hostMain.gameObject);
    
    // 2. 所有远程玩家
    if (remoteCharacters != null)
        foreach (var kv in remoteCharacters)
        {
            var peer = kv.Key;
            var go = kv.Value;
            if (peer == null || !go) continue;
            HealthTool.Server_HookOneHealth(peer, go);
        }
}
```

---

## 📊 性能优化

### 1. 节流机制

```csharp
// 主机端：20Hz 发送频率
private const float SRV_HP_SEND_COOLDOWN = 0.05f;

// 客户端：20Hz 上报频率
_cliNextSendHp = Time.time + 0.05f;
```

### 2. 去抖机制

```csharp
// 值相同直接跳过
if (!force && Mathf.Approximately(max, _cliLastSentHp.max) && Mathf.Approximately(cur, _cliLastSentHp.cur))
    return;
```

### 3. Echo 抑制

```csharp
// 记录最近一次本地受击时间
HealthTool._cliLastSelfHurtAt = Time.time;

// 在上报时检查是否在静音期
if (_cliApplyingSelfSnap || Time.time < _cliEchoMuteUntil) return;
```

---

## 🔗 相关模块

- **HealthTool**: 生命值工具类
- **NetService**: 网络服务核心
- **DamageInfo**: 伤害信息结构
- **CharacterMainControl**: 角色主控制器

---

## 📝 最近更新

### 2024-11-11
- ✅ 实现主机权威伤害转发
- ✅ 优化血量同步节流机制（20Hz）
- ✅ 添加去抖机制避免重复发送
- ✅ 添加 Echo 抑制机制
- ✅ 优化血条显示兜底逻辑
- ✅ 添加初始血量上报机制

---

*文档版本: 1.0.0*  
*最后更新: 2024-11-11*
