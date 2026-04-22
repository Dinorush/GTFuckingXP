using GTFuckingXP.Extensions;
using HarmonyLib;

namespace GTFuckingXP.Patches
{
    [HarmonyPatch(typeof(PlaceNavMarkerOnGO))]
    public class PlaceNavMarkerOnGoPatches
    {
        [HarmonyPatch(nameof(PlaceNavMarkerOnGO.UpdateName))]
        [HarmonyPrefix]
        public static void UpdateNamePrefix(PlaceNavMarkerOnGO __instance, ref string name, string extraInfo)
        {
            var player = __instance.m_player;
            if (player == null || player.Owner.IsBot) return;

            if (CacheApiWrapper.TryGetFullActiveLevel(player, out var level))
            {
                if (!string.IsNullOrEmpty(extraInfo))
                    name = $"{name}\n<color=#{BepInExLoader.LevelColor.Value}>{level.Layout.Header} Lv.{level.LevelNumber}</color>";
                else
                    name = $"<color=#{BepInExLoader.LevelColor.Value}>Lv.{level.LevelNumber}</color> {name}";
            }
        }
    }
}
