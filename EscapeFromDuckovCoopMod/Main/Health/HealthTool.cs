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

using System.Reflection;
using Duckov.UI;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

namespace EscapeFromDuckovCoopMod;

public static class HealthTool
{
    public static bool _cliHookedSelf;
    public static UnityAction<Health> _cbSelfHpChanged, _cbSelfMaxChanged;
    public static UnityAction<DamageInfo> _cbSelfHurt, _cbSelfDead;
    public static float _cliNextSendHp = 0f;
    public static (float max, float cur) _cliLastSentHp = (0f, 0f);

    // 主机端：Health -> 所属 Peer 的映射（host 自己用 null）
    public static readonly Dictionary<Health, NetPeer> _srvHealthOwner = new();
    public static readonly HashSet<Health> _srvHooked = new();
    public static float _cliLastSelfHurtAt = -999f; // 最后本地受击时间
    public static float _cliLastSelfHpLocal = -1f; // 受击后本地血量（用于对比回显）
    public static bool _cliInitHpReported = false;

    public static readonly Dictionary<NetPeer, (float max, float cur)> _srvPendingHp = new();


    // 反射字段（Health 反编译字段）研究了20年研究出来的
    public static readonly FieldInfo FI_defaultMax =
        typeof(Health).GetField("defaultMaxHealth", BindingFlags.NonPublic | BindingFlags.Instance);

    public static readonly FieldInfo FI_lastMax =
        typeof(Health).GetField("lastMaxHealth", BindingFlags.NonPublic | BindingFlags.Instance);

    public static readonly FieldInfo FI__current =
        typeof(Health).GetField("_currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);

    public static readonly FieldInfo FI_characterCached =
        typeof(Health).GetField("characterCached", BindingFlags.NonPublic | BindingFlags.Instance);

    public static readonly FieldInfo FI_hasCharacter =
        typeof(Health).GetField("hasCharacter", BindingFlags.NonPublic | BindingFlags.Instance);

    private static NetService Service => NetService.Instance;
    private static Dictionary<NetPeer, GameObject> remoteCharacters => Service?.remoteCharacters;


    // 小工具：仅做UI表现，不改数值与事件
    public static void TryShowDamageBarUI(Health h, float damage)
    {
        if (h == null || damage <= 0f) 
        {
            Debug.Log($"[HealthTool] TryShowDamageBarUI: Invalid params - Health={h != null}, damage={damage}");
            return;
        }

        Debug.Log($"[HealthTool] TryShowDamageBarUI: Starting for damage={damage}");
        try
        {
            // 1) 找到当前 HealthBar
            var hbm = HealthBarManager.Instance;
            if (hbm == null) return;

            var miGet = AccessTools.DeclaredMethod(typeof(HealthBarManager), "GetActiveHealthBar", new[] { typeof(Health) });
            var hb = miGet?.Invoke(hbm, new object[] { h });
            if (hb == null) return;

            // 2) 取得 fill 的 rect 宽度（像素）
            var fiFill = AccessTools.Field(typeof(HealthBar), "fill");
            var fillImg = fiFill?.GetValue(hb) as Image;
            var width = 0f;
            if (fillImg != null)
                // 注意：rect 是本地空间宽度，足够用于"最小像素宽度"
                width = fillImg.rectTransform.rect.width;

            // 3) 计算"最小可见伤害"
            //    - minPixels: 小伤害条至少显示这么宽
            //    - minPercent: 即使宽度没取到，也保证一个极小百分比
            const float minPixels = 2f;
            const float minPercent = 0.0015f; // 0.15%

            var maxHp = Mathf.Max(1f, h.MaxHealth);
            var minByPixels = width > 0f ? minPixels / width * maxHp : 0f;
            var minByPercent = minPercent * maxHp;
            var minDamageToShow = Mathf.Max(minByPixels, minByPercent);

            // 4) 以"实际伤害 or 最小可见伤害"的较大者来显示受击条（仅视觉，不改真实血量）
            var visualDamage = Mathf.Max(damage, minDamageToShow);

            // 5) 反射调用 HealthBar.ShowDamageBar(float)
            var miShow = AccessTools.DeclaredMethod(typeof(HealthBar), "ShowDamageBar", new[] { typeof(float) });
            miShow?.Invoke(hb, new object[] { visualDamage });
            Debug.Log($"[HealthTool] TryShowDamageBarUI: Successfully showed damage bar with visualDamage={visualDamage}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HealthTool] TryShowDamageBarUI: Exception - {ex.Message}");
        }
    }

    public static NetPeer Server_FindOwnerPeerByHealth(Health h)
    {
        // 检查网络服务和远程角色字典是否可用
        if (Service == null || remoteCharacters == null)
        {
            // Debug.Log("[HealthTool] Server_FindOwnerPeerByHealth: NetService or remoteCharacters not available");
            return null;
        }
        
        if (h == null) 
        {
            Debug.Log("[HealthTool] Server_FindOwnerPeerByHealth: Health is null");
            return null;
        }
        
        Debug.Log($"[HealthTool] Server_FindOwnerPeerByHealth: Finding owner for Health {h.GetInstanceID()}");
        CharacterMainControl cmc = null;
        try
        {
            cmc = h.TryGetCharacter();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HealthTool] Server_FindOwnerPeerByHealth: Exception in TryGetCharacter - {ex.Message}");
        }

        if (!cmc)
            try
            {
                cmc = h.GetComponentInParent<CharacterMainControl>();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HealthTool] Server_FindOwnerPeerByHealth: Exception in GetComponentInParent - {ex.Message}");
            }

