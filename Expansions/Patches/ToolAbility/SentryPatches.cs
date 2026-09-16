using Expansions.Information.ToolAbility;
using HarmonyLib;

namespace Expansions.Patches.ToolAbility
{
    internal static class SentryPatches
    {
        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.OnGearSpawnComplete))]
        [HarmonyPostfix]
        [HarmonyAfter("Dinorush.ExtraToolCustomization")]
        [HarmonyWrapSafe]
        private static void OnSentrySpawn(SentryGunInstance __instance)
        {
            if (ToolAbilityHandler.Exists && __instance.Owner?.IsLocallyOwned == true)
                ToolAbilityHandler.Instance.OnSentrySpawn(__instance);
        }

        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.OnDestroy))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static void OnSentryDespawn(SentryGunInstance __instance)
        {
            if (ToolAbilityHandler.Exists && __instance.Owner?.IsLocallyOwned == true)
                ToolAbilityHandler.Instance.OnSentryDespawn();
        }
    }
}
