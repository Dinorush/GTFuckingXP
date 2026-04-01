using GTFuckingXP.Enums;
using GTFuckingXP.Extensions.Information.Level.Json;
using GTFuckingXP.Information.NetworkingInfo;

namespace GTFuckingXP.Information.Level
{
    /// <summary>
    /// Contains information to be read as an individual level
    /// </summary>
    public class Level
    {
        public Level()
        { }

        public Level(LevelReachedInfo levelData) : this(levelData.LevelNumber, 0, levelData.HealthMultiplier, 0f, 0f,
                null, levelData.GetCustomScaling())
        { }

        public Level(int levelNumber, uint totalXp, float healthMultiplier, float meleeMultiplier, float weaponMultiplier, Dictionary<SingleBuff, float> singleUseBuffs,
            Dictionary<CustomScaling, float> customScaling, LocaleText customLevelUpPopupText = default, LocaleText customLevelStatsText = default)
        {
            LevelNumber = levelNumber;
            TotalXpRequired = totalXp;

            HealthMultiplier = healthMultiplier;
            MeleeDamageMultiplier = meleeMultiplier;
            WeaponDamageMultiplier = weaponMultiplier;
            LevelUpBonus = singleUseBuffs;

            CustomScaling = customScaling;
            CustomLevelStatsText = customLevelStatsText;
            CustomLevelUpPopupText = customLevelUpPopupText;
        }

        /// <summary>
        /// Gets or sets the number this level represents.
        /// </summary>
        public int LevelNumber { get; set; }

        /// <summary>
        /// Gets or sets the amount of xp you need to achieve this level.
        /// </summary>
        public uint TotalXpRequired { get; set; }

        /// <summary>
        /// Gets or sets the custom level reached popup text.
        /// </summary>
        public LocaleText CustomLevelUpPopupText { get; set; }

        /// <summary>
        /// Gets or sets the custom levelstats the player sees at the bottom right ingame.
        /// </summary>
        public LocaleText CustomLevelStatsText { get; set; }

        /// <summary>
        /// Gets or sets the amount the basic hp get scaled with this value.
        /// </summary>
        public float HealthMultiplier { get; set; }

        /// <summary>
        /// Gets or sets the amount the basic melee damage gets scaled with this value.
        /// </summary>
        public float MeleeDamageMultiplier { get; set; }

        /// <summary>
        /// Gets or sets the amount the basic weapon damage gets scaled with this value.
        /// </summary>
        public float WeaponDamageMultiplier { get; set; }

        //public float PrecisionMultiplier { get; set; }

        /// <summary>
        /// Gets or sets all customscaling options
        /// </summary>
        public Dictionary<CustomScaling, float> CustomScaling { get; set; }

        /// <summary>
        /// Gets or sets the single use buffs that gets applied when reaching this level.
        /// </summary>
        public Dictionary<SingleBuff, float> LevelUpBonus { get; set; }

        public override string ToString()
        {
            return $"LevelNumber {LevelNumber}";
        }
    }
}
