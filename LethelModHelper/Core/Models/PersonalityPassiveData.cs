using System.Collections.Generic;

namespace LethelModHelper.Core.Models
{
    /// <summary>
    /// Personality Passive 数据
    /// 对应 custom_limbus_data/personality_passive/ 下的 JSON 文件
    /// </summary>
    public class PersonalityPassiveData
    {
        public List<PersonalityPassiveEntry> list { get; set; } = new();
    }

    public class PersonalityPassiveEntry
    {
        public int personalityID { get; set; }
        public List<PassiveGroup> battlePassiveList { get; set; } = new();
        public List<PassiveGroup> supporterPassiveList { get; set; } = new();
    }

    public class PassiveGroup
    {
        public int level { get; set; }
        public List<int> passiveIDList { get; set; } = new();
    }
}