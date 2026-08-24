using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LethelModHelper.Core.Models;
using LethelModHelper.Handlers;

namespace LethelModHelper.Services
{
    public class ModScanner
    {
        private readonly List<IFileHandler> _handlers = new();

        // 需要跳过的文件夹（只作为容器，不匹配任何 Handler）
        private readonly List<string> _skipFolders = new()
        {
            "custom_limbus_data"
        };

        public Dictionary<string, FileParseResult> ParsedFiles { get; } = new();
        public string CurrentModPath { get; private set; } = "";

        public Dictionary<string, BuffLocaleEntry> BuffLocaleMap { get; private set; } = new();
        // ===== Keyword 本地化 =====
        public Dictionary<string, KeywordLocaleEntry> KeywordLocaleMap { get; private set; } = new();  // ← 添加这一行

        public event EventHandler<string>? FileParsed;
        public event EventHandler<string>? FileParseFailed;

        public ModScanner()
        {
            RegisterHandler(new PersonalityHandler());
            RegisterHandler(new PersonalityPassiveHandler());
            RegisterHandler(new PassiveHandler());
            RegisterHandler(new BuffHandler());
            RegisterHandler(new AbnormalityHandler());
        }

        public void RegisterHandler(IFileHandler handler)
        {
            if (!_handlers.Any(h => h.HandlerName == handler.HandlerName))
            {
                _handlers.Add(handler);
            }
        }

        public void OpenMod(string modPath)
        {
            if (!Directory.Exists(modPath))
            {
                throw new DirectoryNotFoundException($"找不到文件夹: {modPath}");
            }

            CurrentModPath = modPath;
            ParsedFiles.Clear();

            LoadBuffLocaleData(modPath);
            LoadKeywordLocaleData(modPath);

            ScanFolder(modPath);
        }


        private void ScanFolder(string folderPath)
        {
            var folderName = Path.GetFileName(folderPath);
            System.Diagnostics.Debug.WriteLine($"📁 扫描文件夹: '{folderName}'");

            System.Diagnostics.Debug.WriteLine($"扫描文件夹: {folderName}");

            // ===== 检查是否要跳过这个文件夹 =====
            if (_skipFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
            {
                // 直接进入子文件夹，不处理当前文件夹
                foreach (var subFolder in Directory.GetDirectories(folderPath))
                {
                    ScanFolder(subFolder);
                }
                return;
            }
            // =====================================

            System.Diagnostics.Debug.WriteLine($"  已注册的处理器: {string.Join(", ", _handlers.Select(h => h.HandlerName))}");

            // 找能处理这个文件夹的处理器
            var handler = _handlers.FirstOrDefault(h => h.CanHandle(folderName));

            if (handler != null)
            {
                // ===== 获取所有文件 =====
                var files = Directory.GetFiles(folderPath);

                foreach (var file in files)
                {
                    try
                    {
                        var result = handler.Parse(file);
                        ParsedFiles[file] = result;

                        if (result.Success)
                        {
                            FileParsed?.Invoke(this, file);
                        }
                        else
                        {
                            FileParseFailed?.Invoke(this, $"{Path.GetFileName(file)}: {result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        FileParseFailed?.Invoke(this, $"{Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }

            // 递归扫描子文件夹
            foreach (var subFolder in Directory.GetDirectories(folderPath))
            {
                ScanFolder(subFolder);
            }
        }

        private bool IsSupportedFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension == ".json" || extension == ".txt";
        }

        public List<(string FilePath, object Data)> GetParsedData()
        {
            var result = new List<(string, object)>();
            foreach (var kvp in ParsedFiles)
            {
                if (kvp.Value.Success && kvp.Value.Data != null)
                {
                    result.Add((kvp.Key, kvp.Value.Data));
                }
            }
            return result;
        }

        /// <summary>
        /// 加载 Buff 本地化数据 (bufList.json)
        /// </summary>
        private void LoadBuffLocaleData(string modPath)
        {
            BuffLocaleMap.Clear();

            // ===== 固定路径：custom_limbus_locale/EN/bufList/ =====
            var buffFolder = Path.Combine(modPath, "custom_limbus_locale", "EN", "bufList");
            // ====================================================

            if (!Directory.Exists(buffFolder))
            {
                System.Diagnostics.Debug.WriteLine($"未找到 bufList 文件夹: {buffFolder}");
                return;
            }

            var jsonFiles = Directory.GetFiles(buffFolder, "*.json");

            if (jsonFiles.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"bufList 文件夹下没有 JSON 文件");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"✅ 找到 {jsonFiles.Length} 个 Buff 本地化文件");

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    var jsonContent = File.ReadAllText(filePath);

                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };

                    var localeData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, BuffLocaleEntry>>(jsonContent, options);

                    if (localeData != null)
                    {
                        foreach (var kvp in localeData)
                        {
                            if (string.IsNullOrEmpty(kvp.Value.id))
                            {
                                kvp.Value.id = kvp.Key;
                            }
                            BuffLocaleMap[kvp.Key] = kvp.Value;
                            System.Diagnostics.Debug.WriteLine($"  加载 Buff: {kvp.Key} -> {kvp.Value.name}");
                        }

                        LocaleCache.CurrentBuffLocaleFilePath = filePath;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ 加载 Buff 文件失败: {Path.GetFileName(filePath)} - {ex.Message}");
                }
            }

            LocaleCache.BuffLocaleMap = BuffLocaleMap;
            System.Diagnostics.Debug.WriteLine($"✅ 总共加载了 {BuffLocaleMap.Count} 个 Buff 本地化条目");
        }

        /// <summary>
        /// 加载 Keyword 本地化数据 (custom_limbus_locale/EN/keywordList/*.json)
        /// </summary>
        private void LoadKeywordLocaleData(string modPath)
        {
            KeywordLocaleMap.Clear();

            // ===== 固定路径：custom_limbus_locale/EN/keywordList/ =====
            var keywordFolder = Path.Combine(modPath, "custom_limbus_locale", "EN", "keywordList");
            // ========================================================

            if (!Directory.Exists(keywordFolder))
            {
                System.Diagnostics.Debug.WriteLine($"未找到 keywordList 文件夹: {keywordFolder}");
                return;
            }

            // ===== 获取文件夹下所有 JSON 文件 =====
            var jsonFiles = Directory.GetFiles(keywordFolder, "*.json");
            // ====================================

            if (jsonFiles.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"keywordList 文件夹下没有 JSON 文件");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"✅ 找到 {jsonFiles.Length} 个 Keyword 本地化文件");

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    var jsonContent = File.ReadAllText(filePath);

                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };

                    var localeData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, KeywordLocaleEntry>>(jsonContent, options);

                    if (localeData != null)
                    {
                        foreach (var kvp in localeData)
                        {
                            if (string.IsNullOrEmpty(kvp.Value.id))
                            {
                                kvp.Value.id = kvp.Key;
                            }
                            KeywordLocaleMap[kvp.Key] = kvp.Value;
                            System.Diagnostics.Debug.WriteLine($"  加载 Keyword: {kvp.Key} -> {kvp.Value.name}");
                        }

                        // ===== 记录文件路径（用于保存） =====
                        LocaleCache.CurrentKeywordLocaleFilePath = filePath;
                        // ====================================
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ 加载 Keyword 文件失败: {Path.GetFileName(filePath)} - {ex.Message}");
                }
            }

            LocaleCache.KeywordLocaleMap = KeywordLocaleMap;
            System.Diagnostics.Debug.WriteLine($"✅ 总共加载了 {KeywordLocaleMap.Count} 个 Keyword 本地化条目");
        }
    }
}