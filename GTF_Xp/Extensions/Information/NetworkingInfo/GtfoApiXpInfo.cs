using UnityEngine;

namespace GTFuckingXP.Information.NetworkingInfo
{
    /// <summary>
    /// Struct that contains information needed to send xp distribution data between users.
    /// I hate structs, they just behave a really stupid imo
    /// </summary>
    public struct GtfoApiXpInfo : IXpData
    {
        public GtfoApiXpInfo(int xpGain, int debuffXp, int levelScaling, Vector3 position, bool forceDebuffXp = false)
        {
            XpGain = xpGain;
            DebuffXp = debuffXp;    
            LevelScalingXpDecrese = levelScaling;
            ForceDebuffXp = forceDebuffXp;

            PositionX = position.x;
            PositionY = position.y;
            PositionZ = position.z;
        }

        public bool ForceDebuffXp { get; set; }
        public int XpGain { get; set; }
        public int DebuffXp { get; set; }
        public int LevelScalingXpDecrese { get; set; }

        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
    }
}
