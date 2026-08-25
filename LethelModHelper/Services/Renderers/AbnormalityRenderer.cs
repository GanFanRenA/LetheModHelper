using LethelModHelper.Core.Models;
using LethelModHelper.Services.Renderers.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LethelModHelper.Services.Renderers
{
    public class AbnormalityRenderer : IDataRenderer
    {
        private RendererContext? _context;
        public bool CanRender(object data)
        {
            return data is AbnormalityData;
        }

        public void SetContext(RendererContext context)
        {
            _context = context;
        }

        public FrameworkElement Render(object data,string filePath)
        {
            var abnormalityData = (AbnormalityData)data;
            var mainPanel = new StackPanel();

            if (abnormalityData.list == null || abnormalityData.list.Count == 0)
            {
                mainPanel.Children.Add(CreateTextBlock("没有异常体数据", Brushes.Gray));
                return mainPanel;
            }

            // === 标题行 ===
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            headerPanel.Children.Add(CreateTextBlock(
                $"共 {abnormalityData.list.Count} 个异常体",
                Brushes.Black, 14));

            // 保存按钮
            var saveButton = RendererUIHelper.CreateSaveButton(_context, data,filePath);
            headerPanel.Children.Add(saveButton);
            mainPanel.Children.Add(headerPanel);

            // === 每个异常体条目 ===
            foreach (var entry in abnormalityData.list)
            {
                var expander = new Expander
                {
                    Header = $"ID: {entry.id} | 类型: {entry.classType ?? "未知"}",
                    IsExpanded = false,
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(10)
                };

                var contentStack = new StackPanel();
                contentStack.Children.Add(EditorGenerator.GenerateEditor(entry));
                expander.Content = contentStack;
                mainPanel.Children.Add(expander);
            }

            return mainPanel;
        }

        #region 辅助方法

        private TextBlock CreateTextBlock(string text, Brush foreground,
            double fontSize = 12, Thickness? margin = null)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = foreground,
                FontSize = fontSize,
                Margin = margin ?? new Thickness(0)
            };
        }

        #endregion
    }
}