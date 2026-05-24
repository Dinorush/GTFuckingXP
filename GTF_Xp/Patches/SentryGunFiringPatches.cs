using GTFuckingXP.Extensions;
using HarmonyLib;

namespace GTFuckingXP.Patches
{
    [HarmonyPatch(typeof(SentryGunInstance))]
    internal static class SentryGunFiringPatches
    {
        [HarmonyPatch(nameof(SentryGunInstance.CalcCostOfBullet))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PostfixCostOfBullet(SentryGunInstance __instance, ref float __result)
        {
            if (!CacheApiWrapper.TryGetActiveLevel(__instance.Owner, out var level)) return;

            float capMod = 1f;
            if (level.CustomScaling.TryGetValue(Enums.CustomScaling.ToolEfficiency, out var value))
                capMod *= value;
            if (level.CustomScaling.TryGetValue(Enums.CustomScaling.ToolCapacity, out value))
                capMod *= value;

            if (capMod == 1f) return;

            __result /= capMod;
        }

        [HarmonyPatch(nameof(SentryGunInstance.GiveAmmoRel))]
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
