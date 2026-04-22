using EndskApi.Api;
using FloatingTextAPI;
using GTFuckingXP.Enums;
using GTFuckingXP.Extensions;
using GTFuckingXP.Information;
using GTFuckingXP.Information.Level;
using GTFuckingXP.Managers;
using Il2CppInterop.Runtime.Attributes;
using Il2CppSystem.Runtime.Remoting.Messaging;
using Player;
using System;
using System.Linq;
using UnityEngine;

namespace GTFuckingXP.Scripts
{
    /// <summary>
    /// Handles all Xp interactions.
    /// </summary>
    internal class XpHandler : MonoBehaviour
    {
        private bool _hasDebuff;
        private float _nextUpdate;

        //TODO List:
        //TODO Blink Mod
        //TODO Blink Mod expansion
        //TODO Incomming Damage scaling
        //TODO class specific weapons

        public XpHandler(IntPtr intPtr) : base(intPtr)
        {  }

        /// <summary>
        /// Gets or sets the total xp you have to this point.
        /// </summary>
        public uint CurrentTotalXp { get; internal set; }

        /// <summary>
        /// Gets the stats for the next level.
        /// </summary>
        [HideFromIl2Cpp]
        public Level? NextLevel { get; internal set; }

        /// <summary>
        /// Gets if you're already max level.
        /// </summary>
        public bool IsMaxLevel => NextLevel is null;

        public void Awake()
        {
            if (CacheApiWrapper.TryGetXpStorageData(out var checkpointXpData))
            {
                //Old level layout may be changed because of xp dev tools or other future plans :)
                CacheApiWrapper.SetCurrentLevelLayout(checkpointXpData.levelLayout);
                SkipToXp(checkpointXpData.totalXp);
            }
            else
            {
                if (!CacheApiWrapper.TryGetCurrentLevelLayout(out var levelLayout) || !SaveManager.CheckValidLayout(levelLayout))
                {
                    levelLayout = CacheApiWrapper.GetDefaultLayout();
                    CacheApiWrapper.SetCurrentLevelLayout(levelLayout);
                }

                var newActiveLevel = levelLayout.Levels[0];
                NextLevel = levelLayout.GetLevel(newActiveLevel.LevelNumber + 1);
                CurrentTotalXp = 0;
                ChangeCurrentLevel(newActiveLevel, BoosterBuffManager.Instance.GetFittingBoosterBuff(levelLayout.PersistentId, newActiveLevel.LevelNumber));
                CacheApi.GetInstance<XpBar>(CacheApiWrapper.XpModCacheName).UpdateUiString(CacheApiWrapper.GetActiveLevel(), NextLevel, CurrentTotalXp, levelLayout.Header);

                if (!SNetwork.SNet.IsMaster)
                    NetworkApiXpManager.SendRequestXp();
            }
            _nextUpdate = Time.time + 300f;
        }

        public void Update()
        {
            if(_nextUpdate < Time.time)
            {
                //TODO call my own webapi, for average xp data.

                _nextUpdate = Time.time + 300f;
            }
        }

        [HideFromIl2Cpp]
        public void AddXp(IXpData xpData, Vector3 xpTextPosition, bool forceDebuffXp = false, string xpPopupColor = "<#F80>")
        {
            int xpValue = xpData.GetXp(forceDebuffXp || _hasDebuff);

            CurrentTotalXp = (uint) Math.Max(0, CurrentTotalXp + xpValue);
            LogManager.Debug($"Giving xp Amount {xpValue}, new total Xp is {CurrentTotalXp}");
            if (!CheckForLevelThresholdReached(xpTextPosition, out var header) && BepInExLoader.XpPopups.Value)
            {
                if (NextLevel != null)
                {
                    DamageNumberFactory.CreateFloatingText<FloatingTextBase>(new FloatingXpTextInfo(xpTextPosition, $"{xpPopupColor}{xpValue}XP"));
                }
            }

            CacheApi.GetInstance<XpBar>(CacheApiWrapper.XpModCacheName).UpdateUiString(CacheApiWrapper.GetActiveLevel(), NextLevel, CurrentTotalXp, header);
        }

