using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LethelModHelper.Core.Models;

namespace LethelModHelper.Services.Editors
{
    public class NumericEditor : IFieldEditor
    {
        public bool CanEdit(PropertyInfo property, EditableAttribute attribute)
        {
            return attribute.ControlType == "Numeric";
        }

        public UIElement Create(
            object dataObject,
            PropertyInfo property,
            EditableAttribute attribute,
            int depth)
        {
            return CreateNumericField(dataObject, property, attribute);
        }

        private UIElement CreateNumericField(
            object dataObject,
            PropertyInfo prop,
            EditableAttribute attr)
        {
            var currentValue = prop.GetValue(dataObject);
            var label = string.IsNullOrEmpty(attr.Label) ? prop.Name : attr.Label;

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            panel.Children.Add(CreateLabel($"{label}: "));

            var displayValue = currentValue?.ToString() ?? "0";
            var isDouble = prop.PropertyType == typeof(double);

            var box = new TextBox
            {
                Text = displayValue,
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

            box.TextChanged += (s, e) =>
            {
                if (isDouble)
                {
                    if (double.TryParse(box.Text, out double newValue))
                    {
                        if (newValue >= attr.Min && newValue <= attr.Max)
                        {
                            prop.SetValue(dataObject, newValue);
                            box.Background = Brushes.LightYellow;
                        }
                    }
                    else
                    {
                        box.Background = Brushes.LightPink;
                    }
                }
                else
                {
                    if (int.TryParse(box.Text, out int newValue))
                    {
                        if (newValue >= attr.Min && newValue <= attr.Max)
                        {
                            prop.SetValue(dataObject, newValue);
                            box.Background = Brushes.LightYellow;
                        }
                    }
                    else
                    {
                        box.Background = Brushes.LightPink;
                    }
                }
            };
            panel.Children.Add(box);

            var resetBtn = new Button
            {
                Content = "↩️",
                Width = 28,
                Height = 28,
                Margin = new Thickness(5, 0, 0, 0),
                ToolTip = "重置",
                FontSize = 12,
                Padding = new Thickness(2)
            };
            resetBtn.Click += (s, e) =>
            {
                var originalValue = prop.GetValue(dataObject);
                box.Text = originalValue?.ToString() ?? "0";
                box.Background = Brushes.White;
            };
            panel.Children.Add(resetBtn);

            return panel;
        }

        private static TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            };
        }
    }
}