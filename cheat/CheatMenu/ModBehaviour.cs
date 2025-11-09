using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Duckov.Buffs;
using Duckov.Modding;
using Duckov.Utilities;
using ItemStatsSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CheatMenu
{
	// Token: 0x02000005 RID: 5
	[NullableContext(1)]
	[Nullable(0)]
	public class ModBehaviour : ModBehaviour
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000005 RID: 5 RVA: 0x0000208E File Offset: 0x0000028E
		// (set) Token: 0x06000006 RID: 6 RVA: 0x00002095 File Offset: 0x00000295
		[Nullable(2)]
		public static ModBehaviour Instance
		{
			[NullableContext(2)]
			get;
			[NullableContext(2)]
			private set;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x0000209D File Offset: 0x0000029D
		private void Awake()
		{
			ModBehaviour.Instance = this;
			this._damageReceiverLayerMask = GameplayDataSettings.Layers.damageReceiverLayerMask;
			this.LoadSettings();
		}

		// Token: 0x06000008 RID: 8 RVA: 0x000020BB File Offset: 0x000002BB
		private void OnDestroy()
		{
			this.RestoreMovementUnlockEffects();
			this.DestroyOverlayResources();
			if (ModBehaviour.Instance == this)
			{
				ModBehaviour.Instance = null;
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000020DC File Offset: 0x000002DC
		private void OnEnable()
		{
			LevelManager.OnAfterLevelInitialized += this.OnLevelInitialized;
			this.StartAutoAimWorker();
			this.TryApplyNoRecoil();
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000020FC File Offset: 0x000002FC
		private void OnDisable()
		{
			LevelManager.OnAfterLevelInitialized -= this.OnLevelInitialized;
			this.StopAutoAimWorker();
			this.ClearAutoAimThreadData();
			this.SetPenetrationActive(false);
			this.DisableAllGrenadeHoming();
			this.UnhookCharacterEvents();
			this.RestoreOriginalCharacterRecoil();
			this.RestoreGunStatMultipliers();
			this.RestoreItemStatOverrides();
			this.RestoreMovementUnlockEffects();
			this.DestroyOverlayResources();
			this._rangeVisible = true;
			this._lastTriggerInput = false;
			this.RestoreThrowableConsumption();
			this._registeredGrenades.Clear();
			this._activeGrenades.Clear();
			this._activeGrenadeIds.Clear();
			this._grenadeSnapshotBuffer.Clear();
			this._trackedGrenadePrefabs.Clear();
			this._trackedGrenadeSkills.Clear();
			this._grenadeSkillRefs.Clear();
			this._skillReleaseOriginalValues.Clear();
			this._skillKeeperHooks.Clear();
			this._pendingMagazineRestores.Clear();
			this._damageReceiverCache = Array.Empty<DamageReceiver>();
			this._activeProjectiles.Clear();
			this._activeProjectileIds.Clear();
			this._projectileSnapshotBuffer.Clear();
			this._damageReceiverCacheFrame = -1;
			this._damageReceiverCacheCount = 0;
			this._damageReceiverCacheCenter = Vector3.zero;
			this._damageReceiverCacheRadius = 0f;
			this._nextGrenadeScanTime = 0f;
			this._nextProjectileCleanupTime = 0f;
			this._retargetedProjectiles.Clear();
			this._penetratingProjectiles.Clear();
			this._originalProjectileMasks.Clear();
			this._retargetedProjectileRefs.Clear();
			this._penetratingProjectileRefs.Clear();
			this._autoAimThreadTargetBuffer.Clear();
			this._nextCharacterRetryTime = 0f;
			this._trackedAgentHolder = null;
			this._agentHolderEventsHooked = false;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x0000229A File Offset: 0x0000049A
		private void OnLevelInitialized()
		{
			this.ResetSpinKillState();
			this.DestroySpinKillRangeRenderer();
			this._spinKillColliderBuffer = Array.Empty<Collider>();
			this.TryApplyNoRecoil();
		}

		// Token: 0x0600000C RID: 12 RVA: 0x000022BC File Offset: 0x000004BC
		private void TryApplyNoRecoil()
		{
			CharacterMainControl main = CharacterMainControl.Main;
			if (main == null || main.CharacterItem == null)
			{
				return;
			}
			this.HookCharacterEvents(main);
			this.ApplyInvincibility(main);
			this.MaintainSurvivalNeeds(main);
			this.ApplyNoRecoilToCharacter(main);
			ItemAgent_Gun itemAgent_Gun = ((main.agentHolder != null) ? main.agentHolder.CurrentHoldGun : null);
			this.ApplyNoRecoilToGun(itemAgent_Gun);
			this.MaintainHeldItemDurability(main);
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002330 File Offset: 0x00000530
		private void HookCharacterEvents(CharacterMainControl character)
		{
			if (character == null)
			{
				return;
			}
			if (this._trackedCharacter != character)
			{
				this.UnhookCharacterEvents();
				this._trackedCharacter = character;
			}
			if (this._trackedCharacter != null && !this._characterEventsHooked)
			{
				CharacterMainControl.OnMainCharacterInventoryChangedEvent = (Action<CharacterMainControl, Inventory, int>)Delegate.Combine(CharacterMainControl.OnMainCharacterInventoryChangedEvent, new Action<CharacterMainControl, Inventory, int>(this.OnMainCharacterInventoryChanged));
				this._characterEventsHooked = true;
			}
			if (this._trackedCharacter != null)
			{
				this.EnsureAgentHolderHooked(this._trackedCharacter);
				ItemAgent_Gun itemAgent_Gun = ((this._trackedCharacter.agentHolder != null) ? this._trackedCharacter.agentHolder.CurrentHoldGun : null);
				this.HookGunEvents(itemAgent_Gun);
			}
			if (this._trackedCharacter != null)
			{
				this.HookCharacterSkills(this._trackedCharacter);
			}
			this.ApplyInvincibility(this._trackedCharacter);
			this.MaintainSurvivalNeeds(this._trackedCharacter);
			this.MaintainHeldItemDurability(this._trackedCharacter);
			this._lastTriggerInput = false;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000242C File Offset: 0x0000062C
		private void UnhookCharacterEvents()
		{
			if (this._trackedCharacter != null)
			{
				bool characterEventsHooked = this._characterEventsHooked;
			}
			CharacterMainControl.OnMainCharacterInventoryChangedEvent = (Action<CharacterMainControl, Inventory, int>)Delegate.Remove(CharacterMainControl.OnMainCharacterInventoryChangedEvent, new Action<CharacterMainControl, Inventory, int>(this.OnMainCharacterInventoryChanged));
			this._characterEventsHooked = false;
			if (this._trackedAgentHolder != null && this._agentHolderEventsHooked)
			{
				this._trackedAgentHolder.OnHoldAgentChanged -= this.OnHoldAgentChanged;
			}
			this._trackedAgentHolder = null;
			this._agentHolderEventsHooked = false;
			this.HookGunEvents(null);
			this.UnhookCharacterSkills();
			this.RestoreMovementUnlockEffects();
			this.RestoreCharacterInvincibility();
			this._trackedCharacter = null;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000024D0 File Offset: 0x000006D0
		[NullableContext(2)]
		private void EnsureAgentHolderHooked(CharacterMainControl character)
		{
			if (character == null)
			{
				if (this._trackedAgentHolder != null && this._agentHolderEventsHooked)
				{
					this._trackedAgentHolder.OnHoldAgentChanged -= this.OnHoldAgentChanged;
				}
				this._trackedAgentHolder = null;
				this._agentHolderEventsHooked = false;
				return;
			}
			ItemAgentHolder agentHolder = character.agentHolder;
			if (agentHolder == null)
			{
				if (this._trackedAgentHolder != null && this._agentHolderEventsHooked)
				{
					this._trackedAgentHolder.OnHoldAgentChanged -= this.OnHoldAgentChanged;
				}
				this._trackedAgentHolder = null;
				this._agentHolderEventsHooked = false;
				return;
			}
			if (this._trackedAgentHolder == agentHolder)
			{
				if (!this._agentHolderEventsHooked)
				{
					agentHolder.OnHoldAgentChanged += this.OnHoldAgentChanged;
					this._agentHolderEventsHooked = true;
					DuckovItemAgent currentHoldGun = agentHolder.CurrentHoldGun;
					this.OnHoldAgentChanged(currentHoldGun);
				}
				return;
			}
			if (this._trackedAgentHolder != null && this._agentHolderEventsHooked)
			{
				this._trackedAgentHolder.OnHoldAgentChanged -= this.OnHoldAgentChanged;
			}
			this._trackedAgentHolder = agentHolder;
			agentHolder.OnHoldAgentChanged += this.OnHoldAgentChanged;
			this._agentHolderEventsHooked = true;
			DuckovItemAgent currentHoldGun2 = agentHolder.CurrentHoldGun;
			this.OnHoldAgentChanged(currentHoldGun2);
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002607 File Offset: 0x00000807
		private void OnHoldAgentChanged(DuckovItemAgent agent)
		{
			this.ApplyNoRecoilToGun(agent as ItemAgent_Gun);
			if (this._trackedCharacter != null)
			{
				this.MaintainHeldItemDurability(this._trackedCharacter);
				this.HookCharacterSkills(this._trackedCharacter);
			}
		}

		// Token: 0x06000011 RID: 17 RVA: 0x0000263C File Offset: 0x0000083C
		private void OnMainCharacterInventoryChanged(CharacterMainControl character, Inventory inventory, int slotIndex)
		{
			if (character != this._trackedCharacter || inventory == null)
			{
				return;
			}
			if (slotIndex < 0 || slotIndex >= inventory.Content.Count)
			{
				return;
			}
			Item item = inventory.Content[slotIndex];
			if (item == null)
			{
				return;
			}
			if (!this._noRecoilEnabled)
			{
				this.RestoreItemStatOverridesForItem(item);
			}
			if (this._trackedCharacter != null && this._trackedCharacter.agentHolder != null)
			{
				ItemAgent_Gun currentHoldGun = this._trackedCharacter.agentHolder.CurrentHoldGun;
				if (currentHoldGun != null && currentHoldGun.Item == item)
				{
					this.ApplyNoRecoilToGun(currentHoldGun);
				}
			}
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000026EC File Offset: 0x000008EC
		private void ApplyNoRecoilToCharacter(CharacterMainControl character)
		{
			if (!this._noRecoilEnabled)
			{
				this.RestoreOriginalCharacterRecoil();
				return;
			}
			Stat stat = character.CharacterItem.GetStat(ModBehaviour.RecoilControlStatHash);
			if (stat == null)
			{
				return;
			}
			if (this._originalCharacterRecoilControl == null)
			{
				this._originalCharacterRecoilControl = new float?(stat.BaseValue);
			}
			stat.BaseValue = 9999f;
			this.ForceDisableTriggerTimer(character, 0f);
			this.TryEnableDashControl(character);
		}

		// Token: 0x06000013 RID: 19 RVA: 0x0000275C File Offset: 0x0000095C
		[NullableContext(2)]
		private void ApplyNoRecoilToGun(ItemAgent_Gun gun)
		{
			if (gun == null || gun.Item == null)
			{
				return;
			}
			if (this._noRecoilEnabled)
			{
				this.ApplyNoRecoilToItem(gun.Item);
				this.ResetGunInternals(gun);
			}
			if (this._infiniteAmmoEnabled)
			{
				this.RestoreGunAmmo(gun);
			}
			this.HookGunEvents(gun);
			this.TryOverrideTriggerRestrictions(gun);
			this.ApplyGunStatMultipliers(gun);
			if (this._infiniteDurabilityEnabled)
			{
				this.EnsureItemDurability(gun.Item);
			}
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000027D4 File Offset: 0x000009D4
		private void ApplyNoRecoilToItem(Item item)
		{
			foreach (int num in ModBehaviour.GunStatHashes)
			{
				this.OverrideItemStat(item, num, 0f);
			}
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002808 File Offset: 0x00000A08
		[NullableContext(2)]
		private void HookGunEvents(ItemAgent_Gun gun)
		{
			if (this._trackedGun == gun)
			{
				return;
			}
			ItemAgent_Gun trackedGun = this._trackedGun;
			if (trackedGun != null)
			{
				trackedGun.OnShootEvent -= this.OnGunShoot;
				if (this._noRecoilEnabled)
				{
					this.RestoreItemStatOverridesForItem(trackedGun.Item);
				}
			}
			this._trackedGun = gun;
			if (this._trackedGun != null)
			{
				this._trackedGun.OnShootEvent += this.OnGunShoot;
				this.TryOverrideTriggerRestrictions(this._trackedGun);
			}
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002892 File Offset: 0x00000A92
		private void HookCharacterSkills(CharacterMainControl character)
		{
			if (character == null || character.skillAction == null)
			{
				return;
			}
			this.RegisterSkillKeeper(character.skillAction.holdItemSkillKeeper);
			this.RegisterSkillKeeper(character.skillAction.characterSkillKeeper);
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000028D0 File Offset: 0x00000AD0
		[NullableContext(2)]
		private void RegisterSkillKeeper(CharacterSkillKeeper keeper)
		{
			if (keeper == null)
			{
				return;
			}
			if (!this._skillKeeperHooks.ContainsKey(keeper))
			{
				Action action = delegate
				{
					this.OnSkillKeeperChanged(keeper);
				};
				this._skillKeeperHooks[keeper] = action;
				CharacterSkillKeeper keeper2 = keeper;
				keeper2.OnSkillChanged = (Action)Delegate.Combine(keeper2.OnSkillChanged, action);
			}
			this.OnSkillKeeperChanged(keeper);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002954 File Offset: 0x00000B54
		private void OnSkillKeeperChanged(CharacterSkillKeeper keeper)
		{
			if (keeper == null)
			{
				return;
			}
			this.TryTrackGrenadeSkill(keeper.Skill);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002968 File Offset: 0x00000B68
		[NullableContext(2)]
		private void TryTrackGrenadeSkill(SkillBase skill)
		{
			if (skill == null)
			{
				return;
			}
			Skill_Grenade skill_Grenade = skill as Skill_Grenade;
			if (skill_Grenade != null)
			{
				this.EnsureGrenadeSkillTracked(skill_Grenade);
			}
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002990 File Offset: 0x00000B90
		private void EnsureGrenadeSkillTracked(Skill_Grenade skill)
		{
			if (skill == null)
			{
				return;
			}
			int instanceID = skill.GetInstanceID();
			this._grenadeSkillRefs[instanceID] = new WeakReference<Skill_Grenade>(skill);
			if (this._trackedGrenadeSkills.Add(instanceID) && skill.grenadePfb != null)
			{
				this.EnsureGrenadePrefabTracker(skill.grenadePfb);
			}
			this.ConfigureGrenadeSkillConsumption(skill);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000029F0 File Offset: 0x00000BF0
		private void EnsureGrenadePrefabTracker(Grenade grenadePrefab)
		{
			if (grenadePrefab == null)
			{
				return;
			}
			int instanceID = grenadePrefab.GetInstanceID();
			if (!this._trackedGrenadePrefabs.Add(instanceID))
			{
				return;
			}
			if (grenadePrefab.GetComponent<ModBehaviour.GrenadeTracker>() == null)
			{
				grenadePrefab.gameObject.AddComponent<ModBehaviour.GrenadeTracker>();
			}
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002A38 File Offset: 0x00000C38
		private void ConfigureGrenadeSkillConsumption(Skill_Grenade skill)
		{
			if (skill == null)
			{
				return;
			}
			ItemSetting_Skill component = skill.GetComponent<ItemSetting_Skill>();
			if (component == null)
			{
				return;
			}
			if (this._infiniteThrowablesEnabled)
			{
				if (!this._skillReleaseOriginalValues.ContainsKey(component))
				{
					this._skillReleaseOriginalValues[component] = component.onRelease;
				}
				component.onRelease = ItemSetting_Skill.OnReleaseAction.none;
				Item item = component.Item;
				if (item != null && item.Stackable && item.StackCount <= 0)
				{
					item.StackCount = 1;
					return;
				}
			}
			else
			{
				this.RestoreSkillOnRelease(component);
			}
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002AC0 File Offset: 0x00000CC0
		private void RestoreSkillOnRelease(ItemSetting_Skill skillSetting)
		{
			if (skillSetting == null)
			{
				return;
			}
			ItemSetting_Skill.OnReleaseAction onReleaseAction;
			if (this._skillReleaseOriginalValues.TryGetValue(skillSetting, out onReleaseAction))
			{
				skillSetting.onRelease = onReleaseAction;
				this._skillReleaseOriginalValues.Remove(skillSetting);
			}
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002AFC File Offset: 0x00000CFC
		private void ReapplyThrowableConsumption()
		{
			this.CleanGrenadeSkillRefs();
			if (this._grenadeSkillRefs.Count == 0)
			{
				return;
			}
			using (Dictionary<int, WeakReference<Skill_Grenade>>.ValueCollection.Enumerator enumerator = this._grenadeSkillRefs.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Skill_Grenade skill_Grenade;
					if (enumerator.Current.TryGetTarget(out skill_Grenade) && skill_Grenade != null)
					{
						this.ConfigureGrenadeSkillConsumption(skill_Grenade);
					}
				}
			}
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002B7C File Offset: 0x00000D7C
		private void RestoreThrowableConsumption()
		{
			if (this._skillReleaseOriginalValues.Count == 0)
			{
				return;
			}
			foreach (KeyValuePair<ItemSetting_Skill, ItemSetting_Skill.OnReleaseAction> keyValuePair in this._skillReleaseOriginalValues.ToArray<KeyValuePair<ItemSetting_Skill, ItemSetting_Skill.OnReleaseAction>>())
			{
				ItemSetting_Skill key = keyValuePair.Key;
				if (key == null)
				{
					this._skillReleaseOriginalValues.Remove(keyValuePair.Key);
				}
				else
				{
					key.onRelease = keyValuePair.Value;
				}
			}
			this._skillReleaseOriginalValues.Clear();
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002BF8 File Offset: 0x00000DF8
		private void CleanGrenadeSkillRefs()
		{
			if (this._grenadeSkillRefs.Count == 0)
			{
				return;
			}
			List<int> list = null;
			foreach (KeyValuePair<int, WeakReference<Skill_Grenade>> keyValuePair in this._grenadeSkillRefs)
			{
				Skill_Grenade skill_Grenade;
				if (!keyValuePair.Value.TryGetTarget(out skill_Grenade))
				{
					if (list == null)
					{
						list = new List<int>();
					}
					list.Add(keyValuePair.Key);
				}
			}
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					int num = list[i];
					this._grenadeSkillRefs.Remove(num);
					this._trackedGrenadeSkills.Remove(num);
				}
			}
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002CB8 File Offset: 0x00000EB8
		private void UnhookCharacterSkills()
		{
			if (this._skillKeeperHooks.Count > 0)
			{
				foreach (KeyValuePair<CharacterSkillKeeper, Action> keyValuePair in this._skillKeeperHooks.ToArray<KeyValuePair<CharacterSkillKeeper, Action>>())
				{
					if (keyValuePair.Key != null)
					{
						CharacterSkillKeeper key = keyValuePair.Key;
						key.OnSkillChanged = (Action)Delegate.Remove(key.OnSkillChanged, keyValuePair.Value);
					}
				}
				this._skillKeeperHooks.Clear();
			}
			this._trackedGrenadeSkills.Clear();
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002D38 File Offset: 0x00000F38
		private void EnsureCharacterTracking()
		{
			if (this._trackedCharacter != null && !this._trackedCharacter.isActiveAndEnabled)
			{
				this.UnhookCharacterEvents();
			}
			CharacterMainControl main = CharacterMainControl.Main;
			if (main == null)
			{
				if (this._trackedCharacter != null)
				{
					this.UnhookCharacterEvents();
				}
				return;
			}
			if (this._trackedCharacter == main)
			{
				return;
			}
			if (Time.unscaledTime < this._nextCharacterRetryTime)
			{
				return;
			}
			this._nextCharacterRetryTime = Time.unscaledTime + 0.2f;
			this.TryApplyNoRecoil();
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002DC0 File Offset: 0x00000FC0
		private void Update()
		{
			this.EnsureCharacterTracking();
			this.HandleOverlayInput();
			CharacterMainControl trackedCharacter = this._trackedCharacter;
			this.EnsureAgentHolderHooked(trackedCharacter);
			this.MaintainGrenadeTracking(trackedCharacter);
			if (trackedCharacter != null)
			{
				this.MaintainTriggerAccess(trackedCharacter);
				this.MaintainSpinKill(trackedCharacter);
				if (this._infiniteDurabilityEnabled)
				{
					this.MaintainHeldItemDurability(trackedCharacter);
				}
				this.MaintainSurvivalNeeds(trackedCharacter);
				this.MaintainInfiniteStamina(trackedCharacter);
				if (this._obstaclePenetrationEnabled && this._penetrationActive)
				{
					this.MaintainPenetratingProjectiles();
				}
			}
			else
			{
				this._lastTriggerInput = false;
				this.ReleaseSpinKillTrigger(null);
				this.ResetSpinKillState();
			}
			if (trackedCharacter == null)
			{
				this.ClearAutoAimThreadData();
				this.SetMenuVisibility(false);
				this.HideAutoAimRange();
				this._nextAutoAimThreadStateUpdateTime = 0f;
				return;
			}
			LevelManager instance = LevelManager.Instance;
			if (!LevelManager.LevelInited || instance == null || instance.GameCamera == null || instance.GameCamera.renderCamera == null)
			{
				this.ClearAutoAimThreadData();
				this.SetMenuVisibility(false);
				this.HideAutoAimRange();
				this._nextAutoAimThreadStateUpdateTime = 0f;
				return;
			}
			Camera renderCamera = instance.GameCamera.renderCamera;
			InputManager inputManager = instance.InputManager;
			Vector2 vector = ((inputManager != null) ? inputManager.AimScreenPoint : new Vector2((float)Screen.width * 0.5f, (float)Screen.height * 0.5f));
			this._latestAimScreenPoint = vector;
			Ray ray = renderCamera.ScreenPointToRay(new Vector3(vector.x, vector.y, 0f));
			if (this._autoAimEnabled)
			{
				if (Time.unscaledTime >= this._nextAutoAimThreadStateUpdateTime)
				{
					this.MaintainAutoAimThreadState(trackedCharacter, vector, renderCamera, ray);
					this._nextAutoAimThreadStateUpdateTime = Time.unscaledTime + 0.02f;
				}
			}
			else
			{
				this.ClearAutoAimThreadData();
				this._nextAutoAimThreadStateUpdateTime = 0f;
			}
			if (this._menuVisible)
			{
				this.InitializeMenuWindowIfNeeded();
				this.RefreshMenuContent(trackedCharacter, vector);
			}
			else
			{
				this.SetMenuVisibility(false);
			}
			if (this._rangeVisible && this._autoAimEnabled)
			{
				float num = trackedCharacter.transform.position.y + 0.5f;
				this.UpdateAutoAimRangeVisualization(renderCamera, vector, num);
				return;
			}
			this.HideAutoAimRange();
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002FD8 File Offset: 0x000011D8
		private void OnGunShoot()
		{
			if (this._trackedGun == null)
			{
				return;
			}
			Projectile projectile = this.CaptureImmediateProjectile(this._trackedGun);
			this.TryAutoAimShot(this._trackedGun, projectile);
			if (this._noRecoilEnabled)
			{
				this.ResetGunInternals(this._trackedGun);
				if (this._trackedGun.Item != null)
				{
					this.ApplyNoRecoilToItem(this._trackedGun.Item);
				}
			}
			if (this._infiniteAmmoEnabled)
			{
				this.RestoreGunAmmo(this._trackedGun);
			}
			this.MaintainHeldItemDurability(this._trackedCharacter);
			this.TryOverrideTriggerRestrictions(this._trackedGun);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00003074 File Offset: 0x00001274
		[NullableContext(2)]
		private void MaintainGrenadeTracking(CharacterMainControl character)
		{
			if (!this._autoAimEnabled)
			{
				return;
			}
			if (!this._grenadeAutoAimEnabled)
			{
				if (this._registeredGrenades.Count > 0)
				{
					this._registeredGrenades.Clear();
				}
				this._grenadeSnapshotBuffer.Clear();
				return;
			}
			if (!LevelManager.LevelInited || LevelManager.Instance == null)
			{
				return;
			}
			if (Time.unscaledTime < this._nextGrenadeScanTime)
			{
				return;
			}
			this._nextGrenadeScanTime = Time.unscaledTime + 0.1f;
			CharacterMainControl characterMainControl = character ?? CharacterMainControl.Main;
			if (characterMainControl == null)
			{
				return;
			}
			if (this._activeGrenades.Count == 0)
			{
				if (this._registeredGrenades.Count > 0)
				{
					this._registeredGrenades.Clear();
				}
				return;
			}
			this._grenadeSnapshotBuffer.Clear();
			for (int i = 0; i < this._activeGrenades.Count; i++)
			{
				Grenade grenade = this._activeGrenades[i];
				if (grenade != null)
				{
					this._grenadeSnapshotBuffer.Add(grenade);
				}
			}
			if (this._grenadeSnapshotBuffer.Count == 0)
			{
				if (this._registeredGrenades.Count > 0)
				{
					this._registeredGrenades.Clear();
				}
				return;
			}
			foreach (Grenade grenade2 in this._grenadeSnapshotBuffer)
			{
				if (!(grenade2 == null) && !(grenade2.damageInfo.fromCharacter != characterMainControl))
				{
					int instanceID = grenade2.GetInstanceID();
					if (!this._registeredGrenades.Contains(instanceID))
					{
						if (grenade2.gameObject.GetComponent<ModBehaviour.GrenadeHomingComponent>() != null)
						{
							this._registeredGrenades.Add(instanceID);
						}
						else
						{
							Rigidbody component = grenade2.GetComponent<Rigidbody>();
							Vector3 position = grenade2.transform.position;
							Vector3 vector = ((component != null) ? component.velocity : Vector3.zero);
							if (this.TryRegisterHomingGrenade(grenade2, characterMainControl, position, vector))
							{
								this._registeredGrenades.Add(instanceID);
							}
						}
					}
				}
			}
			this._grenadeSnapshotBuffer.Clear();
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00003290 File Offset: 0x00001490
		private bool TryRegisterHomingGrenade(Grenade grenade, CharacterMainControl thrower, Vector3 origin, Vector3 initialVelocity)
		{
			if (!this._autoAimEnabled || !this._grenadeAutoAimEnabled)
			{
				return false;
			}
			if (grenade == null || thrower == null)
			{
				return false;
			}
			LevelManager instance = LevelManager.Instance;
			if (!LevelManager.LevelInited || instance == null || instance.GameCamera == null || instance.GameCamera.renderCamera == null)
			{
				return false;
			}
			Camera renderCamera = instance.GameCamera.renderCamera;
			InputManager inputManager = instance.InputManager;
			Vector2 vector = ((inputManager != null) ? inputManager.AimScreenPoint : new Vector2((float)Screen.width * 0.5f, (float)Screen.height * 0.5f));
			Ray ray = renderCamera.ScreenPointToRay(new Vector3(vector.x, vector.y, 0f));
			ModBehaviour.AutoAimCandidate autoAimCandidate;
			if (!this.TryAcquireThreadCandidate(thrower.Team, renderCamera, origin, ray, vector, 60f, out autoAimCandidate))
			{
				return false;
			}
			if (autoAimCandidate.Receiver == null)
			{
				return false;
			}
			ModBehaviour.GrenadeHomingComponent grenadeHomingComponent = ModBehaviour.EnsureGrenadeHomingComponent(grenade);
			float num = Mathf.Max(initialVelocity.magnitude, 12f);
			grenadeHomingComponent.Initialize(this, grenade, autoAimCandidate.Receiver, 140f, num);
			ModBehaviour.AutoAimLog("Homing grenade locked on " + autoAimCandidate.Receiver.name);
			return true;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000033D3 File Offset: 0x000015D3
		internal void UnregisterHomingGrenade(Grenade grenade)
		{
			if (grenade == null)
			{
				return;
			}
			this._registeredGrenades.Remove(grenade.GetInstanceID());
		}

		// Token: 0x06000028 RID: 40 RVA: 0x000033F4 File Offset: 0x000015F4
		private void MaintainAutoAimThreadState(CharacterMainControl character, Vector2 aimScreenPoint, Camera camera, Ray aimRay)
		{
			if (!this._autoAimEnabled)
			{
				return;
			}
			ItemAgent_Gun itemAgent_Gun = ((character.agentHolder != null) ? character.agentHolder.CurrentHoldGun : null);
			Vector3 vector = ((itemAgent_Gun != null && itemAgent_Gun.muzzle != null) ? itemAgent_Gun.muzzle.position : (character.transform.position + character.transform.forward * 0.4f));
			Vector3 vector2 = ((aimRay.direction.sqrMagnitude > 0.0001f) ? aimRay.direction.normalized : character.transform.forward.normalized);
			Vector3 vector3 = ((itemAgent_Gun != null && itemAgent_Gun.muzzle != null && itemAgent_Gun.muzzle.forward.sqrMagnitude > 0.0001f) ? itemAgent_Gun.muzzle.forward.normalized : vector2);
			float num = Mathf.Max((itemAgent_Gun != null) ? Mathf.Max(2f, itemAgent_Gun.BulletDistance) : 80f, 60f);
			int damageReceiversSnapshot = this.GetDamageReceiversSnapshot(aimRay, camera, aimScreenPoint, this._autoAimScreenRadius, num);
			if (damageReceiversSnapshot == 0)
			{
				this.ClearAutoAimThreadData();
				return;
			}
			this._autoAimThreadTargetBuffer.Clear();
			HashSet<int> hashSet = null;
			for (int i = 0; i < damageReceiversSnapshot; i++)
			{
				DamageReceiver damageReceiver = this._damageReceiverCache[i];
				ModBehaviour.AutoAimThreadTarget autoAimThreadTarget;
				if (this.TryCaptureThreadTarget(character.Team, damageReceiver, vector, num, out autoAimThreadTarget))
				{
					this._autoAimThreadTargetBuffer.Add(autoAimThreadTarget);
					if (hashSet == null)
					{
						hashSet = new HashSet<int>();
					}
					hashSet.Add(damageReceiver.GetInstanceID());
				}
			}
			if (this._autoAimThreadTargetBuffer.Count == 0)
			{
				this.ClearAutoAimThreadData();
				this.CleanupUnusedHeadshotOffsets(hashSet);
				return;
			}
			ModBehaviour.AutoAimThreadTarget[] array = new ModBehaviour.AutoAimThreadTarget[this._autoAimThreadTargetBuffer.Count];
			this._autoAimThreadTargetBuffer.CopyTo(array);
			this._autoAimThreadTargetBuffer.Clear();
			ModBehaviour.AutoAimThreadContext autoAimThreadContext = new ModBehaviour.AutoAimThreadContext
			{
				Targets = array,
				TargetCount = array.Length,
				AimRayOrigin = aimRay.origin,
				AimRayDirection = vector2,
				Forward = vector3,
				MuzzlePosition = vector,
				AimScreenPoint = aimScreenPoint,
				ScreenRadius = this._autoAimScreenRadius,
				MaxRange = num,
				ScreenWidth = (float)Screen.width,
				ScreenHeight = (float)Screen.height,
				ViewProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * camera.worldToCameraMatrix,
				HasData = true
			};
			this._autoAimThreadContext = autoAimThreadContext;
			this.CleanupUnusedHeadshotOffsets(hashSet);
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000036A4 File Offset: 0x000018A4
		private bool TryCaptureThreadTarget(Teams playerTeam, DamageReceiver receiver, Vector3 muzzlePosition, float maxRange, out ModBehaviour.AutoAimThreadTarget target)
		{
			target = default(ModBehaviour.AutoAimThreadTarget);
			if (receiver == null)
			{
				return false;
			}
			if (!receiver.enabled || receiver.gameObject == null || !receiver.gameObject.activeInHierarchy)
			{
				return false;
			}
			if (receiver.IsDead || receiver.IsMainCharacter || !ModBehaviour.ReceiverHasHealth(receiver))
			{
				return false;
			}
			if (!Team.IsEnemy(playerTeam, receiver.Team))
			{
				return false;
			}
			Vector3 receiverAimPoint = this.GetReceiverAimPoint(receiver);
			float sqrMagnitude = (receiverAimPoint - muzzlePosition).sqrMagnitude;
			if (sqrMagnitude <= 0.04f)
			{
				return false;
			}
			float num = maxRange * maxRange;
			if (sqrMagnitude > num)
			{
				return false;
			}
			target = new ModBehaviour.AutoAimThreadTarget
			{
				Receiver = receiver,
				AimPoint = receiverAimPoint
			};
			return true;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00003763 File Offset: 0x00001963
		private bool TryGetAutoAimThreadCandidate(out ModBehaviour.AutoAimCandidate candidate)
		{
			if (!this._autoAimThreadResult.HasCandidate)
			{
				candidate = default(ModBehaviour.AutoAimCandidate);
				return false;
			}
			candidate = this._autoAimThreadResult.Candidate;
			return true;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00003790 File Offset: 0x00001990
		private bool TryAcquireThreadCandidate(Teams playerTeam, Camera camera, Vector3 muzzlePosition, Ray aimRay, Vector2 aimScreenPoint, float maxRange, out ModBehaviour.AutoAimCandidate candidate)
		{
			candidate = default(ModBehaviour.AutoAimCandidate);
			ModBehaviour.AutoAimCandidate autoAimCandidate;
			if (!this.TryGetAutoAimThreadCandidate(out autoAimCandidate))
			{
				return false;
			}
			DamageReceiver receiver = autoAimCandidate.Receiver;
			if (receiver == null)
			{
				return false;
			}
			if (!receiver.enabled || receiver.gameObject == null || !receiver.gameObject.activeInHierarchy)
			{
				return false;
			}
			if (receiver.IsDead || receiver.IsMainCharacter || !ModBehaviour.ReceiverHasHealth(receiver))
			{
				return false;
			}
			if (!Team.IsEnemy(playerTeam, receiver.Team))
			{
				return false;
			}
			Vector3 aimPoint = autoAimCandidate.AimPoint;
			float sqrMagnitude = (aimPoint - muzzlePosition).sqrMagnitude;
			if (sqrMagnitude <= 0.04f)
			{
				return false;
			}
			float num = maxRange * maxRange;
			if (sqrMagnitude > num)
			{
				return false;
			}
			Vector3 vector = ((aimRay.direction.sqrMagnitude > 0.0001f) ? aimRay.direction.normalized : Vector3.forward);
			float num2 = Vector3.Dot(aimPoint - aimRay.origin, vector);
			if (num2 <= 0f)
			{
				return false;
			}
			Vector3 vector2 = camera.WorldToScreenPoint(aimPoint);
			if (vector2.z <= 0f)
			{
				return false;
			}
			float num3 = Vector2.Distance(new Vector2(vector2.x, vector2.y), aimScreenPoint);
			if (num3 > this._autoAimScreenRadius)
			{
				return false;
			}
			bool flag = true;
			candidate = new ModBehaviour.AutoAimCandidate(receiver, aimPoint, num3, num2, flag);
			return true;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x000038F0 File Offset: 0x00001AF0
		private void StartAutoAimWorker()
		{
			if (this._autoAimWorkerThread != null)
			{
				return;
			}
			this._autoAimWorkerCts = new CancellationTokenSource();
			CancellationToken token = this._autoAimWorkerCts.Token;
			this._autoAimWorkerThread = new Thread(new ParameterizedThreadStart(this.AutoAimWorkerLoop))
			{
				IsBackground = true,
				Name = "CheatMenuAutoAimWorker"
			};
			this._autoAimWorkerThread.Start(token);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00003958 File Offset: 0x00001B58
		private void StopAutoAimWorker()
		{
			Thread autoAimWorkerThread = this._autoAimWorkerThread;
			if (autoAimWorkerThread == null)
			{
				return;
			}
			try
			{
				CancellationTokenSource autoAimWorkerCts = this._autoAimWorkerCts;
				if (autoAimWorkerCts != null)
				{
					autoAimWorkerCts.Cancel();
				}
			}
			catch
			{
			}
			if (!autoAimWorkerThread.Join(200))
			{
				try
				{
					autoAimWorkerThread.Interrupt();
				}
				catch
				{
				}
			}
			this._autoAimWorkerThread = null;
			CancellationTokenSource autoAimWorkerCts2 = this._autoAimWorkerCts;
			if (autoAimWorkerCts2 != null)
			{
				autoAimWorkerCts2.Dispose();
			}
			this._autoAimWorkerCts = null;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000039DC File Offset: 0x00001BDC
		[NullableContext(2)]
		private void AutoAimWorkerLoop(object state)
		{
			CancellationToken cancellationToken2;
			if (state is CancellationToken)
			{
				CancellationToken cancellationToken = (CancellationToken)state;
				cancellationToken2 = cancellationToken;
			}
			else
			{
				cancellationToken2 = CancellationToken.None;
			}
			CancellationToken cancellationToken3 = cancellationToken2;
			try
			{
				while (!cancellationToken3.IsCancellationRequested)
				{
					ModBehaviour.AutoAimThreadContext autoAimThreadContext = this._autoAimThreadContext;
					bool flag = false;
					ModBehaviour.AutoAimCandidate autoAimCandidate = default(ModBehaviour.AutoAimCandidate);
					float num = float.MaxValue;
					ModBehaviour.AutoAimThreadTarget[] targets = autoAimThreadContext.Targets;
					if (autoAimThreadContext.HasData && targets != null && autoAimThreadContext.TargetCount > 0)
					{
						Vector3 vector = ((autoAimThreadContext.AimRayDirection.sqrMagnitude > 0.0001f) ? autoAimThreadContext.AimRayDirection.normalized : Vector3.forward);
						Vector3 vector2 = ((autoAimThreadContext.Forward.sqrMagnitude > 0.0001f) ? autoAimThreadContext.Forward.normalized : vector);
						float num2 = autoAimThreadContext.MaxRange * autoAimThreadContext.MaxRange;
						for (int i = 0; i < autoAimThreadContext.TargetCount; i++)
						{
							ModBehaviour.AutoAimThreadTarget autoAimThreadTarget = targets[i];
							DamageReceiver receiver = autoAimThreadTarget.Receiver;
							if (!(receiver == null))
							{
								Vector3 aimPoint = autoAimThreadTarget.AimPoint;
								Vector3 vector3 = aimPoint - autoAimThreadContext.MuzzlePosition;
								float sqrMagnitude = vector3.sqrMagnitude;
								if (sqrMagnitude > 0.04f && sqrMagnitude <= num2)
								{
									float num3 = Vector3.Dot(aimPoint - autoAimThreadContext.AimRayOrigin, vector);
									if (num3 > 0f)
									{
										Vector4 vector4 = autoAimThreadContext.ViewProjection * new Vector4(aimPoint.x, aimPoint.y, aimPoint.z, 1f);
										float w = vector4.w;
										if (Mathf.Abs(w) > 0.0001f)
										{
											float num4 = 1f / w;
											float num5 = vector4.x * num4;
											float num6 = vector4.y * num4;
											float num7 = vector4.z * num4;
											if (num7 >= -1f && num7 <= 1f)
											{
												float num8 = (num5 * 0.5f + 0.5f) * autoAimThreadContext.ScreenWidth;
												float num9 = (num6 * 0.5f + 0.5f) * autoAimThreadContext.ScreenHeight;
												float num10 = num8 - autoAimThreadContext.AimScreenPoint.x;
												float num11 = num9 - autoAimThreadContext.AimScreenPoint.y;
												float num12 = Mathf.Sqrt(num10 * num10 + num11 * num11);
												if (num12 <= autoAimThreadContext.ScreenRadius)
												{
													float num13 = Vector3.Angle(vector2, vector3);
													float num14 = num12 + num13 * 2.25f + Mathf.Max(0f, num3) * 0.1f;
													if (num14 < num)
													{
														num = num14;
														autoAimCandidate = new ModBehaviour.AutoAimCandidate(receiver, aimPoint, num12, num3, false);
														flag = true;
													}
												}
											}
										}
									}
								}
							}
						}
					}
					if (flag)
					{
						this._autoAimThreadResult.Candidate = autoAimCandidate;
						this._autoAimThreadResult.HasCandidate = true;
					}
					else
					{
						this._autoAimThreadResult.HasCandidate = false;
					}
					if (cancellationToken3.IsCancellationRequested)
					{
						break;
					}
					Thread.Sleep(20);
				}
			}
			catch (ThreadInterruptedException)
			{
			}
			catch
			{
				this._autoAimThreadResult.HasCandidate = false;
			}
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00003D00 File Offset: 0x00001F00
		private void ClearAutoAimThreadData()
		{
			this._autoAimThreadContext = default(ModBehaviour.AutoAimThreadContext);
			this._autoAimThreadResult = default(ModBehaviour.AutoAimThreadResult);
			this._autoAimThreadTargetBuffer.Clear();
			this.CleanupUnusedHeadshotOffsets(null);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00003D2C File Offset: 0x00001F2C
		private void CleanupRetargetedProjectilesIfNeeded()
		{
			if (Time.unscaledTime < this._nextProjectileCleanupTime)
			{
				return;
			}
			this._nextProjectileCleanupTime = Time.unscaledTime + 0.2f;
			this.CleanupRetargetedProjectiles();
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003D54 File Offset: 0x00001F54
		private static ModBehaviour.GrenadeHomingComponent EnsureGrenadeHomingComponent(Grenade grenade)
		{
			ModBehaviour.GrenadeHomingComponent grenadeHomingComponent = grenade.gameObject.GetComponent<ModBehaviour.GrenadeHomingComponent>();
			if (grenadeHomingComponent == null)
			{
				grenadeHomingComponent = grenade.gameObject.AddComponent<ModBehaviour.GrenadeHomingComponent>();
			}
			return grenadeHomingComponent;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003D84 File Offset: 0x00001F84
		[NullableContext(2)]
		private void TryOverrideTriggerRestrictions(ItemAgent_Gun gun)
		{
			if (!this._movementUnlockEnabled)
			{
				return;
			}
			if (gun == null)
			{
				return;
			}
			CharacterMainControl holder = gun.Holder;
			if (holder == null)
			{
				return;
			}
			this.ForceDisableTriggerTimer(holder, 0f);
			this.TryEnableDashControl(holder);
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003DC8 File Offset: 0x00001FC8
		private static void TrySetDisableTriggerTimer(CharacterMainControl character, float value)
		{
			if (ModBehaviour.DisableTriggerTimerField == null)
			{
				return;
			}
			try
			{
				ModBehaviour.DisableTriggerTimerField.SetValue(character, value);
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00003E0C File Offset: 0x0000200C
		private static void TryRefillStamina(CharacterMainControl character)
		{
			if (ModBehaviour.CurrentStaminaField == null)
			{
				return;
			}
			try
			{
				ModBehaviour.CurrentStaminaField.SetValue(character, character.MaxStamina);
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00003E54 File Offset: 0x00002054
		private static float GetDisableTriggerTimer(CharacterMainControl character)
		{
			if (ModBehaviour.DisableTriggerTimerField == null)
			{
				return 0f;
			}
			try
			{
				object value = ModBehaviour.DisableTriggerTimerField.GetValue(character);
				if (value is float)
				{
					return (float)value;
				}
			}
			catch (Exception)
			{
			}
			return 0f;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00003EB0 File Offset: 0x000020B0
		private void MaintainTriggerAccess(CharacterMainControl character)
		{
			if (!this._movementUnlockEnabled)
			{
				return;
			}
			this.ForceDisableTriggerTimer(character, 0f);
			this.TryEnableDashControl(character);
			ItemAgentHolder agentHolder = character.agentHolder;
			ItemAgent_Gun itemAgent_Gun = ((agentHolder != null) ? agentHolder.CurrentHoldGun : null);
			if (agentHolder == null || itemAgent_Gun == null)
			{
				return;
			}
			InputManager inputManager = ((LevelManager.Instance != null) ? LevelManager.Instance.InputManager : null);
			if (inputManager == null)
			{
				return;
			}
			bool triggerInput = inputManager.TriggerInput;
			bool flag = triggerInput && !this._lastTriggerInput;
			bool flag2 = !triggerInput && this._lastTriggerInput;
			this._lastTriggerInput = triggerInput;
			if (!triggerInput)
			{
				return;
			}
			CharacterActionBase currentAction = character.CurrentAction;
			bool flag3 = currentAction != null && currentAction.Running;
			bool flag4 = flag3 && character.dashAction != null && currentAction == character.dashAction;
			if (!character.Running && ModBehaviour.GetDisableTriggerTimer(character) <= 0f && (!flag3 || flag4))
			{
				return;
			}
			Movement movementControl = character.movementControl;
			if (movementControl != null)
			{
				movementControl.ForceSetAimDirectionToAimPoint();
			}
			agentHolder.SetTrigger(triggerInput, flag, flag2);
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003FE4 File Offset: 0x000021E4
		private void MaintainSpinKill(CharacterMainControl character)
		{
			if (!this._spinKillEnabled)
			{
				this.EnsureSpinKillPenetrationEnabled(false);
				this.HideSpinKillRangeRenderer();
				return;
			}
			this.UpdateSpinKillRotation(character);
			this.EnsureSpinKillPenetrationEnabled(true);
			this.UpdateSpinKillRangeVisualizer(character);
			if (Time.unscaledTime >= this._spinKillNextSearchTime)
			{
				this.RefreshSpinKillCandidates(character);
				this._spinKillNextSearchTime = Time.unscaledTime + 0.05f;
			}
			ModBehaviour.SpinKillCandidateInfo spinKillCandidateInfo;
			if (!this.TrySelectSpinKillCandidate(out spinKillCandidateInfo))
			{
				this.ReleaseSpinKillTrigger(character);
				this._spinKillCurrentTarget = null;
				return;
			}
			this._spinKillCurrentTarget = spinKillCandidateInfo.Receiver;
			Vector3 aimPoint = spinKillCandidateInfo.AimPoint;
			this._spinKillCurrentAimPoint = aimPoint;
			character.SetAimPoint(aimPoint);
			ModBehaviour.AutoAimLog(string.Format("SpinKill target={0}, distance={1:F1}, inAutoAim={2}, screenDist={3:F1}", new object[]
			{
				spinKillCandidateInfo.Receiver.name,
				spinKillCandidateInfo.Distance,
				spinKillCandidateInfo.IsWithinAutoAim,
				spinKillCandidateInfo.ScreenDistance
			}));
			ItemAgentHolder agentHolder = character.agentHolder;
			ItemAgent_Gun itemAgent_Gun = ((agentHolder != null) ? agentHolder.CurrentHoldGun : null);
			bool flag = itemAgent_Gun != null && ModBehaviour.IsSingleShotTriggerMode(itemAgent_Gun);
			this.EnsureSpinKillTrigger(agentHolder, flag);
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00004100 File Offset: 0x00002300
		private bool TryGetSpinKillAutoAimCandidate(CharacterMainControl holder, Camera camera, Vector2 aimScreenPoint, Ray aimRay, Vector3 muzzlePosition, out ModBehaviour.AutoAimCandidate candidate)
		{
			candidate = default(ModBehaviour.AutoAimCandidate);
			if (!this._spinKillEnabled || this._spinKillCurrentTarget == null)
			{
				return false;
			}
			DamageReceiver spinKillCurrentTarget = this._spinKillCurrentTarget;
			if (!ModBehaviour.IsValidSpinKillTarget(spinKillCurrentTarget))
			{
				return false;
			}
			if (!ModBehaviour.IsReceiverInSameScene(holder.gameObject.scene, spinKillCurrentTarget))
			{
				return false;
			}
			ModBehaviour.SpinKillCandidateInfo spinKillCandidateInfo;
			Vector3 vector;
			float num;
			if (this.TryGetSpinKillCandidateInfo(spinKillCurrentTarget, out spinKillCandidateInfo))
			{
				vector = spinKillCandidateInfo.AimPoint;
				num = spinKillCandidateInfo.ScreenDistance;
			}
			else
			{
				vector = this.GetReceiverAimPoint(spinKillCurrentTarget);
				num = float.PositiveInfinity;
			}
			if ((vector - muzzlePosition).sqrMagnitude <= 0.04f)
			{
				return false;
			}
			Vector3 vector2 = ((aimRay.direction.sqrMagnitude > 0.0001f) ? aimRay.direction.normalized : Vector3.forward);
			float num2 = Vector3.Dot(vector - aimRay.origin, vector2);
			if (num2 <= 0f)
			{
				return false;
			}
			if (!float.IsFinite(num))
			{
				Vector3 vector3 = camera.WorldToScreenPoint(vector);
				if (vector3.z > 0f)
				{
					num = Vector2.Distance(new Vector2(vector3.x, vector3.y), aimScreenPoint);
				}
				else
				{
					num = float.PositiveInfinity;
				}
			}
			candidate = new ModBehaviour.AutoAimCandidate(spinKillCurrentTarget, vector, num, num2, true);
			return true;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x0000423C File Offset: 0x0000243C
		private void EnsureSpinKillPenetrationEnabled(bool active)
		{
			if (active)
			{
				if (!this._spinKillForcedPenetration)
				{
					this._spinKillPreviousPenetrationEnabled = this._obstaclePenetrationEnabled;
				}
				if (!this._obstaclePenetrationEnabled)
				{
					this._spinKillForcedPenetration = true;
					this.SetObstaclePenetrationEnabled(true, false);
					return;
				}
			}
			else if (this._spinKillForcedPenetration)
			{
				this._spinKillForcedPenetration = false;
				if (!this._spinKillPreviousPenetrationEnabled)
				{
					this.SetObstaclePenetrationEnabled(false, false);
				}
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00004297 File Offset: 0x00002497
		private void UpdateSpinKillRangeVisualizer(CharacterMainControl character)
		{
			this.EnsureSpinKillRangeRenderer(character);
			this.UpdateSpinKillRangeRendererRadius();
		}

		// Token: 0x0600003B RID: 59 RVA: 0x000042A8 File Offset: 0x000024A8
		private void EnsureSpinKillRangeRenderer(CharacterMainControl character)
		{
			if (this._spinKillRangeObject == null)
			{
				this._spinKillRangeObject = new GameObject("SpinKillRangeIndicator")
				{
					hideFlags = HideFlags.DontSave
				};
				Transform transform = this._spinKillRangeObject.transform;
				transform.SetParent(character.transform, false);
				transform.localPosition = Vector3.zero;
				this._spinKillRangeRenderer = this._spinKillRangeObject.AddComponent<LineRenderer>();
				LineRenderer spinKillRangeRenderer = this._spinKillRangeRenderer;
				spinKillRangeRenderer.loop = true;
				spinKillRangeRenderer.useWorldSpace = false;
				spinKillRangeRenderer.textureMode = LineTextureMode.Stretch;
				spinKillRangeRenderer.shadowCastingMode = ShadowCastingMode.Off;
				spinKillRangeRenderer.receiveShadows = false;
				spinKillRangeRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
				spinKillRangeRenderer.lightProbeUsage = LightProbeUsage.Off;
				spinKillRangeRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
				spinKillRangeRenderer.widthMultiplier = 0.15f;
				spinKillRangeRenderer.positionCount = 96;
				if (this._spinKillRangeMaterial == null)
				{
					Shader shader = Shader.Find("Sprites/Default");
					if (shader == null)
					{
						shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
					}
					if (shader == null)
					{
						shader = Shader.Find("Hidden/Internal-Colored");
					}
					if (shader == null)
					{
						Debug.LogWarning("[CheatMenu] Failed to locate shader for spin kill range indicator; skipping visualization.");
					}
					else
					{
						this._spinKillRangeMaterial = new Material(shader)
						{
							hideFlags = HideFlags.DontSave,
							color = new Color(1f, 0.2f, 0.2f, 0.4f)
						};
					}
				}
				if (this._spinKillRangeMaterial != null)
				{
					spinKillRangeRenderer.material = this._spinKillRangeMaterial;
				}
				spinKillRangeRenderer.startColor = new Color(1f, 0.25f, 0.25f, 0.6f);
				spinKillRangeRenderer.endColor = new Color(1f, 0.25f, 0.25f, 0.2f);
				return;
			}
			Transform transform2 = this._spinKillRangeObject.transform;
			if (transform2.parent != character.transform)
			{
				transform2.SetParent(character.transform, false);
			}
			transform2.localPosition = Vector3.zero;
			transform2.localRotation = Quaternion.identity;
			transform2.localScale = Vector3.one;
			if (this._spinKillRangeRenderer != null && !this._spinKillRangeRenderer.enabled)
			{
				this._spinKillRangeRenderer.enabled = true;
			}
		}

		// Token: 0x0600003C RID: 60 RVA: 0x000044BC File Offset: 0x000026BC
		private void UpdateSpinKillRangeRendererRadius()
		{
			LineRenderer spinKillRangeRenderer = this._spinKillRangeRenderer;
			if (spinKillRangeRenderer == null)
			{
				return;
			}
			float spinKillSearchRadius = this._spinKillSearchRadius;
			int num = 96;
			float num2 = 6.28318548f / (float)num;
			for (int i = 0; i < num; i++)
			{
				float num3 = num2 * (float)i;
				float num4 = Mathf.Cos(num3) * spinKillSearchRadius;
				float num5 = Mathf.Sin(num3) * spinKillSearchRadius;
				this._spinKillRangePoints[i] = new Vector3(num4, 0.05f, num5);
			}
			if (spinKillRangeRenderer.positionCount != num)
			{
				spinKillRangeRenderer.positionCount = num;
			}
			spinKillRangeRenderer.SetPositions(this._spinKillRangePoints);
		}

		// Token: 0x0600003D RID: 61 RVA: 0x0000454C File Offset: 0x0000274C
		private void HideSpinKillRangeRenderer()
		{
			if (this._spinKillRangeRenderer != null)
			{
				this._spinKillRangeRenderer.enabled = false;
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00004568 File Offset: 0x00002768
		private void DestroySpinKillRangeRenderer()
		{
			if (this._spinKillRangeObject != null)
			{
				Object.Destroy(this._spinKillRangeObject);
			}
			if (this._spinKillRangeMaterial != null)
			{
				Object.Destroy(this._spinKillRangeMaterial);
				this._spinKillRangeMaterial = null;
			}
			this._spinKillRangeRenderer = null;
			this._spinKillRangeObject = null;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000045BC File Offset: 0x000027BC
		private void UpdateSpinKillRotation(CharacterMainControl character)
		{
			Movement movementControl = character.movementControl;
			if (movementControl == null)
			{
				return;
			}
			this._spinKillSpinAngle += this._spinKillAngularSpeed * Time.unscaledDeltaTime;
			if (this._spinKillSpinAngle >= 360f)
			{
				this._spinKillSpinAngle -= 360f;
			}
			else if (this._spinKillSpinAngle < 0f)
			{
				this._spinKillSpinAngle += 360f;
			}
			float num = this._spinKillSpinAngle * 0.0174532924f;
			Vector3 vector = new Vector3(Mathf.Cos(num), 0f, Mathf.Sin(num));
			if (vector.sqrMagnitude < 0.001f)
			{
				return;
			}
			movementControl.ForceTurnTo(vector);
		}

		// Token: 0x06000040 RID: 64 RVA: 0x00004670 File Offset: 0x00002870
		private void RefreshSpinKillCandidates(CharacterMainControl character)
		{
			Vector3 position = character.transform.position;
			Scene scene = character.gameObject.scene;
			this.EnsureSpinKillColliderBufferCapacity(32);
			int num;
			for (;;)
			{
				num = Physics.OverlapSphereNonAlloc(position, this._spinKillSearchRadius, this._spinKillColliderBuffer, this._damageReceiverLayerMask, QueryTriggerInteraction.Collide);
				if (num < this._spinKillColliderBuffer.Length)
				{
					break;
				}
				int num2 = Mathf.Max(this._spinKillColliderBuffer.Length * 2, this._spinKillColliderBuffer.Length + 32);
				this.EnsureSpinKillColliderBufferCapacity(num2);
			}
			this._spinKillCandidates.Clear();
			Camera camera = null;
			LevelManager instance = LevelManager.Instance;
			if (LevelManager.LevelInited && instance != null && instance.GameCamera != null)
			{
				camera = instance.GameCamera.renderCamera;
			}
			Vector2 latestAimScreenPoint = this._latestAimScreenPoint;
			float autoAimScreenRadius = this._autoAimScreenRadius;
			int num3 = Mathf.Min(num, this._spinKillColliderBuffer.Length);
			for (int i = 0; i < num3; i++)
			{
				Collider collider = this._spinKillColliderBuffer[i];
				this._spinKillColliderBuffer[i] = null;
				DamageReceiver damageReceiver;
				if (!(collider == null) && collider.TryGetComponent<DamageReceiver>(out damageReceiver) && this.IsSpinKillCandidateValid(character.Team, position, scene, damageReceiver))
				{
					Vector3 receiverAimPoint = this.GetReceiverAimPoint(damageReceiver);
					float num4 = Vector3.Distance(position, receiverAimPoint);
					bool flag = false;
					float num5 = float.PositiveInfinity;
					if (camera != null)
					{
						Vector3 vector = camera.WorldToScreenPoint(receiverAimPoint);
						if (vector.z > 0f)
						{
							num5 = Vector2.Distance(new Vector2(vector.x, vector.y), latestAimScreenPoint);
							flag = num5 <= autoAimScreenRadius;
						}
					}
					ModBehaviour.SpinKillCandidateInfo spinKillCandidateInfo = new ModBehaviour.SpinKillCandidateInfo
					{
						Receiver = damageReceiver,
						AimPoint = receiverAimPoint,
						Distance = num4,
						IsWithinAutoAim = flag,
						ScreenDistance = num5
					};
					bool flag2 = false;
					int j = 0;
					while (j < this._spinKillCandidates.Count)
					{
						if (this._spinKillCandidates[j].Receiver == damageReceiver)
						{
							flag2 = true;
							if (ModBehaviour.ShouldReplaceSpinKillCandidate(this._spinKillCandidates[j], spinKillCandidateInfo))
							{
								this._spinKillCandidates[j] = spinKillCandidateInfo;
								break;
							}
							break;
						}
						else
						{
							j++;
						}
					}
					if (!flag2)
					{
						this._spinKillCandidates.Add(spinKillCandidateInfo);
					}
				}
			}
			if (this._spinKillCandidates.Count > 1)
			{
				this._spinKillCandidates.Sort(new Comparison<ModBehaviour.SpinKillCandidateInfo>(ModBehaviour.CompareSpinKillCandidates));
			}
		}

		// Token: 0x06000041 RID: 65 RVA: 0x000048E4 File Offset: 0x00002AE4
		private bool TrySelectSpinKillCandidate(out ModBehaviour.SpinKillCandidateInfo candidate)
		{
			candidate = default(ModBehaviour.SpinKillCandidateInfo);
			bool flag = false;
			ModBehaviour.SpinKillCandidateInfo spinKillCandidateInfo = default(ModBehaviour.SpinKillCandidateInfo);
			bool flag2 = false;
			ModBehaviour.SpinKillCandidateInfo spinKillCandidateInfo2 = default(ModBehaviour.SpinKillCandidateInfo);
			for (int i = 0; i < this._spinKillCandidates.Count; i++)
			{
				ModBehaviour.SpinKillCandidateInfo spinKillCandidateInfo3 = this._spinKillCandidates[i];
				if (ModBehaviour.IsValidSpinKillTarget(spinKillCandidateInfo3.Receiver))
				{
					if (spinKillCandidateInfo3.IsWithinAutoAim && (!flag || spinKillCandidateInfo3.ScreenDistance < spinKillCandidateInfo.ScreenDistance - 0.001f || (Mathf.Abs(spinKillCandidateInfo3.ScreenDistance - spinKillCandidateInfo.ScreenDistance) <= 0.001f && spinKillCandidateInfo3.Distance < spinKillCandidateInfo.Distance)))
					{
						spinKillCandidateInfo = spinKillCandidateInfo3;
						flag = true;
					}
					if (!flag2 || spinKillCandidateInfo3.Distance < spinKillCandidateInfo2.Distance)
					{
						spinKillCandidateInfo2 = spinKillCandidateInfo3;
						flag2 = true;
					}
				}
			}
			if (flag)
			{
				candidate = spinKillCandidateInfo;
				return true;
			}
			if (flag2)
			{
				candidate = spinKillCandidateInfo2;
				return true;
			}
			return false;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x000049C8 File Offset: 0x00002BC8
		private bool TryGetSpinKillCandidateInfo(DamageReceiver receiver, out ModBehaviour.SpinKillCandidateInfo candidate)
		{
			for (int i = 0; i < this._spinKillCandidates.Count; i++)
			{
				if (this._spinKillCandidates[i].Receiver == receiver)
				{
					candidate = this._spinKillCandidates[i];
					return true;
				}
			}
			candidate = default(ModBehaviour.SpinKillCandidateInfo);
			return false;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00004A20 File Offset: 0x00002C20
		private static bool ShouldReplaceSpinKillCandidate(ModBehaviour.SpinKillCandidateInfo existing, ModBehaviour.SpinKillCandidateInfo updated)
		{
			if (updated.IsWithinAutoAim && !existing.IsWithinAutoAim)
			{
				return true;
			}
			if (!updated.IsWithinAutoAim && existing.IsWithinAutoAim)
			{
				return false;
			}
			if (updated.IsWithinAutoAim && existing.IsWithinAutoAim)
			{
				if (updated.ScreenDistance < existing.ScreenDistance - 0.001f)
				{
					return true;
				}
				if (existing.ScreenDistance < updated.ScreenDistance - 0.001f)
				{
					return false;
				}
			}
			return updated.Distance < existing.Distance - 0.001f;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00004AA4 File Offset: 0x00002CA4
		private static int CompareSpinKillCandidates(ModBehaviour.SpinKillCandidateInfo a, ModBehaviour.SpinKillCandidateInfo b)
		{
			if (a.IsWithinAutoAim == b.IsWithinAutoAim)
			{
				if (a.IsWithinAutoAim && b.IsWithinAutoAim)
				{
					int num = a.ScreenDistance.CompareTo(b.ScreenDistance);
					if (num != 0)
					{
						return num;
					}
				}
				return a.Distance.CompareTo(b.Distance);
			}
			if (!a.IsWithinAutoAim)
			{
				return 1;
			}
			return -1;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00004B08 File Offset: 0x00002D08
		private bool IsSpinKillCandidateValid(Teams playerTeam, Vector3 origin, Scene characterScene, DamageReceiver receiver)
		{
			if (!ModBehaviour.IsValidSpinKillTarget(receiver))
			{
				return false;
			}
			if (!ModBehaviour.IsReceiverInSameScene(characterScene, receiver))
			{
				return false;
			}
			if (!Team.IsEnemy(playerTeam, receiver.Team))
			{
				return false;
			}
			Vector3 receiverAimPoint = this.GetReceiverAimPoint(receiver);
			return Vector3.Distance(origin, receiverAimPoint) <= this._spinKillSearchRadius;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00004B58 File Offset: 0x00002D58
		[NullableContext(2)]
		private static bool IsValidSpinKillTarget(DamageReceiver receiver)
		{
			return !(receiver == null) && receiver.enabled && !(receiver.gameObject == null) && receiver.gameObject.activeInHierarchy && !receiver.IsDead && !receiver.IsMainCharacter && ModBehaviour.ReceiverHasHealth(receiver);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00004BB0 File Offset: 0x00002DB0
		private static bool IsReceiverInSameScene(Scene characterScene, DamageReceiver receiver)
		{
			if (!characterScene.IsValid())
			{
				return true;
			}
			if (receiver == null || receiver.gameObject == null)
			{
				return false;
			}
			Scene scene = receiver.gameObject.scene;
			return !scene.IsValid() || scene == characterScene;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00004C00 File Offset: 0x00002E00
		private void EnsureSpinKillColliderBufferCapacity(int minimum)
		{
			if (this._spinKillColliderBuffer.Length >= minimum)
			{
				return;
			}
			int i;
			for (i = ((this._spinKillColliderBuffer.Length == 0) ? 32 : this._spinKillColliderBuffer.Length); i < minimum; i *= 2)
			{
			}
			Array.Resize<Collider>(ref this._spinKillColliderBuffer, i);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00004C48 File Offset: 0x00002E48
		[NullableContext(2)]
		private void EnsureSpinKillTrigger(ItemAgentHolder holder)
		{
			if (holder == null || holder.CurrentHoldGun == null)
			{
				this.ReleaseSpinKillTrigger(null);
				return;
			}
			ItemAgent_Gun currentHoldGun = holder.CurrentHoldGun;
			bool flag = currentHoldGun != null && ModBehaviour.IsSingleShotTriggerMode(currentHoldGun);
			this.EnsureSpinKillTrigger(holder, flag);
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00004C98 File Offset: 0x00002E98
		[NullableContext(2)]
		private void ReleaseSpinKillTrigger(CharacterMainControl character = null)
		{
			if (!this._spinKillTriggerHeld)
			{
				return;
			}
			CharacterMainControl characterMainControl = character ?? this._trackedCharacter;
			ItemAgentHolder itemAgentHolder = ((characterMainControl != null) ? characterMainControl.agentHolder : null);
			if (itemAgentHolder != null)
			{
				itemAgentHolder.SetTrigger(false, false, true);
			}
			this._spinKillTriggerHeld = false;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00004CE4 File Offset: 0x00002EE4
		private static bool IsSingleShotTriggerMode(ItemAgent_Gun gun)
		{
			if (gun == null)
			{
				return false;
			}
			ItemSetting_Gun gunItemSetting = gun.GunItemSetting;
			if (gunItemSetting == null)
			{
				return false;
			}
			ItemSetting_Gun.TriggerModes triggerMode = gunItemSetting.triggerMode;
			return triggerMode == ItemSetting_Gun.TriggerModes.semi || triggerMode == ItemSetting_Gun.TriggerModes.bolt;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x00004D20 File Offset: 0x00002F20
		[NullableContext(2)]
		private void EnsureSpinKillTrigger(ItemAgentHolder holder, bool singleShot)
		{
			if (holder == null || holder.CurrentHoldGun == null)
			{
				this.ReleaseSpinKillTrigger(null);
				return;
			}
			ItemAgent_Gun currentHoldGun = holder.CurrentHoldGun;
			if (!singleShot)
			{
				bool flag = !this._spinKillTriggerHeld;
				holder.SetTrigger(true, flag, false);
				this._spinKillTriggerHeld = true;
				return;
			}
			if (!this.TryForceSingleShotFire(currentHoldGun))
			{
				bool flag2 = !this._spinKillTriggerHeld;
				holder.SetTrigger(true, flag2, false);
				this._spinKillTriggerHeld = true;
				return;
			}
			this._spinKillTriggerHeld = false;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00004D9C File Offset: 0x00002F9C
		private bool TryForceSingleShotFire(ItemAgent_Gun gun)
		{
			if (gun == null || ModBehaviour.GunTransToFireMethod == null || ModBehaviour.GunStateField == null)
			{
				return false;
			}
			if (this._spinKillLastManualFireFrame == Time.frameCount)
			{
				return false;
			}
			if (!this.IsGunReadyForManualFire(gun))
			{
				return false;
			}
			bool flag;
			try
			{
				ModBehaviour.GunTransToFireMethod.Invoke(gun, ModBehaviour.GunTransToFireArgs);
				this._spinKillLastManualFireFrame = Time.frameCount;
				flag = true;
			}
			catch (Exception)
			{
				flag = false;
			}
			return flag;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00004E20 File Offset: 0x00003020
		private bool IsGunReadyForManualFire(ItemAgent_Gun gun)
		{
			if (gun == null)
			{
				return false;
			}
			if (ModBehaviour.GunStateField != null)
			{
				try
				{
					object value = ModBehaviour.GunStateField.GetValue(gun);
					if (value is ItemAgent_Gun.GunStates)
					{
						ItemAgent_Gun.GunStates gunStates = (ItemAgent_Gun.GunStates)value;
						if (gunStates != ItemAgent_Gun.GunStates.ready)
						{
							return false;
						}
					}
					else if (value is int)
					{
						int num = (int)value;
						if (num != 1)
						{
							return false;
						}
					}
				}
				catch (Exception)
				{
				}
			}
			return !gun.IsReloading() && gun.BulletCount > 0;
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00004EAC File Offset: 0x000030AC
		private void ResetSpinKillState()
		{
			this.EnsureSpinKillPenetrationEnabled(false);
			this.HideSpinKillRangeRenderer();
			this._spinKillCurrentTarget = null;
			this._spinKillCandidates.Clear();
			this._spinKillSpinAngle = 0f;
			this._spinKillNextSearchTime = 0f;
			this._spinKillCurrentAimPoint = Vector3.zero;
			this._spinKillForcedPenetration = false;
			this._spinKillTriggerHeld = false;
			this._spinKillLastManualFireFrame = -1;
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00004F10 File Offset: 0x00003110
		private void SetSpinKillEnabled(bool enabled, bool persist = true, bool force = false)
		{
			if (!force && this._spinKillEnabled == enabled)
			{
				return;
			}
			this._spinKillEnabled = enabled;
			if (!enabled)
			{
				this.ReleaseSpinKillTrigger(null);
				this.ResetSpinKillState();
				this.EnsureSpinKillPenetrationEnabled(false);
			}
			else
			{
				this._spinKillNextSearchTime = 0f;
				this._spinKillSpinAngle = 0f;
				this.EnsureSpinKillPenetrationEnabled(true);
			}
			if (persist)
			{
				this.SaveSettings();
			}
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00004F70 File Offset: 0x00003170
		[NullableContext(2)]
		private void MaintainHeldItemDurability(CharacterMainControl character)
		{
			if (!this._infiniteDurabilityEnabled || character == null)
			{
				return;
			}
			ItemAgentHolder agentHolder = character.agentHolder;
			DuckovItemAgent duckovItemAgent = ((agentHolder != null) ? agentHolder.CurrentHoldItemAgent : null);
			Item item = ((duckovItemAgent != null) ? duckovItemAgent.Item : null);
			if (item == null)
			{
				return;
			}
			this.EnsureItemDurability(item);
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00004FD0 File Offset: 0x000031D0
		private void EnsureItemDurability(Item item)
		{
			if (item == null || !item.UseDurability)
			{
				return;
			}
			float maxDurability = item.MaxDurability;
			if (maxDurability <= 0f)
			{
				return;
			}
			if (item.DurabilityLoss > 0f)
			{
				item.DurabilityLoss = 0f;
			}
			if (item.Durability < maxDurability || item.Durability > maxDurability)
			{
				item.Durability = maxDurability;
			}
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00005030 File Offset: 0x00003230
		[NullableContext(2)]
		private void MaintainSurvivalNeeds(CharacterMainControl character)
		{
			if (character == null)
			{
				return;
			}
			if (this._noSurvivalNeedsEnabled)
			{
				try
				{
					float num = Mathf.Max(character.MaxEnergy, 0f);
					if (num > 0f && character.CurrentEnergy < num)
					{
						character.CurrentEnergy = num;
					}
					float num2 = Mathf.Max(character.MaxWater, 0f);
					if (num2 > 0f && character.CurrentWater < num2)
					{
						character.CurrentWater = num2;
					}
					ModBehaviour.TryClearSurvivalFlags(character);
					character.RemoveBuffsByTag(Buff.BuffExclusiveTags.Starve, false);
					character.RemoveBuffsByTag(Buff.BuffExclusiveTags.Thirsty, false);
				}
				catch (Exception)
				{
				}
				return;
			}
			if (!this._invincibilityEnabled)
			{
				return;
			}
			Health health = character.Health;
			if (!(health == null))
			{
				bool invincible = health.Invincible;
			}
		}

		// Token: 0x06000054 RID: 84 RVA: 0x000050F0 File Offset: 0x000032F0
		private void AllowSurvivalNeedsDecayWhileInvincible(CharacterMainControl character)
		{
			LevelManager instance = LevelManager.Instance;
			if (instance == null || !LevelManager.LevelInited || !instance.IsRaidMap)
			{
				return;
			}
			float deltaTime = Time.deltaTime;
			if (deltaTime <= 0f)
			{
				return;
			}
			float num = Mathf.Max(0f, character.EnergyCostPerMin);
			if (num > 0f)
			{
				float num2 = num / 60f * deltaTime;
				float num3 = Mathf.Max(0f, character.CurrentEnergy - num2);
				if (!Mathf.Approximately(num3, character.CurrentEnergy))
				{
					character.CurrentEnergy = num3;
				}
			}
			float num4 = Mathf.Max(0f, character.WaterCostPerMin);
			if (num4 > 0f)
			{
				float num5 = num4 / 60f * deltaTime;
				float num6 = Mathf.Max(0f, character.CurrentWater - num5);
				if (!Mathf.Approximately(num6, character.CurrentWater))
				{
					character.CurrentWater = num6;
				}
			}
			character.UpdateThirstyAndStarve();
		}

		// Token: 0x06000055 RID: 85 RVA: 0x000051D1 File Offset: 0x000033D1
		[NullableContext(2)]
		private void MaintainInfiniteStamina(CharacterMainControl character)
		{
			if (!this._infiniteStaminaEnabled || character == null)
			{
				return;
			}
			ModBehaviour.TryRefillStamina(character);
		}

		// Token: 0x06000056 RID: 86 RVA: 0x000051EC File Offset: 0x000033EC
		private static void TryClearSurvivalFlags(CharacterMainControl character)
		{
			try
			{
				if (ModBehaviour.CharacterThirstyField != null)
				{
					ModBehaviour.CharacterThirstyField.SetValue(character, false);
				}
				if (ModBehaviour.CharacterStarveField != null)
				{
					ModBehaviour.CharacterStarveField.SetValue(character, false);
				}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x06000057 RID: 87 RVA: 0x0000524C File Offset: 0x0000344C
		private void TryEnableDashControl(CharacterMainControl character)
		{
			if (!this._movementUnlockEnabled)
			{
				return;
			}
			if (character == null || character.CharacterItem == null)
			{
				return;
			}
			this.EnsureMovementUnlockSnapshot(character);
			this.TryRemoveDashCooldown(character);
			if (character.DashCanControl)
			{
				return;
			}
			try
			{
				character.DashCanControl = true;
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x06000058 RID: 88 RVA: 0x000052B0 File Offset: 0x000034B0
		private void TryRemoveDashCooldown(CharacterMainControl character)
		{
			if (!this._movementUnlockEnabled)
			{
				return;
			}
			if (character == null)
			{
				return;
			}
			CA_Dash dashAction = character.dashAction;
			if (dashAction == null)
			{
				return;
			}
			this.EnsureMovementUnlockSnapshot(character);
			if (Mathf.Abs(dashAction.coolTime) <= 0.0001f)
			{
				return;
			}
			dashAction.coolTime = 0f;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00005308 File Offset: 0x00003508
		private void EnsureMovementUnlockSnapshot(CharacterMainControl character)
		{
			if (!this._dashControlOriginalStates.ContainsKey(character))
			{
				this._dashControlOriginalStates[character] = character.DashCanControl;
			}
			if (!this._disableTriggerOriginalValues.ContainsKey(character))
			{
				this._disableTriggerOriginalValues[character] = ModBehaviour.GetDisableTriggerTimer(character);
			}
			CA_Dash dashAction = character.dashAction;
			if (dashAction != null && !this._dashCooldownOriginalValues.ContainsKey(dashAction))
			{
				this._dashCooldownOriginalValues[dashAction] = dashAction.coolTime;
			}
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00005385 File Offset: 0x00003585
		private void ForceDisableTriggerTimer(CharacterMainControl character, float value)
		{
			if (!this._movementUnlockEnabled)
			{
				return;
			}
			this.EnsureMovementUnlockSnapshot(character);
			ModBehaviour.TrySetDisableTriggerTimer(character, value);
		}

		// Token: 0x0600005B RID: 91 RVA: 0x0000539E File Offset: 0x0000359E
		private void ForceRestoreDisableTriggerTimer(CharacterMainControl character, float originalValue)
		{
			ModBehaviour.TrySetDisableTriggerTimer(character, originalValue);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x000053A8 File Offset: 0x000035A8
		private void RestoreMovementUnlockEffects()
		{
			if (this._dashControlOriginalStates.Count == 0 && this._disableTriggerOriginalValues.Count == 0 && this._dashCooldownOriginalValues.Count == 0)
			{
				return;
			}
			foreach (KeyValuePair<CharacterMainControl, bool> keyValuePair in this._dashControlOriginalStates.ToArray<KeyValuePair<CharacterMainControl, bool>>())
			{
				CharacterMainControl key = keyValuePair.Key;
				if (!(key == null))
				{
					try
					{
						key.DashCanControl = keyValuePair.Value;
					}
					catch (Exception)
					{
					}
				}
			}
			this._dashControlOriginalStates.Clear();
			foreach (KeyValuePair<CharacterMainControl, float> keyValuePair2 in this._disableTriggerOriginalValues.ToArray<KeyValuePair<CharacterMainControl, float>>())
			{
				CharacterMainControl key2 = keyValuePair2.Key;
				if (!(key2 == null))
				{
					this.ForceRestoreDisableTriggerTimer(key2, keyValuePair2.Value);
				}
			}
			this._disableTriggerOriginalValues.Clear();
			foreach (KeyValuePair<CA_Dash, float> keyValuePair3 in this._dashCooldownOriginalValues.ToArray<KeyValuePair<CA_Dash, float>>())
			{
				CA_Dash key3 = keyValuePair3.Key;
				if (!(key3 == null))
				{
					key3.coolTime = keyValuePair3.Value;
				}
			}
			this._dashCooldownOriginalValues.Clear();
		}

		// Token: 0x0600005D RID: 93 RVA: 0x000054E4 File Offset: 0x000036E4
		private void HandleOverlayInput()
		{
			bool flag = ModBehaviour.IsControlModifierActive();
			if (flag && Input.GetKeyDown(KeyCode.F8))
			{
				this.SetMenuVisible(!this._menuVisible);
			}
			if (flag && Input.GetKeyDown(KeyCode.F9))
			{
				this.SetRangeVisible(!this._rangeVisible);
			}
			if (flag && Input.GetKeyDown(KeyCode.F1))
			{
				this.SetAutoAimEnabled(!this._autoAimEnabled);
			}
			if (flag && Input.GetKeyDown(KeyCode.F2))
			{
				this.SetNoRecoilEnabled(!this._noRecoilEnabled);
			}
			if (flag && Input.GetKeyDown(KeyCode.F3))
			{
				this.SetInfiniteAmmoEnabled(!this._infiniteAmmoEnabled);
			}
			if (flag && Input.GetKeyDown(KeyCode.F4))
			{
				this.SetMovementUnlockEnabled(!this._movementUnlockEnabled);
			}
			if (flag && Input.GetKeyDown(KeyCode.F5))
			{
				this.SetObstaclePenetrationEnabled(!this._obstaclePenetrationEnabled, true);
			}
			if (flag && Input.GetKeyDown(KeyCode.F6))
			{
				this.SetInvincibilityEnabled(!this._invincibilityEnabled);
			}
			if (flag && Input.GetKeyDown(KeyCode.F7))
			{
				this.SetInfiniteDurabilityEnabled(!this._infiniteDurabilityEnabled);
			}
			if (flag && Input.GetKeyDown(KeyCode.F10))
			{
				this.SetNoSurvivalNeedsEnabled(!this._noSurvivalNeedsEnabled);
			}
			if (flag && Input.GetKeyDown(KeyCode.F11))
			{
				this.SetSpinKillEnabled(!this._spinKillEnabled, true, false);
			}
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00005642 File Offset: 0x00003842
		private static bool IsControlModifierActive()
		{
			return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x0000565C File Offset: 0x0000385C
		private void SetMenuVisible(bool visible)
		{
			if (this._menuVisible == visible)
			{
				return;
			}
			this._menuVisible = visible;
			if (visible)
			{
				this.SetMenuVisibility(true);
			}
			else
			{
				this.SetMenuVisibility(false);
			}
			this.SaveSettings();
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00005688 File Offset: 0x00003888
		private void SetRangeVisible(bool visible)
		{
			if (this._rangeVisible == visible)
			{
				return;
			}
			this._rangeVisible = visible;
			if (!visible)
			{
				this.HideAutoAimRange();
			}
			this.SaveSettings();
		}

		// Token: 0x06000061 RID: 97 RVA: 0x000056AA File Offset: 0x000038AA
		private void SetAutoAimEnabled(bool enabled)
		{
			if (this._autoAimEnabled == enabled)
			{
				return;
			}
			this._autoAimEnabled = enabled;
			if (!enabled)
			{
				this.HideAutoAimRange();
				this.ClearAutoAimThreadData();
				this.SetPenetrationActive(false);
				this.DisableAllGrenadeHoming();
			}
			this._nextAutoAimThreadStateUpdateTime = 0f;
			this.SaveSettings();
		}

		// Token: 0x06000062 RID: 98 RVA: 0x000056EA File Offset: 0x000038EA
		private void SetGrenadeAutoAimEnabled(bool enabled)
		{
			if (this._grenadeAutoAimEnabled == enabled)
			{
				return;
			}
			this._grenadeAutoAimEnabled = enabled;
			if (!enabled)
			{
				this.DisableAllGrenadeHoming();
			}
			this.SaveSettings();
		}

		// Token: 0x06000063 RID: 99 RVA: 0x0000570C File Offset: 0x0000390C
		private void DisableAllGrenadeHoming()
		{
			if (this._activeGrenades.Count > 0)
			{
				for (int i = this._activeGrenades.Count - 1; i >= 0; i--)
				{
					Grenade grenade = this._activeGrenades[i];
					if (!(grenade == null))
					{
						ModBehaviour.GrenadeHomingComponent component = grenade.GetComponent<ModBehaviour.GrenadeHomingComponent>();
						if (component != null)
						{
							Object.Destroy(component);
						}
					}
				}
			}
			this._registeredGrenades.Clear();
			this._grenadeSnapshotBuffer.Clear();
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00005781 File Offset: 0x00003981
		private void SetNoRecoilEnabled(bool enabled)
		{
			if (this._noRecoilEnabled == enabled)
			{
				return;
			}
			this._noRecoilEnabled = enabled;
			if (this._noRecoilEnabled)
			{
				this.TryApplyNoRecoil();
			}
			else
			{
				this.RestoreOriginalCharacterRecoil();
				this.RestoreItemStatOverrides();
			}
			this.SaveSettings();
		}

		// Token: 0x06000065 RID: 101 RVA: 0x000057B6 File Offset: 0x000039B6
		private void SetInfiniteAmmoEnabled(bool enabled)
		{
			if (this._infiniteAmmoEnabled == enabled)
			{
				return;
			}
			this._infiniteAmmoEnabled = enabled;
			this.SaveSettings();
		}

		// Token: 0x06000066 RID: 102 RVA: 0x000057CF File Offset: 0x000039CF
		private void SetInfiniteThrowablesEnabled(bool enabled)
		{
			if (this._infiniteThrowablesEnabled == enabled)
			{
				return;
			}
			this._infiniteThrowablesEnabled = enabled;
			if (this._infiniteThrowablesEnabled)
			{
				this.ReapplyThrowableConsumption();
			}
			else
			{
				this.RestoreThrowableConsumption();
			}
			this.SaveSettings();
		}

		// Token: 0x06000067 RID: 103 RVA: 0x000057FE File Offset: 0x000039FE
		private void SetInfiniteStaminaEnabled(bool enabled)
		{
			if (this._infiniteStaminaEnabled == enabled)
			{
				return;
			}
			this._infiniteStaminaEnabled = enabled;
			this.SaveSettings();
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00005818 File Offset: 0x00003A18
		private void SetMovementUnlockEnabled(bool enabled)
		{
			if (this._movementUnlockEnabled == enabled)
			{
				return;
			}
			this._movementUnlockEnabled = enabled;
			if (this._movementUnlockEnabled)
			{
				if (this._trackedCharacter != null)
				{
					this.TryEnableDashControl(this._trackedCharacter);
				}
			}
			else
			{
				this.RestoreMovementUnlockEffects();
			}
			this.SaveSettings();
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00005866 File Offset: 0x00003A66
		private void SetInfiniteDurabilityEnabled(bool enabled)
		{
			if (this._infiniteDurabilityEnabled == enabled)
			{
				return;
			}
			this._infiniteDurabilityEnabled = enabled;
			if (this._infiniteDurabilityEnabled && this._trackedCharacter != null)
			{
				this.MaintainHeldItemDurability(this._trackedCharacter);
			}
			this.SaveSettings();
		}

		// Token: 0x0600006A RID: 106 RVA: 0x000058A1 File Offset: 0x00003AA1
		private void SetNoSurvivalNeedsEnabled(bool enabled)
		{
			if (this._noSurvivalNeedsEnabled == enabled)
			{
				return;
			}
			this._noSurvivalNeedsEnabled = enabled;
			if (this._noSurvivalNeedsEnabled && this._trackedCharacter != null)
			{
				this.MaintainSurvivalNeeds(this._trackedCharacter);
			}
			this.SaveSettings();
		}

		// Token: 0x0600006B RID: 107 RVA: 0x000058DC File Offset: 0x00003ADC
		private void SetObstaclePenetrationEnabled(bool enabled, bool persist = true)
		{
			if (this._obstaclePenetrationEnabled == enabled)
			{
				return;
			}
			this._obstaclePenetrationEnabled = enabled;
			if (persist)
			{
				this.SaveSettings();
			}
		}

		// Token: 0x0600006C RID: 108 RVA: 0x000058F8 File Offset: 0x00003AF8
		private void SetInvincibilityEnabled(bool enabled)
		{
			if (this._invincibilityEnabled == enabled)
			{
				return;
			}
			this._invincibilityEnabled = enabled;
			if (this._invincibilityEnabled)
			{
				this.ApplyInvincibility(this._trackedCharacter);
			}
			else
			{
				this.RestoreCharacterInvincibility();
			}
			this.SaveSettings();
		}

		// Token: 0x0600006D RID: 109 RVA: 0x0000592D File Offset: 0x00003B2D
		private void SetRangeMultiplierFromSlider(float value)
		{
			this.HandleMultiplierSliderChanged(ref this._rangeMultiplier, value, null, null);
		}

		// Token: 0x0600006E RID: 110 RVA: 0x0000593E File Offset: 0x00003B3E
		private void SetFireRateMultiplierFromSlider(float value)
		{
			this.HandleMultiplierSliderChanged(ref this._fireRateMultiplier, value, null, null);
		}

		// Token: 0x0600006F RID: 111 RVA: 0x0000594F File Offset: 0x00003B4F
		private void SetBulletSpeedMultiplierFromSlider(float value)
		{
			this.HandleMultiplierSliderChanged(ref this._bulletSpeedMultiplier, value, null, null);
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00005960 File Offset: 0x00003B60
		private void SetDamageMultiplierFromSlider(float value)
		{
			this.HandleMultiplierSliderChanged(ref this._damageMultiplier, value, null, null);
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00005974 File Offset: 0x00003B74
		private void SetAutoAimScreenRadius(float radius)
		{
			float num = Mathf.Clamp(Mathf.Round(radius / 5f) * 5f, 60f, 2048f);
			if (Mathf.Abs(this._autoAimScreenRadius - num) <= 0.001f)
			{
				return;
			}
			this._autoAimScreenRadius = num;
			this._autoAimCircleRadiusPixels = this._autoAimScreenRadius;
			this.UpdateAutoAimSection();
			this.SaveSettings();
		}

		// Token: 0x06000072 RID: 114 RVA: 0x000059D8 File Offset: 0x00003BD8
		private void SetSpinKillSearchRadius(float radius)
		{
			float num = Mathf.Clamp(Mathf.Round(radius / 0.1f) * 0.1f, 5f, 120f);
			if (Mathf.Abs(this._spinKillSearchRadius - num) <= 0.01f)
			{
				return;
			}
			this._spinKillSearchRadius = num;
			this.UpdateSpinKillRangeRendererRadius();
			this.SaveSettings();
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00005A30 File Offset: 0x00003C30
		private void SetSpinKillAngularSpeed(float value)
		{
			float num = Mathf.Clamp(Mathf.Round(value / 30f) * 30f, 180f, 14400f);
			if (Mathf.Abs(this._spinKillAngularSpeed - num) <= 0.01f)
			{
				return;
			}
			this._spinKillAngularSpeed = num;
			this.SaveSettings();
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00005A84 File Offset: 0x00003C84
		private void InitializeMenuWindowIfNeeded()
		{
			if (this._menuWindowInitialized)
			{
				return;
			}
			float num = Mathf.Clamp((float)Screen.width * 0.32f, 320f, 420f);
			float num2 = Mathf.Clamp((float)Screen.height * 0.55f, 360f, Mathf.Max(360f, (float)Screen.height - 120f));
			float num3 = (float)Screen.width - num - 40f;
			float num4 = 40f;
			this._menuWindowRect = new Rect(num3, num4, num, num2);
			this._menuWindowInitialized = true;
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00005B10 File Offset: 0x00003D10
		private void EnsureAutoAimCircleTexture()
		{
			if (this._autoAimCircleTexture != null)
			{
				return;
			}
			Texture2D texture2D = new Texture2D(256, 256, TextureFormat.ARGB32, false)
			{
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear,
				hideFlags = HideFlags.DontSave
			};
			Color32[] array = new Color32[65536];
			float num = 128f;
			float num2 = num - 0.5f;
			float num3 = num * 0.94f;
			Color32 color = new Color32(51, 191, byte.MaxValue, byte.MaxValue);
			for (int i = 0; i < 256; i++)
			{
				float num4 = (float)i - num + 0.5f;
				for (int j = 0; j < 256; j++)
				{
					float num5 = (float)j - num + 0.5f;
					float num6 = Mathf.Sqrt(num5 * num5 + num4 * num4);
					int num7 = i * 256 + j;
					if (num6 > num2 || num6 < num3)
					{
						array[num7] = new Color32(0, 0, 0, 0);
					}
					else
					{
						float num8 = Mathf.InverseLerp(num2, num3, num6);
						byte b = (byte)Mathf.Lerp(72f, 216f, num8);
						array[num7] = new Color32(color.r, color.g, color.b, b);
					}
				}
			}
			texture2D.SetPixels32(array);
			texture2D.Apply(false, true);
			this._autoAimCircleTexture = texture2D;
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00005C70 File Offset: 0x00003E70
		private void EnsureOverlayCanvas()
		{
			if (this._overlayCanvasObject == null)
			{
				this._overlayCanvasObject = new GameObject("CheatMenuOverlayCanvas");
				this._overlayCanvasObject.transform.SetParent(base.transform, false);
				Canvas canvas = this._overlayCanvasObject.AddComponent<Canvas>();
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				canvas.sortingOrder = 5000;
				CanvasScaler canvasScaler = this._overlayCanvasObject.AddComponent<CanvasScaler>();
				canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
				canvasScaler.matchWidthOrHeight = 0.5f;
				this._overlayCanvasObject.AddComponent<GraphicRaycaster>();
			}
			if (!this._overlayCanvasObject.activeSelf)
			{
				this._overlayCanvasObject.SetActive(true);
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00005D28 File Offset: 0x00003F28
		private void EnsureOverlayUI()
		{
			this.EnsureOverlayCanvas();
			if (this._overlayCanvasObject == null)
			{
				return;
			}
			if (this._menuPanel == null)
			{
				GameObject gameObject = new GameObject("CheatMenuPanel");
				gameObject.transform.SetParent(this._overlayCanvasObject.transform, false);
				this._menuPanel = gameObject.AddComponent<RectTransform>();
				this._menuPanel.anchorMin = new Vector2(1f, 1f);
				this._menuPanel.anchorMax = new Vector2(1f, 1f);
				this._menuPanel.pivot = new Vector2(1f, 1f);
				this._menuPanel.anchoredPosition = new Vector2(-32f, -32f);
				this._menuPanel.sizeDelta = new Vector2(580f, 440f);
			}
			if (this._menuPanel != null)
			{
				Image component = this._menuPanel.GetComponent<Image>();
				if (component != null)
				{
					Object.Destroy(component);
				}
			}
			this.EnsureMenuHierarchy();
			this.EnsureSectionRoot(ModBehaviour.MenuSection.FunctionToggles);
			this.EnsureSectionRoot(ModBehaviour.MenuSection.Multipliers);
			this.EnsureSectionRoot(ModBehaviour.MenuSection.AutoAim);
			this.EnsureSectionRoot(ModBehaviour.MenuSection.Weapon);
			this.EnsureSectionRoot(ModBehaviour.MenuSection.Shortcuts);
			this.EnsureMultiplierSliders();
			this.UpdateSidebarSelectionVisuals();
			this.UpdateSectionVisibility();
		}

		// Token: 0x06000078 RID: 120 RVA: 0x00005E70 File Offset: 0x00004070
		private void EnsureMenuHierarchy()
		{
			if (this._menuPanel == null)
			{
				return;
			}
			if (this._menuRoot == null)
			{
				GameObject gameObject = new GameObject("CheatMenuRoot");
				gameObject.transform.SetParent(this._menuPanel, false);
				this._menuRoot = gameObject.AddComponent<RectTransform>();
				this._menuRoot.anchorMin = Vector2.zero;
				this._menuRoot.anchorMax = Vector2.one;
				this._menuRoot.offsetMin = Vector2.zero;
				this._menuRoot.offsetMax = Vector2.zero;
				HorizontalLayoutGroup horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
				horizontalLayoutGroup.spacing = 18f;
				horizontalLayoutGroup.padding = new RectOffset(18, 18, 18, 18);
				horizontalLayoutGroup.childAlignment = 0;
				horizontalLayoutGroup.childControlWidth = true;
				horizontalLayoutGroup.childForceExpandWidth = true;
				horizontalLayoutGroup.childControlHeight = true;
				horizontalLayoutGroup.childForceExpandHeight = true;
			}
			if (this._sidebarContainer == null && this._menuRoot != null)
			{
				GameObject gameObject2 = new GameObject("CheatMenuSidebar");
				gameObject2.transform.SetParent(this._menuRoot, false);
				this._sidebarContainer = gameObject2.AddComponent<RectTransform>();
				LayoutElement layoutElement = gameObject2.AddComponent<LayoutElement>();
				layoutElement.preferredWidth = 188f;
				layoutElement.minWidth = 170f;
				VerticalLayoutGroup verticalLayoutGroup = gameObject2.AddComponent<VerticalLayoutGroup>();
				verticalLayoutGroup.spacing = 10f;
				verticalLayoutGroup.padding = new RectOffset(0, 0, 4, 4);
				verticalLayoutGroup.childAlignment = 0;
				verticalLayoutGroup.childControlWidth = true;
				verticalLayoutGroup.childForceExpandWidth = true;
				verticalLayoutGroup.childControlHeight = false;
				verticalLayoutGroup.childForceExpandHeight = false;
			}
			if (this._contentContainer == null && this._menuRoot != null)
			{
				GameObject gameObject3 = new GameObject("CheatMenuContent");
				gameObject3.transform.SetParent(this._menuRoot, false);
				this._contentContainer = gameObject3.AddComponent<RectTransform>();
				this._contentContainer.anchorMin = Vector2.zero;
				this._contentContainer.anchorMax = Vector2.one;
				this._contentContainer.offsetMin = Vector2.zero;
				this._contentContainer.offsetMax = Vector2.zero;
				LayoutElement layoutElement2 = gameObject3.AddComponent<LayoutElement>();
				layoutElement2.flexibleWidth = 1f;
				layoutElement2.flexibleHeight = 1f;
				layoutElement2.minWidth = 240f;
			}
		}

		// Token: 0x06000079 RID: 121 RVA: 0x000060A8 File Offset: 0x000042A8
		private void EnsureSidebarButtons()
		{
			if (this._sidebarContainer == null)
			{
				return;
			}
			foreach (object obj in Enum.GetValues(typeof(ModBehaviour.MenuSection)))
			{
				ModBehaviour.MenuSection menuSection = (ModBehaviour.MenuSection)obj;
				this.EnsureSidebarButton(menuSection);
			}
		}

		// Token: 0x0600007A RID: 122 RVA: 0x0000611C File Offset: 0x0000431C
		private void EnsureSidebarButton(ModBehaviour.MenuSection section)
		{
			if (this._sidebarContainer == null)
			{
				return;
			}
			if (this._menuSectionButtons.ContainsKey(section))
			{
				return;
			}
			GameObject gameObject = new GameObject(string.Format("{0}Button", section));
			gameObject.transform.SetParent(this._sidebarContainer, false);
			RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
			rectTransform.sizeDelta = new Vector2(0f, 44f);
			Image image = gameObject.AddComponent<Image>();
			image.color = this._sidebarButtonColor;
			Button button = gameObject.AddComponent<Button>();
			button.targetGraphic = image;
			button.transition = Selectable.Transition.None;
			button.navigation = new Navigation
			{
				mode = Navigation.Mode.None
			};
			LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
			layoutElement.minHeight = 44f;
			layoutElement.preferredHeight = 44f;
			TextMeshProUGUI textMeshProUGUI = Object.Instantiate<TextMeshProUGUI>(GameplayDataSettings.UIStyle.TemplateTextUGUI, rectTransform);
			RectTransform rectTransform2 = textMeshProUGUI.rectTransform;
			rectTransform2.anchorMin = Vector2.zero;
			rectTransform2.anchorMax = Vector2.one;
			rectTransform2.offsetMin = new Vector2(14f, 6f);
			rectTransform2.offsetMax = new Vector2(-14f, -6f);
			textMeshProUGUI.alignment = TextAlignmentOptions.MidlineLeft;
			textMeshProUGUI.fontSize = 22f;
			textMeshProUGUI.enableWordWrapping = false;
			textMeshProUGUI.text = ModBehaviour.GetMenuSectionLabel(section);
			ModBehaviour.MenuSection captured = section;
			button.onClick.AddListener(delegate
			{
				this.SwitchMenuSection(captured);
			});
			this._menuSectionButtons[section] = button;
		}

		// Token: 0x0600007B RID: 123 RVA: 0x0000629C File Offset: 0x0000449C
		private static string GetMenuSectionLabel(ModBehaviour.MenuSection section)
		{
			switch (section)
			{
			case ModBehaviour.MenuSection.FunctionToggles:
				return "功能开关";
			case ModBehaviour.MenuSection.Multipliers:
				return "数值倍率";
			case ModBehaviour.MenuSection.AutoAim:
				return "自瞄参数";
			case ModBehaviour.MenuSection.Weapon:
				return "武器信息";
			case ModBehaviour.MenuSection.Shortcuts:
				return "快捷键";
			default:
				return section.ToString();
			}
		}

		// Token: 0x0600007C RID: 124 RVA: 0x000062F0 File Offset: 0x000044F0
		private void EnsureSectionRoot(ModBehaviour.MenuSection section)
		{
			if (this._contentContainer == null)
			{
				return;
			}
			if (this._menuSectionRoots.ContainsKey(section))
			{
				return;
			}
			GameObject gameObject = new GameObject(string.Format("{0}Section", section));
			gameObject.transform.SetParent(this._contentContainer, false);
			RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
			layoutElement.flexibleWidth = 1f;
			layoutElement.flexibleHeight = 1f;
			layoutElement.minHeight = 0f;
			this._menuSectionRoots[section] = gameObject;
			switch (section)
			{
			case ModBehaviour.MenuSection.FunctionToggles:
				this._functionToggleContent = this.CreateSectionText(rectTransform);
				break;
			case ModBehaviour.MenuSection.Multipliers:
				this.SetupMultipliersSection(rectTransform);
				break;
			case ModBehaviour.MenuSection.AutoAim:
				this._autoAimContent = this.CreateSectionText(rectTransform);
				break;
			case ModBehaviour.MenuSection.Weapon:
				this._weaponInfoContent = this.CreateSectionText(rectTransform);
				break;
			case ModBehaviour.MenuSection.Shortcuts:
				this._shortcutsContent = this.CreateSectionText(rectTransform);
				break;
			}
			gameObject.SetActive(section == this._activeMenuSection);
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00006418 File Offset: 0x00004618
		private TextMeshProUGUI CreateSectionText(RectTransform parent)
		{
			TextMeshProUGUI textMeshProUGUI = Object.Instantiate<TextMeshProUGUI>(GameplayDataSettings.UIStyle.TemplateTextUGUI, parent);
			RectTransform rectTransform = textMeshProUGUI.rectTransform;
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = new Vector2(18f, 18f);
			rectTransform.offsetMax = new Vector2(-18f, -18f);
			textMeshProUGUI.alignment = TextAlignmentOptions.TopLeft;
			textMeshProUGUI.fontSize = 22f;
			textMeshProUGUI.enableWordWrapping = true;
			textMeshProUGUI.richText = true;
			return textMeshProUGUI;
		}

		// Token: 0x0600007E RID: 126 RVA: 0x000064A0 File Offset: 0x000046A0
		private void SetupMultipliersSection(RectTransform sectionRect)
		{
			VerticalLayoutGroup verticalLayoutGroup = sectionRect.gameObject.AddComponent<VerticalLayoutGroup>();
			verticalLayoutGroup.spacing = 8f;
			verticalLayoutGroup.padding = new RectOffset(14, 14, 14, 14);
			verticalLayoutGroup.childAlignment = 0;
			verticalLayoutGroup.childControlWidth = true;
			verticalLayoutGroup.childForceExpandWidth = true;
			verticalLayoutGroup.childControlHeight = true;
			verticalLayoutGroup.childForceExpandHeight = false;
			this._multipliersSummary = Object.Instantiate<TextMeshProUGUI>(GameplayDataSettings.UIStyle.TemplateTextUGUI, sectionRect);
			this._multipliersSummary.alignment = TextAlignmentOptions.MidlineLeft;
			this._multipliersSummary.fontSize = 20f;
			this._multipliersSummary.enableWordWrapping = true;
			this._multipliersSummary.richText = true;
			LayoutElement layoutElement = this._multipliersSummary.gameObject.AddComponent<LayoutElement>();
			layoutElement.preferredHeight = 64f;
			layoutElement.minHeight = 48f;
			layoutElement.flexibleHeight = 0f;
			if (this._sliderContainer == null)
			{
				GameObject gameObject = new GameObject("CheatMenuSliderContainer");
				gameObject.transform.SetParent(sectionRect, false);
				this._sliderContainer = gameObject.AddComponent<RectTransform>();
				VerticalLayoutGroup verticalLayoutGroup2 = gameObject.AddComponent<VerticalLayoutGroup>();
				verticalLayoutGroup2.spacing = 8f;
				verticalLayoutGroup2.padding = new RectOffset(0, 0, 18, 0);
				verticalLayoutGroup2.childAlignment = 0;
				verticalLayoutGroup2.childControlWidth = true;
				verticalLayoutGroup2.childForceExpandWidth = true;
				verticalLayoutGroup2.childControlHeight = true;
				verticalLayoutGroup2.childForceExpandHeight = false;
			}
			else
			{
				this._sliderContainer.SetParent(sectionRect, false);
			}
			LayoutElement layoutElement2 = this._sliderContainer.gameObject.GetComponent<LayoutElement>();
			if (layoutElement2 == null)
			{
				layoutElement2 = this._sliderContainer.gameObject.AddComponent<LayoutElement>();
			}
			layoutElement2.flexibleHeight = 1f;
			layoutElement2.minHeight = 180f;
		}

		// Token: 0x0600007F RID: 127 RVA: 0x0000663B File Offset: 0x0000483B
		private void SwitchMenuSection(ModBehaviour.MenuSection section)
		{
			if (this._activeMenuSection == section)
			{
				return;
			}
			this._activeMenuSection = section;
			this.UpdateSidebarSelectionVisuals();
			this.UpdateSectionVisibility();
		}

		// Token: 0x06000080 RID: 128 RVA: 0x0000665C File Offset: 0x0000485C
		private void UpdateSidebarSelectionVisuals()
		{
			foreach (KeyValuePair<ModBehaviour.MenuSection, Button> keyValuePair in this._menuSectionButtons)
			{
				Button value = keyValuePair.Value;
				if (!(value == null))
				{
					Image component = value.GetComponent<Image>();
					if (component != null)
					{
						component.color = ((keyValuePair.Key == this._activeMenuSection) ? this._sidebarButtonSelectedColor : this._sidebarButtonColor);
					}
				}
			}
		}

		// Token: 0x06000081 RID: 129 RVA: 0x000066EC File Offset: 0x000048EC
		private void UpdateSectionVisibility()
		{
			foreach (KeyValuePair<ModBehaviour.MenuSection, GameObject> keyValuePair in this._menuSectionRoots)
			{
				if (!(keyValuePair.Value == null))
				{
					keyValuePair.Value.SetActive(keyValuePair.Key == this._activeMenuSection);
				}
			}
			if (this._sliderContainer != null)
			{
				this._sliderContainer.gameObject.SetActive(this._activeMenuSection == ModBehaviour.MenuSection.Multipliers);
			}
		}

		// Token: 0x06000082 RID: 130 RVA: 0x0000678C File Offset: 0x0000498C
		private void EnsureMultiplierSliders()
		{
			if (this._sliderContainer == null)
			{
				return;
			}
			this.EnsureMultiplierSlider(ref this._rangeSlider, ref this._rangeValueText, "RangeMultiplierRow", "射程倍率", this._rangeMultiplier, new UnityAction<float>(this.OnRangeSliderChanged));
			this.EnsureMultiplierSlider(ref this._fireRateSlider, ref this._fireRateValueText, "FireRateMultiplierRow", "射速倍率", this._fireRateMultiplier, new UnityAction<float>(this.OnFireRateSliderChanged));
			this.EnsureMultiplierSlider(ref this._bulletSpeedSlider, ref this._bulletSpeedValueText, "BulletSpeedMultiplierRow", "子弹速度倍率", this._bulletSpeedMultiplier, new UnityAction<float>(this.OnBulletSpeedSliderChanged));
			this.EnsureMultiplierSlider(ref this._damageSlider, ref this._damageValueText, "DamageMultiplierRow", "伤害倍率", this._damageMultiplier, new UnityAction<float>(this.OnDamageSliderChanged));
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00006860 File Offset: 0x00004A60
		private void EnsureMultiplierSlider([Nullable(2)] ref Slider sliderField, [Nullable(2)] ref TextMeshProUGUI valueLabel, string objectName, string labelText, float currentValue, UnityAction<float> handler)
		{
			if (this._sliderContainer == null)
			{
				return;
			}
			if (sliderField != null)
			{
				sliderField.SetValueWithoutNotify(currentValue);
				this.UpdateSliderValueDisplay(valueLabel, currentValue);
				return;
			}
			GameObject gameObject = new GameObject(objectName);
			gameObject.transform.SetParent(this._sliderContainer, false);
			RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
			HorizontalLayoutGroup horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
			horizontalLayoutGroup.spacing = 10f;
			horizontalLayoutGroup.childAlignment = 3;
			horizontalLayoutGroup.childControlWidth = true;
			horizontalLayoutGroup.childForceExpandWidth = true;
			horizontalLayoutGroup.childControlHeight = true;
			horizontalLayoutGroup.childForceExpandHeight = false;
			LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
			layoutElement.minHeight = 48f;
			layoutElement.preferredHeight = 48f;
			TextMeshProUGUI textMeshProUGUI = Object.Instantiate<TextMeshProUGUI>(GameplayDataSettings.UIStyle.TemplateTextUGUI, rectTransform);
			textMeshProUGUI.text = labelText;
			textMeshProUGUI.fontSize = 20f;
			textMeshProUGUI.alignment = TextAlignmentOptions.MidlineLeft;
			textMeshProUGUI.enableWordWrapping = false;
			LayoutElement layoutElement2 = textMeshProUGUI.gameObject.AddComponent<LayoutElement>();
			layoutElement2.preferredWidth = 132f;
			layoutElement2.minWidth = 116f;
			Slider slider = this.CreateSliderControl(rectTransform);
			slider.minValue = 1f;
			slider.maxValue = 10f;
			slider.wholeNumbers = false;
			slider.SetValueWithoutNotify(currentValue);
			slider.onValueChanged.AddListener(handler);
			LayoutElement layoutElement3 = slider.gameObject.AddComponent<LayoutElement>();
			layoutElement3.flexibleWidth = 1f;
			layoutElement3.minWidth = 220f;
			layoutElement3.preferredHeight = 28f;
			TextMeshProUGUI textMeshProUGUI2 = Object.Instantiate<TextMeshProUGUI>(GameplayDataSettings.UIStyle.TemplateTextUGUI, rectTransform);
			textMeshProUGUI2.fontSize = 20f;
			textMeshProUGUI2.alignment = TextAlignmentOptions.MidlineRight;
			textMeshProUGUI2.enableWordWrapping = false;
			LayoutElement layoutElement4 = textMeshProUGUI2.gameObject.AddComponent<LayoutElement>();
			layoutElement4.preferredWidth = 84f;
			layoutElement4.minWidth = 72f;
			sliderField = slider;
			valueLabel = textMeshProUGUI2;
			this.UpdateSliderValueDisplay(textMeshProUGUI2, currentValue);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00006A1C File Offset: 0x00004C1C
		private Slider CreateSliderControl(RectTransform parent)
		{
			GameObject gameObject = new GameObject("Slider", new Type[] { typeof(RectTransform) });
			gameObject.transform.SetParent(parent, false);
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(0f, 0.5f);
			component.anchorMax = new Vector2(1f, 0.5f);
			component.pivot = new Vector2(0.5f, 0.5f);
			component.sizeDelta = new Vector2(0f, 26f);
			Slider slider = gameObject.AddComponent<Slider>();
			slider.direction = Slider.Direction.LeftToRight;
			slider.navigation = new Navigation
			{
				mode = Navigation.Mode.None
			};
			GameObject gameObject2 = new GameObject("Background", new Type[] { typeof(RectTransform) });
			gameObject2.transform.SetParent(component, false);
			RectTransform component2 = gameObject2.GetComponent<RectTransform>();
			component2.anchorMin = Vector2.zero;
			component2.anchorMax = Vector2.one;
			component2.offsetMin = Vector2.zero;
			component2.offsetMax = Vector2.zero;
			gameObject2.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
			GameObject gameObject3 = new GameObject("Fill Area", new Type[] { typeof(RectTransform) });
			gameObject3.transform.SetParent(component, false);
			RectTransform component3 = gameObject3.GetComponent<RectTransform>();
			component3.anchorMin = new Vector2(0f, 0f);
			component3.anchorMax = new Vector2(1f, 1f);
			component3.offsetMin = new Vector2(4f, 4f);
			component3.offsetMax = new Vector2(-14f, -4f);
			GameObject gameObject4 = new GameObject("Fill", new Type[] { typeof(RectTransform) });
			gameObject4.transform.SetParent(component3, false);
			RectTransform component4 = gameObject4.GetComponent<RectTransform>();
			component4.anchorMin = new Vector2(0f, 0f);
			component4.anchorMax = new Vector2(1f, 1f);
			component4.offsetMin = Vector2.zero;
			component4.offsetMax = Vector2.zero;
			gameObject4.AddComponent<Image>().color = new Color(0.2f, 0.75f, 1f, 0.85f);
			slider.fillRect = component4;
			GameObject gameObject5 = new GameObject("Handle Slide Area", new Type[] { typeof(RectTransform) });
			gameObject5.transform.SetParent(component, false);
			RectTransform component5 = gameObject5.GetComponent<RectTransform>();
			component5.anchorMin = new Vector2(0f, 0f);
			component5.anchorMax = new Vector2(1f, 1f);
			component5.offsetMin = new Vector2(4f, 4f);
			component5.offsetMax = new Vector2(-4f, -4f);
			GameObject gameObject6 = new GameObject("Handle", new Type[] { typeof(RectTransform) });
			gameObject6.transform.SetParent(component5, false);
			RectTransform component6 = gameObject6.GetComponent<RectTransform>();
			component6.sizeDelta = new Vector2(16f, 24f);
			Image image = gameObject6.AddComponent<Image>();
			image.color = Color.white;
			slider.handleRect = component6;
			slider.targetGraphic = image;
			return slider;
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00006D66 File Offset: 0x00004F66
		private static float SnapMultiplier(float value)
		{
			return Mathf.Clamp(Mathf.Round(value / 0.1f) * 0.1f, 1f, 10f);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00006D89 File Offset: 0x00004F89
		private static string FormatMultiplierValue(float value)
		{
			return string.Format("x{0:F1}", value);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00006D9B File Offset: 0x00004F9B
		private static string FormatShortcutLabel(KeyCode key)
		{
			return string.Format("{0} + {1}", "Ctrl", key);
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00006DB2 File Offset: 0x00004FB2
		[NullableContext(2)]
		private void UpdateSliderValueDisplay(TextMeshProUGUI valueText, float value)
		{
			if (valueText == null)
			{
				return;
			}
			valueText.SetText(ModBehaviour.FormatMultiplierValue(value), true);
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00006DCB File Offset: 0x00004FCB
		private void OnRangeSliderChanged(float value)
		{
			if (this._suppressSliderEvents)
			{
				return;
			}
			this.HandleMultiplierSliderChanged(ref this._rangeMultiplier, value, this._rangeSlider, this._rangeValueText);
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00006DEF File Offset: 0x00004FEF
		private void OnFireRateSliderChanged(float value)
		{
			if (this._suppressSliderEvents)
			{
				return;
			}
			this.HandleMultiplierSliderChanged(ref this._fireRateMultiplier, value, this._fireRateSlider, this._fireRateValueText);
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00006E13 File Offset: 0x00005013
		private void OnBulletSpeedSliderChanged(float value)
		{
			if (this._suppressSliderEvents)
			{
				return;
			}
			this.HandleMultiplierSliderChanged(ref this._bulletSpeedMultiplier, value, this._bulletSpeedSlider, this._bulletSpeedValueText);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00006E37 File Offset: 0x00005037
		private void OnDamageSliderChanged(float value)
		{
			if (this._suppressSliderEvents)
			{
				return;
			}
			this.HandleMultiplierSliderChanged(ref this._damageMultiplier, value, this._damageSlider, this._damageValueText);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00006E5C File Offset: 0x0000505C
		[NullableContext(2)]
		private void HandleMultiplierSliderChanged(ref float multiplierField, float sliderValue, Slider slider, TextMeshProUGUI valueLabel)
		{
			float num = ModBehaviour.SnapMultiplier(sliderValue);
			if (Mathf.Abs(num - sliderValue) > 0.0001f)
			{
				this._suppressSliderEvents = true;
				if (slider != null)
				{
					slider.SetValueWithoutNotify(num);
				}
				this._suppressSliderEvents = false;
			}
			if (Mathf.Abs(multiplierField - num) <= 0.0001f)
			{
				this.UpdateSliderValueDisplay(valueLabel, multiplierField);
				return;
			}
			multiplierField = num;
			this.UpdateSliderValueDisplay(valueLabel, multiplierField);
			this.ApplyGunStatMultipliers(this._trackedGun);
			this.SaveSettings();
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00006ED4 File Offset: 0x000050D4
		[NullableContext(2)]
		private void ApplyGunStatMultipliers(ItemAgent_Gun gun)
		{
			if (gun == null || gun.Item == null)
			{
				return;
			}
			this.ApplyStatMultiplier(gun.Item, ModBehaviour.BulletDistanceStatHash, this._rangeMultiplier);
			this.ApplyStatMultiplier(gun.Item, ModBehaviour.ShootSpeedStatHash, this._fireRateMultiplier);
			this.ApplyStatMultiplier(gun.Item, ModBehaviour.BulletSpeedStatHash, this._bulletSpeedMultiplier);
			this.ApplyStatMultiplier(gun.Item, ModBehaviour.DamageStatHash, this._damageMultiplier);
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00006F58 File Offset: 0x00005158
		private void ApplyStatMultiplier(Item item, int statHash, float multiplier)
		{
			if (item == null)
			{
				return;
			}
			Stat stat = item.GetStat(statHash);
			if (stat == null)
			{
				return;
			}
			Dictionary<int, float> dictionary;
			if (!this._statMultiplierOriginalValues.TryGetValue(item, out dictionary))
			{
				dictionary = new Dictionary<int, float>();
				this._statMultiplierOriginalValues[item] = dictionary;
			}
			float baseValue;
			if (!dictionary.TryGetValue(statHash, out baseValue))
			{
				baseValue = stat.BaseValue;
				dictionary[statHash] = baseValue;
			}
			float num = baseValue * multiplier;
			stat.BaseValue = num;
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00006FC4 File Offset: 0x000051C4
		private void RestoreGunStatMultipliers()
		{
			foreach (KeyValuePair<Item, Dictionary<int, float>> keyValuePair in this._statMultiplierOriginalValues)
			{
				Item key = keyValuePair.Key;
				if (!(key == null))
				{
					foreach (KeyValuePair<int, float> keyValuePair2 in keyValuePair.Value)
					{
						Stat stat = key.GetStat(keyValuePair2.Key);
						if (stat != null)
						{
							stat.BaseValue = keyValuePair2.Value;
						}
					}
				}
			}
			this._statMultiplierOriginalValues.Clear();
		}

		// Token: 0x06000091 RID: 145 RVA: 0x0000708C File Offset: 0x0000528C
		private void RefreshMenuContent(CharacterMainControl character, Vector2 aimScreenPoint)
		{
			ItemAgent_Gun itemAgent_Gun = ((character.agentHolder != null) ? character.agentHolder.CurrentHoldGun : null);
			this.UpdateMultipliersSummary();
			this.UpdateAutoAimSection();
			this.UpdateWeaponInfoSection(character, itemAgent_Gun);
			this.UpdateShortcutsSection();
		}

		// Token: 0x06000092 RID: 146 RVA: 0x000070D0 File Offset: 0x000052D0
		private void UpdateFunctionToggleSection()
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			stringBuilder.AppendLine("<b>功能开关</b>");
			stringBuilder.Append("  • 自瞄：").AppendLine(ModBehaviour.FormatToggleStatus(this._autoAimEnabled));
			stringBuilder.Append("  • 手雷自瞄：").AppendLine(ModBehaviour.FormatToggleStatus(this._autoAimEnabled && this._grenadeAutoAimEnabled));
			stringBuilder.Append("  • 自瞄范围显示：").AppendLine(ModBehaviour.FormatToggleStatus(this._autoAimEnabled && this._rangeVisible));
			stringBuilder.Append("  • 无后坐力：").AppendLine(ModBehaviour.FormatToggleStatus(this._noRecoilEnabled));
			stringBuilder.Append("  • 无限弹药：").AppendLine(ModBehaviour.FormatToggleStatus(this._infiniteAmmoEnabled));
			stringBuilder.Append("  • 武器无限耐久：").AppendLine(ModBehaviour.FormatToggleStatus(this._infiniteDurabilityEnabled));
			stringBuilder.Append("  • 动作解锁射击：").AppendLine(ModBehaviour.FormatToggleStatus(this._movementUnlockEnabled));
			stringBuilder.Append("  • 子弹穿透障碍：").AppendLine(ModBehaviour.FormatToggleStatus(this._obstaclePenetrationEnabled));
			stringBuilder.Append("  • 主角无敌：").AppendLine(ModBehaviour.FormatToggleStatus(this._invincibilityEnabled));
			stringBuilder.Append("  • 无限体力：").AppendLine(ModBehaviour.FormatToggleStatus(this._infiniteStaminaEnabled));
			stringBuilder.Append("  • 免除饥渴：").AppendLine(ModBehaviour.FormatToggleStatus(this._noSurvivalNeedsEnabled));
			stringBuilder.Append("  • 陀螺：").AppendLine(ModBehaviour.FormatToggleStatus(this._spinKillEnabled));
			this._functionToggleText = stringBuilder.ToString();
			if (this._functionToggleContent != null)
			{
				this._functionToggleContent.SetText(this._functionToggleText, true);
			}
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00007288 File Offset: 0x00005488
		private void UpdateMultipliersSummary()
		{
			StringBuilder stringBuilder = new StringBuilder(128);
			stringBuilder.AppendLine("<b>当前倍率</b>");
			stringBuilder.Append("  • 射程：").AppendLine(ModBehaviour.FormatMultiplierValue(this._rangeMultiplier));
			stringBuilder.Append("  • 射速：").AppendLine(ModBehaviour.FormatMultiplierValue(this._fireRateMultiplier));
			stringBuilder.Append("  • 子弹速度：").AppendLine(ModBehaviour.FormatMultiplierValue(this._bulletSpeedMultiplier));
			stringBuilder.Append("  • 伤害：").AppendLine(ModBehaviour.FormatMultiplierValue(this._damageMultiplier));
			this._multipliersSummaryText = stringBuilder.ToString();
			if (this._multipliersSummary != null)
			{
				this._multipliersSummary.SetText(this._multipliersSummaryText, true);
			}
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00007348 File Offset: 0x00005548
		private void UpdateAutoAimSection()
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			stringBuilder.AppendLine("<b>自瞄参数</b>");
			stringBuilder.Append("  • 自瞄状态：").AppendLine(ModBehaviour.FormatToggleStatus(this._autoAimEnabled));
			stringBuilder.Append("  • 手雷自瞄：").AppendLine(ModBehaviour.FormatToggleStatus(this._grenadeAutoAimEnabled));
			stringBuilder.Append("  • 自瞄半径：").Append(this._autoAimScreenRadius.ToString("F0")).AppendLine(" px");
			stringBuilder.Append("  • 最大射程：").Append(80f.ToString("F0")).AppendLine(" m");
			stringBuilder.Append("  • 穿透开关：").AppendLine(ModBehaviour.FormatToggleStatus(this._obstaclePenetrationEnabled));
			stringBuilder.Append("  • 穿透激活：").AppendLine(ModBehaviour.FormatToggleStatus(this._penetrationActive));
			this._autoAimContentText = stringBuilder.ToString();
			if (this._autoAimContent != null)
			{
				this._autoAimContent.SetText(this._autoAimContentText, true);
			}
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00007460 File Offset: 0x00005660
		private void UpdateWeaponInfoSection(CharacterMainControl character, [Nullable(2)] ItemAgent_Gun gun)
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			stringBuilder.AppendLine("<b>武器信息</b>");
			if (gun != null)
			{
				float bulletDistance = gun.BulletDistance;
				stringBuilder.Append("  • 当前武器：").AppendLine(gun.name);
				stringBuilder.Append("  • 理论射程：").Append(bulletDistance.ToString("F1")).AppendLine(" m");
				stringBuilder.Append("  • 射程倍率：").Append(character.GunDistanceMultiplier.ToString("F2")).AppendLine();
				stringBuilder.Append("  • 射速：").Append(gun.ShootSpeed.ToString("F2")).AppendLine(" 发/秒");
				stringBuilder.Append("  • 子弹速度：").Append(gun.BulletSpeed.ToString("F1")).AppendLine(" 米/秒");
				stringBuilder.Append("  • 伤害：").Append(gun.Damage.ToString("F1")).AppendLine();
			}
			else
			{
				stringBuilder.AppendLine("  • 当前武器：无");
			}
			this._weaponInfoText = stringBuilder.ToString();
			if (this._weaponInfoContent != null)
			{
				this._weaponInfoContent.SetText(this._weaponInfoText, true);
			}
		}

		// Token: 0x06000096 RID: 150 RVA: 0x000075C0 File Offset: 0x000057C0
		private void UpdateShortcutsSection()
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			stringBuilder.AppendLine("<b>快捷键</b>");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F1)).AppendLine(" 切换自瞄");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F2)).AppendLine(" 切换无后坐力");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F3)).AppendLine(" 切换无限弹药");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F7)).AppendLine(" 切换武器无限耐久");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F4)).AppendLine(" 切换动作解锁射击");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F5)).AppendLine(" 切换子弹穿透障碍");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F6)).AppendLine(" 切换主角无敌");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F10)).AppendLine(" 切换免除饥渴");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F11)).AppendLine(" 切换陀螺");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F8)).AppendLine(" 切换菜单");
			stringBuilder.Append("  • ").Append(ModBehaviour.FormatShortcutLabel(KeyCode.F9)).AppendLine(" 切换自瞄范围显示");
			this._shortcutsText = stringBuilder.ToString();
			if (this._shortcutsContent != null)
			{
				this._shortcutsContent.SetText(this._shortcutsText, true);
			}
		}

		// Token: 0x06000097 RID: 151 RVA: 0x000077A8 File Offset: 0x000059A8
		private void SetMenuVisibility(bool visible)
		{
			if (visible)
			{
				this.EnsureOverlayUI();
				if (this._overlayCanvasObject != null && !this._overlayCanvasObject.activeSelf)
				{
					this._overlayCanvasObject.SetActive(true);
				}
				if (this._menuPanel != null && !this._menuPanel.gameObject.activeSelf)
				{
					this._menuPanel.gameObject.SetActive(true);
					return;
				}
			}
			else if (this._menuPanel != null && this._menuPanel.gameObject.activeSelf)
			{
				this._menuPanel.gameObject.SetActive(false);
			}
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00007848 File Offset: 0x00005A48
		private void EnsureAutoAimRangeRenderer()
		{
			this.EnsureOverlayCanvas();
			GameObject overlayCanvasObject = this._overlayCanvasObject;
			if (overlayCanvasObject == null)
			{
				return;
			}
			if (this._autoAimRangeRect == null)
			{
				GameObject gameObject = new GameObject("CheatMenuAutoAimRange", new Type[] { typeof(RectTransform) });
				gameObject.transform.SetParent(overlayCanvasObject.transform, false);
				RectTransform component = gameObject.GetComponent<RectTransform>();
				component.anchorMin = new Vector2(0.5f, 0.5f);
				component.anchorMax = new Vector2(0.5f, 0.5f);
				component.pivot = new Vector2(0.5f, 0.5f);
				component.sizeDelta = Vector2.zero;
				this._autoAimRangeRect = component;
				LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
				lineRenderer.loop = true;
				lineRenderer.useWorldSpace = false;
				lineRenderer.textureMode = LineTextureMode.Stretch;
				lineRenderer.numCornerVertices = 8;
				lineRenderer.numCapVertices = 0;
				lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
				lineRenderer.receiveShadows = false;
				lineRenderer.startColor = new Color(0.2f, 0.75f, 1f, 0.9f);
				lineRenderer.endColor = new Color(0.2f, 0.75f, 1f, 0.45f);
				lineRenderer.widthMultiplier = 3.5f;
				lineRenderer.positionCount = 64;
				this._autoAimRangeRenderer = lineRenderer;
			}
			if (this._autoAimRangeRenderer == null)
			{
				return;
			}
			ModBehaviour.ConfigureLineRendererForOverlay(this._autoAimRangeRenderer);
			if (this._autoAimRangeMaterial == null)
			{
				Shader shader = Shader.Find("UI/Default");
				if (shader == null)
				{
					shader = Shader.Find("Sprites/Default");
				}
				if (shader == null)
				{
					shader = Shader.Find("Hidden/Internal-Colored");
				}
				if (shader != null)
				{
					this._autoAimRangeMaterial = new Material(shader)
					{
						color = new Color(0.2f, 0.75f, 1f, 0.85f),
						renderQueue = 4000
					};
					ModBehaviour.ConfigureOverlayMaterial(this._autoAimRangeMaterial);
				}
			}
			if (this._autoAimRangeMaterial != null)
			{
				ModBehaviour.ConfigureOverlayMaterial(this._autoAimRangeMaterial);
				if (this._autoAimRangeRenderer.material != this._autoAimRangeMaterial)
				{
					this._autoAimRangeRenderer.material = this._autoAimRangeMaterial;
				}
			}
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00007A7C File Offset: 0x00005C7C
		private static void ConfigureOverlayMaterial(Material material)
		{
			if (material == null)
			{
				return;
			}
			try
			{
				material.renderQueue = 4000;
				if (material.HasProperty("_ZWrite"))
				{
					material.SetInt("_ZWrite", 0);
				}
				if (material.HasProperty("_ZTest"))
				{
					material.SetInt("_ZTest", 8);
				}
				if (material.HasProperty("unity_GUIZTestMode"))
				{
					material.SetInt("unity_GUIZTestMode", 8);
				}
				if (material.HasProperty("_SrcBlend"))
				{
					material.SetInt("_SrcBlend", 5);
				}
				if (material.HasProperty("_DstBlend"))
				{
					material.SetInt("_DstBlend", 10);
				}
				if (material.HasProperty("_Cull"))
				{
					material.SetInt("_Cull", 0);
				}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00007B4C File Offset: 0x00005D4C
		private static void ConfigureLineRendererForOverlay(LineRenderer renderer)
		{
			if (renderer == null)
			{
				return;
			}
			renderer.useWorldSpace = false;
			renderer.allowOcclusionWhenDynamic = false;
			renderer.sortingOrder = 4096;
			renderer.alignment = LineAlignment.Local;
			renderer.numCapVertices = 8;
			renderer.numCornerVertices = 8;
			renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
			renderer.lightProbeUsage = LightProbeUsage.Off;
			renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
			renderer.forceRenderingOff = false;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			try
			{
				renderer.sortingLayerName = "UI";
			}
			catch (ArgumentException)
			{
				renderer.sortingLayerID = 0;
			}
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00007BE4 File Offset: 0x00005DE4
		private void UpdateAutoAimRangeVisualization(Camera camera, Vector2 aimScreenPoint, float planeHeight)
		{
			this.EnsureAutoAimCircleTexture();
			this._autoAimCircleVisible = true;
			this._autoAimCircleCenter = aimScreenPoint;
			this._autoAimCircleRadiusPixels = this._autoAimScreenRadius;
			Vector3 vector = ModBehaviour.EvaluateAimPlaneIntersection(camera.ScreenPointToRay(new Vector3(aimScreenPoint.x, aimScreenPoint.y, 0f)), planeHeight);
			float num = 0f;
			for (int i = 0; i < 64; i++)
			{
				float num2 = 6.28318548f * (float)i / 64f;
				float num3 = Mathf.Cos(num2) * this._autoAimScreenRadius;
				float num4 = Mathf.Sin(num2) * this._autoAimScreenRadius;
				Vector3 vector2 = ModBehaviour.EvaluateAimPlaneIntersection(camera.ScreenPointToRay(new Vector3(aimScreenPoint.x + num3, aimScreenPoint.y + num4, 0f)), planeHeight);
				this._autoAimRangePoints[i] = vector2;
				num += Vector3.Distance(vector, vector2);
			}
			this._lastAutoAimWorldRadius = num / 64f;
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00007CC0 File Offset: 0x00005EC0
		private void HideAutoAimRange()
		{
			this._autoAimCircleVisible = false;
			if (this._autoAimRangeRenderer != null && this._autoAimRangeRenderer.enabled)
			{
				this._autoAimRangeRenderer.enabled = false;
			}
			if (this._autoAimRangeRect != null && this._autoAimRangeRect.gameObject.activeSelf)
			{
				this._autoAimRangeRect.gameObject.SetActive(false);
			}
			this._lastAutoAimWorldRadius = 0f;
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00007D38 File Offset: 0x00005F38
		private void DestroyOverlayResources()
		{
			if (this._menuPanel != null)
			{
				Object.Destroy(this._menuPanel.gameObject);
				this._menuPanel = null;
			}
			this._menuRoot = null;
			this._sidebarContainer = null;
			this._contentContainer = null;
			this._functionToggleContent = null;
			this._autoAimContent = null;
			this._weaponInfoContent = null;
			this._shortcutsContent = null;
			this._multipliersSummary = null;
			this._menuSectionButtons.Clear();
			this._menuSectionRoots.Clear();
			this._activeMenuSection = ModBehaviour.MenuSection.FunctionToggles;
			this._sliderContainer = null;
			this._rangeSlider = null;
			this._fireRateSlider = null;
			this._damageSlider = null;
			this._rangeValueText = null;
			this._fireRateValueText = null;
			this._damageValueText = null;
			if (this._overlayCanvasObject != null)
			{
				Object.Destroy(this._overlayCanvasObject);
				this._overlayCanvasObject = null;
			}
			if (this._autoAimRangeRenderer != null)
			{
				Object.Destroy(this._autoAimRangeRenderer.gameObject);
				this._autoAimRangeRenderer = null;
				this._autoAimRangeRect = null;
			}
			if (this._autoAimRangeMaterial != null)
			{
				Object.Destroy(this._autoAimRangeMaterial);
				this._autoAimRangeMaterial = null;
			}
			if (this._autoAimCircleTexture != null)
			{
				Object.Destroy(this._autoAimCircleTexture);
				this._autoAimCircleTexture = null;
			}
			this._autoAimCircleVisible = false;
			this._menuWindowInitialized = false;
			this._lastAutoAimWorldRadius = 0f;
			this._headshotOffsets.Clear();
			this.DestroySpinKillRangeRenderer();
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00007EA8 File Offset: 0x000060A8
		private void OnGUI()
		{
			this.DrawAutoAimCircleOverlay();
			if (!this._menuVisible)
			{
				return;
			}
			if (Event.current.type == EventType.Layout)
			{
				this.InitializeMenuWindowIfNeeded();
			}
			bool richText = GUI.skin.label.richText;
			bool richText2 = GUI.skin.box.richText;
			GUI.skin.label.richText = true;
			GUI.skin.box.richText = true;
			this._menuWindowRect = GUILayout.Window(this._menuWindowId, this._menuWindowRect, new GUI.WindowFunction(this.DrawMenuWindowContents), "cheatmenu 控制面板", Array.Empty<GUILayoutOption>());
			GUI.skin.label.richText = richText;
			GUI.skin.box.richText = richText2;
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00007F64 File Offset: 0x00006164
		private void DrawAutoAimCircleOverlay()
		{
			if (!this._autoAimCircleVisible || Event.current.type != EventType.Repaint)
			{
				return;
			}
			if (this._autoAimCircleTexture == null)
			{
				this.EnsureAutoAimCircleTexture();
				if (this._autoAimCircleTexture == null)
				{
					return;
				}
			}
			float autoAimCircleRadiusPixels = this._autoAimCircleRadiusPixels;
			float num = autoAimCircleRadiusPixels * 2f;
			float num2 = (float)Screen.height - this._autoAimCircleCenter.y;
			Rect rect = new Rect(this._autoAimCircleCenter.x - autoAimCircleRadiusPixels, num2 - autoAimCircleRadiusPixels, num, num);
			Color color = GUI.color;
			GUI.color = new Color(0.2f, 0.75f, 1f, 0.8f);
			GUI.DrawTexture(rect, this._autoAimCircleTexture);
			GUI.color = color;
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00008018 File Offset: 0x00006218
		private void DrawMenuWindowContents(int windowId)
		{
			GUILayout.Space(4f);
			this._menuScrollPosition = GUILayout.BeginScrollView(this._menuScrollPosition, false, true, Array.Empty<GUILayoutOption>());
			CharacterMainControl trackedCharacter = this._trackedCharacter;
			this.DrawFunctionSection();
			GUILayout.Space(6f);
			this.DrawMultipliersSection();
			GUILayout.Space(6f);
			this.DrawAutoAimInfoSection();
			GUILayout.Space(6f);
			this.DrawWeaponInfoSection();
			GUILayout.Space(6f);
			this.DrawShortcutsSection();
			GUILayout.EndScrollView();
			GUI.DragWindow(new Rect(0f, 0f, this._menuWindowRect.width, 24f));
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x000080BD File Offset: 0x000062BD
		private bool DrawSectionHeader(string title, bool expanded)
		{
			if (GUILayout.Button((expanded ? "[-] " : "[+] ") + title, new GUILayoutOption[] { GUILayout.Height(28f) }))
			{
				expanded = !expanded;
			}
			return expanded;
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x000080F8 File Offset: 0x000062F8
		private void DrawFunctionSection()
		{
			this._functionSectionExpanded = this.DrawSectionHeader("功能开关", this._functionSectionExpanded);
			if (!this._functionSectionExpanded)
			{
				return;
			}
			GUILayout.BeginVertical(GUI.skin.box, Array.Empty<GUILayoutOption>());
			bool flag = GUILayout.Toggle(this._autoAimEnabled, "启用自瞄 (" + ModBehaviour.FormatShortcutLabel(KeyCode.F1) + ")", Array.Empty<GUILayoutOption>());
			if (flag != this._autoAimEnabled)
			{
				this.SetAutoAimEnabled(flag);
			}
			bool flag2 = GUILayout.Toggle(this._grenadeAutoAimEnabled, "手雷自瞄", Array.Empty<GUILayoutOption>());
			if (flag2 != this._grenadeAutoAimEnabled)
			{
				this.SetGrenadeAutoAimEnabled(flag2);
			}
			bool flag3 = GUILayout.Toggle(this._rangeVisible, "显示自瞄范围 (" + ModBehaviour.FormatShortcutLabel(KeyCode.F9) + ")", Array.Empty<GUILayoutOption>());
			if (flag3 != this._rangeVisible)
			{
				this.SetRangeVisible(flag3);
			}
			bool flag4 = GUILayout.Toggle(this._noRecoilEnabled, "无后坐力 (" + ModBehaviour.FormatShortcutLabel(KeyCode.F2) + ")", Array.Empty<GUILayoutOption>());
			if (flag4 != this._noRecoilEnabled)
			{
				this.SetNoRecoilEnabled(flag4);
			}
			bool flag5 = GUILayout.Toggle(this._infiniteAmmoEnabled, "无限弹药 (" + ModBehaviour.FormatShortcutLabel(KeyCode.F3) + ")", Array.Empty<GUILayoutOption>());
			if (flag5 != this._infiniteAmmoEnabled)
			{
				this.SetInfiniteAmmoEnabled(flag5);
			}
			bool flag6 = GUILayout.Toggle(this._infiniteDurabilityEnabled, "武器无限耐久 (" + ModBehaviour.FormatShortcutLabel(KeyCode.F7) + ")", Array.Empty<GUILayoutOption>());
			if (flag6 != this._infiniteDurabilityEnabled)
			{
				this.SetInfiniteDurabilityEnabled(flag6);
			}
			bool flag7 = GUILayout.Toggle(this._movementUnlockEnabled, "动作解锁射击 (" + ModBehaviour.FormatShortcutLabel(KeyCode.F4) + ")", Array.Empty<GUILayoutOption>());
			if (flag7 != this._movementUnlockEnabled)
			{
				this.SetMovementUnlockEnabled(flag7);
			}
			bool flag8 = GUILayout.Toggle(this._obstaclePenetrationEnabled, "子弹穿透障碍 (" + ModBehaviour.FormatShortcutLabel(KeyCode.F5) + ")", Array.Empty<GUILayoutOption>());
			if (flag8 != this._obstaclePenetrationEnabled)
			{
				this.SetObstaclePenetrationEnabled(flag8, true);
			}
			bool flag9 = GUILayout.Toggle(this._invincibilityEnabled, "主角无敌 (" + ModBehaviour.FormatShortcutLabel(KeyCode.F6) + ")", Array.Empty<GUILayoutOption>());
			if (flag9 != this._invincibilityEnabled)
			{
				this.SetInvincibilityEnabled(flag9);
			}
			bool flag10 = GUILayout.Toggle(this._infiniteStaminaEnabled, "无限体力", Array.Empty<GUILayoutOption>());
			if (flag10 != this._infiniteStaminaEnabled)
			{
				this.SetInfiniteStaminaEnabled(flag10);
			}
			bool flag11 = GUILayout.Toggle(this._noSurvivalNeedsEnabled, "免除饥渴 (" + ModBehaviour.FormatShortcutLabel(KeyCode.F10) + ")", Array.Empty<GUILayoutOption>());
			if (flag11 != this._noSurvivalNeedsEnabled)
			{
				this.SetNoSurvivalNeedsEnabled(flag11);
			}
			bool flag12 = GUILayout.Toggle(this._spinKillEnabled, "陀螺 (" + ModBehaviour.FormatShortcutLabel(KeyCode.F11) + ")", Array.Empty<GUILayoutOption>());
			if (flag12 != this._spinKillEnabled)
			{
				this.SetSpinKillEnabled(flag12, true, false);
			}
			GUILayout.EndVertical();
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x000083EC File Offset: 0x000065EC
		private void DrawMultipliersSection()
		{
			this._multipliersSectionExpanded = this.DrawSectionHeader("数值倍率", this._multipliersSectionExpanded);
			if (!this._multipliersSectionExpanded)
			{
				return;
			}
			GUILayout.BeginVertical(GUI.skin.box, Array.Empty<GUILayoutOption>());
			this.DrawMultiplierSliderRow("射程倍率", () => this._rangeMultiplier, new Action<float>(this.SetRangeMultiplierFromSlider));
			this.DrawMultiplierSliderRow("射速倍率", () => this._fireRateMultiplier, new Action<float>(this.SetFireRateMultiplierFromSlider));
			this.DrawMultiplierSliderRow("子弹速度倍率", () => this._bulletSpeedMultiplier, new Action<float>(this.SetBulletSpeedMultiplierFromSlider));
			this.DrawMultiplierSliderRow("伤害倍率", () => this._damageMultiplier, new Action<float>(this.SetDamageMultiplierFromSlider));
			GUILayout.EndVertical();
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x000084C0 File Offset: 0x000066C0
		private void DrawAutoAimInfoSection()
		{
			this._autoAimSectionExpanded = this.DrawSectionHeader("自瞄参数", this._autoAimSectionExpanded);
			if (!this._autoAimSectionExpanded)
			{
				return;
			}
			GUILayout.BeginVertical(GUI.skin.box, Array.Empty<GUILayoutOption>());
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label("屏幕半径", new GUILayoutOption[] { GUILayout.Width(132f) });
			float num = GUILayout.HorizontalSlider(this._autoAimScreenRadius, 60f, 2048f, Array.Empty<GUILayoutOption>());
			if (Mathf.Abs(num - this._autoAimScreenRadius) > 0.001f)
			{
				this.SetAutoAimScreenRadius(num);
			}
			GUILayout.Space(8f);
			GUILayout.Label(string.Format("{0:F0} px", this._autoAimScreenRadius), new GUILayoutOption[] { GUILayout.Width(80f) });
			GUILayout.EndHorizontal();
			GUILayout.Space(4f);
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label("陀螺半径", new GUILayoutOption[] { GUILayout.Width(132f) });
			float num2 = GUILayout.HorizontalSlider(this._spinKillSearchRadius, 5f, 120f, Array.Empty<GUILayoutOption>());
			if (Mathf.Abs(num2 - this._spinKillSearchRadius) > 0.001f)
			{
				this.SetSpinKillSearchRadius(num2);
			}
			GUILayout.Space(8f);
			GUILayout.Label(string.Format("{0:F0} m", this._spinKillSearchRadius), new GUILayoutOption[] { GUILayout.Width(80f) });
			GUILayout.EndHorizontal();
			GUILayout.Space(4f);
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label("旋转速度", new GUILayoutOption[] { GUILayout.Width(132f) });
			float num3 = GUILayout.HorizontalSlider(this._spinKillAngularSpeed, 180f, 14400f, Array.Empty<GUILayoutOption>());
			if (Mathf.Abs(num3 - this._spinKillAngularSpeed) > 0.001f)
			{
				this.SetSpinKillAngularSpeed(num3);
			}
			GUILayout.Space(8f);
			GUILayout.Label(string.Format("{0:F0} °/s", this._spinKillAngularSpeed), new GUILayoutOption[] { GUILayout.Width(80f) });
			GUILayout.EndHorizontal();
			GUILayout.Space(6f);
			if (!string.IsNullOrEmpty(this._autoAimContentText))
			{
				GUILayout.Label(this._autoAimContentText, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
			}
			else
			{
				GUILayout.Label("暂无自瞄信息", new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
			}
			GUILayout.EndVertical();
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00008734 File Offset: 0x00006934
		private void DrawWeaponInfoSection()
		{
			this._weaponSectionExpanded = this.DrawSectionHeader("武器信息", this._weaponSectionExpanded);
			if (!this._weaponSectionExpanded)
			{
				return;
			}
			GUILayout.BeginVertical(GUI.skin.box, Array.Empty<GUILayoutOption>());
			if (!string.IsNullOrEmpty(this._weaponInfoText))
			{
				GUILayout.Label(this._weaponInfoText, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
			}
			else
			{
				GUILayout.Label("暂无武器信息", new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
			}
			GUILayout.EndVertical();
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x000087BC File Offset: 0x000069BC
		private void DrawShortcutsSection()
		{
			this._shortcutsSectionExpanded = this.DrawSectionHeader("快捷键", this._shortcutsSectionExpanded);
			if (!this._shortcutsSectionExpanded)
			{
				return;
			}
			GUILayout.BeginVertical(GUI.skin.box, Array.Empty<GUILayoutOption>());
			if (!string.IsNullOrEmpty(this._shortcutsText))
			{
				GUILayout.Label(this._shortcutsText, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
			}
			else
			{
				GUILayout.Label("暂无快捷键信息", new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
			}
			GUILayout.EndVertical();
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00008844 File Offset: 0x00006A44
		private void DrawMultiplierSliderRow(string label, Func<float> getter, Action<float> setter)
		{
			float num = getter();
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label(label, new GUILayoutOption[] { GUILayout.Width(132f) });
			float num2 = GUILayout.HorizontalSlider(num, 1f, 10f, Array.Empty<GUILayoutOption>());
			if (Mathf.Abs(num2 - num) > 0.0001f)
			{
				setter(num2);
				num = getter();
			}
			GUILayout.Space(8f);
			GUILayout.Label(ModBehaviour.FormatMultiplierValue(num), new GUILayoutOption[] { GUILayout.Width(64f) });
			GUILayout.EndHorizontal();
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x000088DB File Offset: 0x00006ADB
		private static string FormatToggleStatus(bool enabled)
		{
			if (!enabled)
			{
				return "<color=#FF6666>关闭</color>";
			}
			return "<color=#5CFF5C>开启</color>";
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x000088EC File Offset: 0x00006AEC
		private void ResetGunInternals(ItemAgent_Gun gun)
		{
			ModBehaviour.TrySetFieldValue(ModBehaviour.ScatterBeforeControlField, gun, 0f);
			ModBehaviour.TrySetFieldValue(ModBehaviour.ScatterFactorHipsField, gun, 0f);
			ModBehaviour.TrySetFieldValue(ModBehaviour.ScatterFactorAdsField, gun, 0f);
			ModBehaviour.TrySetFieldValue(ModBehaviour.RecoilMoveValueField, gun, 0f);
			ModBehaviour.TrySetFieldValue(ModBehaviour.RecoilBackField, gun, false);
			ModBehaviour.TrySetFieldValue(ModBehaviour.RecoilDistanceField, gun, 0f);
			ModBehaviour.TrySetFieldValue(ModBehaviour.RecoilBackSpeedField, gun, 0f);
			ModBehaviour.TrySetFieldValue(ModBehaviour.RecoilRecoverSpeedField, gun, 0f);
		}

		// Token: 0x060000AA RID: 170 RVA: 0x000089A0 File Offset: 0x00006BA0
		private void RestoreGunAmmo(ItemAgent_Gun gun)
		{
			ItemSetting_Gun gunItemSetting = gun.GunItemSetting;
			if (gunItemSetting == null)
			{
				return;
			}
			int bulletCount = gunItemSetting.BulletCount;
			int num = Mathf.Max(0, gunItemSetting.Capacity);
			int num2 = bulletCount + 1;
			if (num > 0)
			{
				num2 = Mathf.Clamp(num2, 0, num);
			}
			int num3 = num2;
			if (this._infiniteAmmoEnabled)
			{
				int minimumAmmoReserve = this.GetMinimumAmmoReserve(gun, gunItemSetting);
				if (minimumAmmoReserve > num3)
				{
					num3 = minimumAmmoReserve;
				}
			}
			this.ApplyBulletCount(gunItemSetting, num2);
			this.EnsureMagazineStack(gun, gunItemSetting, num3);
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00008A0E File Offset: 0x00006C0E
		private int GetMinimumAmmoReserve(ItemAgent_Gun gun, ItemSetting_Gun setting)
		{
			if (!ModBehaviour.RequiresExplosiveReserve(gun, setting))
			{
				return 0;
			}
			return 10;
		}

		// Token: 0x060000AC RID: 172 RVA: 0x00008A20 File Offset: 0x00006C20
		private static bool RequiresExplosiveReserve(ItemAgent_Gun gun, ItemSetting_Gun setting)
		{
			if (gun == null || setting == null)
			{
				return false;
			}
			if (setting.Capacity > 1)
			{
				return false;
			}
			Projectile bulletPfb = setting.bulletPfb;
			return !(bulletPfb == null) && ModBehaviour.IsExplosiveProjectile(bulletPfb);
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00008A64 File Offset: 0x00006C64
		private void ApplyBulletCount(ItemSetting_Gun setting, int desiredCount)
		{
			if (ModBehaviour.GunBulletCountProperty == null)
			{
				return;
			}
			try
			{
				ModBehaviour.GunBulletCountProperty.SetValue(setting, desiredCount, null);
			}
			catch (Exception)
			{
			}
			bool infiniteAmmoEnabled = this._infiniteAmmoEnabled;
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00008AB0 File Offset: 0x00006CB0
		private void EnsureMagazineStack(ItemAgent_Gun gun, ItemSetting_Gun setting, int desiredCount)
		{
			if (setting == null)
			{
				return;
			}
			Inventory inventory = ((setting.Item != null) ? setting.Item.Inventory : null);
			if (inventory == null)
			{
				return;
			}
			Item item = null;
			foreach (Item item2 in inventory)
			{
				if (item2 != null)
				{
					item = item2;
					break;
				}
			}
			if (item == null)
			{
				this.ScheduleMagazineRestore(gun, setting, desiredCount);
				return;
			}
			if (item.StackCount < desiredCount)
			{
				item.StackCount = desiredCount;
			}
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00008B54 File Offset: 0x00006D54
		private void ScheduleMagazineRestore(ItemAgent_Gun gun, ItemSetting_Gun setting, int desiredCount)
		{
			if (!this._infiniteAmmoEnabled || setting == null)
			{
				return;
			}
			int instanceID = setting.GetInstanceID();
			if (!this._pendingMagazineRestores.Add(instanceID))
			{
				return;
			}
			this.RestoreMagazineBulletAsync(instanceID, gun, setting, desiredCount).Forget();
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00008B98 File Offset: 0x00006D98
		private async UniTask RestoreMagazineBulletAsync(int settingId, ItemAgent_Gun gun, ItemSetting_Gun setting, int desiredCount)
		{
			try
			{
				if (this._infiniteAmmoEnabled)
				{
					if (!(setting == null))
					{
						int targetBulletId = setting.TargetBulletID;
						if (targetBulletId < 0 && gun != null)
						{
							Item bulletItem = gun.BulletItem;
							if (bulletItem != null)
							{
								targetBulletId = bulletItem.TypeID;
							}
						}
						if (targetBulletId >= 0)
						{
							Item bullet = null;
							try
							{
								Item item = await ItemAssetsCollection.InstantiateAsync(targetBulletId);
								bullet = item;
							}
							catch (Exception ex)
							{
								Debug.LogWarning(string.Format("{0} Failed to instantiate bullet type {1} for {2}: {3}", new object[] { "[CheatMenu]", targetBulletId, setting.name, ex }));
							}
							if (!(bullet == null))
							{
								bullet.StackCount = Mathf.Max(desiredCount, 1);
								bullet.Inspected = true;
								Inventory inventory = ((setting.Item != null) ? setting.Item.Inventory : null);
								if (inventory == null)
								{
									bullet.DestroyTree();
								}
								else
								{
									inventory.AddAndMerge(bullet, 0);
									Item currentLoadedBullet = setting.GetCurrentLoadedBullet();
									if (currentLoadedBullet != null && currentLoadedBullet.StackCount < desiredCount)
									{
										currentLoadedBullet.StackCount = desiredCount;
									}
									bullet = null;
								}
							}
						}
					}
				}
			}
			finally
			{
				this._pendingMagazineRestores.Remove(settingId);
			}
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00008BFC File Offset: 0x00006DFC
		private void TryAutoAimShot(ItemAgent_Gun gun, [Nullable(2)] Projectile immediateProjectile)
		{
			if (!this._autoAimEnabled || gun == null)
			{
				return;
			}
			this.CleanupRetargetedProjectilesIfNeeded();
			this._applyPenetrationThisShot = false;
			CharacterMainControl holder = gun.Holder;
			if (holder == null)
			{
				ModBehaviour.AutoAimLog("Skipped auto-aim: gun holder missing");
				return;
			}
			LevelManager instance = LevelManager.Instance;
			if (!LevelManager.LevelInited || instance == null || instance.GameCamera == null || instance.GameCamera.renderCamera == null)
			{
				ModBehaviour.AutoAimLog("Skipped auto-aim: level not initialized");
				return;
			}
			Camera renderCamera = instance.GameCamera.renderCamera;
			InputManager inputManager = instance.InputManager;
			Vector2 vector = ((inputManager != null) ? inputManager.AimScreenPoint : this._latestAimScreenPoint);
			Ray ray = renderCamera.ScreenPointToRay(new Vector3(vector.x, vector.y, 0f));
			Vector3 vector2 = ((gun.muzzle != null) ? gun.muzzle.position : gun.transform.position);
			float num = Mathf.Max(2f, gun.BulletDistance);
			bool spinKillEnabled = this._spinKillEnabled;
			ModBehaviour.AutoAimCandidate autoAimCandidate;
			bool flag = this.TryGetSpinKillAutoAimCandidate(holder, renderCamera, vector, ray, vector2, out autoAimCandidate);
			if (!flag)
			{
				if (spinKillEnabled)
				{
					bool flag2 = this._obstaclePenetrationEnabled && this._penetratingProjectiles.Count > 0;
					this.SetPenetrationActive(flag2);
					this._applyPenetrationThisShot = flag2;
					if (flag2)
					{
						if (immediateProjectile != null)
						{
							this.TryApplyObstaclePenetration(immediateProjectile);
						}
						this.EnsureShotPenetration(holder);
					}
					ModBehaviour.AutoAimLog("SpinKill radius limit: no target within range");
					return;
				}
				if (!this.TryAcquireThreadCandidate(holder.Team, renderCamera, vector2, ray, vector, num, out autoAimCandidate))
				{
					bool flag3 = this._obstaclePenetrationEnabled && this._penetratingProjectiles.Count > 0;
					this.SetPenetrationActive(flag3);
					this._applyPenetrationThisShot = flag3;
					if (flag3)
					{
						if (immediateProjectile != null)
						{
							this.TryApplyObstaclePenetration(immediateProjectile);
						}
						this.EnsureShotPenetration(holder);
					}
					ModBehaviour.AutoAimLog("No auto-aim target available for " + gun.name);
					return;
				}
			}
			bool flag4 = this._spinKillEnabled && this._spinKillCurrentTarget != null;
			bool flag5 = autoAimCandidate.Receiver != null;
			bool flag6 = this._obstaclePenetrationEnabled && (flag4 || flag5 || this._penetratingProjectiles.Count > 0);
			this.SetPenetrationActive(flag6);
			this._applyPenetrationThisShot = this._obstaclePenetrationEnabled && (flag4 || flag5);
			Projectile projectile = this.RetargetImmediateProjectile(gun, immediateProjectile, vector2, autoAimCandidate.AimPoint);
			if (projectile == null)
			{
				this.CleanupRetargetedProjectilesIfNeeded();
				projectile = this.ApplyAutoAimToProjectiles(gun, immediateProjectile, vector2, autoAimCandidate.AimPoint);
			}
			if (this._applyPenetrationThisShot)
			{
				this.EnsureShotPenetration(holder);
			}
			if (projectile == null)
			{
				ModBehaviour.AutoAimLog(string.Format("No projectiles retargeted for {0} toward {1}", gun.name, autoAimCandidate.AimPoint));
				return;
			}
			string text = (flag ? "SpinKill" : "Auto-aim");
			string text2 = "{0} applied: gun={1}, target={2}, aimPoint={3}, screenDist={4:F1}, rayDist={5:F1}";
			object[] array = new object[6];
			array[0] = text;
			array[1] = gun.name;
			int num2 = 2;
			DamageReceiver receiver = autoAimCandidate.Receiver;
			array[num2] = ((receiver != null) ? receiver.name : null) ?? "?";
			array[3] = autoAimCandidate.AimPoint;
			array[4] = autoAimCandidate.ScreenDistance;
			array[5] = autoAimCandidate.RayDistance;
			ModBehaviour.AutoAimLog(string.Format(text2, array));
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00008F54 File Offset: 0x00007154
		[NullableContext(2)]
		private void EnsureShotPenetration(CharacterMainControl shooter)
		{
			if (!this._obstaclePenetrationEnabled || !this._penetrationActive)
			{
				return;
			}
			if (!this._applyPenetrationThisShot && this._penetratingProjectiles.Count == 0)
			{
				return;
			}
			if (shooter == null)
			{
				return;
			}
			if (this._activeProjectiles.Count == 0)
			{
				return;
			}
			for (int i = 0; i < this._activeProjectiles.Count; i++)
			{
				Projectile projectile = this._activeProjectiles[i];
				if (!(projectile == null) && !(projectile.context.fromCharacter != shooter))
				{
					this.TryApplyObstaclePenetration(projectile);
				}
			}
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00008FE8 File Offset: 0x000071E8
		[return: Nullable(2)]
		private Projectile CaptureImmediateProjectile(ItemAgent_Gun gun)
		{
			if (ModBehaviour.GunProjectileField == null)
			{
				return null;
			}
			Projectile projectile2;
			try
			{
				Projectile projectile = ModBehaviour.GunProjectileField.GetValue(gun) as Projectile;
				if (projectile == null)
				{
					projectile2 = null;
				}
				else
				{
					this.EnsureProjectilePoolTracked(projectile);
					this.EnsureProjectileTracker(projectile);
					projectile2 = projectile;
				}
			}
			catch
			{
				projectile2 = null;
			}
			return projectile2;
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x0000904C File Offset: 0x0000724C
		private void EnsureProjectilePoolTracked(Projectile projectile)
		{
			if (projectile == null)
			{
				return;
			}
			Transform parent = projectile.transform.parent;
			if (parent == null)
			{
				return;
			}
			if (parent.GetComponent<ModBehaviour.ProjectileTrackerMarker>() != null)
			{
				return;
			}
			parent.gameObject.AddComponent<ModBehaviour.ProjectileTrackerMarker>();
			Projectile[] componentsInChildren = parent.GetComponentsInChildren<Projectile>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				this.EnsureProjectileTracker(componentsInChildren[i]);
			}
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x000090B3 File Offset: 0x000072B3
		private void EnsureProjectileTracker(Projectile projectile)
		{
			if (projectile == null)
			{
				return;
			}
			if (projectile.GetComponent<ModBehaviour.ProjectileTracker>() != null)
			{
				return;
			}
			projectile.gameObject.AddComponent<ModBehaviour.ProjectileTracker>();
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x000090DC File Offset: 0x000072DC
		private void RegisterActiveProjectile(Projectile projectile)
		{
			if (projectile == null)
			{
				return;
			}
			int instanceID = projectile.GetInstanceID();
			if (this._activeProjectileIds.Add(instanceID))
			{
				this._activeProjectiles.Add(projectile);
			}
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00009114 File Offset: 0x00007314
		private void UnregisterActiveProjectile(Projectile projectile)
		{
			if (projectile == null)
			{
				return;
			}
			int instanceID = projectile.GetInstanceID();
			if (this._activeProjectileIds.Remove(instanceID))
			{
				this._activeProjectiles.Remove(projectile);
			}
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00009150 File Offset: 0x00007350
		private void RegisterActiveGrenade(Grenade grenade)
		{
			if (grenade == null)
			{
				return;
			}
			if (!grenade.gameObject.activeInHierarchy || !grenade.gameObject.scene.IsValid())
			{
				return;
			}
			int instanceID = grenade.GetInstanceID();
			if (this._activeGrenadeIds.Add(instanceID))
			{
				this._activeGrenades.Add(grenade);
			}
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x000091AC File Offset: 0x000073AC
		private void UnregisterActiveGrenade(Grenade grenade)
		{
			if (grenade == null)
			{
				return;
			}
			int instanceID = grenade.GetInstanceID();
			if (this._activeGrenadeIds.Remove(instanceID))
			{
				this._activeGrenades.Remove(grenade);
			}
			this._registeredGrenades.Remove(instanceID);
		}

		// Token: 0x060000BA RID: 186 RVA: 0x000091F4 File Offset: 0x000073F4
		[NullableContext(2)]
		private void CleanupUnusedHeadshotOffsets(HashSet<int> seenReceivers)
		{
			if (this._headshotOffsets.Count == 0)
			{
				return;
			}
			if (seenReceivers == null || seenReceivers.Count == 0)
			{
				this._headshotOffsets.Clear();
				return;
			}
			List<int> list = null;
			foreach (int num in this._headshotOffsets.Keys)
			{
				if (!seenReceivers.Contains(num))
				{
					if (list == null)
					{
						list = new List<int>();
					}
					list.Add(num);
				}
			}
			if (list != null)
			{
				foreach (int num2 in list)
				{
					this._headshotOffsets.Remove(num2);
				}
			}
		}

		// Token: 0x060000BB RID: 187 RVA: 0x000092D0 File Offset: 0x000074D0
		private bool HasLineOfSight(Vector3 origin, Vector3 target, DamageReceiver receiver, out float obstructionDistance)
		{
			obstructionDistance = 0f;
			Vector3 vector = target - origin;
			float magnitude = vector.magnitude;
			if (magnitude <= 0.2f)
			{
				return true;
			}
			vector.Normalize();
			RaycastHit raycastHit;
			if (!Physics.Raycast(origin, vector, out raycastHit, magnitude - 0.2f, ModBehaviour.ObstacleLayerMaskValue, QueryTriggerInteraction.Ignore))
			{
				return true;
			}
			DamageReceiver componentInParent = raycastHit.collider.GetComponentInParent<DamageReceiver>();
			if (componentInParent != null && componentInParent == receiver)
			{
				return true;
			}
			obstructionDistance = raycastHit.distance;
			return false;
		}

		// Token: 0x060000BC RID: 188 RVA: 0x0000934C File Offset: 0x0000754C
		[NullableContext(2)]
		private void ApplyInvincibility(CharacterMainControl character)
		{
			if (!this._invincibilityEnabled)
			{
				this.RestoreCharacterInvincibility();
				return;
			}
			if (character == null)
			{
				return;
			}
			Health health = character.Health;
			if (health == null)
			{
				return;
			}
			if (this._invincibilityTargetHealth != null && this._invincibilityTargetHealth != health)
			{
				this.RestoreCharacterInvincibility();
			}
			if (this._originalCharacterInvincible == null)
			{
				this._originalCharacterInvincible = new bool?(health.Invincible);
			}
			if (!health.Invincible)
			{
				health.SetInvincible(true);
			}
			this._invincibilityTargetHealth = health;
		}

		// Token: 0x060000BD RID: 189 RVA: 0x000093DC File Offset: 0x000075DC
		private void RestoreCharacterInvincibility()
		{
			if (this._invincibilityTargetHealth != null && this._originalCharacterInvincible != null)
			{
				this._invincibilityTargetHealth.SetInvincible(this._originalCharacterInvincible.Value);
			}
			this._invincibilityTargetHealth = null;
			this._originalCharacterInvincible = null;
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00009430 File Offset: 0x00007630
		private void LoadSettings()
		{
			this._autoAimEnabled = ModBehaviour.LoadBoolSetting("AutoAimEnabled", this._autoAimEnabled);
			this._grenadeAutoAimEnabled = ModBehaviour.LoadBoolSetting("GrenadeAutoAimEnabled", this._grenadeAutoAimEnabled);
			this._rangeVisible = ModBehaviour.LoadBoolSetting("RangeVisible", this._rangeVisible);
			this._noRecoilEnabled = ModBehaviour.LoadBoolSetting("NoRecoilEnabled", this._noRecoilEnabled);
			this._infiniteAmmoEnabled = ModBehaviour.LoadBoolSetting("InfiniteAmmoEnabled", this._infiniteAmmoEnabled);
			this._infiniteDurabilityEnabled = ModBehaviour.LoadBoolSetting("InfiniteDurabilityEnabled", this._infiniteDurabilityEnabled);
			this._movementUnlockEnabled = ModBehaviour.LoadBoolSetting("MovementUnlockEnabled", this._movementUnlockEnabled);
			this._obstaclePenetrationEnabled = ModBehaviour.LoadBoolSetting("ObstaclePenetrationEnabled", this._obstaclePenetrationEnabled);
			this._invincibilityEnabled = ModBehaviour.LoadBoolSetting("InvincibilityEnabled", this._invincibilityEnabled);
			this._noSurvivalNeedsEnabled = ModBehaviour.LoadBoolSetting("NoSurvivalNeedsEnabled", this._noSurvivalNeedsEnabled);
			this._infiniteStaminaEnabled = ModBehaviour.LoadBoolSetting("InfiniteStaminaEnabled", this._infiniteStaminaEnabled);
			this._menuVisible = ModBehaviour.LoadBoolSetting("MenuVisible", this._menuVisible);
			bool flag = ModBehaviour.LoadBoolSetting("SpinKillEnabled", this._spinKillEnabled);
			this.SetSpinKillEnabled(flag, false, true);
			this._rangeMultiplier = ModBehaviour.LoadFloatSetting("RangeMultiplier", this._rangeMultiplier, 1f, 10f);
			this._fireRateMultiplier = ModBehaviour.LoadFloatSetting("FireRateMultiplier", this._fireRateMultiplier, 1f, 10f);
			this._bulletSpeedMultiplier = ModBehaviour.LoadFloatSetting("BulletSpeedMultiplier", this._bulletSpeedMultiplier, 1f, 10f);
			this._damageMultiplier = ModBehaviour.LoadFloatSetting("DamageMultiplier", this._damageMultiplier, 1f, 10f);
			this._autoAimScreenRadius = ModBehaviour.LoadFloatSetting("AutoAimScreenRadius", this._autoAimScreenRadius, 60f, 2048f);
			this._spinKillSearchRadius = ModBehaviour.LoadFloatSetting("SpinKillSearchRadius", this._spinKillSearchRadius, 5f, 120f);
			this._spinKillAngularSpeed = ModBehaviour.LoadFloatSetting("SpinKillAngularSpeed", this._spinKillAngularSpeed, 180f, 14400f);
		}

		// Token: 0x060000BF RID: 191 RVA: 0x00009640 File Offset: 0x00007840
		private void SaveSettings()
		{
			ModBehaviour.SaveBoolSetting("AutoAimEnabled", this._autoAimEnabled);
			ModBehaviour.SaveBoolSetting("GrenadeAutoAimEnabled", this._grenadeAutoAimEnabled);
			ModBehaviour.SaveBoolSetting("RangeVisible", this._rangeVisible);
			ModBehaviour.SaveBoolSetting("NoRecoilEnabled", this._noRecoilEnabled);
			ModBehaviour.SaveBoolSetting("InfiniteAmmoEnabled", this._infiniteAmmoEnabled);
			ModBehaviour.SaveBoolSetting("InfiniteDurabilityEnabled", this._infiniteDurabilityEnabled);
			ModBehaviour.SaveBoolSetting("MovementUnlockEnabled", this._movementUnlockEnabled);
			bool flag = (this._spinKillForcedPenetration ? this._spinKillPreviousPenetrationEnabled : this._obstaclePenetrationEnabled);
			ModBehaviour.SaveBoolSetting("ObstaclePenetrationEnabled", flag);
			ModBehaviour.SaveBoolSetting("InvincibilityEnabled", this._invincibilityEnabled);
			ModBehaviour.SaveBoolSetting("NoSurvivalNeedsEnabled", this._noSurvivalNeedsEnabled);
			ModBehaviour.SaveBoolSetting("InfiniteStaminaEnabled", this._infiniteStaminaEnabled);
			ModBehaviour.SaveBoolSetting("SpinKillEnabled", this._spinKillEnabled);
			ModBehaviour.SaveBoolSetting("MenuVisible", this._menuVisible);
			ModBehaviour.SaveFloatSetting("RangeMultiplier", this._rangeMultiplier);
			ModBehaviour.SaveFloatSetting("FireRateMultiplier", this._fireRateMultiplier);
			ModBehaviour.SaveFloatSetting("BulletSpeedMultiplier", this._bulletSpeedMultiplier);
			ModBehaviour.SaveFloatSetting("DamageMultiplier", this._damageMultiplier);
			ModBehaviour.SaveFloatSetting("AutoAimScreenRadius", this._autoAimScreenRadius);
			ModBehaviour.SaveFloatSetting("SpinKillSearchRadius", this._spinKillSearchRadius);
			ModBehaviour.SaveFloatSetting("SpinKillAngularSpeed", this._spinKillAngularSpeed);
			PlayerPrefs.Save();
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x000097A4 File Offset: 0x000079A4
		private static bool ReceiverHasHealth(DamageReceiver receiver)
		{
			if (receiver.useSimpleHealth)
			{
				return receiver.simpleHealth != null;
			}
			return receiver.health != null;
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x000097C8 File Offset: 0x000079C8
		private static bool LoadBoolSetting(string name, bool defaultValue)
		{
			int num = ((defaultValue > false) ? 1 : 0);
			return PlayerPrefs.GetInt(ModBehaviour.GetSettingKey(name), num) != 0;
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x000097E9 File Offset: 0x000079E9
		private static float LoadFloatSetting(string name, float defaultValue, float min, float max)
		{
			return Mathf.Clamp(PlayerPrefs.GetFloat(ModBehaviour.GetSettingKey(name), defaultValue), min, max);
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x000097FE File Offset: 0x000079FE
		private static void SaveBoolSetting(string name, bool value)
		{
			PlayerPrefs.SetInt(ModBehaviour.GetSettingKey(name), (value > false) ? 1 : 0);
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x0000980F File Offset: 0x00007A0F
		private static void SaveFloatSetting(string name, float value)
		{
			PlayerPrefs.SetFloat(ModBehaviour.GetSettingKey(name), value);
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x0000981D File Offset: 0x00007A1D
		private static string GetSettingKey(string name)
		{
			return "CheatMenu." + name;
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x0000982C File Offset: 0x00007A2C
		private Vector3 GetReceiverAimPoint(DamageReceiver receiver)
		{
			ModBehaviour.HeadshotInfo headshotInfo = this.GetHeadshotInfo(receiver);
			Collider collider;
			if (receiver.TryGetComponent<Collider>(out collider))
			{
				return collider.bounds.center;
			}
			if (headshotInfo.HasHead)
			{
				return headshotInfo.HeadPosition;
			}
			return receiver.transform.position + Vector3.up * 0.5f;
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00009888 File Offset: 0x00007A88
		private int GetDamageReceiversSnapshot(Ray aimRay, Camera camera, Vector2 aimScreenPoint, float screenRadiusPixels, float maxRange)
		{
			if (camera == null)
			{
				return 0;
			}
			if (maxRange <= 0f)
			{
				return 0;
			}
			Vector3 vector = ((aimRay.direction.sqrMagnitude > 0.0001f) ? aimRay.direction.normalized : Vector3.forward);
			float num = maxRange * 0.5f;
			Vector3 vector2 = aimRay.origin + vector * num;
			float num2 = num;
			if (screenRadiusPixels > 0f)
			{
				float num3 = this.ComputeAimCircleWorldRadius(camera, aimRay, aimScreenPoint, screenRadiusPixels, maxRange);
				float num4 = Mathf.Sqrt(num3 * num3 + num * num);
				num2 = Mathf.Max(num2, num4);
			}
			Vector3 vector3 = vector2;
			float num5 = num2;
			int frameCount = Time.frameCount;
			if (this._damageReceiverCacheFrame != frameCount || (this._damageReceiverCacheCenter - vector3).sqrMagnitude > 0.01f || Mathf.Abs(this._damageReceiverCacheRadius - num5) > 0.01f)
			{
				int num6 = 0;
				if (num5 > 0f)
				{
					int num7;
					for (;;)
					{
						num7 = Physics.OverlapSphereNonAlloc(vector3, num5, this._damageReceiverColliderBuffer, this._damageReceiverLayerMask, QueryTriggerInteraction.Collide);
						if (num7 < this._damageReceiverColliderBuffer.Length)
						{
							break;
						}
						this.EnsureDamageReceiverColliderCapacity(this._damageReceiverColliderBuffer.Length * 2);
					}
					if (num7 > 0)
					{
						this.EnsureDamageReceiverCacheCapacity(num7);
						for (int i = 0; i < num7; i++)
						{
							Collider collider = this._damageReceiverColliderBuffer[i];
							this._damageReceiverColliderBuffer[i] = null;
							DamageReceiver damageReceiver;
							if (!(collider == null) && collider.TryGetComponent<DamageReceiver>(out damageReceiver))
							{
								this._damageReceiverCache[num6++] = damageReceiver;
							}
						}
					}
				}
				for (int j = num6; j < this._damageReceiverCacheCount; j++)
				{
					this._damageReceiverCache[j] = null;
				}
				this._damageReceiverCacheCount = num6;
				this._damageReceiverCacheFrame = frameCount;
				this._damageReceiverCacheCenter = vector3;
				this._damageReceiverCacheRadius = num5;
			}
			return this._damageReceiverCacheCount;
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00009A54 File Offset: 0x00007C54
		private float ComputeAimCircleWorldRadius(Camera camera, Ray centerRay, Vector2 aimScreenPoint, float screenRadiusPixels, float distance)
		{
			ModBehaviour.<>c__DisplayClass424_0 CS$<>8__locals1;
			CS$<>8__locals1.aimScreenPoint = aimScreenPoint;
			CS$<>8__locals1.camera = camera;
			CS$<>8__locals1.distance = distance;
			if (CS$<>8__locals1.camera == null || screenRadiusPixels <= 0f || CS$<>8__locals1.distance <= 0f)
			{
				return 0f;
			}
			CS$<>8__locals1.centerPoint = centerRay.origin + centerRay.direction * CS$<>8__locals1.distance;
			CS$<>8__locals1.maxRadius = 0f;
			ModBehaviour.<ComputeAimCircleWorldRadius>g__SampleOffset|424_0(screenRadiusPixels, 0f, ref CS$<>8__locals1);
			ModBehaviour.<ComputeAimCircleWorldRadius>g__SampleOffset|424_0(0f, screenRadiusPixels, ref CS$<>8__locals1);
			ModBehaviour.<ComputeAimCircleWorldRadius>g__SampleOffset|424_0(-screenRadiusPixels, 0f, ref CS$<>8__locals1);
			ModBehaviour.<ComputeAimCircleWorldRadius>g__SampleOffset|424_0(0f, -screenRadiusPixels, ref CS$<>8__locals1);
			ModBehaviour.<ComputeAimCircleWorldRadius>g__SampleOffset|424_0(screenRadiusPixels * 0.7071f, screenRadiusPixels * 0.7071f, ref CS$<>8__locals1);
			ModBehaviour.<ComputeAimCircleWorldRadius>g__SampleOffset|424_0(-screenRadiusPixels * 0.7071f, screenRadiusPixels * 0.7071f, ref CS$<>8__locals1);
			ModBehaviour.<ComputeAimCircleWorldRadius>g__SampleOffset|424_0(screenRadiusPixels * 0.7071f, -screenRadiusPixels * 0.7071f, ref CS$<>8__locals1);
			ModBehaviour.<ComputeAimCircleWorldRadius>g__SampleOffset|424_0(-screenRadiusPixels * 0.7071f, -screenRadiusPixels * 0.7071f, ref CS$<>8__locals1);
			return CS$<>8__locals1.maxRadius;
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00009B78 File Offset: 0x00007D78
		private void EnsureDamageReceiverCacheCapacity(int required)
		{
			if (required <= 0)
			{
				return;
			}
			if (this._damageReceiverCache.Length >= required)
			{
				return;
			}
			int i;
			for (i = ((this._damageReceiverCache.Length == 0) ? 16 : this._damageReceiverCache.Length); i < required; i *= 2)
			{
			}
			Array.Resize<DamageReceiver>(ref this._damageReceiverCache, i);
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00009BC4 File Offset: 0x00007DC4
		private void EnsureDamageReceiverColliderCapacity(int required)
		{
			if (required <= this._damageReceiverColliderBuffer.Length)
			{
				return;
			}
			int i = this._damageReceiverColliderBuffer.Length;
			if (i == 0)
			{
				i = 16;
			}
			while (i < required)
			{
				i *= 2;
			}
			Array.Resize<Collider>(ref this._damageReceiverColliderBuffer, i);
		}

		// Token: 0x060000CB RID: 203 RVA: 0x00009C04 File Offset: 0x00007E04
		private ModBehaviour.HeadshotInfo GetHeadshotInfo(DamageReceiver receiver)
		{
			Transform transform = ModBehaviour.TryGetHeadTransform(receiver);
			if (transform == null)
			{
				return default(ModBehaviour.HeadshotInfo);
			}
			int instanceID = receiver.GetInstanceID();
			Vector3 vector;
			if (!this._headshotOffsets.TryGetValue(instanceID, out vector))
			{
				vector = new Vector3(Random.Range(-0.03f, 0.03f), Random.Range(0.02f, 0.06f), Random.Range(-0.03f, 0.03f));
				this._headshotOffsets[instanceID] = vector;
			}
			return new ModBehaviour.HeadshotInfo
			{
				HasHead = true,
				HeadPosition = transform.position,
				RandomOffset = vector
			};
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00009CAC File Offset: 0x00007EAC
		[return: Nullable(2)]
		private static Transform TryGetHeadTransform(DamageReceiver receiver)
		{
			HeadCollider componentInChildren = receiver.GetComponentInChildren<HeadCollider>();
			if (componentInChildren != null)
			{
				return componentInChildren.transform;
			}
			if (receiver.health != null)
			{
				CharacterMainControl characterMainControl = receiver.health.TryGetCharacter();
				if (characterMainControl != null)
				{
					if (characterMainControl.characterModel != null)
					{
						if (characterMainControl.characterModel.HelmatSocket != null)
						{
							return characterMainControl.characterModel.HelmatSocket;
						}
						Transform faceMaskSocket = characterMainControl.characterModel.FaceMaskSocket;
						if (faceMaskSocket != null)
						{
							return faceMaskSocket;
						}
					}
					return characterMainControl.transform;
				}
			}
			if (receiver.simpleHealth != null)
			{
				return receiver.simpleHealth.transform;
			}
			return null;
		}

		// Token: 0x060000CD RID: 205 RVA: 0x00009D5C File Offset: 0x00007F5C
		private static Vector3 EvaluateAimPlaneIntersection(Ray aimRay, float planeHeight)
		{
			Plane plane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
			float num;
			if (plane.Raycast(aimRay, out num))
			{
				return aimRay.origin + aimRay.direction * num;
			}
			return aimRay.origin + aimRay.direction.normalized * 5f;
		}

		// Token: 0x060000CE RID: 206 RVA: 0x00009DD0 File Offset: 0x00007FD0
		[NullableContext(2)]
		private Projectile RetargetImmediateProjectile([Nullable(1)] ItemAgent_Gun gun, Projectile projectile, Vector3 muzzlePosition, Vector3 targetPoint)
		{
			if (projectile == null)
			{
				ModBehaviour.AutoAimLog("Immediate projectile not available via event context");
				return null;
			}
			if (!ModBehaviour.IsProjectileActive(projectile))
			{
				ModBehaviour.AutoAimLog("Immediate projectile inactive; skipping retarget");
				return null;
			}
			if (projectile.context.fromCharacter != gun.Holder)
			{
				ModBehaviour.AutoAimLog("Immediate projectile owner mismatch; skipping retarget");
				return null;
			}
			Vector3 vector = targetPoint - muzzlePosition;
			if (vector.sqrMagnitude < 0.0001f)
			{
				return null;
			}
			if (ModBehaviour.GetProjectileTraveledDistance(projectile) > 0.6f)
			{
				ModBehaviour.AutoAimLog("Immediate projectile already traveled; skipping retarget");
				return null;
			}
			if (!this.TryRetargetProjectile(projectile, vector, targetPoint, true))
			{
				ModBehaviour.AutoAimLog("Failed to retarget immediate projectile");
				return null;
			}
			int instanceID = projectile.GetInstanceID();
			this._retargetedProjectiles.Add(instanceID);
			this._retargetedProjectileRefs[instanceID] = projectile;
			ModBehaviour.AutoAimLog(string.Format("Retargeted immediate projectile {0} toward {1}", projectile.name, targetPoint));
			return projectile;
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00009EB4 File Offset: 0x000080B4
		[NullableContext(2)]
		private Projectile ApplyAutoAimToProjectiles([Nullable(1)] ItemAgent_Gun gun, Projectile immediateProjectile, Vector3 muzzlePosition, Vector3 targetPoint)
		{
			this._projectileSnapshotBuffer.Clear();
			CharacterMainControl holder = gun.Holder;
			if (holder == null)
			{
				return null;
			}
			if (this._activeProjectiles.Count == 0)
			{
				return null;
			}
			Vector3 vector = targetPoint - muzzlePosition;
			if (vector.sqrMagnitude < 0.0001f)
			{
				return null;
			}
			vector.Normalize();
			this._projectileCandidates.Clear();
			for (int i = 0; i < this._activeProjectiles.Count; i++)
			{
				Projectile projectile = this._activeProjectiles[i];
				if (projectile != null)
				{
					this._projectileSnapshotBuffer.Add(projectile);
				}
			}
			foreach (Projectile projectile2 in this._projectileSnapshotBuffer)
			{
				if (!(projectile2 == null) && !(projectile2 == immediateProjectile) && ModBehaviour.IsProjectileActive(projectile2) && !(projectile2.context.fromCharacter != holder))
				{
					int instanceID = projectile2.GetInstanceID();
					if (!this._retargetedProjectiles.Contains(instanceID))
					{
						float projectileTraveledDistance = ModBehaviour.GetProjectileTraveledDistance(projectile2);
						if (projectileTraveledDistance <= 0.6f)
						{
							this._projectileCandidates.Add(new ModBehaviour.ProjectileCandidate
							{
								Projectile = projectile2,
								Traveled = projectileTraveledDistance
							});
						}
					}
				}
			}
			if (this._projectileCandidates.Count == 0)
			{
				this._projectileSnapshotBuffer.Clear();
				return null;
			}
			int num = Mathf.Max(1, gun.ShotCount);
			int num2 = 0;
			Projectile projectile3 = null;
			while (num2 < num && this._projectileCandidates.Count > 0)
			{
				int num3 = 0;
				float num4 = this._projectileCandidates[0].Traveled;
				for (int j = 1; j < this._projectileCandidates.Count; j++)
				{
					if (this._projectileCandidates[j].Traveled < num4)
					{
						num4 = this._projectileCandidates[j].Traveled;
						num3 = j;
					}
				}
				ref ModBehaviour.ProjectileCandidate ptr = this._projectileCandidates[num3];
				this._projectileCandidates.RemoveAt(num3);
				Projectile projectile4 = ptr.Projectile;
				int instanceID2 = projectile4.GetInstanceID();
				if (this.TryRetargetProjectile(projectile4, vector, targetPoint, true))
				{
					this._retargetedProjectiles.Add(instanceID2);
					this._retargetedProjectileRefs[instanceID2] = projectile4;
					num2++;
					if (projectile3 == null)
					{
						projectile3 = projectile4;
					}
				}
			}
			this._projectileCandidates.Clear();
			this._projectileSnapshotBuffer.Clear();
			return projectile3;
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x0000A140 File Offset: 0x00008340
		private bool TryRetargetProjectile(Projectile projectile, Vector3 direction, Vector3 targetPoint, bool forceCritical)
		{
			if (projectile == null)
			{
				return false;
			}
			if (direction.sqrMagnitude < 0.0001f)
			{
				return false;
			}
			direction.Normalize();
			bool flag;
			try
			{
				projectile.context.direction = direction;
				projectile.context.firstFrameCheckStartPoint = projectile.transform.position - direction * 0.1f;
				float num = Vector3.Distance(projectile.transform.position, targetPoint) + 2f;
				if (projectile.context.distance < num)
				{
					projectile.context.distance = num;
				}
				if (forceCritical)
				{
					projectile.context.critRate = 1f;
					projectile.context.ignoreHalfObsticle = true;
				}
				if (ModBehaviour.ProjectileDirectionField != null)
				{
					ModBehaviour.ProjectileDirectionField.SetValue(projectile, direction);
				}
				if (ModBehaviour.ProjectileVelocityField != null)
				{
					Vector3 vector = direction * projectile.context.speed;
					ModBehaviour.ProjectileVelocityField.SetValue(projectile, vector);
				}
				projectile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
				if (this._obstaclePenetrationEnabled)
				{
					this.TryApplyObstaclePenetration(projectile);
				}
				flag = true;
			}
			catch (Exception)
			{
				flag = false;
			}
			return flag;
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x0000A280 File Offset: 0x00008480
		private static float GetProjectileTraveledDistance(Projectile projectile)
		{
			if (ModBehaviour.ProjectileTraveledDistanceField == null)
			{
				return 0f;
			}
			float num;
			try
			{
				num = (float)ModBehaviour.ProjectileTraveledDistanceField.GetValue(projectile);
			}
			catch
			{
				num = 0f;
			}
			return num;
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x0000A2D0 File Offset: 0x000084D0
		private void TryApplyObstaclePenetration(Projectile projectile)
		{
			if (!this._obstaclePenetrationEnabled || !this._penetrationActive)
			{
				return;
			}
			try
			{
				bool flag = false;
				LayerMask layerMask = default(LayerMask);
				int instanceID = projectile.GetInstanceID();
				bool flag2 = this._penetratingProjectiles.Contains(instanceID);
				if (this._applyPenetrationThisShot || flag2)
				{
					bool flag3 = ModBehaviour.IsExplosiveProjectile(projectile);
					if (ModBehaviour.ProjectileHitLayersField != null && !this._originalProjectileMasks.ContainsKey(instanceID))
					{
						object value = ModBehaviour.ProjectileHitLayersField.GetValue(projectile);
						if (value is LayerMask)
						{
							LayerMask layerMask2 = (LayerMask)value;
							layerMask = layerMask2;
							flag = true;
						}
					}
					projectile.context.ignoreHalfObsticle = true;
					if (flag3)
					{
						projectile.context.penetrate = Mathf.Min(projectile.context.penetrate, 0);
					}
					else
					{
						projectile.context.penetrate = Mathf.Max(projectile.context.penetrate, 6);
					}
					this.ConfigureProjectileHitMask(projectile);
					this.RegisterPenetratingProjectile(projectile, flag ? new LayerMask?(layerMask) : null);
					this.EnsureDamagedObjectsInitialized(projectile);
					if (flag3)
					{
						ModBehaviour.ClampExplosiveProjectileState(projectile);
					}
				}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x0000A3F4 File Offset: 0x000085F4
		private static void ClampExplosiveProjectileState(Projectile projectile)
		{
			if (projectile == null)
			{
				return;
			}
			if (projectile.context.penetrate > 0)
			{
				projectile.context.penetrate = 0;
			}
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x0000A41C File Offset: 0x0000861C
		private void ConfigureProjectileHitMask(Projectile projectile)
		{
			if (ModBehaviour.ProjectileHitLayersField == null)
			{
				return;
			}
			try
			{
				int num = GameplayDataSettings.Layers.damageReceiverLayerMask.value;
				int num2 = GameplayDataSettings.Layers.wallLayerMask.value | GameplayDataSettings.Layers.groundLayerMask.value | GameplayDataSettings.Layers.halfObsticleLayer.value;
				num &= ~num2;
				if (num == 0)
				{
					int num3 = LayerMask.NameToLayer("DamageReceiver");
					if (num3 >= 0)
					{
						num |= 1 << num3;
					}
					int num4 = LayerMask.NameToLayer("HeadCollider");
					if (num4 >= 0)
					{
						num |= 1 << num4;
					}
				}
				LayerMask layerMask = new LayerMask
				{
					value = num
				};
				ModBehaviour.ProjectileHitLayersField.SetValue(projectile, layerMask);
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x0000A4EC File Offset: 0x000086EC
		private static bool IsExplosiveProjectile(Projectile projectile)
		{
			if (projectile == null)
			{
				return false;
			}
			ProjectileContext context = projectile.context;
			if (context.explosionRange > 0.01f || context.explosionDamage > 0.01f)
			{
				return true;
			}
			string name = projectile.name;
			return !string.IsNullOrEmpty(name) && name.IndexOf("rocket", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x0000A54C File Offset: 0x0000874C
		private void RegisterPenetratingProjectile(Projectile projectile, LayerMask? originalMask)
		{
			if (projectile == null)
			{
				return;
			}
			int instanceID = projectile.GetInstanceID();
			this._penetratingProjectiles.Add(instanceID);
			this._penetratingProjectileRefs[instanceID] = projectile;
			if (originalMask != null && !this._originalProjectileMasks.ContainsKey(instanceID))
			{
				this._originalProjectileMasks[instanceID] = originalMask.Value;
			}
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x0000A5B0 File Offset: 0x000087B0
		private static bool HasGrenadeRegisteredCollision(Grenade grenade)
		{
			if (ModBehaviour.GrenadeCollideField == null)
			{
				return false;
			}
			bool flag;
			try
			{
				flag = (bool)ModBehaviour.GrenadeCollideField.GetValue(grenade);
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000A5F8 File Offset: 0x000087F8
		private static void SimulateGrenadeCollision(Grenade grenade, [Nullable(2)] Rigidbody rigidbody)
		{
			if (grenade == null)
			{
				return;
			}
			if (ModBehaviour.GrenadeCollideField != null)
			{
				try
				{
					ModBehaviour.GrenadeCollideField.SetValue(grenade, true);
				}
				catch
				{
				}
			}
			if (rigidbody == null)
			{
				return;
			}
			Vector3 velocity = rigidbody.velocity;
			velocity.x *= 0.5f;
			velocity.z *= 0.5f;
			rigidbody.velocity = velocity;
			rigidbody.angularVelocity *= 0.3f;
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000A690 File Offset: 0x00008890
		private static void TryUpdateInputAimPoint(InputManager inputManager, Vector3 aimPoint)
		{
			if (ModBehaviour.InputAimPointField == null)
			{
				return;
			}
			try
			{
				ModBehaviour.InputAimPointField.SetValue(inputManager, aimPoint);
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x060000DA RID: 218 RVA: 0x0000A6D4 File Offset: 0x000088D4
		private static void TrySetAimingEnemyHead(InputManager inputManager, bool value)
		{
			if (ModBehaviour.InputAimingEnemyHeadField == null)
			{
				return;
			}
			try
			{
				ModBehaviour.InputAimingEnemyHeadField.SetValue(inputManager, value);
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000A718 File Offset: 0x00008918
		private void CleanupRetargetedProjectiles()
		{
			if (this._retargetedProjectiles.Count == 0 && this._penetratingProjectiles.Count == 0)
			{
				return;
			}
			if (this._retargetedProjectiles.Count > 0)
			{
				foreach (int num in this._retargetedProjectiles.ToArray<int>())
				{
					Projectile projectile;
					if (!this._retargetedProjectileRefs.TryGetValue(num, out projectile) || !ModBehaviour.IsProjectileActive(projectile))
					{
						this._retargetedProjectiles.Remove(num);
						this._retargetedProjectileRefs.Remove(num);
					}
				}
			}
			if (this._penetratingProjectiles.Count > 0)
			{
				foreach (int num2 in this._penetratingProjectiles.ToArray<int>())
				{
					Projectile projectile2;
					if (!this._penetratingProjectileRefs.TryGetValue(num2, out projectile2) || !ModBehaviour.IsProjectileActive(projectile2))
					{
						this._penetratingProjectiles.Remove(num2);
						this._penetratingProjectileRefs.Remove(num2);
						this._originalProjectileMasks.Remove(num2);
					}
				}
			}
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000A810 File Offset: 0x00008A10
		private void MaintainPenetratingProjectiles()
		{
			if (this._penetratingProjectiles.Count == 0)
			{
				return;
			}
			foreach (int num in this._penetratingProjectiles.ToArray<int>())
			{
				Projectile projectile;
				if (!this._penetratingProjectileRefs.TryGetValue(num, out projectile) || !ModBehaviour.IsProjectileActive(projectile))
				{
					this._penetratingProjectiles.Remove(num);
					this._penetratingProjectileRefs.Remove(num);
					this._originalProjectileMasks.Remove(num);
				}
			}
		}

		// Token: 0x060000DD RID: 221 RVA: 0x0000A888 File Offset: 0x00008A88
		private void SetPenetrationActive(bool active)
		{
			if (this._penetrationActive == active)
			{
				return;
			}
			this._penetrationActive = active;
			if (!active)
			{
				this._applyPenetrationThisShot = false;
				this.RestorePenetratingProjectiles();
			}
		}

		// Token: 0x060000DE RID: 222 RVA: 0x0000A8AC File Offset: 0x00008AAC
		private void RestorePenetratingProjectiles()
		{
			if (this._penetratingProjectiles.Count == 0)
			{
				this._penetratingProjectileRefs.Clear();
				this._originalProjectileMasks.Clear();
				return;
			}
			foreach (int num in this._penetratingProjectiles.ToArray<int>())
			{
				Projectile projectile;
				if (!this._penetratingProjectileRefs.TryGetValue(num, out projectile) || projectile == null)
				{
					this._penetratingProjectiles.Remove(num);
					this._penetratingProjectileRefs.Remove(num);
					this._originalProjectileMasks.Remove(num);
				}
				else
				{
					try
					{
						LayerMask layerMask;
						if (this._originalProjectileMasks.TryGetValue(num, out layerMask) && ModBehaviour.ProjectileHitLayersField != null)
						{
							ModBehaviour.ProjectileHitLayersField.SetValue(projectile, layerMask);
						}
						projectile.context.ignoreHalfObsticle = false;
					}
					catch (Exception)
					{
					}
				}
			}
			this._penetratingProjectiles.Clear();
			this._originalProjectileMasks.Clear();
			this._penetratingProjectileRefs.Clear();
		}

		// Token: 0x060000DF RID: 223 RVA: 0x0000A9B8 File Offset: 0x00008BB8
		private static bool IsProjectileActive(Projectile projectile)
		{
			if (projectile == null)
			{
				return false;
			}
			if (projectile.gameObject == null || !projectile.gameObject.activeInHierarchy)
			{
				return false;
			}
			if (ModBehaviour.ProjectileDeadField != null)
			{
				try
				{
					if ((bool)ModBehaviour.ProjectileDeadField.GetValue(projectile))
					{
						return false;
					}
				}
				catch
				{
				}
				return true;
			}
			return true;
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x0000AA28 File Offset: 0x00008C28
		private void EnsureDamagedObjectsInitialized(Projectile projectile)
		{
			if (ModBehaviour.ProjectileDamagedObjectsField == null)
			{
				return;
			}
			try
			{
				List<GameObject> list = ModBehaviour.ProjectileDamagedObjectsField.GetValue(projectile) as List<GameObject>;
				if (list != null)
				{
					for (int i = list.Count - 1; i >= 0; i--)
					{
						GameObject gameObject = list[i];
						if (gameObject == null)
						{
							list.RemoveAt(i);
						}
						else if (gameObject.GetComponent<DamageReceiver>() == null)
						{
							list.RemoveAt(i);
						}
					}
				}
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000AAB0 File Offset: 0x00008CB0
		private static void AutoAimLog(string message)
		{
			bool enableAutoAimDebugLogs = ModBehaviour.EnableAutoAimDebugLogs;
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000AAB8 File Offset: 0x00008CB8
		private static void TrySetFieldValue([Nullable(2)] FieldInfo field, ItemAgent_Gun gun, object value)
		{
			if (field == null)
			{
				return;
			}
			try
			{
				field.SetValue(gun, value);
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x0000AAF0 File Offset: 0x00008CF0
		private void OverrideItemStat(Item item, int statHash, float newValue)
		{
			if (item == null)
			{
				return;
			}
			Stat stat = item.GetStat(statHash);
			if (stat == null)
			{
				return;
			}
			Dictionary<int, float> dictionary;
			if (!this._originalItemStatValues.TryGetValue(item, out dictionary))
			{
				dictionary = new Dictionary<int, float>();
				this._originalItemStatValues[item] = dictionary;
			}
			if (!dictionary.ContainsKey(statHash))
			{
				dictionary[statHash] = stat.BaseValue;
			}
			stat.BaseValue = newValue;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x0000AB54 File Offset: 0x00008D54
		private void RestoreOriginalCharacterRecoil()
		{
			if (this._originalCharacterRecoilControl == null)
			{
				return;
			}
			CharacterMainControl main = CharacterMainControl.Main;
			if (main == null || main.CharacterItem == null)
			{
				this._originalCharacterRecoilControl = null;
				return;
			}
			Stat stat = main.CharacterItem.GetStat(ModBehaviour.RecoilControlStatHash);
			if (stat != null)
			{
				stat.BaseValue = this._originalCharacterRecoilControl.Value;
			}
			this._originalCharacterRecoilControl = null;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000ABCC File Offset: 0x00008DCC
		[NullableContext(2)]
		private void RestoreItemStatOverridesForItem(Item item)
		{
			if (item == null)
			{
				return;
			}
			Dictionary<int, float> dictionary;
			if (!this._originalItemStatValues.TryGetValue(item, out dictionary))
			{
				return;
			}
			foreach (KeyValuePair<int, float> keyValuePair in dictionary)
			{
				Stat stat = item.GetStat(keyValuePair.Key);
				if (stat != null)
				{
					stat.BaseValue = keyValuePair.Value;
				}
			}
			this._originalItemStatValues.Remove(item);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000AC5C File Offset: 0x00008E5C
		private void RestoreItemStatOverrides()
		{
			if (this._originalItemStatValues.Count == 0)
			{
				return;
			}
			foreach (KeyValuePair<Item, Dictionary<int, float>> keyValuePair in this._originalItemStatValues.ToArray<KeyValuePair<Item, Dictionary<int, float>>>())
			{
				this.RestoreItemStatOverridesForItem(keyValuePair.Key);
			}
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x0000ACA8 File Offset: 0x00008EA8
		private static string GetItemDebugName([Nullable(2)] Item item)
		{
			if (item == null)
			{
				return "<null item>";
			}
			string text = item.TypeID.ToString();
			return item.name + " (TypeID: " + text + ")";
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x0000ACE9 File Offset: 0x00008EE9
		private static string GetCharacterDebugName([Nullable(2)] CharacterMainControl character)
		{
			if (character == null)
			{
				return "<null character>";
			}
			return character.name;
		}

		// Token: 0x060000EF RID: 239 RVA: 0x0000B46C File Offset: 0x0000966C
		[CompilerGenerated]
		internal static void <ComputeAimCircleWorldRadius>g__SampleOffset|424_0(float offsetX, float offsetY, ref ModBehaviour.<>c__DisplayClass424_0 A_2)
		{
			Vector3 vector = new Vector3(A_2.aimScreenPoint.x + offsetX, A_2.aimScreenPoint.y + offsetY, 0f);
			Ray ray = A_2.camera.ScreenPointToRay(vector);
			Vector3 vector2 = ray.origin + ray.direction * A_2.distance;
			float num = Vector3.Distance(A_2.centerPoint, vector2);
			if (num > A_2.maxRadius)
			{
				A_2.maxRadius = num;
			}
		}

		// Token: 0x04000004 RID: 4
		private const string LogPrefix = "[CheatMenu]";

		// Token: 0x04000005 RID: 5
		private static readonly int RecoilControlStatHash = "RecoilControl".GetHashCode();

		// Token: 0x04000006 RID: 6
		private static readonly int[] GunStatHashes = new int[]
		{
			"RecoilVMin".GetHashCode(),
			"RecoilVMax".GetHashCode(),
			"RecoilHMin".GetHashCode(),
			"RecoilHMax".GetHashCode(),
			"RecoilScaleV".GetHashCode(),
			"RecoilScaleH".GetHashCode(),
			"ScatterFactor".GetHashCode(),
			"ScatterFactorADS".GetHashCode(),
			"DefaultScatter".GetHashCode(),
			"DefaultScatterADS".GetHashCode(),
			"MaxScatter".GetHashCode(),
			"MaxScatterADS".GetHashCode(),
			"ScatterGrow".GetHashCode(),
			"ScatterGrowADS".GetHashCode(),
			"ScatterRecover".GetHashCode(),
			"ScatterRecoverADS".GetHashCode(),
			"ShotAngle".GetHashCode()
		};

		// Token: 0x04000007 RID: 7
		private const float NoRecoilControlValue = 9999f;

		// Token: 0x04000008 RID: 8
		private const float NoRecoilStatValue = 0f;

		// Token: 0x04000009 RID: 9
		private readonly Dictionary<Item, Dictionary<int, float>> _originalItemStatValues = new Dictionary<Item, Dictionary<int, float>>();

		// Token: 0x0400000A RID: 10
		private float? _originalCharacterRecoilControl;

		// Token: 0x0400000B RID: 11
		[Nullable(2)]
		private CharacterMainControl _trackedCharacter;

		// Token: 0x0400000C RID: 12
		private bool _characterEventsHooked;

		// Token: 0x0400000D RID: 13
		[Nullable(2)]
		private ItemAgent_Gun _trackedGun;

		// Token: 0x0400000E RID: 14
		[Nullable(2)]
		private ItemAgentHolder _trackedAgentHolder;

		// Token: 0x0400000F RID: 15
		private bool _agentHolderEventsHooked;

		// Token: 0x04000010 RID: 16
		private bool _lastTriggerInput;

		// Token: 0x04000011 RID: 17
		private const int AutoAimCircleSegments = 64;

		// Token: 0x04000012 RID: 18
		private const KeyCode MenuToggleKey = KeyCode.F8;

		// Token: 0x04000013 RID: 19
		private const KeyCode RangeToggleKey = KeyCode.F9;

		// Token: 0x04000014 RID: 20
		private const KeyCode ToggleAutoAimKey = KeyCode.F1;

		// Token: 0x04000015 RID: 21
		private const KeyCode ToggleNoRecoilKey = KeyCode.F2;

		// Token: 0x04000016 RID: 22
		private const KeyCode ToggleInfiniteAmmoKey = KeyCode.F3;

		// Token: 0x04000017 RID: 23
		private const KeyCode ToggleMovementUnlockKey = KeyCode.F4;

		// Token: 0x04000018 RID: 24
		private const KeyCode ToggleObstaclePenetrationKey = KeyCode.F5;

		// Token: 0x04000019 RID: 25
		private const KeyCode ToggleInvincibilityKey = KeyCode.F6;

		// Token: 0x0400001A RID: 26
		private const KeyCode ToggleInfiniteDurabilityKey = KeyCode.F7;

		// Token: 0x0400001B RID: 27
		private const KeyCode ToggleNoSurvivalNeedsKey = KeyCode.F10;

		// Token: 0x0400001C RID: 28
		private const KeyCode ToggleSpinKillKey = KeyCode.F11;

		// Token: 0x0400001D RID: 29
		private const string ShortcutModifierLabel = "Ctrl";

		// Token: 0x0400001E RID: 30
		private const string SettingsKeyPrefix = "CheatMenu.";

		// Token: 0x0400001F RID: 31
		private readonly Vector3[] _autoAimRangePoints = new Vector3[64];

		// Token: 0x04000020 RID: 32
		private readonly Vector3[] _spinKillRangePoints = new Vector3[96];

		// Token: 0x04000021 RID: 33
		private bool _menuVisible = true;

		// Token: 0x04000022 RID: 34
		private bool _rangeVisible = true;

		// Token: 0x04000023 RID: 35
		private bool _autoAimEnabled = true;

		// Token: 0x04000024 RID: 36
		private bool _grenadeAutoAimEnabled = true;

		// Token: 0x04000025 RID: 37
		private bool _noRecoilEnabled = true;

		// Token: 0x04000026 RID: 38
		private bool _infiniteAmmoEnabled = true;

		// Token: 0x04000027 RID: 39
		private bool _infiniteThrowablesEnabled = true;

		// Token: 0x04000028 RID: 40
		private bool _movementUnlockEnabled = true;

		// Token: 0x04000029 RID: 41
		private bool _obstaclePenetrationEnabled = true;

		// Token: 0x0400002A RID: 42
		private bool _invincibilityEnabled = true;

		// Token: 0x0400002B RID: 43
		private bool _infiniteStaminaEnabled;

		// Token: 0x0400002C RID: 44
		private bool _infiniteDurabilityEnabled = true;

		// Token: 0x0400002D RID: 45
		private bool _noSurvivalNeedsEnabled = true;

		// Token: 0x0400002E RID: 46
		private bool _spinKillEnabled;

		// Token: 0x0400002F RID: 47
		private float _rangeMultiplier = 1f;

		// Token: 0x04000030 RID: 48
		private float _fireRateMultiplier = 1f;

		// Token: 0x04000031 RID: 49
		private float _bulletSpeedMultiplier = 1f;

		// Token: 0x04000032 RID: 50
		private float _damageMultiplier = 1f;

		// Token: 0x04000033 RID: 51
		[Nullable(2)]
		private GameObject _overlayCanvasObject;

		// Token: 0x04000034 RID: 52
		[Nullable(2)]
		private RectTransform _menuPanel;

		// Token: 0x04000035 RID: 53
		[Nullable(2)]
		private RectTransform _menuRoot;

		// Token: 0x04000036 RID: 54
		[Nullable(2)]
		private RectTransform _sidebarContainer;

		// Token: 0x04000037 RID: 55
		[Nullable(2)]
		private RectTransform _contentContainer;

		// Token: 0x04000038 RID: 56
		[Nullable(2)]
		private RectTransform _sliderContainer;

		// Token: 0x04000039 RID: 57
		private readonly Dictionary<ModBehaviour.MenuSection, Button> _menuSectionButtons = new Dictionary<ModBehaviour.MenuSection, Button>();

		// Token: 0x0400003A RID: 58
		private readonly Dictionary<ModBehaviour.MenuSection, GameObject> _menuSectionRoots = new Dictionary<ModBehaviour.MenuSection, GameObject>();

		// Token: 0x0400003B RID: 59
		[Nullable(2)]
		private TextMeshProUGUI _functionToggleContent;

		// Token: 0x0400003C RID: 60
		[Nullable(2)]
		private TextMeshProUGUI _autoAimContent;

		// Token: 0x0400003D RID: 61
		[Nullable(2)]
		private TextMeshProUGUI _weaponInfoContent;

		// Token: 0x0400003E RID: 62
		[Nullable(2)]
		private TextMeshProUGUI _shortcutsContent;

		// Token: 0x0400003F RID: 63
		[Nullable(2)]
		private TextMeshProUGUI _multipliersSummary;

		// Token: 0x04000040 RID: 64
		[Nullable(2)]
		private TextMeshProUGUI _rangeValueText;

		// Token: 0x04000041 RID: 65
		[Nullable(2)]
		private TextMeshProUGUI _fireRateValueText;

		// Token: 0x04000042 RID: 66
		[Nullable(2)]
		private TextMeshProUGUI _bulletSpeedValueText;

		// Token: 0x04000043 RID: 67
		[Nullable(2)]
		private TextMeshProUGUI _damageValueText;

		// Token: 0x04000044 RID: 68
		[Nullable(2)]
		private Slider _rangeSlider;

		// Token: 0x04000045 RID: 69
		[Nullable(2)]
		private Slider _fireRateSlider;

		// Token: 0x04000046 RID: 70
		[Nullable(2)]
		private Slider _bulletSpeedSlider;

		// Token: 0x04000047 RID: 71
		[Nullable(2)]
		private Slider _damageSlider;

		// Token: 0x04000048 RID: 72
		private bool _suppressSliderEvents;

		// Token: 0x04000049 RID: 73
		[Nullable(2)]
		private LineRenderer _autoAimRangeRenderer;

		// Token: 0x0400004A RID: 74
		[Nullable(2)]
		private RectTransform _autoAimRangeRect;

		// Token: 0x0400004B RID: 75
		[Nullable(2)]
		private Material _autoAimRangeMaterial;

		// Token: 0x0400004C RID: 76
		private Rect _menuWindowRect;

		// Token: 0x0400004D RID: 77
		[Nullable(2)]
		private Material _spinKillRangeMaterial;

		// Token: 0x0400004E RID: 78
		private Vector2 _menuScrollPosition;

		// Token: 0x0400004F RID: 79
		private bool _menuWindowInitialized;

		// Token: 0x04000050 RID: 80
		private readonly int _menuWindowId = "CheatMenuWindow".GetHashCode();

		// Token: 0x04000051 RID: 81
		private bool _functionSectionExpanded = true;

		// Token: 0x04000052 RID: 82
		private bool _multipliersSectionExpanded = true;

		// Token: 0x04000053 RID: 83
		private bool _autoAimSectionExpanded = true;

		// Token: 0x04000054 RID: 84
		private bool _weaponSectionExpanded = true;

		// Token: 0x04000055 RID: 85
		private bool _shortcutsSectionExpanded = true;

		// Token: 0x04000056 RID: 86
		[Nullable(2)]
		private Texture2D _autoAimCircleTexture;

		// Token: 0x04000057 RID: 87
		private bool _autoAimCircleVisible;

		// Token: 0x04000058 RID: 88
		private Vector2 _autoAimCircleCenter;

		// Token: 0x04000059 RID: 89
		private float _autoAimCircleRadiusPixels;

		// Token: 0x0400005A RID: 90
		private Vector2 _latestAimScreenPoint;

		// Token: 0x0400005B RID: 91
		private string _functionToggleText = string.Empty;

		// Token: 0x0400005C RID: 92
		private string _multipliersSummaryText = string.Empty;

		// Token: 0x0400005D RID: 93
		private string _autoAimContentText = string.Empty;

		// Token: 0x0400005E RID: 94
		private string _weaponInfoText = string.Empty;

		// Token: 0x0400005F RID: 95
		private string _shortcutsText = string.Empty;

		// Token: 0x04000060 RID: 96
		private ModBehaviour.MenuSection _activeMenuSection;

		// Token: 0x04000061 RID: 97
		private readonly Color _sidebarButtonColor = new Color(0.16f, 0.16f, 0.16f, 0.88f);

		// Token: 0x04000062 RID: 98
		private readonly Color _sidebarButtonSelectedColor = new Color(0.24f, 0.55f, 0.96f, 0.9f);

		// Token: 0x04000063 RID: 99
		private float _lastAutoAimWorldRadius;

		// Token: 0x04000064 RID: 100
		private readonly Dictionary<Item, Dictionary<int, float>> _statMultiplierOriginalValues = new Dictionary<Item, Dictionary<int, float>>();

		// Token: 0x04000065 RID: 101
		private readonly Dictionary<CharacterMainControl, bool> _dashControlOriginalStates = new Dictionary<CharacterMainControl, bool>();

		// Token: 0x04000066 RID: 102
		private readonly Dictionary<CA_Dash, float> _dashCooldownOriginalValues = new Dictionary<CA_Dash, float>();

		// Token: 0x04000067 RID: 103
		private readonly Dictionary<CharacterMainControl, float> _disableTriggerOriginalValues = new Dictionary<CharacterMainControl, float>();

		// Token: 0x04000068 RID: 104
		private bool? _originalCharacterInvincible;

		// Token: 0x04000069 RID: 105
		[Nullable(2)]
		private Health _invincibilityTargetHealth;

		// Token: 0x0400006A RID: 106
		[Nullable(2)]
		private DamageReceiver _spinKillCurrentTarget;

		// Token: 0x0400006B RID: 107
		private readonly List<ModBehaviour.SpinKillCandidateInfo> _spinKillCandidates = new List<ModBehaviour.SpinKillCandidateInfo>(64);

		// Token: 0x0400006C RID: 108
		private Collider[] _spinKillColliderBuffer = Array.Empty<Collider>();

		// Token: 0x0400006D RID: 109
		private float _spinKillNextSearchTime;

		// Token: 0x0400006E RID: 110
		private bool _spinKillTriggerHeld;

		// Token: 0x0400006F RID: 111
		private float _spinKillSpinAngle;

		// Token: 0x04000070 RID: 112
		private Vector3 _spinKillCurrentAimPoint;

		// Token: 0x04000071 RID: 113
		private float _spinKillSearchRadius = 600f;

		// Token: 0x04000072 RID: 114
		private float _spinKillAngularSpeed = 720f;

		// Token: 0x04000073 RID: 115
		private bool _spinKillForcedPenetration;

		// Token: 0x04000074 RID: 116
		private bool _spinKillPreviousPenetrationEnabled;

		// Token: 0x04000075 RID: 117
		[Nullable(2)]
		private GameObject _spinKillRangeObject;

		// Token: 0x04000076 RID: 118
		[Nullable(2)]
		private LineRenderer _spinKillRangeRenderer;

		// Token: 0x04000077 RID: 119
		private int _spinKillLastManualFireFrame = -1;

		// Token: 0x04000078 RID: 120
		[Nullable(2)]
		private static readonly FieldInfo ScatterBeforeControlField = typeof(ItemAgent_Gun).GetField("scatterBeforeControl", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000079 RID: 121
		[Nullable(2)]
		private static readonly FieldInfo ScatterFactorHipsField = typeof(ItemAgent_Gun).GetField("scatterFactorHips", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400007A RID: 122
		[Nullable(2)]
		private static readonly FieldInfo ScatterFactorAdsField = typeof(ItemAgent_Gun).GetField("scatterFactorAds", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400007B RID: 123
		[Nullable(2)]
		private static readonly FieldInfo RecoilMoveValueField = typeof(ItemAgent_Gun).GetField("_recoilMoveValue", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400007C RID: 124
		[Nullable(2)]
		private static readonly FieldInfo RecoilBackField = typeof(ItemAgent_Gun).GetField("_recoilBack", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400007D RID: 125
		[Nullable(2)]
		private static readonly FieldInfo RecoilDistanceField = typeof(ItemAgent_Gun).GetField("_recoilDistance", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400007E RID: 126
		[Nullable(2)]
		private static readonly FieldInfo RecoilBackSpeedField = typeof(ItemAgent_Gun).GetField("_recoilBackSpeed", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400007F RID: 127
		[Nullable(2)]
		private static readonly FieldInfo RecoilRecoverSpeedField = typeof(ItemAgent_Gun).GetField("_recoilRecoverSpeed", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000080 RID: 128
		[Nullable(2)]
		private static readonly MethodInfo GunTransToFireMethod = typeof(ItemAgent_Gun).GetMethod("TransToFire", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000081 RID: 129
		[Nullable(2)]
		private static readonly FieldInfo GunStateField = typeof(ItemAgent_Gun).GetField("gunState", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000082 RID: 130
		private static readonly object[] GunTransToFireArgs = new object[] { true };

		// Token: 0x04000083 RID: 131
		[Nullable(2)]
		private static readonly PropertyInfo GunBulletCountProperty = typeof(ItemSetting_Gun).GetProperty("bulletCount", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000084 RID: 132
		[Nullable(2)]
		private static readonly FieldInfo ProjectileVelocityField = typeof(Projectile).GetField("velocity", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000085 RID: 133
		[Nullable(2)]
		private static readonly FieldInfo ProjectileDirectionField = typeof(Projectile).GetField("direction", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000086 RID: 134
		[Nullable(2)]
		private static readonly FieldInfo ProjectileTraveledDistanceField = typeof(Projectile).GetField("traveledDistance", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000087 RID: 135
		[Nullable(2)]
		private static readonly FieldInfo InputAimPointField = typeof(InputManager).GetField("inputAimPoint", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000088 RID: 136
		[Nullable(2)]
		private static readonly FieldInfo GunProjectileField = typeof(ItemAgent_Gun).GetField("projInst", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000089 RID: 137
		[Nullable(2)]
		private static readonly FieldInfo InputAimingEnemyHeadField = typeof(InputManager).GetField("aimingEnemyHead", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400008A RID: 138
		[Nullable(2)]
		private static readonly FieldInfo ProjectileHitLayersField = typeof(Projectile).GetField("hitLayers", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400008B RID: 139
		[Nullable(2)]
		private static readonly FieldInfo ProjectileDamagedObjectsField = typeof(Projectile).GetField("damagedObjects", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400008C RID: 140
		[Nullable(2)]
		private static readonly FieldInfo ProjectileDeadField = typeof(Projectile).GetField("dead", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400008D RID: 141
		[Nullable(2)]
		private static readonly FieldInfo GrenadeCollideField = typeof(Grenade).GetField("collide", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x0400008E RID: 142
		private static readonly int BulletDistanceStatHash = "BulletDistance".GetHashCode();

		// Token: 0x0400008F RID: 143
		private static readonly int ShootSpeedStatHash = "ShootSpeed".GetHashCode();

		// Token: 0x04000090 RID: 144
		private static readonly int BulletSpeedStatHash = "BulletSpeed".GetHashCode();

		// Token: 0x04000091 RID: 145
		private static readonly int DamageStatHash = "Damage".GetHashCode();

		// Token: 0x04000092 RID: 146
		private const float StatMultiplierMin = 1f;

		// Token: 0x04000093 RID: 147
		private const float StatMultiplierMax = 10f;

		// Token: 0x04000094 RID: 148
		private const float StatMultiplierStep = 0.1f;

		// Token: 0x04000095 RID: 149
		[Nullable(2)]
		private static readonly FieldInfo DisableTriggerTimerField = typeof(CharacterMainControl).GetField("disableTriggerTimer", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000096 RID: 150
		[Nullable(2)]
		private static readonly FieldInfo CurrentStaminaField = typeof(CharacterMainControl).GetField("currentStamina", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000097 RID: 151
		[Nullable(2)]
		private static readonly FieldInfo CharacterThirstyField = typeof(CharacterMainControl).GetField("thirsty", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000098 RID: 152
		[Nullable(2)]
		private static readonly FieldInfo CharacterStarveField = typeof(CharacterMainControl).GetField("starve", BindingFlags.Instance | BindingFlags.NonPublic);

		// Token: 0x04000099 RID: 153
		private static readonly bool EnableAutoAimDebugLogs = true;

		// Token: 0x0400009A RID: 154
		private static readonly int DamageReceiverLayerMaskValue = GameplayDataSettings.Layers.damageReceiverLayerMask;

		// Token: 0x0400009B RID: 155
		private static readonly int ObstacleLayerMaskValue = GameplayDataSettings.Layers.wallLayerMask | GameplayDataSettings.Layers.groundLayerMask | GameplayDataSettings.Layers.halfObsticleLayer;

		// Token: 0x0400009C RID: 156
		private float _autoAimScreenRadius = 140f;

		// Token: 0x0400009D RID: 157
		private const float AutoAimScreenRadiusMin = 60f;

		// Token: 0x0400009E RID: 158
		private const float AutoAimScreenRadiusMax = 2048f;

		// Token: 0x0400009F RID: 159
		private const float AutoAimScreenRadiusStep = 5f;

		// Token: 0x040000A0 RID: 160
		private const float AutoAimMaxDistance = 80f;

		// Token: 0x040000A1 RID: 161
		private const float AutoAimAngleWeight = 2.25f;

		// Token: 0x040000A2 RID: 162
		private const float AutoAimDistanceWeight = 0.1f;

		// Token: 0x040000A3 RID: 163
		private const float AutoAimObstaclePadding = 0.2f;

		// Token: 0x040000A4 RID: 164
		private const float AutoAimThrowableMaxDistance = 60f;

		// Token: 0x040000A5 RID: 165
		private const float ProjectileSelectionTravelThreshold = 0.6f;

		// Token: 0x040000A6 RID: 166
		private const float GrenadeScanInterval = 0.1f;

		// Token: 0x040000A7 RID: 167
		private const float GrenadeHomingAcceleration = 140f;

		// Token: 0x040000A8 RID: 168
		private const float GrenadeHomingMaxSpeed = 32f;

		// Token: 0x040000A9 RID: 169
		private const float GrenadeHomingMinSpeed = 4f;

		// Token: 0x040000AA RID: 170
		private const float GrenadeHomingMinSpeedCloseRange = 2.8f;

		// Token: 0x040000AB RID: 171
		private const float GrenadeHomingSpeedSlope = 7.5f;

		// Token: 0x040000AC RID: 172
		private const float GrenadeHomingSlowdownDistance = 2.6f;

		// Token: 0x040000AD RID: 173
		private const float GrenadeHomingArrivalStopDistance = 0.12f;

		// Token: 0x040000AE RID: 174
		private const float GrenadeHomingInitialBoost = 12f;

		// Token: 0x040000AF RID: 175
		private const float GrenadeCollisionTriggerDistance = 0.36f;

		// Token: 0x040000B0 RID: 176
		private const float GrenadeCollisionSurfaceThreshold = 0.08f;

		// Token: 0x040000B1 RID: 177
		private const float ProjectileCleanupInterval = 0.2f;

		// Token: 0x040000B2 RID: 178
		private const int AutoAimThreadSleepMilliseconds = 20;

		// Token: 0x040000B3 RID: 179
		private const float AutoAimThreadMinDistanceSqr = 0.04f;

		// Token: 0x040000B4 RID: 180
		private const float AutoAimThreadRefreshInterval = 0.02f;

		// Token: 0x040000B5 RID: 181
		private const float SpinKillSearchInterval = 0.05f;

		// Token: 0x040000B6 RID: 182
		private const float SpinKillRangeDefault = 600f;

		// Token: 0x040000B7 RID: 183
		private const float SpinKillRangeMin = 5f;

		// Token: 0x040000B8 RID: 184
		private const float SpinKillRangeMax = 120f;

		// Token: 0x040000B9 RID: 185
		private const float SpinKillRangeStep = 0.1f;

		// Token: 0x040000BA RID: 186
		private const float SpinKillAngularSpeedDefault = 720f;

		// Token: 0x040000BB RID: 187
		private const float SpinKillAngularSpeedMin = 180f;

		// Token: 0x040000BC RID: 188
		private const float SpinKillAngularSpeedMax = 14400f;

		// Token: 0x040000BD RID: 189
		private const float SpinKillAngularSpeedStep = 30f;

		// Token: 0x040000BE RID: 190
		private const int SpinKillRangeSegments = 96;

		// Token: 0x040000BF RID: 191
		private const int ExplosiveAmmoReserve = 10;

		// Token: 0x040000C0 RID: 192
		private readonly HashSet<int> _retargetedProjectiles = new HashSet<int>();

		// Token: 0x040000C1 RID: 193
		private readonly HashSet<int> _penetratingProjectiles = new HashSet<int>();

		// Token: 0x040000C2 RID: 194
		private readonly Dictionary<int, LayerMask> _originalProjectileMasks = new Dictionary<int, LayerMask>();

		// Token: 0x040000C3 RID: 195
		private bool _penetrationActive;

		// Token: 0x040000C4 RID: 196
		private bool _applyPenetrationThisShot;

		// Token: 0x040000C5 RID: 197
		private readonly Dictionary<int, Projectile> _retargetedProjectileRefs = new Dictionary<int, Projectile>();

		// Token: 0x040000C6 RID: 198
		private readonly Dictionary<int, Projectile> _penetratingProjectileRefs = new Dictionary<int, Projectile>();

		// Token: 0x040000C7 RID: 199
		private readonly Dictionary<int, Vector3> _headshotOffsets = new Dictionary<int, Vector3>();

		// Token: 0x040000C8 RID: 200
		private readonly HashSet<int> _registeredGrenades = new HashSet<int>();

		// Token: 0x040000C9 RID: 201
		private readonly List<Grenade> _activeGrenades = new List<Grenade>(32);

		// Token: 0x040000CA RID: 202
		private readonly HashSet<int> _activeGrenadeIds = new HashSet<int>();

		// Token: 0x040000CB RID: 203
		private readonly List<Grenade> _grenadeSnapshotBuffer = new List<Grenade>(32);

		// Token: 0x040000CC RID: 204
		private readonly HashSet<int> _trackedGrenadePrefabs = new HashSet<int>();

		// Token: 0x040000CD RID: 205
		private readonly HashSet<int> _trackedGrenadeSkills = new HashSet<int>();

		// Token: 0x040000CE RID: 206
		private readonly Dictionary<int, WeakReference<Skill_Grenade>> _grenadeSkillRefs = new Dictionary<int, WeakReference<Skill_Grenade>>();

		// Token: 0x040000CF RID: 207
		private readonly Dictionary<ItemSetting_Skill, ItemSetting_Skill.OnReleaseAction> _skillReleaseOriginalValues = new Dictionary<ItemSetting_Skill, ItemSetting_Skill.OnReleaseAction>();

		// Token: 0x040000D0 RID: 208
		private readonly Dictionary<CharacterSkillKeeper, Action> _skillKeeperHooks = new Dictionary<CharacterSkillKeeper, Action>();

		// Token: 0x040000D1 RID: 209
		private readonly HashSet<int> _pendingMagazineRestores = new HashSet<int>();

		// Token: 0x040000D2 RID: 210
		private readonly List<ModBehaviour.ProjectileCandidate> _projectileCandidates = new List<ModBehaviour.ProjectileCandidate>(32);

		// Token: 0x040000D3 RID: 211
		private readonly List<ModBehaviour.AutoAimThreadTarget> _autoAimThreadTargetBuffer = new List<ModBehaviour.AutoAimThreadTarget>(64);

		// Token: 0x040000D4 RID: 212
		private float _nextGrenadeScanTime;

		// Token: 0x040000D5 RID: 213
		private float _nextProjectileCleanupTime;

		// Token: 0x040000D6 RID: 214
		private float _nextAutoAimThreadStateUpdateTime;

		// Token: 0x040000D7 RID: 215
		private DamageReceiver[] _damageReceiverCache = Array.Empty<DamageReceiver>();

		// Token: 0x040000D8 RID: 216
		private int _damageReceiverCacheFrame = -1;

		// Token: 0x040000D9 RID: 217
		private int _damageReceiverCacheCount;

		// Token: 0x040000DA RID: 218
		private Vector3 _damageReceiverCacheCenter;

		// Token: 0x040000DB RID: 219
		private float _damageReceiverCacheRadius;

		// Token: 0x040000DC RID: 220
		private Collider[] _damageReceiverColliderBuffer = new Collider[128];

		// Token: 0x040000DD RID: 221
		private LayerMask _damageReceiverLayerMask;

		// Token: 0x040000DE RID: 222
		private readonly List<Projectile> _activeProjectiles = new List<Projectile>(128);

		// Token: 0x040000DF RID: 223
		private readonly HashSet<int> _activeProjectileIds = new HashSet<int>();

		// Token: 0x040000E0 RID: 224
		private readonly List<Projectile> _projectileSnapshotBuffer = new List<Projectile>(32);

		// Token: 0x040000E1 RID: 225
		[Nullable(2)]
		private Thread _autoAimWorkerThread;

		// Token: 0x040000E2 RID: 226
		[Nullable(2)]
		private CancellationTokenSource _autoAimWorkerCts;

		// Token: 0x040000E3 RID: 227
		private ModBehaviour.AutoAimThreadContext _autoAimThreadContext;

		// Token: 0x040000E4 RID: 228
		private ModBehaviour.AutoAimThreadResult _autoAimThreadResult;

		// Token: 0x040000E5 RID: 229
		private float _nextCharacterRetryTime;

		// Token: 0x02000006 RID: 6
		[NullableContext(0)]
		private enum MenuSection
		{
			// Token: 0x040000E7 RID: 231
			FunctionToggles,
			// Token: 0x040000E8 RID: 232
			Multipliers,
			// Token: 0x040000E9 RID: 233
			AutoAim,
			// Token: 0x040000EA RID: 234
			Weapon,
			// Token: 0x040000EB RID: 235
			Shortcuts
		}

		// Token: 0x02000007 RID: 7
		[NullableContext(0)]
		private struct HeadshotInfo
		{
			// Token: 0x040000EC RID: 236
			public bool HasHead;

			// Token: 0x040000ED RID: 237
			public Vector3 HeadPosition;

			// Token: 0x040000EE RID: 238
			public Vector3 RandomOffset;
		}

		// Token: 0x02000008 RID: 8
		[NullableContext(0)]
		private sealed class ProjectileTracker : MonoBehaviour
		{
			// Token: 0x060000F0 RID: 240 RVA: 0x0000B4E8 File Offset: 0x000096E8
			private void Awake()
			{
				this._projectile = base.GetComponent<Projectile>();
			}

			// Token: 0x060000F1 RID: 241 RVA: 0x0000B4F8 File Offset: 0x000096F8
			private void OnEnable()
			{
				if (this._projectile == null)
				{
					this._projectile = base.GetComponent<Projectile>();
				}
				ModBehaviour instance = ModBehaviour.Instance;
				if (instance == null || this._projectile == null)
				{
					return;
				}
				instance.RegisterActiveProjectile(this._projectile);
			}

			// Token: 0x060000F2 RID: 242 RVA: 0x0000B54C File Offset: 0x0000974C
			private void OnDisable()
			{
				if (this._projectile == null)
				{
					this._projectile = base.GetComponent<Projectile>();
				}
				ModBehaviour instance = ModBehaviour.Instance;
				if (instance == null || this._projectile == null)
				{
					return;
				}
				instance.UnregisterActiveProjectile(this._projectile);
			}

			// Token: 0x040000EF RID: 239
			[Nullable(2)]
			private Projectile _projectile;
		}

		// Token: 0x02000009 RID: 9
		[NullableContext(0)]
		private sealed class ProjectileTrackerMarker : MonoBehaviour
		{
		}

		// Token: 0x0200000A RID: 10
		[NullableContext(0)]
		private sealed class GrenadeTracker : MonoBehaviour
		{
			// Token: 0x060000F5 RID: 245 RVA: 0x0000B5AD File Offset: 0x000097AD
			private void Awake()
			{
				this._grenade = base.GetComponent<Grenade>();
			}

			// Token: 0x060000F6 RID: 246 RVA: 0x0000B5BC File Offset: 0x000097BC
			private void OnEnable()
			{
				if (this._grenade == null)
				{
					this._grenade = base.GetComponent<Grenade>();
				}
				ModBehaviour instance = ModBehaviour.Instance;
				if (instance == null || this._grenade == null)
				{
					return;
				}
				instance.RegisterActiveGrenade(this._grenade);
			}

			// Token: 0x060000F7 RID: 247 RVA: 0x0000B610 File Offset: 0x00009810
			private void OnDisable()
			{
				if (this._grenade == null)
				{
					this._grenade = base.GetComponent<Grenade>();
				}
				ModBehaviour instance = ModBehaviour.Instance;
				if (instance == null || this._grenade == null)
				{
					return;
				}
				instance.UnregisterActiveGrenade(this._grenade);
			}

			// Token: 0x040000F0 RID: 240
			[Nullable(2)]
			private Grenade _grenade;
		}

		// Token: 0x0200000B RID: 11
		[NullableContext(2)]
		[Nullable(0)]
		private sealed class GrenadeHomingComponent : MonoBehaviour
		{
			// Token: 0x060000F9 RID: 249 RVA: 0x0000B66C File Offset: 0x0000986C
			[NullableContext(1)]
			public void Initialize(ModBehaviour owner, Grenade grenade, DamageReceiver target, float acceleration, float inheritedSpeed)
			{
				this._owner = owner;
				this._grenade = grenade;
				this._target = target;
				this._acceleration = Mathf.Max(1f, acceleration);
				this._inheritedSpeed = Mathf.Max(12f, inheritedSpeed);
				this._rigidbody = grenade.GetComponent<Rigidbody>();
				this._targetColliders = target.GetComponentsInChildren<Collider>();
				this._collisionSimulated = ModBehaviour.HasGrenadeRegisteredCollision(grenade);
				base.enabled = true;
				this.SnapVelocityToTarget();
			}

			// Token: 0x060000FA RID: 250 RVA: 0x0000B6E4 File Offset: 0x000098E4
			private void FixedUpdate()
			{
				if (!this.ValidateState())
				{
					return;
				}
				this.TrySimulateCollisionProximity();
				ModBehaviour owner = this._owner;
				if (!owner._autoAimEnabled || !owner._grenadeAutoAimEnabled)
				{
					return;
				}
				Vector3 vector;
				float num;
				if (!this.TryComputeDesiredVelocity(out vector, out num))
				{
					float num2 = this._acceleration * Time.fixedDeltaTime;
					if (this._rigidbody.velocity.sqrMagnitude > 0.0001f)
					{
						this._rigidbody.velocity = Vector3.MoveTowards(this._rigidbody.velocity, Vector3.zero, num2);
					}
					return;
				}
				float num3 = this._acceleration * Time.fixedDeltaTime;
				Vector3 vector2 = Vector3.MoveTowards(this._rigidbody.velocity, vector, num3);
				if (vector2.magnitude > 32f)
				{
					vector2 = vector2.normalized * 32f;
				}
				this._rigidbody.velocity = vector2;
				this.TrySimulateCollisionProximity();
			}

			// Token: 0x060000FB RID: 251 RVA: 0x0000B7C8 File Offset: 0x000099C8
			private void SnapVelocityToTarget()
			{
				if (this._rigidbody == null || this._owner == null || this._target == null)
				{
					return;
				}
				Vector3 vector = this._owner.GetReceiverAimPoint(this._target) - this._rigidbody.position;
				if (vector.sqrMagnitude < 0.01f)
				{
					return;
				}
				Vector3 normalized = vector.normalized;
				float magnitude = vector.magnitude;
				float num = Mathf.Min(32f, Mathf.Max(this._inheritedSpeed, magnitude * 7.5f));
				if (magnitude <= 0.12f)
				{
					num = Mathf.Max(num, 2.8f);
				}
				else
				{
					num = Mathf.Max(num, 4f);
				}
				this._rigidbody.velocity = normalized * num;
			}

			// Token: 0x060000FC RID: 252 RVA: 0x0000B894 File Offset: 0x00009A94
			private bool ValidateState()
			{
				if (this._owner == null || this._grenade == null)
				{
					this.Cleanup();
					return false;
				}
				if (this._target == null || !this._target.enabled || this._target.IsDead || this._target.gameObject == null || !this._target.gameObject.activeInHierarchy)
				{
					this.Cleanup();
					return false;
				}
				if (this._rigidbody == null)
				{
					this._rigidbody = this._grenade.GetComponent<Rigidbody>();
					if (this._rigidbody == null)
					{
						this.Cleanup();
						return false;
					}
				}
				if (!this._grenade.gameObject.activeInHierarchy)
				{
					this.Cleanup();
					return false;
				}
				return true;
			}

			// Token: 0x060000FD RID: 253 RVA: 0x0000B96C File Offset: 0x00009B6C
			private void TrySimulateCollisionProximity()
			{
				if (this._grenade == null || !this._grenade.delayFromCollide)
				{
					return;
				}
				if (this._collisionSimulated)
				{
					return;
				}
				if (ModBehaviour.HasGrenadeRegisteredCollision(this._grenade))
				{
					this._collisionSimulated = true;
					return;
				}
				if (this._owner == null || this._target == null || this._rigidbody == null)
				{
					return;
				}
				Vector3 position = this._rigidbody.position;
				Vector3 receiverAimPoint = this._owner.GetReceiverAimPoint(this._target);
				float num = Vector3.Distance(position, receiverAimPoint);
				if (num > 0.36f)
				{
					return;
				}
				if (this.ComputeSurfaceDistance(position, num) > 0.08f)
				{
					return;
				}
				ModBehaviour.SimulateGrenadeCollision(this._grenade, this._rigidbody);
				this._collisionSimulated = true;
			}

			// Token: 0x060000FE RID: 254 RVA: 0x0000BA38 File Offset: 0x00009C38
			private float ComputeSurfaceDistance(Vector3 grenadePosition, float fallbackDistance)
			{
				float num = float.MaxValue;
				if (this._targetColliders != null && this._targetColliders.Length != 0)
				{
					for (int i = 0; i < this._targetColliders.Length; i++)
					{
						Collider collider = this._targetColliders[i];
						if (!(collider == null) && collider.enabled && collider.gameObject.activeInHierarchy)
						{
							try
							{
								Vector3 vector = collider.ClosestPoint(grenadePosition);
								float num2 = Vector3.Distance(grenadePosition, vector);
								if (num2 < num)
								{
									num = num2;
								}
							}
							catch
							{
							}
						}
					}
				}
				if (num == 3.40282347E+38f)
				{
					float num3 = 0.35f;
					return Mathf.Max(fallbackDistance - num3, 0f);
				}
				return num;
			}

			// Token: 0x060000FF RID: 255 RVA: 0x0000BAE4 File Offset: 0x00009CE4
			private bool TryComputeDesiredVelocity(out Vector3 desiredVelocity, out float distance)
			{
				desiredVelocity = Vector3.zero;
				distance = 0f;
				if (this._owner == null || this._target == null || this._rigidbody == null)
				{
					return false;
				}
				Vector3 vector = this._owner.GetReceiverAimPoint(this._target) - this._rigidbody.position;
				distance = vector.magnitude;
				if (distance < 0.0005f)
				{
					return false;
				}
				Vector3 vector2 = vector / distance;
				float num;
				if (distance <= 0.12f)
				{
					num = 2.8f;
				}
				else
				{
					num = Mathf.Min(32f, distance * 7.5f);
					if (distance < 2.6f)
					{
						float num2 = Mathf.Clamp01(distance / 2.6f);
						float num3 = 0.45f;
						num *= Mathf.Lerp(num3, 1f, num2);
						num = Mathf.Max(num, 2.8f);
					}
					else
					{
						num = Mathf.Max(num, 4f);
					}
				}
				desiredVelocity = vector2 * num;
				return true;
			}

			// Token: 0x06000100 RID: 256 RVA: 0x0000BBE8 File Offset: 0x00009DE8
			private void Cleanup()
			{
				if (this._owner != null && this._grenade != null)
				{
					this._owner.UnregisterHomingGrenade(this._grenade);
				}
				this._owner = null;
				this._grenade = null;
				this._target = null;
				this._rigidbody = null;
				this._targetColliders = Array.Empty<Collider>();
				base.enabled = false;
				Object.Destroy(this);
			}

			// Token: 0x040000F1 RID: 241
			private ModBehaviour _owner;

			// Token: 0x040000F2 RID: 242
			private Grenade _grenade;

			// Token: 0x040000F3 RID: 243
			private DamageReceiver _target;

			// Token: 0x040000F4 RID: 244
			private Rigidbody _rigidbody;

			// Token: 0x040000F5 RID: 245
			private float _acceleration;

			// Token: 0x040000F6 RID: 246
			private float _inheritedSpeed;

			// Token: 0x040000F7 RID: 247
			[Nullable(1)]
			private Collider[] _targetColliders = Array.Empty<Collider>();

			// Token: 0x040000F8 RID: 248
			private bool _collisionSimulated;
		}

		// Token: 0x0200000C RID: 12
		[NullableContext(0)]
		private struct AutoAimThreadTarget
		{
			// Token: 0x040000F9 RID: 249
			[Nullable(1)]
			public DamageReceiver Receiver;

			// Token: 0x040000FA RID: 250
			public Vector3 AimPoint;
		}

		// Token: 0x0200000D RID: 13
		[NullableContext(0)]
		private struct AutoAimThreadContext
		{
			// Token: 0x040000FB RID: 251
			[Nullable(2)]
			public ModBehaviour.AutoAimThreadTarget[] Targets;

			// Token: 0x040000FC RID: 252
			public int TargetCount;

			// Token: 0x040000FD RID: 253
			public Vector3 AimRayOrigin;

			// Token: 0x040000FE RID: 254
			public Vector3 AimRayDirection;

			// Token: 0x040000FF RID: 255
			public Vector3 Forward;

			// Token: 0x04000100 RID: 256
			public Vector3 MuzzlePosition;

			// Token: 0x04000101 RID: 257
			public Vector2 AimScreenPoint;

			// Token: 0x04000102 RID: 258
			public float ScreenRadius;

			// Token: 0x04000103 RID: 259
			public float MaxRange;

			// Token: 0x04000104 RID: 260
			public float ScreenWidth;

			// Token: 0x04000105 RID: 261
			public float ScreenHeight;

			// Token: 0x04000106 RID: 262
			public Matrix4x4 ViewProjection;

			// Token: 0x04000107 RID: 263
			public bool HasData;
		}

		// Token: 0x0200000E RID: 14
		[NullableContext(0)]
		private struct AutoAimThreadResult
		{
			// Token: 0x04000108 RID: 264
			public bool HasCandidate;

			// Token: 0x04000109 RID: 265
			public ModBehaviour.AutoAimCandidate Candidate;
		}

		// Token: 0x0200000F RID: 15
		[NullableContext(0)]
		private struct ProjectileCandidate
		{
			// Token: 0x0400010A RID: 266
			[Nullable(1)]
			public Projectile Projectile;

			// Token: 0x0400010B RID: 267
			public float Traveled;
		}

		// Token: 0x02000010 RID: 16
		[NullableContext(0)]
		private struct SpinKillCandidateInfo
		{
			// Token: 0x0400010C RID: 268
			[Nullable(1)]
			public DamageReceiver Receiver;

			// Token: 0x0400010D RID: 269
			public Vector3 AimPoint;

			// Token: 0x0400010E RID: 270
			public float Distance;

			// Token: 0x0400010F RID: 271
			public bool IsWithinAutoAim;

			// Token: 0x04000110 RID: 272
			public float ScreenDistance;
		}

		// Token: 0x02000011 RID: 17
		[Nullable(0)]
		private readonly struct AutoAimCandidate
		{
			// Token: 0x06000102 RID: 258 RVA: 0x0000BC69 File Offset: 0x00009E69
			public AutoAimCandidate(DamageReceiver receiver, Vector3 aimPoint, float screenDistance, float rayDistance, bool requiresPenetration)
			{
				this.Receiver = receiver;
				this.AimPoint = aimPoint;
				this.ScreenDistance = screenDistance;
				this.RayDistance = rayDistance;
				this.RequiresPenetration = requiresPenetration;
			}

			// Token: 0x17000002 RID: 2
			// (get) Token: 0x06000103 RID: 259 RVA: 0x0000BC90 File Offset: 0x00009E90
			public DamageReceiver Receiver { get; }

			// Token: 0x17000003 RID: 3
			// (get) Token: 0x06000104 RID: 260 RVA: 0x0000BC98 File Offset: 0x00009E98
			public Vector3 AimPoint { get; }

			// Token: 0x17000004 RID: 4
			// (get) Token: 0x06000105 RID: 261 RVA: 0x0000BCA0 File Offset: 0x00009EA0
			public float ScreenDistance { get; }

			// Token: 0x17000005 RID: 5
			// (get) Token: 0x06000106 RID: 262 RVA: 0x0000BCA8 File Offset: 0x00009EA8
			public float RayDistance { get; }

			// Token: 0x17000006 RID: 6
			// (get) Token: 0x06000107 RID: 263 RVA: 0x0000BCB0 File Offset: 0x00009EB0
			public bool RequiresPenetration { get; }
		}
	}
}
