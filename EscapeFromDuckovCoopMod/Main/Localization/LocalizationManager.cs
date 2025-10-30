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

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SodaCraft.Localizations;

namespace EscapeFromDuckovCoopMod
{
    /// <summary>
    /// 集中式本地化管理器
    /// 从JSON文件加载和管理翻译
    /// </summary>
    public static class CoopLocalization
    {
        private static Dictionary<string, string> currentTranslations = new Dictionary<string, string>(); // 当前翻译字典
        private static string currentLanguageCode = "en-US"; // 当前语言代码
        private static bool isInitialized = false; // 是否已初始化
        private static SystemLanguage lastSystemLanguage = SystemLanguage.Unknown; // 上次系统语言

        /// <summary>
        /// 初始化本地化系统
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;

            // 检测并加载游戏当前语言
            DetectAndLoadLanguage();
            isInitialized = true;

            Debug.Log($"[CoopLocalization] 已初始化，语言: {currentLanguageCode}");
        }

        /// <summary>
        /// 检查系统语言变更并重新加载
        /// </summary>
        public static void CheckLanguageChange()
        {
            if (!isInitialized) return;

            var currentSystemLang = LocalizationManager.CurrentLanguage;
            if (currentSystemLang != lastSystemLanguage)
            {
                Debug.Log($"[CoopLocalization] 语言已从 {lastSystemLanguage} 更改为 {currentSystemLang}，重新加载翻译...");
                DetectAndLoadLanguage();
            }
        }

        /// <summary>
        /// 检测系统语言并加载翻译
        /// </summary>
        private static void DetectAndLoadLanguage()
        {
            var systemLang = LocalizationManager.CurrentLanguage;
            lastSystemLanguage = systemLang;

            switch (systemLang)
            {
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                case SystemLanguage.Chinese:
                    currentLanguageCode = "zh-CN";
                    break;
                case SystemLanguage.Korean:
                    currentLanguageCode = "ko-KR";
                    break;
                case SystemLanguage.Japanese:
                    currentLanguageCode = "ja-JP";
                    break;
                case SystemLanguage.English:
                default:
                    currentLanguageCode = "en-US";
                    break;
            }

            LoadTranslations(currentLanguageCode);
        }

        /// <summary>
        /// 从JSON文件加载翻译
        /// </summary>
        private static void LoadTranslations(string languageCode)
        {
            currentTranslations.Clear();

            try
            {
                // 查找模组文件夹路径
                string modPath = Path.GetDirectoryName(typeof(CoopLocalization).Assembly.Location);
                string localizationPath = Path.Combine(modPath, "Localization", $"{languageCode}.json");

                // 如果JSON文件不存在，使用英语作为后备
                if (!File.Exists(localizationPath))
                {
                    Debug.LogWarning($"[CoopLocalization] 未找到翻译文件: {localizationPath}，使用后备翻译");
                    LoadFallbackTranslations();
                    return;
                }

                string json = File.ReadAllText(localizationPath);

                // 手动JSON解析（避免Unity JsonUtility的数组解析问题）
                ParseJsonTranslations(json);

                if (currentTranslations.Count > 0)
                {
                    Debug.Log($"[CoopLocalization] 从 {localizationPath} 加载了 {currentTranslations.Count} 条翻译");
                }
                else
                {
                    Debug.LogWarning($"[CoopLocalization] 解析翻译文件失败，使用后备翻译");
                    LoadFallbackTranslations();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CoopLocalization] 加载翻译时出错: {e.Message}");
                LoadFallbackTranslations();
            }
        }

        /// <summary>
        /// 手动JSON解析（避免Unity JsonUtility的数组解析问题）
        /// </summary>
        private static void ParseJsonTranslations(string json)
        {
            try
            {
                // 查找 "translations": [ 部分
                int startIndex = json.IndexOf("\"translations\"");
                if (startIndex == -1) return;

                // 查找 [
                int arrayStart = json.IndexOf('[', startIndex);
                if (arrayStart == -1) return;

                // 查找 ] (最后一个)
                int arrayEnd = json.LastIndexOf(']');
                if (arrayEnd == -1) return;

                // 解析每个条目
                string arrayContent = json.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);

                // 按 { } 块分割
                int braceCount = 0;
                int entryStart = -1;

                for (int i = 0; i < arrayContent.Length; i++)
                {
                    char c = arrayContent[i];

                    if (c == '{')
                    {
                        if (braceCount == 0) entryStart = i;
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0 && entryStart != -1)
                        {
                            // 提取一个条目
                            string entry = arrayContent.Substring(entryStart, i - entryStart + 1);
                            ParseSingleEntry(entry);
                            entryStart = -1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CoopLocalization] JSON解析错误: {e.Message}");
            }
        }

