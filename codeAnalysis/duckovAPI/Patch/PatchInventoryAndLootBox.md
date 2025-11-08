# Patch/InventoryAndLootBox 模块 API 文档

## 模块概述

InventoryAndLootBox 补丁模块拦截背包和战利品箱操作，实现物品拾取、战利品箱状态的网络同步，防止重复拾取和状态不一致。

## 补丁目标

- `InteractableLootbox` - 可交互战利品箱

- `Inventory` - 背包系统

- `LootBoxLoader` - 战利品箱加载器

- `LootSpawner` - 战利品生成器

- `LootView` - 战利品视图

## 文件列表

| 文件名 | 补丁目标 | 说明 |

|--------|---------|------|
|---|---|---|

| `InventoryPatch.cs` | Inventory | 背包补丁 |
|---|---|---|

| `LootSpawner.cs` | LootSpawner | 生成器补丁 |
|---|---|---|

## 核心补丁说明

### InteractableLootboxPatch

**用途**：

- 同步战利品箱打开/关闭状态

- 防止重复拾取

- 同步战利品箱生成

### InventoryPatch

**用途**：

- 同步背包操作（可能仅本地）

- 物品添加/移除/移动

### LootBoxLoaderPatch

**用途**：

- 确保战利品一致性

- 同步战利品箱加载

### LootSpawner

**用途**：

- 同步战利品生成种子

- 确保所有客户端生成相同战利品

### LootViewPatch

**用途**：

- 显示远程玩家的战利品操作

- 同步战利品 UI 状态

## 关键流程

### 战利品箱打开流程

```

客户端 → 发送打开请求 → 服务器
服务器 → 验证并广播 → 所有客户端
客户端 → 显示战利品内容

```

### 物品拾取流程

```

客户端 → 发送拾取请求 → 服务器
服务器 → 验证并处理 → 广播物品消失
客户端 → 移除物品显示

```
