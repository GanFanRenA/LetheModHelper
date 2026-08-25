using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using LethelModHelper.Core.Models;

namespace LethelModHelper.Services.Editors
{
    public class FieldEditorFactory
    {
        private readonly List<IFieldEditor> _editors;

        public FieldEditorFactory(IEnumerable<IFieldEditor> editors)
        {
            _editors = editors.ToList();
        }

        public UIElement? Create(
            object dataObject,
            PropertyInfo property,
            EditableAttribute attribute,
            int depth)
        {
            var editor = _editors.FirstOrDefault(
                x => x.CanEdit(property, attribute));

            return editor?.Create(
                dataObject,
                property,
                attribute,
                depth);
        }
    }
}