using LethelModHelper.Core.Models;
using LethelModHelper.Services.Renderers;
using LethelModHelper.Services.Renderers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LethelModHelper.Services.Renderers
{
    public class BuffRenderer : IDataRenderer
    {
        private readonly ScriptParser _scriptParser = new();
        private RendererContext? _context;

        public bool CanRender(object data)
        {
            return data is BuffData;
        }

        public void SetContext(RendererContext context)
        {
            _context = context;
        }

        public FrameworkElement Render(object data, string filePath)
        {
            var buffData = (BuffData)data;
            var mainPanel = new StackPanel();

            // 检查数据是否为空
            if (buffData.list == null || buffData.list.Count == 0)
            {
                mainPanel.Children.Add(CreateSelectableText("没有 Buff 数据", false, Brushes.Gray, 11));
                return mainPanel;
            }

            // === 标题行 ===
            var headerPanel = CreateHeaderPanel(buffData, filePath);
            mainPanel.Children.Add(headerPanel);

            // === 每个 Buff 条目 ===
            foreach (var entry in buffData.list)
            {
                var entryExpander = CreateBuffEntry(entry);
                mainPanel.Children.Add(entryExpander);
            }

            return mainPanel;
        }

        #region 辅助方法

        private StackPanel CreateHeaderPanel(BuffData data,string filePath)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            headerPanel.Children.Add(CreateSelectableText($"共 {data.list.Count} 个 Buff", true, null, 14));

            // 保存本地化按钮
            var saveLocaleButton = new Button
            {
                Content = "💾 保存本地化",
                Margin = new Thickness(10, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5),
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.LightGreen
            };
            saveLocaleButton.Click += (s, e) =>
            {
                LocaleCache.SaveBuffLocaleData();
                LocaleCache.SaveKeywordLocaleData();
                MessageBox.Show("本地化已保存！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            };
            headerPanel.Children.Add(saveLocaleButton);

            // 保存按钮
            var saveButton = RendererUIHelper.CreateSaveButton(_context, data, filePath);
            headerPanel.Children.Add(saveButton);

            return headerPanel;
        }

        private Expander CreateBuffEntry(BuffEntry entry)
        {
            var locale = LocaleCache.GetBuffLocale(entry.id);
            var headerText = $"📊 {entry.id} ({(entry.buffType ?? "未知")})";

            if (locale != null && !string.IsNullOrEmpty(locale.name))
            {
                headerText = $"📊 {locale.name} ({entry.id}) - {(entry.buffType ?? "未知")}";
            }

            var expander = new Expander
            {
                Header = headerText,
                IsExpanded = false,
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(10)
            };

            var contentStack = new StackPanel();

            // Buff 数据部分
            AddBorderedSection(contentStack, "📋 Buff 数据",
                Brushes.DarkGreen, Brushes.Honeydew,
                stack => stack.Children.Add(EditorGenerator.GenerateEditor(entry)));

            // 本地化部分
            if (locale != null)
            {
                AddBorderedSection(contentStack, "📋 buflist 本地化文本 (自动同步到 keywordList)",
                    Brushes.DarkBlue, Brushes.AliceBlue,
                    stack => AddLocaleEditor(stack, locale));
            }

            // 脚本部分
            if (entry.list?.Any(a => !string.IsNullOrEmpty(a.ability)) == true)
            {
                AddBorderedSection(contentStack, "📜 脚本",
                    Brushes.DarkOrange, Brushes.LemonChiffon,
                    stack => AddScriptDisplays(stack, entry.list));
            }

            expander.Content = contentStack;
            return expander;
        }

        private void AddBorderedSection(StackPanel parent, string title,
            Brush borderColor, Brush backgroundColor, Action<StackPanel> addContent)
        {
            var border = new Border
            {
                BorderBrush = borderColor,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10),
                Background = backgroundColor
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = borderColor,
                Margin = new Thickness(0, 0, 0, 8)
            });

            addContent(stack);
            border.Child = stack;
            parent.Children.Add(border);
        }

        private void AddLocaleEditor(StackPanel stack, BuffLocaleEntry locale)
        {
            AddLocaleTextBox(stack, "📛 名字:", locale.name, 250, false,
                (text) =>
                {
                    locale.name = text;
                    var keywordEntry = LocaleCache.GetKeywordLocale(locale.id);
                    if (keywordEntry != null) keywordEntry.name = text;
                });

            AddLocaleTextBox(stack, "📝 描述:", locale.desc?.Replace("\\n", "\n") ?? "",
                double.NaN, true,
                (text) =>
                {
                    locale.desc = text.Replace("\n", "\\n");
                    var keywordEntry = LocaleCache.GetKeywordLocale(locale.id);
                    if (keywordEntry != null) keywordEntry.desc = text.Replace("\n", "\\n");
                }, 80);

            AddLocaleTextBox(stack, "🎭 风味文本:", locale.flavor ?? "", double.NaN, true,
                (text) =>
                {
                    locale.flavor = text;
                    var keywordEntry = LocaleCache.GetKeywordLocale(locale.id);
                    if (keywordEntry != null) keywordEntry.flavor = text;
                }, 60, true);
        }

        private void AddLocaleTextBox(StackPanel stack, string label, string text,
            double width, bool isMultiline, Action<string> onTextChanged,
            double height = 0, bool isItalic = false)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            panel.Children.Add(CreateSelectableText(label, true, null, 12));

            var textBox = new TextBox
            {
                Text = text,
                Width = width,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

            if (isMultiline)
            {
                textBox.Width = double.NaN;
                textBox.Height = height > 0 ? height : 80;
                textBox.TextWrapping = TextWrapping.Wrap;
                textBox.AcceptsReturn = true;
                textBox.AcceptsTab = true;
                textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                textBox.Margin = new Thickness(10, 2, 0, 5);
            }

            if (isItalic)
            {
                textBox.FontStyle = FontStyles.Italic;
            }

            textBox.TextChanged += (s, e) =>
            {
                textBox.Background = Brushes.LightYellow;
                onTextChanged(textBox.Text);
            };

            stack.Children.Add(panel);
            stack.Children.Add(textBox);
        }

        private void AddScriptDisplays(StackPanel stack, List<BuffAbility> abilities)
        {
            foreach (var ability in abilities.Where(a => !string.IsNullOrEmpty(a.ability)))
            {
                var parsed = _scriptParser.Parse(ability.ability);
                stack.Children.Add(CreateScriptDisplayBlock(ability.ability, parsed));
            }
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
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            };
        }

        private TextBox CreateScriptDisplayBlock(string rawScript, ParsedScript parsed)
        {
            var textBox = new TextBox
            {
                Text = $"📜 脚本: {rawScript}",
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
            return textBox;
        }

        #endregion
    }
}