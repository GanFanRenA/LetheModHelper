using System;
using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LethelModHelper.Core.Models;

namespace LethelModHelper.Services.Editors
{
    public class SpeedRangeEditor : IFieldEditor
    {
        public bool CanEdit(PropertyInfo property, EditableAttribute attribute)
        {
            return attribute.ControlType == "SpeedRange";
        }

        public UIElement Create(
            object dataObject,
            PropertyInfo property,
            EditableAttribute attribute,
            int depth)
        {
            return CreateSpeedRangeField(dataObject, attribute.Label);
        }

        private UIElement CreateSpeedRangeField(object dataObject, string label)
        {
            var type = dataObject.GetType();
            var minProp = type.GetProperty("minSpeedList");
            var maxProp = type.GetProperty("maxSpeedList");

            var minList = minProp?.GetValue(dataObject) as IList;
            var maxList = maxProp?.GetValue(dataObject) as IList;

            var border = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(8)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"⚡ {label}:",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            });

            if (minList == null || maxList == null || minList.Count == 0 || maxList.Count == 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "  (无速度数据)",
                    Foreground = Brushes.Gray,
                    FontSize = 11
                });
                border.Child = stack;
                return border;
            }

            int count = Math.Min(minList.Count, maxList.Count);

            for (int i = 0; i < count; i++)
            {
                var tierName = i switch
                {
                    0 => "Tier 1",
                    1 => "Tier 2",
                    2 => "Tier 3",
                    3 => "Tier 4",
                    _ => $"Tier {i + 1}"
                };

                var rowPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 3, 0, 3)
                };

                rowPanel.Children.Add(new TextBlock
                {
                    Text = $"  {tierName}: ",
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 55
                });

                var minBox = new TextBox
                {
                    Text = minList[i]?.ToString() ?? "0",
                    Width = 40,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Center
                };
                int idx = i;
                var minListRef = minList;
                minBox.TextChanged += (s, e) =>
                {
                    if (int.TryParse(minBox.Text, out int newValue))
                    {
                        minListRef[idx] = newValue;
                        minBox.Background = Brushes.LightYellow;
                    }
                    else
                    {
                        minBox.Background = Brushes.LightPink;
                    }
                };
                rowPanel.Children.Add(minBox);

                rowPanel.Children.Add(new TextBlock
                {
                    Text = " - ",
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(3, 0, 3, 0)
                });

                var maxBox = new TextBox
                {
                    Text = maxList[i]?.ToString() ?? "0",
                    Width = 40,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Center
                };
                int maxIdx = i;
                var maxListRef = maxList;
                maxBox.TextChanged += (s, e) =>
                {
                    if (int.TryParse(maxBox.Text, out int newValue))
                    {
                        maxListRef[maxIdx] = newValue;
                        maxBox.Background = Brushes.LightYellow;
                    }
                    else
                    {
                        maxBox.Background = Brushes.LightPink;
                    }
                };
                rowPanel.Children.Add(maxBox);

                var resetBtn = new Button
                {
                    Content = "↩️",
                    Width = 24,
                    Height = 24,
                    Margin = new Thickness(5, 0, 0, 0),
                    FontSize = 10,
                    Padding = new Thickness(2),
                    ToolTip = "重置该行"
                };
                int resetIdx = i;
                resetBtn.Click += (s, e) =>
                {
                    var originalMin = minListRef[resetIdx];
                    var originalMax = maxListRef[resetIdx];
                    minBox.Text = originalMin?.ToString() ?? "0";
                    maxBox.Text = originalMax?.ToString() ?? "0";
                    minBox.Background = Brushes.White;
                    maxBox.Background = Brushes.White;
                };
                rowPanel.Children.Add(resetBtn);

                stack.Children.Add(rowPanel);
            }

            stack.Children.Add(new TextBlock
            {
                Text = "💡 修改数值后会自动保存到列表中",
                Foreground = Brushes.Gray,
                FontSize = 10,
                Margin = new Thickness(0, 5, 0, 0)
            });

            border.Child = stack;
            return border;
        }
    }
}