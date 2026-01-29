namespace XpExpansions.Information.StartingXp
{
    public class StartingXpData
    {
        public StartingXpData(uint levelLayoutData, int startingXp)
        {
            LevelLayoutData = levelLayoutData;
            StartingXp = startingXp;
        }

        public uint LevelLayoutData { get; set; }
        public int StartingXp { get; set; }
    }
}
