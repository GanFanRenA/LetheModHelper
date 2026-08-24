using System.Collections.Generic;

namespace LethelModHelper.Core.Models
{
    /// <summary>
    /// Passive 数据
    /// 对应 custom_limbus_data/passive/ 下的 JSON 文件
    /// </summary>
    public class PassiveData
    {
        public List<PassiveEntry> list { get; set; } = new();
    }

    public class PassiveEntry
    {
        public int id { get; set; }
        public List<string> requireIDList { get; set; } = new();
    }
}
