namespace EscapeFromDuckovCoopMod;

/// <summary>
/// 阻止客户端玩家死亡时掉落所有物品
/// 参考NoDeathDrops模组的实现方式
/// </summary>
[HarmonyPatch(typeof(CharacterMainControl), "DropAllItems")]
internal static class PreventClientPlayerDropAllItemsPatch
{
    [HarmonyPrefix]
    private static bool PreventPlayerDropAllItems(CharacterMainControl __instance)
    {
        var mod = ModBehaviourF.Instance;
        if (mod == null || !mod.networkStarted) 
        {
            Debug.Log("[COOP] DropAllItems - 非联机模式，允许正常执行");
            return true;
        }
        
        // 只在客户端阻止玩家掉落物品
        if (!mod.IsServer && __instance == CharacterMainControl.Main)
        {
            Debug.Log("[COOP] 阻止客户端玩家死亡时掉落所有物品");
            return false; // 阻止掉落
        }
        
        // 服务端或其他角色的掉落
        if (mod.IsServer)
        {
            Debug.Log("[COOP] 服务端 DropAllItems - 允许正常执行");
        }
        else
        {
            Debug.Log("[COOP] 客户端其他角色 DropAllItems - 允许正常执行");
        }
        
        return true; // 允许其他情况正常执行
    }
}

/// <summary>
/// 阻止客户端玩家死亡时销毁所有物品
/// </summary>
[HarmonyPatch(typeof(CharacterMainControl), "DestroyAllItem")]
internal static class PreventClientPlayerDestroyAllItemsPatch
{
    [HarmonyPrefix]
    private static bool PreventPlayerDestroyAllItems(CharacterMainControl __instance)
    {
        var mod = ModBehaviourF.Instance;
        if (mod == null || !mod.networkStarted) 
        {
            Debug.Log("[COOP] DestroyAllItem - 非联机模式，允许正常执行");
            return true;
        }
        
        // 只在客户端阻止玩家销毁物品
        if (!mod.IsServer && __instance == CharacterMainControl.Main)
        {
            Debug.Log("[COOP] 阻止客户端玩家死亡时销毁所有物品");
            return false; // 阻止销毁
        }
        
        // 服务端或其他角色的销毁
        if (mod.IsServer)
        {
            Debug.Log("[COOP] 服务端 DestroyAllItem - 允许正常执行");
        }
        else
        {
            Debug.Log("[COOP] 客户端其他角色 DestroyAllItem - 允许正常执行");
        }
        
        return true; // 允许其他情况正常执行
    }
}

/// <summary>
/// 阻止客户端玩家的OnDead方法执行，防止物品被清空
/// 这是最关键的补丁，因为OnDead方法中包含了物品清空逻辑
/// </summary>
[HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
internal static class PreventClientPlayerOnDeadPatch
{
    [HarmonyPrefix]
    private static bool PreventPlayerOnDead(CharacterMainControl __instance, DamageInfo dmgInfo)
    {
        var mod = ModBehaviourF.Instance;
        if (mod == null || !mod.networkStarted) 
        {
            Debug.Log("[COOP] OnDead - 非联机模式，允许正常执行");
            return true;
        }
        
        // 只在客户端阻止玩家的OnDead
        if (!mod.IsServer && __instance == CharacterMainControl.Main)
        {
            Debug.Log("[COOP] 阻止客户端玩家OnDead执行，保留物品");
            
            // 手动触发观战模式（如果有队友）
            try
            {
                if (Spectator.Instance != null)
                {
                    Spectator.Instance.TryEnterSpectatorOnDeath(dmgInfo);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[COOP] 触发观战模式失败: {e}");
            }
            
            return false; // 阻止OnDead执行
        }
        
        // 服务端或其他角色的OnDead
        if (mod.IsServer)
        {
            Debug.Log("[COOP] 服务端 OnDead - 允许正常执行");
        }
        else
        {
            Debug.Log("[COOP] 客户端其他角色 OnDead - 允许正常执行");
        }
        
        return true; // 允许其他情况正常执行
    }
}

/// <summary>
/// 阻止生成墓碑，防止物品被转移到墓碑中
/// 参考NoDeathDrops模组的实现
/// </summary>
[HarmonyPatch(typeof(LevelConfig), "get_SpawnTomb")]
internal static class PreventTombSpawnPatch
{
    [HarmonyPrefix]
    private static bool PreventTombSpawn(ref bool __result)
    {
        var mod = ModBehaviourF.Instance;
        if (mod == null || !mod.networkStarted) 
        {
            Debug.Log("[COOP] SpawnTomb - 非联机模式，允许正常执行");
            return true;
        }
        
        // 在联机模式下阻止生成墓碑
        Debug.Log("[COOP] 阻止生成墓碑，防止物品被转移");
        __result = false;
        return false; // 阻止原方法执行
    }
}

/// <summary>
/// 阻止客户端的死亡事件补发，这是联机模组触发死亡的另一个路径
/// </summary>
[HarmonyPatch(typeof(LoaclPlayerManager), "Client_EnsureSelfDeathEvent")]
internal static class PreventClientEnsureSelfDeathEventPatch
{
    [HarmonyPrefix]
    private static bool PreventEnsureSelfDeathEvent(LoaclPlayerManager __instance, Health h, CharacterMainControl cmc)
    {
        var mod = ModBehaviourF.Instance;
        if (mod == null || !mod.networkStarted) 
        {
            Debug.Log("[COOP] Client_EnsureSelfDeathEvent - 非联机模式，允许正常执行");
            return true;
        }
        
        // 只在客户端阻止本地玩家的死亡事件补发
        if (!mod.IsServer && cmc == CharacterMainControl.Main)
        {
            Debug.Log("[COOP] 阻止客户端死亡事件补发，保留物品");
            return false; // 阻止死亡事件补发
        }
        
        Debug.Log("[COOP] Client_EnsureSelfDeathEvent - 允许正常执行");
        return true; // 允许其他情况正常执行
    }
}