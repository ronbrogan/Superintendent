using System.Text.Json;

namespace Superintendent.Core.Serialization
{
    public class SerializationOptions
    {
        public static JsonSerializerOptions DefaultJson = new()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };

        static SerializationOptions()
        {
            AddJsonConverters(DefaultJson);
        }

        public static void AddJsonConverters(JsonSerializerOptions options)
        {
            options.Converters.Add(new HexStringNintConverter());
            options.Converters.Add(new HexStringFuncConverter());
            options.Converters.Add(new HexStringPointerConverter());
        }
    }
}
