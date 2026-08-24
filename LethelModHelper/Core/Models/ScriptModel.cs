using System.Collections.Generic;

namespace LethelModHelper.Core.Models
{
    /// <summary>
    /// 脚本解析结果
    /// </summary>
    public class ParsedScript
    {
        public string RawScript { get; set; } = "";
        public List<ScriptPart> Parts { get; set; } = new();
        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; } = "";
    }

    /// <summary>
    /// 脚本的每个部分
    /// </summary>
    public class ScriptPart
    {
        public string Type { get; set; } = "";    // TIMING, LUA, LUAMAIN, LOOP, VALUE, IF, FUNCTION
        public string Name { get; set; } = "";    // 具体名称
        public List<string> Arguments { get; set; } = new();
        public string RawText { get; set; } = "";
    }
}