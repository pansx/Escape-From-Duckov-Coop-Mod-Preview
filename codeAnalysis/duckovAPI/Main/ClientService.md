# Main/ClientService 模块 API 文档

## 模块概述

ClientService 模块负责处理客户端状态更新和应用，包括远程角色的创建、装备更新、武器更新等。该模块实现了去抖和幂等性控制，确保状态更新的准确性和效率。

**核心职责**：

- 处理客户端状态更新

- 管理远程角色创建

- 应用装备和武器更新

- 实现去抖和幂等性控制

## 文件列表

| 文件名 | 说明 |

|--------|------|
|---|---|

| `ClientPlayerApply.cs` | 客户端玩家应用，处理装备和武器更新 |
|---|---|

## 核心类说明

### ClientHandle

**功能**：处理客户端状态更新，管理远程角色创建

**主要方法**：

- `HandleClientStatusUpdate` - 处理客户端状态更新

- 创建和管理远程角色 GameObject

- 应用装备和武器更新

### ClientPlayerApply

**功能**：应用装备和武器更新到远程角色

**关键机制**：

- **去抖窗口**：200ms 防止重复应用

- **幂等性**：通过字典记录已应用状态

- **异步处理**：使用 UniTask 进行异步操作

**主要方法**：

- `ApplyEquipmentUpdate_Client` - 应用装备更新

- `ApplyWeaponUpdate_Client` - 应用武器更新

### Send_ClientStatus

**功能**：发送客户端状态更新到服务器

**主要方法**：

- `SendClientStatusUpdate` - 发送状态更新

- 管理本地玩家状态

## 依赖关系

**依赖模块**：

- NetService - 网络通信

- COOPManager - 资源管理

**被依赖模块**：

- Main/NetService - 调用客户端处理器

## 性能优化

1. **去抖控制**：200ms 窗口防止重复应用
2. **幂等性**：避免重复处理相同状态
3. **异步处理**：使用 UniTask 避免阻塞
