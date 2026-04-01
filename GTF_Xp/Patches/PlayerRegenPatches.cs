using GTFuckingXP.Extensions;
using HarmonyLib;
using GTFuckingXP.Information.Level;
using GTFuckingXP.Enums;
using Player;

namespace GTFuckingXp.Patches
{
    [HarmonyPatch(typeof(Dam_PlayerDamageBase))]
    internal static class PlayerRegenPatches
    {
        [HarmonyPatch(nameof(Dam_PlayerDamageBase.OnIncomingDamage))]
        [HarmonyPostfix]
        private static void Postfix_OnDamage(Dam_PlayerDamageBase __instance)
        {
            PlayerAgent player = __instance.Owner;
            if (!CacheApiWrapper.TryGetActiveLevel(player, out var level))
                return;

            if (level.CustomScaling.TryGetValue(CustomScaling.RegenStartDelayMultiplier, out var value))
                __instance.m_nextRegen = Clock.Time + player.PlayerData.healthRegenStartDelayAfterDamage * value;
        }
    }
}
