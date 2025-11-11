using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ItemStatsSystem;

namespace EscapeFromDuckovCoopMod.Utils.Database;

/// <summary>
/// 战利品箱数据库 - 高性能查询和管理
/// </summary>
public class LootBoxDatabase
{
    private readonly InMemoryDatabase<LootBoxEntity> _db;
    
    public int Count => _db.Count;

    public LootBoxDatabase(float spatialCellSize = 50f)
    {
        _db = new InMemoryDatabase<LootBoxEntity>()
            .WithPrimaryKey(e => e.SetId)
            .WithIndex("SceneName", e => e.SceneName)
            .WithIndex("OwnerId", e => e.OwnerId)
            .WithSpatialIndex(e => e.Position, spatialCellSize);
    }

    #region 基础操作

    /// <summary>
    /// 添加战利品箱到数据库
    /// </summary>
    public bool AddLootBox(GameObject go, Inventory inv, string setId)
    {
        if (go == null || inv == null || string.IsNullOrEmpty(setId))
            return false;

        var entity = new LootBoxEntity(go, inv, setId);
        return _db.Insert(entity);
    }

    /// <summary>
    /// 更新战利品箱状态
    /// </summary>
    public bool UpdateLootBox(string setId)
    {
        var entity = _db.FindByKey(setId);
        if (entity == null)
            return false;

        entity.LastModified = DateTime.Now;
        return _db.Update(entity);
    }

    /// <summary>
    /// 删除战利品箱
    /// </summary>
    public bool RemoveLootBox(string setId)
    {
        return _db.Delete(setId);
    }

    /// <summary>
    /// 清空数据库
    /// </summary>
    public void Clear()
    {
        _db.Clear();
    }

    #endregion

    #region 查询操作

    /// <summary>
    /// 按 SetId 查询（O(1)）
    /// </summary>
    public LootBoxEntity GetLootBox(string setId)
    {
        return _db.FindByKey(setId);
    }

    /// <summary>
    /// 按场景查询（O(1)）
    /// </summary>
    public IEnumerable<LootBoxEntity> GetLootBoxesByScene(string sceneName)
    {
        return _db.FindByIndex("SceneName", sceneName);
    }

    /// <summary>
    /// 空间范围查询（O(1)）
    /// </summary>
    public IEnumerable<LootBoxEntity> GetLootBoxesInRadius(Vector3 center, float radius)
    {
        return _db.FindInRadius(center, radius);
    }

    /// <summary>
    /// 获取所有公共战利品箱
    /// </summary>
    public IEnumerable<LootBoxEntity> GetPublicLootBoxes()
    {
        return _db.FindByIndex("OwnerId", -1);
    }

    /// <summary>
    /// 获取所有战利品箱
    /// </summary>
    public IEnumerable<LootBoxEntity> GetAllLootBoxes()
    {
        return _db.GetAll();
    }

    #endregion

    #region JSON 导出

    /// <summary>
    /// 导出整库为 JSON
    /// </summary>
    public string ExportToJson(bool indented = true)
    {
        var exportData = new List<object>();
        
        foreach (var entity in _db.GetAll())
        {
            exportData.Add(new
            {
                entity.SetId,
                entity.SceneName,
                entity.OwnerId,
                Position = new
                {
                    x = entity.Position.x,
                    y = entity.Position.y,
                    z = entity.Position.z
                },
                entity.Capacity,
                ItemCount = entity.Items.Count,
                GameObjectName = entity.GameObject?.name ?? "null",
                Active = entity.GameObject?.activeSelf ?? false,
                LastModified = entity.LastModified.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        var formatting = indented 
            ? Newtonsoft.Json.Formatting.Indented 
            : Newtonsoft.Json.Formatting.None;
        
        return Newtonsoft.Json.JsonConvert.SerializeObject(exportData, formatting);
    }

    /// <summary>
    /// 导出带统计信息的 JSON
    /// </summary>
    public string ExportToJsonWithStats(bool indented = true)
    {
        var items = new List<object>();
        int totalItems = 0;
        
        foreach (var entity in _db.GetAll())
        {
            totalItems += entity.Items.Count;
            items.Add(new
            {
                entity.SetId,
                entity.SceneName,
                entity.OwnerId,
                Position = new
                {
                    x = entity.Position.x,
                    y = entity.Position.y,
                    z = entity.Position.z
                },
                entity.Capacity,
                ItemCount = entity.Items.Count,
                GameObjectName = entity.GameObject?.name ?? "null",
                Active = entity.GameObject?.activeSelf ?? false,
                LastModified = entity.LastModified.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        var data = new
        {
            ExportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalLootBoxes = _db.Count,
            TotalItems = totalItems,
            LootBoxes = items
        };

        var formatting = indented 
            ? Newtonsoft.Json.Formatting.Indented 
            : Newtonsoft.Json.Formatting.None;
        
        return Newtonsoft.Json.JsonConvert.SerializeObject(data, formatting);
    }

    /// <summary>
    /// 按场景导出为 JSON
    /// </summary>
    public string ExportBySceneToJson(string sceneName, bool indented = true)
    {
        var items = new List<object>();
        
        foreach (var entity in _db.FindByIndex("SceneName", sceneName))
        {
            items.Add(new
            {
                entity.SetId,
                entity.SceneName,
                entity.OwnerId,
                Position = new
                {
                    x = entity.Position.x,
                    y = entity.Position.y,
                    z = entity.Position.z
                },
                entity.Capacity,
                ItemCount = entity.Items.Count,
                GameObjectName = entity.GameObject?.name ?? "null",
                Active = entity.GameObject?.activeSelf ?? false,
                LastModified = entity.LastModified.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        var formatting = indented 
            ? Newtonsoft.Json.Formatting.Indented 
            : Newtonsoft.Json.Formatting.None;
        
        return Newtonsoft.Json.JsonConvert.SerializeObject(items, formatting);
    }

    #endregion
}
