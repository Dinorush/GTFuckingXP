using GTFuckingXP.Enums;

namespace GTFuckingXP.Information.Level
{
    public class StartingBuff
    {
        public StartingBuff(StartBuff startBuff, float value)
        {
            StartBuff = startBuff;
            Value = value;
        }

        public StartBuff StartBuff { get; set; } = StartBuff.Invalid;

        public float Value { get; set; }
    }
}
