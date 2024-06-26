﻿using EndskApi.Api;
using GTFuckingXP.Extensions;
using GTFuckingXP.Information.Enemies;
using GTFuckingXP.Information.Level;
using GTFuckingXP.Managers;
using GTFuckingXP.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GTFuckingXP.Communication
{
    public static class XpApi
    {
        /// <summary>
        /// Reloads the entire xp data.
        /// </summary>
        /// <returns>If the new layout was applied ingame.</returns>
        public static bool ReloadData()
        {
            try
            {
                var scriptManager = ScriptManager.Instance;

                scriptManager.UpdateEverything();

                //There is no real need to change the current Levellayout, this information is only set while in an expedition.
                if (CacheApi.TryGetInformation<LevelLayout>(CacheApiWrapper.LevelLayoutKey, out var levelLayout, CacheApiWrapper.XpModCacheName))
                {
                    var lvls = CacheApi.GetInstance<List<LevelLayout>>(CacheApiWrapper.XpModCacheName);
                    var newLevelLayout = lvls.FirstOrDefault(it => it.Header == levelLayout.Header);

                    var oldActiveLevel = CacheApiWrapper.GetActiveLevel();

                    CacheApiWrapper.SetCurrentLevelLayout(newLevelLayout);

                    SetCurrentLevel(oldActiveLevel.LevelNumber, out _);
                }

                return true;
            }
            catch (Exception e)
            {
                LogManager.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Adds <paramref name="xpAmount"/> to the current xp amount.
        /// </summary>
        /// <returns>If the current api call was successful.</returns>
        public static bool AddXp(uint xpAmount)
        {
            try
            {
                var xpHandler = CacheApi.GetInstance<XpHandler>(CacheApiWrapper.XpModCacheName);
                xpHandler.AddXp(new EnemyXp(0, "", xpAmount, xpAmount, 0), default, false);

                return true;
            }
            catch (Exception e)
            {
                LogManager.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Sets the current total xp to <paramref name="newTotalXpAmount"/>.<br/>
        /// WARNING: This call does not support backwards leveling! Which will cause problems, like a buggy xp bar.<br/>
        /// For lowering levels you may use <see cref="SetCurrentLevel(int, out int)"/>!
        /// </summary>
        /// <returns>If the call was successful.</returns>
        public static bool SetCurrentTotalXp(uint newTotalXpAmount, out int cheatedXp)
        {
            try
            {
                var xpHandler = CacheApi.GetInstance<XpHandler>(CacheApiWrapper.XpModCacheName);

                cheatedXp = (int)newTotalXpAmount - (int)xpHandler.CurrentTotalXp;

                xpHandler.CurrentTotalXp = newTotalXpAmount;
                xpHandler.CheckForLevelThresholdReached(default, out var header);

                CacheApi.GetInstance<XpBar>(CacheApiWrapper.XpModCacheName).UpdateUiString(CacheApiWrapper.GetActiveLevel(), xpHandler.NextLevel, xpHandler.CurrentTotalXp, header);
                return true;
            }
            catch (Exception e)
            {
                LogManager.Error(e);
                cheatedXp = 0;
                return false;
            }
        }

        /// <summary>
        /// Sets the current level to <paramref name="levelNumber"/>.
        /// </summary>
        /// <returns>If the call was successful.</returns>
        public static bool SetCurrentLevel(int levelNumber, out int cheatedXp)
        {
            try
            {
                var levelLayout = CacheApiWrapper.GetCurrentLevelLayout();
                var xpHandler = CacheApi.GetInstance<XpHandler>(CacheApiWrapper.XpModCacheName);

                var newLevel = levelLayout.Levels.First(it => it.LevelNumber == levelNumber);

                xpHandler.ChangeCurrentLevel(newLevel, BoosterBuffManager.Instance.GetFittingBoosterBuff(levelLayout.PersistentId, newLevel.LevelNumber));

                cheatedXp = (int)newLevel.TotalXpRequired - (int)xpHandler.CurrentTotalXp;

                xpHandler.CurrentTotalXp = newLevel.TotalXpRequired + 1;
                xpHandler.NextLevel = levelLayout.Levels.FirstOrDefault(it => it.LevelNumber == levelNumber + 1);
                CacheApi.GetInstance<XpBar>(CacheApiWrapper.XpModCacheName).UpdateUiString(CacheApiWrapper.GetActiveLevel(), xpHandler.NextLevel, xpHandler.CurrentTotalXp, levelLayout.Header);
            }
            catch (Exception e)
            {
                LogManager.Error(e);
                cheatedXp = 0;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Changes the level class of the player to the <see cref="LevelLayout"/> where <see cref="LevelLayout.Header"/> is the same as <paramref name="header"/>.
        /// </summary>
        /// <param name="header">The header of the new levelLayout.</param>
        /// <returns>If the api call was successful.</returns>
        public static bool ChangeCurrentLevelLayout(string header)
        {
            try
            {
                var newLevelLayout = CacheApi.GetInstance<List<LevelLayout>>(CacheApiWrapper.XpModCacheName).First(it => it.Header == header);
                return ChangeCurrentLevelLayout(newLevelLayout);
            }
            catch (Exception e)
            {
                LogManager.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Changes the level class of the player to the <paramref name="newActiveLevelLayout"/>. <br/>
        /// This call may give yourself some xp depending on if the level 1 in the other levellayout only needs 150XP but now needs 400XP which will just 
        /// </summary>
        /// <param name="newActiveLevelLayout">The new <see cref="LevelLayout"/> that should be applied.</param>
        /// <returns>If the api call was successful.</returns>
        public static bool ChangeCurrentLevelLayout(LevelLayout newActiveLevelLayout)
        {
            try
            {
                bool returnBool = true;

                CacheApiWrapper.SetCurrentLevelLayout(newActiveLevelLayout);
                if (CacheApi.TryGetInstance<XpHandler>(out var xpHandler, CacheApiWrapper.XpModCacheName))
                {
                    var oldTotalXp = xpHandler.CurrentTotalXp;
                    returnBool = SetCurrentLevel(0, out _) && SetCurrentTotalXp(oldTotalXp, out _);
                }

                return true;
            }
            catch (Exception e)
            {
                LogManager.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Adds <paramref name="lvlUpCallback"/> to the lvl up callback list, invoked whenever the local players achieves another level.
        /// </summary>
        /// <param name="lvlUpCallback">The event that should be called, </param>
        public static void AddOnLevelUpCallback(Action<Level> lvlUpCallback)
        {
            CacheApiWrapper.AddLvlUpCallback(lvlUpCallback);   
        }

        /// <summary>
        /// Adds <paramref name="scriptsLoadedCallback"/> to the scripts loaded callback list, invoked when the <see cref="XpHandler"/> is finished with initializing the levels.
        /// </summary>
        /// <param name="scriptsLoadedCallback">The event that should be invoked.</param>
        public static void AddScriptsLoaded(Action<Level> scriptsLoadedCallback)
        {
            CacheApiWrapper.AddScriptsStartedCallback(scriptsLoadedCallback);
        }
    }
}
