using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LethelModHelper.Models;

namespace LethelModHelper.Services
{
    public class ScriptParser
    {
        public ParsedScript Parse(string script)
        {
            var result = new ParsedScript { RawScript = script };

            if (string.IsNullOrWhiteSpace(script))
            {
                result.IsValid = false;
                result.ErrorMessage = "脚本为空";
                return result;
            }

            try
            {
                var cleanScript = script;
                if (cleanScript.StartsWith("Modular/"))
                    cleanScript = cleanScript.Substring(8);

                var parts = cleanScript.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    var parsedPart = ParsePart(part);
                    if (parsedPart != null)
                        result.Parts.Add(parsedPart);
                }

                if (result.Parts.Count == 0)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "无法解析脚本结构";
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"解析错误: {ex.Message}";
            }

            return result;
        }

        private ScriptPart? ParsePart(string part)
        {
            var match = Regex.Match(part, @"^(\w+):(.+)$");
            if (!match.Success)
            {
                return new ScriptPart
                {
                    Type = "FUNCTION",
                    Name = part,
                    RawText = part
                };
            }

            var type = match.Groups[1].Value;
            var value = match.Groups[2].Value;

            var result = new ScriptPart
            {
                Type = type,
                RawText = part
            };

            var paramMatch = Regex.Match(value, @"^(\w+)\(([^)]*)\)$");
            if (paramMatch.Success)
            {
                result.Name = paramMatch.Groups[1].Value;
                var args = paramMatch.Groups[2].Value;
                if (!string.IsNullOrEmpty(args))
                {
                    result.Arguments.AddRange(args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            else
            {
                result.Name = value;
            }

            return result;
        }
    }
}