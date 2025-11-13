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

// ============================================================================
// 文件: InventoryPatch.cs (带详细文档版本)
// 描述: 库存系统的 Harmony 补丁集合，用于实现联机模式下的战利品同步
// 
// 主要功能:
// 1. 拦截客户端对公共战利品容器的本地操作（AddAt/RemoveAt/Sort）
// 2. 将客户端操作转换为网络请求发送给主机
// 3. 主机执行操作后广播状态给所有客户端
// 4. 处理物品拾取、放置、拆分、合并、武器插槽等复杂场景
// 5. 防止客户端本地修改导致的状态不一致（幽灵物品问题）
// 
// 核心设计原则:
// - 客户端只发请求，不直接修改战利品容器
// - 主机是唯一的权威状态源
// - 所有状态变更由主机广播同步
// 
// 补丁总数: 17 个
// ============================================================================

using System.Reflection;
using Duckov.UI;
using ItemStatsSystem;
using Object = UnityEngine.Object;
using EscapeFromDuckovCoopMod.Net;  // 引入智能发送扩展方法

namespace EscapeFromDuckovCoopMod;

/// <summary>
/// 补丁 1/17: Inventory.AddAt - 拦截从公共战利品容器取物品
/// 
/// 触发时机: 玩家从战利品箱拖拽物品到背包/装备栏
/// 
/// 工作流程:
/// 1. 检测物品来源是否为公共战利品容器
/// 2. 拦截本地操作，发送网络请求给主机
/// 3. 等待主机处理并广播结果
/// 
/// 防止问题: 客户端本地修改导致状态不一致
/// </summary>
[HarmonyPatch(typeof(Inventory), "AddAt")]
internal static class Patch_Inventory_AddAt_FromLoot
{
    /// <summary>
    /// 前置拦截: 在 AddAt 执行前检查并拦截
    /// </summary>
    /// <param name="__instance">目标库存（物品要放入的位置）</param>
    /// <param name="item">要添加的物品</param>
    /// <param name="atPosition">目标位置索引</param>
    /// <param name="__result">返回值（是否成功）</param>
    /// <returns>true=继续执行原方法，false=跳过原方法</returns>
    private static bool Prefix(Inventory __instance, Item item, int atPosition, ref bool __result)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/当前是主机 -> 放行
        if (m == null || !m.networkStarted || m.IsServer) return true;
        // 条件检查: 正在应用服务器快照（同步状态中）-> 放行
        if (COOPManager.LootNet._applyingLootState) return true;

        // 获取物品的源库存（物品当前所在的容器）
        var srcInv = item ? item.InInventory : null;
        // 条件检查: 源库存为空 或 源库存就是目标库存（同容器内移动）-> 放行
        if (srcInv == null || srcInv == __instance) return true;

        // ★ 核心逻辑: 只拦截从公共战利品容器取物品的操作
        if (LootboxDetectUtil.IsLootboxInventory(srcInv) && !LootboxDetectUtil.IsPrivateInventory(srcInv))
        {
            // 获取物品在源容器中的位置索引
            var srcPos = srcInv.GetIndex(item);

            // 进入保护区: 增加递归深度计数，防止重复拦截
            LootUiGuards.InLootAddAtDepth++;
            try
            {
                // 发送"取物品"网络请求给主机
                // 参数: 源容器, 源位置, 目标容器, 目标位置, 回调
                COOPManager.LootNet.Client_SendLootTakeRequest(srcInv, srcPos, __instance, atPosition, null);
            }
            finally
            {
                // 退出保护区: 减少递归深度计数
                LootUiGuards.InLootAddAtDepth--;
            }

            // 告诉调用者操作已受理（实际由主机处理）
            __result = true;
            // 跳过原方法执行（不在本地修改库存）
            return false;
        }

        // 其他情况放行原方法
        return true;
    }
}

/// <summary>
/// 补丁 2/17: Inventory.AddAt - 拦截武器插槽附件到私有背包
/// 
/// 触发时机: 玩家从战利品箱中的武器上拆下附件到自己背包
/// 例如: 从箱子里的 AK 上拆下瞄准镜到背包
/// 
/// 问题背景: 原生游戏会直接执行 AddAt，但附件仍有"父物体"引用，导致报错
/// 
/// 解决方案:
/// 1. 拦截这种操作
/// 2. 发送"卸载插槽到背包"的网络请求
/// 3. 主机先卸载，再通过 TAKE_OK 驱动本地落到指定位置
/// 
/// 优先级: Priority.First（最先执行，优先于其他 AddAt 补丁）
/// </summary>
[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddAt), typeof(Item), typeof(int))]
[HarmonyPriority(Priority.First)]
internal static class Patch_Inventory_AddAt_SlotToPrivate_Reroute
{
    /// <summary>
    /// 前置拦截: 拦截"插槽附件 -> 私有背包"的操作
    /// </summary>
    /// <param name="__instance">目标库存（玩家背包/装备栏）</param>
    /// <param name="item">要添加的物品（附件）</param>
    /// <param name="atPosition">目标位置索引</param>
    /// <param name="__result">返回值（是否成功）</param>
    /// <returns>true=继续执行原方法，false=跳过原方法</returns>
    private static bool Prefix(Inventory __instance, Item item, int atPosition, ref bool __result)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/当前是主机 -> 放行
        if (m == null || !m.networkStarted || m.IsServer) return true;

        // 条件检查: 只拦截"落到私有库存"的情况（玩家背包/身上/宠物包）
        if (!LootboxDetectUtil.IsPrivateInventory(__instance)) return true;

        // 条件检查: 只拦截"仍插在武器槽位里的附件"
        var slot = item ? item.PluggedIntoSlot : null;
        // 如果物品不在插槽里，放行
        if (slot == null) return true;

        // 找到最外层主件（武器）
        // 例如: 瞄准镜 -> 导轨 -> AK，需要找到 AK
        var master = slot.Master;
        while (master && master.PluggedIntoSlot != null)
            master = master.PluggedIntoSlot.Master;

        // 源容器: 优先使用 master.InInventory；拿不到时兜底用当前 LootView 的容器
        var srcLoot = master ? master.InInventory : null;
        if (!srcLoot)
            try
            {
                // 尝试从 LootView 获取当前打开的战利品容器
                var lv = LootView.Instance;
                if (lv) srcLoot = lv.TargetInventory;
            }
            catch
            {
                // 忽略异常（可能 LootView 未初始化）
            }

        // 源容器必须是"公共战利品容器"
        if (!srcLoot || !LootboxDetectUtil.IsLootboxInventory(srcLoot) || LootboxDetectUtil.IsPrivateInventory(srcLoot))
        {
            // 为了不触发原生的"父物体"报错，这里直接拦下，不执行原方法
            Debug.LogWarning($"[Coop] AddAt(private, slot->backpack) srcLoot not found; block local AddAt for '{item?.name}'");
            // 返回失败
            __result = false;
            // 跳过原方法
            return false;
        }

