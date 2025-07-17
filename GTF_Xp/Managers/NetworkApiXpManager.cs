using EndskApi.Api;
using GTFO.API;
using GTFuckingXP.Extensions;
using GTFuckingXP.Information.Enemies;
using GTFuckingXP.Information.Level;
using GTFuckingXP.Information.NetworkingInfo;
using GTFuckingXP.Scripts;
using Player;
using SNetwork;
using System.Text.Json;
using UnityEngine;

namespace GTFuckingXP.Managers
{
    /// <summary>
    /// Handles all interactions with the GtfoApi networking interactions.
    /// </summary>
    public static class NetworkApiXpManager
    {
        private static readonly Dictionary<ulong, uint> _clientXP = new();
        private static readonly Dictionary<ulong, uint> _checkpointXP = new();

        private const string _sendXpString = "ThisSeemsLikeItComesFromTheRandomXpMod...";
        private const string _initXpString = "XpModJoinClientXP";
        private const string _requestXpString = "XpModClientRequestXP";
        private const string _levelStatsDistribution = "ReachedSentLevel_XP";
        private const string _receiveStaticXp = "XpModTriesToGiveYouSomeHalfAssedXP";
        private const string _undistributedXp = "XpModGivesYouSomeXpWhereYouHadNotToDoAnything";
        private const string _sendBoosterNetworkString = "MyNewBoostersEffectFromTheXpMod";       

        public static void Setup()
        {
            NetworkAPI.RegisterEvent<GtfoApiXpInfo>(_sendXpString, ReceiveXp);
            NetworkAPI.RegisterEvent<LevelReachedInfo>(_levelStatsDistribution, ReceiveLevelReached);
            NetworkAPI.RegisterEvent<StaticXpInfo>(_receiveStaticXp, ReceiveStaticXp);
            NetworkAPI.RegisterEvent<BoosterInfo>(_sendBoosterNetworkString, ReceiveBoosterBuffs);
            NetworkAPI.RegisterEvent<GtfoApiXpInfo>(_undistributedXp, ReceiveHalfAssedXp);
            NetworkAPI.RegisterEvent<InitXpInfo>(_initXpString, ReceiveInitXp);
            NetworkAPI.RegisterEvent<bool>(_requestXpString, ReceiveRequestXp);
            CheckpointApi.AddCheckpointReachedCallback(OnCheckpointReached);
            CheckpointApi.AddCheckpointReloadedCallback(OnCheckpointReloaded);
            LevelAPI.OnLevelCleanup += OnLevelCleanup;
        }

        private static void OnLevelCleanup()
        {
            _clientXP.Clear();
            _checkpointXP.Clear();
        }

        private static void OnCheckpointReached()
        {
            _checkpointXP.Clear();
            foreach (var kv in _clientXP)
                _checkpointXP.Add(kv.Key, kv.Value);
        }

        private static void OnCheckpointReloaded()
        {
            _clientXP.Clear();
            foreach (var kv in _checkpointXP)
                _clientXP.Add(kv.Key, kv.Value);
        }

        public static void ReceiveXp(ulong snetPlayer, GtfoApiXpInfo xpData)
        {
            LogManager.Debug("Received xp networking package");
            if (CacheApi.TryGetInstance(out XpHandler xpHandler, CacheApiWrapper.XpModCacheName))
            {
                xpHandler.AddXp(xpData, new UnityEngine.Vector3(xpData.PositionX, xpData.PositionY, xpData.PositionZ));
            }
        }

        public static void ReceiveHalfAssedXp(ulong snetPlayer, GtfoApiXpInfo xpData)
        {
            if (CacheApi.TryGetInstance(out XpHandler xpHandler, CacheApiWrapper.XpModCacheName))
            {
                xpHandler.AddXp(xpData, new UnityEngine.Vector3(xpData.PositionX, xpData.PositionY, xpData.PositionZ), xpData.ForceDebuffXp, "<#888>");
            }
        }

        public static void ReceiveStaticXp(ulong snetPlayer, StaticXpInfo xpInfo)
        {
            LogManager.Debug("Received static xp networking pckage");
            if (CacheApi.TryGetInstance(out XpHandler xpHandler, CacheApiWrapper.XpModCacheName))
            {
                xpHandler.AddXp(xpInfo, xpInfo.Position, false, "<#F30>");
            }
        }

        public static void ReceiveInitXp(ulong snetPlayer, InitXpInfo xpInfo)
        {
            if (CacheApi.TryGetInstance(out XpHandler xpHandler, CacheApiWrapper.XpModCacheName))
            {
                xpHandler.SkipToXp(xpInfo.Xp);
                if (xpInfo.CheckpointXp > 0)
                    CacheApiWrapper.SetXpStorageData(xpInfo.CheckpointXp);
            }
        }

