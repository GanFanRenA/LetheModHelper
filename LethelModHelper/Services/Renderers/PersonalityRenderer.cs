using LethelModHelper.Core.Models;
using LethelModHelper.Services.Editors;
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
                var expander = new Expander
                {
                    Header = $"ID: {entry.id} | 罪人: {GetSinnerName(entry.characterId)} | 星级: {GetStarText(entry.rank)}",
                    IsExpanded = false,
                    Margin = new Thickness(0, 5, 0, 5),
                    Padding = new Thickness(10)
                };

                var contentStack = new StackPanel();

                // 基础数据编辑
                contentStack.Children.Add(EditorGenerator.GenerateEditor(entry));

                // ===== ✅ 修复：PersonalityPassive 编辑器（可编辑） =====
                var passiveEditor = new PersonalityPassiveEditor(
                    entry,
                    filePath,
                    () => OnPassiveDataChanged(entry)
                );
                // 🔴 关键修复：将编辑器添加到 contentStack
                contentStack.Children.Add(passiveEditor.Create());

                expander.Content = contentStack;
                mainPanel.Children.Add(expander);
            }

            return mainPanel;
        }

        private void OnPassiveDataChanged(PersonalityEntry entry)
        {
            System.Diagnostics.Debug.WriteLine(
                $"PersonalityPassive 数据已变更: Personality {entry.id}");
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