        // 记录日志: 发送卸载请求
        Debug.Log($"[Coop] AddAt(private, slot->backpack) -> send UNPLUG(takeToBackpack), destPos={atPosition}");
        // 让主机先卸下附件，再由 TAKE_OK 消息驱动本地落到 atPosition
        // 参数: 源容器, 主件, 插槽键, 目标容器, 目标位置
        COOPManager.LootNet.Client_RequestSlotUnplugToBackpack(srcLoot, master, slot.Key, __instance, atPosition);

        // 本地视为已受理
        __result = true;
        // 阻止原生 AddAt（否则就会出现"仍有父物体"的报错）
        return false;
    }
}

/// <summary>
/// 补丁 3/17: Inventory.AddAt - 拦截客户端对战利品容器的本地 AddAt 操作
/// 
/// 触发时机: 客户端尝试直接修改战利品容器内容
/// 
/// 处理的三种场景:
/// A) 同容器内换位: 改为 TAKE -> PUT 两段式
/// B) 其他库存 -> 容器: 发送 TAKE 请求（携带目的地）
/// C) 插槽 -> 容器: 发送 UNPLUG 请求
/// 
/// 核心目标: 防止客户端本地修改战利品容器，所有操作必须通过主机处理
/// </summary>
[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddAt), typeof(Item), typeof(int))]
internal static class Patch_Inventory_AddAt_BlockLocalInLoot
{
    /// <summary>
    /// 前置拦截: 拦截客户端对战利品容器的 AddAt 操作
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="item">要添加的物品</param>
    /// <param name="atPosition">目标位置索引</param>
    /// <param name="__result">返回值（是否成功）</param>
    /// <returns>true=继续执行原方法，false=跳过原方法</returns>
    private static bool Prefix(Inventory __instance, Item item, int atPosition, ref bool __result)
    {
        // 获取 Mod 实例
        var mod = ModBehaviourF.Instance;
        // 条件检查: 不是客户端 -> 放行
        if (mod == null || !mod.IsClient) return true;

        // 条件检查: 目标是私有库存 -> 放行
        if (LootboxDetectUtil.IsPrivateInventory(__instance)) return true;

        // 条件检查: 目标不是当前打开的战利品容器 -> 放行
        if (!LootManager.IsCurrentLootInv(__instance)) return true;
        // 条件检查: 正在应用服务器快照 -> 放行
        if (COOPManager.LootNet.ApplyingLootState) return true;

        // 进入保护区: 增加递归深度计数
        LootUiGuards.InLootAddAtDepth++;
        try
        {
            // 获取物品的源库存
            var srcInv = item ? item.InInventory : null;

            // === A) 同容器内换位 / 挪动: 改为 TAKE -> PUT 两段式 ===
            if (ReferenceEquals(srcInv, __instance))
            {
                // 获取物品在源容器中的位置
                var srcPos = __instance.GetIndex(item);
                // 如果源位置和目标位置相同，直接视为成功
                if (srcPos == atPosition)
                {
                    __result = true;
                    return false;
                }

                // 如果源位置无效，返回失败
                if (srcPos < 0)
                {
                    __result = false;
                    return false;
                }

                // 1) 发送 TAKE 请求（不带目的地）
                var tk = COOPManager.LootNet.Client_SendLootTakeRequest(__instance, srcPos, null, -1, null);

                // 2) 记录"待重排"，让 TAKE_OK 到达后立刻对同一容器发 PUT(atPosition)
                LootManager.Instance.NoteLootReorderPending(tk, __instance, atPosition);

                // 告诉上层: 已受理，别回退
                __result = true;
                // 不执行原 AddAt（避免本地改内容）
                return false;
            }

            // === B) 其他容器 -> 当前容器: 发送 TAKE 请求（携带目的地） ===
            if (LootManager.IsCurrentLootInv(srcInv))
            {
                // 获取物品在源容器中的位置
                var srcPos = srcInv.GetIndex(item);
                // 如果源位置无效，返回失败
                if (srcPos < 0)
                {
                    __result = false;
                    return false;
                }

                // 发送 TAKE 请求（携带目的地）
                COOPManager.LootNet.Client_SendLootTakeRequest(srcInv, srcPos, __instance, atPosition, null);
                // 已受理
                __result = true;
                // 跳过原方法
                return false;
            }

            // === C) 插槽 -> 容器（同容器）: 拦截"从容器内武器插槽卸下到容器格子" ===
            if (__instance && LootboxDetectUtil.IsLootboxInventory(__instance) && !LootboxDetectUtil.IsPrivateInventory(__instance))
            {
                // 获取物品所在的插槽
                var slot = item ? item.PluggedIntoSlot : null;
                if (slot != null)
                {
                    // 找到这个槽位所属武器的"根主件"，以及它所在的容器
                    var master = slot.Master;
                    while (master && master.PluggedIntoSlot != null) master = master.PluggedIntoSlot.Master;
                    var masterLoot = master ? master.InInventory : null;

                    // 如果主件和目标容器是同一个: 这是"拆附件放回容器"的场景
                    if (masterLoot == __instance)
                    {
                        // 记录日志
                        Debug.Log("[Coop] AddAt@Loot (slot->loot) -> send UNPLUG(takeToBackpack=false)");
                        try
                        {
                            // 再次增加递归深度（双重保护）
                            LootUiGuards.InLootAddAtDepth++;
                        }
                        catch
                        {
                            // 忽略异常
                        }

                        try
                        {
                            // 走"旧负载+追加字段"的新重载: takeToBackpack=false
                            // 参数: 容器, 主件, 插槽键, 是否到背包, 目标位置
                            COOPManager.LootNet.Client_RequestLootSlotUnplug(__instance, slot.Master, slot.Key, false, 0);
                        }
                        finally
                        {
                            // 退出保护区
                            LootUiGuards.InLootAddAtDepth--;
                        }

                        // 认为成功，等待主机的 LOOT_STATE 对齐UI
                        __result = true;
                        // 阻断本地 AddAt，避免出现本地就先放进去
                        return false;
                    }
                }
            }

            // 其它来源 -> 容器: 交给 PUT 拦截处理
            return true;
        }
        finally
        {
            // 退出保护区: 减少递归深度计数
            LootUiGuards.InLootAddAtDepth--;
        }
    }
}

