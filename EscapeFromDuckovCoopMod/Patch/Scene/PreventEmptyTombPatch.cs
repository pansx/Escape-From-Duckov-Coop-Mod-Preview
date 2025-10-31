using HarmonyLib;
using UnityEngine;
using ItemStatsSystem;

namespace EscapeFromDuckovCoopMod.Patch.Scene
{
    /// <summary>
    /// 额外的空坟墓防护补丁
    /// 防止在任何情况下创建空的坟墓
    /// </summary>
    [HarmonyPatch]
    internal static class PreventEmptyTombPatch
    {
        /// <summary>
        /// 如果有其他创建坟墓的路径，在这里拦截空坟墓的创建
        /// </summary>
        [HarmonyPatch(typeof(InteractableLootbox), "CreateFromItem")]
        [HarmonyPrefix]
        private static bool PreventEmptyTombCreation(
            Item item,
            Vector3 position,
            Quaternion rotation,
            bool moveToMainScene,
            InteractableLootbox prefab,
            bool filterDontDropOnDead,
            ref InteractableLootbox __result)
        {
            var mod = ModBehaviourF.Instance;
            if (mod == null || !mod.networkStarted) return true;

            // 检查是否是坟墓预制体
            if (prefab != null && IsTombPrefab(prefab))
            {
                // 检查物品是否为空或无效
                if (item == null || IsEmptyItem(item))
                {
                    Debug.Log($"[COOP] 阻止创建空坟墓 - prefab: {prefab.name}, item: {item?.name ?? "null"}");
                    __result = null;
                    return false;
                }
                
                Debug.Log($"[COOP] 允许创建有物品的坟墓 - prefab: {prefab.name}, item: {item.name}");
            }

            return true;
        }

        /// <summary>
        /// 检查预制体是否是坟墓类型
        /// </summary>
        private static bool IsTombPrefab(InteractableLootbox prefab)
        {
            if (prefab == null) return false;
            
            var name = prefab.name.ToLower();
            return name.Contains("tomb") || name.Contains("grave") || name.Contains("墓");
        }

        /// <summary>
        /// 检查物品是否为空
        /// </summary>
        private static bool IsEmptyItem(Item item)
        {
            if (item == null) return true;
            
            // 检查物品是否有有效的子物品
            if (item.transform.childCount == 0) return true;
            
            // 可以添加更多的空物品检查逻辑
            return false;
        }
    }
}