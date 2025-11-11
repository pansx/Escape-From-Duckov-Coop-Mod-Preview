using System;
using System.Collections.Generic;
using UnityEngine;
using ItemStatsSystem;

namespace EscapeFromDuckovCoopMod.Utils.Database;

/// <summary>
/// 战利品箱中的物品条目
/// </summary>
public class LootItemEntry
{
    public int Position { get; set; }
    public LootNet.ItemSnapshot Snapshot { get; set; }

    public LootItemEntry(int position, LootNet.ItemSnapshot snapshot)
    {
        Position = position;
        Snapshot = snapshot;
    }
}

/// <summary>
/// 战利品箱实体 - 存储在数据库中的数据结构
/// </summary>
public class LootBoxEntity
{
    // 唯一标识
    public string SetId { get; set; }
    
    // 游戏对象引用
    public GameObject GameObject { get; set; }
    public Inventory Inventory { get; set; }
    
    // 位置信息
    public Vector3 Position { get; set; }
    public string SceneName { get; set; }
    
    // 容器信息
    public int Capacity { get; set; }
    public List<LootItemEntry> Items { get; set; }
    
    // 元数据
    public int OwnerId { get; set; }  // -1 表示公共容器
    public DateTime LastModified { get; set; }
    
    // 自定义数据
    public Dictionary<string, object> CustomData { get; set; }

    public LootBoxEntity(GameObject go, Inventory inv, string setId)
    {
        GameObject = go;
        Inventory = inv;
        SetId = setId;
        Position = go != null ? go.transform.position : Vector3.zero;
        SceneName = go != null ? go.scene.name : "";
        Capacity = inv != null ? inv.Capacity : 0;
        Items = new List<LootItemEntry>();
        OwnerId = -1;  // 默认为公共容器
        LastModified = DateTime.Now;
        CustomData = new Dictionary<string, object>();
    }
}
