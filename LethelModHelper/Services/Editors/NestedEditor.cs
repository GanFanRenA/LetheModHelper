using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LethelModHelper.Core.Models;

namespace LethelModHelper.Services.Editors
{
    public class NestedEditor : IFieldEditor
    {
        private readonly FieldEditorFactory _factory;

        public NestedEditor(FieldEditorFactory factory)
        {
            _factory = factory;
        }

        public bool CanEdit(PropertyInfo property, EditableAttribute attribute)
        {
            // 1. 显式标记为 Nested
            if (attribute.ControlType == "Nested")
                return true;

            // 2. 是类且不是 string 且不是值类型（即引用类型）
            if (property.PropertyType.IsClass &&
                property.PropertyType != typeof(string) &&
                !property.PropertyType.IsValueType)
                return true;

            return false;
        }

        public UIElement Create(
            object dataObject,
            PropertyInfo property,
            EditableAttribute attribute,
            int depth)
        {
            return CreateNestedField(dataObject, property, attribute, depth);
        }

        private UIElement CreateNestedField(
            object dataObject,
            PropertyInfo prop,
            EditableAttribute attr,
            int depth)
        {
            var currentValue = prop.GetValue(dataObject);
            var label = string.IsNullOrEmpty(attr.Label) ? prop.Name : attr.Label;

            if (currentValue == null)
            {
                // 如果嵌套对象为 null，尝试创建实例
                try
                {
                    currentValue = Activator.CreateInstance(prop.PropertyType);
                    prop.SetValue(dataObject, currentValue);
                }
                catch
                {
                    // 如果无法创建，显示错误
                    var errorBorder = new Border
                    {
                        BorderBrush = Brushes.Red,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Margin = new Thickness(depth * 15, 5, 0, 5),
                        Padding = new Thickness(8)
                    };
                    errorBorder.Child = new TextBlock
                    {
                        Text = $"⚠️ 无法创建嵌套对象: {label}",
                        Foreground = Brushes.Red,
                        FontSize = 11
                    };
                    return errorBorder;
                }
            }

            var border = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(depth * 15, 5, 0, 5),
                Padding = new Thickness(8)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"📁 {label}:",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // 使用 EditorGenerator 递归生成嵌套对象的编辑界面
            var nestedEditor = EditorGenerator.GenerateEditor(currentValue, depth + 1);
            stack.Children.Add(nestedEditor);

            border.Child = stack;
            return border;
        }
    }
}