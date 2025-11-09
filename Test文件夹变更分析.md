# Test 文件夹变更分析

## 📋 概述

Test 文件夹包含了一个新的**音频同步功能**，用于在多人游戏中同步音效事件。检测到很多冲突,可能导致合并后游戏出错

## 🆕 新增文件

### 1. 音频同步核心
- `Test/EscapeFromDuckovCoopMod/Main/Audio/CoopAudioSync.cs` - 音频同步管理器
- `Test/EscapeFromDuckovCoopMod/Main/Audio/CoopAudioEmitter.cs` - 音频发射器
- `Test/EscapeFromDuckovCoopMod/Main/Audio/CoopAudioEventPayload.cs` - 音频事件数据结构

### 2. 网络消息
- `Test/EscapeFromDuckovCoopMod/Net/AudioEventMessage.cs` - 音频事件网络消息

## 🔄 修改文件

### 1. Op.cs
**新增操作码**:
```csharp
AUDIO_EVENT = 204, // 主机 -> 客户端：同步音效事件
```

### 2. Mod.cs
**新增消息处理**:
```csharp
case Op.AUDIO_EVENT:
{
    var payload = CoopAudioEventPayload.Read(reader);
    
    if (IsServer)
    {
        AudioEventMessage.ServerBroadcastExcept(payload, peer);
        CoopAudioSync.HandleIncoming(payload);
    }
    else
    {
        CoopAudioSync.HandleIncoming(payload);
    }
    break;
}
```

### 3. OpPriority.cs
**新增优先级配置**:
```csharp
Op.AUDIO_EVENT => PacketPriority.Normal,
```

## 🎯 功能特性

### 音频同步机制

1. **2D 音频** - 无位置信息的全局音效
2. **3D 音频** - 带位置信息的空间音效

### UI 音效过滤

自动过滤 UI 相关音效，避免同步不必要的界面音效：
- inventory, stash, ragfair, trader
- menu, loading, matching
- dialog, quest, profile
- 等等...

### 抑制机制

使用 `IDisposable` 模式防止音频事件的无限循环：
```csharp
using (BeginSuppress())
{
    // 播放音频不会触发新的同步事件
}
```

## ⚠️ 潜在冲突

### 1. Op.cs
- **当前代码**: 没有 `AUDIO_EVENT = 204`
- **Test 代码**: 添加了 `AUDIO_EVENT = 204`
- **冲突风险**: 低（纯新增）

### 2. Mod.cs
- **当前代码**: 没有 `AUDIO_EVENT` 的 case 处理
- **Test 代码**: 添加了完整的音频事件处理
- **冲突风险**: 低（需要在 switch 中添加新 case）

### 3. OpPriority.cs
- **当前代码**: 可能没有 `AUDIO_EVENT` 的优先级配置
- **Test 代码**: 添加了 `Op.AUDIO_EVENT => PacketPriority.Normal`
- **冲突风险**: 低（纯新增）

## 📝 合并建议

### 步骤 1: 复制新文件
```
Test/EscapeFromDuckovCoopMod/Main/Audio/*.cs
  -> EscapeFromDuckovCoopMod/Main/Audio/

Test/EscapeFromDuckovCoopMod/Net/AudioEventMessage.cs
  -> EscapeFromDuckovCoopMod/Net/
```

### 步骤 2: 更新 Op.cs
在 `PLAYER_APPEARANCE = 26` 后添加：
```csharp
AUDIO_EVENT = 204, // 主机 -> 客户端：同步音效事件
```

### 步骤 3: 更新 Mod.cs
在消息处理 switch 中添加 `AUDIO_EVENT` case

### 步骤 4: 更新 OpPriority.cs
添加音频事件的优先级配置

### 步骤 5: 添加 Patch
可能需要添加 Harmony Patch 来拦截游戏的音频播放调用

## 🔍 需要检查的文件

1. ✅ `Op.cs` - 操作码定义
2. ✅ `Mod.cs` - 消息处理
3. ✅ `OpPriority.cs` - 优先级配置
4. ❓ `Patch/` - 可能需要音频相关的 Patch
5. ❓ `.csproj` - 可能需要添加新文件引用

## 🎮 使用场景

- 枪声同步
- 脚步声同步
- 环境音效同步
- 技能音效同步
- 物品交互音效同步

## ⚡ 性能考虑

- 使用 `PacketPriority.Normal` 避免阻塞关键消息
- UI 音效自动过滤减少网络流量
- 抑制机制防止音频事件循环

---

*分析时间: 2025-11-09*
