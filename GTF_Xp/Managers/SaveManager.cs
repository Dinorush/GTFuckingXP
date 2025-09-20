using BepInEx;
using EndskApi.Api;
using GTFuckingXP.Extensions;
using GTFuckingXP.Information.ClassSelector;
using GTFuckingXP.Information.Level;
using SNetwork;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace GTFuckingXP.Managers
{
    public static class SaveManager
    {
        private static readonly JsonSerializerOptions _settings = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public static readonly string SavePath;
        private static LevelLayout? _loadedLayout;
        private static bool _hasLoaded = false;

        static SaveManager()
        {
            var saveDir = Path.Combine(Paths.BepInExRootPath, "GameData/Favorites");
            SavePath = Path.Combine(saveDir, "XPData.json");
            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);
        }

        public static void SaveLayout(LevelLayout layout)
        {
            if (layout.PersistentId == _loadedLayout?.PersistentId) return;

            _loadedLayout = layout;
            SaveData _data = new(layout);
            File.WriteAllText(SavePath, JsonSerializer.Serialize(_data, _settings));
            LogManager.Warn($"Saved layout to {SavePath}");
        }

        public static bool TryLoadLayout([MaybeNullWhen(false)] out LevelLayout layout)
        {
            if (_hasLoaded)
            {
                layout = _loadedLayout;
                return CheckValidLayout();
            }

            _hasLoaded = true;
            if (!File.Exists(SavePath))
            {
                layout = null;
                return false;
            }

            string content;
            try
            {
                content = File.ReadAllText(SavePath);
            }
            catch
            {
                layout = null;
                return false;
            }

            var data = JsonSerializer.Deserialize<SaveData>(content, _settings);
            if (data == null)
            {
                layout = null;
                return false;
            }

            var levelLayouts = CacheApi.GetInstance<List<LevelLayout>>(CacheApiWrapper.XpModCacheName);
            layout = _loadedLayout = levelLayouts.Find(l => l.PersistentId == data.LayoutID);
            return CheckValidLayout();
        }

        private static bool CheckValidLayout()
        {
            if (_loadedLayout == null) return false;
            
            var groupID = _loadedLayout.GroupPersistentId;
            var groups = CacheApi.GetInstance<List<Group>>(CacheApiWrapper.XpModCacheName);
            var parentGroup = groups.Find(group => group.PersistentId == groupID);
            return parentGroup != null && parentGroup.AllowedForCount(SNet.SessionHub.PlayersInSession.Count);
        }

        class SaveData
        {
            public string Version { get; } = "1.0.0";
            public int LayoutID { get; set; } = 0;

            public SaveData() { }
            public SaveData(LevelLayout layout) => LayoutID = layout.PersistentId;
        }
    }
}
