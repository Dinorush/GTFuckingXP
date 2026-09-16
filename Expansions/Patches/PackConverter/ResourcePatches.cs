using HarmonyLib;
using Player;
using XpExpansions.Manager;

namespace Expansions.Patches.PackConverter
{
    internal static class ResourcePatches
    {
        [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.GiveAmmoRel))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_GiveAmmo(PlayerAgent __instance, float ammoStandardRel, float ammoSpecialRel, float ammoClassRel)
        {
            PackConverterManager.ApplyConversions(__instance, ammoStandardRel, ammoSpecialRel, ammoClassRel);
        }

        [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.GiveHealth))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_GiveHealth(PlayerAgent __instance, float amountRel)
        {
            PackConverterManager.ApplyConversions(__instance, Information.PackConverter.ResourceType.Health, amountRel);
        }

        [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.GiveDisinfection))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_GiveDisinfect(PlayerAgent __instance, float amountRel)
        {
            PackConverterManager.ApplyConversions(__instance, Information.PackConverter.ResourceType.Disinfect, amountRel);
        }
    }
}
