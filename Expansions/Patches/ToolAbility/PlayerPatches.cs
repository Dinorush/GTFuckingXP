using Expansions.Information.ToolAbility;
using HarmonyLib;
using Player;

namespace Expansions.Patches.ToolAbility
{
    internal static class PlayerPatches
    {
        [HarmonyPatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void OnPlayerSpawn(LocalPlayerAgent __instance)
        {
            if (__instance.GetComponent<ToolAbilityHandler>() == null)
                __instance.gameObject.AddComponent<ToolAbilityHandler>();
        }
    }
}
