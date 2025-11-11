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
using EscapeFromDuckovCoopMod.Utils.Database;
using static EscapeFromDuckovCoopMod.LootNet;

namespace EscapeFromDuckovCoopMod.Net;

#region 请求消息类

/// <summary>
/// 打开战利品箱请求
/// </summary>
[Serializable]
public class LootOpenRequest
{
    public string type = "lootOpenRequest";
    public string setId;
    public int requestVersion;

    public LootOpenRequest()
    {
    }

    public LootOpenRequest(string setId, int requestVersion = 1)
    {
        this.setId = setId;
        this.requestVersion = requestVersion;
    }
}

/// <summary>
/// 放入物品请求
/// </summary>
[Serializable]
public class LootPutRequest
{
    public string type = "lootPutRequest";
    public string setId;
    public int preferredPosition;
    public uint token;
    public ItemSnapshot itemSnapshot;

    public LootPutRequest()
    {
    }

    public LootPutRequest(string setId, int preferredPosition, uint token, ItemSnapshot itemSnapshot)
    {
        this.setId = setId;
        this.preferredPosition = preferredPosition;
        this.token = token;
        this.itemSnapshot = itemSnapshot;
    }
}

/// <summary>
/// 取出物品请求
/// </summary>
[Serializable]
public class LootTakeRequest
{
    public string type = "lootTakeRequest";
    public string setId;
    public int position;
    public uint token;
    public string destType;
    public int destPosition;

    public LootTakeRequest()
    {
    }

    public LootTakeRequest(string setId, int position, uint token, string destType, int destPosition = -1)
    {
        this.setId = setId;
        this.position = position;
        this.token = token;
        this.destType = destType;
        this.destPosition = destPosition;
    }
}

/// <summary>
/// 拆分物品请求
/// </summary>
[Serializable]
public class LootSplitRequest
{
    public string type = "lootSplitRequest";
    public string setId;
    public int sourcePosition;
    public int count;
    public int preferredPosition;

    public LootSplitRequest()
    {
    }

    public LootSplitRequest(string setId, int sourcePosition, int count, int preferredPosition)
    {
        this.setId = setId;
        this.sourcePosition = sourcePosition;
        this.count = count;
        this.preferredPosition = preferredPosition;
    }
}

#endregion

#region 响应消息类

/// <summary>
/// 战利品箱状态响应
/// </summary>
[Serializable]
public class LootStateResponse
{
    public string type = "lootStateResponse";
    public string setId;
    public int capacity;
    public List<LootItemEntry> items;
    public string timestamp;

    public LootStateResponse()
    {
        items = new List<LootItemEntry>();
    }

    public LootStateResponse(string setId, int capacity, List<LootItemEntry> items)
    {
        this.setId = setId;
        this.capacity = capacity;
        this.items = items ?? new List<LootItemEntry>();
        this.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}

/// <summary>
/// 操作确认响应
/// </summary>
[Serializable]
public class LootOperationResponse
{
    public string type = "lootOperationResponse";
    public bool success;
    public uint token;
    public string errorMessage;
    public ItemSnapshot resultItem;

    public LootOperationResponse()
    {
    }

    public LootOperationResponse(bool success, uint token, string errorMessage = null)
    {
        this.success = success;
        this.token = token;
        this.errorMessage = errorMessage;
        this.resultItem = default;
    }
    
    public LootOperationResponse(bool success, uint token, string errorMessage, ItemSnapshot resultItem)
    {
        this.success = success;
        this.token = token;
        this.errorMessage = errorMessage;
        this.resultItem = resultItem;
    }
}

#endregion

#region 广播消息类（增量更新）

/// <summary>
/// 物品添加广播（增量更新）
/// </summary>
[Serializable]
public class LootItemAdded
{
    public string type = "lootItemAdded";
    public string setId;
    public int position;
    public ItemSnapshot itemSnapshot;

    public LootItemAdded()
    {
    }

    public LootItemAdded(string setId, int position, ItemSnapshot itemSnapshot)
    {
        this.setId = setId;
        this.position = position;
        this.itemSnapshot = itemSnapshot;
    }
}

/// <summary>
/// 物品移除广播（增量更新）
/// </summary>
[Serializable]
public class LootItemRemoved
{
    public string type = "lootItemRemoved";
    public string setId;
    public int position;

    public LootItemRemoved()
    {
    }

    public LootItemRemoved(string setId, int position)
    {
        this.setId = setId;
        this.position = position;
    }
}

/// <summary>
/// 物品修改广播（增量更新，如拆分后数量变化）
/// </summary>
[Serializable]
public class LootItemModified
{
    public string type = "lootItemModified";
    public string setId;
    public int position;
    public ItemSnapshot itemSnapshot;

    public LootItemModified()
    {
    }

    public LootItemModified(string setId, int position, ItemSnapshot itemSnapshot)
    {
        this.setId = setId;
        this.position = position;
        this.itemSnapshot = itemSnapshot;
    }
}

#endregion
