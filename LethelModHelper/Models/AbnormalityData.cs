using System.Collections.Generic;

namespace LethelModHelper.Models
{
    public class AbnormalityData
    {
        public List<AbnormalityEntry> list { get; set; } = new();
    }

    public class AbnormalityEntry
    {
        [Editable(Label = "ID", ControlType = "Numeric", Order = 1)]
        public int id { get; set; }

        [Editable(Label = "外观", ControlType = "Text", Order = 2)]
        public string appearance { get; set; } = "";

        [Editable(Label = "阵营标签", ControlType = "List", Order = 3, AllowAddRemove = true)]
        public List<string> associationList { get; set; } = new();

        /*[Editable(Label = "阶段ID列表", ControlType = "List", Order = 4, AllowAddRemove = true)]
        public List<int> phaseIDList { get; set; } = new();*/

        [Editable(Label = "危险等级", ControlType = "Dropdown",
                  Options = "ZAYIN,TETH,HE,WAW,ALEPH,UNKNOWN", Order = 5)]
        public string classType { get; set; } = "";

        [Editable(Label = "HP", ControlType = "Nested", Order = 6)]
        public HpInfo hp { get; set; } = new();

        [Editable(Label = "是否有理智", ControlType = "Boolean", Order = 7)]
        public bool hasMp { get; set; }

        [Editable(Label = "恐慌类型", ControlType = "Numeric", Order = 8)]
        public int panicType { get; set; }

        [Editable(Label = "低士气类型", ControlType = "Numeric", Order = 9)]
        public int lowMorale { get; set; }
        [Editable(Label = "恐慌", ControlType = "Numeric", Order = 10)]
        public int panic { get; set; }

        [Editable(Label = "起始行动槽", ControlType = "Numeric", Order = 11)]
        public int startActionSlotNum { get; set; }

        [Editable(Label = "最大行动槽", ControlType = "Numeric", Order = 12)]
        public int maxActionSlotNum { get; set; }

        [Editable(Label = "模式ID", ControlType = "Dropdown",
                  Options = "AutoPick,PickByPattern_Abnormality_UptoActionSlotCnt", Order = 13)]
        public string patternID { get; set; } = "";

        [Editable(Label = "部位列表", ControlType = "List", Order = 14, AllowAddRemove = true)]
        public List<int> abnormalityPartList { get; set; } = new();

        [Editable(Label = "技能列表(显示)", ControlType = "List", Order = 15, AllowAddRemove = true)]
        public List<SkillSlot> attributeList { get; set; } = new();

        [Editable(Label = "技能行为列表", ControlType = "List", Order = 16, AllowAddRemove = true)]
        public List<Pattern> patternList { get; set; } = new();

        [Editable(Label = "被动", ControlType = "Nested", Order = 17)]
        public PassiveSet passiveSet { get; set; } = new();
    }

    /// <summary>
    /// 模式（包含多个槽位）
    /// </summary>
    public class Pattern
    {
        [Editable(Label = "槽位列表", ControlType = "List", Order = 1, AllowAddRemove = true)]
        public List<Slot> slotList { get; set; } = new();

        public override string ToString()
        {
            int slotCount = slotList?.Count ?? 0;
            return $"Pattern (槽位数: {slotCount})";
        }
    }

    /// <summary>
    /// 槽位（包含多个技能父级）
    /// </summary>
    public class Slot
    {
        public List<SkillParent> skillParentList { get; set; } = new();

        public override string ToString()
        {
            int parentCount = skillParentList?.Count ?? 0;
            return $"Slot (技能父级数: {parentCount})";
        }
    }

    /// <summary>
    /// 技能父级（包含多个技能子级）
    /// </summary>
    public class SkillParent
    {
        public List<SkillChild> skillChildList { get; set; } = new();

        [Editable(Label = "权重", ControlType = "Numeric", Order = 2)]
        public int chance { get; set; } = 1;

        public override string ToString()
        {
            int childCount = skillChildList?.Count ?? 0;
            return $"SkillParent (子级数: {childCount}, 权重: {chance})";
        }
    }

    /// <summary>
    /// 技能子级（实际技能）
    /// </summary>
    public class SkillChild
    {
        [Editable(Label = "技能ID", ControlType = "Numeric", Order = 1)]
        public int skillID { get; set; }

        public int chance { get; set; } = 1;

        public override string ToString()
        {
            return $"技能ID: {skillID}, 权重: {chance}";
        }
    }

    /// <summary>
    /// 被动集
    /// </summary>
    public class PassiveSet
    {
        // 解锁被动列表 - 不显示在UI中，由被动列表实时映射
        public List<int> unlockPassiveIdListForUI
        {
            get => new List<int>(_passiveIdList);
            set
            {
                // 兼容部分数据只提供 unlock 列表的情况
                if ((_passiveIdList == null || _passiveIdList.Count == 0) && value != null)
                {
                    _passiveIdList = new List<int>(value);
                }
            }
        }

        // 被动列表 - 只显示这个
        private List<int> _passiveIdList = new();

        [Editable(Label = "被动列表", ControlType = "List", Order = 2, AllowAddRemove = true)]
        public List<int> passiveIdList
        {
            get => _passiveIdList;
            set => _passiveIdList = value ?? new List<int>();
        }

        public override string ToString()
        {
            return $"被动: {_passiveIdList.Count} 个";
        }
    }
}