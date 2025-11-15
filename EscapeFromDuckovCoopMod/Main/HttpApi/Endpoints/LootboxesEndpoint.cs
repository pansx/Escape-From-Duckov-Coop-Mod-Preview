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
            var lootboxes = new List<object>();

            // 查找所有可交互的战利品箱
            var interactableLootboxes = UnityEngine.Object.FindObjectsOfType<InteractableLootbox>();

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