        if (!cmc) 
        {
            Debug.Log("[HealthTool] Server_FindOwnerPeerByHealth: No CharacterMainControl found");
            return null;
        }

        foreach (var kv in remoteCharacters) // remoteCharacters: NetPeer -> GameObject（主机维护）
            if (kv.Value == cmc.gameObject)
            {
                Debug.Log($"[HealthTool] Server_FindOwnerPeerByHealth: Found owner peer {kv.Key}");
                return kv.Key;
            }
        
        Debug.Log("[HealthTool] Server_FindOwnerPeerByHealth: No matching peer found");
        return null;
    }


    public static void Server_HookOneHealth(NetPeer peer, GameObject instance)
    {
        // 检查网络服务是否可用
        if (Service == null)
        {
            // Debug.Log("[HealthTool] Server_HookOneHealth: NetService not available, skipping");
            return;
        }
        
        if (!instance) 
        {
            Debug.LogError("[HealthTool] Server_HookOneHealth: Instance is null");
            return;
        }

        Debug.Log($"[HealthTool] Server_HookOneHealth: Starting for peer={peer}, instance={instance.name}");
        var h = instance.GetComponentInChildren<Health>(true);
        var cmc = instance.GetComponent<CharacterMainControl>();
        if (!h) 
        {
            Debug.LogError($"[HealthTool] Server_HookOneHealth: No Health component found on {instance.name}");
            return;
        }

        try
        {
            h.autoInit = false;
        }
        catch
        {
        }

        BindHealthToCharacter(h, cmc); // 你已有：修正 hasCharacter 以便 UI/Hidden 逻辑正常

        // 记录归属 + 绑定事件（避免重复）
        _srvHealthOwner[h] = peer; // host 自己传 null
        Debug.Log($"[HealthTool] Server_HookOneHealth: Registered health owner - Health={h.GetInstanceID()}, Peer={peer}");
        
        if (!_srvHooked.Contains(h))
        {
            h.OnHealthChange.AddListener(_ => HealthM.Instance.Server_OnHealthChanged(peer, h));
            h.OnMaxHealthChange.AddListener(_ => HealthM.Instance.Server_OnHealthChanged(peer, h));
            _srvHooked.Add(h);
            Debug.Log($"[HealthTool] Server_HookOneHealth: Added event listeners for Health {h.GetInstanceID()}");
        }
        else
        {
            Debug.Log($"[HealthTool] Server_HookOneHealth: Health {h.GetInstanceID()} already hooked, skipping listeners");
        }

        // 1) 若服务器已缓存了该客户端"自报"的权威血量，先套用并广给其他客户端
        if (peer != null && _srvPendingHp.TryGetValue(peer, out var snap))
        {
            Debug.Log($"[HealthTool] Server_HookOneHealth: Applying pending HP for peer {peer} - max={snap.max}, cur={snap.cur}");
            HealthM.Instance.ApplyHealthAndEnsureBar(instance, snap.max, snap.cur);
            _srvPendingHp.Remove(peer);
            HealthM.Instance.Server_OnHealthChanged(peer, h);
            return;
        }

        // 2) 否则读取当前值；若 Max<=0（常见于克隆且 autoInit=false），用兜底 40f 起条并广播
        float max = 0f, cur = 0f;
        try
        {
            max = h.MaxHealth;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HealthTool] Server_HookOneHealth: Exception getting MaxHealth - {ex.Message}");
        }

        try
        {
            cur = h.CurrentHealth;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HealthTool] Server_HookOneHealth: Exception getting CurrentHealth - {ex.Message}");
        }

        Debug.Log($"[HealthTool] Server_HookOneHealth: Read health values - max={max}, cur={cur}");

        if (max <= 0f)
        {
            max = 40f;
            if (cur <= 0f) cur = max;
            Debug.Log($"[HealthTool] Server_HookOneHealth: Applied fallback values - max={max}, cur={cur}");
        }

        HealthM.Instance.ApplyHealthAndEnsureBar(instance, max, cur); // 会确保 showHealthBar + RequestHealthBar + 多帧重试
        HealthM.Instance.Server_OnHealthChanged(peer, h); // 立刻推一帧给"其他玩家"
    }


    public static void Client_HookSelfHealth()
    {
        // 检查网络服务是否可用且已连接
        if (Service == null || Service.remoteCharacters == null)
        {
            // Debug.Log("[HealthTool] Client_HookSelfHealth: NetService not available or not connected, skipping");
            return;
        }
        
        if (_cliHookedSelf) 
        {
            Debug.Log("[HealthTool] Client_HookSelfHealth: Already hooked, skipping");
            return;
        }
        
        Debug.Log("[HealthTool] Client_HookSelfHealth: Starting self health hook");
        var main = CharacterMainControl.Main;
        var h = main ? main.GetComponentInChildren<Health>(true) : null;
        if (!h) 
        {
            Debug.LogError("[HealthTool] Client_HookSelfHealth: No Health component found on main character");
            return;
        }

        _cbSelfHpChanged = _ => HealthM.Instance.Client_SendSelfHealth(h, false);
        _cbSelfMaxChanged = _ => HealthM.Instance.Client_SendSelfHealth(h, true);
        _cbSelfHurt = di =>
        {
            _cliLastSelfHurtAt = Time.time; // 记录受击时间
            try
            {
                _cliLastSelfHpLocal = h.CurrentHealth;
                Debug.Log($"[HealthTool] Client_HookSelfHealth: Self hurt detected - newHP={_cliLastSelfHpLocal}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HealthTool] Client_HookSelfHealth: Exception in hurt callback - {ex.Message}");
            }

            HealthM.Instance.Client_SendSelfHealth(h, true); // 受击当帧强制上报，跳过 20Hz 节流
        };
        _cbSelfDead = _ => HealthM.Instance.Client_SendSelfHealth(h, true);

        h.OnHealthChange.AddListener(_cbSelfHpChanged);
        h.OnMaxHealthChange.AddListener(_cbSelfMaxChanged);
        h.OnHurtEvent.AddListener(_cbSelfHurt);
        h.OnDeadEvent.AddListener(_cbSelfDead);

        _cliHookedSelf = true;
        Debug.Log($"[HealthTool] Client_HookSelfHealth: Successfully hooked self health - Health ID={h.GetInstanceID()}");

        // 初次钩上也主动发一次，作为双保险
        HealthM.Instance.Client_SendSelfHealth(h, true);
    }

    public static void Client_UnhookSelfHealth()
    {
        if (!_cliHookedSelf) 
        {
            Debug.Log("[HealthTool] Client_UnhookSelfHealth: Not hooked, skipping");
            return;
        }
        
        Debug.Log("[HealthTool] Client_UnhookSelfHealth: Starting unhook process");
        var main = CharacterMainControl.Main;
        var h = main ? main.GetComponentInChildren<Health>(true) : null;
        if (h)
        {
            if (_cbSelfHpChanged != null) h.OnHealthChange.RemoveListener(_cbSelfHpChanged);
            if (_cbSelfMaxChanged != null) h.OnMaxHealthChange.RemoveListener(_cbSelfMaxChanged);
            if (_cbSelfHurt != null) h.OnHurtEvent.RemoveListener(_cbSelfHurt);
            if (_cbSelfDead != null) h.OnDeadEvent.RemoveListener(_cbSelfDead);
            Debug.Log($"[HealthTool] Client_UnhookSelfHealth: Removed listeners from Health {h.GetInstanceID()}");
        }
        else
        {
            Debug.Log("[HealthTool] Client_UnhookSelfHealth: No Health component found during unhook");
        }

        _cliHookedSelf = false;
        _cbSelfHpChanged = _cbSelfMaxChanged = null;
        _cbSelfHurt = _cbSelfDead = null;
        Debug.Log("[HealthTool] Client_UnhookSelfHealth: Unhook completed");
    }

    // 绑定 Health⇄Character，修复"Health 没绑定角色"导致的 UI/Hidden 逻辑缺参
    public static void BindHealthToCharacter(Health h, CharacterMainControl cmc)
    {
        if (h == null)
        {
            Debug.LogError("[HealthTool] BindHealthToCharacter: Health is null");
            return;
        }
        
        Debug.Log($"[HealthTool] BindHealthToCharacter: Binding Health {h.GetInstanceID()} to Character {(cmc != null ? cmc.name : "null")}");
        try
        {
            FI_characterCached?.SetValue(h, cmc);
            FI_hasCharacter?.SetValue(h, true);
            Debug.Log($"[HealthTool] BindHealthToCharacter: Successfully bound Health {h.GetInstanceID()}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HealthTool] BindHealthToCharacter: Exception - {ex.Message}");
        }
    }
}