/// <summary>
/// 补丁 4/17: Inventory.RemoveAt - 拦截客户端对战利品容器的本地 RemoveAt 操作
/// 
/// 触发时机: 客户端尝试从战利品容器中移除物品
/// 
/// 核心目标: 阻止客户端直接调用 RemoveAt，所有移除操作必须通过主机的 TAKE 请求处理
/// 
/// 技术细节: 使用反射精确锁定 RemoveAt(int, out Item) 重载，第二个参数是 out Item，用 ref 接收
/// </summary>
[HarmonyPatch(typeof(Inventory))]
internal static class Patch_Inventory_RemoveAt_BlockLocalInLoot
{
    /// <summary>
    /// 使用反射精确锁定 RemoveAt(int, out Item) 这个重载
    /// 
    /// 为什么需要这样做:
    /// - Inventory 有多个 RemoveAt 重载
    /// - 我们只想拦截 RemoveAt(int, out Item) 这个版本
    /// - 使用 TargetMethod 可以精确指定要补丁的方法
    /// </summary>
    /// <returns>要补丁的方法信息</returns>
    private static MethodBase TargetMethod()
    {
        // 获取 Inventory 类型
        var tInv = typeof(Inventory);
        // 创建 Item 的 ByRef 类型（对应 out Item 参数）
        var tItemByRef = typeof(Item).MakeByRefType();
        // 返回 RemoveAt(int, out Item) 方法
        return AccessTools.Method(tInv, "RemoveAt", new[] { typeof(int), tItemByRef });
    }

    /// <summary>
    /// 前置拦截: 拦截客户端对战利品容器的 RemoveAt 操作
    /// 
    /// 注意: 第二个参数是 out Item，在 Harmony 中用 ref 接收
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="position">要移除的位置索引</param>
    /// <param name="__1">out Item 参数（被移除的物品）</param>
    /// <param name="__result">返回值（是否成功）</param>
    /// <returns>true=继续执行原方法，false=跳过原方法</returns>
    private static bool Prefix(Inventory __instance, int position, ref Item __1, ref bool __result)
    {
        // 获取 Mod 实例
        var mod = ModBehaviourF.Instance;
        // 条件检查: 不是客户端 -> 放行
        if (mod == null || !mod.IsClient) return true;

        // 条件检查: 目标是私有库存 -> 放行
        if (LootboxDetectUtil.IsPrivateInventory(__instance)) return true;

        // 检查是否是当前打开的战利品容器
        var isLootInv = false;
        try
        {
            var lv = LootView.Instance;
            // 使用 ReferenceEquals 确保是同一个对象引用
            isLootInv = lv && __instance && ReferenceEquals(__instance, lv.TargetInventory);
        }
        catch
        {
            // 忽略异常（可能 LootView 未初始化）
        }

        // 条件检查: 不是战利品容器 -> 放行
        if (!isLootInv) return true;
        // 条件检查: 是私有库存 -> 放行（双重检查）
        if (LootboxDetectUtil.IsPrivateInventory(__instance)) return true;
        // 条件检查: 正在应用服务器快照 -> 放行（UI 刷新需要）
        if (COOPManager.LootNet.ApplyingLootState) return true;
        // 拦截操作: 设置 out 参数为 null
        __1 = null;
        // 返回失败
        __result = false;
        // 跳过原方法
        return false;

        // 注意: 下面的代码永远不会执行（上面已经 return false）
        // 这是原代码的冗余部分，保留以保持与原文件一致
        // 应用服务器快照期间允许 RemoveAt（UI 刷新），其余时间一律拦截
        if (COOPManager.LootNet.ApplyingLootState) return true;

        __1 = null;
        __result = false;
        return false;
    }
}

/// <summary>
/// 补丁 5/17: Inventory.NotifyContentChanged - 处理物品拾取的网络同步
/// 
/// 触发时机: 玩家从地面拾取物品（掉落物）
/// 
/// 工作流程:
/// 1. 检测物品是否是网络掉落物（带 NetDropTag）
/// 2. 客户端: 发送拾取请求给主机
/// 3. 主机: 销毁掉落物并广播给所有客户端
/// 
/// 技术细节:
/// - 使用近场检测（OverlapSphere）查找最近的掉落物
/// - 支持物品堆叠合并的情况（引用不同但位置相同）
/// </summary>
[HarmonyPatch(typeof(Inventory), "NotifyContentChanged")]
public static class Patch_Inventory_NotifyContentChanged
{
    // 拾取半径: 2.5 米（可根据手感调整）
    private const float PICK_RADIUS = 2.5f;
    // 查询触发器交互模式: 包含触发器
    private const QueryTriggerInteraction QTI = QueryTriggerInteraction.Collide;
    // 层级掩码: 所有层级
    private const int LAYER_MASK = ~0;
    // 层级掩码（任意）: 所有层级
    private const int LAYER_MASK_ANY = ~0;

    // 在主角附近找最近的带 NetDropTag 的拾取体
    // 缓冲区: 最多检测 64 个碰撞体
    private static readonly Collider[] _nearbyBuf = new Collider[64];

    /// <summary>
    /// 后置拦截: 在 NotifyContentChanged 执行后处理拾取同步
    /// </summary>
    /// <param name="__instance">发生变化的库存</param>
    /// <param name="item">新增的物品</param>
    private static void Postfix(Inventory __instance, Item item)
    {
        // 获取 Mod 实例
        var mod = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/物品为空 -> 直接返回
        if (mod == null || !mod.networkStarted || item == null) return;

        // 条件检查: 正在应用战利品状态 -> 跳过（避免重复处理）
        if (COOPManager.LootNet._applyingLootState) return;

        // 条件检查: 是战利品容器且不是私有库存 -> 跳过（不处理容器内物品变化）
        if (LootboxDetectUtil.IsLootboxInventory(__instance) && !LootboxDetectUtil.IsPrivateInventory(__instance))
            return;

        // --- 客户端逻辑 ---
        if (!mod.IsServer)
        {
            // A) 引用命中（少见: 非合并堆叠的情况）
            // 尝试在客户端掉落物字典中找到这个物品
            if (TryFindId(COOPManager.ItemHandle.clientDroppedItems, item, out var cid))
            {
                // 销毁本地的掉落物代理（ActiveAgent）
                LocalDestroyAgent(item);
                // 发送拾取请求给主机
                SendPickupReq(mod, cid);
                return;
            }

            // B) 合并堆叠: 引用不同，用近场 NetDropTag 反查
            // 例如: 拾取 5 个子弹，合并到背包里的 10 个子弹
            if (TryFindNearestTaggedId(out var nearId))
            {
                // 根据 ID 销毁本地掉落物
                LocalDestroyAgentById(COOPManager.ItemHandle.clientDroppedItems, nearId);
                // 发送拾取请求
                SendPickupReq(mod, nearId);
            }

            return;
        }

        // --- 主机逻辑 ---
        // A) 引用命中: 在主机掉落物字典中找到这个物品
        if (TryFindId(COOPManager.ItemHandle.serverDroppedItems, item, out var sid))
        {
            // 主机销毁掉落物并广播
            ServerDespawn(mod, sid);
            return;
        }

        // B) 合并堆叠: 用近场 NetDropTag 反查
        if (TryFindNearestTaggedId(out var nearSid)) ServerDespawn(mod, nearSid);
    }

