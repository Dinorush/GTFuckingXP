using HarmonyLib;
using Player;

namespace Expansions.Patches.DoubleJump
{
    [HarmonyPatch]
    internal static class FallDamagePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Dam_SyncedDamageBase), nameof(Dam_SyncedDamageBase.FallDamage))]
        private static bool FallDamage(Dam_SyncedDamageBase __instance)
        {
            if (__instance.DamageBaseOwner != DamageBaseOwnerType.Player) return true;

            PlayerAgent playerAgent = __instance.GetBaseAgent().Cast<PlayerAgent>();
            if (playerAgent.Locomotion.m_currentStateEnum == PlayerLocomotion.PLOC_State.Jump || playerAgent.Locomotion.m_currentStateEnum == PlayerLocomotion.PLOC_State.Fall)
            {
                return false;
            }
            return true;
        }
    }
}
