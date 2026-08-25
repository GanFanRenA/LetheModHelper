using LethelModHelper.Core.Models;
using System.Collections.Generic;

namespace LethelModHelper.Core.Models
{
    /// <summary>
    /// Buff 数据（增益/减益）
    /// 对应 custom_limbus_data/buff/ 下的 JSON 文件
    /// </summary>
    public class BuffData
    {
        public List<BuffEntry> list { get; set; } = new();
    }

    /// <summary>
    /// 单个 Buff 条目
    /// </summary>
    public class BuffEntry
    {
        private string _id = "";
        private string _iconId = "";

        [Editable(Label = "ID", ControlType = "Text", Order = 1)]
        public string id
        {
            get => _id;
            set
            {
                var oldId = _id;
                _id = value;
                // 只有在 iconId 为空或与旧 ID 相同时才同步
                if (string.IsNullOrEmpty(_iconId) || _iconId == oldId)
                {
                    _iconId = value;
                }
            }
        }

        public string iconId
        {
            get => _iconId;
            set => _iconId = value;
        }

        [Editable(Label = "类型", ControlType = "Dropdown",
                  Options = "Negative,Positive,Neutral", Order = 3)]
        public string buffType { get; set; } = "";

        [Editable(Label = "最大强度", ControlType = "Numeric",
                  Min = 0, Max = 32767, Order = 4)]
        public int maxStack { get; set; }

        [Editable(Label = "最大层数", ControlType = "Numeric",
              Min = 0, Max = 32767, Order = 5)]
        public int maxTurn { get; set; }

        [Editable(Label = "可驱散", ControlType = "Boolean", Order = 6)]
        public bool canBeDespelled { get; set; }

        [Editable(Label = "归零销毁", ControlType = "Boolean", Order = 7)]
        public bool destroyableOnZero { get; set; }

        public List<BuffAbility> list { get; set; } = new();
    }

    /// <summary>
    /// Buff 的能力/技能
    /// </summary>
    public class BuffAbility
    {
        public string ability { get; set; } = "";
        public int value { get; set; }
        public Dictionary<string, object> buffData { get; set; } = new();
    }
}