    /// <summary>
    /// 发送拾取请求给主机
    /// </summary>
    /// <param name="mod">Mod 实例</param>
    /// <param name="id">掉落物 ID</param>
    private static void SendPickupReq(ModBehaviourF mod, uint id)
    {
        // 获取网络写入器
        var w = mod.writer;
        // 重置写入器
        w.Reset();
        // 写入操作码: 物品拾取请求
        w.Put((byte)Op.ITEM_PICKUP_REQUEST);
        // 写入掉落物 ID
        w.Put(id);
        // 发送给主机（使用智能发送: 根据操作码选择传输方式）
        mod.connectedPeer?.SendSmart(w, Op.ITEM_PICKUP_REQUEST);
    }

    /// <summary>
    /// 主机: 销毁掉落物并广播给所有客户端
    /// </summary>
    /// <param name="mod">Mod 实例</param>
    /// <param name="id">掉落物 ID</param>
    private static void ServerDespawn(ModBehaviourF mod, uint id)
    {
        // 如果掉落物存在，销毁本地代理
        if (COOPManager.ItemHandle.serverDroppedItems.TryGetValue(id, out var it) && it != null)
            LocalDestroyAgent(it);
        // 从字典中移除
        COOPManager.ItemHandle.serverDroppedItems.Remove(id);

        // 广播给所有客户端
        var w = mod.writer;
        w.Reset();
        // 写入操作码: 物品消失
        w.Put((byte)Op.ITEM_DESPAWN);
        // 写入掉落物 ID
        w.Put(id);
        // 广播给所有客户端
        mod.netManager.SendSmart(w, Op.ITEM_DESPAWN);
    }

    /// <summary>
    /// 销毁物品的 ActiveAgent（地面掉落物的 GameObject）
    /// </summary>
    /// <param name="it">物品</param>
    private static void LocalDestroyAgent(Item it)
    {
        try
        {
            // 获取物品的 ActiveAgent（地面掉落物）
            var ag = it.ActiveAgent;
            // 如果存在，销毁 GameObject
            if (ag && ag.gameObject) Object.Destroy(ag.gameObject);
        }
        catch
        {
            // 忽略异常（可能已被销毁）
        }
    }

    /// <summary>
    /// 根据 ID 从字典中查找物品并销毁其 ActiveAgent
    /// </summary>
    /// <param name="dict">掉落物字典</param>
    /// <param name="id">掉落物 ID</param>
    private static void LocalDestroyAgentById(Dictionary<uint, Item> dict, uint id)
    {
        // 如果找到物品，销毁其 ActiveAgent
        if (dict.TryGetValue(id, out var it) && it != null) LocalDestroyAgent(it);
    }

    /// <summary>
    /// 在字典中查找物品的 ID（通过引用比较）
    /// </summary>
    /// <param name="dict">掉落物字典</param>
    /// <param name="it">物品</param>
    /// <param name="id">输出: 找到的 ID</param>
    /// <returns>是否找到</returns>
    private static bool TryFindId(Dictionary<uint, Item> dict, Item it, out uint id)
    {
        // 遍历字典
        foreach (var kv in dict)
            // 使用 ReferenceEquals 确保是同一个对象
            if (ReferenceEquals(kv.Value, it))
            {
                // 找到了，返回 ID
                id = kv.Key;
                return true;
            }

        // 没找到
        id = 0;
        return false;
    }

    /// <summary>
    /// 在主角附近查找最近的带 NetDropTag 的掉落物
    /// 
    /// 用途: 处理物品堆叠合并的情况
    /// 例如: 拾取 5 个子弹合并到背包的 10 个子弹，引用不同但位置相同
    /// </summary>
    /// <param name="id">输出: 找到的掉落物 ID</param>
    /// <returns>是否找到</returns>
    private static bool TryFindNearestTaggedId(out uint id)
    {
        // 初始化输出
        id = 0;
        // 获取主角控制器
        var main = CharacterMainControl.Main;
        // 如果主角不存在，返回失败
        if (main == null) return false;

        // 获取主角位置
        var pos = main.transform.position;
        // 在主角周围进行球形检测，查找所有碰撞体
        var n = Physics.OverlapSphereNonAlloc(pos, PICK_RADIUS, _nearbyBuf, LAYER_MASK_ANY, QTI);

        // 记录最近的距离和标签
        var best = float.MaxValue;
        NetDropTag bestTag = null;

        // 遍历所有检测到的碰撞体
        for (var i = 0; i < n; i++)
        {
            var c = _nearbyBuf[i];
            // 如果碰撞体无效，跳过
            if (!c) continue;
            // 尝试获取 NetDropTag 组件（从父物体或自身）
            var t = c.GetComponentInParent<NetDropTag>() ?? c.GetComponent<NetDropTag>();
            // 如果没有标签或 ID 为 0，跳过
            if (t == null || t.id == 0) continue;

            // 计算距离的平方（避免开方运算，提高性能）
            var d2 = (t.transform.position - pos).sqrMagnitude;
            // 如果距离更近，更新最佳结果
            if (d2 < best)
            {
                best = d2;
                bestTag = t;
            }
        }

        // 如果找到了标签
        if (bestTag != null)
        {
            // 返回 ID
            id = bestTag.id;
            return true;
        }

        // 没找到
        return false;
    }
}

/// <summary>
/// 补丁 6/17: Inventory.AddAt - 拦截往战利品箱放物品（LootPut）
/// 
/// 触发时机: 客户端尝试往战利品容器放物品
/// 
/// 处理的四种场景:
/// A) 容器内换位: 改为 TAKE -> PUT 两段式
/// B) 其它库存 -> 容器: 发送 TAKE 请求
/// C) 容器 -> 其它库存: 发送 TAKE 请求
/// D) 直接往容器放: 发送 PUT 请求
/// 
/// 核心目标: 所有往容器放物品的操作都通过网络请求处理
/// </summary>
[HarmonyPatch(typeof(Inventory), "AddAt")]
internal static class Patch_Inventory_AddAt_LootPut
{
    /// <summary>
    /// 前置拦截: 拦截往战利品容器放物品的操作
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="item">要添加的物品</param>
    /// <param name="atPosition">目标位置索引</param>
    /// <param name="__result">返回值（是否成功）</param>
    /// <returns>true=继续执行原方法，false=跳过原方法</returns>
    private static bool Prefix(Inventory __instance, Item item, int atPosition, ref bool __result)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动 -> 放行
        if (m == null || !m.networkStarted) return true;

