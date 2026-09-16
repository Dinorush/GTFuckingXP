namespace Expansions.Information.PackConverter
{
    public class ToolAbilityData
    {
        public static readonly ToolAbilityData[] Template = new ToolAbilityData[]
        {
            new()
            {
                DebugName = "Template",
                DataChanges = new()
                {
                    new()
                }
            }
        };

        public int LevelLayoutID { get; set; } = 0;
        public string DebugName { get; set; } = string.Empty;
        public List<LevelData> DataChanges { get; set; } = new();

        public class LevelData
        {
            public int Level { get; set; } = 0;

            public bool SentryRecall { get; set; } = false;
            public float SentryRecallSpeed { get; set; } = 1f;
            public bool MoveMines { get; set; } = false;
            public bool MineRecall { get; set; } = false;
            public float MineRecallSpeed { get; set; } = 1f;
        }
    }
}
