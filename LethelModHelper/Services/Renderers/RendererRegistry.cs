using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LethelModHelper.Services.Renderers
{
    public class RendererRegistry
    {
        private readonly List<IDataRenderer> _renderers;

        public RendererRegistry(RendererContext context)
        {
            _renderers = new List<IDataRenderer>
            {
                new PersonalityRenderer(),
                new PassiveRenderer(),
                new BuffRenderer(),
                new AbnormalityRenderer()
            };

            foreach (var renderer in _renderers)
            {
                renderer.SetContext(context);
            }
        }

        public FrameworkElement? Render(object data)
        {
            var renderer = _renderers
                .FirstOrDefault(r => r.CanRender(data));

            return renderer?.Render(data, "filePath");
        }
    }
}