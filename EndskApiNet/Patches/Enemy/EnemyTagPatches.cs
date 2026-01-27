using EndskApi.Api;
using EndskApi.Manager.Internal;
using Enemies;
using Gear;
using HarmonyLib;

namespace EndskApi.Patches.Enemy
{
    [HarmonyPatch]
    internal static class EnemyTagPatches
    {
        private static bool _inBotTag = false;
        [HarmonyPatch(typeof(EnemyScanner), nameof(EnemyScanner.BotTag))]
        [HarmonyPrefix]
        private static void Pre_BotTag()
        {
            _inBotTag = true;
        }

        [HarmonyPatch(typeof(EnemyScanner), nameof(EnemyScanner.BotTag))]
        [HarmonyPostfix]
        private static void Post_BotTag()
        {
            _inBotTag = false;
        }

        [HarmonyPatch(typeof(ToolSyncManager), nameof(ToolSyncManager.WantToTagEnemy))]
        [HarmonyPostfix]
        private static void Post_TagEnemy(EnemyAgent enemy)
        {
            if (_inBotTag) return;

            NetworkManager.SendBiotag(enemy);
        }
    }
}
