namespace Expansions.Information.PackConverter
{
    public class PackConverterData
    {
        public static readonly PackConverterData[] Template = new PackConverterData[]
        {
            new PackConverterData()
            {
                DebugName = "Template",
                DataChanges = new()
                {
                    new()
                    {
                        Level = 0,
                        Conversions = new()
                        {
                            new()
                            {
                                ReceiveType = ResourceType.Ammo,
                                ConvertType = ResourceType.Tool,
                                Value = 0.5f
                            }
                        },
                    },
                    new()
                    {
                        Level = 10,
                        Conversions = new()
                        {
                            new()
                            {
                                ReceiveType = ResourceType.Standard,
                                ConvertType = ResourceType.Tool,
                                Value = 0.25f
                            },
                            new()
                            {
                                ReceiveType = ResourceType.Special,
                                ConvertType = ResourceType.Tool,
                                Value = 0.25f
                            },
                            new()
                            {
                                ReceiveType = ResourceType.Health,
                                ConvertType = ResourceType.Disinfect,
                                Value = 1f
                            }
                        },
                    }
                }
            }
        };

        public int LevelLayoutID { get; set; } = 0;
        public string DebugName { get; set; } = string.Empty;
        public List<LevelData> DataChanges { get; set; } = new();

        public class LevelData
        {
            public int Level { get; set; } = 0;
            public List<ConversionData> Conversions { get; set; } = new();

            public class ConversionData
            {
                public ResourceType ReceiveType { get; set; } = ResourceType.None;
                public ResourceType ConvertType { get; set; } = ResourceType.None;
                public float Value { get; set; } = 0;
            }
        }
    }

    public enum ResourceType
    {
        None,
        Standard,
        Special,
        Ammo,
        Tool,
        Health,
        Disinfect
    }
}