        // 只在客户端、且不是"应用服务器快照"阶段时干预
        if (!m.IsServer && !COOPManager.LootNet._applyingLootState)
        {
            // 判断目标是否为战利品容器
            var targetIsLoot = LootboxDetectUtil.IsLootboxInventory(__instance) && !LootboxDetectUtil.IsPrivateInventory(__instance);
            // 获取物品的源库存
            var srcInv = item ? item.InInventory : null;
            // 判断源是否为战利品容器
            var srcIsLoot = LootboxDetectUtil.IsLootboxInventory(srcInv) && !LootboxDetectUtil.IsPrivateInventory(srcInv);

            // === A) 容器内换位 ===
            if (targetIsLoot && ReferenceEquals(srcInv, __instance))
            {
                // 获取物品在源容器中的位置
                var srcPos = __instance.GetIndex(item);
                // 如果源位置和目标位置相同，直接视为成功
                if (srcPos == atPosition)
                {
                    __result = true;
                    return false;
                }

                // 发送 TAKE 请求（不带目的地）
                var tk = COOPManager.LootNet.Client_SendLootTakeRequest(__instance, srcPos, null, -1, null);
                // 记录"待重排"
                LootManager.Instance.NoteLootReorderPending(tk, __instance, atPosition);
                // 已受理
                __result = true;
                // 跳过原方法
                return false;
            }

            // === B) 其它库存 -> 容器 ===
            if (targetIsLoot && srcInv && !ReferenceEquals(srcInv, __instance))
            {
                // 获取物品在源容器中的位置
                var srcPos = srcInv.GetIndex(item);
                if (srcPos >= 0)
                {
                    // 发送 TAKE 请求
                    COOPManager.LootNet.Client_SendLootTakeRequest(srcInv, srcPos, __instance, atPosition, null);
                    // 已受理
                    __result = true;
                    // 跳过原方法
                    return false;
                }
            }

            // === C) 容器 -> 其它库存（直接 PUT）===
            if (!targetIsLoot && srcIsLoot)
            {
                // 获取物品在源容器中的位置
                var srcPos = srcInv.GetIndex(item);
                if (srcPos >= 0)
                {
                    // 发送 TAKE 请求
                    COOPManager.LootNet.Client_SendLootTakeRequest(srcInv, srcPos, __instance, atPosition, null);
                    // 已受理
                    __result = true;
                    // 跳过原方法
                    return false;
                }
            }

            // === D) 直接往容器放（UI 上新建/拖入） ===
            var isLootInv = LootboxDetectUtil.IsLootboxInventory(__instance) && !LootboxDetectUtil.IsPrivateInventory(__instance);
            if (isLootInv)
            {
                // 发送 PUT 请求
                COOPManager.LootNet.Client_SendLootPutRequest(__instance, item, atPosition);
                // 返回失败（等待主机处理）
                __result = false;
                // 跳过原方法
                return false;
            }
        }

        // 其他情况放行原方法
        return true;
    }
}

/// <summary>
/// 补丁 7/17: Inventory.AddItem - 拦截 AddItem 到战利品箱
/// 
/// 触发时机: 客户端尝试使用 AddItem 往战利品容器添加物品
/// 
/// 处理两种场景:
/// 1. 战利品容器初始化时: 吞掉本地 Add，销毁物品
/// 2. 正常操作时: 发送 PUT 请求
/// 
/// 核心目标: 防止客户端在容器初始化时本地添加物品
/// </summary>
[HarmonyPatch(typeof(Inventory), "AddItem")]
internal static class Patch_Inventory_AddItem_LootPut
{
    /// <summary>
    /// 前置拦截: 拦截 AddItem 到战利品容器的操作
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="item">要添加的物品</param>
    /// <param name="__result">返回值（是否成功）</param>
    /// <returns>true=继续执行原方法，false=跳过原方法</returns>
    private static bool Prefix(Inventory __instance, Item item, ref bool __result)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动 -> 放行
        if (m == null || !m.networkStarted) return true;

        // 场景 1: 只在"真正的战利品容器初始化"时吞掉本地 Add
        if (!m.IsServer && m.ClientLootSetupActive)
        {
            // 判断是否为战利品容器
            var isLootInv = LootboxDetectUtil.IsLootboxInventory(__instance)
                            && !LootboxDetectUtil.IsPrivateInventory(__instance);
            if (isLootInv)
            {
                try
                {
                    if (item)
                    {
                        // 分离物品
                        item.Detach();
                        // 销毁物品 GameObject
                        Object.Destroy(item.gameObject);
                    }
                }
                catch
                {
                    // 忽略异常
                }

                // 返回成功（物品已被销毁）
                __result = true;
                // 跳过原方法
                return false;
            }
        }

        // 场景 2: 正常操作时发送 PUT 请求
        if (!m.IsServer && !COOPManager.LootNet._applyingLootState)
        {
            // 判断是否为战利品容器
            var isLootInv = LootboxDetectUtil.IsLootboxInventory(__instance)
                            && !LootboxDetectUtil.IsPrivateInventory(__instance);
            if (isLootInv)
            {
                // 发送 PUT 请求（位置为 0，自动寻找空位）
                COOPManager.LootNet.Client_SendLootPutRequest(__instance, item, 0);
                // 返回失败（等待主机处理）
                __result = false;
                // 跳过原方法
                return false;
            }
        }

        // 其他情况放行原方法
        return true;
    }
}

/// <summary>
/// 补丁 8/17: Inventory.AddAt - 主机 AddAt 成功后广播
/// 
/// 触发时机: 主机成功往战利品容器添加物品后
/// 
/// 工作流程:
/// 1. 检查是否为战利品容器
/// 2. 性能监控: 记录检查耗时
/// 3. 延迟到帧结束时广播状态
/// 
/// 优化措施:
/// - 场景切换时跳过同步（避免崩溃）
/// - 延迟到帧结束执行（减少性能压力）
/// - 性能监控（记录耗时超过阈值的操作）
/// </summary>
[HarmonyPatch(typeof(Inventory), "AddAt")]
internal static class Patch_Inventory_AddAt_BroadcastOnServer
{
    /// <summary>
    /// 后置拦截: 主机 AddAt 成功后广播状态
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="item">添加的物品</param>
    /// <param name="atPosition">目标位置索引</param>
    /// <param name="__result">原方法返回值（是否成功）</param>
    private static void Postfix(Inventory __instance, Item item, int atPosition, bool __result)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/不是主机 -> 返回
        if (m == null || !m.networkStarted || !m.IsServer) return;
        // 条件检查: 操作失败/正在应用战利品状态 -> 返回
        if (!__result || COOPManager.LootNet._serverApplyingLoot) return;

        // ✅ 修复: 场景切换时 LevelManager 可能正在初始化，跳过同步避免崩溃
        try
        {
            if (LevelManager.Instance == null || LevelManager.LootBoxInventories == null)
            {
                // 场景初始化中，跳过
                return;
            }
        }
        catch
        {
            // 访问 LootBoxInventories 失败，说明场景正在切换
            return;
        }

