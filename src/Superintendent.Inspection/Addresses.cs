using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Superintendent.Huragok
{
    public interface IPointerObject
    {
        long BaseAddress { get; }
    }

    public class AddressTypeAttribute : Attribute
    {
        public AddressTypeAttribute(Type type)
        {
            this.Type = type;
        }

        public Type Type { get; }
    }

    public class SinglePointer : IPointerObject
    {
        public long BaseAddress { get; set; }

        public SinglePointer(long l)
        {
            this.BaseAddress = l;
        }

        public static implicit operator long(SinglePointer p) => p.BaseAddress;
        public static implicit operator SinglePointer(long l) => new SinglePointer(l);
    }

    [JsonConverter(typeof(Converter))]
    public class FunctionPointer
    {
        public long Address { get; set; }

        public FunctionPointer(long l)
        {
            this.Address = l;
        }

        public static implicit operator long(FunctionPointer p) => p.Address;
        public static implicit operator FunctionPointer(long l) => new FunctionPointer(l);

        public class Converter : JsonConverter<FunctionPointer>
        {
            public override FunctionPointer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var stringVal = reader.GetString();
                    if (stringVal.StartsWith("0x"))
                    {
                        var address = Convert.ToInt64(stringVal[2..], 16);
                        return new FunctionPointer(address);
                    }
                    else if (long.TryParse(stringVal, out var address))
                    {
                        return new FunctionPointer(address);
                    }
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    return new FunctionPointer(reader.GetInt64());
                }

                throw new JsonException($"Unable to decode FunctionPointer from {reader.GetString()}");
            }

            public override void Write(Utf8JsonWriter writer, FunctionPointer value, JsonSerializerOptions options)
            {
                writer.WriteStringValue("0x" + Convert.ToString(value.Address, 16));
            }
        }
    }
}
