using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using Duckov.Utilities;
using EscapeFromDuckovCoopMod.Utils.Database;
using EscapeFromDuckovCoopMod.Utils.Logger.Tools;
using EscapeFromDuckovCoopMod.Net;

namespace EscapeFromDuckovCoopMod;

/// <summary>
/// 战利品箱同步管理器 - 协调主机和客户端的数据库操作
/// </summary>
public class LootBoxSyncManager
{
    // 主机端数据库（仅主机使用）
    private LootBoxDatabase _hostDatabase;
    
    // 客户端当前打开的箱子（仅在打开箱子时有数据）
    private LootStateResponse _currentLootBox;
    
    // 统计字段
    private int _queryCount = 0;
    private int _operationCount = 0;
    private long _totalLatencyMs = 0;
    private System.Diagnostics.Stopwatch _operationStopwatch = new System.Diagnostics.Stopwatch();
    
    // 单例模式
    private static LootBoxSyncManager _instance;
    public static LootBoxSyncManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new LootBoxSyncManager();
            return _instance;
        }
    }

    private bool IsHost => NetService.Instance?.IsServer ?? false;

    private LootBoxSyncManager()
    {
        // 私有构造函数，强制使用单例
    }

    #region 主机端方法

    /// <summary>
    /// 主机：初始化数据库，扫描场景中的所有战利品箱
    /// 1. 扫描所有战利品箱
    /// 2. 从 Inventory 读取物品并保存到数据库
    /// 3. 清空 Inventory（避免数据不一致）
    /// 4. 数据库成为唯一的数据源
    /// </summary>
    public void Host_InitializeDatabase()
    {
        if (!IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_InitializeDatabase called on client, ignoring");
            return;
        }

        LoggerHelper.Log("[LootBoxSync] Starting database initialization...");

        // 创建数据库实例（50m 网格大小）
        _hostDatabase = new LootBoxDatabase(spatialCellSize: 50f);

        // 扫描场景中的所有战利品箱
        int totalBoxes = 0;
        int totalItems = 0;

        try
        {
            // 使用缓存管理器获取所有战利品箱（优化性能）
            IEnumerable<InteractableLootbox> lootboxes;
            
            if (Utils.GameObjectCacheManager.Instance != null)
            {
                lootboxes = Utils.GameObjectCacheManager.Instance.Loot.GetAllLootboxes();
                LoggerHelper.Log("[LootBoxSync] Using cached lootboxes from GameObjectCacheManager");
            }
            else
            {
                // 降级方案：直接使用 FindObjectsOfType
                lootboxes = UnityEngine.Object.FindObjectsOfType<InteractableLootbox>(true);
                LoggerHelper.LogWarning("[LootBoxSync] GameObjectCacheManager not available, using FindObjectsOfType");
            }

            foreach (var lootbox in lootboxes)
            {
                if (lootbox == null) continue;

                var inv = lootbox.Inventory;
                if (inv == null) continue;

                // 跳过私有容器（如玩家背包）
                if (LootboxDetectUtil.IsPrivateInventory(inv))
                    continue;

                // 生成唯一的 SetId（使用场景名 + 位置哈希）
                var setId = GenerateLootBoxSetId(lootbox.gameObject);

                // 读取物品并保存到数据库
                int itemCount = Host_ReadAndStoreLootBoxItems(lootbox.gameObject, inv, setId);
                
                totalBoxes++;
                totalItems += itemCount;
            }

            LoggerHelper.Log($"[LootBoxSync] Database initialization complete: {totalBoxes} loot boxes, {totalItems} total items");
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Database initialization failed: {ex.Message}");
            LoggerHelper.LogError($"[LootBoxSync] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 生成战利品箱的唯一 SetId（场景名 + 位置哈希）
    /// </summary>
    private string GenerateLootBoxSetId(GameObject go)
    {
        if (go == null) return Guid.NewGuid().ToString();

        var sceneName = go.scene.name;
        var pos = go.transform.position;
        
        // 使用位置哈希（精度到厘米）
        var posKey = $"{Mathf.RoundToInt(pos.x * 100)}_{Mathf.RoundToInt(pos.y * 100)}_{Mathf.RoundToInt(pos.z * 100)}";
        
        return $"{sceneName}_{posKey}";
    }

    /// <summary>
    /// 读取战利品箱中的物品并存储到数据库，然后清空 Inventory
    /// </summary>
    private int Host_ReadAndStoreLootBoxItems(GameObject go, Inventory inv, string setId)
    {
        if (go == null || inv == null || string.IsNullOrEmpty(setId))
            return 0;

        try
        {
            // 创建实体并添加到数据库
            var entity = new LootBoxEntity(go, inv, setId);
            
            // 读取所有物品
            var content = inv.Content;
            if (content != null)
            {
                for (int i = 0; i < content.Count; i++)
                {
                    var item = content[i];
                    if (item == null) continue;

                    try
                    {
                        // 将物品转换为快照
                        var snapshot = ItemTool.MakeSnapshot(item);
                        
                        // 创建 LootItemEntry 并添加到实体
                        var entry = new LootItemEntry(i, snapshot);
                        entity.Items.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogError($"[LootBoxSync] Failed to snapshot item at position {i} in {setId}: {ex.Message}");
                    }
                }
            }

            // 插入数据库
            if (!_hostDatabase.AddLootBox(go, inv, setId))
            {
                LoggerHelper.LogWarning($"[LootBoxSync] Failed to add loot box to database: {setId}");
                return 0;
            }

            int itemCount = entity.Items.Count;
            
            // 清空 Inventory（从后往前删除，避免索引问题）
            for (int i = content.Count - 1; i >= 0; i--)
            {
                try
                {
                    Item removed;
                    if (inv.RemoveAt(i, out removed) && removed != null)
                    {
                        UnityEngine.Object.Destroy(removed.gameObject);
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError($"[LootBoxSync] Failed to remove item at position {i} in {setId}: {ex.Message}");
                }
            }

            if (itemCount > 0)
            {
                LoggerHelper.Log($"[LootBoxSync] Initialized loot box {setId}: {itemCount} items");
            }

            return itemCount;
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Failed to read loot box {setId}: {ex.Message}");
            return 0;
        }
    }

    #endregion

    #region 客户端方法

    // UI 状态标志
    private bool _isLoadingLootBox = false;
    private string _loadingLootBoxSetId = null;

    /// <summary>
    /// 客户端：请求打开战利品箱
    /// </summary>
    /// <param name="setId">战利品箱的唯一标识符</param>
    public void Client_RequestOpen(string setId)
    {
        if (IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] 主机不应该调用 Client_RequestOpen");
            return;
        }

        if (string.IsNullOrEmpty(setId))
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestOpen: setId 为空");
            return;
        }

        // 防止重复请求
        if (_isLoadingLootBox)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] 正在加载战利品箱 {_loadingLootBoxSetId}，忽略新请求 {setId}");
            return;
        }

        var service = NetService.Instance;
        if (service == null || service.connectedPeer == null)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestOpen: 未连接到主机");
            return;
        }

        try
        {
            // 🔧 更新 UI 状态：显示加载中，禁用交互
            SetLootBoxLoadingState(true, setId);

            // 创建打开请求消息
            var request = new LootOpenRequest(setId, requestVersion: 1);

            // 序列化为 JSON（使用 Newtonsoft.Json 以支持复杂对象）
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);

            // 通过 JsonMessage 工具类发送给主机
            JsonMessage.SendToHost(json, LiteNetLib.DeliveryMethod.ReliableOrdered);

            // 记录日志
            LoggerHelper.Log($"[LOOT][Client] Request: OPEN, SetId: {setId}, Details: Time={DateTime.Now:HH:mm:ss.fff}");
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Client_RequestOpen 失败: {ex.Message}");
            
            // 发生错误时恢复 UI 状态
            SetLootBoxLoadingState(false, null);
        }
    }

    /// <summary>
    /// 设置战利品箱加载状态（用于 UI 更新）
    /// </summary>
    /// <param name="isLoading">是否正在加载</param>
    /// <param name="setId">正在加载的战利品箱 SetId</param>
    private void SetLootBoxLoadingState(bool isLoading, string setId)
    {
        _isLoadingLootBox = isLoading;
        _loadingLootBoxSetId = setId;

        if (isLoading)
        {
            LoggerHelper.Log($"[LootBoxSync][UI] 显示加载中 UI: SetId={setId}");
            // TODO: 在任务 9 中实现 UI 更新
            // - 显示加载中动画或提示
            // - 禁用 UI 交互（防止重复点击）
            // - 可能需要调用 LootView.ShowLoading() 或类似方法
        }
        else
        {
            LoggerHelper.Log($"[LootBoxSync][UI] 隐藏加载中 UI");
            // TODO: 在任务 9 中实现 UI 恢复
            // - 隐藏加载中动画
            // - 启用 UI 交互
            // - 可能需要调用 LootView.HideLoading() 或类似方法
        }
    }

    /// <summary>
    /// 获取当前是否正在加载战利品箱
    /// </summary>
    public bool IsLoadingLootBox => _isLoadingLootBox;

    /// <summary>
    /// 获取当前正在加载的战利品箱 SetId
    /// </summary>
    public string LoadingLootBoxSetId => _loadingLootBoxSetId;

    // 客户端：待处理的放入物品（token -> Item）
    private Dictionary<uint, Item> _pendingPutItems = new Dictionary<uint, Item>();
    
    // 客户端：待处理的取出物品目的地（token -> DestInfo）
    private Dictionary<uint, TakeDestinationInfo> _pendingTakeDestinations = new Dictionary<uint, TakeDestinationInfo>();
    
    // 客户端：Token 生成器
    private uint _nextToken = 1;

    /// <summary>
    /// 取出物品的目的地信息
    /// </summary>
    public class TakeDestinationInfo
    {
        public string DestType { get; set; }  // "Backpack", "Slot", "Specific"
        public Inventory DestInv { get; set; }
        public int DestPos { get; set; }
        public Slot DestSlot { get; set; }
    }

    /// <summary>
    /// 客户端：请求放入物品到战利品箱
    /// 1. 生成唯一的 Token
    /// 2. 将物品序列化为 ItemSnapshot
    /// 3. 创建 LootPutRequest 消息
    /// 4. 保存待处理的物品
    /// 5. 发送给主机
    /// </summary>
    /// <param name="setId">战利品箱的唯一标识符</param>
    /// <param name="item">要放入的物品</param>
    /// <param name="preferredPosition">期望的位置（-1 表示自动选择）</param>
    public void Client_RequestPut(string setId, Item item, int preferredPosition)
    {
        if (IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] 主机不应该调用 Client_RequestPut");
            return;
        }

        if (string.IsNullOrEmpty(setId))
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestPut: setId 为空");
            return;
        }

        if (item == null)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestPut: item 为空");
            return;
        }

        var service = NetService.Instance;
        if (service == null || service.connectedPeer == null)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestPut: 未连接到主机");
            return;
        }

        try
        {
            // 10.1 生成唯一的 Token
            var token = _nextToken++;

            // 防止重复请求：检查是否已经有相同物品的在途请求
            foreach (var kv in _pendingPutItems)
            {
                var pending = kv.Value;
                if (pending != null && ReferenceEquals(pending, item))
                {
                    LoggerHelper.LogWarning($"[LootBoxSync][Client] 重复的 PUT 请求被抑制: item={item.DisplayName}, existingToken={kv.Key}");
                    return;
                }
            }

            // 10.1 将物品序列化为 ItemSnapshot
            var snapshot = ItemTool.MakeSnapshot(item);

            // 10.2 保存待处理的物品
            _pendingPutItems[token] = item;

            // 10.2 创建 LootPutRequest 消息
            var request = new Net.LootPutRequest(
                setId: setId,
                preferredPosition: preferredPosition,
                token: token,
                itemSnapshot: snapshot
            );

            // 10.2 序列化为 JSON
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);

            // 10.2 通过 JsonMessage 工具类发送给主机
            JsonMessage.SendToHost(json, LiteNetLib.DeliveryMethod.ReliableOrdered);

            // 10.2 记录日志
            LoggerHelper.Log($"[LOOT][Client] Request: PUT, SetId: {setId}, Details: Token={token}, ItemType={item.TypeID}, ItemName={item.DisplayName}, PreferredPos={preferredPosition}, Time={DateTime.Now:HH:mm:ss.fff}");
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Client_RequestPut 失败: {ex.Message}");
            LoggerHelper.LogError($"[LootBoxSync] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 客户端：处理战利品箱状态响应
    /// 1. 保存当前打开的箱子
    /// 2. 记录日志
    /// 3. 启动协程更新 UI
    /// </summary>
    /// <param name="response">战利品箱状态响应</param>
    public void Client_HandleStateResponse(Net.LootStateResponse response)
    {
        if (IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] 主机不应该调用 Client_HandleStateResponse");
            return;
        }

        if (response == null)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_HandleStateResponse: response 为空");
            SetLootBoxLoadingState(false, null);
            return;
        }

        var setId = response.setId;
        var itemCount = response.items?.Count ?? 0;
        var receiveTime = DateTime.Now;

        LoggerHelper.Log($"[LootBoxSync][Client] 收到状态响应: SetId={setId}, 物品数量={itemCount}, 容量={response.capacity}, 接收时间={receiveTime:HH:mm:ss.fff}");

        // 保存当前打开的箱子
        _currentLootBox = response;

        // 启动协程更新 UI（分帧处理，避免卡顿）
        var mod = ModBehaviourF.Instance;
        if (mod != null)
        {
            mod.StartCoroutine(ApplyLootboxStateCoroutine(response));
        }
        else
        {
            LoggerHelper.LogError("[LootBoxSync] ModBehaviourF.Instance 为空，无法启动协程");
            SetLootBoxLoadingState(false, null);
        }
    }

    /// <summary>
    /// 协程：分帧处理战利品箱状态更新，避免主线程阻塞导致掉帧
    /// 1. 清空 UI 中的旧物品（遍历并销毁）
    /// 2. 遍历 response.Items，为每个物品创建 UI 元素
    /// 3. 分帧处理：每 5 个物品 yield return null
    /// 4. 记录处理耗时
    /// </summary>
    /// <param name="response">战利品箱状态响应</param>
    private System.Collections.IEnumerator ApplyLootboxStateCoroutine(Net.LootStateResponse response)
    {
        if (response == null)
        {
            LoggerHelper.LogError("[LootBoxSync] ApplyLootboxStateCoroutine: response 为空");
            SetLootBoxLoadingState(false, null);
            yield break;
        }

        var startTime = DateTime.Now;
        var setId = response.setId;
        var capacity = response.capacity;
        var items = response.items ?? new List<LootItemEntry>();

        LoggerHelper.Log($"[LootBoxSync][Client] 开始应用战利品箱状态: SetId={setId}, 物品数量={items.Count}");

        // 查找对应的 Inventory（通过 SetId）
        Inventory targetInventory = FindInventoryBySetId(setId);

        if (targetInventory == null)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] 无法找到对应的 Inventory: SetId={setId}");
            SetLootBoxLoadingState(false, null);
            yield break;
        }

        // 设置容量
        targetInventory.SetCapacity(capacity);
        targetInventory.Loading = false;

        // 9.2.1 清空 UI 中的旧物品（每删除 5 个物品后 yield 一次）
        int deleteCount = 0;
        var content = targetInventory.Content;
        
        for (int i = content.Count - 1; i >= 0; i--)
        {
            try
            {
                Item removed;
                if (targetInventory.RemoveAt(i, out removed) && removed != null)
                {
                    UnityEngine.Object.Destroy(removed.gameObject);
                }

                deleteCount++;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"[LootBoxSync] 删除物品失败: position={i}, error={ex.Message}");
            }

            // 每删除5个物品后等待1帧（yield 必须在 try-catch 外面）
            if (deleteCount % 5 == 0)
            {
                yield return null;
            }
        }

        LoggerHelper.Log($"[LootBoxSync][Client] 已清空旧物品: 删除数量={deleteCount}");

        // 9.2.2 遍历 response.Items，为每个物品创建 UI 元素（每添加 5 个物品后 yield 一次）
        int addCount = 0;
        
        foreach (var entry in items)
        {
            if (entry == null) continue;

            bool itemAdded = false;
            try
            {
                // 从快照重建物品
                var item = ItemTool.BuildItemFromSnapshot(entry.Snapshot);
                
                if (item == null)
                {
                    LoggerHelper.LogWarning($"[LootBoxSync] 无法从快照重建物品: position={entry.Position}, typeId={entry.Snapshot.typeId}");
                    continue;
                }

                // 添加到指定位置
                if (!targetInventory.AddAt(item, entry.Position))
                {
                    LoggerHelper.LogWarning($"[LootBoxSync] 无法添加物品到位置: position={entry.Position}, typeId={entry.Snapshot.typeId}");
                    
                    // 如果添加失败，销毁物品
                    UnityEngine.Object.Destroy(item.gameObject);
                    continue;
                }

                itemAdded = true;
                addCount++;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"[LootBoxSync] 添加物品失败: position={entry.Position}, error={ex.Message}");
            }

            // 每添加 5 个物品后等待1帧（yield 必须在 try-catch 外面）
            if (itemAdded && addCount % 5 == 0)
            {
                yield return null;
            }
        }

        LoggerHelper.Log($"[LootBoxSync][Client] 已添加新物品: 添加数量={addCount}");

        // 9.2.3 记录处理耗时
        var endTime = DateTime.Now;
        var elapsed = (endTime - startTime).TotalMilliseconds;
        LoggerHelper.Log($"[LootBoxSync][Client] 应用战利品箱状态完成: SetId={setId}, 耗时={elapsed:F2}ms");

        // 9.3 实现 UI 状态恢复（在协程的最后）
        RestoreLootBoxUIState(targetInventory);
    }

    /// <summary>
    /// 查找对应的 Inventory（通过 SetId）
    /// 注意：这是一个简化实现，实际可能需要更复杂的查找逻辑
    /// </summary>
    private Inventory FindInventoryBySetId(string setId)
    {
        if (string.IsNullOrEmpty(setId))
            return null;

        try
        {
            // 尝试通过 LootView 获取当前打开的 Inventory
            var lootView = Duckov.UI.LootView.Instance;
            if (lootView != null && lootView.open)
            {
                var targetInv = lootView.TargetInventory;
                if (targetInv != null)
                {
                    // 验证这是否是我们要找的 Inventory
                    // 注意：这里需要根据实际情况验证
                    // 可能需要通过 GameObject 的位置或其他方式匹配
                    return targetInv;
                }
            }

            // 如果 LootView 没有打开，尝试通过其他方式查找
            // 例如：通过 LevelManager.LootBoxInventories 或 GameObjectCacheManager
            LoggerHelper.LogWarning($"[LootBoxSync] LootView 未打开或 TargetInventory 为空，无法找到 Inventory: SetId={setId}");
            return null;
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] FindInventoryBySetId 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 恢复战利品箱 UI 状态
    /// 1. 隐藏加载中 UI
    /// 2. 启用 UI 交互
    /// 3. 刷新容量文本、按钮状态等
    /// </summary>
    private void RestoreLootBoxUIState(Inventory targetInventory)
    {
        try
        {
            // 隐藏加载中 UI，启用交互
            SetLootBoxLoadingState(false, null);

            // 刷新 LootView UI（如果箱子正在被查看）
            var lootView = Duckov.UI.LootView.Instance;
            if (lootView != null && lootView.open && ReferenceEquals(lootView.TargetInventory, targetInventory))
            {
                LoggerHelper.Log("[LootBoxSync][UI] 刷新 LootView UI");

                // 轻量刷新：不强制重开，只更新细节/按钮与容量文本
                try
                {
                    AccessTools.Method(typeof(Duckov.UI.LootView), "RefreshDetails")?.Invoke(lootView, null);
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogWarning($"[LootBoxSync] RefreshDetails 失败: {ex.Message}");
                }

                try
                {
                    AccessTools.Method(typeof(Duckov.UI.LootView), "RefreshPickAllButton")?.Invoke(lootView, null);
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogWarning($"[LootBoxSync] RefreshPickAllButton 失败: {ex.Message}");
                }

                try
                {
                    AccessTools.Method(typeof(Duckov.UI.LootView), "RefreshCapacityText")?.Invoke(lootView, null);
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogWarning($"[LootBoxSync] RefreshCapacityText 失败: {ex.Message}");
                }

                LoggerHelper.Log("[LootBoxSync][UI] LootView UI 刷新完成");
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] RestoreLootBoxUIState 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 客户端：请求取出物品从战利品箱
    /// 1. 生成唯一的 Token
    /// 2. 创建目的地信息对象（destType, destInv, destPos, destSlot）
    /// 3. 保存目的地信息
    /// 4. 创建 LootTakeRequest 消息
    /// 5. 发送给主机
    /// </summary>
    /// <param name="setId">战利品箱的唯一标识符</param>
    /// <param name="position">要取出的物品位置</param>
    /// <param name="destType">目的地类型（"Backpack", "Slot", "Specific"）</param>
    /// <param name="destInv">目的地背包（可选）</param>
    /// <param name="destPos">目的地位置（可选，用于 Specific 类型）</param>
    /// <param name="destSlot">目的地装备槽（可选，用于 Slot 类型）</param>
    /// <returns>生成的 Token</returns>
    public uint Client_RequestTake(
        string setId, 
        int position, 
        string destType = "Backpack",
        Inventory destInv = null,
        int destPos = -1,
        Slot destSlot = null)
    {
        if (IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] 主机不应该调用 Client_RequestTake");
            return 0;
        }

        if (string.IsNullOrEmpty(setId))
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestTake: setId 为空");
            return 0;
        }

        if (position < 0)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestTake: position 无效");
            return 0;
        }

        var service = NetService.Instance;
        if (service == null || service.connectedPeer == null)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestTake: 未连接到主机");
            return 0;
        }

        try
        {
            // 11.1 生成唯一的 Token
            var token = _nextToken++;

            // 11.1 创建目的地信息对象
            var destInfo = new TakeDestinationInfo
            {
                DestType = destType ?? "Backpack",
                DestInv = destInv,
                DestPos = destPos,
                DestSlot = destSlot
            };

            // 11.1 保存目的地信息
            _pendingTakeDestinations[token] = destInfo;

            // 11.2 创建 LootTakeRequest 消息
            var request = new Net.LootTakeRequest(
                setId: setId,
                position: position,
                token: token,
                destType: destType ?? "Backpack",
                destPosition: destPos
            );

            // 11.2 序列化为 JSON
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);

            // 11.2 通过 JsonMessage 工具类发送给主机
            JsonMessage.SendToHost(json, LiteNetLib.DeliveryMethod.ReliableOrdered);

            // 11.2 记录日志
            LoggerHelper.Log($"[LOOT][Client] Request: TAKE, SetId: {setId}, Details: Token={token}, Position={position}, DestType={destType}, DestPos={destPos}, Time={DateTime.Now:HH:mm:ss.fff}");

            return token;
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Client_RequestTake 失败: {ex.Message}");
            LoggerHelper.LogError($"[LootBoxSync] Stack trace: {ex.StackTrace}");
            return 0;
        }
    }

    /// <summary>
    /// 客户端：请求拆分物品
    /// 1. 创建 LootSplitRequest 消息
    /// 2. 通过 JsonMessage 工具类发送给主机
    /// 3. 记录日志
    /// </summary>
    /// <param name="setId">战利品箱的唯一标识符</param>
    /// <param name="sourcePosition">源物品位置</param>
    /// <param name="count">要拆分的数量</param>
    /// <param name="preferredPosition">期望的目标位置（-1 表示自动选择）</param>
    public void Client_RequestSplit(string setId, int sourcePosition, int count, int preferredPosition)
    {
        if (IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] 主机不应该调用 Client_RequestSplit");
            return;
        }

        if (string.IsNullOrEmpty(setId))
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestSplit: setId 为空");
            return;
        }

        if (sourcePosition < 0)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestSplit: sourcePosition 无效");
            return;
        }

        if (count <= 0)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestSplit: count 必须大于 0");
            return;
        }

        var service = NetService.Instance;
        if (service == null || service.connectedPeer == null)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_RequestSplit: 未连接到主机");
            return;
        }

        try
        {
            // 创建 LootSplitRequest 消息
            var request = new Net.LootSplitRequest(
                setId: setId,
                sourcePosition: sourcePosition,
                count: count,
                preferredPosition: preferredPosition
            );

            // 序列化为 JSON
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);

            // 通过 JsonMessage 工具类发送给主机
            JsonMessage.SendToHost(json, LiteNetLib.DeliveryMethod.ReliableOrdered);

            // 记录日志
            LoggerHelper.Log($"[LOOT][Client] Request: SPLIT, SetId: {setId}, Details: SourcePos={sourcePosition}, Count={count}, PreferredPos={preferredPosition}, Time={DateTime.Now:HH:mm:ss.fff}");
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Client_RequestSplit 失败: {ex.Message}");
            LoggerHelper.LogError($"[LootBoxSync] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 客户端：处理物品添加广播
    /// 1. 检查是否是当前打开的箱子
    /// 2. 如果不是，忽略并返回
    /// 3. 如果是，从 ItemSnapshot 重建物品并添加到 UI（指定位置）
    /// 4. 记录日志
    /// </summary>
    /// <param name="message">物品添加广播消息</param>
    public void Client_HandleItemAdded(Net.LootItemAdded message)
    {
        if (IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] 主机不应该调用 Client_HandleItemAdded");
            return;
        }

        if (message == null)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_HandleItemAdded: message 为空");
            return;
        }

        var setId = message.setId;
        var position = message.position;
        var itemSnapshot = message.itemSnapshot;

        // 14.1 检查是否是当前打开的箱子
        if (_currentLootBox == null || _currentLootBox.setId != setId)
        {
            // 不是当前打开的箱子，忽略
            LoggerHelper.Log($"[LootBoxSync][Client] 忽略 ITEM_ADDED 广播: SetId={setId} (当前打开的箱子: {_currentLootBox?.setId ?? "null"})");
            return;
        }

        LoggerHelper.Log($"[LootBoxSync][Client] 收到 ITEM_ADDED 广播: SetId={setId}, Position={position}, ItemType={itemSnapshot.typeId}");

        try
        {
            // 查找对应的 Inventory
            Inventory targetInventory = FindInventoryBySetId(setId);

            if (targetInventory == null)
            {
                LoggerHelper.LogWarning($"[LootBoxSync] 无法找到对应的 Inventory: SetId={setId}");
                return;
            }

            // 14.1 从 ItemSnapshot 重建物品
            var item = ItemTool.BuildItemFromSnapshot(itemSnapshot);

            if (item == null)
            {
                LoggerHelper.LogWarning($"[LootBoxSync] 无法从快照重建物品: position={position}, typeId={itemSnapshot.typeId}");
                return;
            }

            // 14.1 添加到 UI（指定位置）
            if (!targetInventory.AddAt(item, position))
            {
                LoggerHelper.LogWarning($"[LootBoxSync] 无法添加物品到位置: position={position}, typeId={itemSnapshot.typeId}");

                // 如果添加失败，销毁物品
                UnityEngine.Object.Destroy(item.gameObject);
                return;
            }

            // 14.1 记录日志
            LoggerHelper.Log($"[LootBoxSync][Client] 成功添加物品: SetId={setId}, Position={position}, ItemType={itemSnapshot.typeId}, ItemName={item.DisplayName}");

            // 更新 _currentLootBox 的 items 列表（保持同步）
            if (_currentLootBox.items == null)
                _currentLootBox.items = new List<LootItemEntry>();

            _currentLootBox.items.Add(new LootItemEntry(position, itemSnapshot));
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Client_HandleItemAdded 失败: {ex.Message}");
            LoggerHelper.LogError($"[LootBoxSync] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 客户端：处理物品移除广播
    /// 1. 检查是否是当前打开的箱子
    /// 2. 如果不是，忽略并返回
    /// 3. 如果是，从 UI 中移除对应位置的物品（销毁 UI 元素）
    /// 4. 记录日志
    /// </summary>
    /// <param name="message">物品移除广播消息</param>
    public void Client_HandleItemRemoved(Net.LootItemRemoved message)
    {
        if (IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] 主机不应该调用 Client_HandleItemRemoved");
            return;
        }

        if (message == null)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_HandleItemRemoved: message 为空");
            return;
        }

        var setId = message.setId;
        var position = message.position;

        // 14.2 检查是否是当前打开的箱子
        if (_currentLootBox == null || _currentLootBox.setId != setId)
        {
            // 不是当前打开的箱子，忽略
            LoggerHelper.Log($"[LootBoxSync][Client] 忽略 ITEM_REMOVED 广播: SetId={setId} (当前打开的箱子: {_currentLootBox?.setId ?? "null"})");
            return;
        }

        LoggerHelper.Log($"[LootBoxSync][Client] 收到 ITEM_REMOVED 广播: SetId={setId}, Position={position}");

        try
        {
            // 查找对应的 Inventory
            Inventory targetInventory = FindInventoryBySetId(setId);

            if (targetInventory == null)
            {
                LoggerHelper.LogWarning($"[LootBoxSync] 无法找到对应的 Inventory: SetId={setId}");
                return;
            }

            // 14.2 从 UI 中移除对应位置的物品
            var content = targetInventory.Content;
            if (position >= 0 && position < content.Count)
            {
                var item = content[position];
                if (item != null)
                {
                    var itemName = item.DisplayName;
                    var itemType = item.TypeID;

                    // 从 Inventory 移除
                    Item removed;
                    if (targetInventory.RemoveAt(position, out removed) && removed != null)
                    {
                        // 销毁 UI 元素
                        UnityEngine.Object.Destroy(removed.gameObject);

                        // 14.2 记录日志
                        LoggerHelper.Log($"[LootBoxSync][Client] 成功移除物品: SetId={setId}, Position={position}, ItemType={itemType}, ItemName={itemName}");
                    }
                    else
                    {
                        LoggerHelper.LogWarning($"[LootBoxSync] 移除物品失败: SetId={setId}, Position={position}");
                    }
                }
                else
                {
                    LoggerHelper.LogWarning($"[LootBoxSync] 位置上没有物品: SetId={setId}, Position={position}");
                }
            }
            else
            {
                LoggerHelper.LogWarning($"[LootBoxSync] 无效的位置: SetId={setId}, Position={position}, Capacity={content.Count}");
            }

            // 更新 _currentLootBox 的 items 列表（保持同步）
            if (_currentLootBox.items != null)
            {
                _currentLootBox.items.RemoveAll(entry => entry.Position == position);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Client_HandleItemRemoved 失败: {ex.Message}");
            LoggerHelper.LogError($"[LootBoxSync] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 客户端：处理物品修改广播
    /// 1. 检查是否是当前打开的箱子
    /// 2. 如果不是，忽略并返回
    /// 3. 如果是，更新对应位置的物品（如堆叠数量、耐久度）
    /// 4. 记录日志
    /// </summary>
    /// <param name="message">物品修改广播消息</param>
    public void Client_HandleItemModified(Net.LootItemModified message)
    {
        if (IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] 主机不应该调用 Client_HandleItemModified");
            return;
        }

        if (message == null)
        {
            LoggerHelper.LogError("[LootBoxSync] Client_HandleItemModified: message 为空");
            return;
        }

        var setId = message.setId;
        var position = message.position;
        var itemSnapshot = message.itemSnapshot;

        // 14.3 检查是否是当前打开的箱子
        if (_currentLootBox == null || _currentLootBox.setId != setId)
        {
            // 不是当前打开的箱子，忽略
            LoggerHelper.Log($"[LootBoxSync][Client] 忽略 ITEM_MODIFIED 广播: SetId={setId} (当前打开的箱子: {_currentLootBox?.setId ?? "null"})");
            return;
        }

        LoggerHelper.Log($"[LootBoxSync][Client] 收到 ITEM_MODIFIED 广播: SetId={setId}, Position={position}, ItemType={itemSnapshot.typeId}");

        try
        {
            // 查找对应的 Inventory
            Inventory targetInventory = FindInventoryBySetId(setId);

            if (targetInventory == null)
            {
                LoggerHelper.LogWarning($"[LootBoxSync] 无法找到对应的 Inventory: SetId={setId}");
                return;
            }

            // 14.3 更新对应位置的物品
            var content = targetInventory.Content;
            if (position >= 0 && position < content.Count)
            {
                var oldItem = content[position];
                if (oldItem != null)
                {
                    var oldItemName = oldItem.DisplayName;
                    var oldStack = oldItem.StackCount;

                    // 移除旧物品
                    Item removed;
                    if (targetInventory.RemoveAt(position, out removed) && removed != null)
                    {
                        UnityEngine.Object.Destroy(removed.gameObject);
                    }

                    // 从新快照重建物品
                    var newItem = ItemTool.BuildItemFromSnapshot(itemSnapshot);

                    if (newItem == null)
                    {
                        LoggerHelper.LogWarning($"[LootBoxSync] 无法从快照重建物品: position={position}, typeId={itemSnapshot.typeId}");
                        return;
                    }

                    // 添加到相同位置
                    if (!targetInventory.AddAt(newItem, position))
                    {
                        LoggerHelper.LogWarning($"[LootBoxSync] 无法添加修改后的物品到位置: position={position}, typeId={itemSnapshot.typeId}");

                        // 如果添加失败，销毁物品
                        UnityEngine.Object.Destroy(newItem.gameObject);
                        return;
                    }

                    // 14.3 记录日志（包含修改内容）
                    var modifications = new List<string>();
                    if (oldStack != newItem.StackCount)
                        modifications.Add($"堆叠数量: {oldStack} -> {newItem.StackCount}");

                    var modificationStr = modifications.Count > 0 ? string.Join(", ", modifications) : "未知修改";
                    LoggerHelper.Log($"[LootBoxSync][Client] 成功修改物品: SetId={setId}, Position={position}, ItemType={itemSnapshot.typeId}, ItemName={oldItemName}, 修改内容: {modificationStr}");
                }
                else
                {
                    LoggerHelper.LogWarning($"[LootBoxSync] 位置上没有物品: SetId={setId}, Position={position}");
                }
            }
            else
            {
                LoggerHelper.LogWarning($"[LootBoxSync] 无效的位置: SetId={setId}, Position={position}, Capacity={content.Count}");
            }

            // 更新 _currentLootBox 的 items 列表（保持同步）
            if (_currentLootBox.items != null)
            {
                var existingEntry = _currentLootBox.items.Find(entry => entry.Position == position);
                if (existingEntry != null)
                {
                    existingEntry.Snapshot = itemSnapshot;
                }
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Client_HandleItemModified 失败: {ex.Message}");
            LoggerHelper.LogError($"[LootBoxSync] Stack trace: {ex.StackTrace}");
        }
    }

    #endregion

    #region 主机端请求处理

    /// <summary>
    /// 主机：处理打开战利品箱请求
    /// 1. 从数据库查询箱子（O(1)）
    /// 2. 返回完整物品列表
    /// </summary>
    public void Host_HandleOpenRequest(LiteNetLib.NetPeer peer, Net.LootOpenRequest request)
    {
        if (!IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandleOpenRequest called on client, ignoring");
            return;
        }

        if (peer == null)
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandleOpenRequest: peer is null");
            return;
        }

        if (request == null || string.IsNullOrEmpty(request.setId))
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandleOpenRequest: invalid request");
            return;
        }

        var setId = request.setId;
        var peerEndPoint = peer.EndPoint?.ToString() ?? "unknown";

        LoggerHelper.Log($"[LOOT][Host] Operation: OPEN, SetId: {setId}, Peer: {peerEndPoint}, Details: RequestVersion={request.requestVersion}");

        // 开始计时
        _operationStopwatch.Restart();

        // 4.1 从数据库查询战利品箱
        var entity = _hostDatabase?.GetLootBox(setId);
        
        // 更新统计：查询次数
        _queryCount++;

        // 4.2 错误处理：战利品箱不存在
        if (entity == null)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] LootBox not found: setId={setId}, peer={peerEndPoint}");

            // 发送错误响应
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: 0,
                errorMessage: "LootBoxNotFound"
            );

            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 4.3 成功响应：创建并发送状态响应
        var response = new Net.LootStateResponse(
            setId: entity.SetId,
            capacity: entity.Capacity,
            items: entity.Items
        );

        // 序列化为 JSON
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.None);
        int jsonSize = json.Length;

        LoggerHelper.Log($"[LOOT][Host] Operation: STATE_RESPONSE, SetId: {setId}, Peer: {peerEndPoint}, Details: Items={entity.Items.Count}, Size={jsonSize}bytes");

        // 发送给请求者
        SendJsonToPeer(peer, response);
        
        // 更新统计：操作次数和延迟
        _operationStopwatch.Stop();
        _operationCount++;
        _totalLatencyMs += _operationStopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// 发送 JSON 消息给指定 Peer
    /// </summary>
    private void SendJsonToPeer(LiteNetLib.NetPeer peer, object data)
    {
        if (peer == null || data == null)
        {
            LoggerHelper.LogWarning("[LootBoxSync] SendJsonToPeer: peer or data is null");
            return;
        }

        try
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.None);

            var service = NetService.Instance;
            if (service == null || service.writer == null)
            {
                LoggerHelper.LogError("[LootBoxSync] NetService or writer is null");
                return;
            }

            service.writer.Reset();
            service.writer.Put((byte)Op.JSON);
            service.writer.Put(json);
            peer.Send(service.writer, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Failed to send JSON to peer: {ex.Message}");
        }
    }

    /// <summary>
    /// 主机：处理放入物品请求
    /// 1. 验证请求
    /// 2. 更新数据库（添加物品）
    /// 3. 广播增量更新
    /// </summary>
    public void Host_HandlePutRequest(LiteNetLib.NetPeer peer, Net.LootPutRequest request)
    {
        if (!IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandlePutRequest called on client, ignoring");
            return;
        }

        if (peer == null)
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandlePutRequest: peer is null");
            return;
        }

        if (request == null || string.IsNullOrEmpty(request.setId))
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandlePutRequest: invalid request");
            return;
        }

        var setId = request.setId;
        var preferredPosition = request.preferredPosition;
        var token = request.token;
        var itemSnapshot = request.itemSnapshot;
        var peerEndPoint = peer.EndPoint?.ToString() ?? "unknown";

        LoggerHelper.Log($"[LOOT][Host] Operation: PUT, SetId: {setId}, Peer: {peerEndPoint}, Details: Token={token}, PreferredPos={preferredPosition}, ItemType={itemSnapshot.typeId}");

        // 开始计时
        _operationStopwatch.Restart();

        // 5.1 验证请求

        // 从数据库查询战利品箱
        var entity = _hostDatabase?.GetLootBox(setId);
        
        // 更新统计：查询次数
        _queryCount++;

        // 验证战利品箱是否存在
        if (entity == null)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] PUT failed: LootBox not found, setId={setId}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: token,
                errorMessage: "LootBoxNotFound"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 验证物品快照是否有效
        if (!IsValidItemSnapshot(itemSnapshot))
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] PUT failed: Invalid item snapshot, setId={setId}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: token,
                errorMessage: "ValidationFailed"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 5.2 实现物品添加

        // 查找合适的位置
        int targetPosition = FindAvailablePosition(entity, preferredPosition);

        // 如果没有空位，发送容量超限错误
        if (targetPosition < 0)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] PUT failed: Capacity exceeded, setId={setId}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: token,
                errorMessage: "CapacityExceeded"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 创建 LootItemEntry 并添加到 entity.Items
        var newEntry = new LootItemEntry(targetPosition, itemSnapshot);
        entity.Items.Add(newEntry);

        // 更新数据库
        entity.LastModified = DateTime.Now;
        _hostDatabase.UpdateLootBox(entity.SetId);

        LoggerHelper.Log($"[LOOT][Host] Operation: PUT_SUCCESS, SetId: {setId}, Peer: {peerEndPoint}, Details: Position={targetPosition}, ItemType={itemSnapshot.typeId}");

        // 5.3 实现响应和广播

        // 发送成功响应给请求者
        var successResponse = new Net.LootOperationResponse(
            success: true,
            token: token,
            errorMessage: null
        );
        SendJsonToPeer(peer, successResponse);

        // 创建并广播物品添加消息
        var broadcastMessage = new Net.LootItemAdded(
            setId: entity.SetId,
            position: targetPosition,
            itemSnapshot: itemSnapshot
        );
        BroadcastJsonToAll(broadcastMessage);

        LoggerHelper.Log($"[LOOT][Host] Operation: BROADCAST_ITEM_ADDED, SetId: {setId}, Peer: ALL, Details: Position={targetPosition}, ItemType={itemSnapshot.typeId}");
        
        // 更新统计：操作次数和延迟
        _operationStopwatch.Stop();
        _operationCount++;
        _totalLatencyMs += _operationStopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// 验证物品快照是否有效
    /// </summary>
    private bool IsValidItemSnapshot(LootNet.ItemSnapshot snapshot)
    {
        // 验证 typeId 是否存在（非零）
        if (snapshot.typeId <= 0)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] Invalid snapshot: typeId={snapshot.typeId}");
            return false;
        }

        // 验证堆叠数量是否合法（> 0）
        if (snapshot.stack <= 0)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] Invalid snapshot: stack={snapshot.stack}");
            return false;
        }

        // 注意：MaxStack 的验证需要物品类型信息，这里简化处理
        // 如果需要严格验证，可以尝试实例化物品并检查 MaxStackCount
        // 但为了性能考虑，我们只做基本验证

        return true;
    }

    /// <summary>
    /// 查找可用的位置：优先使用 preferredPosition，否则查找第一个空位
    /// </summary>
    private int FindAvailablePosition(LootBoxEntity entity, int preferredPosition)
    {
        if (entity == null)
            return -1;

        // 检查 preferredPosition 是否可用
        if (preferredPosition >= 0 && preferredPosition < entity.Capacity)
        {
            bool occupied = entity.Items.Any(item => item.Position == preferredPosition);
            if (!occupied)
                return preferredPosition;
        }

        // 查找第一个空位
        for (int i = 0; i < entity.Capacity; i++)
        {
            bool occupied = entity.Items.Any(item => item.Position == i);
            if (!occupied)
                return i;
        }

        // 没有空位
        return -1;
    }

    /// <summary>
    /// 广播 JSON 消息给所有客户端
    /// </summary>
    private void BroadcastJsonToAll(object data)
    {
        if (data == null)
        {
            LoggerHelper.LogWarning("[LootBoxSync] BroadcastJsonToAll: data is null");
            return;
        }

        try
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.None);

            var service = NetService.Instance;
            if (service == null || service.writer == null || service.netManager == null)
            {
                LoggerHelper.LogError("[LootBoxSync] NetService or writer or netManager is null");
                return;
            }

            service.writer.Reset();
            service.writer.Put((byte)Op.JSON);
            service.writer.Put(json);
            
            // 广播给所有连接的客户端
            service.netManager.SendToAll(service.writer, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
        catch (Exception ex)
        {
            LoggerHelper.LogError($"[LootBoxSync] Failed to broadcast JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// 主机：处理取出物品请求
    /// 1. 验证请求
    /// 2. 更新数据库（移除物品）
    /// 3. 广播增量更新
    /// </summary>
    public void Host_HandleTakeRequest(LiteNetLib.NetPeer peer, Net.LootTakeRequest request)
    {
        if (!IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandleTakeRequest called on client, ignoring");
            return;
        }

        if (peer == null)
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandleTakeRequest: peer is null");
            return;
        }

        if (request == null || string.IsNullOrEmpty(request.setId))
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandleTakeRequest: invalid request");
            return;
        }

        var setId = request.setId;
        var position = request.position;
        var token = request.token;
        var peerEndPoint = peer.EndPoint?.ToString() ?? "unknown";

        LoggerHelper.Log($"[LOOT][Host] Operation: TAKE, SetId: {setId}, Peer: {peerEndPoint}, Details: Token={token}, Position={position}");

        // 开始计时
        _operationStopwatch.Restart();

        // 6.1 验证请求

        // 从数据库查询战利品箱
        var entity = _hostDatabase?.GetLootBox(setId);
        
        // 更新统计：查询次数
        _queryCount++;

        // 验证战利品箱是否存在
        if (entity == null)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] TAKE failed: LootBox not found, setId={setId}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: token,
                errorMessage: "LootBoxNotFound"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 验证位置是否有效
        if (position < 0 || position >= entity.Capacity)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] TAKE failed: Invalid position, setId={setId}, position={position}, capacity={entity.Capacity}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: token,
                errorMessage: "InvalidPosition"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 查找该位置的物品
        var item = entity.Items.Find(e => e.Position == position);
        if (item == null)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] TAKE failed: Item not found at position, setId={setId}, position={position}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: token,
                errorMessage: "ItemNotFound"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 6.2 实现物品移除

        // 从 entity.Items 列表中移除该物品
        entity.Items.Remove(item);

        // 更新数据库
        entity.LastModified = DateTime.Now;
        _hostDatabase.UpdateLootBox(entity.SetId);

        LoggerHelper.Log($"[LOOT][Host] Operation: TAKE_SUCCESS, SetId: {setId}, Peer: {peerEndPoint}, Details: Position={position}, ItemType={item.Snapshot.typeId}");

        // 6.3 实现响应和广播

        // 发送成功响应给请求者（包含物品快照）
        var successResponse = new Net.LootOperationResponse(
            success: true,
            token: token,
            errorMessage: null,
            resultItem: item.Snapshot
        );
        SendJsonToPeer(peer, successResponse);

        // 创建并广播物品移除消息
        var broadcastMessage = new Net.LootItemRemoved(
            setId: entity.SetId,
            position: position
        );
        BroadcastJsonToAll(broadcastMessage);

        LoggerHelper.Log($"[LOOT][Host] Operation: BROADCAST_ITEM_REMOVED, SetId: {setId}, Peer: ALL, Details: Position={position}, ItemType={item.Snapshot.typeId}");
        
        // 更新统计：操作次数和延迟
        _operationStopwatch.Stop();
        _operationCount++;
        _totalLatencyMs += _operationStopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// 主机：处理拆分物品请求
    /// 1. 验证请求
    /// 2. 更新数据库（修改源物品数量，添加新物品）
    /// 3. 广播增量更新
    /// </summary>
    public void Host_HandleSplitRequest(LiteNetLib.NetPeer peer, Net.LootSplitRequest request)
    {
        if (!IsHost)
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandleSplitRequest called on client, ignoring");
            return;
        }

        if (peer == null)
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandleSplitRequest: peer is null");
            return;
        }

        if (request == null || string.IsNullOrEmpty(request.setId))
        {
            LoggerHelper.LogWarning("[LootBoxSync] Host_HandleSplitRequest: invalid request");
            return;
        }

        var setId = request.setId;
        var sourcePosition = request.sourcePosition;
        var count = request.count;
        var preferredPosition = request.preferredPosition;
        var peerEndPoint = peer.EndPoint?.ToString() ?? "unknown";

        LoggerHelper.Log($"[LOOT][Host] Operation: SPLIT, SetId: {setId}, Peer: {peerEndPoint}, Details: SourcePos={sourcePosition}, Count={count}, PreferredPos={preferredPosition}");

        // 开始计时
        _operationStopwatch.Restart();

        // 7.1 验证请求

        // 从数据库查询战利品箱
        var entity = _hostDatabase?.GetLootBox(setId);
        
        // 更新统计：查询次数
        _queryCount++;

        // 验证战利品箱是否存在
        if (entity == null)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] SPLIT failed: LootBox not found, setId={setId}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: 0,
                errorMessage: "LootBoxNotFound"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 查找源位置的物品
        var sourceItem = entity.Items.Find(e => e.Position == sourcePosition);
        if (sourceItem == null)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] SPLIT failed: Item not found at source position, setId={setId}, sourcePos={sourcePosition}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: 0,
                errorMessage: "ItemNotFound"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 验证物品是否可堆叠
        // 注意：ItemSnapshot 没有 Stackable 字段，我们通过 stack > 1 来判断
        if (sourceItem.Snapshot.stack <= 1)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] SPLIT failed: Item is not stackable, setId={setId}, sourcePos={sourcePosition}, stack={sourceItem.Snapshot.stack}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: 0,
                errorMessage: "ValidationFailed"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 验证拆分数量是否合法：Count > 0 && Count < StackCount
        if (count <= 0 || count >= sourceItem.Snapshot.stack)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] SPLIT failed: Invalid count, setId={setId}, sourcePos={sourcePosition}, count={count}, currentStack={sourceItem.Snapshot.stack}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: 0,
                errorMessage: "ValidationFailed"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 7.2 实现拆分逻辑

        // 获取源物品的当前快照
        var originalSnapshot = sourceItem.Snapshot;

        // 创建修改后的源物品快照（减少堆叠数量）
        var modifiedSourceSnapshot = new LootNet.ItemSnapshot
        {
            typeId = originalSnapshot.typeId,
            stack = originalSnapshot.stack - count,
            durability = originalSnapshot.durability,
            durabilityLoss = originalSnapshot.durabilityLoss,
            inspected = originalSnapshot.inspected,
            slots = originalSnapshot.slots != null 
                ? new List<(string key, LootNet.ItemSnapshot child)>(originalSnapshot.slots) 
                : null,
            inventory = originalSnapshot.inventory != null 
                ? new List<LootNet.ItemSnapshot>(originalSnapshot.inventory) 
                : null
        };

        // 创建新物品快照：复制源物品快照，设置 Stack = count
        var newSnapshot = new LootNet.ItemSnapshot
        {
            typeId = originalSnapshot.typeId,
            stack = count,
            durability = originalSnapshot.durability,
            durabilityLoss = originalSnapshot.durabilityLoss,
            inspected = originalSnapshot.inspected,
            slots = originalSnapshot.slots != null 
                ? new List<(string key, LootNet.ItemSnapshot child)>(originalSnapshot.slots) 
                : null,
            inventory = originalSnapshot.inventory != null 
                ? new List<LootNet.ItemSnapshot>(originalSnapshot.inventory) 
                : null
        };

        // 查找目标位置：优先使用 PreferredPosition，否则查找第一个空位
        int targetPosition = FindAvailablePosition(entity, preferredPosition);

        // 如果没有空位，发送容量超限错误
        if (targetPosition < 0)
        {
            LoggerHelper.LogWarning($"[LootBoxSync] [Host] SPLIT failed: Capacity exceeded, setId={setId}, peer={peerEndPoint}");
            
            var errorResponse = new Net.LootOperationResponse(
                success: false,
                token: 0,
                errorMessage: "CapacityExceeded"
            );
            SendJsonToPeer(peer, errorResponse);
            return;
        }

        // 更新源物品的快照（减少堆叠数量）
        sourceItem.Snapshot = modifiedSourceSnapshot;

        // 创建新的 LootItemEntry 并添加到 entity.Items
        var newEntry = new LootItemEntry(targetPosition, newSnapshot);
        entity.Items.Add(newEntry);

        // 更新数据库
        entity.LastModified = DateTime.Now;
        _hostDatabase.UpdateLootBox(entity.SetId);

        LoggerHelper.Log($"[LOOT][Host] Operation: SPLIT_SUCCESS, SetId: {setId}, Peer: {peerEndPoint}, Details: SourcePos={sourcePosition}, Count={count}, TargetPos={targetPosition}, ItemType={originalSnapshot.typeId}");

        // 7.3 实现广播

        // 创建 LootItemModified 消息（源物品）
        var modifiedMessage = new Net.LootItemModified(
            setId: entity.SetId,
            position: sourcePosition,
            itemSnapshot: modifiedSourceSnapshot
        );
        BroadcastJsonToAll(modifiedMessage);

        // 创建 LootItemAdded 消息（新物品）
        var addedMessage = new Net.LootItemAdded(
            setId: entity.SetId,
            position: targetPosition,
            itemSnapshot: newSnapshot
        );
        BroadcastJsonToAll(addedMessage);

        LoggerHelper.Log($"[LOOT][Host] Operation: BROADCAST_ITEM_MODIFIED, SetId: {setId}, Peer: ALL, Details: SourcePos={sourcePosition}, NewStack={modifiedSourceSnapshot.stack}");
        LoggerHelper.Log($"[LOOT][Host] Operation: BROADCAST_ITEM_ADDED, SetId: {setId}, Peer: ALL, Details: TargetPos={targetPosition}, Count={count}, ItemType={newSnapshot.typeId}");
        
        // 更新统计：操作次数和延迟
        _operationStopwatch.Stop();
        _operationCount++;
        _totalLatencyMs += _operationStopwatch.ElapsedMilliseconds;
    }

    #endregion

    #region 调试和导出

    /// <summary>
    /// 导出数据库为 JSON（调试用）
    /// </summary>
    public string ExportDatabaseToJson(bool indented = true)
    {
        if (_hostDatabase == null)
        {
            return "{ \"error\": \"Database not initialized\" }";
        }

        return _hostDatabase.ExportToJsonWithStats(indented);
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    /// <returns>包含统计信息的字典</returns>
    public Dictionary<string, object> GetStatistics()
    {
        var stats = new Dictionary<string, object>
        {
            ["QueryCount"] = _queryCount,
            ["OperationCount"] = _operationCount,
            ["TotalLatencyMs"] = _totalLatencyMs,
            ["AverageLatencyMs"] = _operationCount > 0 ? (double)_totalLatencyMs / _operationCount : 0.0,
            ["DatabaseSize"] = _hostDatabase?.Count ?? 0,
            ["CurrentLootBoxSetId"] = _currentLootBox?.setId ?? "null",
            ["IsHost"] = IsHost
        };

        return stats;
    }

    #endregion
}