        // ✅ 性能监控: 记录检查耗时
        var checkStartTime = Time.realtimeSinceStartup;
        bool isLootbox = LootboxDetectUtil.IsLootboxInventory(__instance);
        bool isPrivate = LootboxDetectUtil.IsPrivateInventory(__instance);
        var checkDuration = (Time.realtimeSinceStartup - checkStartTime) * 1000f;

        // 如果检查耗时超过 1ms，记录警告
        if (checkDuration > 1f)
        {
            Debug.LogWarning($"[InventoryPatch] IsLootboxInventory 检查耗时: {checkDuration:F2}ms, isLootbox={isLootbox}, isPrivate={isPrivate}");
        }

        // ✅ 关键: 排除私有库存和非箱子 Inventory（墓碑等）
        if (!isLootbox || isPrivate)
        {
            // 墓碑、仓库、宠物包等不同步
            return;
        }

        // ✅ 优化: 延迟到帧结束时执行，减少场景加载时的性能压力
        DeferedRunner.EndOfFrame(() =>
        {
            // ✅ 二次检查: 确保 Inventory 仍然有效且可同步
            if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance))
            {
                return;
            }

            // 记录广播开始时间
            var broadcastStartTime = Time.realtimeSinceStartup;
            // 广播战利品箱状态给所有客户端
            COOPManager.LootNet.Server_SendLootboxState(null, __instance);
            // 计算广播耗时
            var broadcastDuration = (Time.realtimeSinceStartup - broadcastStartTime) * 1000f;

            // 如果广播耗时超过 5ms，记录警告
            if (broadcastDuration > 5f)
            {
                Debug.LogWarning($"[InventoryPatch] 广播 LootboxState 耗时: {broadcastDuration:F2}ms");
            }
        });
    }
}

/// <summary>
/// 补丁 9/17: Inventory.AddItem - 主机 AddItem 成功后广播
/// 
/// 触发时机: 主机成功使用 AddItem 往战利品容器添加物品后
/// 
/// 工作流程:
/// 1. 检查是否为战利品容器
/// 2. 确认容器在 LootBoxInventories 中
/// 3. 延迟到帧结束时广播状态
/// 
/// 优化措施:
/// - 场景切换时跳过同步（避免崩溃）
/// - 延迟到帧结束执行（减少性能压力）
/// </summary>
[HarmonyPatch(typeof(Inventory), "AddItem")]
internal static class Patch_Inventory_AddItem_BroadcastLootState
{
    /// <summary>
    /// 后置拦截: 主机 AddItem 成功后广播状态
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="item">添加的物品</param>
    /// <param name="__result">原方法返回值（是否成功）</param>
    private static void Postfix(Inventory __instance, Item item, bool __result)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/不是主机 -> 返回
        if (m == null || !m.networkStarted || !m.IsServer) return;
        // 条件检查: 操作失败/正在应用战利品状态 -> 返回
        if (!__result || COOPManager.LootNet._serverApplyingLoot) return;

        // ✅ 修复: 场景切换时 LevelManager 可能正在初始化，跳过同步避免崩溃
        try
        {
            if (LevelManager.Instance == null || LevelManager.LootBoxInventories == null)
            {
                // 场景初始化中，跳过
                return;
            }
        }
        catch
        {
            // 访问 LootBoxInventories 失败，说明场景正在切换
            return;
        }

        // 条件检查: 不是战利品容器/是私有库存 -> 返回
        if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;

        // ✅ 再次确认容器确实在 LootBoxInventories 中
        try
        {
            var dict = InteractableLootbox.Inventories;
            var isLootInv = dict != null && dict.ContainsValue(__instance);
            if (!isLootInv) return;
        }
        catch
        {
            // 访问失败，跳过
            return;
        }

        // ✅ 优化: 延迟到帧结束时执行，减少场景加载时的性能压力
        DeferedRunner.EndOfFrame(() =>
        {
            // 再次检查容器有效性
            if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;
            // 广播战利品箱状态给所有客户端
            COOPManager.LootNet.Server_SendLootboxState(null, __instance);
        });
    }
}

/// <summary>
/// 补丁 10/17: Inventory.RemoveAt - 主机 RemoveAt 成功后广播
/// 
/// 触发时机: 主机成功从战利品容器移除物品后
/// 
/// 问题背景: 修复"主机本地拿走，客户端不刷"的幽灵物品问题
/// 
/// 工作流程:
/// 1. 检查是否为战利品容器
/// 2. 延迟到帧结束时广播状态
/// 
/// 技术细节: 使用反射精确锁定 RemoveAt(int, out Item) 重载
/// </summary>
[HarmonyPatch(typeof(Inventory))]
internal static class Patch_Inventory_RemoveAt_BroadcastOnServer
{
    /// <summary>
    /// 使用反射精确锁定 RemoveAt(int, out Item) 这个重载
    /// </summary>
    /// <returns>要补丁的方法信息</returns>
    private static MethodBase TargetMethod()
    {
        // 获取 Inventory 类型
        var tInv = typeof(Inventory);
        // 创建 Item 的 ByRef 类型（对应 out Item 参数）
        var tItemByRef = typeof(Item).MakeByRefType();
        // 返回 RemoveAt(int, out Item) 方法
        return AccessTools.Method(tInv, "RemoveAt", new[] { typeof(int), tItemByRef });
    }

    /// <summary>
    /// 后置拦截: 当主机本地从"公共战利品容器"取出成功后，广播一次全量状态
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="position">移除的位置索引</param>
    /// <param name="__1">out Item 参数（被移除的物品）</param>
    /// <param name="__result">原方法返回值（是否成功）</param>
    private static void Postfix(Inventory __instance, int position, Item __1, bool __result)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/不是主机 -> 返回（仅主机）
        if (m == null || !m.networkStarted || !m.IsServer) return;
        // 条件检查: 操作失败/正在应用战利品状态 -> 返回（跳过失败/网络路径内部调用）
        if (!__result || COOPManager.LootNet._serverApplyingLoot) return;

        // ✅ 修复: 场景切换时 LevelManager 可能正在初始化，跳过同步避免崩溃
        try
        {
            if (LevelManager.Instance == null || LevelManager.LootBoxInventories == null)
            {
                // 场景初始化中，跳过
                return;
            }
        }
        catch
        {
            // 访问 LootBoxInventories 失败，说明场景正在切换
            return;
        }

        // 条件检查: 只处理战利品容器
        if (!LootboxDetectUtil.IsLootboxInventory(__instance)) return;
        // 条件检查: 跳过玩家仓库/宠物包等私有库存
        if (LootboxDetectUtil.IsPrivateInventory(__instance)) return;

        // ✅ 优化: 延迟到帧结束时执行，减少场景加载时的性能压力
        DeferedRunner.EndOfFrame(() =>
        {
            // 再次检查容器有效性
            if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;
            // 广播给所有客户端
            COOPManager.LootNet.Server_SendLootboxState(null, __instance);
        });
    }
}

