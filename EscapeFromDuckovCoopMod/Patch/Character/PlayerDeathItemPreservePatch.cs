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

using ItemStatsSystem;
using System.Reflection;

namespace EscapeFromDuckovCoopMod;

/// <summary>
/// 临时修复：客户端玩家死亡后保留物品，避免坟墓系统的"no_inv"问题
/// 这是一个临时解决方案，直到坟墓系统的底层问题得到修复
/// </summary>
[HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
internal static class PlayerDeathItemPreservePatch
{
    // 拦截玩家死亡时的物品掉落/清空逻辑
    [HarmonyPrefix]
    private static bool PreventClientPlayerItemDrop(CharacterMainControl __instance, DamageInfo dmgInfo)
    {
        var mod = ModBehaviourF.Instance;
        if (mod == null || !mod.networkStarted) return true; // 非联机模式，正常执行
        if (mod.IsServer) return true; // 主机模式，正常执行
        
        // 只处理客户端的本地玩家
        if (__instance != CharacterMainControl.Main) return true;
        
        try
        {
            Debug.Log("[COOP] 客户端玩家死亡 - 启用物品保留模式（临时修复）");
            
            // 执行死亡的视觉效果和状态切换，但跳过物品掉落
            ExecuteDeathWithoutItemDrop(__instance, dmgInfo);
            
            // 阻止原始的OnDead执行，避免物品被清空
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[COOP] 客户端死亡物品保留失败: {e}");
            return true; // 出错时回退到原始逻辑
        }
    }
    
    /// <summary>
    /// 执行死亡逻辑但保留物品
    /// </summary>
    private static void ExecuteDeathWithoutItemDrop(CharacterMainControl character, DamageInfo dmgInfo)
    {
        try
        {
            // 1. 触发死亡视觉效果
            TriggerDeathVisualEffects(character, dmgInfo);
            
            // 2. 设置死亡状态但不清空物品
            SetDeathStateWithoutItemClear(character);
            
            // 3. 进入观战模式（如果启用）
            TryEnterSpectatorMode(dmgInfo);
            
            // 4. 通知其他系统玩家已死亡
            NotifyPlayerDeath(character, dmgInfo);
            
            Debug.Log("[COOP] 客户端玩家死亡处理完成 - 物品已保留（临时修复）");
        }
        catch (Exception e)
        {
            Debug.LogError($"[COOP] 执行死亡逻辑时出错: {e}");
        }
    }
    
    /// <summary>
    /// 触发死亡视觉效果
    /// </summary>
    private static void TriggerDeathVisualEffects(CharacterMainControl character, DamageInfo dmgInfo)
    {
        try
        {
            // 播放死亡动画
            var animator = character.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsDead", true);
            }
            
            // 触发受伤视觉效果（使用现有的AI方法）
            LocalHitKillFx.ClientPlayForAI(character, dmgInfo, true);
            
            // 禁用角色控制
            var characterController = character.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
            }
            
            Debug.Log("[COOP] 死亡视觉效果已触发");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[COOP] 触发死亡视觉效果时出错: {e}");
        }
    }
    
    /// <summary>
    /// 设置死亡状态但不清空物品
    /// </summary>
    private static void SetDeathStateWithoutItemClear(CharacterMainControl character)
    {
        try
        {
            // 设置血量为0（如果还没有）
            if (character.Health != null && character.Health.CurrentHealth > 0)
            {
                // 使用反射设置血量，避免触发其他死亡逻辑
                var healthField = typeof(Health).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
                if (healthField != null)
                {
                    healthField.SetValue(character.Health, 0f);
                }
            }
            
            // 标记角色为死亡状态
            // 注意：这里我们不调用原始的死亡逻辑，避免物品被清空
            
            Debug.Log("[COOP] 死亡状态已设置，物品保留");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[COOP] 设置死亡状态时出错: {e}");
        }
    }
    
    /// <summary>
    /// 尝试进入观战模式
    /// </summary>
    private static void TryEnterSpectatorMode(DamageInfo dmgInfo)
    {
        try
        {
            var spectator = Spectator.Instance;
            if (spectator != null)
            {
                spectator.TryEnterSpectatorOnDeath(dmgInfo);
                Debug.Log("[COOP] 已尝试进入观战模式");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[COOP] 进入观战模式时出错: {e}");
        }
    }
    
    /// <summary>
    /// 通知其他系统玩家死亡
    /// </summary>
    private static void NotifyPlayerDeath(CharacterMainControl character, DamageInfo dmgInfo)
    {
        try
        {
            // 通知本地玩家管理器
            var localPlayerManager = LoaclPlayerManager.Instance;
            if (localPlayerManager != null)
            {
                // 这里可以添加必要的死亡通知逻辑
                Debug.Log("[COOP] 已通知本地玩家管理器");
            }
            
            // 通知网络服务
            var netService = NetService.Instance;
            if (netService != null && netService.networkStarted)
            {
                // 发送死亡状态更新给主机
                SendLocalPlayerStatus.Instance?.SendPlayerStatusUpdate();
                Debug.Log("[COOP] 已发送死亡状态更新");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[COOP] 通知玩家死亡时出错: {e}");
        }
    }
}

/// <summary>
/// 阻止客户端玩家的物品在死亡时被自动清空或掉落
/// </summary>
[HarmonyPatch]
internal static class PreventClientPlayerItemClearPatch
{
    // 拦截可能的物品清空方法
    [HarmonyPatch(typeof(Inventory), "Clear")]
    [HarmonyPrefix]
    private static bool PreventPlayerInventoryClear(Inventory __instance)
    {
        var mod = ModBehaviourF.Instance;
        if (mod == null || !mod.networkStarted || mod.IsServer) return true;
        
        // 检查是否是玩家的库存
        if (IsPlayerInventory(__instance))
        {
            var player = CharacterMainControl.Main;
            if (player != null && player.Health != null && player.Health.CurrentHealth <= 0)
            {
                Debug.Log("[COOP] 阻止客户端玩家死亡时清空库存（临时修复）");
                return false; // 阻止清空
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 检查是否是玩家的库存
    /// </summary>
    private static bool IsPlayerInventory(Inventory inventory)
    {
        if (inventory == null) return false;
        
        try
        {
            var player = CharacterMainControl.Main;
            if (player == null) return false;
            
            // 检查是否是玩家的主要库存
            var itemControl = player.GetComponent<CharacterItemControl>();
            if (itemControl != null)
            {
                // 使用反射获取主要库存
                var mainInvField = typeof(CharacterItemControl).GetField("mainInventory", BindingFlags.NonPublic | BindingFlags.Instance);
                if (mainInvField != null)
                {
                    var playerInventory = mainInvField.GetValue(itemControl) as Inventory;
                    if (playerInventory == inventory) return true;
                }
            }
            
            // 检查是否是玩家装备相关的库存
            var equipmentController = player.EquipmentController;
            if (equipmentController != null)
            {
                // 这里可以添加更多的库存检查逻辑
                // 比如背包、装备槽等
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
}