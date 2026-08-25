using LethelModHelper.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LethelModHelper.Services.Editors
{
    public class ListEditor : IFieldEditor
    {
        public bool CanEdit(PropertyInfo property, EditableAttribute attribute)
        {
            return attribute.ControlType == "List" ||
                   (property.PropertyType.IsGenericType &&
                    property.PropertyType.GetGenericTypeDefinition() == typeof(List<>));
        }

        public UIElement Create(
            object dataObject,
            PropertyInfo property,
            EditableAttribute attribute,
            int depth)
        {
            var currentValue = property.GetValue(dataObject);
            var label = string.IsNullOrEmpty(attribute.Label) ? property.Name : attribute.Label;
            var safeValue = currentValue ?? GetDefaultValue(property.PropertyType);

            return CreateListField(label, dataObject, property, attribute, safeValue, depth);
        }

        private UIElement CreateListField(
            string label,
            object dataObject,
            PropertyInfo prop,
            EditableAttribute attr,
            object currentValue,
            int depth)
        {
            var border = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(depth * 15, 5, 0, 5),
                Padding = new Thickness(8)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"📋 {label}:",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var list = prop.GetValue(dataObject) as IList;
            var elementType = prop.PropertyType.GetGenericArguments().FirstOrDefault();

            if (list == null || list.Count == 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "  (空)",
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 2)
                });
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(5, 2, 0, 2)
                    };

                    itemPanel.Children.Add(new TextBlock
                    {
                        Text = $"{i + 1}. ",
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
                        Foreground = Brushes.Gray
                    });

                    // 根据元素类型创建编辑控件
                    if (elementType == typeof(int))
                    {
                        CreateIntListItem(itemPanel, list, i);
                    }
                    else if (elementType == typeof(double))
                    {
                        CreateDoubleListItem(itemPanel, list, i);
                    }
                    else if (elementType == typeof(string))
                    {
                        CreateStringListItem(itemPanel, list, i);
                    }
                    else if (elementType == typeof(ResistEntry))
                    {
                        CreateResistListItem(itemPanel, list, i);
                    }
                    else if (elementType == typeof(SkillSlot))
                    {
                        CreateSkillSlotListItem(itemPanel, list, i);
                    }
                    else if (elementType == typeof(Pattern))
                    {
                        RenderPatternItem(itemPanel, item, list, i, attr, depth, dataObject, stack, prop);
                    }
                    else
                    {
                        // 检查是否是自定义类（需要递归展开）
                        if (elementType != null && elementType.IsClass && elementType != typeof(string) && !elementType.IsValueType && item != null)
                        {
                            var hasEditableProps = elementType.GetProperties()
                                .Any(p => p.GetCustomAttribute<EditableAttribute>() != null);

                            if (hasEditableProps)
                            {
                                var expander = new Expander
                                {
                                    Header = item.ToString(),
                                    IsExpanded = false,
                                    Margin = new Thickness(0, 2, 0, 2)
                                };

                                var nestedEditor = EditorGenerator.GenerateEditor(item, depth + 1);
                                expander.Content = nestedEditor;

                                var itemBorder = new Border
                                {
                                    BorderBrush = Brushes.LightGray,
                                    BorderThickness = new Thickness(1),
                                    CornerRadius = new CornerRadius(4),
                                    Margin = new Thickness(5, 2, 0, 2),
                                    Padding = new Thickness(5),
                                    Child = expander
                                };
                                itemPanel.Children.Add(itemBorder);

                                if (attr.AllowAddRemove)
                                {
                                    var deleteBtn = CreateDeleteButton(list, i, () => RebuildListField(stack, dataObject, prop, attr, depth));
                                    itemPanel.Children.Add(deleteBtn);
                                }

                                stack.Children.Add(itemPanel);
                                continue;
                            }
                        }

                        // 其他复杂对象，只显示 ToString
                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = item?.ToString() ?? "null",
                            FontSize = 11,
                            Foreground = Brushes.Gray
                        });
                        stack.Children.Add(itemPanel);
                    }

                    // 重置按钮
                    var resetBtn = CreateResetButton(list, elementType, i, () => RebuildListField(stack, dataObject, prop, attr, depth));
                    itemPanel.Children.Add(resetBtn);

                    // 删除按钮
                    if (attr.AllowAddRemove)
                    {
                        var deleteBtn = CreateDeleteButton(list, i, () => RebuildListField(stack, dataObject, prop, attr, depth));
                        itemPanel.Children.Add(deleteBtn);
                    }

                    stack.Children.Add(itemPanel);
                }
            }

            // 添加按钮
            if (attr.AllowAddRemove)
            {
                var addPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5, 5, 0, 5)
                };
                var addBtn = new Button
                {
                    Content = "➕ 添加",
                    Width = 80,
                    Height = 28,
                    FontSize = 11,
                    Padding = new Thickness(5, 2, 5, 2),
                    Background = Brushes.LightGreen,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1)
                };
                addBtn.Click += (s, e) =>
                {
                    if (elementType != null)
                    {
                        var newItem = CreateDefaultListItem(elementType);
                        if (newItem == null)
                        {
                            MessageBox.Show($"无法为类型 {elementType.Name} 创建默认项，请确认该类型有无参构造函数。", "添加失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        list.Add(newItem);
                        RebuildListField(stack, dataObject, prop, attr, depth);
                    }
                };
                addPanel.Children.Add(addBtn);
                stack.Children.Add(addPanel);
            }

            // 提示信息
            var hintText = attr.AllowAddRemove
                ? "💡 点击 ✕ 删除，点击 ➕ 添加"
                : "💡 修改数值即可更新";
            stack.Children.Add(new TextBlock
            {
                Text = hintText,
                Foreground = Brushes.Gray,
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 0)
            });

            border.Child = stack;
            return border;
        }

        #region 辅助方法

        private static object GetDefaultValue(Type type)
        {
            if (type == typeof(string)) return "";
            if (type == typeof(int)) return 0;
            if (type == typeof(double)) return 0.0;
            if (type == typeof(bool)) return false;
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null!;
        }

        private static object? CreateDefaultListItem(Type type)
        {
            if (type == typeof(string)) return "";
            if (type.IsValueType) return Activator.CreateInstance(type);
            if (type.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        private static void CreateIntListItem(StackPanel panel, IList list, int index)
        {
            var box = new TextBox
            {
                Text = list[index]?.ToString() ?? "0",
                Width = 60,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                TextAlignment = TextAlignment.Center
            };
            int idx = index;
            box.TextChanged += (s, e) =>
            {
                if (int.TryParse(box.Text, out int newValue))
                {
                    list[idx] = newValue;
                    box.Background = Brushes.LightYellow;
                }
                else
                {
                    box.Background = Brushes.LightPink;
                }
            };
            panel.Children.Add(box);
        }

        private static void CreateDoubleListItem(StackPanel panel, IList list, int index)
        {
            var box = new TextBox
            {
                Text = list[index]?.ToString() ?? "0",
                Width = 60,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                TextAlignment = TextAlignment.Center
            };
            int idx = index;
            box.TextChanged += (s, e) =>
            {
                if (double.TryParse(box.Text, out double newValue))
                {
                    list[idx] = newValue;
                    box.Background = Brushes.LightYellow;
                }
                else
                {
                    box.Background = Brushes.LightPink;
                }
            };
            panel.Children.Add(box);
        }

        private static void CreateStringListItem(StackPanel panel, IList list, int index)
        {
            var box = new TextBox
            {
                Text = list[index]?.ToString() ?? "",
                Width = 150,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };
            int idx = index;
            box.TextChanged += (s, e) =>
            {
                list[idx] = box.Text;
                box.Background = Brushes.LightYellow;
            };
            panel.Children.Add(box);
        }

        private static void CreateResistListItem(StackPanel panel, IList list, int index)
        {
            var resist = list[index] as ResistEntry;
            if (resist == null) return;

            panel.Children.Add(new TextBlock
            {
                Text = $"{resist.type}: ",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = Brushes.DarkBlue
            });

            var valueBox = new TextBox
            {
                Text = resist.value.ToString(),
                Width = 50,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                TextAlignment = TextAlignment.Center
            };
            valueBox.TextChanged += (s, e) =>
            {
                if (double.TryParse(valueBox.Text, out double newValue))
                {
                    resist.value = newValue;
                    valueBox.Background = Brushes.LightYellow;
                }
                else
                {
                    valueBox.Background = Brushes.LightPink;
                }
            };
            panel.Children.Add(valueBox);
        }

        private static void CreateSkillSlotListItem(StackPanel panel, IList list, int index)
        {
            var slot = list[index] as SkillSlot;
            if (slot == null) return;

            panel.Children.Add(new TextBlock
            {
                Text = $"[{index}]: ",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            });

            var idBox = new TextBox
            {
                Text = slot.skillId.ToString(),
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            int idx = index;
            idBox.TextChanged += (s, e) =>
            {
                if (int.TryParse(idBox.Text, out int newId))
                {
                    var currentSlot = list[idx] as SkillSlot;
                    if (currentSlot != null)
                    {
                        currentSlot.skillId = newId;
                        idBox.Background = Brushes.LightYellow;
                    }
                }
                else
                {
                    idBox.Background = Brushes.LightPink;
                }
            };
            panel.Children.Add(idBox);

            panel.Children.Add(new TextBlock
            {
                Text = " × ",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            });

            var numBox = new TextBox
            {
                Text = slot.number.ToString(),
                Width = 60,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            int numIdx = index;
            numBox.TextChanged += (s, e) =>
            {
                if (int.TryParse(numBox.Text, out int newNum))
                {
                    var currentSlot = list[numIdx] as SkillSlot;
                    if (currentSlot != null)
                    {
                        currentSlot.number = newNum;
                        numBox.Background = Brushes.LightYellow;
                    }
                }
                else
                {
                    numBox.Background = Brushes.LightPink;
                }
            };
            panel.Children.Add(numBox);
        }

        private static Button CreateResetButton(IList list, Type? elementType, int index, Action rebuildAction)
        {
            var resetBtn = new Button
            {
                Content = "↩️",
                Width = 24,
                Height = 24,
                FontSize = 10,
                Padding = new Thickness(2),
                ToolTip = "重置此项",
                Margin = new Thickness(5, 0, 0, 0)
            };
            int resetIdx = index;
            resetBtn.Click += (s, e) =>
            {
                if (elementType == typeof(int))
                {
                    list[resetIdx] = 0;
                }
                else if (elementType == typeof(double))
                {
                    list[resetIdx] = 0.0;
                }
                else if (elementType == typeof(string))
                {
                    list[resetIdx] = "";
                }
                else if (elementType == typeof(ResistEntry))
                {
                    var resist = list[resetIdx] as ResistEntry;
                    if (resist != null)
                    {
                        resist.value = 1;
                    }
                }
                rebuildAction();
            };
            return resetBtn;
        }

        private static Button CreateDeleteButton(IList list, int index, Action rebuildAction)
        {
            var deleteBtn = new Button
            {
                Content = "✕",
                Width = 24,
                Height = 24,
                FontSize = 10,
                Padding = new Thickness(2),
                ToolTip = "删除此项",
                Margin = new Thickness(2, 0, 0, 0),
                Background = Brushes.LightPink,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            int deleteIdx = index;
            deleteBtn.Click += (s, e) =>
            {
                if (list.Count > deleteIdx)
                {
                    list.RemoveAt(deleteIdx);
                    rebuildAction();
                }
            };
            return deleteBtn;
        }

        private void RebuildListField(StackPanel parentStack, object dataObject, PropertyInfo prop,
            EditableAttribute attr, int depth)
        {
            // 清除旧的列表项（保留标题）
            var toRemove = new List<UIElement>();
            bool afterTitle = false;
            foreach (var child in parentStack.Children)
            {
                if (!afterTitle && child is TextBlock tb && tb.Text?.Contains("📋") == true)
                {
                    afterTitle = true;
                    continue;
                }
                if (afterTitle)
                {
                    toRemove.Add((UIElement)child);
                }
            }
            foreach (var item in toRemove)
            {
                parentStack.Children.Remove(item);
            }

            // 重新生成列表
            var list = prop.GetValue(dataObject) as IList;
            var elementType = prop.PropertyType.GetGenericArguments().FirstOrDefault();

            if (list == null || list.Count == 0)
            {
                parentStack.Children.Add(new TextBlock
                {
                    Text = "  (空)",
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 2)
                });
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(5, 2, 0, 2)
                    };

                    itemPanel.Children.Add(new TextBlock
                    {
                        Text = $"{i + 1}. ",
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
                        Foreground = Brushes.Gray
                    });

                    if (elementType == typeof(int))
                    {
                        CreateIntListItem(itemPanel, list, i);
                    }
                    else if (elementType == typeof(double))
                    {
                        CreateDoubleListItem(itemPanel, list, i);
                    }
                    else if (elementType == typeof(string))
                    {
                        CreateStringListItem(itemPanel, list, i);
                    }
                    else if (elementType == typeof(ResistEntry))
                    {
                        CreateResistListItem(itemPanel, list, i);
                    }
                    else if (elementType == typeof(SkillSlot))
                    {
                        CreateSkillSlotListItem(itemPanel, list, i);
                    }
                    else
                    {
                        // 其他类型简单显示
                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = item?.ToString() ?? "null",
                            FontSize = 11,
                            Foreground = Brushes.Gray
                        });
                    }

                    var resetBtn = CreateResetButton(list, elementType, i, () => RebuildListField(parentStack, dataObject, prop, attr, depth));
                    itemPanel.Children.Add(resetBtn);

                    if (attr.AllowAddRemove)
                    {
                        var deleteBtn = CreateDeleteButton(list, i, () => RebuildListField(parentStack, dataObject, prop, attr, depth));
                        itemPanel.Children.Add(deleteBtn);
                    }

                    parentStack.Children.Add(itemPanel);
                }
            }

            // 添加按钮
            if (attr.AllowAddRemove && elementType != null)
            {
                var addPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5, 5, 0, 5)
                };
                var addBtn = new Button
                {
                    Content = "➕ 添加",
                    Width = 80,
                    Height = 28,
                    FontSize = 11,
                    Padding = new Thickness(5, 2, 5, 2),
                    Background = Brushes.LightGreen,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1)
                };
                addBtn.Click += (s, e) =>
                {
                    var newItem = CreateDefaultListItem(elementType);
                    if (newItem == null)
                    {
                        MessageBox.Show($"无法为类型 {elementType.Name} 创建默认项，请确认该类型有无参构造函数。", "添加失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    list.Add(newItem);
                    RebuildListField(parentStack, dataObject, prop, attr, depth);
                };
                addPanel.Children.Add(addBtn);
                parentStack.Children.Add(addPanel);
            }

            parentStack.Children.Add(new TextBlock
            {
                Text = attr.AllowAddRemove ? "💡 点击 ✕ 删除，点击 ➕ 添加" : "💡 修改数值即可更新",
                Foreground = Brushes.Gray,
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        #endregion
        #region Pattern 渲染

        private void RenderPatternItem(StackPanel itemPanel, object item, IList list,
            int index, EditableAttribute attr, int depth, object dataObject,
            StackPanel parentStack, PropertyInfo prop, int? expandedPatternIndex = null)
        {
            var pattern = item as Pattern;
            if (pattern == null) return;

            // 确保数据结构完整
            EnsurePatternStructure(pattern);

            // 获取所有技能ID
            var skillIds = GetSkillIdsFromPattern(pattern);

            // 显示为 "模式 1"、"模式 2"
            int patternNumber = index + 1;
            itemPanel.Children.Add(new TextBlock
            {
                Text = $"📋 模式 {patternNumber} (共 {skillIds.Count} 个技能)",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.DarkBlue,
                FontWeight = FontWeights.SemiBold
            });

            var isInitiallyExpanded = expandedPatternIndex.HasValue && expandedPatternIndex.Value == index;

            var expander = new Expander
            {
                Header = $"▼ 点击展开编辑 模式 {patternNumber}",
                IsExpanded = isInitiallyExpanded,
                Margin = new Thickness(0, 2, 0, 2),
                Foreground = Brushes.DarkGray,
                FontSize = 11
            };

            var skillEditorPanel = new StackPanel { Margin = new Thickness(15, 5, 0, 5) };

            skillEditorPanel.Children.Add(new TextBlock
            {
                Text = $"当前技能数: {skillIds.Count} 个",
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // 技能编辑行
            for (int sIdx = 0; sIdx < skillIds.Count; sIdx++)
            {
                int localIdx = sIdx;
                var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };

                rowPanel.Children.Add(new TextBlock
                {
                    Text = $"{sIdx + 1}. ",
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0),
                    Foreground = Brushes.Gray
                });

                var skillBox = new TextBox
                {
                    Text = skillIds[sIdx].ToString(),
                    Width = 80,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    TextAlignment = TextAlignment.Center
                };

                skillBox.TextChanged += (s, e) =>
                {
                    if (int.TryParse(skillBox.Text, out int newId))
                    {
                        UpdateSkillIdInPattern(pattern, localIdx, newId);
                        skillBox.Background = Brushes.LightYellow;
                    }
                    else
                    {
                        skillBox.Background = Brushes.LightPink;
                    }
                };
                rowPanel.Children.Add(skillBox);
                skillEditorPanel.Children.Add(rowPanel);
            }

            // 添加/删除按钮
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 3) };

            var addBtn = new Button
            {
                Content = "➕ 添加技能",
                Width = 90,
                Height = 26,
                FontSize = 11,
                Margin = new Thickness(0, 0, 8, 0),
                Background = Brushes.LightGreen,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            addBtn.Click += (s, e) =>
            {
                var currentPattern = list[index] as Pattern;
                if (currentPattern != null)
                {
                    AddSkillToPattern(currentPattern);
                    RebuildListFieldWithList(parentStack, dataObject, prop, attr, depth, list, index);
                }
            };
            btnPanel.Children.Add(addBtn);

            var removeBtn = new Button
            {
                Content = "➖ 删除最后一个",
                Width = 100,
                Height = 26,
                FontSize = 11,
                Background = Brushes.LightPink,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            removeBtn.Click += (s, e) =>
            {
                var currentPattern = list[index] as Pattern;
                if (currentPattern != null)
                {
                    var currentIds = GetSkillIdsFromPattern(currentPattern);
                    if (currentIds.Count <= 1)
                    {
                        MessageBox.Show("最少保留1个技能，无法继续删除！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    RemoveLastSkillFromPattern(currentPattern);
                    RebuildListFieldWithList(parentStack, dataObject, prop, attr, depth, list, index);
                }
            };
            btnPanel.Children.Add(removeBtn);
            skillEditorPanel.Children.Add(btnPanel);

            expander.Content = skillEditorPanel;
            itemPanel.Children.Add(expander);
        }

        private void EnsurePatternStructure(Pattern pattern)
        {
            if (pattern == null) return;

            if (pattern.slotList == null)
            {
                pattern.slotList = new List<Slot>();
            }

            var nonEmptySlots = new List<Slot>();
            foreach (var slot in pattern.slotList)
            {
                if (slot == null) continue;

                if (slot.skillParentList == null)
                {
                    slot.skillParentList = new List<SkillParent>();
                }

                var nonEmptyParents = new List<SkillParent>();
                foreach (var parent in slot.skillParentList)
                {
                    if (parent == null) continue;

                    if (parent.skillChildList == null)
                    {
                        parent.skillChildList = new List<SkillChild>();
                    }

                    if (parent.skillChildList.Count > 0)
                    {
                        nonEmptyParents.Add(parent);
                    }
                }
                slot.skillParentList = nonEmptyParents;

                if (slot.skillParentList.Count > 0)
                {
                    nonEmptySlots.Add(slot);
                }
            }

            pattern.slotList = nonEmptySlots;

            if (pattern.slotList.Count == 0)
            {
                pattern.slotList.Add(new Slot
                {
                    skillParentList = new List<SkillParent>
            {
                new SkillParent
                {
                    skillChildList = new List<SkillChild>()
                }
            }
                });
            }
        }

        private List<int> GetSkillIdsFromPattern(Pattern pattern)
        {
            var result = new List<int>();
            if (pattern?.slotList == null) return result;

            foreach (var slot in pattern.slotList)
            {
                if (slot?.skillParentList == null) continue;
                foreach (var parent in slot.skillParentList)
                {
                    if (parent?.skillChildList == null) continue;
                    foreach (var child in parent.skillChildList)
                    {
                        result.Add(child.skillID);
                    }
                }
            }
            return result;
        }

        private void UpdateSkillIdInPattern(Pattern pattern, int index, int newId)
        {
            if (pattern?.slotList == null) return;

            int counter = 0;
            foreach (var slot in pattern.slotList)
            {
                if (slot?.skillParentList == null) continue;
                foreach (var parent in slot.skillParentList)
                {
                    if (parent?.skillChildList == null) continue;
                    foreach (var child in parent.skillChildList)
                    {
                        if (counter == index)
                        {
                            child.skillID = newId;
                            return;
                        }
                        counter++;
                    }
                }
            }
        }

        private void AddSkillToPattern(Pattern pattern)
        {
            if (pattern == null) return;

            if (pattern.slotList == null)
            {
                pattern.slotList = new List<Slot>();
            }

            var newSlot = new Slot
            {
                skillParentList = new List<SkillParent>
        {
            new SkillParent
            {
                chance = 1,
                skillChildList = new List<SkillChild>
                {
                    new SkillChild { skillID = 0, chance = 1 }
                }
            }
        }
            };

            pattern.slotList.Add(newSlot);
        }

        private void RemoveLastSkillFromPattern(Pattern pattern)
        {
            if (pattern?.slotList == null || pattern.slotList.Count == 0) return;

            for (int slotIdx = pattern.slotList.Count - 1; slotIdx >= 0; slotIdx--)
            {
                var slot = pattern.slotList[slotIdx];
                if (slot?.skillParentList == null || slot.skillParentList.Count == 0) continue;

                for (int parentIdx = slot.skillParentList.Count - 1; parentIdx >= 0; parentIdx--)
                {
                    var parent = slot.skillParentList[parentIdx];
                    if (parent?.skillChildList == null || parent.skillChildList.Count == 0) continue;

                    parent.skillChildList.RemoveAt(parent.skillChildList.Count - 1);

                    if (parent.skillChildList.Count == 0)
                    {
                        slot.skillParentList.RemoveAt(parentIdx);
                    }

                    if (slot.skillParentList.Count == 0)
                    {
                        pattern.slotList.RemoveAt(slotIdx);
                    }

                    return;
                }
            }
        }

        private void RebuildListFieldWithList(StackPanel parentStack, object dataObject,
            PropertyInfo prop, EditableAttribute attr, int depth, IList existingList, int? expandedPatternIndex = null)
        {
            // 清除旧的列表项（保留标题）
            var toRemove = new List<UIElement>();
            bool afterTitle = false;
            foreach (var child in parentStack.Children)
            {
                if (!afterTitle && child is TextBlock tb && tb.Text?.Contains("📋") == true)
                {
                    afterTitle = true;
                    continue;
                }
                if (afterTitle)
                {
                    toRemove.Add((UIElement)child);
                }
            }
            foreach (var item in toRemove)
            {
                parentStack.Children.Remove(item);
            }

            var list = existingList;
            var elementType = prop.PropertyType.GetGenericArguments().FirstOrDefault();

            if (list == null || list.Count == 0)
            {
                parentStack.Children.Add(new TextBlock
                {
                    Text = "  (空)",
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 2)
                });
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    var itemPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(5, 2, 0, 2)
                    };

                    itemPanel.Children.Add(new TextBlock
                    {
                        Text = $"{i + 1}. ",
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
                        Foreground = Brushes.Gray
                    });

                    if (elementType == typeof(Pattern))
                    {
                        RenderPatternItem(itemPanel, item, list, i, attr, depth, dataObject, parentStack, prop, expandedPatternIndex);
                    }
                    else
                    {
                        // 其他类型简单显示
                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = item?.ToString() ?? "null",
                            FontSize = 11,
                            Foreground = Brushes.Gray
                        });
                    }

                    if (attr.AllowAddRemove)
                    {
                        var deleteBtn = CreateDeleteButton(list, i, () => RebuildListFieldWithList(parentStack, dataObject, prop, attr, depth, list));
                        itemPanel.Children.Add(deleteBtn);
                    }

                    parentStack.Children.Add(itemPanel);
                }
            }

            // 添加按钮
            if (attr.AllowAddRemove && elementType != null)
            {
                var addPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 5, 0, 5) };
                var addBtn = new Button
                {
                    Content = "➕ 添加",
                    Width = 80,
                    Height = 28,
                    FontSize = 11,
                    Padding = new Thickness(5, 2, 5, 2),
                    Background = Brushes.LightGreen,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1)
                };
                addBtn.Click += (s, e) =>
                {
                    var newItem = CreateDefaultListItem(elementType);
                    if (newItem == null)
                    {
                        MessageBox.Show($"无法为类型 {elementType.Name} 创建默认项，请确认该类型有无参构造函数。", "添加失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    list.Add(newItem);
                    RebuildListFieldWithList(parentStack, dataObject, prop, attr, depth, list);
                };
                addPanel.Children.Add(addBtn);
                parentStack.Children.Add(addPanel);
            }

            parentStack.Children.Add(new TextBlock
            {
                Text = attr.AllowAddRemove ? "💡 点击 ✕ 删除，点击 ➕ 添加" : "💡 修改数值即可更新",
                Foreground = Brushes.Gray,
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        #endregion
    }
}