/// <summary>
/// 补丁 11/17: Inventory.AddAt - 处理物品拆分（Split）
/// 
/// 触发时机: 客户端尝试拆分堆叠物品到战利品容器
/// 例如: 将 10 个子弹拆分成 5 个和 5 个
/// 
/// 工作流程:
/// 1. 检查物品是否在拆分映射表中
/// 2. 发送拆分请求给主机
/// 3. 销毁本地临时物品
/// 
/// 优先级: Priority.First（最先执行）
/// 
/// 技术细节: 有些路径会直接调用 Inventory.AddAt（不走 AddAndMerge），需要拦截
/// </summary>
[HarmonyPatch(typeof(Inventory), "AddAt")]
[HarmonyPriority(Priority.First)]
internal static class Patch_AddAt_SplitFirst
{
    /// <summary>
    /// 前置拦截: 拦截物品拆分操作
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="item">要添加的物品（拆分出的新物品）</param>
    /// <param name="atPosition">目标位置索引</param>
    /// <param name="__result">返回值（是否成功）</param>
    /// <returns>true=继续执行原方法，false=跳过原方法</returns>
    private static bool Prefix(Inventory __instance, Item item, int atPosition, ref bool __result)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/当前是主机 -> 放行
        if (m == null || !m.networkStarted || m.IsServer) return true;

        // 条件检查: 库存或物品为空 -> 放行
        if (__instance == null || item == null) return true;
        // 条件检查: 不是战利品容器/是私有库存 -> 放行
        if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance))
            return true;

        // 检查物品是否在拆分映射表中
        if (!ModBehaviourF.map.TryGetValue(item.GetInstanceID(), out var p)) return true;
        // 检查映射的库存是否匹配
        if (!ReferenceEquals(p.inv, __instance)) return true;

        // 发送拆分请求给主机
        // 参数: 容器, 源位置, 拆分数量, 目标位置
        COOPManager.LootNet.Client_SendLootSplitRequest(__instance, p.srcPos, p.count, atPosition);

        try
        {
            if (item)
            {
                // 分离物品
                item.Detach();
                // 销毁本地临时物品 GameObject
                Object.Destroy(item.gameObject);
            }
        }
        catch
        {
            // 忽略异常
        }

        // 从映射表中移除
        ModBehaviourF.map.Remove(item.GetInstanceID());

        // 返回成功（等待主机处理）
        __result = true;
        // 跳过原方法
        return false;
    }
}

/// <summary>
/// 补丁 12/17: Inventory.Sort - 拦截客户端排序操作
/// 
/// 触发时机: 客户端尝试对战利品容器进行排序/整理
/// 
/// 核心目标: 阻止客户端本地排序，防止制造幽灵物品
/// 
/// 未来扩展: 可以发送"请求合并/整理"的网络指令给主机，让主机执行再广播
/// </summary>
[HarmonyPatch(typeof(Inventory), nameof(Inventory.Sort), new Type[] { })]
internal static class Patch_Inventory_Sort_BlockLocalInLoot
{
    /// <summary>
    /// 前置拦截: 拦截客户端对战利品容器的排序操作
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <returns>true=继续执行原方法，false=跳过原方法</returns>
    private static bool Prefix(Inventory __instance)
    {
        // 获取 Mod 实例
        var mod = ModBehaviourF.Instance;
        // 条件检查: 不是客户端 -> 放行（主机/单机场景放行）
        if (mod == null || !mod.IsClient) return true;
        // 条件检查: 正在应用服务器快照 -> 放行（用于UI重建）
        if (COOPManager.LootNet.ApplyingLootState) return true;
        // 条件检查: 不是当前战利品容器 -> 放行
        if (!LootManager.IsCurrentLootInv(__instance)) return true;
        // 条件检查: 是私有库存 -> 放行
        if (LootboxDetectUtil.IsPrivateInventory(__instance)) return true;

        // 这里可选: 发一个"请求合并/整理"的网络指令给主机，让主机执行再广播
        // 若不想加协议，先纯拦也行，至少不会再制造幽灵
        // 阻止原始 Sort()
        return false;
    }
}

/// <summary>
/// 补丁 13/17: Inventory.RemoveAt - 主机移除物品后广播（带静音检查）
/// 
/// 触发时机: 主机成功从战利品容器移除物品后
/// 
/// 工作流程:
/// 1. 检查是否为战利品容器
/// 2. 检查容器是否处于静音期
/// 3. 如果不在静音期，广播状态
/// 
/// 静音机制: 防止 AI 死亡填充战利品时频繁广播
/// </summary>
[HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveAt))]
internal static class Patch_ServerBroadcast_OnRemoveAt
{
    /// <summary>
    /// 后置拦截: 主机移除物品后广播状态
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="position">移除的位置索引</param>
    /// <param name="removedItem">被移除的物品</param>
    /// <param name="__result">原方法返回值（是否成功）</param>
    private static void Postfix(Inventory __instance, int position, Item removedItem, bool __result)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/不是主机 -> 返回
        if (m == null || !m.networkStarted || !m.IsServer) return;
        // 条件检查: 操作失败/正在应用战利品状态 -> 返回
        if (!__result || COOPManager.LootNet._serverApplyingLoot) return;
        // 条件检查: 不是战利品容器/是私有库存 -> 返回
        if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;

        // ★ 新增: 检查容器是否处于静音期
        if (LootManager.Instance.Server_IsLootMuted(__instance)) return;
        // 广播战利品箱状态给所有客户端
        COOPManager.LootNet.Server_SendLootboxState(null, __instance);
    }
}

/// <summary>
/// 补丁 14/17: Inventory.AddAt - 主机添加物品后广播（带静音机制）
/// 
/// 触发时机: 主机成功往战利品容器添加物品后
/// 
/// 工作流程:
/// 1. Prefix: 在 AI 死亡填充场景时，给容器加"静音窗口"
/// 2. Postfix: 检查静音期，如果不在静音期则广播状态
/// 
/// 静音机制: 防止 AI 死亡填充战利品时频繁广播（1秒静音足够覆盖整次填充）
/// </summary>
[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddAt))]
internal static class Patch_ServerBroadcast_OnAddAt
{
    /// <summary>
    /// 前置拦截: 在 AddAt 前给容器加"静音窗口"
    /// 
    /// 死亡填充场景: 在 AddAt 前给该容器加"静音窗口"，屏蔽本次及紧随其后的群发
    /// </summary>
    /// <param name="__instance">目标库存</param>
    private static void Prefix(Inventory __instance)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/不是主机 -> 返回
        if (m == null || !m.networkStarted || !m.IsServer) return;

        // 仅在 AI 死亡 OnDead 流程里触发（项目里已有这个上下文标记）
        if (DeadLootSpawnContext.InOnDead == null) return;

