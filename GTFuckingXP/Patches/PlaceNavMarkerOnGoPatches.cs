﻿using GTFuckingXP.Extensions;
using GTFuckingXP.Managers;
using HarmonyLib;

namespace GTFuckingXP.Patches
{
    [HarmonyPatch(typeof(PlaceNavMarkerOnGO))]
    public class PlaceNavMarkerOnGoPatches
    {
        [HarmonyPatch(nameof(PlaceNavMarkerOnGO.UpdateName))]
        [HarmonyPrefix]
        public static void UpdateNamePrefix(PlaceNavMarkerOnGO __instance, ref string name)
        {
            var playerToLevelMap = InstanceCache.Instance.GetPlayerToLevelMapping();
            if (playerToLevelMap.TryGetValue(__instance.m_player.PlayerSlotIndex, out var levelNumber))
            {
                name = $"<color=#F80>Lv.{levelNumber}</color> {name}";
            }
        }
    }
}