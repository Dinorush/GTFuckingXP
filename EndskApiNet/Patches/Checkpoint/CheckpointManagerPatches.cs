using EndskApi.Api;
using EndskApi.Manager;
using EndskApi.Manager.Internal;
using HarmonyLib;
using UnityEngine;

namespace EndskApi.Patches.Checkpoint
{
    [HarmonyPatch(typeof(CheckpointManager))]
    internal static class CheckpointManagerPatches
    {
        private static Vector3 _lastCheckpointPos = Vector3.zero;
        [HarmonyPatch(nameof(CheckpointManager.OnStateChange))]
        [HarmonyPostfix]
        public static void OnCheckpointStateChange(pCheckpointState newState)
        {
            if (newState.lastInteraction == eCheckpointInteractionType.StoreCheckpoint && _lastCheckpointPos != newState.doorLockPosition)
            {
                _lastCheckpointPos = newState.doorLockPosition;
                CheckpointApi.InvokeCheckpointReachedCallbacks();
            }
            else if (newState.lastInteraction == eCheckpointInteractionType.ReloadCheckpoint)
            {
                CheckpointApi.InvokeCheckpointReloadedCallbacks();
            }
        }

        [HarmonyPatch(nameof(CheckpointManager.OnLevelCleanup))]
        [HarmonyPostfix]
        public static void OnLevelCleanupPostfix()
        {
            CheckpointApi.InvokeCheckpointCleanupCallbacks();
            NetworkManager.SendCheckpointCleanups();
        }
    }
}
