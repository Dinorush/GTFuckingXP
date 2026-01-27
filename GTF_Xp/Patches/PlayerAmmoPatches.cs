using GTFuckingXP.Dependencies;
using GTFuckingXP.Extensions;
using GTFuckingXP.Managers;
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
        private static void Pre_SetStorage(PlayerAmmoStorage __instance, PlayerAgent owner)
        {
            if (RundownManager.ActiveExpedition == null || !owner.IsLocallyOwned || !CacheApiWrapper.TryGetCurrentLevelLayout(out var layout)) return;

            float[] mods = new float[]{ 1f, 1f, 1f };
            foreach (var startingBuff in layout.StartingBuffs)
            {
                switch (startingBuff.StartBuff)
                {
                    case Enums.StartBuff.AmmunitionMainMultiplier:
                        mods[0] *= startingBuff.Value;
                        break;
                    case Enums.StartBuff.AmmunitionSpecialMultiplier:
                        mods[1] *= startingBuff.Value;
                        break;
                    case Enums.StartBuff.AmmunitionToolMultiplier:
                        mods[2] *= startingBuff.Value;
                        break;
                }
            }

            if (mods.All(mod => mod == 1)) return;

            var idArr = __instance.m_ammoModificationIDs;
            var specialOverride = RundownManager.ActiveExpedition.SpecialOverrideData;

            AgentModifierManager.TryGetModifierInstance(owner, out var modifierInstance);

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
    }
}
