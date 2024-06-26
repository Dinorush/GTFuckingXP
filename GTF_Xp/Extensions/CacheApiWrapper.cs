﻿using EndskApi.Api;
using GTFuckingXP.Information.Level;
using GTFuckingXP.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GTFuckingXP.Extensions
{
    /// <summary>
    /// Provides extensions methods for <see cref="CacheApi"/>.
    /// </summary>
    public static class CacheApiWrapper
    {
        public const string XpModCacheName = "GTF_XP";

        internal const string LevelLayoutKey = "LevelLayout";
        internal const string CheckpointData = "XpCheckpointData";
        private const string ActiveLevelKey = "ActiveLevel";
        private const string DefaultDataBlockMaxHpKey = "MaxHpDefault";
        private const string PlayerSlotToLevelIndexMappingKey = "PlayerLevelIndexMapping";
        private const string BoosterBuffKey = "BoosterBuffKey";
        private const string AnchorDifferenceKey = "AnchorDifferenceKey";
        private const string LvlUpCallbackKey = "LvlUpCallbackKey";
        private const string ScriptsStartedCallbackKey = "ScriptsStartedCallback";

        private const string DefaultMeleeRangeKey = "DefaultMeleeRangeKey";
        private const string DefaultMeleeHitBoxKey = "DefaultMeleeHitBoxKey";
        private const string DefaultMovmentKey = "DefaultMovmentKeys";

        private const string DefaultJumpVelInitialKey = "DefaultVelInitialKey";
        private const string DefaultJumpGravityMulDefaultKey = "DefaultJumpGravityMulDefaultKey";
        private const string DefaultJumpGravityMulButtonReleasedKey = "DefaultJumpGravityMulButtonReleasedKey";
        private const string DefaultJumpGravityMulAfterPeakKey = "DefaultJumpGravityMulAfterPeakKey";
        private const string DefaultJumpGravityMulFallingKey = "DefaultJumpGravityMulFallingKey";
        private const string DefaultJumpVelocityMaxKey = "DefaultJumpVelocityMaxKey";

        /// <summary>
        /// Creates a new component of type <typeparamref name="Tscript"/> and saves it into the cache.
        /// Returns the newly created <typeparamref name="Tscript"/> after this.
        /// </summary>
        /// <typeparam name="Tscript">The component that should be created and cached.</typeparam>
        public static Tscript DestroyOldCreateRegisterAndReturnComponent<Tscript>() where Tscript : Component
        {
            if (CacheApi.TryGetInstance<Tscript>(out var instance, XpModCacheName))
            {
                instance.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(instance);
                CacheApi.RemoveInstance<Tscript>(XpModCacheName);
            }

            var gameObject = new GameObject("GetTheFuckingXp_Endskill");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            instance = gameObject.AddComponent<Tscript>();

            CacheApi.SaveInstance(instance, XpModCacheName);
            return instance;
        }

        /// <summary>
        /// Create a new component of type <typeparamref name="Tscript"/> and registers if it is not already registered.
        /// </summary>
        /// <typeparam name="Tscript">The component that should be created and registered if it does not exist yet.</typeparam>
        public static Tscript CreateRegisterAndReturnComponent<Tscript>() where Tscript : Component
        {
            if (CacheApi.TryGetInstance<Tscript>(out var instance, XpModCacheName))
            {
                instance.gameObject.SetActive(true);
                return instance;
            }

            var gameObject = new GameObject("GetTheFuckingXp_Endskill");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            instance = gameObject.AddComponent<Tscript>();

            CacheApi.SaveInstance(instance, XpModCacheName);
            return instance;
        }

        /// <summary>
        /// Kills the cached <typeparamref name="Tscript"/> component if it exists.
        /// </summary>
        public static void KillScript<Tscript>() where Tscript : Component
        {
            if (CacheApi.TryGetInstance<Tscript>(out var script, XpModCacheName))
            {
                script.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(script);
                CacheApi.RemoveInstance<Tscript>(XpModCacheName);
            }
        }

        public static void SetCurrentLevelLayout(LevelLayout levelLayout)
        {
            LogManager.Debug($"Set current level layout to {levelLayout.Header}.");
            CacheApi.SaveInformation(LevelLayoutKey, levelLayout, XpModCacheName);
        }

        public static LevelLayout GetCurrentLevelLayout()
        {
            return CacheApi.GetInformation<LevelLayout>(LevelLayoutKey, XpModCacheName);
        }

        public static bool TryGetCurrentLevelLayout(out LevelLayout levelLayout)
        {
            return CacheApi.TryGetInformation(LevelLayoutKey, out levelLayout, XpModCacheName);
        }

        /// <summary>
        /// Sets the new current active level to <paramref name="newLevel"/>
        /// </summary>
        public static void SetActiveLevel(Level newLevel, bool sendToOtherPeople = true)
        {
            LogManager.Debug($"Setting new level to {newLevel.LevelNumber}.");
            CacheApi.SaveInformation(ActiveLevelKey, newLevel, XpModCacheName);
            if (sendToOtherPeople)
            {
                NetworkApiXpManager.SendNewLevelActive(newLevel);
            }

            foreach (var callBack in GetLvlUpCallBackList())
            {
                LogManager.Message("Doing some weird Lvl Up callback stuff");
                callBack.Invoke(newLevel);
            }
        }

        /// <summary>
        /// Gets the current level with all stats.
        /// </summary>
        public static Level GetActiveLevel()
        {
            return CacheApi.GetInformation<Level>(ActiveLevelKey, XpModCacheName);
        }

        public static float GetDefaultMaxHp()
        {
            return CacheApi.GetInformation<float>(DefaultDataBlockMaxHpKey, XpModCacheName);
        }

        /// <summary>
        /// Sets the default HP.
        /// </summary>
        public static void SetDefaultMaxHp(float defaultMaxHp)
        {
            CacheApi.SaveInformation(DefaultDataBlockMaxHpKey, defaultMaxHp, XpModCacheName);
        }

        /// <summary>
        /// Sets the player to index map dictionary in the <paramref name="instanceCache"/>.
        /// </summary>
        public static void SetPlayerToLevelMapping(Dictionary<int, Level> playerToLevelMap)
        {
            CacheApi.SaveInformation(PlayerSlotToLevelIndexMappingKey, playerToLevelMap, XpModCacheName);
        }

        public static Dictionary<int, Level> GetPlayerToLevelMapping()
        {
            return CacheApi.GetInformation<Dictionary<int, Level>>(PlayerSlotToLevelIndexMappingKey, XpModCacheName);
        }

        public static void SetXpStorageData(uint knownXpState)
        {
            CacheApi.SaveInformation(CheckpointData, (GetCurrentLevelLayout(), knownXpState, XpModCacheName));
        }

        public static bool TryGetXpStorageData(out (LevelLayout levelLayout, uint totalXp) checkpointData)
        {
            return CacheApi.TryGetInformation(CheckpointData, out checkpointData, XpModCacheName);
        }

        public static BoosterBuffs GetCurrentBoosterBuffs()
        {
            return CacheApi.GetInformation<BoosterBuffs>(BoosterBuffKey, XpModCacheName);
        }

        public static void SetCurrentBoosterBuff(BoosterBuffs boosterBuff)
        {
            CacheApi.SaveInformation(BoosterBuffKey, boosterBuff, XpModCacheName);
        }

        public static void SetAnchorDifference(float anchorDifference)
        {
            CacheApi.SaveInformation(AnchorDifferenceKey, anchorDifference, XpModCacheName);
        }

        public static bool TryGetAnchorDifference(out float anchorDifference)
        {
            return CacheApi.TryGetInformation(AnchorDifferenceKey, out anchorDifference, XpModCacheName);
        }

        public static void SetLvlUpCallBackList(List<Action<Level>> lvlUpCallbacks)
        {
            CacheApi.SaveInformation(LvlUpCallbackKey, lvlUpCallbacks, XpModCacheName);
        }

        public static void AddLvlUpCallback(Action<Level> lvlUpCallback)
        {
            CacheApi.GetInformation<List<Action<Level>>>(LvlUpCallbackKey, XpModCacheName).Add(lvlUpCallback);
        }

        public static void SetScriptsStartedCallBackList(List<Action<Level>> lvlUpCallbacks)
        {
            CacheApi.SaveInformation(ScriptsStartedCallbackKey, lvlUpCallbacks, XpModCacheName);
        }

        public static void AddScriptsStartedCallback(Action<Level> lvlUpCallback)
        {
            CacheApi.GetInformation<List<Action<Level>>>(ScriptsStartedCallbackKey, XpModCacheName).Add(lvlUpCallback);
        }

        //public static bool RemoveLvlUpCallback(Action<Level> lvlUpCallback)
        //{
        //    return CacheApi.GetInformation<List<Action<Level>>>(LvlUpCallbackKey, XpModCacheName).Remove(lvlUpCallback);
        //}

        public static List<Action<Level>> GetLvlUpCallBackList()
        {
            return CacheApi.GetInformation<List<Action<Level>>>(LvlUpCallbackKey, XpModCacheName);
        }

        public static List<Action<Level>> GetScriptsStartedCallbackList()
        {
            return CacheApi.GetInformation<List<Action<Level>>>(ScriptsStartedCallbackKey, XpModCacheName);
        }

        public static void SetDefaultMeleeRange(float meleeRange)
        {
            CacheApi.SaveInformation(DefaultMeleeRangeKey, meleeRange, XpModCacheName);
        }

        public static void RemoveDefaultMeleeRange()
        {
            CacheApi.RemoveInformation(DefaultMeleeRangeKey, XpModCacheName);
        }

        public static bool TryGetDefaultMeleeRange(out float meleeRange)
        {
            return CacheApi.TryGetInformation(DefaultMeleeRangeKey, out meleeRange, XpModCacheName);
        }

        public static void SetDefaultMeleeHitBox(float meleeHitbox)
        {
            CacheApi.SaveInformation(DefaultMeleeHitBoxKey, meleeHitbox, XpModCacheName);
        }

        public static void RemoveDefaultMeleeHitBox()
        {
            CacheApi.RemoveInformation(DefaultMeleeHitBoxKey, XpModCacheName);
        }

        public static bool TryGetDefaultMeleeHitBox(out float meleeHitbox)
        {
            return CacheApi.TryGetInformation(DefaultMeleeHitBoxKey, out meleeHitbox, XpModCacheName);
        }

        public static bool TryGetDefaultMovment(out (float walk, float run, float air, float crouch) movmentData)
        {
            return CacheApi.TryGetInformation(DefaultMovmentKey, out movmentData, XpModCacheName);
        }

        public static void SetDefaultMovment(float walk, float run, float air, float crouch)
        {
            CacheApi.SaveInformation(DefaultMovmentKey, (walk, run, air, crouch), XpModCacheName);
        }

        public static void SetDefaultJumpVelInitial(float value)
        {
            CacheApi.SaveInformation(DefaultJumpVelInitialKey, value, XpModCacheName);
        }

        public static void SetDefaultJumpGravityMulDefault(float value)
        {
            CacheApi.SaveInformation(DefaultJumpGravityMulDefaultKey, value, XpModCacheName);
        }

        public static void SetDefaultJumpGravityMulButtonReleased(float value)
        {
            CacheApi.SaveInformation(DefaultJumpGravityMulButtonReleasedKey, value, XpModCacheName);
        }

        public static void SetDefaultJumpGravityMulAfterPeak(float value)
        {
            CacheApi.SaveInformation(DefaultJumpGravityMulAfterPeakKey, value, XpModCacheName);
        }

        public static void SetDefaultJumpGravityMulFalling(float value)
        {
            CacheApi.SaveInformation(DefaultJumpGravityMulFallingKey, value, XpModCacheName);
        }

        public static void SetDefaultJumpVelocityMax(float value)
        {
            CacheApi.SaveInformation(DefaultJumpVelocityMaxKey, value, XpModCacheName);
        }




        public static bool TryGetDefaultJumpVelInitial(out float value)
        {
            return CacheApi.TryGetInformation(DefaultJumpVelInitialKey, out value, XpModCacheName);
        }

        public static bool TryGetDefaultJumpGravityMulDefault(out float value)
        {
            return CacheApi.TryGetInformation(DefaultJumpGravityMulDefaultKey, out value, XpModCacheName);
        }

        public static bool TryGetDefaultJumpGravityMulButtonReleased(out float value)
        {
            return CacheApi.TryGetInformation(DefaultJumpGravityMulButtonReleasedKey, out value, XpModCacheName);
        }

        public static bool TryGetDefaultJumpGravityMulAfterPeak(out float value)
        {
            return CacheApi.TryGetInformation(DefaultJumpGravityMulAfterPeakKey, out value, XpModCacheName);
        }

        public static bool TryGetDefaultJumpGravityMulFalling(out float value)
        {
            return CacheApi.TryGetInformation(DefaultJumpGravityMulFallingKey, out value, XpModCacheName);
        }

        public static bool TryGetDefaultJumpVelocityMax(out float value)
        {
            return CacheApi.TryGetInformation(DefaultJumpVelocityMaxKey, out value, XpModCacheName);
        }
    }
}
