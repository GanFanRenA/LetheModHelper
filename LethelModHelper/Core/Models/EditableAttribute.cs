using System;

namespace LethelModHelper.Core.Models
{
    /// <summary>
    /// 标记字段为可编辑
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EditableAttribute : Attribute
    {
        public string Label { get; set; } = "";
        public string ControlType { get; set; } = "Numeric";  // Numeric, Boolean, Dropdown, Text
        public int Min { get; set; } = int.MinValue;
        public int Max { get; set; } = int.MaxValue;
        public string Options { get; set; } = "";  // 逗号分隔的选项
        public int Order { get; set; } = 999;      // 显示顺序


        public bool AllowAddRemove { get; set; } = false;
    }
}