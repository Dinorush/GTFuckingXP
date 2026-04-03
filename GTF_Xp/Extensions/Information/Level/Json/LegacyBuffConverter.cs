using GTFuckingXP.Managers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTFuckingXP.Extensions.Information.Level.Json
{
    public sealed class LegacyBuffConverter<T> : JsonConverter<Dictionary<T, float>> where T : struct, Enum
    {
        private static readonly string TypeName = typeof(T).Name;
        private static readonly T InvalidValue = (T)Enum.ToObject(typeof(T), -1);
        public override Dictionary<T, float>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (TryLegacyRead(ref reader, out var result))
                return result;

            return JsonSerializer.Deserialize<Dictionary<T, float>>(ref reader, GetExcludedOptions(options));
        }

        private static bool TryLegacyRead(ref Utf8JsonReader reader, out Dictionary<T, float> result)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                result = null!;
                return false;
            }

            result = new();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return true;

                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException($"LegacyFormat: Expected {TypeName} to be an object, found {reader.TokenType}");

                reader.Read();

                if (reader.TokenType == JsonTokenType.EndObject)
                    continue;

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException($"LegacyFormat: Expected {TypeName} name or object property, found {reader.TokenType}");

                string name = reader.GetString()!;
                float value = 1f;
                // Name:Value case
                if (name.TryToEnum(out T buff))
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.Number) throw new JsonException($"LegacyFormat: Expected {TypeName} name to be followed by a value, found {reader.TokenType}");

                    value = reader.GetSingle();
                    reader.Read();
                }
                else // Separate properties case
                {
                    do
                    {
                        if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException($"LegacyFormat: Expected {TypeName} object property, found {reader.TokenType}");

                        name = reader.GetString()!;
                        reader.Read();
                        switch (name)
                        {
                            case "Value":
                                value = reader.GetSingle();
                                break;
                            default:
                                if (!name.EndsWith("Buff"))
                                {
                                    LogManager.Warn($"LegacyFormat: Expected {TypeName}:Value format or \"---Buff\" field for {TypeName} object format, found \"{name}\"! Skipping...");
                                    buff = InvalidValue;
                                    break;
                                }

                                if (reader.TokenType != JsonTokenType.String)
                                {
                                    if (!reader.TryGetInt32(out var enumVal))
                                        throw new JsonException($"LegacyFormat: Unable to read int enum value from \"{reader.GetString()}\" when parsing {TypeName}! (Field name: \"{name}\")");
                                    buff = (T)Enum.ToObject(typeof(T), enumVal);
                                }
                                else if (!reader.GetString().TryToEnum(out buff))
                                {
                                    LogManager.Warn($"LegacyFormat: Unable to get {TypeName} type for \"{reader.GetString()}\"! Skipping...");
                                    buff = InvalidValue;
                                }
                                break;
                        }
                    } while (reader.Read() && reader.TokenType != JsonTokenType.EndObject);
                }

                if (reader.TokenType == JsonTokenType.EndObject)
                    result.TryAdd(buff, value);
                else
                    throw new JsonException($"LegacyFormat: Expected EndObject token when parsing {TypeName}, found {reader.TokenType}");
            }

            throw new JsonException($"LegacyFormat: Expected EndArray token when parsing {TypeName}, found {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<T, float> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, GetExcludedOptions(options));
        }

        private static JsonSerializerOptions GetExcludedOptions(JsonSerializerOptions options)
        {
            JsonSerializerOptions newOptions = new(options);
            var converters = newOptions.Converters;
            for (int i = 0; i < converters.Count; i++)
            {
                if (converters[i] is LegacyBuffConverter<T>)
                {
                    converters.RemoveAt(i);
                    break;
                }
            }
            return newOptions;
        }
    }
}
