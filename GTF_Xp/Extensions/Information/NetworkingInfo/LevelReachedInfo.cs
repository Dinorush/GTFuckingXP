namespace GTFuckingXP.Information.NetworkingInfo
{
    public struct LevelReachedInfo
    {
        public LevelReachedInfo(int layoutID, int levelNumber)
        {
            LayoutID = layoutID;
            LevelNumber = levelNumber;
        }

        public int LevelNumber;
        public int LayoutID;
    }
}