        // 条件检查: 不是战利品容器/是私有库存 -> 返回
        if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;
        // 给容器加静音: 1秒静音足够覆盖整次填充
        LootManager.Instance.Server_MuteLoot(__instance, 1.0f);
    }

    /// <summary>
    /// 后置拦截: 主机添加物品后广播状态（检查静音期）
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="item">添加的物品</param>
    /// <param name="atPosition">目标位置索引</param>
    /// <param name="__result">原方法返回值（是否成功）</param>
    private static void Postfix(Inventory __instance, Item item, int atPosition, bool __result)
    {
        // 获取 Mod 实例
        var m = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/不是主机 -> 返回
        if (m == null || !m.networkStarted || !m.IsServer) return;
        // 条件检查: 操作失败/正在应用战利品状态 -> 返回
        if (!__result || COOPManager.LootNet._serverApplyingLoot) return;
        // 条件检查: 不是战利品容器/是私有库存 -> 返回
        if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;

        // ★ 新增: 静音期内跳过群发（真正有人打开时仍会单播，应答不受影响）
        if (LootManager.Instance.Server_IsLootMuted(__instance)) return;

        // 广播战利品箱状态给所有客户端
        COOPManager.LootNet.Server_SendLootboxState(null, __instance);
    }
}

/// <summary>
/// 补丁 15/17: Inventory.AddAt - 标记未检视物品（应用战利品状态时）
/// 
/// 触发时机: 客户端应用服务器战利品状态时，往容器添加物品后
/// 
/// 工作流程:
/// 1. 检查是否正在应用战利品状态
/// 2. 遍历容器中的所有物品
/// 3. 如果有未检视的物品，设置容器的 NeedInspection 标志
/// 
/// 用途: 在战利品箱UI上显示"未检视"标记（黄色感叹号）
/// </summary>
[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddAt), typeof(Item), typeof(int))]
internal static class Patch_Inventory_AddAt_FlagUninspected_WhenApplyingLoot
{
    /// <summary>
    /// 后置拦截: AddAt 后标记未检视物品
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="item">添加的物品</param>
    private static void Postfix(Inventory __instance, Item item)
    {
        // 调用标记方法
        ApplyUninspectedFlag(__instance, item);
    }

    /// <summary>
    /// 应用未检视标志
    /// </summary>
    /// <param name="inv">库存</param>
    /// <param name="item">物品</param>
    private static void ApplyUninspectedFlag(Inventory inv, Item item)
    {
        // 获取 Mod 实例
        var mod = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/当前是主机 -> 返回
        if (mod == null || !mod.networkStarted || mod.IsServer) return;

        // 条件检查: 不是正在应用战利品状态 -> 返回
        if (!COOPManager.LootNet.ApplyingLootState) return;

        // 条件检查: 是私有库存 -> 返回
        if (LootboxDetectUtil.IsPrivateInventory(inv)) return;
        // 条件检查: 不是战利品容器且不是当前战利品容器 -> 返回
        if (!(LootboxDetectUtil.IsLootboxInventory(inv) || LootManager.IsCurrentLootInv(inv))) return;

        try
        {
            // 获取容器中最后一个物品的位置
            var last = inv.GetLastItemPosition();
            // 标记是否有未检视物品
            var hasUninspected = false;
            // 遍历容器中的所有物品
            for (var i = 0; i <= last; i++)
            {
                // 获取该位置的物品
                var it = inv.GetItemAt(i);
                // 如果物品存在且未被检视
                if (it != null && !it.Inspected)
                {
                    // 标记有未检视物品
                    hasUninspected = true;
                    // 跳出循环
                    break;
                }
            }

            // 设置容器的 NeedInspection 标志
            inv.NeedInspection = hasUninspected;
        }
        catch
        {
            // 忽略异常
        }
    }
}

/// <summary>
/// 补丁 16/17: Inventory.AddItem - 标记未检视物品（应用战利品状态时）
/// 
/// 触发时机: 客户端应用服务器战利品状态时，使用 AddItem 往容器添加物品后
/// 
/// 工作流程:
/// 1. 检查是否正在应用战利品状态
/// 2. 遍历容器中的所有物品
/// 3. 如果有未检视的物品，设置容器的 NeedInspection 标志
/// 
/// 用途: 在战利品箱UI上显示"未检视"标记（黄色感叹号）
/// 
/// 注意: 与补丁 15 功能相同，但针对 AddItem 方法
/// </summary>
[HarmonyPatch(typeof(Inventory), "AddItem", typeof(Item))]
internal static class Patch_Inventory_AddItem_FlagUninspected_WhenApplyingLoot
{
    /// <summary>
    /// 后置拦截: AddItem 后标记未检视物品
    /// </summary>
    /// <param name="__instance">目标库存</param>
    /// <param name="item">添加的物品</param>
    private static void Postfix(Inventory __instance, Item item)
    {
        // 调用标记方法
        ApplyUninspectedFlag(__instance, item);
    }

    /// <summary>
    /// 应用未检视标志
    /// </summary>
    /// <param name="inv">库存</param>
    /// <param name="item">物品</param>
    private static void ApplyUninspectedFlag(Inventory inv, Item item)
    {
        // 获取 Mod 实例
        var mod = ModBehaviourF.Instance;
        // 条件检查: Mod 未初始化/网络未启动/当前是主机 -> 返回
        if (mod == null || !mod.networkStarted || mod.IsServer) return;

        // 条件检查: 不是正在应用战利品状态 -> 返回
        if (!COOPManager.LootNet.ApplyingLootState) return;

        // 条件检查: 是私有库存 -> 返回
        if (LootboxDetectUtil.IsPrivateInventory(inv)) return;
        // 条件检查: 不是战利品容器且不是当前战利品容器 -> 返回
        if (!(LootboxDetectUtil.IsLootboxInventory(inv) || LootManager.IsCurrentLootInv(inv))) return;

        try
        {
            // 获取容器中最后一个物品的位置
            var last = inv.GetLastItemPosition();
            // 标记是否有未检视物品
            var hasUninspected = false;
            // 遍历容器中的所有物品
            for (var i = 0; i <= last; i++)
            {
                // 获取该位置的物品
                var it = inv.GetItemAt(i);
                // 如果物品存在且未被检视
                if (it != null && !it.Inspected)
                {
                    // 标记有未检视物品
                    hasUninspected = true;
                    // 跳出循环
                    break;
                }
            }

            // 设置容器的 NeedInspection 标志
            inv.NeedInspection = hasUninspected;
        }
        catch
        {
            // 忽略异常
        }
    }
}

// ============================================================================
// 文件结束
// 
// 总结:
// - 共 17 个补丁类
// - 涵盖客户端拦截、主机广播、物品拾取、拆分、排序等所有场景
// - 实现了完整的战利品同步机制
// - 防止了幽灵物品、状态不一致等问题
// ============================================================================
