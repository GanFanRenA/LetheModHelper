using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LethelModHelper.Core.Models;

namespace LethelModHelper.Services.Editors
{
    public class DropdownEditor : IFieldEditor
    {
        public bool CanEdit(PropertyInfo property, EditableAttribute attribute)
        {
            return attribute.ControlType == "Dropdown";
        }

        public UIElement Create(
            object dataObject,
            PropertyInfo property,
            EditableAttribute attribute,
            int depth)
        {
            return CreateDropdownField(dataObject, property, attribute);
        }

        private UIElement CreateDropdownField(
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

            var comboBox = new ComboBox
            {
                Width = 150,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            var options = new List<string>();
            if (!string.IsNullOrEmpty(attr.Options))
            {
                options = attr.Options.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(o => o.Trim())
                                      .ToList();
            }

            comboBox.Items.Add("无");

            foreach (var option in options)
            {
                comboBox.Items.Add(option);
            }

            // 设置选中项
            if (currentValue != null)
            {
                var currentStr = currentValue.ToString();
                if (!string.IsNullOrEmpty(currentStr) && options.Contains(currentStr))
                {
                    comboBox.SelectedItem = currentStr;
                }
                else if (int.TryParse(currentStr, out int intValue))
                {
                    var matchingOption = options.FirstOrDefault(o => o.StartsWith($"{intValue}-"));
                    comboBox.SelectedItem = matchingOption ?? "无";
                }
                else
                {
                    comboBox.SelectedItem = "无";
                }
            }

            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem == null || comboBox.SelectedItem.ToString() == "无")
                {
                    prop.SetValue(dataObject, prop.PropertyType == typeof(int) ? 0 : "");
                    return;
                }

                var selectedValue = comboBox.SelectedItem.ToString();

                if (prop.PropertyType == typeof(int))
                {
                    var match = Regex.Match(selectedValue, @"^(\d+)-");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int intValue))
                    {
                        prop.SetValue(dataObject, intValue);
                    }
                    else if (int.TryParse(selectedValue, out int intValue2))
                    {
                        prop.SetValue(dataObject, intValue2);
                    }
                    else
                    {
                        prop.SetValue(dataObject, 0);
                    }
                }
                else
                {
                    prop.SetValue(dataObject, selectedValue);
                }
            };

            panel.Children.Add(comboBox);
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