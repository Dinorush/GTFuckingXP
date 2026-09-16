using Expansions.Information.PackConverter;
using Expansions.Patches.PackConverter;
using GTFuckingXP.Information.Level;
using HarmonyLib;
using Player;
using System.Text.Json;
using System.Text.Json.Serialization;
using XpExpansions.Information;

namespace XpExpansions.Manager
{
    public class PackConverterManager : BaseManager
    {
        public const string ExpansionID = "Endskill.PackConverter";
        private const string _expansionFileName = "PackConverterExpansion.json";

        private static readonly Dictionary<IntPtr, ConversionWrapper> _activeConversions = new();
        private static List<PackConverterData> JsonData = new();

        public static void ApplyConversions(PlayerAgent player, ResourceType type, float amount)
        {
            if (_activeConversions.TryGetValue(player.Pointer, out var wrapper))
                wrapper.ApplyConversions(player, type, amount);
        }

        public static void ApplyConversions(PlayerAgent player, float standardAmount, float specialAmount, float toolAmount)
        {
            if (_activeConversions.TryGetValue(player.Pointer, out var wrapper))
                wrapper.ApplyConversions(player, standardAmount, specialAmount, toolAmount);
        }

        public PackConverterManager()
        {
            Harmony harmony = new(ExpansionID);
            harmony.PatchAll(typeof(ResourcePatches));
        }

        public override bool LocalOnly => false;

        public override void Initialize()
        {
            WriteDefaultJsonBlocks();
            UpdateEverything();
        }

        public override void LevelCleanup()
        {
            _activeConversions.Clear();
        }

        public override void LevelReached(PlayerAgent player, Level level)
        {
            if (!GTFuckingXP.Extensions.CacheApiWrapper.TryGetFullActiveLevel(player, out var activeLevel)) return;

            var levelLayoutID = activeLevel.Layout.PersistentId;
            var converterData = JsonData.Find(it => it.LevelLayoutID == levelLayoutID);
            if (converterData != null)
            {
                for (int i = converterData.DataChanges.Count - 1; i >= 0; i--)
                {
                    var dataChange = converterData.DataChanges[i];
                    if (dataChange.Level <= level.LevelNumber)
                    {
                        _activeConversions[player.Pointer] = new(dataChange);
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

            var packConverterPath = Path.Combine(FolderPath, _expansionFileName);
            if (!File.Exists(packConverterPath))
            {
                File.WriteAllText(packConverterPath, ExpJson.Serialize(PackConverterData.Template));
            }
        }

        private void UpdateEverything()
        {
            JsonData = ExpJson.Deserialize<List<PackConverterData>>(File.ReadAllText(Path.Combine(FolderPath, _expansionFileName)))!;

            foreach (var data in JsonData)
            {
                data.DataChanges.Sort((x, y) => x.Level.CompareTo(y.Level));
            }
        }

        class ConversionWrapper
        {
            private readonly Dictionary<ResourceType, List<PackConverterData.LevelData.ConversionData>> _typeToConversions = new();

            public ConversionWrapper(PackConverterData.LevelData dataChanges)
            {
                foreach (var conversionData in dataChanges.Conversions)
                {
                    if (!_typeToConversions.TryGetValue(conversionData.ReceiveType, out var list))
                        _typeToConversions[conversionData.ReceiveType] = list = new();

                    list.Add(conversionData);
                }
            }

            public void ApplyConversions(PlayerAgent player, ResourceType type, float amount)
            {
                if (!_typeToConversions.TryGetValue(type, out var list)) return;

                float standard = 0;
                float special = 0;
                float tool = 0;
                float disinfect = 0;
                float health = 0;
                AddConversions(list, amount, ref standard, ref special, ref tool, ref health, ref disinfect);
                ApplyConversions(player, standard, special, tool, health, disinfect);
            }

            public void ApplyConversions(PlayerAgent player, float standardAmount, float specialAmount, float toolAmount)
            {
                float standard = 0;
                float special = 0;
                float tool = 0;
                float disinfect = 0;
                float health = 0;

                if (_typeToConversions.TryGetValue(ResourceType.Standard, out var list))
                    AddConversions(list, standardAmount, ref standard, ref special, ref tool, ref health, ref disinfect);
                if (_typeToConversions.TryGetValue(ResourceType.Special, out list))
                    AddConversions(list, specialAmount, ref standard, ref special, ref tool, ref health, ref disinfect);
                if (_typeToConversions.TryGetValue(ResourceType.Ammo, out list))
                    AddConversions(list, (standardAmount + specialAmount) / 2f, ref standard, ref special, ref tool, ref health, ref disinfect);
                if (_typeToConversions.TryGetValue(ResourceType.Tool, out list))
                    AddConversions(list, toolAmount, ref standard, ref special, ref tool, ref health, ref disinfect);

                ApplyConversions(player, standard, special, tool, health, disinfect);
            }

            private static void AddConversions(List<PackConverterData.LevelData.ConversionData> list, float amount, ref float standard, ref float special, ref float tool, ref float health, ref float disinfect)
            {
                foreach (var conversion in list)
                {
                    float value = conversion.Value * amount;
                    switch (conversion.ConvertType)
                    {
                        case ResourceType.Standard:
                            standard += value;
                            break;
                        case ResourceType.Special:
                            special += value;
                            break;
                        case ResourceType.Ammo:
                            standard += value / 2f;
                            special += value / 2f;
                            break;
                        case ResourceType.Tool:
                            tool += value;
                            break;
                        case ResourceType.Health:
                            health += value;
                            break;
                        case ResourceType.Disinfect:
                            disinfect += value;
                            break;
                    }
                }
            }

            private static void ApplyConversions(PlayerAgent player, float standard, float special, float tool, float health, float disinfect)
            {
                // Use internals of GiveAmmoRel, GiveHealth, GiveDisinfection to avoid hitting patches
                if (standard != 0 || special != 0 || tool != 0)
                    PlayerBackpackManager.GiveAmmoToPlayer(player.Owner, standard, special, tool);
                if (health != 0)
                    PlayerBackpackManager.PickupHealthRel(health, player);
                if (disinfect != 0)
                {
                    player.Damage.ModifyInfection(new pInfection
                    {
                        amount = 0f - disinfect,
                        effect = pInfectionEffect.DisinfectionPack,
                        mode = pInfectionMode.Add
                    }, sync: true, updatePageMap: true);
                }
            }
        }
    }
}
