// Services/Renderers/PassiveRenderer.cs

using LethelModHelper.Core.Models;
using LethelModHelper.Services.Renderers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LethelModHelper.Services.Renderers
{
    public class PassiveRenderer : IDataRenderer
    {
        private readonly ScriptParser _scriptParser = new();
        private RendererContext? _context;
        private string? _currentFilePath;

        public bool CanRender(object data)
        {
            return data is PassiveData;
        }

        public void SetContext(RendererContext context)
        {
            _context = context;
        }

        public FrameworkElement Render(object data, string filePath)
        {
            _currentFilePath = filePath;
            var passiveData = (PassiveData)data;
            var mainPanel = new StackPanel();

            if (passiveData.list == null || passiveData.list.Count == 0)
            {
                mainPanel.Children.Add(CreateTextBlock("没有 passive 数据", Brushes.Gray));
                return mainPanel;
            }

            // 标题
            var headerPanel = CreateHeaderPanel(passiveData, filePath);
            mainPanel.Children.Add(headerPanel);

            // 每个 passive 条目
            foreach (var entry in passiveData.list)
            {
                var expander = CreatePassiveEntry(entry);
                mainPanel.Children.Add(expander);
            }

            return mainPanel;
        }

        private StackPanel CreateHeaderPanel(PassiveData data, string filePath)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            headerPanel.Children.Add(CreateTextBlock(
                $"📋 共 {data.list.Count} 个 passive 条目",
                Brushes.Black, 14));

            var saveButton = RendererUIHelper.CreateSaveButton(_context, data, filePath);
            headerPanel.Children.Add(saveButton);

            return headerPanel;
        }

        private Expander CreatePassiveEntry(PassiveEntry entry)
        {
            // ===== 获取本地化信息 =====
            var locale = LocaleCache.GetPassiveLocale(entry.id.ToString());

            // ===== 生成带本地化名称的 Header =====
            string headerText;
            if (locale != null && !string.IsNullOrEmpty(locale.name))
            {
                headerText = $"📋 {locale.name} (ID: {entry.id})";
            }
            else
            {
                headerText = $"📋 被动 ID: {entry.id}";
            }

            var expander = new Expander
            {
                Header = headerText,
                IsExpanded = false,
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(10)
            };

            var contentStack = new StackPanel();

            // ===== 基础信息 =====
            AddBorderedSection(contentStack, "📋 被动数据",
                Brushes.DarkGreen, Brushes.Honeydew,
                stack =>
                {
                    stack.Children.Add(EditorGenerator.GenerateEditor(entry));

                    // ===== 显示本地化信息 =====
                    if (locale != null)
                    {
                        AddLocaleEditor(stack, locale, entry.id);
                    }
                    else
                    {
                        // 显示创建本地化的按钮
                        var createPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(0, 10, 0, 0)
                        };

                        createPanel.Children.Add(new TextBlock
                        {
                            Text = "⚠️ 未找到 Passive 本地化数据 (passiveList)",
                            Foreground = Brushes.Orange,
                            FontSize = 11,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 10, 0)
                        });

                        var createBtn = new Button
                        {
                            Content = "➕ 创建 Passive 本地化",
                            Background = Brushes.LightGreen,
                            Padding = new Thickness(10, 5, 10, 5)
                        };
                        createBtn.Click += (s, e) =>
                        {
                            CreatePassiveLocale(entry.id);
                        };
                        createPanel.Children.Add(createBtn);
                        stack.Children.Add(createPanel);
                    }
                });

            // ===== 脚本部分 =====
            var scripts = (entry.requireIDList ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (scripts.Count > 0)
            {
                AddBorderedSection(contentStack, "📜 脚本 (requireIDList)",
                    Brushes.DarkOrange, Brushes.LemonChiffon,
                    stack =>
                    {
                        stack.Children.Add(CreateSelectableText($"共 {scripts.Count} 个脚本", true, null, 12));
                        foreach (var script in scripts)
                        {
                            var parsed = _scriptParser.Parse(script);
                            stack.Children.Add(CreateScriptDisplayBlock(script, parsed));
                        }
                    });
            }

            expander.Content = contentStack;
            return expander;
        }

        /// <summary>
        /// 添加本地化编辑器
        /// </summary>
        /// <summary>
        /// 添加本地化编辑器
        /// </summary>
        private void AddLocaleEditor(StackPanel stack, PassiveLocaleEntry locale, int passiveId)
        {
            var localeBorder = new Border
            {
                BorderBrush = Brushes.MediumBlue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 5, 0, 0),
                Padding = new Thickness(8),
                Background = Brushes.Ivory
            };

            var localeStack = new StackPanel();

            localeStack.Children.Add(new TextBlock
            {
                Text = "🌍 本地化 (passiveList)",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.MediumBlue,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // 被动名称
            AddLocaleTextBox(localeStack, "📛 名称:", locale.name ?? "", 300, false,
                text => { locale.name = text; });

            // 被动描述
            AddLocaleTextBox(localeStack, "📝 描述:", locale.desc?.Replace("\\n", "\n") ?? "",
                double.NaN, true,
                text => { locale.desc = text.Replace("\n", "\\n"); }, 80);

            // 风味文本
            AddLocaleTextBox(localeStack, "🎭 风味:", locale.flavor ?? "",
                double.NaN, true,
                text => { locale.flavor = text; }, 60, true);

            // 概要 (虽然没用，但还是保留编辑)
            AddLocaleTextBox(localeStack, "📄 概要:", locale.summary ?? "",
                double.NaN, true,
                text => { locale.summary = text; }, 40, true);

            localeBorder.Child = localeStack;
            stack.Children.Add(localeBorder);
        }

        /// <summary>
        /// 添加本地化文本框辅助方法
        /// </summary>
        private void AddLocaleTextBox(StackPanel stack, string label, string text,
            double width, bool isMultiline, Action<string> onTextChanged,
            double height = 0, bool isItalic = false)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            });

            var textBox = new TextBox
            {
                Text = text,
                Width = width > 0 ? width : double.NaN,
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

        /// <summary>
        /// 创建 Passive 本地化
        /// </summary>
        private void CreatePassiveLocale(int passiveId)
        {
            var existingEntry = LocaleCache.GetPassiveLocale(passiveId.ToString());

            if (existingEntry != null)
            {
                MessageBox.Show($"Passive {passiveId} 已有本地化数据，请直接编辑。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var newLocale = new PassiveLocaleEntry
            {
                id = passiveId.ToString(),
                name = $"被动 {passiveId}",
                desc = "",
                summary = "",
                flavor = ""
            };

            LocaleCache.PassiveLocaleMap[passiveId.ToString()] = newLocale;
            LocaleCache.SavePassiveLocaleData();

            MessageBox.Show($"Passive {passiveId} 本地化已创建！请刷新查看。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
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

        #endregion
    }
}