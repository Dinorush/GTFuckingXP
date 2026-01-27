using Agents;
using EndskApi.Api;
using EndskApi.Information.WeaponSwitcher;
using Enemies;
using GTFO.API;
using Player;
using SNetwork;

namespace EndskApi.Manager.Internal
{
    internal static class NetworkManager
    {
        private const string _sendCheckpointReachedKey = "CheckpointHasBeenReached";
        private const string _sendCheckpointCleanupKey = "CheckpointCleanup";
        private const string _sendBiotagKey = "BiotagApplied";

        public static void Setup()
        {
            NetworkAPI.RegisterEvent<DummyStruct>(_sendCheckpointCleanupKey, ReceiveCheckpointCleanup);
            NetworkAPI.RegisterEvent<DummyStruct>(_sendCheckpointReachedKey, ReceiveCheckpointReached);
            NetworkAPI.RegisterEvent<pEnemyAgent>(_sendBiotagKey, ReceiveBiotag);
        }

        private static void ReceiveCheckpointReached(ulong snetPlayer, DummyStruct _)
        {
            CheckpointApi.InvokeCheckpointReachedCallbacks();
        }

        private static void ReceiveCheckpointCleanup(ulong snetPlayer, DummyStruct _)
        {
            CheckpointApi.InvokeCheckpointCleanupCallbacks();
        }

        private static void ReceiveBiotag(ulong lookup, pEnemyAgent packet)
        {
            if (!packet.TryGet(out var enemy) || !enemy.Alive || !SNet.TryGetPlayer(lookup, out var snetPlayer)) return;

            EnemyKillApi.RegisterBiotag(enemy, snetPlayer.PlayerAgent.Cast<PlayerAgent>());
        }

        public static void SendCheckpointReached()
        {
            NetworkAPI.InvokeEvent(_sendCheckpointReachedKey, new DummyStruct());
        }

        public static void SendCheckpointCleanups()
        {
            NetworkAPI.InvokeEvent(_sendCheckpointCleanupKey, new DummyStruct());
        }

        public static void SendBiotag(EnemyAgent enemy)
        {
            pEnemyAgent packet = default;
            packet.Set(enemy);
            NetworkAPI.InvokeEvent(_sendBiotagKey, packet, SNet.Master);
        }

        public static void SendEquipGear(GearInfo info)
        {

        }

        internal struct DummyStruct
        { }

        internal struct EquipGearStruct
        {

        }
    }
}
