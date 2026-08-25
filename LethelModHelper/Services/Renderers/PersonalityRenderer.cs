using LethelModHelper.Core.Models;
using LethelModHelper.Services.Renderers.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LethelModHelper.Services.Renderers
{
    public class PersonalityRenderer : IDataRenderer
    {
        private RendererContext? _context;
        private static readonly Dictionary<int, string> SinnerNames = new()
        {
            { 1, "Yi Sang" }, { 2, "Faust" }, { 3, "Don Quixote" },
            { 4, "Ryoshu" }, { 5, "Meursault" }, { 6, "Hong Lu" },
            { 7, "Heathcliff" }, { 8, "Ishmael" }, { 9, "Rodion" },
            { 10, "Sinclair" }, { 11, "Outis" }, { 12, "Gregor" }
        };

        public bool CanRender(object data)
        {
            return data is PersonalityData;
        }

        public void SetContext(RendererContext context)
        {
            _context = context;
        }


        // Services/Renderers/PersonalityRenderer.cs
        public FrameworkElement Render(object data, string filePath)
        {
            var personalityData = (PersonalityData)data;
            var mainPanel = new StackPanel();

            if (personalityData.list == null || personalityData.list.Count == 0)
            {
                mainPanel.Children.Add(CreateTextBlock("没有 Personality 数据", Brushes.Gray));
                return mainPanel;
            }

            // 标题行
            var headerPanel = CreateHeaderPanel(personalityData, filePath);
            mainPanel.Children.Add(headerPanel);

            // 每个 Personality 条目
            foreach (var entry in personalityData.list)
            {
                // ===== 确保数据已关联 =====
                // 如果 LinkedPassiveEntry 为 null，尝试重新关联
                if (entry.LinkedPassiveEntry == null && _context != null)
                {
                    // 通过反射获取 ModScanner 实例（或通过其他方式）
                    // 这里暂时跳过，由 MainWindow 负责关联
                }

                var expander = new Expander
                {
                    Header = $"ID: {entry.id} | 罪人: {GetSinnerName(entry.characterId)} | 星级: {GetStarText(entry.rank)}",
                    IsExpanded = false,
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(10)
                };

                var contentStack = new StackPanel();

                // ===== 添加 PersonalityPassive 数据显示 =====
                AddPersonalityPassiveSection(contentStack, entry);

                // 原有的编辑器
                contentStack.Children.Add(EditorGenerator.GenerateEditor(entry));

                expander.Content = contentStack;
                mainPanel.Children.Add(expander);
            }

            return mainPanel;
        }

        /// <summary>
        /// 添加 PersonalityPassive 数据显示区域（修复版）
        /// </summary>
        private void AddPersonalityPassiveSection(StackPanel parent, PersonalityEntry entry)
        {
            var passiveEntry = entry.LinkedPassiveEntry;

            // 创建边框容器
            var border = new Border
            {
                BorderBrush = Brushes.MediumPurple,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 5, 0, 10),
                Padding = new Thickness(10),
                Background = Brushes.Lavender
            };

            var stack = new StackPanel();

            // 标题
            stack.Children.Add(new TextBlock
            {
                Text = "📋 Personality Passive 配置",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.Purple,
                Margin = new Thickness(0, 0, 0, 8)
            });

            if (passiveEntry == null)
            {
                // 显示"未找到"信息
                stack.Children.Add(new TextBlock
                {
                    Text = "  ⚠️ 未找到对应的 personality_passive 数据",
                    Foreground = Brushes.Orange,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 2)
                });
                border.Child = stack;
                parent.Children.Add(border);
                return;
            }

            // 显示基本信息
            var infoText = $"  📌 Personality ID: {passiveEntry.personalityID}";
            stack.Children.Add(new TextBlock
            {
                Text = infoText,
                FontSize = 11,
                Foreground = Brushes.DarkSlateBlue,
                Margin = new Thickness(0, 2, 0, 5)
            });

            // ===== 战斗被动 =====
            AddPassiveGroupDisplay(stack, "⚔️ 战斗被动", passiveEntry.battlePassiveList);

            // ===== 支援被动 =====
            AddPassiveGroupDisplay(stack, "🛡️ 支援被动", passiveEntry.supporterPassiveList);

            border.Child = stack;
            parent.Children.Add(border);
        }

        /// <summary>
        /// 显示被动组列表（修复版 - 显示实际数据）
        /// </summary>
        private void AddPassiveGroupDisplay(StackPanel parent, string title, List<PassiveGroup>? groups)
        {
            // 先检查 null
            if (groups == null)
            {
                parent.Children.Add(new TextBlock
                {
                    Text = $"  {title}: (null)",
                    Foreground = Brushes.Red,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 2)
                });
                return;
            }

            // 检查是否为空列表
            if (groups.Count == 0)
            {
                parent.Children.Add(new TextBlock
                {
                    Text = $"  {title}: (空)",
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 2)
                });
                return;
            }

            // 统计总被动数
            var totalPassives = groups.Sum(g => g.passiveIDList?.Count ?? 0);

            // 组标题
            parent.Children.Add(new TextBlock
            {
                Text = $"  {title} ({groups.Count} 个等级组, 共 {totalPassives} 个被动):",
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Foreground = Brushes.DarkSlateBlue,
                Margin = new Thickness(0, 5, 0, 3)
            });

            // 显示每个等级组
            foreach (var group in groups.OrderBy(g => g.level))
            {
                var groupPanel = new StackPanel
                {
                    Margin = new Thickness(15, 2, 0, 2)
                };

                var levelText = group.level == 0 ? "初始解锁" : $"Lv.{group.level} 解锁";
                var passiveCount = group.passiveIDList?.Count ?? 0;

                // 等级标题
                groupPanel.Children.Add(new TextBlock
                {
                    Text = $"    📌 {levelText} (被动数: {passiveCount})",
                    FontSize = 10,
                    Foreground = Brushes.DarkSlateBlue,
                    FontWeight = FontWeights.SemiBold
                });

                // 显示被动 ID 列表（如果有）
                if (group.passiveIDList != null && group.passiveIDList.Count > 0)
                {
                    // 显示为可读格式
                    var idText = string.Join(", ", group.passiveIDList);

                    var idBlock = new TextBlock
                    {
                        Text = $"       ID: [{idText}]",
                        FontSize = 10,
                        Foreground = Brushes.DimGray,
                        Margin = new Thickness(0, 1, 0, 2),
                        TextWrapping = TextWrapping.Wrap
                    };
                    groupPanel.Children.Add(idBlock);

                    // 调试输出
                    System.Diagnostics.Debug.WriteLine($"    显示被动 IDs: {idText}");
                }
                else
                {
                    groupPanel.Children.Add(new TextBlock
                    {
                        Text = "       (无被动ID)",
                        FontSize = 10,
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic
                    });
                }

                parent.Children.Add(groupPanel);
            }
        }
        private StackPanel CreateHeaderPanel(PersonalityData data, string filePath)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            headerPanel.Children.Add(CreateTextBlock(
                $"共 {data.list.Count} 个 Personality 条目",
                Brushes.Black, 14));

            // 保存按钮
            var saveButton = RendererUIHelper.CreateSaveButton(_context, data, filePath);
            headerPanel.Children.Add(saveButton);

            return headerPanel;
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

        private string GetSinnerName(int characterId)
        {
            return SinnerNames.TryGetValue(characterId, out var name)
                ? name
                : $"罪人 {characterId}";
        }

        private string GetStarText(int rank)
        {
            return rank switch
            {
                1 => "⭐ (1星)",
                2 => "⭐⭐ (2星)",
                3 => "⭐⭐⭐ (3星)",
                _ => $"{rank}星"
            };
        }

        #endregion
    }
}