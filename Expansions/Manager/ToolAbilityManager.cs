using AIGraph;
using AK;
using Expansions.Information.PackConverter;
using Expansions.Information.ToolAbility;
using Expansions.Patches.ToolAbility;
using GTFO.API;
using GTFuckingXP.Information.Level;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Player;
using SNetwork;
using UnityEngine;
using XpExpansions.Information;
using XpExpansions.Util;

namespace XpExpansions.Manager
{
    public class ToolAbilityManager : BaseManager
    {
        public const string ExpansionID = "Endskill.ToolAbility";
        private const string _expansionFileName = "ToolAbilityExpansion.json";
        private const string PickupNetworkID = "ToolAbilityPickup";
        private const string MoveMineNetworkID = "ToolAbilityMoveMine";

        public static ToolAbilityData.LevelData ActiveAbility { get; private set; } = new();
        private static List<ToolAbilityData> JsonData = new();

        public ToolAbilityManager()
        {
            Harmony harmony = new(ExpansionID);
            harmony.PatchAll(typeof(PlayerPatches));
            harmony.PatchAll(typeof(SentryPatches));
            harmony.PatchAll(typeof(MinePatches));
            ClassInjector.RegisterTypeInIl2Cpp<ToolAbilityHandler>();
            NetworkAPI.RegisterEvent<SNetStructs.pReplicator>(PickupNetworkID, PickupSentry);
            NetworkAPI.RegisterEvent<MoveMineData>(MoveMineNetworkID, MoveMine);
        }

        public override void Initialize()
        {
            WriteDefaultJsonBlocks();
            UpdateEverything();
        }

        public override void LevelCleanup()
        {
            ActiveAbility = new();
        }

        public override void LevelReached(PlayerAgent player, Level level)
        {
            if (!GTFuckingXP.Extensions.CacheApiWrapper.TryGetFullActiveLevel(player, out var activeLevel)) return;

            var levelLayoutID = activeLevel.Layout.PersistentId;
            var toolData = JsonData.Find(it => it.LevelLayoutID == levelLayoutID);
            if (toolData != null)
            {
                for (int i = toolData.DataChanges.Count - 1; i >= 0; i--)
                {
                    var dataChange = toolData.DataChanges[i];
                    if (dataChange.Level <= level.LevelNumber)
                    {
                        ActiveAbility = dataChange;
                        break;
                    }
                }
            }
        }

        private void WriteDefaultJsonBlocks()
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            var toolAbilityPath = Path.Combine(FolderPath, _expansionFileName);
            if (!File.Exists(toolAbilityPath))
            {
                File.WriteAllText(toolAbilityPath, ExpJson.Serialize(ToolAbilityData.Template));
            }
        }

        private void UpdateEverything()
        {
            JsonData = ExpJson.Deserialize<List<ToolAbilityData>>(File.ReadAllText(Path.Combine(FolderPath, _expansionFileName)))!;

            foreach (var data in JsonData)
            {
                data.DataChanges.Sort((x, y) => x.Level.CompareTo(y.Level));
            }
        }

        public static void DoSentryPickup(SentryGunInstance sentry)
        {
            SNetStructs.pReplicator packet = new();
            packet.SetID(sentry.Replicator);
            NetworkAPI.InvokeEvent(PickupNetworkID, packet, SNet.Master);
        }

        public static void DoMineMove(MineDeployerInstance mine, Vector3 newPos, Quaternion rotation)
        {
            MoveMineData packet = new();
            packet.mine.SetID(mine.Replicator);
            packet.position = newPos;
            packet.rotation = rotation;
            packet.courseNode.Set(DoorUtil.GetCorrectNode(newPos, mine.Owner.CourseNode));
            NetworkAPI.InvokeEvent(MoveMineNetworkID, packet);
            MoveMine(0, packet);
        }

        // Want to use SyncedPickup, but don't want to equip sentry, so need to run a custom SetDeployed first
        private static void PickupSentry(ulong _, SNetStructs.pReplicator packet)
        {
            if (!packet.TryGetID(out var rep) || rep == null) return;

            var sentry = rep.ReplicatorSupplier.Cast<SentryGunInstance>();
            if (!sentry.Alive || sentry.Owner == null) return;

            var player = sentry.Owner;
            var snetPlayer = player.Owner;
            if (!PlayerBackpackManager.TryGetBackpack(snetPlayer, out var bp) || !bp.TryGetBackpackItem(InventorySlot.GearClass, out var bpItem)) return;

            if (SNet.IsMaster)
            {
                if (sentry.Alive)
                    NetworkAPI.InvokeEvent(PickupNetworkID, packet);
                else
                    return;
            }

            if (bpItem.Status != eInventoryItemStatus.InBackpack)
            {
                bpItem.Status = eInventoryItemStatus.InBackpack;
                if (snetPlayer.IsLocal || (SNet.IsMaster && snetPlayer.IsBot))
                {
                    pInventoryItemStatus data = new();
                    data.sourcePlayer.SetPlayer(snetPlayer);
                    data.slot = InventorySlot.GearClass;
                    data.status = eInventoryItemStatus.InBackpack;
                    PlayerBackpackManager.InventoryItemStatusChange(data);
                }
                else
                {
                    bp.ShowHideInstance(bpItem, visible: true);
                }
                GuiManager.PlayerLayer.Inventory.UpdateItemUI(bpItem, player.Inventory.WieldedSlot);
            }
            sentry.SyncedPickup(player);
        }

        private static void MoveMine(ulong _, MoveMineData data)
        {
            if (!data.mine.TryGetID(out var rep) || rep == null) return;

            var mine = rep.ReplicatorSupplier.Cast<MineDeployerInstance>();
            var transform = mine.transform;
            transform.position = data.position;
            transform.rotation = data.rotation;
            if (data.courseNode.TryGet(out var node))
                mine.m_courseNode = node;
            mine.Sound.Post(EVENTS.STICKYMINEATTACH, data.position);
        }

        struct MoveMineData
        {
            public SNetStructs.pReplicator mine;
            public pCourseNode courseNode;
            public Vector3 position;
            public Quaternion rotation;
        }
    }
}
