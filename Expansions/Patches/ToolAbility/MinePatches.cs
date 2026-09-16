using Expansions.Information.ToolAbility;
using HarmonyLib;

namespace Expansions.Patches.ToolAbility
{
    internal static class MinePatches
    {
        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.OnWield))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void OnDeployerSpawn(MineDeployerFirstPerson __instance)
        {
            if (__instance.Owner.IsLocallyOwned && !__instance.m_isConsumable && ToolAbilityHandler.Exists)
                ToolAbilityHandler.Instance.OnDeployerWield(__instance);
        }

        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.OnStickyMineSpawned))]
        [HarmonyPostfix]
        [HarmonyAfter("Dinorush.ExtraToolCustomization")]
        [HarmonyWrapSafe]
        private static void OnMineSpawn(MineDeployerFirstPerson __instance, ISyncedItem item)
        {
            if (ToolAbilityHandler.Exists && !__instance.m_isConsumable)
                ToolAbilityHandler.Instance.OnMineSpawn(item.Cast<MineDeployerInstance>());
        }

        [HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.OnDestroy))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static void OnMineDespawn(MineDeployerInstance __instance)
        {
            if (ToolAbilityHandler.Exists && __instance.Owner?.IsLocallyOwned == true)
                ToolAbilityHandler.Instance.OnMineDespawn(__instance);
        }

        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.Update))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static void OnMineUpdate(MineDeployerFirstPerson __instance, ref (IInteractable interact, bool ladder) __state)
        {
            if (ToolAbilityHandler.MovingMine)
            {
                var interact = __instance.Owner.Interaction;
                __state = (interact.m_bestSelectedInteract, interact.m_enterLadderVisible);
                interact.m_enterLadderVisible = false;
                interact.m_bestSelectedInteract = null;
            }
        }

        [HarmonyPatch(typeof(MineDeployerFirstPerson), nameof(MineDeployerFirstPerson.Update))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void OnMineUpdate(MineDeployerFirstPerson __instance, (IInteractable interact, bool ladder) __state)
        {
            if (ToolAbilityHandler.MovingMine)
            {
                __instance.m_placementIndicator.SetVisible(true);

                var fpHolder = __instance.FPItemHolder;
                bool hiddenTrigger = fpHolder.ItemHiddenTrigger;
                fpHolder.ItemHiddenTrigger = false;
                __instance.m_placementIndicator.SetPlacementEnabled(ToolAbilityHandler.CheckCanPlaceIgnoreHidden(__instance));
                fpHolder.ItemHiddenTrigger = hiddenTrigger;

                var interact = __instance.Owner.Interaction;
                interact.m_enterLadderVisible = __state.ladder;
                interact.m_bestSelectedInteract = __state.interact;
            }
        }
    }
}
