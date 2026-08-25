// Core/Models/PassiveLocaleEntry.cs

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LethelModHelper.Core.Models
{
    /// <summary>
    /// 自定义 JsonConverter 处理 string/int 转换
    /// </summary>
    public class StringOrIntConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                System.Text.Json.JsonTokenType.String => reader.GetString() ?? "",
                System.Text.Json.JsonTokenType.Number => reader.GetInt64().ToString(),
                System.Text.Json.JsonTokenType.Null => "",
                _ => reader.GetString() ?? ""
            };
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    /// <summary>
    /// Passive 本地化条目
    /// 对应 passiveList/*.json 中的单个条目
    /// </summary>
    public class PassiveLocaleEntry
    {
        [JsonPropertyName("id")]
        [JsonConverter(typeof(StringOrIntConverter))]
        public string id { get; set; } = "";

        [JsonPropertyName("name")]
        public string name { get; set; } = "";

        [JsonPropertyName("desc")]
        public string desc { get; set; } = "";

        [JsonPropertyName("summary")]
        public string summary { get; set; } = "";

        [JsonPropertyName("flavor")]
        public string flavor { get; set; } = "";
    }
}