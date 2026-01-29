using UnityEngine;

namespace GTFuckingXP.Information.NetworkingInfo
{
    public struct StaticXpInfo : IXpData
    {
        public StaticXpInfo(int xpGain, int debuffXp, int levelScaling, Vector3 position)
        {
            XpGain = (int) xpGain;
            DebuffXp = (int) debuffXp;
            LevelScalingXpDecrese = levelScaling;
            Position = position;
        }

        public int XpGain { get; set; }
        public int DebuffXp { get; set; }
        public int LevelScalingXpDecrese { get; set; }
        public Vector3 Position { get; set; }
    }
}
