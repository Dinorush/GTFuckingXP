using System.Text.Json.Serialization;
using System.Text.Json;
using GTFuckingXP.Information.Level;
using GTFuckingXP.Enums;

namespace GTFuckingXP.Extensions.Information.Level.Json
{
    public sealed class StartBuffConverter : JsonConverter<StartingBuff>
    {
        public override StartingBuff? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected StartingBuff to be an object");
            reader.Read();

            if (reader.TokenType == JsonTokenType.EndObject)
                return new StartingBuff(StartBuff.Invalid, 0f);

            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected StartingBuff name or \"StartingBuff\" property");

            StartingBuff buff;
            string name = reader.GetString()!;

            // Name:Value case
            if (name.TryToEnum<StartBuff>(out var startBuff))
            {
                reader.Read();
                if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected StartingBuff name to be followed by a value");

                float value = reader.GetSingle();
                reader.Read();
                buff = new StartingBuff(startBuff, value);
            }
            else // Separate properties case
            {
                buff = new StartingBuff(StartBuff.Invalid, 1f);
                do
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected property name when parsing StartingBuff");
                    string propertyName = reader.GetString()!;
                    reader.Read();
                    switch (propertyName)
                    {
                        case nameof(StartingBuff.StartBuff):
                            if (reader.TokenType == JsonTokenType.String)
                                buff.StartBuff = reader.GetString().ToEnum(StartBuff.Invalid);
                            else
                                buff.StartBuff = (StartBuff)reader.GetInt32();
                            break;
                        case nameof(SingleUseBuff.Value):
                            buff.Value = reader.GetSingle();
                            break;
                    }
                } while (reader.Read() && reader.TokenType != JsonTokenType.EndObject);
            }

            if (reader.TokenType == JsonTokenType.EndObject)
                return buff;

            throw new JsonException("Expected EndObject token when parsing StartingBuff");
        }

        public override void Write(Utf8JsonWriter writer, StartingBuff value, JsonSerializerOptions options)
        {
            writer.WriteNumber(value.StartBuff.ToString(), value.Value);
        }
    }
}
