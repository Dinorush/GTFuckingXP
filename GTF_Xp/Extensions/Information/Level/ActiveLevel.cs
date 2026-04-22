namespace GTFuckingXP.Information.Level
{
    public struct ActiveLevel
    {
        public LevelLayout Layout;
        public Level Level;
        public int LevelNumber;

        public ActiveLevel(LevelLayout layout, int levelNumber)
        {
            Layout = layout;
            Level = layout.Levels[levelNumber];
            LevelNumber = levelNumber;
        }
    }
}
