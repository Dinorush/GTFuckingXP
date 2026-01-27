using EndskApi.Enums.EnemyKill;
using EndskApi.Information.EnemyKill;
using EndskApi.Manager;
using EndskApi.Patches.EnemyKill;
using Enemies;
using Player;
using System;
using System.Collections.Generic;

namespace EndskApi.Api
{
    public static class EnemyKillApi
    {
        private const string EnemyKillKey = "EnemyKillCallbacks";
        private static bool _setup = false;

        private static Dictionary<IntPtr, bool> _enemyStates = new Dictionary<IntPtr, bool>();
        private static Dictionary<IntPtr, EnemyKillDistribution> _enemyDistributions = new Dictionary<IntPtr, EnemyKillDistribution>();

        /// <summary>
        /// Adds a callback to whenever an enemy dies.
        /// THIS FUNCTIONALITY IS HOST ONLY!
        /// </summary>
        /// <param name="callBack"></param>
        public static void AddEnemyKilledCallback(Action<EnemyKillDistribution> callBack)
        {
            Setup();
            if (!CacheApi.TryGetInformation<List<Action<EnemyKillDistribution>>>(EnemyKillKey, out var callBackList, CacheApi.InternalCache, false))
            {
                callBackList = new List<Action<EnemyKillDistribution>>();
                CacheApi.SaveInformation(EnemyKillKey, callBackList, CacheApi.InternalCache);
            }

            callBackList.Add(callBack);
        }

        internal static void InvokeEnemyKilledCallbacks(EnemyKillDistribution enemyKill)
        {
            if (CacheApi.TryGetInformation<List<Action<EnemyKillDistribution>>>(EnemyKillKey, out var callBackList, CacheApi.InternalCache, false))
            {
                foreach (var callBack in callBackList)
                {
                    callBack.Invoke(enemyKill);
                }
            }
        }

        private static void Setup()
        {
            if (!_setup)
            {
                EnemyDamageBasePatchApi.AddReceiveMeleePrefix(1, ReceiveMeleePrefix);
                EnemyDamageBasePatchApi.AddReceiveMeleePostfix(1, ReceiveMeleePostfix);
                EnemyDamageBasePatchApi.AddReceiveBulletPrefix(1, ReceiveBulletPrefix);
                EnemyDamageBasePatchApi.AddReceiveBulletPostfix(1, ReceiveBulletPostfix);
                EnemyDamageBasePatchApi.AddReceiveExplosionPrefix(1, ReceiveExplosionPrefix);
                EnemyDamageBasePatchApi.AddReceiveExplosionPostfix(1, ReceiveExplosionPostfix);

                EndskApi.Api.LevelApi.AddEndLevelCallback(() =>
                {
                    _enemyStates.Clear();
                    _enemyDistributions.Clear();
                });

                _setup = true;
            }
        }

        private static void ReceiveMeleePrefix(Dam_EnemyDamageBase instance, ref pFullDamageData data)
        {
            var alive = instance.Owner.Alive;
            if (alive || _enemyStates.ContainsKey(instance.Pointer))
                _enemyStates[instance.Pointer] = alive;
        }

        private static void ReceiveMeleePostfix(Dam_EnemyDamageBase instance, ref pFullDamageData data)
        {
            if (_enemyStates.TryGetValue(instance.Pointer, out var state) && state)
            {
                data.source.TryGet(out var agent);
                if (agent == null)
                {
                    return;
                }
                var source = agent.TryCast<PlayerAgent>();
                DamageDistributionAddDamageDealt(instance.Owner, source, data.damage.Get(instance.HealthMax));

                if (!instance.Owner.Alive)
                {
                    EnemyDied(instance.Owner, source, LastHitType.Melee);
                }
            }
        }

        private static void ReceiveBulletPrefix(Dam_EnemyDamageBase instance, ref pBulletDamageData data)
        {
            var alive = instance.Owner.Alive;
            if (alive || _enemyStates.ContainsKey(instance.Pointer))
                _enemyStates[instance.Pointer] = alive;
        }

        private static void ReceiveBulletPostfix(Dam_EnemyDamageBase instance, ref pBulletDamageData data)
        {
            if (_enemyStates.TryGetValue(instance.Pointer, out var state) && state)
            {
                data.source.TryGet(out var agent);
                if (agent == null)
                {
                    return;
                }
                var source = agent.TryCast<PlayerAgent>();
                DamageDistributionAddDamageDealt(instance.Owner, source, data.damage.Get(instance.HealthMax));

                if (!instance.Owner.Alive)
                {
                    EnemyDied(instance.Owner, source, LastHitType.ShootyWeapon);
                }
            }
        }

        private static void ReceiveExplosionPrefix(Dam_EnemyDamageBase instance, PlayerAgent? source, ref pExplosionDamageData data)
        {
            var alive = instance.Owner.Alive;
            if (alive || _enemyStates.ContainsKey(instance.Pointer))
                _enemyStates[instance.Pointer] = alive;
        }

        private static void ReceiveExplosionPostfix(Dam_EnemyDamageBase instance, PlayerAgent? source, ref pExplosionDamageData data)
        {
            if (_enemyStates.TryGetValue(instance.Pointer, out var state) && state)
            {
                if (source != null)
                {
                    DamageDistributionAddDamageDealt(instance.Owner, source, data.damage.Get(instance.HealthMax));
                }

                if (!instance.Owner.Alive)
                {
                    EnemyDied(instance.Owner, source, LastHitType.Explosion);
                }
            }
        }

        private static void DamageDistributionAddDamageDealt(EnemyAgent hitEnemy, PlayerAgent damageDealer, float damageDealt)
        {
            if (!_enemyDistributions.TryGetValue(hitEnemy.Pointer, out var distribution))
                _enemyDistributions.Add(hitEnemy.Pointer, distribution = new(hitEnemy));
            distribution.AddDamageDealtByPlayerAgent(damageDealer, damageDealt);
        }

        private static void EnemyDied(EnemyAgent hitEnemy, PlayerAgent? lastHit, LastHitType lastHitType)
        {
            if (_enemyDistributions.TryGetValue(hitEnemy.Pointer, out var distribution))
            {
                distribution.LastHitDealtBy = lastHit;
                distribution.lastHitType = lastHitType;

                EnemyKillApi.InvokeEnemyKilledCallbacks(distribution);
                _enemyDistributions.Remove(hitEnemy.Pointer);
            }

            _enemyStates.Remove(hitEnemy.Pointer);
        }

        public static void RegisterPlayerBiotrackTagEnemy(PlayerAgent playerAgent, EnemyAgent taggedEnemy)
        {
            if (!_enemyDistributions.TryGetValue(taggedEnemy.Pointer, out var distribution))
                _enemyDistributions.Add(taggedEnemy.Pointer, distribution = new(taggedEnemy));
            distribution.TaggedByPlayer = playerAgent;
        }

        public static void RegisterDamage(EnemyAgent enemy, PlayerAgent? source, float damage, bool willKill)
        {
            if (source != null)
                DamageDistributionAddDamageDealt(enemy, source, damage);
            if (willKill)
                EnemyDied(enemy, source, LastHitType.ShootyWeapon);
        }
    }
}
