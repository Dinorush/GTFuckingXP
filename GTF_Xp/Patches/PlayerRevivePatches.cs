using Agents;
using GTFuckingXP.Managers;
using HarmonyLib;

namespace GTFuckingXP.Patches
{
    [HarmonyPatch]
    public class PlayerRevivePatches
    {
        [HarmonyPatch(typeof(AgentReplicatedActions), nameof(AgentReplicatedActions.DoPlayerReviveValidation))]
        [HarmonyPrefix]
        public static void RevivePostfix(AgentReplicatedActions.pPlayerReviveAction data)
        {
            if (!data.TargetPlayer.TryGet(out var target) || !data.SourcePlayer.TryGet(out var reviver)) return;

            // Prevent double rez
            if (target.Locomotion.Downed.m_isRevived) return;

            PlayerReviveManager.OnRevive(target, reviver);
        }
    }
}
