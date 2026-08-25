using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LethelModHelper.Core.Models;

namespace LethelModHelper.Services.Editors
{
    public class TextEditor : IFieldEditor
    {
        public bool CanEdit(PropertyInfo property, EditableAttribute attribute)
        {
            return attribute.ControlType == "Text";
        }

        public UIElement Create(
            object dataObject,
            PropertyInfo property,
            EditableAttribute attribute,
            int depth)
        {
            return CreateTextField(dataObject, property, attribute);
        }

        private UIElement CreateTextField(
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

            var box = new TextBox
            {
                Text = currentValue?.ToString() ?? "",
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

            box.TextChanged += (s, e) =>
            {
                prop.SetValue(dataObject, box.Text);
                box.Background = Brushes.LightYellow;
            };
            panel.Children.Add(box);

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