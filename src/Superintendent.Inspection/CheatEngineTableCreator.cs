using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;

namespace Superintendent.Huragok
{
    public class CheatEngineTableCreator
    {
        private class Types
        {
            public static string Byte = "1 Byte";
            public static string Short = "2 Bytes";
            public static string Int = "4 Bytes";
            public static string Float = "Float";
            public static string Array = "Array of byte";

            public static string Get(AddressTypeAttribute? attr)
            {
                if (attr == null) return Byte;
                var t = attr.Type;
                if (t == typeof(byte)) return Byte;
                if (t == typeof(short) || t == typeof(ushort)) return Short;
                if (t == typeof(int) || t == typeof(uint)) return Int;
                if (t == typeof(float)) return Float;
                if (t == typeof(byte[])) return Array;
                return Byte;
            }
        }

        public void Create(List<CheatTableCheatEntry> entries,
            object addressObj,
            PropertyInfo member,
            string? module = null,
            long imageBase = 0)
        {
            var val = member.GetValue(addressObj);

            switch (val)
            {
                case long l:
                    {
                        entries.Add(new CheatTableCheatEntry()
                        {
                            ID = (byte)entries.Count,
                            Description = member.Name,
                            VariableType = Types.Get(member.GetCustomAttribute<AddressTypeAttribute>()),
                            Address = $"{module}+{l-imageBase:x}"
                        });
                        return;
                    }
                case IPointerObject p:
                    {
                        HandlePointerObj(p);
                        return;
                    }
            }

            void HandlePointerObj(IPointerObject obj)
            {
                var pointers = obj.GetType().GetProperties().Where(p => p.PropertyType == typeof(int)).ToArray();

                
                var baseEntry = new CheatTableCheatEntry()
                {
                    ID = (byte)entries.Count,
                    Description = member.Name,
                    VariableType = Types.Byte,
                    Address = $"{module}+{obj.BaseAddress - imageBase:x}"
                };

                baseEntry.Offsets = new[] { 0 };

                entries.Add(baseEntry);
                

                foreach(var p in pointers)
                {
                    var entry = new CheatTableCheatEntry()
                    {
                        ID = (byte)entries.Count,
                        Description = member.Name + "_" + p.Name,
                        VariableType = Types.Get(p.GetCustomAttribute<AddressTypeAttribute>()),
                        Address = $"{module}+{obj.BaseAddress-imageBase:x}"
                    };

                    entry.Offsets = new[] { (int)p.GetValue(obj)! };

                    entries.Add(entry);
                }
            }
        }
    }


    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public partial class CheatTable
    {
        [XmlArrayItem("CheatEntry", IsNullable = false)]
        public CheatTableCheatEntry[]? CheatEntries { get; set; }

        public object? UserdefinedSymbols { get; set; }

        [XmlAttribute()]
        public byte CheatEngineTableVersion { get; set; }
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class CheatTableCheatEntry
    {
        public byte ID { get; set; }

        public string? Description { get; set; }

        public CheatTableCheatEntryLastState? LastState { get; set; }

        public byte ShowAsHex { get; set; }

        [XmlIgnore()]
        public bool ShowAsHexSpecified { get; set; }

        public string? VariableType { get; set; }

        public byte Length { get; set; }

        [XmlIgnore()]
        public bool LengthSpecified { get; set; }

        public byte Unicode { get; set; }

        [XmlIgnore()]
        public bool UnicodeSpecified { get; set; }

        public byte CodePage { get; set; }

        [XmlIgnore()]
        public bool CodePageSpecified { get; set; }

        public byte ZeroTerminate { get; set; }

        [XmlIgnore()]
        public bool ZeroTerminateSpecified { get; set; }

        public byte ByteLength { get; set; }

        [XmlIgnore()]
        public bool ByteLengthSpecified { get; set; }

        public string? Address { get; set; }

        [XmlArrayAttribute()]
        [XmlArrayItem("Offset", IsNullable = false)]
        public int[]? Offsets { get; set; }
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public partial class CheatTableCheatEntryLastState
    {
        [XmlAttribute()]
        public string? Value { get; set; }

        [XmlAttribute()]
        public string? RealAddress { get; set; }
    }
}
