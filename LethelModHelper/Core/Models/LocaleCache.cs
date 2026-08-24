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
    }
}