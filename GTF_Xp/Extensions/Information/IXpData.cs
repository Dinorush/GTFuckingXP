using GTFuckingXP.Extensions;

namespace GTFuckingXP.Information
{
    /// <summary>
    /// Interface for handling xp calculations
    /// </summary>
    public interface IXpData
    {
        /// <summary>
        /// Gets or sets the xp gained by this instance.
        /// </summary>
        int XpGain { get; set; }

        /// <summary>
        /// Gets or sets the xp gained by this instance while being in a debuff state.
        /// </summary>
        int DebuffXp { get; set; }

        /// <summary>
        /// Gets or sets the amount of xp that gets subtracted for each level you currently are.
        /// Example: 
        /// <see cref="XpGain"/> is 20
        /// <see cref="LevelScalingXpDecrese"/> is 4
        /// Your current level is 4
        /// On receiving this XpData you gain <see cref="XpGain"/> - (<see cref="LevelScalingXpDecrese"/> * 4)
        /// which results into 4XP.
        /// </summary>
        int LevelScalingXpDecrese { get; set; }

        public int GetXp(bool isDebuff)
        {
            int xpValue = isDebuff ? DebuffXp : XpGain;
            bool isNeg = xpValue < 0;
            if (isNeg)
                xpValue *= -1;

            var levelScalingDecreaseXp = LevelScalingXpDecrese * CacheApiWrapper.GetActiveLevel().LevelNumber;
            if (xpValue <= levelScalingDecreaseXp)
            {
                xpValue = 1;
            }
            else
            {
                xpValue -= levelScalingDecreaseXp;
            }

            if (isNeg)
                xpValue *= -1;
            return xpValue;
        }
    }
}
