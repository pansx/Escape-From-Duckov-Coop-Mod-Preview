# 场景名称中文化

## 概述

在所有UI界面显示中文场景名称，提升中文用户体验。

## 主要更改

### 1. 新增 `SceneNameMapper` 工具类
- 📍 位置：`EscapeFromDuckovCoopMod/Utils/SceneNameMapper.cs`
- 提供场景ID到中文名称的映射
- 支持15个游戏场景的中文翻译
- 智能判断并优先使用游戏内置翻译

### 2. 更新 `MModUI` 投票面板
- 📍 位置：`EscapeFromDuckovCoopMod/Main/UI/MModUI.cs`
- 场景投票时显示中文场景名

### 3. 更新 `ModUI` 投票窗口
- 📍 位置：`EscapeFromDuckovCoopMod/Main/UI/ModUI.cs`
- 传统UI投票窗口显示中文场景名

### 4. 更新 `WaitingSynchronizationUI` 加载界面
- 📍 位置：`EscapeFromDuckovCoopMod/Main/UI/WaitingSynchronizationUI.cs`
- 场景加载时显示中文地图名
- 支持从Unity场景名称（如 `Base_Scenev2`, `Level_Factory_Main`）提取场景ID

## 场景映射表

| 场景ID | 中文名称 |
|--------|---------|
| Base | 基地 |
| Custom | 海关 |
| Custom_01 ~ Custom_05 | 海关1 ~ 海关5 |
| Factory | 工厂 |
| Factory_01 ~ Factory_04 | 工厂1 ~ 工厂4 |
| Village | 村庄 |
| Village_01 | 村庄1 |
| Any | 任意地图 |

## 效果对比

### 场景加载界面
- ❌ 旧版：`正在加载场景...地图: Base_Scenev2`
- ✅ 新版：`正在加载场景...地图: 基地`

### 场景投票界面
- ❌ 旧版：显示 `Factory`、`Custom_01` 等英文ID
- ✅ 新版：显示 `工厂`、`海关1` 等中文名称

## 技术实现

### 智能名称选择逻辑
`SceneNameMapper.GetDisplayName()` 方法实现了智能选择：
1. 首先尝试从游戏的 `SceneInfoCollection` 获取场景信息
2. 如果获取到的 `DisplayName` 是英文或与场景ID相同，则使用我们的中文映射
3. 如果游戏已经提供了中文名称，则直接使用游戏的名称
4. 如果都没有，返回场景ID作为后备

### Unity场景名称提取
`WaitingSynchronizationUI.ExtractSceneId()` 方法支持：
- `Base_Scenev2` → `Base`
- `Level_Factory_Main` → `Factory`
- `Level_Custom_Main` → `Custom`

## 数据来源

场景名称映射来自游戏官方本地化文件：
```
C:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\StreamingAssets\Localization\ChineseSimplified.csv
```

## 测试

- ✅ 编译通过，无错误
- ✅ 代码格式检查通过
- ✅ 已在游戏中测试验证

## 兼容性

- ✅ 不修改游戏原始文件
- ✅ 向后兼容：如果场景ID不在映射表中，会返回原始ID
- ✅ 不影响其他功能
- ✅ 易于维护和扩展

## 后续扩展

如果游戏添加了新的场景，只需在 `SceneNameMapper.cs` 的 `SceneNames` 字典中添加新的映射即可。
