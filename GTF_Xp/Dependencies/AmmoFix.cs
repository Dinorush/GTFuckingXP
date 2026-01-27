using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Player;

namespace GTFuckingXP.Dependencies
{
    internal static class AmmoFix
    {
        public const string ETC_GUID = "Dinorush.ExtraToolCustomization";
        public const string SSA_GUID = "Dinorush.SpreadStartingAmmo";

        public static readonly bool HasETC;
        public static readonly bool HasSSA;

        static AmmoFix()
        {
            HasETC = IL2CPPChainloader.Instance.Plugins.ContainsKey(ETC_GUID);
            HasSSA = IL2CPPChainloader.Instance.Plugins.ContainsKey(SSA_GUID);
        }

        public static void TryApplyPatches(Harmony harmony)
        {
            // SpreadStartingAmmo does both patches if ETC does not exist
            if (HasSSA) return;

            harmony.PatchAll(typeof(SSA_InventorySlotPatches));
            if (!HasETC)
                harmony.PatchAll(typeof(ETC_ToolAmmoPatches));
        }

        [HarmonyPatch(typeof(InventorySlotAmmo))]
        class SSA_InventorySlotPatches
        {
            [HarmonyPatch(nameof(InventorySlotAmmo.AddAmmo))]
            [HarmonyWrapSafe]
            [HarmonyPrefix]
            private static bool FixAmmoOverflow(InventorySlotAmmo __instance, float ammoAmount)
            {
                float ammo = __instance.AmmoInPack;
                float maxAmmo = __instance.AmmoMaxCap;

                // If it doesn't exceed capacity, we don't care
                if (ammo + ammoAmount < maxAmmo) return true;

                // If the existing ammo is bigger than max ammo, keeps it, otherwise gets set to max ammo.
                if (ammoAmount > 0 && ammo > 0)
                {
                    if (ammo >= maxAmmo)
                        return false;
                    else
                        __instance.AmmoInPack = maxAmmo;
                }
                else
                    __instance.AmmoInPack += ammoAmount;

                __instance.OnBulletsUpdateCallback?.Invoke(__instance.BulletsInPack);
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerAmmoStorage))]
        static class ETC_ToolAmmoPatches
        {
            [HarmonyPatch(nameof(PlayerAmmoStorage.UpdateBulletsInPack))]
            [HarmonyWrapSafe]
            [HarmonyPrefix]
            private static bool Unclamp_UpdateBullets(PlayerAmmoStorage __instance, AmmoType ammoType, int bulletCount, ref float __result)
            {
                // Only care about tools
                if (ammoType != AmmoType.Class) return true;

                InventorySlotAmmo inventorySlotAmmo = __instance.GetInventorySlotAmmo(ammoType);
                float ammo = inventorySlotAmmo.AmmoInPack;
                float maxAmmo = inventorySlotAmmo.AmmoMaxCap;
                float newAmmo = bulletCount * inventorySlotAmmo.CostOfBullet;

                // If it doesn't exceed capacity, we don't care
                if (ammo + newAmmo < maxAmmo) return true;

                float result;
                // Prevent gains from going above max cap or current ammo.
                // Otherwise you could place mines, get tool, and pick them up to overflow.
                if (newAmmo > 0 && ammo > 0)
                {
                    if (ammo >= maxAmmo || newAmmo >= maxAmmo)
                        result = inventorySlotAmmo.AmmoInPack = Math.Max(ammo, newAmmo);
                    else
                        result = inventorySlotAmmo.AmmoInPack = maxAmmo;
                }
                else
                    result = inventorySlotAmmo.AmmoInPack += newAmmo;

                inventorySlotAmmo.OnBulletsUpdateCallback?.Invoke(inventorySlotAmmo.BulletsInPack);
                __instance.NeedsSync = true;
                __instance.UpdateSlotAmmoUI(inventorySlotAmmo);
                __result = result;
                return false;
            }

            [HarmonyPatch(nameof(PlayerAmmoStorage.UpdateAmmoInPack))]
            [HarmonyWrapSafe]
            [HarmonyPrefix]
            private static bool Unclamp_UpdateAmmo(PlayerAmmoStorage __instance, AmmoType ammoType, float delta, ref float __result)
            {
                // Only care about tools
                if (ammoType != AmmoType.Class) return true;

                InventorySlotAmmo inventorySlotAmmo = __instance.GetInventorySlotAmmo(ammoType);
                float ammo = inventorySlotAmmo.AmmoInPack;
                float maxAmmo = inventorySlotAmmo.AmmoMaxCap;
                float newAmmo = delta;

                // If it doesn't exceed capacity, we don't care
                if (ammo + delta < maxAmmo) return true;

                float result;
                // Sentry could have ammo even while down due to GtfXP adding ammo,
                // so still need to prevent overflow
                if (newAmmo > 0 && ammo > 0)
                {
                    if (ammo >= maxAmmo || newAmmo >= maxAmmo)
                        result = inventorySlotAmmo.AmmoInPack = Math.Max(ammo, newAmmo);
                    else
                        result = inventorySlotAmmo.AmmoInPack = maxAmmo;
                }
                else
                    result = inventorySlotAmmo.AmmoInPack += newAmmo;

                inventorySlotAmmo.OnBulletsUpdateCallback?.Invoke(inventorySlotAmmo.BulletsInPack);
                __instance.NeedsSync = true;
                __instance.UpdateSlotAmmoUI(inventorySlotAmmo);
                __result = result;
                return false;
            }
        }
    }
}
