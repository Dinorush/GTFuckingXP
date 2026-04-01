using System.Text.Json;
using System.Text.Json.Serialization;

namespace GTFuckingXP.Extensions.Information.Level.Json
{
    public sealed class LegacyBuffConverter<T> : JsonConverter<Dictionary<T, float>> where T : struct, Enum
    {
        private static readonly string TypeName = typeof(T).Name;
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

                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException($"LegacyFormat: Expected {TypeName} name or \"{TypeName}\" property");

                string name = reader.GetString()!;
                float value = 1f;
                // Name:Value case
                if (name.TryToEnum(out T buff))
                {
                    reader.Read();
                    if (reader.TokenType != JsonTokenType.Number) throw new JsonException($"LegacyFormat: Expected {TypeName} name to be followed by a value");

                    value = reader.GetSingle();
                    reader.Read();
                }
                else // Separate properties case
                {
                    do
                    {
                        if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException($"LegacyFormat: Expected \"{TypeName}\" property");

                        name = reader.GetString()!;
                        reader.Read();
                        switch (name)
                        {
                            case "Value":
                                value = reader.GetSingle();
                                break;
                            default:
                                if (reader.TokenType == JsonTokenType.String)
                                {
                                    if (!reader.GetString().TryToEnum(out buff))
                                        throw new JsonException($"LegacyFormat: Unable to get {TypeName} for string {reader.GetString()}");
                                }
                                else
                                    buff = (T)Enum.ToObject(typeof(T), reader.GetInt32());
                                break;
                        }
                    } while (reader.Read() && reader.TokenType != JsonTokenType.EndObject);
                }

                if (reader.TokenType == JsonTokenType.EndObject)
                    result.TryAdd(buff, value);
                else
                    throw new JsonException($"LegacyFormat: Expected EndObject token when parsing {TypeName}");
            }

            throw new JsonException($"LegacyFormat: Expected EndArray token when parsing {TypeName}");
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
