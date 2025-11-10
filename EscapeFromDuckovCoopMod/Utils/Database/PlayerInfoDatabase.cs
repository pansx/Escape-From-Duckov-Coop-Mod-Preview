using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromDuckovCoopMod.Utils.Database;

/// <summary>
/// 玩家信息实体
/// </summary>
public class PlayerInfoEntity
{
    // Steam 信息
    public string SteamId { get; set; }
    public string PlayerName { get; set; }  // Steam 名字 = 玩家名字
    public string SteamAvatarUrl { get; set; }
    public Texture2D AvatarTexture { get; set; }
    
    // 网络信息（来自 ClientStatusData）
    public string EndPoint { get; set; }
    public string LastUpdate { get; set; }  // 最后更新时间戳
    
    // 元数据
    public DateTime LastSeen { get; set; }
    public bool IsLocalPlayer { get; set; }
    public Dictionary<string, object> CustomData { get; set; } = new();

    public PlayerInfoEntity(string steamId, string playerName, string avatarUrl = null)
    {
        SteamId = steamId;
        PlayerName = playerName;
        SteamAvatarUrl = avatarUrl;
        LastSeen = DateTime.Now;
        IsLocalPlayer = false;
    }
}

/// <summary>
/// 玩家信息数据库
/// </summary>
public class PlayerInfoDatabase
{
    private readonly InMemoryDatabase<PlayerInfoEntity> _db;
    private static PlayerInfoDatabase _instance;

