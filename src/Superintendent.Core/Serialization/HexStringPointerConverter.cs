using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Superintendent.Core.Serialization
{
    public class HexStringPointerConverter : JsonConverterFactory
    {
        private ConcurrentDictionary<Type, JsonConverter> converterInstances = new();

        public override bool CanConvert(Type typeToConvert)
        {
            return  typeToConvert.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().IsAssignableTo(typeof(IPtr<>)));
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return converterInstances.GetOrAdd(typeToConvert, t =>
            {
                return (JsonConverter)Activator.CreateInstance(typeof(PtrConverter<>).MakeGenericType(typeToConvert));
            });
        }

        private class PtrConverter<T> : JsonConverter<T> where T : IPtr<T>
        {
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (HexStringNintConverter.TryReadNintFromHex(ref reader, out nint value))
                {
                    return T.Create(value, Array.Empty<nint>());
                }

                if(TryReadHexStringPointerWithChain(ref reader, out nint baseOffset, out nint[] chain))
                {
                    return T.Create(baseOffset, chain);
                }

                throw new JsonException($"Unable to decode native integer from {reader.GetString()}");
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                if(value.Chain == null || value.Chain.Length == 0)
                {
                    writer.WriteStringValue("0x" + Convert.ToString(value.Base, 16));
                }
                else
                {
                    writer.WriteStartArray();
                    writer.WriteStringValue("0x" + Convert.ToString(value.Base, 16));
                    foreach(var c in value.Chain)
                    {
                        writer.WriteStringValue("0x" + Convert.ToString(c, 16));
                    }

                }
            }

            private bool TryReadHexStringPointerWithChain(ref Utf8JsonReader reader, out nint baseOffset, out nint[] chain)
            {
                baseOffset = 0;
                chain = Array.Empty<nint>();

                if (reader.TokenType != JsonTokenType.StartArray)
                    return false;

                // eat start of array
                reader.Read();

                if(!HexStringNintConverter.TryReadNintFromHex(ref reader, out baseOffset))
                {
                    throw new JsonException($"Unable to decode pointer base from {reader.GetString()}");
                }

                reader.Read();

                var chainList = new List<nint>();
                while(reader.TokenType != JsonTokenType.EndArray)
                {
                    if(!HexStringNintConverter.TryReadNintFromHex(ref reader, out var chainLink))
                    {
                        throw new JsonException($"Unable to decode pointer chain link from {reader.GetString()}");
                    }

                    chainList.Add(chainLink);
                    reader.Read();
                }

                // verify end of array
                Debug.Assert(reader.TokenType == JsonTokenType.EndArray);

                chain = chainList.ToArray();
                return true;
            }
        }
    }
}
