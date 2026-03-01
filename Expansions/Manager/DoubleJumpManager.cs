using EndskApi.Api;
using Expansions.Patches.DoubleJump;
using GTFuckingXp.Information;
using GTFuckingXP.Information.Level;
using HarmonyLib;
using MTFO.Managers;
using System.Text.Json;
using XpExpansions.Information.DoubleJump;

namespace XpExpansions.Manager
{
    public class DoubleJumpManager : BaseManager
    {
        public const string OldGUID = "com.mccad00.DoubleJump";
        public const string DoubleJumpXpExpansionId = "Endskill.DoubleJumpExpansion";
        private const string _expansionFileName = "DoubleJumpExpansion.json";
        private const string _configFileName = "DoubleJumpDefaultConfig.json";

        public static DoubleJumpConfig ActiveConfig { get; private set; } = new();
        private static DoubleJumpConfig _baseConfig = new();
        public static bool DoubleJumpUnlocked { get; private set; }

        public DoubleJumpManager()
        {
            Harmony.UnpatchID(OldGUID);
            DoubleJumpUnlocked = false;

            Harmony harmony = new(DoubleJumpXpExpansionId);
            harmony.PatchAll(typeof(PLOC_Patches));
            harmony.PatchAll(typeof(FallDamagePatches));
            harmony.PatchAll(typeof(SuperJumpPatches));
        }

        public override void Initialize()
        {
            WriteDefaultJsonBlocks();
            UpdateEverything();
        }

        public override void LevelCleanup()
        {
            DoubleJumpUnlocked = false;
        }

        public override void LevelReached(Level level)
        {
            LogManager.Message($"LevelReached, DoubleJump {level.ToString()}");
            
            var levelLayout = GTFuckingXP.Extensions.CacheApiWrapper.GetCurrentLevelLayout();
            var data = CacheApi.GetInstance<List<DoubleJumpData>>(Extensions.CacheApiWrapper.ExtensionCacheName);
            var doubleJump = data.FirstOrDefault(it => it.LevelLayoutPersistentId == levelLayout.PersistentId);
            if (doubleJump != null)
            {
                if (doubleJump.UnlockAtLevel <= level.LevelNumber)
                {
                    DoubleJumpUnlocked = true;
                    ActiveConfig = doubleJump.DoubleJumpConfig ?? _baseConfig;
                }
            }
            else
                DoubleJumpUnlocked = false;
        }


        private void WriteDefaultJsonBlocks()
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            var doubleJumpPath = Path.Combine(FolderPath, _expansionFileName);
            if (!File.Exists(doubleJumpPath))
            {
                File.WriteAllText(doubleJumpPath, DefaultConstants.DoubleJumpExpansion);
            }

            string configPath = Path.Combine(FolderPath, _configFileName);
            if (File.Exists(configPath))
            {
                _baseConfig = JsonSerializer.Deserialize<DoubleJumpConfig>(File.ReadAllText(configPath))!;
            }
            else
            {
                var origConfigPath = Path.Combine(ConfigManager.CustomPath, "mccad00", "DoubleJump.json");
                if (File.Exists(origConfigPath))
                    _baseConfig = JsonSerializer.Deserialize<DoubleJumpConfig>(File.ReadAllText(origConfigPath))!;
                else
                    _baseConfig = new();

                File.WriteAllText(configPath, JsonSerializer.Serialize(_baseConfig, new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true }));
            }
        }

        private void UpdateEverything()
        {
            CacheApi.SaveInstance(JsonSerializer.Deserialize<List<DoubleJumpData>>(
                File.ReadAllText(Path.Combine(FolderPath, _expansionFileName))),
                Extensions.CacheApiWrapper.ExtensionCacheName);
        }
    }
}
