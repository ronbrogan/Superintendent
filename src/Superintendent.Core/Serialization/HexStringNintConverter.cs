using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Superintendent.Core.Serialization
{
    public class HexStringNintConverter : JsonConverter<nint>
    {
        public override nint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (TryReadNintFromHex(ref reader, out var value))
                return value;

            throw new JsonException($"Unable to decode native integer from {reader.GetString()}");
        }

        public override void Write(Utf8JsonWriter writer, nint value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("0x" + Convert.ToString(value, 16));
        }

        public static bool TryReadNintFromHex(ref Utf8JsonReader reader, out nint value)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringVal = reader.GetString();
                if (stringVal.StartsWith("0x"))
                {
                    var address = Convert.ToInt64(stringVal[2..], 16);
                    value = (nint)address;
                    return true;
                }
                else if (long.TryParse(stringVal, out var address))
                {
                    value = (nint)address;
                    return true;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                value = (nint)reader.GetInt64();
                return true;
            }

            value = default;
            return false;
        }
    }
}
