using Agents;
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

        public static event Action<Item>? OnSentryPickup;

        public ToolAbilityManager()
        {
            Harmony harmony = new(ExpansionID);
            harmony.PatchAll(typeof(PlayerPatches));
            harmony.PatchAll(typeof(SentryPatches));
            harmony.PatchAll(typeof(MinePatches));
            ClassInjector.RegisterTypeInIl2Cpp<ToolAbilityHandler>();
            NetworkAPI.RegisterEvent<PickupSentryData>(PickupNetworkID, ReceivePickupSentry);
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

            LogManager.Warn($"{player.Owner.NickName} reached {level.LevelNumber} with layout {activeLevel.Layout.PersistentId}, checking tool abilities...");
            var levelLayoutID = activeLevel.Layout.PersistentId;
            var toolData = JsonData.Find(it => it.LevelLayoutID == levelLayoutID);
            LogManager.Warn($"Found matching data? {toolData != null}");
            if (toolData != null)
            {
                for (int i = toolData.DataChanges.Count - 1; i >= 0; i--)
                {
                    var dataChange = toolData.DataChanges[i];
                    LogManager.Warn($"Checking level requirement on {dataChange.Level}");
                    if (dataChange.Level <= level.LevelNumber)
                    {
                        LogManager.Warn($"Level reached with tool ability: {dataChange.Level}, layout ID: {toolData.LevelLayoutID}.\nRecalls: {dataChange.SentryRecall}, {dataChange.MineRecall}. Recall speeds: {dataChange.SentryRecallSpeed}, {dataChange.MineRecallSpeed}. Move mines: {dataChange.MoveMines}");
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

        public static void DoSentryPickup(PlayerAgent player, SentryGunInstance sentry)
        {
            PickupSentryData packet = new();
            packet.player.Set(player);
            packet.sentry.SetID(sentry.Replicator);
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

        private static void ReceivePickupSentry(ulong _, PickupSentryData packet)
        {
            if (!packet.player.TryGet(out var player)) return;

            if (!SNet.IsMaster)
            {
                PickupSentry(player, packet.ammo);
                return;
            }

            if (!packet.sentry.TryGetID(out var rep) || rep == null) return;

            var sentry = rep.ReplicatorSupplier.Cast<SentryGunInstance>();
            if (!sentry.Alive) return;

            packet.ammo = sentry.Ammo;
            NetworkAPI.InvokeEvent(PickupNetworkID, packet);
            PickupSentry(player, packet.ammo);
            ItemReplicationManager.DeSpawn(rep);
        }

        // Want to use SyncedPickup, but don't want to equip sentry, so need to run a custom variant
        private static void PickupSentry(PlayerAgent player, float ammo)
        {
            var snetPlayer = player.Owner;
            bool managed = snetPlayer.IsLocal || (snetPlayer.IsBot && SNet.IsMaster);
            var bp = PlayerBackpackManager.GetBackpack(snetPlayer);
            var hasBPItem = bp.TryGetBackpackItem(InventorySlot.GearClass, out var bpItem);
            if (hasBPItem && bpItem.Status != eInventoryItemStatus.InBackpack)
            {
                bpItem.Status = eInventoryItemStatus.InBackpack;
                if (managed)
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

                if (snetPlayer.IsLocal)
                {
                    var inventoryUI = GuiManager.PlayerLayer.Inventory;
                    inventoryUI.UpdateItemUI(bpItem, player.Inventory.WieldedSlot);
                }
                else
                {
                    var inventorySynced = player.Inventory.Cast<PlayerInventorySynced>();
                    inventorySynced.PlaceBackpackItem(bpItem, InventorySlot.GearClass);
                }

                if (snetPlayer.IsInSlot)
                {
                    var inventoryMap = GuiManager.MainMenuLayer.PageMap.m_inventory[snetPlayer.PlayerSlotIndex()];
                    inventoryMap.UpdateItemUI(bpItem, InventorySlot.None);
                }
            }

            if (managed)
                bp.AmmoStorage.UpdateAmmoInPack(AmmoType.Class, ammo);

            if (hasBPItem)
                OnSentryPickup?.Invoke(bpItem.Instance);
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

        struct PickupSentryData
        {
            public pPlayerAgent player;
            public SNetStructs.pReplicator sentry;
            public float ammo;
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
