using EndskApi.Api;
using GTFO.API;
using GTFuckingXP.Extensions;
using Player;
using UnityEngine;

namespace GTFuckingXP.Managers
{
    public static class PlayerReviveManager
    {
        private static readonly Dictionary<ulong, int> _storedXP = new();
        private static readonly Dictionary<ulong, int> _totalXP = new();
        private static readonly Dictionary<ulong, (int stored, int total)> _checkpointXP = new();

        internal static void Init()
        {
            CheckpointApi.AddCheckpointReachedCallback(OnCheckpointReached);
            CheckpointApi.AddCheckpointReloadedCallback(OnCheckpointReloaded);
            LevelAPI.OnLevelCleanup += OnLevelCleanup;
        }

        private static void OnLevelCleanup()
        {
            _totalXP.Clear();
            _storedXP.Clear();
            _checkpointXP.Clear();
        }

        private static void OnCheckpointReached()
        {
            _checkpointXP.Clear();
            foreach (var kv in _storedXP)
                _checkpointXP.Add(kv.Key, (kv.Value, _totalXP[kv.Key]));
        }

        private static void OnCheckpointReloaded()
        {
            _storedXP.Clear();
            foreach (var kv in _checkpointXP)
                _storedXP.Add(kv.Key, kv.Value.stored);
            _totalXP.Clear();
            foreach (var kv in _checkpointXP)
                _totalXP.Add(kv.Key, kv.Value.total);
        }

        public static void AddXP(ulong playerLookup, int xpChange)
        {
            _storedXP.TryAdd(playerLookup, 0);
            _storedXP[playerLookup] += xpChange;
            _totalXP.TryAdd(playerLookup, 0);
            _totalXP[playerLookup] += xpChange;
        }

        public static void OnRevive(PlayerAgent downed, PlayerAgent reviver)
        {
            var downedOwner = downed.Owner;
            if (!_storedXP.TryGetValue(downedOwner.Lookup, out var value)) return;

            var globals = CacheApiWrapper.GetGlobalValues();
            var cap = _totalXP[downedOwner.Lookup] * globals.ReviveXpTransferCapFrac;
            value = (int) Math.Min(cap, value * globals.ReviveXpStoredFrac);
            if (value <= 0) return;

            NetworkApiXpManager.SendStaticXpInfo(reviver.Owner, value, value, 0, downed.Position + Vector3.up * 1.5f);

            Vector3 downedXpPos = downed.Position + Vector3.up * 1.5f;
            if (Physics.Raycast(downedXpPos, downed.Forward, out var hitInfo, 2f, LayerManager.MASK_WORLD))
                downedXpPos = hitInfo.point - downed.Forward * 0.5f;
            else
                downedXpPos += downed.Forward * 2;
            NetworkApiXpManager.SendStaticXpInfo(downedOwner, -value, -value, 0, downedXpPos);
            _storedXP[downedOwner.Lookup] = 0;
        }
    }
}
