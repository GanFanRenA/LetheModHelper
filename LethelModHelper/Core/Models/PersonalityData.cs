using System.Collections.Generic;

namespace LethelModHelper.Core.Models
{
    public class PersonalityData
    {
        public List<PersonalityEntry> list { get; set; } = new();
    }

    public class PersonalityEntry
    {
        // ===== 基础标识 =====
        [Editable(Label = "人格ID", ControlType = "Numeric", Min = 10000, Max = 99999, Order = 1)]
        public int id { get; set; }

        [Editable(Label = "外观", ControlType = "Text", Order = 2)]
        public string appearance { get; set; } = "";

        public List<string> unitKeywordList { get; set; } = new();
        public List<string> associationList { get; set; } = new();

        [Editable(Label = "所属罪人", ControlType = "Dropdown",
                  Options = "1-Yi Sang,2-Faust,3-Don Quixote,4-Ryoshu,5-Meursault,6-Hong Lu,7-Heathcliff,8-Ishmael,9-Rodion,10-Sinclair,11-Outis,12-Gregor",
                  Order = 3)]
        public int characterId { get; set; }

        [Editable(Label = "恐慌类型", ControlType = "Numeric", Min = 0, Max = 9999, Order = 4)]
        public int panicType { get; set; } = 9999;

        [Editable(Label = "赛季", ControlType = "Numeric", Min = 0, Order = 5)]
        public int season { get; set; }

        public List<int> defenseSkillIDList { get; set; } = new();

        [Editable(Label = "侵蚀恐慌技能", ControlType = "Numeric", Min = 0, Order = 6)]
        public int panicSkillOnErosion { get; set; }

        public List<string> slotWeightConditionList { get; set; } = new();

        [Editable(Label = "星级", ControlType = "Dropdown",
                  Options = "1,2,3", Order = 7)]
        public int rank { get; set; }

        // ===== 数值属性 =====
        [Editable(Label = "HP", ControlType = "Nested", Order = 10)]
        public HpInfo hp { get; set; } = new();

        [Editable(Label = "防御修正", ControlType = "Numeric", Min = -10, Max = 10, Order = 11)]
        public int defCorrection { get; set; }

        // ===== 速度 =====
        [Editable(Label = "速度", ControlType = "SpeedRange", Order = 20)]
        public object SpeedRangePlaceholder { get; set; } = null!;  // 占位属性，实际数据在 minSpeedList/maxSpeedList

        public List<int> minSpeedList { get; set; } = new();
        public List<int> maxSpeedList { get; set; } = new();

        // ===== 特殊属性 =====
        [Editable(Label = "独有属性", ControlType = "Dropdown",
                  Options = "AZURE,SCARLET,CRIMSON,AMBER,SHAMROCK,INDIGO,VIOLET",
                  Order = 12)]
        public string uniqueAttribute { get; set; } = "";

        // ===== SP =====
        public MentalConditionInfo mentalConditionInfo { get; set; } = new();

        // ===== 混乱阈值 =====
        [Editable(Label = "混乱阈值", ControlType = "Nested", Order = 30)]
        public BreakSection breakSection { get; set; } = new();

        // ===== 抗性 =====
        [Editable(Label = "攻击抗性", ControlType = "Nested", Order = 31)]
        public ResistInfo resistInfo { get; set; } = new();

        // ===== 技能列表 =====
        public List<SkillSlot> attributeList { get; set; } = new();

        // ===== 可选：脚本ID (一般不用) =====
        public string unitScriptID { get; set; } = "";
    }

    // ========== 嵌套类 ==========

    public class HpInfo
    {
        [Editable(Label = "基础HP", ControlType = "Numeric", Min = 0, Order = 1)]
        public int defaultStat { get; set; }

        [Editable(Label = "每级HP增量", ControlType = "Numeric", Min = 0, Order = 2)]
        public double incrementByLevel { get; set; }
    }

    public class MentalConditionInfo
    {
        public List<MentalConditionGroup> add { get; set; } = new();
        public List<MentalConditionGroup> min { get; set; } = new();
    }

    public class MentalConditionGroup
    {
        public int level { get; set; }
        public List<ConditionRef> conditionIDList { get; set; } = new();
    }

    public class ConditionRef
    {
        public string conditionID { get; set; } = "";
    }

    public class BreakSection
    {
        [Editable(Label = "混乱阈值 (%)", ControlType = "List", Order = 1, AllowAddRemove = true)]
        public List<int> sectionList { get; set; } = new();
    }

    public class ResistInfo
    {
        [Editable(Label = "攻击抗性", ControlType = "List", Order = 1, AllowAddRemove = false)]
        public List<ResistEntry> atkResistList { get; set; } = new();
    }

    public class ResistEntry
    {
        [Editable(Label = "伤害类型", ControlType = "Dropdown",
                  Options = "SLASH,PENETRATE,HIT", Order = 1)]
        public string type { get; set; } = "";

        [Editable(Label = "倍率", ControlType = "Numeric", Min = 0, Max = 200, Order = 2)]
        public double value { get; set; }

        // ===== 添加 ToString 方法 =====
        public override string ToString()
        {
            return $"{type}: {value}x";
        }
    }

    public class SkillSlot
    {
        public int skillId { get; set; }
        public int number { get; set; }

        // ===== 添加 ToString 方法 =====
        public override string ToString()
        {
            return $"Skill: {skillId} × {number}";
        }
    }
}