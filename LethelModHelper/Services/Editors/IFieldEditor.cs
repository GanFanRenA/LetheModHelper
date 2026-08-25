using System.Reflection;
using System.Windows;
using LethelModHelper.Core.Models;

namespace LethelModHelper.Services.Editors
{
    public interface IFieldEditor
    {
        bool CanEdit(PropertyInfo property, EditableAttribute attribute);

        UIElement Create(
            object dataObject,
            PropertyInfo property,
            EditableAttribute attribute,
            int depth);
    }
}