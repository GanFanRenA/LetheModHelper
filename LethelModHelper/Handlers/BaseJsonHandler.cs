using LethelModHelper.Core.Models;
using LethelModHelper.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
namespace LethelModHelper.Handlers
{
    /// <summary>
    /// 泛型 JSON 处理器基类
    /// 子类只需指定：文件夹名 + 数据类型
    /// </summary>
    /// <typeparam name="T">数据模型类型</typeparam>
    public abstract class BaseJsonHandler<T> : IFileHandler where T : class, new()
    {
        /// <summary>
        /// 子类必须指定要处理的文件夹名
        /// </summary>
        public abstract string TargetFolderName { get; }

        /// <summary>
        /// 处理器名称（可重写）
        /// </summary>
        public virtual string HandlerName => $"{typeof(T).Name} 处理器";

        public virtual bool CanHandle(string folderName)
        {
            return string.Equals(folderName, TargetFolderName, StringComparison.OrdinalIgnoreCase);
        }

        public FileParseResult Parse(string filePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"  📄 开始解析: {Path.GetFileName(filePath)}");

                var jsonContent = File.ReadAllText(filePath);
                System.Diagnostics.Debug.WriteLine($"  JSON 内容长度: {jsonContent.Length}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var data = JsonSerializer.Deserialize<T>(jsonContent, options);
                System.Diagnostics.Debug.WriteLine($"  反序列化完成: {(data != null ? "成功" : "失败")}");

                if (data == null)
                {
                    return new FileParseResult
                    {
                        Success = false,
                        ErrorMessage = "反序列化失败，数据为空"
                    };
                }

                // ===== ✅ 添加：清理 Pattern 中的空结构 =====
                if (data is AbnormalityData abnormalityData)
                {
                    System.Diagnostics.Debug.WriteLine("  🧹 清理 Pattern 空结构...");
                    foreach (var entry in abnormalityData.list)
                    {
                        if (entry.patternList != null)
                        {
                            CleanPatternList(entry.patternList);
                        }
                    }
                }
                // ==========================================

                var warnings = Validate(data);

                // 自动检测并解析脚本字段
                ProcessScriptFields(data);

                System.Diagnostics.Debug.WriteLine($"  ✅ 解析完成，数据: {data.GetType().Name}");

                return new FileParseResult
                {
                    Success = true,
                    Data = data,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"  ❌ 解析异常: {ex.Message}");
                return new FileParseResult
                {
                    Success = false,
                    ErrorMessage = $"解析失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 子类可重写此方法进行自定义验证
        /// </summary>
        protected virtual List<string> Validate(T data)
        {
            return new List<string>();
        }

        protected virtual void ProcessScriptFields(object obj)
        {
            if (obj == null) return;
            ProcessScriptFieldsByReflection(obj);
        }

        private void ProcessScriptFieldsByReflection(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();
            var properties = type.GetProperties();

            foreach (var prop in properties)
            {
                try  // ← 添加 try-catch
                {
                    var propValue = prop.GetValue(obj);
                    if (propValue == null) continue;

                    // ===== 情况1：字符串属性 =====
                    if (prop.PropertyType == typeof(string))
                    {
                        var stringValue = propValue.ToString();
                        if (!string.IsNullOrEmpty(stringValue) && IsScriptString(stringValue))
                        {
                            var parser = new ScriptParser();
                            var parsed = parser.Parse(stringValue);
                            ScriptFieldCache.Store(obj, prop.Name, parsed);
                        }
                    }

                    // ===== 情况2：字符串列表 =====
                    else if (prop.PropertyType == typeof(List<string>))
                    {
                        var list = propValue as List<string>;
                        if (list != null)
                        {
                            var scriptItems = new List<string>();
                            foreach (var item in list)
                            {
                                if (!string.IsNullOrEmpty(item) && IsScriptString(item))
                                {
                                    scriptItems.Add(item);
                                }
                            }
                            if (scriptItems.Count > 0)
                            {
                                var parser = new ScriptParser();
                                var parsedList = new List<ParsedScript>();
                                foreach (var item in scriptItems)
                                {
                                    parsedList.Add(parser.Parse(item));
                                }
                                var key = $"{obj.GetHashCode()}_{prop.Name}_LIST";
                                ScriptFieldCache.StoreList(key, parsedList);
                            }
                        }
                    }

                    // ===== 情况3：嵌套对象 =====
                    else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    {
                        if (prop.PropertyType.IsGenericType &&
                            prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var list = propValue as System.Collections.IEnumerable;
                            if (list != null)
                            {
                                foreach (var item in list)
                                {
                                    ProcessScriptFieldsByReflection(item);
                                }
                            }
                        }
                        else
                        {
                            ProcessScriptFieldsByReflection(propValue);
                        }
                    }
                }
                catch (Exception ex)  // ← 捕获异常
                {
                    System.Diagnostics.Debug.WriteLine($"  ⚠️ ProcessScriptFields 异常: {ex.Message}");
                }
            }
        }

        private bool IsScriptString(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Contains("Modular/") ||
                   text.Contains("/TIMING:") ||
                   text.Contains("/LUA:") ||
                   text.Contains("/LUAMAIN:") ||
                   text.Contains("/IF(") ||
                   text.Contains("/VALUE_") ||
                   text.Contains("/LOOP:");
        }

        /// <summary>
        /// 清理 Pattern 中的空结构
        /// </summary>
        private void CleanPatternList(List<Pattern> patternList)
        {
            if (patternList == null) return;

            foreach (var pattern in patternList)
            {
                if (pattern?.slotList == null) continue;

                var cleanedSlots = new List<Slot>();
                foreach (var slot in pattern.slotList)
                {
                    if (slot?.skillParentList == null) continue;

                    var cleanedParents = new List<SkillParent>();
                    foreach (var parent in slot.skillParentList)
                    {
                        if (parent?.skillChildList == null) continue;

                        // 只保留有技能的 SkillParent
                        if (parent.skillChildList.Count > 0)
                        {
                            cleanedParents.Add(parent);
                        }
                    }
                    slot.skillParentList = cleanedParents;

                    // 只保留有技能的 Slot
                    if (slot.skillParentList.Count > 0)
                    {
                        cleanedSlots.Add(slot);
                    }
                }
                pattern.slotList = cleanedSlots;
            }
        }
    }


}