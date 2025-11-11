-   [x] 3. 分析最深层模块（深度 2）

    -   [x] 3.1 分析 Main 子模块

        -   读取并解析 Main/AI/目录下的所有 C#文件（5 个文件）
        -   读取并解析 Main/ClientService/目录下的所有 C#文件（3 个文件）
        -   读取并解析 Main/Health/目录下的所有 C#文件（4 个文件）
        -   读取并解析 Main/HostService/目录下的所有 C#文件（2 个文件）
        -   读取并解析 Main/Item/目录下的所有 C#文件（3 个文件）
        -   读取并解析 Main/Loader/目录下的所有 C#文件（2 个文件）
        -   读取并解析 Main/Localization/目录下的所有 C#文件（1 个文件）
        -   读取并解析 Main/LocalPlayer/目录下的所有 C#文件（3 个文件）
        -   读取并解析 Main/SceneService/目录下的所有 C#文件（8 个文件）
        -   读取并解析 Main/UI/目录下的所有 C#文件（4 个文件）
        -   读取并解析 Main/Weapon/目录下的所有 C#文件（5 个文件）
        -   读取并解析 Main/WeatherAndTime/目录下的所有 C#文件（1 个文件）
        -   _Requirements: 2.4, 3.1, 3.2, 3.3, 3.4_

    -   [x] 3.2 分析 Net 子模块

        -   读取并解析 Net/NetPack/目录下的所有 C#文件（3 个文件）
        -   读取并解析 Net/Steam/目录下的所有 C#文件（6 个文件）
        -   _Requirements: 2.4, 3.1, 3.2, 3.3, 3.4_

    -   [x] 3.3 分析 Patch 子模块

        -   读取并解析 Patch/Character/目录下的所有 C#文件（8 个文件）
        -   读取并解析 Patch/InventoryAndLootBox/目录下的所有 C#文件（5 个文件）
        -   读取并解析 Patch/Item/目录下的所有 C#文件（6 个文件）
        -   读取并解析 Patch/Projectile/目录下的所有 C#文件（1 个文件）
        -   读取并解析 Patch/Scene/目录下的所有 C#文件（3 个文件）
        -   读取并解析 Patch/SteamP2P/目录下的所有 C#文件（3 个文件）

        -   _Requirements: 2.4, 3.1, 3.2, 3.3, 3.4_

-   [x] 4. 为最深层模块生成 API 文档

    -   [x] 4.1 生成 Main 子模块文档

        -   为 Main/AI/生成 API 文档（AI.md）
        -   为 Main/ClientService/生成 API 文档（ClientService.md）
        -   为 Main/Health/生成 API 文档（Health.md）
        -   为 Main/HostService/生成 API 文档（HostService.md）
        -   为 Main/Item/生成 API 文档（Item.md）
        -   为 Main/Loader/生成 API 文档（Loader.md）
        -   为 Main/Localization/生成 API 文档（Localization.md）
        -   为 Main/LocalPlayer/生成 API 文档（LocalPlayer.md）
        -   为 Main/SceneService/生成 API 文档（SceneService.md）
        -   为 Main/UI/生成 API 文档（UI.md）
        -   为 Main/Weapon/生成 API 文档（Weapon.md）

        -   为 Main/WeatherAndTime/生成 API 文档（WeatherAndTime.md）
        -   每个文档包含：模块概述、文件列表、核心类说明、主要方法、依赖关系
        -   _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

    -   [x] 4.2 生成 Net 子模块文档

        -   为 Net/NetPack/生成 API 文档（NetPack.md）
        -   为 Net/Steam/生成 API 文档（Steam.md）
        -   每个文档包含：模块概述、文件列表、核心类说明、主要方法、依赖关系
        -   _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

    -   [x] 4.3 生成 Patch 子模块文档

        -   为 Patch/Character/生成 API 文档（PatchCharacter.md）
        -   为 Patch/InventoryAndLootBox/生成 API 文档（PatchInventoryAndLootBox.md）
        -   为 Patch/Item/生成 API 文档（PatchItem.md）
        -   为 Patch/Projectile/生成 API 文档（PatchProjectile.md）
        -   为 Patch/Scene/生成 API 文档（PatchScene.md）

        -   为 Patch/SteamP2P/生成 API 文档（PatchSteamP2P.md）
        -   每个文档包含：模块概述、补丁目标、核心补丁类、补丁方法说明
        -   _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

