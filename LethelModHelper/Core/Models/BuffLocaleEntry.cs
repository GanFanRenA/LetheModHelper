namespace LethelModHelper.Core.Models
{
    /// <summary>
    /// Buff 本地化条目
    /// 对应 bufList.json 中的单个条目
    /// </summary>
    public class BuffLocaleEntry
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string desc { get; set; } = "";
        public string summary { get; set; } = "";
        public string flavor { get; set; } = "";
    }
}