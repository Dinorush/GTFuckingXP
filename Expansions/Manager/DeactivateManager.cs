using GTFuckingXP.Information.Level;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using XpExpansions.Information;

namespace XpExpansions.Manager
{
    public class DeactivateManager : BaseManager
    {
        private const string _expansionFileName = "DeactivateOnMissions.json";
        private const string _expansionClassFileName = "DeactivateClassLayout.json";

        public override void Initialize()
        {

        }

        private void WriteDefaultJsonBlocks()
        {
            if (!FolderPath.Contains("BepInEx"))
                return;

            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            var doubleJumpPath = Path.Combine(FolderPath, _expansionFileName);
            if (!File.Exists(doubleJumpPath))
            {
               
                File.WriteAllText(doubleJumpPath, ExpJson.Serialize(new List<int>() { 69420,42069}));
            }
        }
    }
}
