using GTFuckingXP.Enums;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTFuckingXP.Extensions.Information.Level.Json
{
    internal static class BuffJson
    {
        private static readonly JsonSerializerOptions _setting = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,
        };

        static BuffJson()
        {
            _setting.Converters.Add(new JsonStringEnumConverter());
            _setting.Converters.Add(new CustomBuffConverter());
            _setting.Converters.Add(new SingleBuffConverter());
            _setting.Converters.Add(new StartBuffConverter());
            _setting.Converters.Add(new LegacyBuffConverter<CustomScaling>());
            _setting.Converters.Add(new LegacyBuffConverter<SingleBuff>());
            _setting.Converters.Add(new LegacyBuffConverter<StartBuff>());
        }

        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _setting);
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, _setting);
        }
    }
}