        /// <summary>
        /// 解析单个JSON条目
        /// </summary>
        private static void ParseSingleEntry(string entry)
        {
            try
            {
                string key = null;
                string value = null;

                // 解析 "key": "..."
                int keyIndex = entry.IndexOf("\"key\"");
                if (keyIndex != -1)
                {
                    int keyValueStart = entry.IndexOf(':', keyIndex);
                    if (keyValueStart != -1)
                    {
                        int keyQuoteStart = entry.IndexOf('\"', keyValueStart);
                        int keyQuoteEnd = entry.IndexOf('\"', keyQuoteStart + 1);
                        if (keyQuoteStart != -1 && keyQuoteEnd != -1)
                        {
                            key = entry.Substring(keyQuoteStart + 1, keyQuoteEnd - keyQuoteStart - 1);
                        }
                    }
                }

                // 解析 "value": "..."
                int valueIndex = entry.IndexOf("\"value\"");
                if (valueIndex != -1)
                {
                    int valueValueStart = entry.IndexOf(':', valueIndex);
                    if (valueValueStart != -1)
                    {
                        int valueQuoteStart = entry.IndexOf('\"', valueValueStart);
                        int valueQuoteEnd = entry.IndexOf('\"', valueQuoteStart + 1);
                        if (valueQuoteStart != -1 && valueQuoteEnd != -1)
                        {
                            value = entry.Substring(valueQuoteStart + 1, valueQuoteEnd - valueQuoteStart - 1);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(key) && value != null)
                {
                    currentTranslations[key] = value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CoopLocalization] 条目解析错误: {e.Message}");
            }
        }

        /// <summary>
        /// 加载后备翻译（当JSON文件不存在时）
        /// </summary>
        private static void LoadFallbackTranslations()
        {
            // 提供硬编码的默认中文翻译
            currentTranslations.Clear();
            currentTranslations["ui.window.title"] = "联机模组控制面板";
            currentTranslations["ui.window.playerStatus"] = "玩家状态";
            currentTranslations["ui.mode.current"] = "当前模式";
            currentTranslations["ui.mode.server"] = "服务器";
            currentTranslations["ui.mode.client"] = "客户端";
            currentTranslations["ui.mode.switchTo"] = "切换到{0}模式";
            currentTranslations["ui.hostList.title"] = "🔍 局域网主机列表";
            currentTranslations["ui.hostList.empty"] = "（等待广播回应，暂无主机）";
            currentTranslations["ui.hostList.connect"] = "连接";
            currentTranslations["ui.manualConnect.title"] = "手动输入IP和端口连接:";
            currentTranslations["ui.manualConnect.ip"] = "IP:";
            currentTranslations["ui.manualConnect.port"] = "端口:";
            currentTranslations["ui.manualConnect.button"] = "手动连接";
            currentTranslations["ui.manualConnect.portError"] = "端口格式错误";
            currentTranslations["ui.status.label"] = "状态:";
            currentTranslations["ui.status.notConnected"] = "未连接";
            currentTranslations["ui.status.connecting"] = "连接中...";
            currentTranslations["ui.status.connected"] = "已连接";
            currentTranslations["ui.server.listenPort"] = "服务器监听端口:";
            currentTranslations["ui.server.connections"] = "当前连接数:";
            currentTranslations["ui.playerStatus.toggle"] = "显示玩家状态窗口（切换键: {0}）";
            currentTranslations["ui.playerStatus.id"] = "ID:";
            currentTranslations["ui.playerStatus.name"] = "名称:";
            currentTranslations["ui.playerStatus.latency"] = "延迟:";
            currentTranslations["ui.playerStatus.inGame"] = "游戏中:";
            currentTranslations["ui.playerStatus.yes"] = "是";
            currentTranslations["ui.playerStatus.no"] = "否";
            currentTranslations["ui.debug.printLootBoxes"] = "[调试] 打印此地图中的所有战利品箱";
            currentTranslations["ui.vote.mapVote"] = "地图投票 / 准备  [{0}]";
            currentTranslations["ui.vote.pressKey"] = "按 {0} 切换准备状态（当前: {1}）";
            currentTranslations["ui.vote.ready"] = "准备";
            currentTranslations["ui.vote.notReady"] = "未准备";
            currentTranslations["ui.vote.playerReadyStatus"] = "玩家准备状态:";
            currentTranslations["ui.vote.readyIcon"] = "✅ 准备";
            currentTranslations["ui.vote.notReadyIcon"] = "⌛ 未准备";
            currentTranslations["ui.spectator.mode"] = "观战模式: 左键 ▶ 下一个 | 右键 ◀ 上一个 | 观战中";

            // 场景相关
            currentTranslations["scene.waitingForHost"] = "[联机] 等待主机完成加载…（如延迟将在30秒后自动进入）";
            currentTranslations["scene.hostReady"] = "主机准备完毕，正在进入…";

            // 网络相关
            currentTranslations["net.connectionSuccess"] = "连接成功: {0}";
            currentTranslations["net.connectedTo"] = "已连接到 {0}";
            currentTranslations["net.disconnected"] = "已断开连接: {0}，原因: {1}";
            currentTranslations["net.connectionLost"] = "连接丢失";
            currentTranslations["net.networkError"] = "网络错误: {0} 来自 {1}";
            currentTranslations["net.hostDiscovered"] = "发现主机: {0}";
            currentTranslations["net.serverStarted"] = "服务器已启动，监听端口 {0}";
            currentTranslations["net.serverStartFailed"] = "服务器启动失败，请检查端口是否已被占用";
            currentTranslations["net.clientStarted"] = "客户端已启动";
            currentTranslations["net.clientStartFailed"] = "客户端启动失败";
            currentTranslations["net.networkStarted"] = "网络已启动";
            currentTranslations["net.networkStopped"] = "网络已停止";
            currentTranslations["net.ipEmpty"] = "IP地址为空";
            currentTranslations["net.invalidPort"] = "无效端口";
            currentTranslations["net.serverModeCannotConnect"] = "服务器模式无法连接到其他主机";
            currentTranslations["net.alreadyConnecting"] = "正在连接中。";
            currentTranslations["net.clientNetworkStartFailed"] = "启动客户端网络失败: {0}";
            currentTranslations["net.clientNetworkStartFailedStatus"] = "客户端网络启动失败";
            currentTranslations["net.clientNotStarted"] = "客户端未启动";
            currentTranslations["net.connectingTo"] = "正在连接到: {0}:{1}";
            currentTranslations["net.connectionFailedLog"] = "连接主机失败: {0}";
            currentTranslations["net.connectionFailed"] = "连接失败";
            
            // 死亡物品保留相关
            currentTranslations["death.itemPreserve.enabled"] = "客户端死亡物品保留已启用（临时修复）";
            currentTranslations["death.itemPreserve.notice"] = "注意：这是坟墓系统问题的临时解决方案";
            currentTranslations["death.itemPreserve.inventoryBlocked"] = "已阻止死亡时清空库存";
        }

        /// <summary>
        /// 获取翻译后的字符串
        /// </summary>
        /// <param name="key">翻译键</param>
        /// <param name="args">格式化参数</param>
        /// <returns>翻译后的字符串</returns>
        public static string Get(string key, params object[] args)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (currentTranslations.TryGetValue(key, out string value))
            {
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        return string.Format(value, args);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[CoopLocalization] 键 '{key}' 的格式化错误: {e.Message}");
                        return value;
                    }
                }
                return value;
            }

            Debug.LogWarning($"[CoopLocalization] 缺少翻译键: {key}");
            return $"[{key}]";
        }

        /// <summary>
        /// 更改语言
        /// </summary>
        /// <param name="languageCode">语言代码 (zh-CN, en-US, ko-KR, ja-JP)</param>
        public static void SetLanguage(string languageCode)
        {
            if (currentLanguageCode == languageCode) return;

            currentLanguageCode = languageCode;
            LoadTranslations(languageCode);
            Debug.Log($"[CoopLocalization] 语言已更改为: {languageCode}");
        }

        /// <summary>
        /// 获取当前语言代码
        /// </summary>
        public static string GetCurrentLanguage()
        {
            return currentLanguageCode;
        }
    }
}
