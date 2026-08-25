using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LethelModHelper.Core.Models;

namespace LethelModHelper.Services.Editors
{
    public class BooleanEditor : IFieldEditor
    {
        public bool CanEdit(PropertyInfo property, EditableAttribute attribute)
        {
            return attribute.ControlType == "Boolean";
        }

        public UIElement Create(
            object dataObject,
            PropertyInfo property,
            EditableAttribute attribute,
            int depth)
        {
            return CreateBooleanField(dataObject, property, attribute);
        }

        private UIElement CreateBooleanField(
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

            var checkBox = new CheckBox
            {
                IsChecked = (bool?)currentValue ?? false,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };
            checkBox.Checked += (s, e) => prop.SetValue(dataObject, true);
            checkBox.Unchecked += (s, e) => prop.SetValue(dataObject, false);
            panel.Children.Add(checkBox);

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