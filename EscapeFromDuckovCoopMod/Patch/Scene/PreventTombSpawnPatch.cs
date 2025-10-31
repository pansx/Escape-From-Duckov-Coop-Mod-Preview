using HarmonyLib;
using UnityEngine;

namespace EscapeFromDuckovCoopMod.Patch.Scene
{
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

            // 在联机模式下阻止生成墓碑，防止客机本地创建空坟墓
            Debug.Log($"[COOP] 阻止生成墓碑，防止物品被转移到墓碑中 - IsServer: {mod.IsServer}");
            __result = false;
            return false; // 阻止原方法执行
        }
    }
}