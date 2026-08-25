using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LethelModHelper.Core.Models;

namespace LethelModHelper.Services.Renderers
{
    public class PassiveRenderer : IDataRenderer
    {
        private readonly ScriptParser _scriptParser = new();
        private Action<object>? _saveCallback;

        public bool CanRender(object data)
        {
            return data is PassiveData;
        }

        public void SetSaveCallback(Action<object> saveAction)
        {
            _saveCallback = saveAction;
        }

      

        public FrameworkElement Render(object data)
        {
            var passiveData = (PassiveData)data;
            var mainPanel = new StackPanel();

            if (passiveData.list == null || passiveData.list.Count == 0)
            {
                mainPanel.Children.Add(CreateTextBlock("没有 passive 数据", Brushes.Gray));
                return mainPanel;
            }

            // 标题
            mainPanel.Children.Add(CreateTextBlock(
                $"共 {passiveData.list.Count} 个 passive 条目",
                Brushes.Black, 14, new Thickness(0, 0, 0, 10)));

            foreach (var entry in passiveData.list)
            {
                var expander = new Expander
                {
                    Header = $"被动 ID: {entry.id}",
                    IsExpanded = false,
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(10)
                };

                var contentStack = new StackPanel();
                var scripts = (entry.requireIDList ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (scripts.Count == 0)
                {
                    contentStack.Children.Add(CreateSelectableText("（没有 requireIDList）", false, Brushes.Gray, 12));
                }
                else
                {
                    contentStack.Children.Add(CreateSelectableText("📜 requireIDList:", true, null, 12));

                    foreach (var script in scripts)
                    {
                        var parsed = _scriptParser.Parse(script);
                        contentStack.Children.Add(CreateScriptDisplayBlock(script, parsed));
                    }
                }

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
                Margin = margin ?? new Thickness(0),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private TextBlock CreateSelectableText(string text, bool isBold = false,
            Brush? foreground = null, double fontSize = 12)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                FontSize = fontSize,
                Foreground = foreground ?? Brushes.Black,
                Margin = new Thickness(0, 2, 0, 2)
            };
        }

        private TextBox CreateScriptDisplayBlock(string rawScript, ParsedScript parsed)
        {
            return new TextBox
            {
                Text = $"  📜 脚本: {rawScript}",
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                Margin = new Thickness(10, 2, 0, 2),
                Foreground = Brushes.Gray,
                IsTabStop = false,
                IsReadOnlyCaretVisible = false
            };
        }

        #endregion
    }
}