        /// <summary>
        /// Looks if the next level is reached and sets it, if it was reached.
        /// </summary>
        /// <param name="xpTextPosition">The world position, where this floating level up position should spawn.</param>
        /// <param name="floatingLevelUpMessage">If a floating level up message should appear.</param>
        /// <returns>If a new level got reached when this method got called.</returns>
        public bool CheckForLevelThresholdReached(Vector3 xpTextPosition, out string currentClassName)
        {
            var levels = CacheApiWrapper.GetCurrentLevelLayout();
            currentClassName = levels.Header;
            if (IsMaxLevel)
            {
                return false;
            }

            var oldLevel = CacheApiWrapper.GetActiveLevel();
            if (NextLevel!.TotalXpRequired <= CurrentTotalXp)
            {
                var newLevel = NextLevel;
                while (NextLevel != null && NextLevel.TotalXpRequired <= CurrentTotalXp)
                {
                    ApplySingleUseBuffs(NextLevel);
                    newLevel = NextLevel;
                    NextLevel = levels.GetLevel(newLevel.LevelNumber + 1);
                }

                ChangeCurrentLevel(newLevel, BoosterBuffManager.Instance.GetFittingBoosterBuff(levels.PersistentId, newLevel.LevelNumber));

                if (BepInExLoader.LvlUpPopups.Value)
                {
                    if (string.IsNullOrEmpty(newLevel.CustomLevelUpPopupText) )
                    {
                        DamageNumberFactory.CreateFloatingText<FloatingTextBase>(new FloatingXpTextInfo(xpTextPosition,
                       $"<#f00>LV {newLevel.LevelNumber}\n" +
                       $"HP: +<#f80>{Math.Round((newLevel.HealthMultiplier * CacheApiWrapper.GetDefaultMaxHp()) - (oldLevel.HealthMultiplier * CacheApiWrapper.GetDefaultMaxHp()), 1)}\n" +
                       $"<#f00>MD: <#f80>{Math.Round(newLevel.MeleeDamageMultiplier - oldLevel.MeleeDamageMultiplier, 2)}x \n" +
                       $"<#f00>WD: <#f80>{Math.Round(newLevel.WeaponDamageMultiplier - oldLevel.WeaponDamageMultiplier, 2)}x", 4f));
                    }
                    else
                    {
                        DamageNumberFactory.CreateFloatingText<FloatingTextBase>(new FloatingXpTextInfo(xpTextPosition,
                            newLevel.CustomLevelUpPopupText, 4f));
                    }
                }

                return true;
            }

            return false;
        }

        internal void SkipToXp(uint totalXp)
        {
            var levelLayout = CacheApiWrapper.GetCurrentLevelLayout();
            CurrentTotalXp = totalXp;
            var level = levelLayout.Levels.OrderByDescending(it => it.LevelNumber).First(it => it.TotalXpRequired <= CurrentTotalXp);
            NextLevel = levelLayout.GetLevel(level.LevelNumber + 1);
            var boosterBuffs = BoosterBuffManager.Instance.GetFittingBoosterBuff(levelLayout.PersistentId, level.LevelNumber);
            ChangeCurrentLevel(level, boosterBuffs, applyLevelBonuses: false);
            CacheApi.GetInstance<XpBar>(CacheApiWrapper.XpModCacheName).UpdateUiString(CacheApiWrapper.GetActiveLevel(), NextLevel, CurrentTotalXp, levelLayout.Header);
        }

        [HideFromIl2Cpp]
        internal void ChangeCurrentLevel(Level newLevel, BoosterBuffs newBoosterBuff = null, bool applyLevelBonuses = true)
        {
            CacheApiWrapper.SetActiveLevel(newLevel);
            CacheApiWrapper.SetCurrentBoosterBuff(newBoosterBuff);

            LogManager.Debug("saved information.");

            BoosterBuffManager.Instance.ApplyBoosterEffects(PlayerManager.GetLocalPlayerAgent(), newBoosterBuff);
            NetworkApiXpManager.SendBoosterStatsReached(newBoosterBuff);

            var localDamage = PlayerManager.GetLocalPlayerAgent().Damage;
            var oldMaxHealth = localDamage.HealthMax;
            var newMaxHealth = CacheApiWrapper.GetDefaultMaxHp() * newLevel.HealthMultiplier;

            localDamage.HealthMax = newMaxHealth;
            if (applyLevelBonuses)
                localDamage.Health += newMaxHealth - oldMaxHealth;

            localDamage.Cast<Dam_PlayerDamageLocal>().UpdateHealthGui();

            if (applyLevelBonuses)
                ApplySingleUseBuffs(newLevel);
            LogManager.Debug("Pre applying custom scaling effects.");
            CustomScalingBuffManager.ApplyCustomScalingEffects(newLevel.CustomScaling);
        }

        [HideFromIl2Cpp]
        private void ApplySingleUseBuffs(Level reachedLevel)
        {
            var player = PlayerManager.GetLocalPlayerAgent();
            foreach((var buff, var value) in reachedLevel.LevelUpBonus)
            {
                switch(buff)
                {
                    case SingleBuff.Heal:
                        player.GiveHealth(player, value);
                        break;
                    case SingleBuff.Desinfect:
                        player.GiveDisinfection(player, value);
                        break;
                    case SingleBuff.AmmunitionMain:
                        GiveAmmoRel(player, value, InventorySlot.GearStandard);
                        break;
                    case SingleBuff.AmmunitionSpecial:
                        GiveAmmoRel(player, value, InventorySlot.GearSpecial);
                        break;
                    case SingleBuff.AmmunitionTool:
                        GiveAmmoRel(player, value, InventorySlot.GearClass);
                        break;
                }
            }
        }

        private static void GiveAmmoRel(PlayerAgent player, float ammoRel, InventorySlot slot)
        {
            var block = player.PlayerData;
            var ammoStorage = PlayerBackpackManager.LocalBackpack.AmmoStorage;
            var slotAmmo = ammoStorage.GetInventorySlotAmmo(slot);
            if (slotAmmo.IsFull) return;

            float cap = slot switch
            {
                InventorySlot.GearStandard => block.AmmoStandardResourcePackMaxCap,
                InventorySlot.GearSpecial => block.AmmoSpecialResourcePackMaxCap,
                InventorySlot.GearClass => block.AmmoClassResourcePackMaxCap,
                _ => 0
            };

            ammoStorage.PickupAmmo(slot.ToAmmoType(), ammoRel * cap);
            PlayerBackpackManager.ForceLocalAmmoStorageUpdate();
        }
    }
}
