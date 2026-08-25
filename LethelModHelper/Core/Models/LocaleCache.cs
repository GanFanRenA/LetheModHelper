using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LethelModHelper.Core.Models
{
    public static class LocaleCache
    {
        // ===== Buff 本地化 =====
        public static Dictionary<string, BuffLocaleEntry> BuffLocaleMap { get; set; } = new();
        public static string CurrentBuffLocaleFilePath { get; set; } = "";

        // ===== Keyword 本地化 =====
        public static Dictionary<string, KeywordLocaleEntry> KeywordLocaleMap { get; set; } = new();
        public static string CurrentKeywordLocaleFilePath { get; set; } = "";

        // ===== Skill 本地化 (新增) =====
        public static Dictionary<string, SkillLocaleEntry> SkillLocaleMap { get; set; } = new();
        public static string CurrentSkillLocaleFilePath { get; set; } = "";

        // ===== Passive 本地化 (新增) =====
        public static Dictionary<string, PassiveLocaleEntry> PassiveLocaleMap { get; set; } = new();
        public static string CurrentPassiveLocaleFilePath { get; set; } = "";

        // ===== Buff 方法 =====
        public static BuffLocaleEntry? GetBuffLocale(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            BuffLocaleMap.TryGetValue(id, out var entry);
            return entry;
        }

        public static void SaveBuffLocaleData()
        {
            SaveLocaleData(CurrentBuffLocaleFilePath, BuffLocaleMap);
        }

        // ===== Keyword 方法 =====
        public static KeywordLocaleEntry? GetKeywordLocale(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            KeywordLocaleMap.TryGetValue(id, out var entry);
            return entry;
        }

        public static void SaveKeywordLocaleData()
        {
            SaveLocaleData(CurrentKeywordLocaleFilePath, KeywordLocaleMap);
        }

        // ===== 通用保存方法 =====
        public static void SaveLocaleData<T>(string filePath, Dictionary<string, T> data)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"❌ 没有文件路径，无法保存");
                return;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, json);
                System.Diagnostics.Debug.WriteLine($"✅ 已保存: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 保存失败: {ex.Message}");
            }
        }

        

        // ===== Skill 方法 (新增) =====
        public static SkillLocaleEntry? GetSkillLocale(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            SkillLocaleMap.TryGetValue(id, out var entry);
            return entry;
        }

        /// <summary>
        /// 获取技能本地化的第一个等级（通常只有 level 1）
        /// </summary>
        public static SkillLocaleLevel? GetSkillLocaleLevel(string id)
        {
            var entry = GetSkillLocale(id);
            if (entry?.levelList == null || entry.levelList.Count == 0) return null;
            return entry.levelList[0];  // 直接取第一个
        }

        public static void SaveSkillLocaleData()
        {
            SaveLocaleData(CurrentSkillLocaleFilePath, SkillLocaleMap);
        }

        // ===== Passive 方法 (新增) =====
        public static PassiveLocaleEntry? GetPassiveLocale(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            PassiveLocaleMap.TryGetValue(id, out var entry);
            return entry;
        }

        public static void SavePassiveLocaleData()
        {
            SaveLocaleData(CurrentPassiveLocaleFilePath, PassiveLocaleMap);
        }
    }
}