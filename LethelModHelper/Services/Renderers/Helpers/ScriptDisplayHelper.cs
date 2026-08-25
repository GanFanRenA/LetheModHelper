using LethelModHelper.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LethelModHelper.Services.Renderers.Helpers
{
    internal class ScriptDisplayHelper
    {
        public static void DisplayScriptFields(object data, StackPanel container, int indent = 0)
        {
            if (data == null) return;

            var type = data.GetType();
            var properties = type.GetProperties();

            foreach (var prop in properties)
            {
                var propValue = prop.GetValue(data);
                if (propValue == null) continue;

                var margin = new Thickness(indent * 15, 2, 0, 2);

                if (prop.PropertyType == typeof(string) && ScriptFieldCache.HasScript(data, prop.Name))
                {
                    var parsed = ScriptFieldCache.Get(data, prop.Name);
                    container.Children.Add(RendererUIHelper.CreateSelectableText($"📜 {prop.Name}:", true, null, 12, margin.Left));
                    DisplayParsedScript(parsed, container, margin.Left + 10);
                }
                else if (prop.PropertyType == typeof(List<string>))
                {
                    DisplayStringList(propValue, prop, data, container, margin);
                }
                else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                {
                    DisplayNestedObject(propValue, prop.PropertyType, container, margin, indent);
                }
            }
        }

        public static void DisplayParsedScript(ParsedScript? parsed, StackPanel container, double marginLeft)
        {
            if (parsed == null)
            {
                container.Children.Add(RendererUIHelper.CreateSelectableText(
                    "  (解析失败)", false, Brushes.Red, 11, marginLeft));
                return;
            }

            if (!parsed.IsValid)
            {
                container.Children.Add(RendererUIHelper.CreateSelectableText(
                    $"  ⚠️ {parsed.ErrorMessage}", false, Brushes.Red, 11, marginLeft));
                return;
            }

            if (parsed.Parts.Count == 0)
            {
                container.Children.Add(RendererUIHelper.CreateSelectableText(
                    "  (空脚本)", false, Brushes.Gray, 11, marginLeft));
                return;
            }

            foreach (var part in parsed.Parts)
            {
                var color = GetPartColor(part.Type);
                container.Children.Add(RendererUIHelper.CreateSelectableText(
                    $"  • {GetPartDisplayText(part)}", false, color, 11, marginLeft));
            }
        }

        public static Brush GetPartColor(string type)
        {
            return type.ToUpper() switch
            {
                "TIMING" => Brushes.Blue,
                "LUA" => Brushes.Green,
                "LUAMAIN" => Brushes.Purple,
                "LOOP" => Brushes.Orange,
                "VALUE" => Brushes.Brown,
                "IF" => Brushes.Red,
                "FUNCTION" => Brushes.DarkCyan,
                _ => Brushes.Black
            };
        }

        public static string GetPartDisplayText(ScriptPart part)
        {
            var display = $"[{part.Type}] {part.Name}";
            if (part.Arguments.Count > 0)
            {
                display += $"({string.Join(", ", part.Arguments)})";
            }
            return display;
        }

        public static void DisplayStringList(object propValue, System.Reflection.PropertyInfo prop,
            object data, StackPanel container, Thickness margin)
        {
            var key = $"{data.GetHashCode()}_{prop.Name}_LIST";
            if (ScriptFieldCache.HasList(key))
            {
                var parsedList = ScriptFieldCache.GetList(key);
                if (parsedList != null && parsedList.Count > 0)
                {
                    container.Children.Add(RendererUIHelper.CreateSelectableText(
                        $"📜 {prop.Name} ({parsedList.Count} 个脚本):", true, null, 12, margin.Left));

                    for (int i = 0; i < parsedList.Count; i++)
                    {
                        container.Children.Add(RendererUIHelper.CreateSelectableText($"  [{i}]", true, null, 11, margin.Left + 10));
                        DisplayParsedScript(parsedList[i], container, margin.Left + 25);
                    }
                }
            }
            else if (propValue is System.Collections.IEnumerable list)
            {
                DisplayEnumerable(list, container, margin, 1);
            }
        }

        public static void DisplayNestedObject(object propValue, Type propType,
            StackPanel container, Thickness margin, int indent)
        {
            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (propValue is System.Collections.IEnumerable list)
                {
                    DisplayEnumerable(list, container, margin, indent + 1);
                }
            }
            else
            {
                DisplayScriptFields(propValue, container, indent);
            }
        }

        public static void DisplayEnumerable(System.Collections.IEnumerable list,
            StackPanel container, Thickness margin, int indent)
        {
            int index = 0;
            foreach (var item in list)
            {
                container.Children.Add(RendererUIHelper.CreateSelectableText($"  [{index++}]", true, null, 11, margin.Left));
                DisplayScriptFields(item, container, indent);
            }
        }
    }
}
