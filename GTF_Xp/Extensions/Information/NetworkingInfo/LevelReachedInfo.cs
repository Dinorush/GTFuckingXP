using GTFuckingXP.Enums;
using System.Runtime.InteropServices;

namespace GTFuckingXP.Information.NetworkingInfo
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LevelReachedInfo
    {
        private const int BufferSize = (int) CustomScaling.Count + 1;

        public LevelReachedInfo(int levelNumber, float healthMultiplier, Dictionary<CustomScaling, float> customScaling)
        {
            LevelNumber = levelNumber;
            HealthMultiplier = healthMultiplier;
            _customScalingEnum = new CustomScaling[BufferSize];
            _customScalingValue = new float[BufferSize];
            int i = 0;
            foreach (var kv in customScaling)
            {
                _customScalingEnum[i] = kv.Key;
                _customScalingValue[i] = kv.Value;
                i++;
            }
            _customScalingEnum[customScaling.Count + 1] = CustomScaling.NetworkBreak;
        }

        public int LevelNumber;
        public float HealthMultiplier;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = BufferSize)]
        private readonly CustomScaling[] _customScalingEnum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = BufferSize)]
        private readonly float[] _customScalingValue;

        public readonly Dictionary<CustomScaling, float> GetCustomScaling()
        {
            Dictionary<CustomScaling, float> result = new();
            for (int i = 0; i < BufferSize; i++)
            {
                if (_customScalingEnum[i] == CustomScaling.NetworkBreak)
                    return result;
                result.Add(_customScalingEnum[i], _customScalingValue[i]);
            }
            return result;
        }
    }
}
