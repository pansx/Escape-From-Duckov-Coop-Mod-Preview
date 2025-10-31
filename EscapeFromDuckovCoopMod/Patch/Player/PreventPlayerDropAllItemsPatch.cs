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

using HarmonyLib;
using UnityEngine;

namespace EscapeFromDuckovCoopMod.Patch.Player
{
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
}