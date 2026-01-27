using EndskApi.Api;
using EndskApi.Information.EnemyKill;
using Enemies;
using GTFuckingXP.Extensions;
using GTFuckingXP.Information.Enemies;
using GTFuckingXP.Managers;
using GTFuckingXP.Scripts;
using SNetwork;

namespace GTFuckingXp.Managers
{
    public static class EnemyKillManager
    {
        public static void Setup()
        {
            EnemyKillApi.AddEnemyKilledCallback(EnemyKilled);
        }

        public static void EnemyKilled(EnemyKillDistribution info)
        {
            // LastHitDealtBy may be null if a non-Tripmine explosion killed (e.g. EEC explosion)
            if (info.LastHitDealtBy == null)
            {
                GiveXpToEveryone(info.KilledEnemyAgent);
                return;
            }

            var enemyXpData = GetEnemyXp(info.KilledEnemyAgent);
            var position = info.KilledEnemyAgent.Position;
            position.y += 1f;
            foreach (var player in SNet.LobbyPlayers)
            {
                if(player != null && !player.IsBot)
                {
                    if(info.LastHitDealtBy.Owner.PlayerSlot.index == player.PlayerSlot.index)
                    {
                        if(player.IsLocal)
                        {
                            if (CacheApi.TryGetInstance<XpHandler>(out var xpHandler, CacheApiWrapper.XpModCacheName))
                            {
                                xpHandler.AddXp(enemyXpData, position, false);
                            }
                        }
                        else
                        {
                            NetworkApiXpManager.SendReceiveXp(player, enemyXpData, position, false);
                        }
                    }
                    else
                    {
                        // Potential bio assist
                        uint bioAssistXp = 0;
                        if (info.KilledEnemyAgent.IsTagged && enemyXpData.BioAssistMultiplier > 0.0f &&
                            info.TaggedByPlayer != null && info.TaggedByPlayer.Owner.PlayerSlot.index == player.PlayerSlot.index)
                        {
                            bioAssistXp = (uint) (enemyXpData.XpGain * enemyXpData.BioAssistMultiplier);
                        }
                        var damageDealt = info.GetDamageDealtBySnet(player);
                        if (damageDealt > 0.5f)
                        {
                            var percentageDealt = damageDealt / info.KilledEnemyAgent.Damage.HealthMax;
                            LogManager.Debug($"percentageDealt = {percentageDealt} and damageDealt is {damageDealt}");

                            uint xpGain = (uint)(enemyXpData.XpGain * percentageDealt);
                            // If we gained more xp from the biotracker assist, use that instead
                            if (bioAssistXp > xpGain)
                            {
                                xpGain = bioAssistXp;
                            }
                            
                            NetworkApiXpManager.SendStaticXpInfo(player, xpGain,
                                        (uint)(enemyXpData.DebuffXp * percentageDealt), (int)(enemyXpData.LevelScalingXpDecrese * percentageDealt), position);
                        }
                        else
                        {
                            // If we didn't deal any damage, but we had assist xp, send that.
                            if (bioAssistXp > 0)
                            {
                                NetworkApiXpManager.SendStaticXpInfo(player, bioAssistXp,
                                    (uint)(enemyXpData.DebuffXp * enemyXpData.BioAssistMultiplier), (int)(enemyXpData.LevelScalingXpDecrese * enemyXpData.BioAssistMultiplier), position);
                            }
                        }
                    }
                }
            }
        }

        private static void GiveXpToEveryone(EnemyAgent killedEnemy)
        {
            var enemyXpData = GetEnemyXp(killedEnemy);

            var position = killedEnemy.Position;
            position.y = position.y + 1f;

            if (CacheApi.TryGetInstance<XpHandler>(out var xpHandler, CacheApiWrapper.XpModCacheName))
            {
                xpHandler.AddXp(enemyXpData, position, true, "<#F30>");
            }
            NetworkApiXpManager.SendHalfAssedXp(enemyXpData, position, true);
        }
        private static EnemyXp GetEnemyXp(EnemyAgent killedEnemy)
        {
            var enemyData = CacheApi.GetInstance<List<EnemyXp>>(CacheApiWrapper.XpModCacheName);
            var enemyXpData = enemyData.FirstOrDefault(it => it.EnemyId == killedEnemy.EnemyDataID);
            if (enemyXpData is null)
            {
                //No data found, creating a new instance and 
                LogManager.Warn($"There was no enemy XP data found for {killedEnemy.EnemyDataID}!");
                enemyXpData = new EnemyXp(killedEnemy.EnemyDataID, killedEnemy.name, 0, 0.0f, 0, 0);
                enemyData.Add(enemyXpData);
                CacheApi.SaveInstance(enemyData, CacheApiWrapper.XpModCacheName);
            }

            LogManager.Debug($"Enemy kill was registered. Enemy XpData was {enemyXpData.XpGain}.");

            return enemyXpData;
        }
    }
}
