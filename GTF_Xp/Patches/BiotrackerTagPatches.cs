using Enemies;
using Gear;
using GTFO.API;
using GTFuckingXP.Information.NetworkingInfo;
using GTFuckingXP.Managers;
using HarmonyLib;

namespace EndskApi.Patches.EnemyKill
{
    [HarmonyPatch(typeof(EnemyScanner))]
    internal class BiotrackerTagPatches
    {
        [HarmonyPatch(nameof(EnemyScanner.UpdateTagProgress))]
        [HarmonyPostfix]
        public static void Postfix_UpdateTagProgress(EnemyScanner __instance, int maxTags)
        {
            // Conditions of tag: Last tagging = true, current tagging = false, and vice versa for recharging

            if (__instance.m_lastTagging && !__instance.m_tagging &&
                !__instance.m_lastRecharging && !__instance.m_recharging)
            {
                // Read list of enemies
                List<EnemyAgent> agents = new List<EnemyAgent>();
                // Copy into standard list - TODO, fix this
                for (int i = 0; i < __instance.m_taggableEnemies.Count; i++)
                {
                    agents.Add(__instance.m_taggableEnemies[i]);
                }
                BiotrackerTags bioTags = new BiotrackerTags();
                NetworkAPI.InvokeEvent(NetworkApiXpManager.SendPlayerBiotrackTagged, bioTags);
            }
        }
    }
}