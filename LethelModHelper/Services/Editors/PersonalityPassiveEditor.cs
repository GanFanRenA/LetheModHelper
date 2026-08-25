// Services/Editors/PersonalityPassiveEditor.cs
using LethelModHelper.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LethelModHelper.Services.Editors
{
    /// <summary>
    /// PersonalityPassive 编辑器
    /// </summary>
    public class PersonalityPassiveEditor
    {
        private PersonalityEntry _entry;
        private string _filePath;
        private StackPanel _container;
        private Action _onDataChanged;

        public PersonalityPassiveEditor(
            PersonalityEntry entry,
            string filePath,
            Action onDataChanged)
        {
            _entry = entry;
            _filePath = filePath;
            _onDataChanged = onDataChanged;
        }

        /// <summary>
        /// 生成编辑界面
        /// </summary>
        public UIElement Create()
        {
            var border = new Border
            {
                BorderBrush = Brushes.MediumPurple,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 5, 0, 10),
                Padding = new Thickness(10),
                Background = Brushes.Lavender
            };

            _container = new StackPanel();

            // 标题
            _container.Children.Add(new TextBlock
            {
                Text = "📋 人格被动",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.Purple,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // 检查是否有对应的 Passive 数据
            if (_entry.LinkedPassiveEntry == null)
            {
                _container.Children.Add(new TextBlock
                {
                    Text = "⚠️ 未找到对应的 personality_passive 数据，点击下方创建",
                    Foreground = Brushes.Orange,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 5)
                });

                var createBtn = new Button
                {
                    Content = "➕ 创建 PersonalityPassive",
                    Background = Brushes.LightGreen,
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 5, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                createBtn.Click += (s, e) =>
                {
                    CreateNewPassiveEntry();
                    RebuildUI();
                };
                _container.Children.Add(createBtn);
            }
            else
            {
                // 显示编辑区域
                AddPassiveGroupEditor("⚔️ 战斗被动", _entry.LinkedPassiveEntry.battlePassiveList);
                AddPassiveGroupEditor("🛡️ 支援被动", _entry.LinkedPassiveEntry.supporterPassiveList);
            }

            border.Child = _container;
            return border;
        }

        /// <summary>
        /// 重建 UI
        /// </summary>
        private void RebuildUI()
        {
            if (_container == null) return;
            _container.Children.Clear();

            // 重新添加标题
            _container.Children.Add(new TextBlock
            {
                Text = "📋 人格被动",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.Purple,
                Margin = new Thickness(0, 0, 0, 8)
            });

            if (_entry.LinkedPassiveEntry == null)
            {
                _container.Children.Add(new TextBlock
                {
                    Text = "⚠️ 未找到对应的 人格被动 数据，点击下方创建",
                    Foreground = Brushes.Orange,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 5)
                });

                var createBtn = new Button
                {
                    Content = "➕ 创建 人格被动",
                    Background = Brushes.LightGreen,
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 5, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                createBtn.Click += (s, e) =>
                {
                    CreateNewPassiveEntry();
                    RebuildUI();
                };
                _container.Children.Add(createBtn);
            }
            else
            {
                AddPassiveGroupEditor("⚔️ 战斗被动", _entry.LinkedPassiveEntry.battlePassiveList);
                AddPassiveGroupEditor("🛡️ 支援被动", _entry.LinkedPassiveEntry.supporterPassiveList);

                var saveBtn = new Button
                {
                    Content = "💾 保存 PersonalityPassive 修改",
                    Background = Brushes.LightBlue,
                    Padding = new Thickness(15, 5, 15, 5),
                    Margin = new Thickness(0, 10, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    FontWeight = FontWeights.Bold
                };
                saveBtn.Click += (s, e) => SavePassiveData();
                _container.Children.Add(saveBtn);
            }
        }

        /// <summary>
        /// 添加被动组编辑器
        /// </summary>
        private void AddPassiveGroupEditor(string title, List<PassiveGroup> groupList)
        {
            if (groupList == null) return;

            var groupBorder = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(8),
                Background = Brushes.White
            };

            var groupStack = new StackPanel();

            // 标题行
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };

            headerPanel.Children.Add(new TextBlock
            {
                Text = $"{title} ({groupList.Count} 个等级组)",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            });

            // 添加组按钮
            var addGroupBtn = new Button
            {
                Content = "➕ 添加等级组",
                Margin = new Thickness(10, 0, 0, 0),
                Padding = new Thickness(8, 2, 8, 2),
                FontSize = 11,
                Background = Brushes.LightGreen,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            addGroupBtn.Click += (s, e) =>
            {
                groupList.Add(new PassiveGroup
                {
                    level = groupList.Count > 0 ? groupList.Max(g => g.level) + 1 : 1,
                    passiveIDList = new List<int>()
                });
                _onDataChanged?.Invoke();
                RebuildUI();
            };
            headerPanel.Children.Add(addGroupBtn);

            groupStack.Children.Add(headerPanel);

            // 显示每个等级组 - 使用快照
            var groupListSnapshot = groupList.ToList();
            for (int i = 0; i < groupListSnapshot.Count; i++)
            {
                var groupEditor = CreatePassiveGroupEditor(groupList, i);
                groupStack.Children.Add(groupEditor);
            }

            groupBorder.Child = groupStack;
            _container.Children.Add(groupBorder);
        }

        /// <summary>
        /// 创建单个等级组的编辑器
        /// </summary>
        private UIElement CreatePassiveGroupEditor(List<PassiveGroup> groupList, int index)
        {
            // 安全检查
            if (index < 0 || index >= groupList.Count)
            {
                return new TextBlock
                {
                    Text = "⚠️ 无效的索引",
                    Foreground = Brushes.Red,
                    FontSize = 11
                };
            }

            var group = groupList[index];
            var border = new Border
            {
                BorderBrush = Brushes.LightBlue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 3, 0, 3),
                Padding = new Thickness(6),
                Background = Brushes.AliceBlue
            };

            var stack = new StackPanel();

            // 等级行
            var levelPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            levelPanel.Children.Add(new TextBlock
            {
                Text = "人格同步: ",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            });

            var levelBox = new TextBox
            {
                Text = group.level.ToString(),
                Width = 50,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };
            int idx = index;
            levelBox.TextChanged += (s, e) =>
            {
                if (idx >= 0 && idx < groupList.Count)
                {
                    if (int.TryParse(levelBox.Text, out int newLevel))
                    {
                        groupList[idx].level = newLevel;
                        levelBox.Background = Brushes.LightYellow;
                    }
                    else
                    {
                        levelBox.Background = Brushes.LightPink;
                    }
                }
            };
            levelPanel.Children.Add(levelBox);

            // 被动ID列表标签
            levelPanel.Children.Add(new TextBlock
            {
                Text = $"  被动ID ({group.passiveIDList?.Count ?? 0} 个):",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            });

            // 添加被动ID按钮
            var addIdBtn = new Button
            {
                Content = "+",
                Width = 24,
                Height = 24,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.LightGreen,
                ToolTip = "添加被动ID"
            };
            addIdBtn.Click += (s, e) =>
            {
                if (idx >= 0 && idx < groupList.Count)
                {
                    if (groupList[idx].passiveIDList == null)
                        groupList[idx].passiveIDList = new List<int>();
                    groupList[idx].passiveIDList.Add(0);
                    _onDataChanged?.Invoke();
                    RebuildUI();
                }
            };
            levelPanel.Children.Add(addIdBtn);

            // 删除组按钮
            var deleteGroupBtn = new Button
            {
                Content = "🗑️",
                Width = 24,
                Height = 24,
                FontSize = 10,
                Margin = new Thickness(8, 0, 0, 0),
                Background = Brushes.LightPink,
                ToolTip = "删除此等级组"
            };
            deleteGroupBtn.Click += (s, e) =>
            {
                if (idx >= 0 && idx < groupList.Count)
                {
                    if (MessageBox.Show($"确定要删除等级 {groupList[idx].level} 的被动组吗？",
                        "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        groupList.RemoveAt(idx);
                        _onDataChanged?.Invoke();
                        RebuildUI();
                    }
                }
            };
            levelPanel.Children.Add(deleteGroupBtn);

            stack.Children.Add(levelPanel);

            // 被动ID列表
            var idListPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(15, 3, 0, 3)
            };

            if (group.passiveIDList != null)
            {
                // 使用快照
                var idListSnapshot = group.passiveIDList.ToList();
                for (int idIdx = 0; idIdx < idListSnapshot.Count; idIdx++)
                {
                    var idBorder = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Margin = new Thickness(2, 1, 2, 1),
                        Padding = new Thickness(2)
                    };

                    var idPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal
                    };

                    int localIdIdx = idIdx;
                    int localGroupIdx = idx;

                    var idBox = new TextBox
                    {
                        Text = group.passiveIDList[localIdIdx].ToString(),
                        Width = 45,
                        Height = 22,
                        FontSize = 10,
                        TextAlignment = TextAlignment.Center,
                        Background = Brushes.White,
                        BorderThickness = new Thickness(0)
                    };
                    idBox.TextChanged += (s, e) =>
                    {
                        if (localGroupIdx >= 0 && localGroupIdx < groupList.Count &&
                            localIdIdx >= 0 && localIdIdx < groupList[localGroupIdx].passiveIDList.Count)
                        {
                            if (int.TryParse(idBox.Text, out int newId))
                            {
                                groupList[localGroupIdx].passiveIDList[localIdIdx] = newId;
                                idBox.Background = Brushes.LightYellow;
                            }
                            else
                            {
                                idBox.Background = Brushes.LightPink;
                            }
                        }
                    };

                    // 删除单个ID按钮
                    var removeIdBtn = new Button
                    {
                        Content = "✕",
                        Width = 16,
                        Height = 16,
                        FontSize = 8,
                        Padding = new Thickness(0),
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Foreground = Brushes.Red,
                        ToolTip = "删除此被动ID"
                    };
                    removeIdBtn.Click += (s, e) =>
                    {
                        if (localGroupIdx >= 0 && localGroupIdx < groupList.Count &&
                            localIdIdx >= 0 && localIdIdx < groupList[localGroupIdx].passiveIDList.Count)
                        {
                            groupList[localGroupIdx].passiveIDList.RemoveAt(localIdIdx);
                            _onDataChanged?.Invoke();
                            RebuildUI();
                        }
                    };

                    idPanel.Children.Add(idBox);
                    idPanel.Children.Add(removeIdBtn);
                    idBorder.Child = idPanel;
                    idListPanel.Children.Add(idBorder);
                }
            }

            stack.Children.Add(idListPanel);

            border.Child = stack;
            return border;
        }

        /// <summary>
        /// 创建新的 PersonalityPassive 条目
        /// </summary>
        private void CreateNewPassiveEntry()
        {
            var newEntry = new PersonalityPassiveEntry
            {
                personalityID = _entry.id,
                battlePassiveList = new List<PassiveGroup>(),
                supporterPassiveList = new List<PassiveGroup>()
            };

            _entry.LinkedPassiveEntry = newEntry;
            _onDataChanged?.Invoke();
        }

        /// <summary>
        /// 保存数据到文件
        /// </summary>
        private void SavePassiveData()
        {
            if (_entry.LinkedPassiveEntry == null)
            {
                MessageBox.Show("没有数据可保存", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var passiveData = LoadOrCreatePassiveData();

                var existingIndex = passiveData.list.FindIndex(
                    p => p.personalityID == _entry.id);

                if (existingIndex >= 0)
                {
                    passiveData.list[existingIndex] = _entry.LinkedPassiveEntry;
                }
                else
                {
                    passiveData.list.Add(_entry.LinkedPassiveEntry);
                }

                SavePassiveFile(passiveData);

                MessageBox.Show("PersonalityPassive 数据保存成功！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载或创建 PersonalityPassiveData
        /// </summary>
        private PersonalityPassiveData LoadOrCreatePassiveData()
        {
            if (!string.IsNullOrEmpty(_entry.PassiveFilePath) &&
                System.IO.File.Exists(_entry.PassiveFilePath))
            {
                var fileService = new FileService();
                var dataService = new ModDataService(fileService);
                return dataService.Load<PersonalityPassiveData>(_entry.PassiveFilePath);
            }

            var modPath = GetModPath();
            if (!string.IsNullOrEmpty(modPath))
            {
                var passiveFolder = System.IO.Path.Combine(
                    modPath, "custom_limbus_data", "personality-passive");

                if (System.IO.Directory.Exists(passiveFolder))
                {
                    var jsonFiles = System.IO.Directory.GetFiles(passiveFolder, "*.json");
                    if (jsonFiles.Length > 0)
                    {
                        var fileService = new FileService();
                        var dataService = new ModDataService(fileService);
                        return dataService.Load<PersonalityPassiveData>(jsonFiles[0]);
                    }
                }
            }

            return new PersonalityPassiveData
            {
                list = new List<PersonalityPassiveEntry>()
            };
        }

        /// <summary>
        /// 获取 Mod 根路径
        /// </summary>
        private string GetModPath()
        {
            if (string.IsNullOrEmpty(_filePath)) return "";

            var dir = new System.IO.DirectoryInfo(_filePath);
            while (dir != null)
            {
                var customLimbusData = System.IO.Path.Combine(
                    dir.FullName, "custom_limbus_data");
                if (System.IO.Directory.Exists(customLimbusData))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            return "";
        }

        /// <summary>
        /// 保存 PersonalityPassive 文件
        /// </summary>
        private void SavePassiveFile(PersonalityPassiveData data)
        {
            var fileService = new FileService();
            var dataService = new ModDataService(fileService);

            string savePath = _entry.PassiveFilePath;

            if (string.IsNullOrEmpty(savePath))
            {
                var modPath = GetModPath();
                if (string.IsNullOrEmpty(modPath))
                    throw new Exception("无法找到 Mod 根目录");

                var passiveFolder = System.IO.Path.Combine(
                    modPath, "custom_limbus_data", "personality-passive");
                System.IO.Directory.CreateDirectory(passiveFolder);

                savePath = System.IO.Path.Combine(
                    passiveFolder, $"personality_{_entry.id}_passive.json");
            }

            dataService.Save(savePath, data);
            _entry.PassiveFilePath = savePath;
        }
    }
}