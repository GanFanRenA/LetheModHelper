// Core/Models/SkillLocaleEntry.cs

using System.Text.Json;
using System.Text.Json.Serialization;

namespace LethelModHelper.Core.Models
{
    /// <summary>
    /// 技能本地化条目
    /// 对应 skillList/*.json 中的单个条目
    /// </summary>
    public class SkillLocaleEntry
    {
        [JsonIgnore]
        public string Id
        {
            get => _id?.ToString() ?? "";
            set => _id = value;
        }

        [JsonPropertyName("id")]
        public object? IdRaw
        {
            get => _id;
            set
            {
                if (value == null)
                    _id = null;
                else if (value is JsonElement element)
                {
                    // 处理 JSON 元素
                    if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
                        _id = element.GetInt64().ToString();
                    else if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                        _id = element.GetString();
                    else
                        _id = element.ToString();
                }
                else
                    _id = value.ToString();
            }
        }
        private string? _id;

        [JsonPropertyName("levelList")]
        public List<SkillLocaleLevel> levelList { get; set; } = new();
    }

    /// <summary>
    /// 技能本地化 - 等级数据 (实际只有一个等级)
    /// </summary>
    public class SkillLocaleLevel
    {
        [System.Text.Json.Serialization.JsonPropertyName("level")]
        public int level { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string name { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("desc")]
        public string desc { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("flavor")]
        public string flavor { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("abName")]
        public string abName { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("coinlist")]
        public List<SkillLocaleCoin> coinlist { get; set; } = new();
    }

    /// <summary>
    /// 技能本地化 - 硬币描述
    /// </summary>
    public class SkillLocaleCoin
    {
        [System.Text.Json.Serialization.JsonPropertyName("coindescs")]
        public List<SkillLocaleCoinDesc> coindescs { get; set; } = new();
    }

    /// <summary>
    /// 技能本地化 - 单个硬币描述
    /// </summary>
    public class SkillLocaleCoinDesc
    {
        [System.Text.Json.Serialization.JsonPropertyName("desc")]
        public string desc { get; set; } = "";
    }
}