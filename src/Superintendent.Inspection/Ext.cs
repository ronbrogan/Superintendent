using Gee.External.Capstone.X86;
using Iced.Intel;
using PeNet.Header.Pe;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Superintendent.Huragok
{
    public static class Ext
    {
        public static long GetMemoryLocation(this X86Instruction instruction)
        {
            var reader = new ByteArrayCodeReader(instruction.Bytes);
            var decoder = Decoder.Create(64, reader);
            decoder.IP = (ulong)instruction.Address;
            var inst = decoder.Decode();
            return (long)inst.MemoryDisplacement64;
        }

        public static long GetCallLocation(this X86Instruction instruction)
        {
            var reader = new ByteArrayCodeReader(instruction.Bytes);
            var decoder = Decoder.Create(64, reader);
            decoder.IP = (ulong)instruction.Address;
            var inst = decoder.Decode();
            return (long)(inst.IsCallNear || inst.IsJmpNear ? inst.NearBranch64 : inst.FarBranch32);
        }

        public static InstructionInfo CreateInfo(this X86Instruction instruction)
        {
            var infofac = new InstructionInfoFactory();
            var reader = new ByteArrayCodeReader(instruction.Bytes);
            var decoder = Iced.Intel.Decoder.Create(64, reader);
            decoder.IP = (ulong)instruction.Address;
            var inst = decoder.Decode();
            var info = infofac.GetInfo(inst);
            return info;
        }

        public static long AddressToOffset(this ImageSectionHeader section, long address)
        {
            return address - (long)section.ImageBaseAddress - section.VirtualAddress + section.PointerToRawData;
        }

        public static long OffsetToAddress(this ImageSectionHeader section, long offset)
        {
            return (long)section.ImageBaseAddress + section.VirtualAddress + offset - section.PointerToRawData;
        }

        private static JsonSerializerOptions dumpOpts = new JsonSerializerOptions()
        {
            WriteIndented = true
        }; 

        static Ext()
        {
            dumpOpts.Converters.Add(new LongHexConverter());
        }


        public static void Dump(this object item)
        {
            if (item is string)
                Console.WriteLine(item);
            else
                Console.WriteLine(JsonSerializer.Serialize(item, dumpOpts));
        }

        public class LongHexConverter : JsonConverter<long>
        {
            public override long Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    return Convert.ToInt64(reader.GetString(), 16);
                }

                // Default behavior; will throw if TokenType != Number
                return reader.GetInt64();
            }

            public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            {
                writer.WriteStringValue($"0x{value:x}");
            }
        }
    }

}
