// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EscapeFromDuckovCoopMod.Net;

/// <summary>
/// 传送消息 - 用于场景同步完成后的位置校正
/// </summary>
public static class TeleportMessage
{
    /// <summary>
    /// 客户端完成同步UI的消息
    /// </summary>
    [Serializable]
    public class FinishSyncUiData
    {
        public string type = "finishSyncUi";
        public string playerId;
        public Vector3Data position;
    }

    /// <summary>
    /// 主机下发的传送指令
    /// </summary>
    [Serializable]
    public class TeleportData
    {
        public string type = "teleport";
        public string playerId;
        public Vector3Data targetPosition;
    }

    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data() { }

        public Vector3Data(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// 客户端：发送完成同步UI的消息（上报自己的位置）
    /// </summary>
    public static void Client_SendFinishSyncUi()
    {
        // 启动协程进行重试
        if (ModBehaviourF.Instance != null)
        {
            ModBehaviourF.Instance.StartCoroutine(Client_SendFinishSyncUiWithRetry());
        }
        else
        {
            Debug.LogError("[TELEPORT] ModBehaviourF.Instance 为空，无法启动协程");
        }
    }

    /// <summary>
    /// 客户端：发送完成同步UI的消息（带重试逻辑）
    /// </summary>
    private static System.Collections.IEnumerator Client_SendFinishSyncUiWithRetry()
    {
        const int MAX_RETRIES = 10;
        const float RETRY_DELAY = 3f; // 1秒

        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            var service = NetService.Instance;

            // 检查 service 是否为空
            if (service == null)
            {
                Debug.LogWarning(
                    $"[TELEPORT] NetService 未初始化，尝试重建服务 ({attempt}/{MAX_RETRIES})"
                );

                // 尝试重建 NetService
                try
                {
                    // 检查 ModBehaviourF 是否存在
                    if (ModBehaviourF.Instance != null && ModBehaviourF.Instance.gameObject != null)
                    {
                        // 检查是否已经有 NetService 组件
                        var existingService = ModBehaviourF
                            .Instance
                            .gameObject
                            .GetComponent<NetService>();
                        if (existingService == null)
                        {
                            Debug.Log("[TELEPORT] 尝试添加 NetService 组件...");
                            var newService = ModBehaviourF
                                .Instance
                                .gameObject
                                .AddComponent<NetService>();
                            if (newService != null)
                            {
                                Debug.Log("[TELEPORT] ✅ NetService 组件已添加，等待初始化...");
                            }
                        }
                        else
                        {
                            Debug.Log(
                                "[TELEPORT] NetService 组件已存在，但 Instance 为 null，等待初始化..."
                            );
                        }
                    }
                    else
                    {
                        Debug.LogError("[TELEPORT] ModBehaviourF.Instance 不可用，无法重建服务");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[TELEPORT] 重建 NetService 失败: {ex.Message}");
                }

                yield return new UnityEngine.WaitForSeconds(RETRY_DELAY);
                continue;
            }

            // 检查是否是主机
            if (service.IsServer)
            {
                Debug.LogWarning("[TELEPORT] 当前是主机，无需发送完成同步UI消息");
                yield break;
            }

            // 检查是否已连接
            if (service.connectedPeer == null)
            {
                // 尝试获取要连接的主机信息
                string targetHost = "未知";
                try
                {
                    if (!string.IsNullOrEmpty(service.manualIP))
                    {
                        targetHost = $"{service.manualIP}:{service.manualPort}";
                    }
                    else if (service.hostList != null && service.hostList.Count > 0)
                    {
                        targetHost = service.hostList[0];
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[TELEPORT] 获取主机信息失败: {ex.Message}");
                }

                Debug.LogWarning(
                    $"[TELEPORT] 未连接到主机，尝试重连 ({attempt}/{MAX_RETRIES}) → 目标主机: {targetHost}"
                );

                // 尝试重连
                try
                {
                    if (!string.IsNullOrEmpty(service.manualIP))
                    {
                        Debug.Log($"[TELEPORT] 尝试连接到手动指定的主机: {targetHost}");
                        service.TryAutoReconnect();
                    }
                    else
                    {
                        Debug.Log($"[TELEPORT] 等待自动发现主机...");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[TELEPORT] 重连失败: {ex.Message}");
                }

                yield return new UnityEngine.WaitForSeconds(RETRY_DELAY);
                continue;
            }

            // 检查角色是否存在
            var character = CharacterMainControl.Main;
            if (character == null)
            {
                Debug.LogWarning(
                    $"[TELEPORT] 角色未初始化，等待重试 ({attempt}/{MAX_RETRIES})"
                );
                yield return new UnityEngine.WaitForSeconds(RETRY_DELAY);
                continue;
            }

            // 所有条件满足，发送消息
            bool sendSuccess = false;
            try
            {
                var data = new FinishSyncUiData
                {
                    playerId = service.localPlayerStatus?.EndPoint ?? "Client:Unknown",
                    position = new Vector3Data(character.transform.position)
                };

                // 使用与现有代码相同的方式发送 JSON 消息
                var json = JsonConvert.SerializeObject(data, Formatting.None);
                var writer = service.writer;
                writer.Reset();
                writer.Put((byte)Op.JSON);
                writer.Put(json);

                service.connectedPeer.Send(writer, LiteNetLib.DeliveryMethod.ReliableOrdered);
                Debug.Log(
                    $"[TELEPORT] ✅ 客户端发送完成同步UI消息成功 (尝试 {attempt}/{MAX_RETRIES}): playerId={data.playerId}, pos={character.transform.position}"
                );
                sendSuccess = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[TELEPORT] 发送消息失败 (尝试 {attempt}/{MAX_RETRIES}): {ex.Message}"
                );
            }

            if (sendSuccess)
            {
                yield break; // 成功发送，退出
            }

            // 如果失败且还有重试次数，等待后继续
            if (attempt < MAX_RETRIES)
            {
                yield return new UnityEngine.WaitForSeconds(RETRY_DELAY);
            }
        }

        // 所有重试都失败，输出详细的诊断信息
        var finalService = NetService.Instance;
        string diagnosticInfo = "未知原因";

        if (finalService == null)
        {
            diagnosticInfo = "NetService 未初始化";
        }
        else if (finalService.IsServer)
        {
            diagnosticInfo = "当前是主机模式";
        }
        else if (finalService.connectedPeer == null)
        {
            string targetHost = "未知";
            try
            {
                if (!string.IsNullOrEmpty(finalService.manualIP))
                {
                    targetHost = $"{finalService.manualIP}:{finalService.manualPort}";
                }
                else if (finalService.hostList != null && finalService.hostList.Count > 0)
                {
                    targetHost = finalService.hostList[0];
                }
            }
            catch { }

            diagnosticInfo = $"无法连接到主机 ({targetHost})";

            // 最后一次尝试重连
            Debug.LogWarning($"[TELEPORT] 最后一次尝试重连到主机: {targetHost}");
            try
            {
                finalService.TryAutoReconnect();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TELEPORT] 最后重连尝试失败: {ex.Message}");
            }
        }
        else if (CharacterMainControl.Main == null)
        {
            diagnosticInfo = "角色未初始化";
        }

        Debug.LogError(
            $"[TELEPORT] ❌ 发送完成同步UI消息失败，已重试 {MAX_RETRIES} 次 - 原因: {diagnosticInfo}"
        );
    }

    /// <summary>
    /// 主机：处理客户端完成同步UI的消息
    /// </summary>
    public static void Host_HandleFinishSyncUi(LiteNetLib.NetPeer fromPeer, FinishSyncUiData data)
    {
        var service = NetService.Instance;
        if (service == null || !service.IsServer)
        {
            Debug.LogWarning("[TELEPORT] 不是主机，忽略完成同步UI消息");
            return;
        }

        var hostCharacter = CharacterMainControl.Main;
        if (hostCharacter == null)
        {
            Debug.LogWarning("[TELEPORT] 主机角色为空，无法处理传送请求");
            return;
        }

        var clientPos = data.position.ToVector3();
        var hostPos = hostCharacter.transform.position;
        var distance = Vector3.Distance(clientPos, hostPos);

        Debug.Log($"[TELEPORT] 收到客户端完成同步UI: playerId={data.playerId}, clientPos={clientPos}, hostPos={hostPos}, distance={distance:F2}");

        // 如果距离大于1米，下发传送指令
        if (distance > 1f)
        {
            Debug.Log($"[TELEPORT] 距离过大({distance:F2}m)，下发传送指令到主机位置");
            Host_SendTeleport(fromPeer, data.playerId, hostPos);
        }
        else
        {
            Debug.Log($"[TELEPORT] 距离合理({distance:F2}m)，无需传送");
        }
    }

    /// <summary>
    /// 主机：下发传送指令
    /// </summary>
    public static void Host_SendTeleport(LiteNetLib.NetPeer toPeer, string playerId, Vector3 targetPosition)
    {
        var service = NetService.Instance;
        if (service == null || !service.IsServer)
        {
            Debug.LogWarning("[TELEPORT] 不是主机，无法下发传送指令");
            return;
        }

        var data = new TeleportData
        {
            playerId = playerId,
            targetPosition = new Vector3Data(targetPosition)
        };

        // 使用与现有代码相同的方式发送 JSON 消息
        var json = JsonConvert.SerializeObject(data, Formatting.None);
        var writer = service.writer;
        writer.Reset();
        writer.Put((byte)Op.JSON);
        writer.Put(json);

        toPeer.Send(writer, LiteNetLib.DeliveryMethod.ReliableOrdered);
        Debug.Log($"[TELEPORT] 主机下发传送指令: playerId={playerId}, targetPos={targetPosition}");
    }

    /// <summary>
    /// 客户端：处理主机下发的传送指令
    /// </summary>
    public static void Client_HandleTeleport(TeleportData data)
    {
        var service = NetService.Instance;
        if (service == null || service.IsServer)
        {
            Debug.LogWarning("[TELEPORT] 不是客户端，忽略传送指令");
            return;
        }

        var character = CharacterMainControl.Main;
        if (character == null)
        {
            Debug.LogWarning("[TELEPORT] 角色为空，无法执行传送");
            return;
        }

        var targetPos = data.targetPosition.ToVector3();
        var currentPos = character.transform.position;

        Debug.Log($"[TELEPORT] 收到传送指令: 从 {currentPos} 传送到 {targetPos}");

        // 使用 SetPosition 方法传送（参考 MapTeleport）
        character.SetPosition(targetPos);

        Debug.Log($"[TELEPORT] 传送完成: 新位置 {character.transform.position}");
    }
}
