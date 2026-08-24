using LethelModHelper.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LethelModHelper.Services
{
    public class ModDataService
    {
        private readonly FileService _fileService;
        private readonly LocaleService _localeService;

        // 默认序列化选项（内部管理）
        private static readonly JsonSerializerOptions DefaultSaveOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly JsonSerializerOptions DefaultLocaleReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly JsonSerializerOptions DefaultLocaleWriteOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public ModDataService(FileService fileService)
        {
            _fileService = fileService;
            _localeService = new LocaleService(fileService);
        }

        /// <summary>
        /// 加载 JSON 文件并反序列化为指定类型
        /// </summary>
        public T Load<T>(string filePath)
        {
            if (!_fileService.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            var json = _localeService.LoadLocale(filePath);
            return JsonSerializer.Deserialize<T>(json)
                   ?? throw new InvalidOperationException($"反序列化失败: {filePath}");
        }

        /// <summary>
        /// 保存数据到 JSON 文件（使用默认选项）
        /// </summary>
        public void Save<T>(string filePath, T data)
        {
            var json = JsonSerializer.Serialize(data, DefaultSaveOptions);
            _localeService.SaveLocale(filePath, json);
        }

        /// <summary>
        /// 加载本地化字典数据
        /// </summary>
        public Dictionary<string, T> LoadLocaleDictionary<T>(string filePath)
        {
            if (!_fileService.Exists(filePath))
            {
                return new Dictionary<string, T>();
            }

            var json = _localeService.LoadLocale(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, T>>(json, DefaultLocaleReadOptions)
                   ?? new Dictionary<string, T>();
        }

        /// <summary>
        /// 保存本地化字典数据
        /// </summary>
        public void SaveLocaleDictionary<T>(string filePath, Dictionary<string, T> data)
        {
            var json = JsonSerializer.Serialize(data, DefaultLocaleWriteOptions);
            _localeService.SaveLocale(filePath, json);
        }
    }
}