    public static PlayerInfoDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = new PlayerInfoDatabase();
            return _instance;
        }
    }

    public int Count => _db.Count;

    private PlayerInfoDatabase()
    {
        _db = new InMemoryDatabase<PlayerInfoEntity>()
            .WithPrimaryKey(e => e.SteamId)
            .WithIndex("PlayerName", e => e.PlayerName)
            .WithIndex("EndPoint", e => e.EndPoint)
            .WithIndex("IsLocalPlayer", e => e.IsLocalPlayer);
    }

    #region 基础操作

    /// <summary>
    /// 添加或更新玩家信息
    /// </summary>
    public bool AddOrUpdatePlayer(
        string steamId,
        string playerName,
        string avatarUrl = null,
        bool isLocal = false,
        string endPoint = null,
        string lastUpdate = null
    )
    {
        if (string.IsNullOrEmpty(steamId))
            return false;

        // 检查是否已存在
        var existing = _db.FindByKey(steamId);
        if (existing != null)
        {
            // 更新现有信息
            existing.PlayerName = playerName ?? existing.PlayerName;
            existing.SteamAvatarUrl = avatarUrl ?? existing.SteamAvatarUrl;
            existing.EndPoint = endPoint ?? existing.EndPoint;
            existing.LastUpdate = lastUpdate ?? existing.LastUpdate;
            existing.LastSeen = DateTime.Now;
            existing.IsLocalPlayer = isLocal;
            return _db.Update(existing);
        }

        // 添加新玩家
        var entity = new PlayerInfoEntity(steamId, playerName, avatarUrl)
        {
            IsLocalPlayer = isLocal,
            EndPoint = endPoint,
            LastUpdate = lastUpdate
        };

        return _db.Insert(entity);
    }

    /// <summary>
    /// 删除玩家信息
    /// </summary>
    public bool RemovePlayer(string steamId)
    {
        return _db.Delete(steamId);
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
    /// 按 SteamId 查询
    /// </summary>
    public PlayerInfoEntity GetPlayerBySteamId(string steamId)
    {
        return _db.FindByKey(steamId);
    }

    /// <summary>
    /// 按玩家名字查询
    /// </summary>
    public IEnumerable<PlayerInfoEntity> GetPlayersByName(string playerName)
    {
        return _db.FindByIndex("PlayerName", playerName);
    }

    /// <summary>
    /// 按 EndPoint 查询
    /// </summary>
    public PlayerInfoEntity GetPlayerByEndPoint(string endPoint)
    {
        foreach (var player in _db.FindByIndex("EndPoint", endPoint))
        {
            return player; // 只应该有一个
        }
        return null;
    }

    /// <summary>
    /// 获取本地玩家
    /// </summary>
    public PlayerInfoEntity GetLocalPlayer()
    {
        foreach (var player in _db.FindByIndex("IsLocalPlayer", true))
        {
            return player; // 只应该有一个本地玩家
        }
        return null;
    }

    /// <summary>
    /// 获取所有玩家
    /// </summary>
    public IEnumerable<PlayerInfoEntity> GetAllPlayers()
    {
        return _db.GetAll();
    }

    /// <summary>
    /// 条件查询
    /// </summary>
    public IEnumerable<PlayerInfoEntity> FindPlayers(Func<PlayerInfoEntity, bool> predicate)
    {
        return _db.Where(predicate);
    }

    #endregion

    #region 头像管理

    /// <summary>
    /// 设置玩家头像纹理
    /// </summary>
    public bool SetPlayerAvatar(string steamId, Texture2D avatarTexture)
    {
        var player = _db.FindByKey(steamId);
        if (player == null)
            return false;

        player.AvatarTexture = avatarTexture;
        return true;
    }

    /// <summary>
    /// 获取玩家头像纹理
    /// </summary>
    public Texture2D GetPlayerAvatar(string steamId)
    {
        var player = _db.FindByKey(steamId);
        return player?.AvatarTexture;
    }

    #endregion

    #region 自定义数据

    /// <summary>
    /// 设置自定义数据
    /// </summary>
    public bool SetCustomData(string steamId, string key, object value)
    {
        var player = _db.FindByKey(steamId);
        if (player == null)
            return false;

        player.CustomData[key] = value;
        return true;
    }

    /// <summary>
    /// 获取自定义数据
    /// </summary>
    public object GetCustomData(string steamId, string key)
    {
        var player = _db.FindByKey(steamId);
        return player?.CustomData.GetValueOrDefault(key);
    }

    #endregion

    #region JSON 导出

    /// <summary>
    /// 导出为 JSON
    /// </summary>
    public string ExportToJson(bool indented = true)
    {
        var players = new List<object>();

        foreach (var player in _db.GetAll())
        {
            players.Add(new
            {
                player.SteamId,
                player.PlayerName,
                player.SteamAvatarUrl,
                player.EndPoint,
                player.LastUpdate,
                HasAvatar = player.AvatarTexture != null,
                player.IsLocalPlayer,
                LastSeen = player.LastSeen.ToString("yyyy-MM-dd HH:mm:ss"),
                CustomData = player.CustomData
            });
        }

        var formatting = indented
            ? Newtonsoft.Json.Formatting.Indented
            : Newtonsoft.Json.Formatting.None;

        return Newtonsoft.Json.JsonConvert.SerializeObject(players, formatting);
    }

    /// <summary>
    /// 导出带统计信息的 JSON
    /// </summary>
    public string ExportToJsonWithStats(bool indented = true)
    {
        var players = new List<object>();

        foreach (var player in _db.GetAll())
        {
            players.Add(new
            {
                player.SteamId,
                player.PlayerName,
                player.SteamAvatarUrl,
                player.EndPoint,
                player.LastUpdate,
                HasAvatar = player.AvatarTexture != null,
                player.IsLocalPlayer,
                LastSeen = player.LastSeen.ToString("yyyy-MM-dd HH:mm:ss"),
                CustomData = player.CustomData
            });
        }

        var data = new
        {
            ExportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TotalPlayers = _db.Count,
            LocalPlayer = GetLocalPlayer()?.PlayerName ?? "Unknown",
            Players = players
        };

        var formatting = indented
            ? Newtonsoft.Json.Formatting.Indented
            : Newtonsoft.Json.Formatting.None;

        return Newtonsoft.Json.JsonConvert.SerializeObject(data, formatting);
    }

    #endregion
}
