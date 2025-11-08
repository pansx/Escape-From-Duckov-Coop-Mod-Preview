# 项目结构扫描报告

**项目路径**：`C:\SteamLibrary\steamapps\common\Escape from Duckov\Escape-From-Duckov-Coop-Mod-Preview-master\EscapeFromDuckovCoopMod`

**统计信息**：- 总目录数: 28

- 总 C#文件数: 110

- 最大深度: 2

## 按深度分组的目录

### 深度 2 (20 个目录)

#### `Main\AI`

- C#文件数: 5

- 文件列表:

  - AIHandle.cs

  - AIHealth.cs

  - AIName.cs

  - AIRequest.cs

  - AITool.cs

#### `Main\ClientService`

- C#文件数: 3

- 文件列表:

  - ClientHandle.cs

  - ClientPlayerApply.cs

  - SnedClientStatus.cs

#### `Main\Health`

- C#文件数: 4

- 文件列表:

  - Buff.cs

  - HealthM.cs

  - HealthTool.cs

  - HurtM.cs

#### `Main\HostService`

- C#文件数: 2

- 文件列表:

  - HostHandle.cs

  - HostPlayerApply.cs

#### `Main\Item`

- C#文件数: 3

- 文件列表:

  - ItemHandle.cs

  - ItemRequest.cs

  - ItemTool.cs

#### `Main\Loader`

- C#文件数: 2

- 文件列表:

  - Loader.cs

  - Mod.cs

#### `Main\LocalPlayer`

- C#文件数: 3

- 文件列表:

  - LocalPlayerManager.cs

  - SendLocalPlayerStatus.cs

  - Spectator.cs

#### `Main\Localization`

- C#文件数: 1

- 文件列表:

  - LocalizationManager.cs

#### `Main\SceneService`

- C#文件数: 8

- 文件列表:

  - CreateRemoteCharacter.cs

  - DeadLootBox.cs

  - Destructible.cs

  - Door.cs

  - LootManager.cs

  - LootNet.cs

  - SceneM.cs

  - SceneNet.cs

#### `Main\UI`

- C#文件数: 4

- 文件列表:

  - MModUI.cs

  - MModUIComponents.cs

  - MModUILayoutBuilder.cs

  - ModUI.cs

#### `Main\Weapon`

- C#文件数: 5

- 文件列表:

  - FakeProjectileRegistry.cs

  - GrenadeM.cs

  - WeaponHandle.cs

  - WeaponRequest.cs

  - WeaponTool.cs

#### `Main\WeatherAndTime`

- C#文件数: 1

- 文件列表:

  - Weather.cs

#### `Net\NetPack`

- C#文件数: 3

- 文件列表:

  - NetPack.cs

  - NetPackProjectile.cs

  - PackFlag.cs

#### `Net\Steam`

- C#文件数: 6

- 文件列表:

  - SteamEndPointMapper.cs

  - SteamLobbyHelper.cs

  - SteamLobbyManager.cs

  - SteamLobbyOptions.cs

  - SteamP2PLoader.cs

  - SteamP2PManager.cs

#### `Patch\Character`

- C#文件数: 8

- 文件列表:

  - AICharacterControllerPatch.cs

  - AnimPacth.cs

  - BuffPatch.cs

  - CharacterItemControl_Patch.cs

  - CharacterMainControlPatch.cs

  - CharacterSpawnerRootPatch.cs

  - HealthPatch.cs

  - Health_HurtPacth.cs

#### `Patch\InventoryAndLootBox`

- C#文件数: 5

- 文件列表:

  - InteractableLootboxPatch.cs

  - InventoryPatch.cs

  - LootBoxLoaderPatch.cs

  - LootSpawner.cs

  - LootViewPatch.cs

#### `Patch\Item`

- C#文件数: 6

- 文件列表:

  - Grenade_BreaKablePatch.cs

  - GunPatch.cs

  - ItemExtensionsPatch.cs

  - ItemPatch.cs

  - ItemUtilitiesPatch.cs

  - SlotPatch.cs

#### `Patch\Projectile`

- C#文件数: 1

- 文件列表:

  - FakeProjectilePatch.cs

#### `Patch\Scene`

- C#文件数: 3

- 文件列表:

  - DoorPatch.cs

  - LevelManagerPatch.cs

  - ScenePatch.cs

#### `Patch\SteamP2P`

- C#文件数: 3

- 文件列表:

  - PacketSignature.cs

  - Patch_LiteNetLib.cs

  - Patch_Socket.cs

### 深度 1 (7 个目录)

#### `Main`

- C#文件数: 8

- 文件列表:

  - COOPManager.cs

  - CoopTool.cs

  - CustomFace.cs

  - FxManager.cs

  - HarmonyFix.cs

  - NetService.cs

  - Op.cs

  - PublicHandleUpdate.cs

- 子目录: AI, ClientService, Health, HostService, Item, Loader, LocalPlayer, Localization, SceneService, UI, Weapon, WeatherAndTime

#### `Net`

- C#文件数: 11

- 文件列表:

  - LocalHitKillFx.cs

  - NetAiFollower.cs

  - NetAiTag.cs

  - NetAiVisibilityGuard.cs

  - NetDataExtensions.cs

  - NetInterpolator.cs

  - NetPacketPool.cs

  - NetSilenceGuards.cs

  - NetworkExtensions.cs

  - OpPriority.cs

  - PacketPriority.cs

- 子目录: NetPack, Steam

#### `NetTag`

- C#文件数: 3

- 文件列表:

  - NetDestructibleTag.cs

  - NetDropTag.cs

  - NetGrenadeTag.cs

#### `Patch`

- C#文件数: 0

- 子目录: Character, InventoryAndLootBox, Item, Projectile, Scene, SteamP2P

#### `Properties`

- C#文件数: 1

- 文件列表:

  - AssemblyInfo.cs

#### `SyncData`

- C#文件数: 1

- 文件列表:

  - SyncDataManger.cs

#### `Utils`

- C#文件数: 2

- 文件列表:

  - ExponentialMovingAverage.cs

  - SceneTriggerResetter.cs

### 深度 0 (1 个目录)

#### `.`

- C#文件数: 8

- 文件列表:

  - AnimParamInterpolator .cs

  - AutoRequestHealthBar.cs

  - BuffLateBinder.cs

  - BuildInfo.cs

  - DeferedRunner.cs

  - GlobalUsings.cs

  - HoldVisualBinder.cs

  - HostForceHealthBar.cs

- 子目录: Main, Net, NetTag, Patch, Properties, SyncData, Utils
