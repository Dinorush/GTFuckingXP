using Agents;
using GTFuckingXP.Enums;
using GTFuckingXP.Extensions;
using GTFuckingXP.Information.Level;
using GTFuckingXP.Managers;
using HarmonyLib;
using Player;

namespace GTFuckingXp.Patches
{
    [HarmonyPatch]
    internal static class PlayerDamagePatches
    {
        [HarmonyPatch(typeof(Dam_PlayerDamageLimb), nameof(Dam_PlayerDamageLimb.BulletDamage))]
        [HarmonyPrefix]
        private static void Prefix_BulletDamage(Dam_PlayerDamageLimb __instance, ref float dam, Agent sourceAgent)
        {
            PlayerAgent player = __instance.m_base.Owner;
            if (!player.Alive)
                return;

            if (!CacheApiWrapper.TryGetActiveLevel(player, out var level)) return;

            if (level.CustomScaling.TryGetValue(CustomScaling.BulletResistance, out var value))
                dam *= 2f - value;

            if (player.IsLocallyOwned && !SentryGunCheckPatches.SentryShot)
            {
                var damage = dam;
                LogManager.Debug($"Bullet damage from local player registered. {damage} was scaled up to:");
                damage *= level.WeaponDamageMultiplier;
                LogManager.Debug($"{damage}");
                dam = damage;
            }
        }

        // Dam_PlayerDamageBase does not override FireDamage
        [HarmonyPatch(typeof(Dam_SyncedDamageBase), nameof(Dam_SyncedDamageBase.FireDamage))]
        [HarmonyPrefix]
        private static void Prefix_FireDamage(Dam_SyncedDamageBase __instance, ref float dam, Agent sourceAgent)
        {
            if (__instance.DamageBaseOwner != DamageBaseOwnerType.Player) return;
            PlayerAgent player = __instance.GetBaseAgent().Cast<PlayerAgent>();

            if (!player.Alive || !player.IsLocallyOwned) return;

            if (CacheApiWrapper.GetActiveLevel().CustomScaling.TryGetValue(CustomScaling.BulletResistance, out var value))
                dam *= 2f - value;
        }

        // Dam_PlayerDamageBase does not override ExplosionDamage
        [HarmonyPatch(typeof(Dam_SyncedDamageBase), nameof(Dam_SyncedDamageBase.ExplosionDamage))]
        [HarmonyPrefix]
        private static void Prefix_ExplosionDamage(Dam_SyncedDamageBase __instance, ref float dam)
        {
            if (__instance.DamageBaseOwner != DamageBaseOwnerType.Player) return;
            PlayerAgent player = __instance.GetBaseAgent().Cast<PlayerAgent>();

            if (!CacheApiWrapper.TryGetActiveLevel(player, out var level)) return;

            if (level.CustomScaling.TryGetValue(CustomScaling.ExplosionResistance, out var value))
                dam *= 2f - value;
        }
    }
}
