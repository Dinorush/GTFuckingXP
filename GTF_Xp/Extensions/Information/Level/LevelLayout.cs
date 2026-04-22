using GTFuckingXP.Enums;
using GTFuckingXP.Extensions.Information.Level.Json;
using System.Collections.Generic;

namespace GTFuckingXP.Information.Level
{
    /// <summary>
    /// Contains all levels
    /// </summary>
    public class LevelLayout
    {
        public LevelLayout(int persistentId, LocaleText header, int groupPersistentId, LocaleText infoText, Dictionary<StartBuff, float> startingBuffs, List<Level> levels)
        {
            PersistentId = persistentId;
            GroupPersistentId = groupPersistentId;
            Header = header;
            InfoText = infoText;
            StartingBuffs = startingBuffs;
            Levels = levels;
        }

        /// <summary>
        /// Gets or sets the id leading to that 
        /// </summary>
        public LocaleText Header { get; set; }

        /// <summary>
        /// Gets or sets the name of the header, that is used in the scrollwindow of the loadoutpage.
        /// </summary>
        public int GroupPersistentId { get; set; }

        /// <summary>
        /// Gets or sets an unique id alongst all <see cref="LevelLayout"/>.
        /// </summary>
        public int PersistentId { get; set; }

        /// <summary>
        /// Gets or sets the info text for this class.
        /// </summary>
        public LocaleText InfoText { get; set; }

        ///// <summary>
        ///// Gets or sets all constant booster effects that does gets applied, when this <see cref="LevelLayout"/> is chosen.
        ///// </summary>
        //public Dictionary<AgentModifier, float> ConstantBoosterEffects { get; set; }

        public Dictionary<StartBuff, float>? StartingBuffs { get; set; }

        /// <summary>
        /// Gets or sets all levels containing in this layout.
        /// </summary>
        public List<Level> Levels { get; set; }

        /// <summary>
        /// When JSON reading is done, force Levels into ascending, consecutive order.
        /// </summary>
        public void OnReadDone()
        {
            Levels.Sort((a, b) => a.LevelNumber == b.LevelNumber ? 0 : (a.LevelNumber < b.LevelNumber ? -1 : 1));
            for (int i = 0; i < Levels.Count; i++)
                Levels[i].LevelNumber = i;
        }

        public Level? GetLevel(int index)
        {
            if (index < Levels.Count)
                return Levels[index];
            return null;
        }
    }
}
