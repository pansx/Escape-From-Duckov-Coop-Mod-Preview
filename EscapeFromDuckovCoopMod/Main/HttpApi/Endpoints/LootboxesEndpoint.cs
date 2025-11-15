// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using ItemStatsSystem;

namespace EscapeFromDuckovCoopMod.HttpApi.Endpoints;

/// <summary>
/// 战利品箱列表端点 "/api/lootboxes"
/// </summary>
public class LootboxesEndpoint : IHttpEndpoint
{
    public string Path => "/api/lootboxes";

    public void Handle(HttpListenerContext context)
    {
        try
        {
            // 安全检查：确保游戏已完全加载
            if (CharacterMainControl.Main == null)
            {
                var safeResult = new
                {
                    count = 0,
                    lootboxes = new List<object>(),
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    message = "游戏未完全加载，战利品箱数据暂不可用"
                };
                HttpHelper.SendJson(context.Response, safeResult);
                return;
            }

            var lootboxes = new List<object>();

            // 使用 try-catch 包裹 FindObjectsOfType，防止在场景切换时崩溃
            InteractableLootbox[] interactableLootboxes;
            try
            {
                interactableLootboxes = UnityEngine.Object.FindObjectsOfType<InteractableLootbox>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LootboxesEndpoint] 无法获取战利品箱列表（可能正在加载场景）: {ex.Message}");
                var safeResult = new
                {
                    count = 0,
                    lootboxes = new List<object>(),
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    message = "场景正在加载中"
                };
                HttpHelper.SendJson(context.Response, safeResult);
                return;
            }

            foreach (var lootbox in interactableLootboxes)
            {
                if (lootbox == null) continue;

                try
                {
                    var inventory = lootbox.Inventory;
                    var items = new List<object>();

                    // 获取战利品箱中的物品
                    if (inventory != null)
                    {
                        // 遍历所有槽位
                        for (int i = 0; i < inventory.Capacity; i++)
                        {
                            try
                            {
                                var item = inventory[i];
                                if (item == null) continue;

                                items.Add(new
                                {
                                    slot = i,
                                    name = item.name,
                                    displayName = item.DisplayName ?? item.name
                                });
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[LootboxesEndpoint] 处理物品时出错: {ex.Message}");
                            }
                        }
                    }

                    var lootboxData = new
                    {
                        // 基础信息
                        instanceId = lootbox.GetInstanceID(),
                        name = lootbox.gameObject.name,
                        active = lootbox.gameObject.activeSelf,
                        
                        // 位置信息
                        position = new
                        {
                            x = lootbox.transform.position.x,
                            y = lootbox.transform.position.y,
                            z = lootbox.transform.position.z
                        },
                        
                        // 容器信息
                        inventory = inventory != null ? new
                        {
                            capacity = inventory.Capacity,
                            itemCount = items.Count,
                            isEmpty = items.Count == 0
                        } : null,
                        
                        // 物品列表
                        items = items,
                        
                        // 交互状态
                        interactable = new
                        {
                            enabled = lootbox.enabled
                        }
                    };

                    lootboxes.Add(lootboxData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LootboxesEndpoint] 处理战利品箱时出错: {ex.Message}");
                }
            }

            var result = new
            {
                count = lootboxes.Count,
                lootboxes = lootboxes,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            HttpHelper.SendJson(context.Response, result);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LootboxesEndpoint] 处理请求时出错: {ex.Message}\n{ex.StackTrace}");
            HttpHelper.SendError(context.Response, 500, $"获取战利品箱列表失败: {ex.Message}");
        }
    }
}
