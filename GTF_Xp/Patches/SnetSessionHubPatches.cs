using CellMenu;
using EndskApi.Api;
using GTFuckingXP.Communication;
using GTFuckingXP.Extensions;
using GTFuckingXP.Information.ClassSelector;
using GTFuckingXP.Information.Level;
using GTFuckingXP.Managers;
using GTFuckingXP.Patches.SelectLevelPatches;
using HarmonyLib;
using SNetwork;

namespace GTFuckingXP.Patches
{
    [HarmonyPatch(typeof(SNet_SessionHub))]
    public class SnetSessionHubPatches
    {
        [HarmonyPatch(nameof(SNet_SessionHub.AddPlayerToSession))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void AddPlayerToSessionPostfix(SNet_SessionHub __instance)
        {
            if(!CacheApiWrapper.TryGetCurrentLevelLayout(out var classLayout) || !SNet.HasLocalPlayer)
            {
                return;
            }
            var groups = CacheApi.GetInstance<List<Group>>( CacheApiWrapper.XpModCacheName);

            var classInGroup = groups.FirstOrDefault(it => it.PersistentId == classLayout.GroupPersistentId);
            if (classInGroup == null)
            {
                LogManager.Warn($"Found no valid group for class {classLayout.Header}, id {classLayout.PersistentId}, group id {classLayout.GroupPersistentId}");
                return;
            }

            if((!classInGroup.ExpandAboveFourCount || classInGroup.VisibleForPlayerCount.Max() < 4) && !classInGroup.VisibleForPlayerCount.Contains(__instance.PlayersInSession.Count))
            {
                //TODO do Standard class
                if (GameStateManager.Current.m_currentStateName == eGameStateName.Lobby)
                {
                    foreach(var bar in CM_PageLoadout.Current.m_playerLobbyBars)
                    {
                        if(bar.m_player.Lookup == SNet.LocalPlayer.Lookup)
                        {
                            CacheApiWrapper.SetCurrentLevelLayout(CacheApi.GetInstance<List<LevelLayout>>(CacheApiWrapper.XpModCacheName)[0]);
                            PlayerLobbyBarPatches.ShowClassesSelector(bar);
                            break;
                        }
                    }
                }
                else
                {
                    XpApi.ChangeCurrentLevelLayout(CacheApi.GetInstance<List<LevelLayout>>(CacheApiWrapper.XpModCacheName)[0]);
                }
            }
        }
    }
}
