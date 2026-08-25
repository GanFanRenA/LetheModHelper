// Services/Renderers/SkillRenderer.cs
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
    public class SkillRenderer : IDataRenderer
    {
        private readonly ScriptParser _scriptParser = new();
        private RendererContext? _context;
        private string? _currentFilePath;

        public bool CanRender(object data)
        {
            return data is SkillData;
        }

        public void SetContext(RendererContext context)
        {
            _context = context;
        }

        public FrameworkElement Render(object data, string filePath)
        {
            _currentFilePath = filePath;
            var skillData = (SkillData)data;
            var mainPanel = new StackPanel();

            if (skillData.list == null || skillData.list.Count == 0)
            {
                mainPanel.Children.Add(CreateSelectableText("没有技能数据", false, Brushes.Gray, 11));
                return mainPanel;
            }

            var headerPanel = CreateHeaderPanel(skillData, filePath);
            mainPanel.Children.Add(headerPanel);

            var statsPanel = CreateStatsPanel(skillData);
            mainPanel.Children.Add(statsPanel);

            foreach (var entry in skillData.list)
            {
                var entryExpander = CreateSkillEntry(entry);
                mainPanel.Children.Add(entryExpander);
            }

            return mainPanel;
        }

        private StackPanel CreateHeaderPanel(SkillData data, string filePath)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            headerPanel.Children.Add(CreateSelectableText($"⚔️ 共 {data.list.Count} 个技能", true, null, 14));

            var saveButton = RendererUIHelper.CreateSaveButton(_context, data, filePath);
            headerPanel.Children.Add(saveButton);

            return headerPanel;
        }

        private StackPanel CreateStatsPanel(SkillData data)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var skill1Count = data.list.Count(e => e.skillTier == 1);
            var skill2Count = data.list.Count(e => e.skillTier == 2);
            var skill3Count = data.list.Count(e => e.skillTier == 3);

            panel.Children.Add(new TextBlock
            {
                Text = $"📊 Skill 1: {skill1Count}  |  Skill 2: {skill2Count}  |  Skill 3: {skill3Count}",
                FontSize = 12,
                Foreground = Brushes.DarkGray,
                Margin = new Thickness(0, 0, 10, 0)
            });

            return panel;
        }

        private Expander CreateSkillDataExpander(SkillDataEntry skillData)
        {
            var attributeDisplay = AttributeTypeMapping.GetDisplayName(skillData.attributeType);
            var powerDisplay = GetPowerDisplay(skillData);

            var headerText = $"{attributeDisplay} | {skillData.atkType} | 威力: {powerDisplay}";

            var expander = new Expander
            {
                Header = headerText,
                IsExpanded = false,
                Margin = new Thickness(0, 3, 0, 3),
                Padding = new Thickness(8),
                Background = Brushes.White
            };

            var dataStack = new StackPanel();

            dataStack.Children.Add(EditorGenerator.GenerateEditor(skillData));

            if (skillData.abilityScriptList?.Count > 0)
            {
                dataStack.Children.Add(CreateScriptSection("📜 技能脚本", skillData.abilityScriptList));
            }

            if (skillData.coinList?.Count > 0)
            {
                dataStack.Children.Add(CreateCoinSection(skillData.coinList));
            }

            expander.Content = dataStack;
            return expander;
        }

        // ===== 只保留一个 CreateSkillEntry 方法 =====
        private Expander CreateSkillEntry(SkillEntry entry)
        {
            var localeLevel = LocaleCache.GetSkillLocaleLevel(entry.id.ToString());
            var tierText = GetSkillTierText(entry.skillTier);
            var typeText = entry.skillType ?? "SKILL";

            string headerText;
            if (localeLevel != null && !string.IsNullOrEmpty(localeLevel.name))
            {
                headerText = $"⚔️ [{typeText}] {localeLevel.name} (ID: {entry.id}) - {tierText}";
            }
            else
            {
                headerText = $"⚔️ [{typeText}] 技能 {entry.id} - {tierText}";
            }

            var expander = new Expander
            {
                Header = headerText,
                IsExpanded = false,
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(10),
                Background = Brushes.WhiteSmoke
            };

            var contentStack = new StackPanel();

            AddBorderedSection(contentStack, "📋 基础信息",
                Brushes.DarkBlue, Brushes.AliceBlue,
                stack =>
                {
                    stack.Children.Add(EditorGenerator.GenerateEditor(entry));

                    if (localeLevel != null)
                    {
                        AddLocaleEditor(stack, localeLevel, entry.id);
                    }
                    else
                    {
                        var createPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(0, 10, 0, 0)
                        };

                        createPanel.Children.Add(new TextBlock
                        {
                            Text = "⚠️ 未找到技能本地化数据 (skillList)",
                            Foreground = Brushes.Orange,
                            FontSize = 11,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 10, 0)
                        });

                        var createBtn = new Button
                        {
                            Content = "➕ 创建技能本地化",
                            Background = Brushes.LightGreen,
                            Padding = new Thickness(10, 5, 10, 5)
                        };
                        createBtn.Click += (s, e) =>
                        {
                            CreateSkillLocale(entry.id);
                        };
                        createPanel.Children.Add(createBtn);
                        stack.Children.Add(createPanel);
                    }
                });

            // ===== 显示技能数据（单个） =====
            if (entry.skillData != null)
            {
                AddBorderedSection(contentStack, "📊 技能数据",
                    Brushes.DarkGreen, Brushes.Honeydew,
                    stack =>
                    {
                        var dataExpander = CreateSkillDataExpander(entry.skillData[0]);
                        stack.Children.Add(dataExpander);
                    });
            }

            expander.Content = contentStack;
            return expander;
        }

        /// <summary>
        /// 生成威力显示字符串
        /// </summary>
        private string GetPowerDisplay(SkillDataEntry skillData)
        {
            var parts = new List<string>();

            parts.Add(skillData.defaultValue.ToString());

            if (skillData.coinList != null)
            {
                foreach (var coin in skillData.coinList)
                {
                    var opSymbol = coin.operatorType == "ADD" ? "+" :
                                   coin.operatorType == "SUB" ? "-" : "×";
                    parts.Add($"{opSymbol}{coin.scale}");
                }
            }

            return string.Join(" ", parts);
        }

        private UIElement CreateCoinSection(List<CoinEntry> coinList)
        {
            var border = new Border
            {
                BorderBrush = Brushes.Gold,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(10),
                Background = Brushes.LightYellow
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = $"🪙 硬币 ({coinList.Count} 个)",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.DarkGoldenrod,
                Margin = new Thickness(0, 0, 0, 8)
            });

            for (int i = 0; i < coinList.Count; i++)
            {
                var coin = coinList[i];
                var coinBorder = new Border
                {
                    BorderBrush = GetCoinColor(coin.color ?? "GREY"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 3, 0, 3),
                    Padding = new Thickness(8),
                    Background = Brushes.White
                };

                var coinStack = new StackPanel();

                var coinHeader = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                var opSymbol = coin.operatorType == "ADD" ? "+" :
                               coin.operatorType == "SUB" ? "-" : "×";
                var gradeText = coin.grade > 0 ? $" [等级 {coin.grade}]" : "";

                coinHeader.Children.Add(new TextBlock
                {
                    Text = $"硬币 {i + 1}: {opSymbol}{coin.scale}{gradeText}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12
                });

                if (coin.grade > 0)
                {
                    var gradeType = coin.grade >= 3 ? "切除" : "不可破坏";
                    coinHeader.Children.Add(new TextBlock
                    {
                        Text = $" ({gradeType})",
                        FontSize = 11,
                        Foreground = Brushes.Red,
                        Margin = new Thickness(5, 0, 0, 0)
                    });
                }

                coinStack.Children.Add(coinHeader);

                if (coin.abilityScriptList?.Count > 0)
                {
                    coinStack.Children.Add(CreateScriptDisplay(coin.abilityScriptList, 15));
                }

                coinBorder.Child = coinStack;
                stack.Children.Add(coinBorder);
            }

            border.Child = stack;
            return border;
        }

        private Brush GetCoinColor(string color)
        {
            return color.ToUpper() switch
            {
                "GREEN" => Brushes.Green,
                "PURPLE" => Brushes.Purple,
                _ => Brushes.Gray
            };
        }

        private UIElement CreateScriptSection(string title, List<ScriptEntry> scriptList)
        {
            var border = new Border
            {
                BorderBrush = Brushes.DarkOrange,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(8),
                Background = Brushes.LemonChiffon
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.DarkOrange,
                Margin = new Thickness(0, 0, 0, 5)
            });

            stack.Children.Add(CreateScriptDisplay(scriptList, 0));
            border.Child = stack;
            return border;
        }

        private UIElement CreateScriptDisplay(List<ScriptEntry> scriptList, int indentLevel)
        {
            var stack = new StackPanel();

            foreach (var script in scriptList)
            {
                if (string.IsNullOrEmpty(script.scriptName)) continue;

                var parsed = _scriptParser.Parse(script.scriptName);
                var margin = new Thickness(indentLevel + 5, 1, 0, 1);

                var nameText = new TextBlock
                {
                    Text = $"📜 {script.scriptName}",
                    FontSize = 11,
                    Foreground = Brushes.DarkSlateGray,
                    Margin = margin,
                    TextWrapping = TextWrapping.Wrap
                };
                stack.Children.Add(nameText);

                if (parsed.IsValid && parsed.Parts.Count > 0)
                {
                    foreach (var part in parsed.Parts)
                    {
                        var color = GetPartColor(part.Type);
                        var partText = $"    • [{part.Type}] {part.Name}";
                        if (part.Arguments.Count > 0)
                        {
                            partText += $"({string.Join(", ", part.Arguments)})";
                        }
                        stack.Children.Add(new TextBlock
                        {
                            Text = partText,
                            FontSize = 10,
                            Foreground = color,
                            Margin = new Thickness(indentLevel + 15, 0, 0, 0)
                        });
                    }
                }
            }

            return stack;
        }

        private Brush GetPartColor(string type)
        {
            return type.ToUpper() switch
            {
                "TIMING" => Brushes.Blue,
                "LUA" => Brushes.Green,
                "LUAMAIN" => Brushes.Purple,
                "LOOP" => Brushes.Orange,
                "VALUE" => Brushes.Brown,
                "IF" => Brushes.Red,
                "FUNCTION" => Brushes.DarkCyan,
                _ => Brushes.Black
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

        private void AddLocaleEditor(StackPanel stack, SkillLocaleLevel localeLevel, int skillId)
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
                Text = "🌍 本地化 (skillList)",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.MediumBlue,
                Margin = new Thickness(0, 0, 0, 5)
            });

            AddLocaleTextBox(localeStack, "📛 名称:", localeLevel.name ?? "", 300, false,
                text => { localeLevel.name = text; });

            AddLocaleTextBox(localeStack, "📝 描述:", localeLevel.desc?.Replace("\\n", "\n") ?? "",
                double.NaN, true,
                text => { localeLevel.desc = text.Replace("\n", "\\n"); }, 80);

            AddLocaleTextBox(localeStack, "🎭 风味:", localeLevel.flavor ?? "",
                double.NaN, true,
                text => { localeLevel.flavor = text; }, 60, true);

            AddLocaleTextBox(localeStack, "📌 缩写:", localeLevel.abName ?? "", 150, false,
                text => { localeLevel.abName = text; });

            if (localeLevel.coinlist != null && localeLevel.coinlist.Count > 0)
            {
                var coinPanel = new StackPanel { Margin = new Thickness(0, 5, 0, 3) };
                coinPanel.Children.Add(new TextBlock
                {
                    Text = $"🪙 硬币描述 ({localeLevel.coinlist.Count} 个硬币)",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11,
                    Foreground = Brushes.DarkGoldenrod,
                    Margin = new Thickness(0, 0, 0, 3)
                });

                for (int i = 0; i < localeLevel.coinlist.Count; i++)
                {
                    var coin = localeLevel.coinlist[i];
                    if (coin.coindescs != null && coin.coindescs.Count > 0)
                    {
                        var coinDesc = coin.coindescs[0];
                        var coinPanelInner = new StackPanel { Margin = new Thickness(10, 2, 0, 2) };

                        AddLocaleTextBox(coinPanelInner, $"  硬币 {i + 1}:", coinDesc.desc ?? "",
                            double.NaN, true,
                            text => { coinDesc.desc = text; }, 40);

                        coinPanel.Children.Add(coinPanelInner);
                    }
                }
                localeStack.Children.Add(coinPanel);
            }

            var saveLocaleBtn = new Button
            {
                Content = "💾 保存本地化 (skillList)",
                Background = Brushes.LightBlue,
                Padding = new Thickness(10, 3, 10, 3),
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                FontSize = 11
            };
            saveLocaleBtn.Click += (s, e) =>
            {
                LocaleCache.SaveSkillLocaleData();
                MessageBox.Show("技能本地化已保存！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            };
            localeStack.Children.Add(saveLocaleBtn);

            localeBorder.Child = localeStack;
            stack.Children.Add(localeBorder);
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

        private void CreateSkillLocale(int skillId)
        {
            var existingEntry = LocaleCache.GetSkillLocale(skillId.ToString());

            if (existingEntry != null)
            {
                if (existingEntry.levelList == null || existingEntry.levelList.Count == 0)
                {
                    existingEntry.levelList = new List<SkillLocaleLevel>
                    {
                        new SkillLocaleLevel
                        {
                            level = 1,
                            name = $"技能 {skillId}",
                            desc = "",
                            flavor = "",
                            abName = "",
                            coinlist = new List<SkillLocaleCoin>()
                        }
                    };
                }
                else
                {
                    MessageBox.Show($"技能 {skillId} 已有本地化数据，请直接编辑。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            else
            {
                var newLocale = new SkillLocaleEntry
                {
                    Id = skillId.ToString(),
                    levelList = new List<SkillLocaleLevel>
                    {
                        new SkillLocaleLevel
                        {
                            level = 1,
                            name = $"技能 {skillId}",
                            desc = "",
                            flavor = "",
                            abName = "",
                            coinlist = new List<SkillLocaleCoin>()
                        }
                    }
                };

                LocaleCache.SkillLocaleMap[skillId.ToString()] = newLocale;
            }

            LocaleCache.SaveSkillLocaleData();

            MessageBox.Show($"技能 {skillId} 本地化已创建！请刷新查看。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
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

        private string GetSkillTierText(int tier)
        {
            return tier switch
            {
                1 => "Skill 1",
                2 => "Skill 2",
                3 => "Skill 3",
                _ => $"Skill {tier}"
            };
        }
    }
}