        public static void ReceiveRequestXp(ulong lookup, bool _)
        {
            if (_clientXP.TryGetValue(lookup, out var xp) && xp > 0 && SNet.TryGetPlayer(lookup, out var player))
            {
                if (!_checkpointXP.TryGetValue(lookup, out var checkpointXP))
                    checkpointXP = 0;
                NetworkAPI.InvokeEvent(_initXpString, new InitXpInfo(xp, checkpointXP), player);
            }
        }

        public static void ReceiveLevelReached(ulong snetPlayer, LevelReachedInfo levelData)
        {
            LogManager.Debug("Receive level reached info");
            if(SNet.TryGetPlayer(snetPlayer, out var snet))
            {
                var playerAgents = PlayerManager.PlayerAgentsInLevel;
                foreach(var player in playerAgents)
                {
                    if(player.PlayerSlotIndex == snet.PlayerSlotIndex())
                    {
                        Level level = new Level(levelData);

                        var newHealth = level.HealthMultiplier * CacheApiWrapper.GetDefaultMaxHp();
                        LogManager.Debug($"Setting HP of {player.name} to {newHealth}");
                        player.Damage.HealthMax = newHealth;

                        CustomScalingBuffManager.ApplyCustomScalingEffects(player, level.CustomScaling);
                        CacheApiWrapper.GetPlayerToLevelMapping()[player.PlayerSlotIndex] = level;
                    }
                }
            }
        }

        internal static void ReceiveBoosterBuffs(ulong snetPlayer, BoosterInfo newInfo)
        {
            if (SNet.TryGetPlayer(snetPlayer, out var snet))
            {
                var playerAgents = PlayerManager.PlayerAgentsInLevel;
                foreach (var player in playerAgents)
                {
                    if (player.PlayerSlotIndex == snet.PlayerSlotIndex())
                    {
                        BoosterBuffManager.Instance.ApplyBoosterEffects(player, newInfo);
                    }
                }
            }
        }

        public static void SendReceiveXp(SNet_Player receiver, EnemyXp xpData, Vector3 position, bool forceDebuffXp)
        {
            TrackClientXp(receiver, xpData, forceDebuffXp);
            NetworkAPI.InvokeEvent(_sendXpString, new GtfoApiXpInfo(xpData.XpGain, xpData.DebuffXp, xpData.LevelScalingXpDecrese, position, forceDebuffXp),
                receiver);
        }

        public static void SendHalfAssedXp(EnemyXp xpData, Vector3 position, bool forceDebuffXp)
        {
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
                if (!player.IsLocallyOwned)
                    TrackClientXp(player.Owner, xpData, forceDebuffXp);

            NetworkAPI.InvokeEvent(_undistributedXp, new GtfoApiXpInfo(xpData.XpGain, xpData.DebuffXp, xpData.LevelScalingXpDecrese, position, forceDebuffXp));
        }

        public static void SendNewLevelActive(Level newLevel)
        {
            var customScaling = JsonSerializer.Serialize((
                    newLevel.CustomScaling is null ?
                    new List<CustomScalingBuff>() :
                    newLevel.CustomScaling));
            NetworkAPI.InvokeEvent(_levelStatsDistribution, new LevelReachedInfo(newLevel.LevelNumber, newLevel.HealthMultiplier, customScaling));
        }

        public static void SendBoosterStatsReached(BoosterInfo boosterInfo)
        {
            NetworkAPI.InvokeEvent(_sendBoosterNetworkString, boosterInfo);
        }

        public static void SendStaticXpInfo(SNet_Player receiver, uint xpGain, uint debuffXp, int levelScalingDecrease, Vector3 position)
        {
            NetworkAPI.InvokeEvent(_receiveStaticXp, new StaticXpInfo(xpGain, debuffXp, levelScalingDecrease, position), receiver);
        }

        public static void SendRequestXp()
        {
            NetworkAPI.InvokeEvent(_requestXpString, false, SNet.Master);
        }

        private static void TrackClientXp(SNet_Player receiver, EnemyXp xpData, bool forceDebuffXp)
        {
            uint xpValue = forceDebuffXp ? xpData.DebuffXp : xpData.XpGain;

            int levelScalingDecreaseXp = 0;
            if (CacheApiWrapper.GetPlayerToLevelMapping().TryGetValue(receiver.PlayerSlotIndex(), out var level))
                levelScalingDecreaseXp = xpData.LevelScalingXpDecrese * level.LevelNumber;

            if (xpValue <= levelScalingDecreaseXp)
            {
                xpValue = 1;
            }
            else
            {
                xpValue = (uint)(xpValue - levelScalingDecreaseXp);
            }

            if (!_clientXP.ContainsKey(receiver.Lookup))
                _clientXP.Add(receiver.Lookup, 0);
            _clientXP[receiver.Lookup] += xpValue;
        }
    }
}
