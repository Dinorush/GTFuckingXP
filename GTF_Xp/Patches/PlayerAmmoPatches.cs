using GTFuckingXP.Enums;
using GTFuckingXP.Extensions;
using GTFuckingXP.Information.Level;
using HarmonyLib;
using Player;

namespace GTFuckingXP.Patches
{
    [HarmonyPatch(typeof(PlayerAmmoStorage))]
    internal class PlayerAmmoPatches
    {
        [HarmonyPatch(nameof(PlayerAmmoStorage.AddLevelDefaultAmmoModifications))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void Post_SetStorage(PlayerAmmoStorage __instance, PlayerAgent owner)
        {
            if (RundownManager.ActiveExpedition == null || !owner.IsLocallyOwned || !CacheApiWrapper.TryGetCurrentLevelLayout(out var layout) || layout.StartingBuffs == null) return;

            float[] mods = new float[]
            {
                layout.StartingBuffs.GetValueOrDefault(StartBuff.AmmunitionMainMultiplier, 1f),
                layout.StartingBuffs.GetValueOrDefault(StartBuff.AmmunitionSpecialMultiplier, 1f),
                layout.StartingBuffs.GetValueOrDefault(StartBuff.AmmunitionToolMultiplier, 1f)
            };

            if (mods.All(mod => mod == 1)) return;

            var idArr = __instance.m_ammoModificationIDs;
            var specialOverride = RundownManager.ActiveExpedition.SpecialOverrideData;

            if (!AgentModifierManager.TryGetModifierInstance(owner, out var modifierInstance))
            {
                AgentModifierManager.AddModifierValue(owner, AgentModifier.InitialAmmoStandard, 0);
                AgentModifierManager.TryGetModifierInstance(owner, out modifierInstance);
            }

            float oldMod = modifierInstance.GetModifierValue(AgentModifier.InitialAmmoStandard) + 1;
            AgentModifierManager.ClearModifierChange(idArr[0]);
            idArr[0] = AgentModifierManager.AddModifierValue(owner, AgentModifier.InitialAmmoStandard, oldMod * mods[0] - 1f);

            oldMod = modifierInstance.GetModifierValue(AgentModifier.InitialAmmoSpecial) + 1;
            AgentModifierManager.ClearModifierChange(idArr[1]);
            idArr[1] = AgentModifierManager.AddModifierValue(owner, AgentModifier.InitialAmmoSpecial, oldMod * mods[1] - 1f);

            oldMod = modifierInstance.GetModifierValue(AgentModifier.InitialAmmoTool) + 1;
            AgentModifierManager.ClearModifierChange(idArr[2]);
            idArr[2] = AgentModifierManager.AddModifierValue(owner, AgentModifier.InitialAmmoTool, oldMod * mods[2] - 1f);
        }

        [HarmonyPatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.PickupAmmo))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void AmmoPackCallback(AmmoType ammoType, ref float ammoAmount)
        {
            var customBuff = ammoType switch
            {
                AmmoType.Standard or AmmoType.Special => CustomScaling.AmmoGainEfficiency,
                AmmoType.Class => CustomScaling.ToolGainEfficiency,
                _ => CustomScaling.Invalid
            };

            var level = CacheApiWrapper.GetActiveLevel();
            float totalMod = 1f;
            if (customBuff != CustomScaling.Invalid)
            {
                if (level.CustomScaling.TryGetValue(customBuff, out var value))
                    totalMod *= value;
            }

            customBuff = ammoType switch
            {
                AmmoType.Standard or AmmoType.Special => CustomScaling.AmmoCapacity,
                AmmoType.Class => CustomScaling.ToolCapacity,
                _ => CustomScaling.Invalid
            };
            if (customBuff != CustomScaling.Invalid)
            {
                if (level.CustomScaling.TryGetValue(customBuff, out var value))
                    totalMod /= value;
            }

            if (totalMod == 1f) return;

            ammoAmount *= totalMod;
        }
    }
}
