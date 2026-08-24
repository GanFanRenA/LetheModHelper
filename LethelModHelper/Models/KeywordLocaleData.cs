namespace LethelModHelper.Models
{
    /// <summary>
    /// Keyword 本地化条目
    /// 对应 keywordList.json 中的单个条目
    /// </summary>
    public class KeywordLocaleEntry
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string desc { get; set; } = "";
        public string flavor { get; set; } = "";
    }
}