// Core/Models/SkillData.cs

using System.Collections.Generic;

namespace LethelModHelper.Core.Models
{
    /// <summary>
    /// 技能数据
    /// 对应 custom_limbus_data/skill/ 下的 JSON 文件
    /// </summary>
    public class SkillData
    {
        public List<SkillEntry> list { get; set; } = new();
    }

    /// <summary>
    /// 单个技能条目
    /// </summary>
    public class SkillEntry
    {
        [Editable(Label = "技能ID", ControlType = "Numeric", Min = 1, Max = 999999999, Order = 1)]
        public int id { get; set; }

        [Editable(Label = "技能类型", ControlType = "Dropdown",
                  Options = "SKILL,EGO_AWAKENING,EGO_CORROSION", Order = 2)]
        public string skillType { get; set; } = "SKILL";

        [Editable(Label = "技能等级", ControlType = "Dropdown",
                  Options = "1,2,3", Order = 3)]
        public int skillTier { get; set; }

        [Editable(Label = "前置条件", ControlType = "List", Order = 4, AllowAddRemove = true)]
        public List<string> requireIDList { get; set; } = new();
        public List<SkillDataEntry> skillData { get; set; } = new();
    }

    /// <summary>
    /// 技能数据条目（简化版，移除 gaksungLevel）
    /// </summary>
    public class SkillDataEntry
    {

        [Editable(Label = "罪孽属性", ControlType = "Dropdown",
                  Options = "CRIMSON,SCARLET,AMBER,SHAMROCK,AZURE,INDIGO,VIOLET,WHITE,BLACK,NEUTRAL",
                  Order = 1)]
        public string attributeType { get; set; } = "";

        [Editable(Label = "攻击类型", ControlType = "Dropdown",
                  Options = "HIT,PENETRATE,SLASH,NONE", Order = 2)]
        public string atkType { get; set; } = "HIT";

        [Editable(Label = "防御类型", ControlType = "Dropdown",
                  Options = "ATTACK,GUARD,EVADE,COUNTER", Order = 3)]
        public string defType { get; set; } = "ATTACK";

        [Editable(Label = "技能目标类型", ControlType = "Dropdown",
                  Options = "FRONT,RANDOM,ALL,SELF", Order = 4)]
        public string skillTargetType { get; set; } = "FRONT";

        [Editable(Label = "目标数量 (攻击权重)", ControlType = "Numeric", Min = 0, Max = 10, Order = 5)]
        public int targetNum { get; set; } = 1;

        [Editable(Label = "MP/SP消耗", ControlType = "Numeric", Min = 0, Max = 999, Order = 6)]
        public int mpUsage { get; set; }

        [Editable(Label = "技能等级修正", ControlType = "Numeric", Min = -10, Max = 20, Order = 7)]
        public int skillLevelCorrection { get; set; }

        [Editable(Label = "基础值", ControlType = "Numeric", Min = 0, Max = 999, Order = 8)]
        public int defaultValue { get; set; }

        [Editable(Label = "可攻击队友", ControlType = "Boolean", Order = 9)]
        public bool canTeamKill { get; set; }

        [Editable(Label = "可决斗/拼点", ControlType = "Boolean", Order = 10)]
        public bool canDuel { get; set; }

        [Editable(Label = "可切换目标", ControlType = "Boolean", Order = 11)]
        public bool canChangeTarget { get; set; }

        [Editable(Label = "技能动作", ControlType = "Text", Order = 12)]
        public string skillMotion { get; set; } = "";

        [Editable(Label = "视图类型", ControlType = "Dropdown",
                  Options = "BATTLE,ENCOUNTER", Order = 13)]
        public string viewType { get; set; } = "BATTLE";

        [Editable(Label = "格斗距离", ControlType = "Dropdown",
                  Options = "NEAR,FAR", Order = 14)]
        public string parryingCloseType { get; set; } = "NEAR";

        [Editable(Label = "距离", ControlType = "Numeric", Min = 0, Max = 20, Order = 15)]
        public double range { get; set; } = 6.5;

        [Editable(Label = "技能脚本列表", ControlType = "List", Order = 16, AllowAddRemove = true)]
        public List<ScriptEntry> abilityScriptList { get; set; } = new();

        [Editable(Label = "硬币列表", ControlType = "List", Order = 17, AllowAddRemove = true)]
        public List<CoinEntry> coinList { get; set; } = new();
    }

    /// <summary>
    /// 脚本条目
    /// </summary>
    public class ScriptEntry
    {
        [Editable(Label = "脚本名称", ControlType = "Text", Order = 1)]
        public string scriptName { get; set; } = "";
    }

    /// <summary>
    /// 硬币条目
    /// </summary>
    public class CoinEntry
    {
        [Editable(Label = "操作类型", ControlType = "Dropdown",
                  Options = "ADD,SUB,MUL", Order = 1)]
        public string operatorType { get; set; } = "ADD";

        [Editable(Label = "倍率 (硬币威力)", ControlType = "Numeric", Min = 0, Max = 999, Order = 2)]
        public int scale { get; set; } = 1;

        [Editable(Label = "硬币颜色", ControlType = "Dropdown",
                  Options = "GREY,GREEN,PURPLE", Order = 3)]
        public string color { get; set; } = "GREY";

        [Editable(Label = "硬币等级 (用于不可破坏/切除)", ControlType = "Numeric", Min = 0, Max = 5, Order = 4)]
        public int grade { get; set; }

        [Editable(Label = "硬币脚本", ControlType = "List", Order = 5, AllowAddRemove = true)]
        public List<ScriptEntry> abilityScriptList { get; set; } = new();
    }

    /// <summary>
    /// 罪孽属性映射 (用于显示)
    /// </summary>
    public static class AttributeTypeMapping
    {
        public static readonly Dictionary<string, string> AttributeNames = new()
        {
            { "CRIMSON", "Wrath (愤怒)" },
            { "SCARLET", "Lust (色欲)" },
            { "AMBER", "Sloth (怠惰)" },
            { "SHAMROCK", "Gluttony (暴食)" },
            { "AZURE", "Gloom (忧郁)" },
            { "INDIGO", "Pride (傲慢)" },
            { "VIOLET", "Envy (嫉妒)" },
            { "WHITE", "Madness (疯狂)" },
            { "BLACK", "Angst (恐惧)" },
            { "NEUTRAL", "Neutral (无)" }
        };

        public static string GetDisplayName(string attributeType)
        {
            return AttributeNames.TryGetValue(attributeType, out var name) ? name : attributeType;
        }
    }
}