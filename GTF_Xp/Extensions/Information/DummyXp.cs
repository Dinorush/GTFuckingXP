namespace GTFuckingXP.Information
{
    internal class DummyXp : IXpData
    {
        public DummyXp(int xpGain, int debuffXp)
        {
            XpGain = xpGain;
            DebuffXp = debuffXp;
            LevelScalingXpDecrese = 0;
        }

        public int XpGain { get; set; }
        public int DebuffXp { get; set; }
        public int LevelScalingXpDecrese { get; set; }
    }
}
