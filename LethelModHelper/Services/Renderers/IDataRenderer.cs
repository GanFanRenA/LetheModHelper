using System.Windows;

namespace LethelModHelper.Services.Renderers
{
    public interface IDataRenderer
    {
        bool CanRender(object data);

        FrameworkElement Render(object data);

        void SetSaveCallback(Action<object> saveAction);
    }
}