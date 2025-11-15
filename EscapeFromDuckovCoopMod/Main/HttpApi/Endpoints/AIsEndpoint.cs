// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

namespace EscapeFromDuckovCoopMod.HttpApi.Endpoints;

/// <summary>
/// AI 列表端点 "/api/ais"
/// </summary>
public class AIsEndpoint : IHttpEndpoint
{
    public string Path => "/api/ais";

    public void Handle(HttpListenerContext context)
    {
        try
        {
            var ais = new List<object>();

            // 查找所有 AI 角色控制器
            var aiControllers = UnityEngine.Object.FindObjectsOfType<AICharacterController>();

            foreach (var ai in aiControllers)
            {
                if (ai == null) continue;

                try
                {
                    // 使用 GetComponentInChildren 查找子对象上的组件
                    var cmc = ai.GetComponentInChildren<CharacterMainControl>();
                    var health = ai.GetComponentInChildren<Health>();
                    var brain = ai.GetComponentInChildren<AIMainBrain>();

                    var aiData = new
                    {
                        // 基础信息
                        instanceId = ai.GetInstanceID(),
                        name = ai.gameObject.name,
                        active = ai.gameObject.activeSelf,
                        
                        // 位置信息
                        position = new
                        {
                            x = ai.transform.position.x,
                            y = ai.transform.position.y,
                            z = ai.transform.position.z
                        },
                        
                        // 血量信息
                        health = health != null ? new
                        {
                            current = health.CurrentHealth,
                            max = health.MaxHealth,
                            alive = health.CurrentHealth > 0,
                            percentage = health.MaxHealth > 0 
                                ? (int)((health.CurrentHealth / health.MaxHealth) * 100) 
                                : 0
                        } : null,
                        
                        // AI 状态
                        aiState = brain != null ? new
                        {
                            isActive = brain.enabled,
                            hasBrain = true
                        } : null,
                        
                        // 角色信息
                        character = cmc != null ? new
                        {
                            exists = true,
                            name = cmc.gameObject.name
                        } : null
                    };

                    ais.Add(aiData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AIsEndpoint] 处理 AI 时出错: {ex.Message}");
                }
            }

            var result = new
            {
                count = ais.Count,
                ais = ais,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            HttpHelper.SendJson(context.Response, result);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AIsEndpoint] 处理请求时出错: {ex.Message}\n{ex.StackTrace}");
            HttpHelper.SendError(context.Response, 500, $"获取 AI 列表失败: {ex.Message}");
        }
    }
}
