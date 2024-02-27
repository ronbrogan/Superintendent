using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Superintendent.Core.Serialization
{
    public class HexStringFuncConverter : JsonConverterFactory
    {
        private ConcurrentDictionary<Type, JsonConverter> converterInstances = new();

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo(typeof(Fun));
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return converterInstances.GetOrAdd(typeToConvert, t =>
            {
                return (JsonConverter)Activator.CreateInstance(typeof(FunConverter<>).MakeGenericType(typeToConvert));
            });
        }

        private class FunConverter<T> : JsonConverter<T> where T : Fun, IFun<T>
        {
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (HexStringNintConverter.TryReadNintFromHex(ref reader, out nint value))
                {
                    return T.Create(value);
                }

                throw new JsonException($"Unable to decode native integer from {reader.GetString()}");
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                writer.WriteStringValue("0x" + Convert.ToString(value.Address, 16));
            }
        }
    }
}
