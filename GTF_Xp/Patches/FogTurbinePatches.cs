using HarmonyLib;

namespace GTFuckingXP.Patches
{
    [HarmonyPatch(typeof(HeavyFogRepellerGlobalState))]
    public class FogTurbinePatches
    {
        [HarmonyPatch(nameof(HeavyFogRepellerGlobalState.OnStateChange))]
        [HarmonyPrefix]
        public static void OnStateChangePostfix(HeavyFogRepellerGlobalState __instance, pCarryItemWithGlobalState_State newState, bool isDropinState)
        {
            if (isDropinState) return;
            if ((eHeavyFogRepellerStatus)newState.status != eHeavyFogRepellerStatus.Activated) return;
            if (newState.owner != eCarryItemWithGlobalStateOwner.Player || !newState.player.TryGetPlayer(out var player) || !player.HasPlayerAgent) return;

            __instance.m_repellerSphere.Range = AgentModifierManager.ApplyModifier(player.PlayerAgent.Cast <Player.PlayerAgent>(), AgentModifier.FogRepellerEffect, 7f);
        }
    }
}
