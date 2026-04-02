using GTFuckingXP.Extensions;
using GTFuckingXP.Information.Level;
using HarmonyLib;
using Player;

namespace GTFuckingXP.Patches
{
    [HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets))]
    internal static class SentryGunFiringPatches
    {
        [HarmonyPatch(nameof(SentryGunInstance_Firing_Bullets.UpdateAmmo))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void PrefixSentryAmmo(SentryGunInstance_Firing_Bullets __instance)
        {
            if (!CacheApiWrapper.TryGetActiveLevel(__instance.m_core.Owner, out var level)) return;

            float capMod = 1f;
            if (level.CustomScaling.TryGetValue(Enums.CustomScaling.ToolEfficiency, out var value))
                capMod *= value;
            if (level.CustomScaling.TryGetValue(Enums.CustomScaling.ToolCapacity, out value))
                capMod *= value;

            if (capMod == 1f) return;

            var core = __instance.m_core.Cast<SentryGunInstance>();
            // Cost of bullet updates every update before UpdateAmmo so don't need to worry about double calcs.
            // Apparently correctly modifies ammo shown on deployed sentry screen??? I have no idea how.
            core.CostOfBullet /= capMod;
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.GiveAmmoRel))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void PrefixSentryAmmo(SentryGunInstance __instance, ref float ammoClassRel)
        {
            if (!CacheApiWrapper.TryGetActiveLevel(__instance.Owner, out var level)) return;

            float gainMod = 1f;
            if (level.CustomScaling.TryGetValue(Enums.CustomScaling.ToolGainEfficiency, out var value))
                gainMod *= value;
            if (level.CustomScaling.TryGetValue(Enums.CustomScaling.ToolCapacity, out value))
                gainMod /= value;

            ammoClassRel *= gainMod;
        }
    }
}
