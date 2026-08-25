using LethelModHelper.Core.Models;
using LethelModHelper.Services.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LethelModHelper.Services
{
    public static class EditorGenerator
    {
        /// <summary>
        /// 自动为对象生成编辑界面
        /// </summary>
        public static StackPanel GenerateEditor(object dataObject, int depth = 0)
        {
            var panel = new StackPanel();
            var type = dataObject.GetType();
            var properties = type.GetProperties();

            var editableProps = new List<(PropertyInfo Prop, EditableAttribute Attr)>();
            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<EditableAttribute>();
                if (attr != null)
                {
                    editableProps.Add((prop, attr));
                }
            }

            foreach (var (prop, attr) in editableProps.OrderBy(x => x.Attr.Order))
            {
                // 检查是否是脚本字段
                if (prop.PropertyType == typeof(string) && ScriptFieldCache.HasScript(dataObject, prop.Name))
                {
                    var parsed = ScriptFieldCache.Get(dataObject, prop.Name);
                    panel.Children.Add(CreateScriptDisplay(prop.Name, parsed));
                }
                else
                {
                    var control = CreateControl(dataObject, prop, attr, depth);
                    if (control != null)
                    {
                        panel.Children.Add(control);
                    }
                }
            }

            return panel;
        }

        private static UIElement CreateControl(object dataObject, PropertyInfo prop, EditableAttribute attr, int depth = 0)
        {
            // 所有字段类型都委托给对应的 Editor
            // 这个类现在只做路由

            if (attr.ControlType == "SpeedRange")
            {
                var editor = new SpeedRangeEditor();
                return editor.Create(dataObject, prop, attr, depth);
            }

            if (attr.ControlType == "Numeric")
            {
                var editor = new NumericEditor();
                return editor.Create(dataObject, prop, attr, depth);
            }

            if (attr.ControlType == "Boolean")
            {
                var editor = new BooleanEditor();
                return editor.Create(dataObject, prop, attr, depth);
            }

            if (attr.ControlType == "Dropdown")
            {
                var editor = new DropdownEditor();
                return editor.Create(dataObject, prop, attr, depth);
            }

            if (attr.ControlType == "Text")
            {
                var editor = new TextEditor();
                return editor.Create(dataObject, prop, attr, depth);
            }

            if (prop.PropertyType.IsGenericType &&
                prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var editor = new ListEditor();
                return editor.Create(dataObject, prop, attr, depth);
            }

            if (attr.ControlType == "Nested" ||
                (prop.PropertyType.IsClass && prop.PropertyType != typeof(string) && !prop.PropertyType.IsValueType))
            {
                var editor = new NestedEditor(null!);
                return editor.Create(dataObject, prop, attr, depth);
            }

            // 未知类型，返回空
            return new TextBlock
            {
                Text = $"⚠️ 未知控件类型: {attr.ControlType}",
                Foreground = Brushes.Red,
                Margin = new Thickness(10, 5, 0, 5)
            };
        }

        private static UIElement CreateScriptDisplay(string fieldName, ParsedScript? parsed)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };

            panel.Children.Add(new TextBlock
            {
                Text = $"📜 {fieldName}:",
                FontWeight = FontWeights.Bold,
                FontSize = 12
            });

            if (parsed == null || !parsed.IsValid)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"  {(parsed?.ErrorMessage ?? "解析失败")}",
                    Foreground = Brushes.Red,
                    FontSize = 11,
                    Margin = new Thickness(10, 0, 0, 0)
                });
                return panel;
            }

            if (parsed.Parts.Count == 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "  (空脚本)",
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(10, 0, 0, 0)
                });
                return panel;
            }

            foreach (var part in parsed.Parts)
            {
                var color = GetPartColor(part.Type);
                var displayText = $"  • [{part.Type}] {part.Name}";
                if (part.Arguments.Count > 0)
                {
                    displayText += $"({string.Join(", ", part.Arguments)})";
                }
                panel.Children.Add(new TextBlock
                {
                    Text = displayText,
                    Foreground = color,
                    FontSize = 11,
                    Margin = new Thickness(10, 0, 0, 0)
                });
            }

            return panel;
        }

        private static Brush GetPartColor(string type)
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
    }
}