-   [x] 5. 分析中层模块（深度 1）并生成汇总文档

    -   [x] 5.1 分析和汇总 Main 模块

    -   [ ] 5.1 分析和汇总 Main 模块

        -   读取并解析 Main/目录下的根级 C#文件（8 个文件：COOPManager.cs, CoopTool.cs, CustomFace.cs, FxManager.cs, HarmonyFix.cs, NetService.cs, Op.cs, PublicHandleUpdate.cs）
        -   汇总所有 Main 子模块的功能

        -   生成 Main 模块总览文档（Main/README.md）
        -   描述 Main 模块的整体架构和子模块关系
        -   _Requirements: 2.4, 5.1, 5.2, 5.3, 5.4_

    -   [x] 5.2 分析和汇总 Net 模块

        -   读取并解析 Net/目录下的根级 C#文件（11 个文件：LocalHitKillFx.cs, NetAiFollower.cs, NetAiTag.cs, NetAiVisibilityGuard.cs, NetDataExtensions.cs, NetInterpolator.cs, NetPacketPool.cs, NetSilenceGuards.cs, NetworkExtensions.cs, OpPriority.cs, PacketPriority.cs）
        -   汇总 NetPack 和 Steam 子模块的功能
        -   生成 Net 模块总览文档（Net/README.md）

        -   描述网络层架构和通信机制
        -   _Requirements: 2.4, 5.1, 5.2, 5.3, 5.4_

    -   [x] 5.3 分析和汇总 Patch 模块

        -   汇总所有 Patch 子模块的功能

        -   生成 Patch 模块总览文档（Patch/README.md）
        -   描述补丁系统的整体设计和各补丁模块的作用
        -   _Requirements: 2.4, 5.1, 5.2, 5.3, 5.4_

    -   [x] 5.4 分析其他中层模块

        -   读取并解析 NetTag/目录下的 C#文件（3 个文件：NetDestructibleTag.cs, NetDropTag.cs, NetGrenadeTag.cs），生成 NetTag.md
        -   读取并解析 Properties/目录下的 C#文件（1 个文件：AssemblyInfo.cs），生成 Properties.md
        -   读取并解析 SyncData/目录下的 C#文件（1 个文件：SyncDataManger.cs），生成 SyncData.md
        -   读取并解析 Utils/目录下的 C#文件（2 个文件：ExponentialMovingAverage.cs, SceneTriggerResetter.cs），生成 Utils.md
        -   _Requirements: 2.4, 4.1, 4.2, 4.3, 4.4, 4.5_

    -   [x] 5.5 分析根目录文件

        -   读取并解析 EscapeFromDuckovCoopMod/根目录下的 C#文件（8 个文件：AnimParamInterpolator .cs, AutoRequestHealthBar.cs, BuffLateBinder.cs, BuildInfo.cs, DeferedRunner.cs, GlobalUsings.cs, HoldVisualBinder.cs, HostForceHealthBar.cs）

        -   分析这些辅助类的功能和作用
        -   生成根目录辅助类文档（RootHelpers.md）
        -   _Requirements: 2.4, 4.1, 4.2, 4.3, 4.4, 4.5_

-   [x] 6. 生成项目总体架构文档

    -   [x] 6.1 创建项目总览文档

    -   [ ] 6.1 创建项目总览文档

        -   创建 codeAnalysis/README.md 作为总入口
        -   编写项目概述和背景介绍
        -   说明技术栈和依赖库（Unity、LiteNetLib、HarmonyLib）
        -   更新统计信息：28 个目录，110 个 C#文件，最大深度 2
        -   _Requirements: 6.1, 6.2, 6.3_

    -   [x] 6.2 描述整体架构

        -   绘制或描述项目的模块架构图
        -   说明各主要模块的职责和边界
        -   描述模块间的依赖关系和交互方式
        -   _Requirements: 6.2, 6.4_

    -   [x] 6.3 说明核心流程

        -   描述模组加载和初始化流程

        -   描述网络连接建立流程（Steam/LAN）
        -   描述玩家同步流程
        -   描述 AI 同步流程
        -   描述物品和场景同步流程
        -   描述场景切换和投票流程
        -   _Requirements: 6.5, 8.2, 8.3, 8.4_

    -   [x] 6.4 突出关键 API 和设计模式

        -   列出核心类和关键接口
        -   说明 RPC 通信机制
        -   解释状态同步机制
        -   识别并说明使用的设计模式
        -   _Requirements: 6.6, 8.1, 8.3, 8.4, 8.5_

    -   [x] 6.5 创建文档导航和索引

        -   在总览文档中添加目录结构
        -   创建各模块文档的链接索引

        -   添加快速导航指南
        -   _Requirements: 7.5_

-   [x] 7. 优化和完善文档

    -   [x] 7.1 格式化和美化文档

        -   统一所有文档的 Markdown 格式
        -   添加适当的标题层次结构
        -   使用代码块展示代码示例
        -   使用表格和列表组织信息
        -   _Requirements: 7.1, 7.2, 7.3, 7.4_

    -   [x] 7.2 添加代码示例

        -   为关键 API 添加使用示例
        -   为核心流程添加代码片段
        -   确保代码示例准确可用

        -   _Requirements: 8.5_

    -   [x] 7.3 审查和修正


        -   检查所有类名、方法名的准确性
        -   验证文档的完整性和覆盖率
        -   修正中英文混排和术语使用
        -   检查链接和引用的有效性
        -   _Requirements: 7.6_

    -   [x] 7.4 生成最终文档包




        -   将所有文档整理到 codeAnalysis/目录
        -   创建文档版本信息
        -   添加文档更新日志
        -   记录当前分支：feat/net-sync-fix
        -   _Requirements: 6.1_
