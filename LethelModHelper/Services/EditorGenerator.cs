using LethelModHelper.Core.Models;
using LethelModHelper.Services.Editors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LethelModHelper.Services
{
    public static class EditorGenerator
    {
        /// <summary>
        /// 自动为对象生成编辑界面
        /// </summary>
        public static StackPanel GenerateEditor(object dataObject, int depth = 0)
        {
            var panel = new StackPanel();
            var type = dataObject.GetType();
            var properties = type.GetProperties();

            var editableProps = new List<(PropertyInfo Prop, EditableAttribute Attr)>();
            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<EditableAttribute>();
                if (attr != null)
                {
                    editableProps.Add((prop, attr));
                }
            }

            foreach (var (prop, attr) in editableProps.OrderBy(x => x.Attr.Order))
            {
                // 检查是否是脚本字段
                if (prop.PropertyType == typeof(string) && ScriptFieldCache.HasScript(dataObject, prop.Name))
                {
                    var parsed = ScriptFieldCache.Get(dataObject, prop.Name);
                    panel.Children.Add(CreateScriptDisplay(prop.Name, parsed));
                }
                else
                {
                    var control = CreateControl(dataObject, prop, attr, depth);
                    if (control != null)
                    {
                        panel.Children.Add(control);
                    }
                }
            }

            return panel;
        }

        private static UIElement CreateControl(object dataObject, PropertyInfo prop, EditableAttribute attr, int depth = 0)
        {
            var currentValue = prop.GetValue(dataObject);
            var label = string.IsNullOrEmpty(attr.Label) ? prop.Name : attr.Label;
            var safeValue = currentValue ?? GetDefaultValue(prop.PropertyType);

            // ===== 处理 SpeedRange 特殊类型 =====
            if (attr.ControlType == "SpeedRange")
            {
                var editor = new SpeedRangeEditor();
                return editor.Create(dataObject, prop, attr, depth);
            }

            // ===== 处理 List 类型 =====
            if (prop.PropertyType.IsGenericType &&
                prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return CreateListField(label, dataObject, prop, attr, safeValue, depth);
            }

            // ===== 处理嵌套对象 (Nested) =====
            if (attr.ControlType == "Nested" ||
                (prop.PropertyType.IsClass && prop.PropertyType != typeof(string) && !prop.PropertyType.IsValueType))
            {
                // 创建嵌套面板
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
                    Text = $"📁 {label}:",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 5)
                });

                // 递归生成嵌套对象的编辑界面
                var nestedEditor = GenerateEditor(safeValue, depth + 1);
                stack.Children.Add(nestedEditor);

                border.Child = stack;
                return border;
            }

            // ===== 其他控件类型 =====
            switch (attr.ControlType.ToLower())
            {
                case "numeric":
                    return CreateNumericField(label, dataObject, prop, attr, safeValue);
                case "boolean":
                    return CreateBooleanField(label, dataObject, prop, safeValue);
                case "dropdown":
                    return CreateDropdownField(label, dataObject, prop, attr, safeValue);
                case "text":
                default:
                    return CreateTextField(label, dataObject, prop, safeValue);
            }
        }

        /// <summary>
        /// 获取类型的默认值
        /// </summary>
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

        /// <summary>
        /// 创建列表编辑字段
        /// </summary>
        /// <summary>
        /// 创建列表编辑字段（支持添加/删除）
        /// </summary>
        /// <summary>
        /// 创建列表编辑字段（支持添加/删除）
        /// </summary>
        /// <summary>
        /// 创建列表编辑字段
        /// </summary>
        private static UIElement CreateListField(string label, object dataObject, PropertyInfo prop,
            EditableAttribute attr, object currentValue, int depth)
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

            var list = prop.GetValue(dataObject) as System.Collections.IList;
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
                    var itemPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 2, 0, 2) };

                    // ===== 显示索引 =====
                    itemPanel.Children.Add(new TextBlock
                    {
                        Text = $"{i + 1}. ",
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
                        Foreground = Brushes.Gray
                    });

                    // ===== 根据元素类型创建编辑控件 =====
                    if (elementType == typeof(int))
                    {
                        var box = new TextBox
                        {
                            Text = item?.ToString() ?? "0",
                            Width = 60,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.White,
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(1),
                            TextAlignment = TextAlignment.Center
                        };
                        int idx = i;
                        var listRef = list;
                        box.TextChanged += (s, e) =>
                        {
                            if (int.TryParse(box.Text, out int newValue))
                            {
                                listRef[idx] = newValue;
                                box.Background = Brushes.LightYellow;
                            }
                            else
                            {
                                box.Background = Brushes.LightPink;
                            }
                        };
                        itemPanel.Children.Add(box);
                    }
                    else if (elementType == typeof(double))
                    {
                        var box = new TextBox
                        {
                            Text = item?.ToString() ?? "0",
                            Width = 60,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.White,
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(1),
                            TextAlignment = TextAlignment.Center
                        };
                        int idx = i;
                        var listRef = list;
                        box.TextChanged += (s, e) =>
                        {
                            if (double.TryParse(box.Text, out double newValue))
                            {
                                listRef[idx] = newValue;
                                box.Background = Brushes.LightYellow;
                            }
                            else
                            {
                                box.Background = Brushes.LightPink;
                            }
                        };
                        itemPanel.Children.Add(box);
                    }
                    else if (elementType == typeof(string))
                    {
                        var box = new TextBox
                        {
                            Text = item?.ToString() ?? "",
                            Width = 150,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.White,
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(1)
                        };
                        int idx = i;
                        var listRef = list;
                        box.TextChanged += (s, e) =>
                        {
                            listRef[idx] = box.Text;
                            box.Background = Brushes.LightYellow;
                        };
                        itemPanel.Children.Add(box);
                    }
                    else if (elementType == typeof(ResistEntry))
                    {
                        var resist = item as ResistEntry;
                        if (resist == null) continue;

                        itemPanel.Children.Add(new TextBlock
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
                        itemPanel.Children.Add(valueBox);
                    }
                    else if (elementType == typeof(SkillSlot))
                    {
                        var slot = item as SkillSlot;
                        if (slot == null) continue;

                        // 显示索引
                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = $"[{i}]: ",
                            FontSize = 11,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        });

                        // skillId 输入框
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
                        int idx = i;
                        var listRef = list;
                        idBox.TextChanged += (s, e) =>
                        {
                            if (int.TryParse(idBox.Text, out int newId))
                            {
                                var currentSlot = listRef[idx] as SkillSlot;
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
                        itemPanel.Children.Add(idBox);

                        // "×" 分隔符
                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = " × ",
                            FontSize = 11,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        });

                        // number 输入框
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
                        int numIdx = i;
                        numBox.TextChanged += (s, e) =>
                        {
                            if (int.TryParse(numBox.Text, out int newNum))
                            {
                                var currentSlot = listRef[numIdx] as SkillSlot;
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
                        itemPanel.Children.Add(numBox);
                    }// ===== 处理 Pattern 类型 =====
                    else if (elementType == typeof(Pattern))
                    {
                        RenderPatternItem(itemPanel, item, list, i, attr, depth, dataObject, stack, prop);
                    }
                    else
                    {
                        // ===== 检查是否是自定义类（需要递归展开） =====
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

                                var nestedEditor = GenerateEditor(item, depth + 1);
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
                                    var deleteBtn = new Button
                                    {
                                        Content = "✕",
                                        Width = 24,
                                        Height = 24,
                                        FontSize = 10,
                                        Padding = new Thickness(2),
                                        ToolTip = "删除此项",
                                        Margin = new Thickness(5, 0, 0, 0),
                                        Background = Brushes.LightPink,
                                        BorderBrush = Brushes.Gray,
                                        BorderThickness = new Thickness(1)
                                    };
                                    int deleteIdx = i;
                                    deleteBtn.Click += (s, e) =>
                                    {
                                        if (list.Count > deleteIdx)
                                        {
                                            list.RemoveAt(deleteIdx);
                                            RebuildListField(stack, dataObject, prop, attr, depth);
                                        }
                                    };
                                    itemPanel.Children.Add(deleteBtn);
                                }

                                stack.Children.Add(itemPanel);
                                continue;
                            }
                        }
                        // =============================================

                        // 其他复杂对象，只显示 ToString
                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = item?.ToString() ?? "null",
                            FontSize = 11,
                            Foreground = Brushes.Gray
                        });
                        stack.Children.Add(itemPanel);
                    }

                    // ===== 重置按钮 =====
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
                    int resetIdx = i;
                    var resetList = list;
                    resetBtn.Click += (s, e) =>
                    {
                        if (elementType == typeof(int))
                        {
                            resetList[resetIdx] = 0;
                        }
                        else if (elementType == typeof(double))
                        {
                            resetList[resetIdx] = 0.0;
                        }
                        else if (elementType == typeof(string))
                        {
                            resetList[resetIdx] = "";
                        }
                        else if (elementType == typeof(ResistEntry))
                        {
                            var resist = resetList[resetIdx] as ResistEntry;
                            if (resist != null)
                            {
                                resist.value = 1;
                            }
                        }
                        // 刷新显示
                        RebuildListField(stack, dataObject, prop, attr, depth);
                    };
                    itemPanel.Children.Add(resetBtn);

                    // ===== 删除按钮（仅当 AllowAddRemove = true） =====
                    if (attr.AllowAddRemove)
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
                        int deleteIdx = i;
                        deleteBtn.Click += (s, e) =>
                        {
                            if (list.Count > deleteIdx)
                            {
                                list.RemoveAt(deleteIdx);
                                RebuildListField(stack, dataObject, prop, attr, depth);
                            }
                        };
                        itemPanel.Children.Add(deleteBtn);
                    }

                    stack.Children.Add(itemPanel);
                }
            }

            // ===== 添加按钮（仅当 AllowAddRemove = true 且列表不为空或允许空列表） =====
            if (attr.AllowAddRemove)
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

            // ===== 提示信息 =====
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

        /// <summary>
        /// 编辑 ResistEntry（弹出简单编辑）
        /// </summary>
        private static void EditResistEntry(ResistEntry resist, int index, StackPanel parentPanel)
        {
            // 创建临时编辑面板
            var editPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5, 2, 0, 2) };

            // 类型下拉框
            var typeBox = new ComboBox
            {
                Width = 100,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            typeBox.Items.Add("SLASH");
            typeBox.Items.Add("PENETRATE");
            typeBox.Items.Add("HIT");
            typeBox.SelectedItem = resist.type;

            // 倍率输入框
            var valueBox = new TextBox
            {
                Text = resist.value.ToString(),
                Width = 50,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                TextAlignment = TextAlignment.Center
            };

            // 保存按钮
            var saveBtn = new Button
            {
                Content = "✅",
                Width = 28,
                Height = 24,
                FontSize = 12,
                Padding = new Thickness(2),
                ToolTip = "保存修改",
                Background = Brushes.LightGreen
            };
            saveBtn.Click += (s, e) =>
            {
                if (typeBox.SelectedItem != null)
                {
                    resist.type = typeBox.SelectedItem.ToString();
                }
                if (double.TryParse(valueBox.Text, out double newValue))
                {
                    resist.value = newValue;
                }
                // 刷新显示
                RefreshResistDisplay(parentPanel, resist, index);
            };

            // 取消按钮
            var cancelBtn = new Button
            {
                Content = "✕",
                Width = 28,
                Height = 24,
                FontSize = 12,
                Padding = new Thickness(2),
                ToolTip = "取消",
                Background = Brushes.LightPink
            };
            cancelBtn.Click += (s, e) =>
            {
                // 恢复原来的显示
                RefreshResistDisplay(parentPanel, resist, index);
            };

            editPanel.Children.Add(typeBox);
            editPanel.Children.Add(valueBox);
            editPanel.Children.Add(saveBtn);
            editPanel.Children.Add(cancelBtn);

            // 替换原来的内容
            parentPanel.Children.Clear();
            parentPanel.Children.Add(editPanel);
        }

        /// <summary>
        /// 刷新 ResistEntry 显示
        /// </summary>
        private static void RefreshResistDisplay(StackPanel parentPanel, ResistEntry resist, int index)
        {
            parentPanel.Children.Clear();

            parentPanel.Children.Add(new TextBlock
            {
                Text = $"[{index}]: ",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            });

            parentPanel.Children.Add(new TextBlock
            {
                Text = $"{resist.type}: {resist.value}x",
                FontSize = 11,
                Foreground = Brushes.DarkBlue,
                Margin = new Thickness(0, 0, 10, 0)
            });

            var editBtn = new Button
            {
                Content = "✏️",
                Width = 28,
                Height = 24,
                FontSize = 10,
                Padding = new Thickness(2),
                ToolTip = "编辑此项",
                Margin = new Thickness(0, 0, 5, 0)
            };
            editBtn.Click += (s, e) =>
            {
                EditResistEntry(resist, index, parentPanel);
            };
            parentPanel.Children.Add(editBtn);
        }

        private static UIElement CreateNumericField(string label, object dataObject, PropertyInfo prop,
            EditableAttribute attr, object currentValue)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

            panel.Children.Add(CreateLabel($"{label}: "));

            var displayValue = currentValue?.ToString() ?? "0";
            var isDouble = prop.PropertyType == typeof(double);

            var box = new TextBox
            {
                Text = displayValue,
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

            box.TextChanged += (s, e) =>
            {
                if (isDouble)
                {
                    if (double.TryParse(box.Text, out double newValue))
                    {
                        if (newValue >= attr.Min && newValue <= attr.Max)
                        {
                            prop.SetValue(dataObject, newValue);
                            box.Background = Brushes.LightYellow;
                        }
                    }
                    else
                    {
                        box.Background = Brushes.LightPink;
                    }
                }
                else
                {
                    if (int.TryParse(box.Text, out int newValue))
                    {
                        if (newValue >= attr.Min && newValue <= attr.Max)
                        {
                            prop.SetValue(dataObject, newValue);
                            box.Background = Brushes.LightYellow;
                        }
                    }
                    else
                    {
                        box.Background = Brushes.LightPink;
                    }
                }
            };
            panel.Children.Add(box);

            // 重置按钮
            var resetBtn = new Button
            {
                Content = "↩️",
                Width = 28,
                Height = 28,
                Margin = new Thickness(5, 0, 0, 0),
                ToolTip = "重置",
                FontSize = 12,
                Padding = new Thickness(2)
            };
            resetBtn.Click += (s, e) =>
            {
                var originalValue = prop.GetValue(dataObject);
                box.Text = originalValue?.ToString() ?? "0";
                box.Background = Brushes.White;
            };
            panel.Children.Add(resetBtn);

            return panel;
        }

        private static UIElement CreateBooleanField(string label, object dataObject, PropertyInfo prop, object currentValue)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

            panel.Children.Add(CreateLabel($"{label}: "));

            var checkBox = new CheckBox
            {
                IsChecked = (bool?)currentValue ?? false,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };
            checkBox.Checked += (s, e) => prop.SetValue(dataObject, true);
            checkBox.Unchecked += (s, e) => prop.SetValue(dataObject, false);
            panel.Children.Add(checkBox);

            return panel;
        }

        private static UIElement CreateDropdownField(string label, object dataObject, PropertyInfo prop,
            EditableAttribute attr, object currentValue)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

            panel.Children.Add(CreateLabel($"{label}: "));

            var comboBox = new ComboBox
            {
                Width = 150,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            var options = new List<string>();
            if (!string.IsNullOrEmpty(attr.Options))
            {
                options = attr.Options.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(o => o.Trim())
                                      .ToList();
            }

            comboBox.Items.Add("无");

            foreach (var option in options)
            {
                comboBox.Items.Add(option);
            }

            // 设置选中项
            if (currentValue != null)
            {
                var currentStr = currentValue.ToString();
                if (!string.IsNullOrEmpty(currentStr) && options.Contains(currentStr))
                {
                    comboBox.SelectedItem = currentStr;
                }
                else if (int.TryParse(currentStr, out int intValue))
                {
                    var matchingOption = options.FirstOrDefault(o => o.StartsWith($"{intValue}-"));
                    comboBox.SelectedItem = matchingOption ?? "无";
                }
                else
                {
                    comboBox.SelectedItem = "无";
                }
            }

            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem == null || comboBox.SelectedItem.ToString() == "无")
                {
                    prop.SetValue(dataObject, prop.PropertyType == typeof(int) ? 0 : "");
                    return;
                }

                var selectedValue = comboBox.SelectedItem.ToString();

                if (prop.PropertyType == typeof(int))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(selectedValue, @"^(\d+)-");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int intValue))
                    {
                        prop.SetValue(dataObject, intValue);
                    }
                    else if (int.TryParse(selectedValue, out int intValue2))
                    {
                        prop.SetValue(dataObject, intValue2);
                    }
                    else
                    {
                        prop.SetValue(dataObject, 0);
                    }
                }
                else
                {
                    prop.SetValue(dataObject, selectedValue);
                }
            };

            panel.Children.Add(comboBox);
            return panel;
        }

        private static UIElement CreateTextField(string label, object dataObject, PropertyInfo prop, object currentValue)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

            panel.Children.Add(CreateLabel($"{label}: "));

            var box = new TextBox
            {
                Text = currentValue?.ToString() ?? "",
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

            box.TextChanged += (s, e) =>
            {
                prop.SetValue(dataObject, box.Text);
                box.Background = Brushes.LightYellow;
            };
            panel.Children.Add(box);

            return panel;
        }

        private static UIElement CreateScriptDisplay(string fieldName, ParsedScript? parsed)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };

            panel.Children.Add(new TextBlock
            {
                Text = $"📜 {fieldName}:",
                FontWeight = FontWeights.Bold,
                FontSize = 12
            });

            if (parsed == null || !parsed.IsValid)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"  {(parsed?.ErrorMessage ?? "解析失败")}",
                    Foreground = Brushes.Red,
                    FontSize = 11,
                    Margin = new Thickness(10, 0, 0, 0)
                });
                return panel;
            }

            if (parsed.Parts.Count == 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "  (空脚本)",
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(10, 0, 0, 0)
                });
                return panel;
            }

            foreach (var part in parsed.Parts)
            {
                var color = GetPartColor(part.Type);
                var displayText = $"  • [{part.Type}] {part.Name}";
                if (part.Arguments.Count > 0)
                {
                    displayText += $"({string.Join(", ", part.Arguments)})";
                }
                panel.Children.Add(new TextBlock
                {
                    Text = displayText,
                    Foreground = color,
                    FontSize = 11,
                    Margin = new Thickness(10, 0, 0, 0)
                });
            }

            return panel;
        }

        private static Brush GetPartColor(string type)
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

        private static TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            };
        }
        /// <summary>
        /// 重建列表字段（用于添加/删除后刷新）
        /// </summary>
        /// <summary>
        /// 重建列表字段（用于添加/删除后刷新）
        /// </summary>
        /// <summary>
        /// 重建列表字段（用于添加/删除后刷新）
        /// </summary>
        private static void RebuildListField(StackPanel parentStack, object dataObject, PropertyInfo prop,
            EditableAttribute attr, int depth)
        {
            // 清除所有子元素（保留标题）
            var itemsToRemove = new List<UIElement>();
            bool foundTitle = false;

            foreach (var child in parentStack.Children)
            {
                if (!foundTitle && child is TextBlock tb && tb.Text?.Contains("📋") == true)
                {
                    foundTitle = true;
                    continue;
                }
                if (foundTitle)
                {
                    itemsToRemove.Add((UIElement)child);
                }
            }

            foreach (UIElement item in itemsToRemove)
            {
                parentStack.Children.Remove(item);
            }

            // 重新获取列表
            var list = prop.GetValue(dataObject) as System.Collections.IList;
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
                        var box = new TextBox
                        {
                            Text = item?.ToString() ?? "0",
                            Width = 60,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.White,
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(1),
                            TextAlignment = TextAlignment.Center
                        };
                        int idx = i;
                        var listRef = list;
                        box.TextChanged += (s, e) =>
                        {
                            if (int.TryParse(box.Text, out int newValue))
                            {
                                listRef[idx] = newValue;
                                box.Background = Brushes.LightYellow;
                            }
                            else
                            {
                                box.Background = Brushes.LightPink;
                            }
                        };
                        itemPanel.Children.Add(box);
                    }
                    else if (elementType == typeof(double))
                    {
                        var box = new TextBox
                        {
                            Text = item?.ToString() ?? "0",
                            Width = 60,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.White,
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(1),
                            TextAlignment = TextAlignment.Center
                        };
                        int idx = i;
                        var listRef = list;
                        box.TextChanged += (s, e) =>
                        {
                            if (double.TryParse(box.Text, out double newValue))
                            {
                                listRef[idx] = newValue;
                                box.Background = Brushes.LightYellow;
                            }
                            else
                            {
                                box.Background = Brushes.LightPink;
                            }
                        };
                        itemPanel.Children.Add(box);
                    }
                    else if (elementType == typeof(string))
                    {
                        var box = new TextBox
                        {
                            Text = item?.ToString() ?? "",
                            Width = 150,
                            VerticalAlignment = VerticalAlignment.Center,
                            Background = Brushes.White,
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(1)
                        };
                        int idx = i;
                        var listRef = list;
                        box.TextChanged += (s, e) =>
                        {
                            listRef[idx] = box.Text;
                            box.Background = Brushes.LightYellow;
                        };
                        itemPanel.Children.Add(box);
                    }
                    else if (elementType == typeof(ResistEntry))
                    {
                        var resist = item as ResistEntry;
                        if (resist == null) continue;

                        itemPanel.Children.Add(new TextBlock
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
                        itemPanel.Children.Add(valueBox);
                    }
                    else if (elementType == typeof(SkillSlot))
                    {
                        var slot = item as SkillSlot;
                        if (slot == null) continue;

                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = $"{i + 1}. ",
                            FontSize = 11,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        });

                        // skillId 输入框
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
                        int idx = i;
                        var listRef = list;
                        idBox.TextChanged += (s, e) =>
                        {
                            if (int.TryParse(idBox.Text, out int newId))
                            {
                                var currentSlot = listRef[idx] as SkillSlot;
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
                        itemPanel.Children.Add(idBox);

                        // "×" 分隔符
                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = " × ",
                            FontSize = 11,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 5, 0)
                        });

                        // number 输入框
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
                        int numIdx = i;
                        numBox.TextChanged += (s, e) =>
                        {
                            if (int.TryParse(numBox.Text, out int newNum))
                            {
                                var currentSlot = listRef[numIdx] as SkillSlot;
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
                        itemPanel.Children.Add(numBox);
                    }
                    // ===== 处理 Pattern 类型 =====
                    else if (elementType == typeof(Pattern))
                    {
                        RenderPatternItem(itemPanel, item, list, i, attr, depth, dataObject, parentStack, prop);
                    }
                    else
                    {
                        // ===== 检查是否是自定义类（需要递归展开） =====
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

                                var nestedEditor = GenerateEditor(item, depth + 1);
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
                                    var deleteBtn = new Button
                                    {
                                        Content = "✕",
                                        Width = 24,
                                        Height = 24,
                                        FontSize = 10,
                                        Padding = new Thickness(2),
                                        ToolTip = "删除此项",
                                        Margin = new Thickness(5, 0, 0, 0),
                                        Background = Brushes.LightPink,
                                        BorderBrush = Brushes.Gray,
                                        BorderThickness = new Thickness(1)
                                    };
                                    int deleteIdx = i;
                                    deleteBtn.Click += (s, e) =>
                                    {
                                        if (list.Count > deleteIdx)
                                        {
                                            list.RemoveAt(deleteIdx);
                                            RebuildListField(parentStack, dataObject, prop, attr, depth);
                                        }
                                    };
                                    itemPanel.Children.Add(deleteBtn);
                                }

                                parentStack.Children.Add(itemPanel);
                                continue;
                            }
                        }
                        // =============================================

                        // 其他复杂对象，只显示 ToString
                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = item?.ToString() ?? "null",
                            FontSize = 11,
                            Foreground = Brushes.Gray
                        });
                        parentStack.Children.Add(itemPanel);
                    }

                    // ===== 重置按钮 =====
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
                    int resetIdx = i;
                    var resetList = list;
                    resetBtn.Click += (s, e) =>
                    {
                        if (elementType == typeof(int))
                        {
                            resetList[resetIdx] = 0;
                        }
                        else if (elementType == typeof(ResistEntry))
                        {
                            var resist = resetList[resetIdx] as ResistEntry;
                            if (resist != null)
                            {
                                resist.value = 1;
                            }
                        }
                        RebuildListField(parentStack, dataObject, prop, attr, depth);
                    };
                    itemPanel.Children.Add(resetBtn);

                    // ===== 删除按钮（仅当 AllowAddRemove = true） =====
                    if (attr.AllowAddRemove)
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
                        int deleteIdx = i;
                        deleteBtn.Click += (s, e) =>
                        {
                            if (list.Count > deleteIdx)
                            {
                                list.RemoveAt(deleteIdx);
                                RebuildListField(parentStack, dataObject, prop, attr, depth);
                            }
                        };
                        itemPanel.Children.Add(deleteBtn);
                    }

                    if (itemPanel.Parent is Panel oldParent)
                    {
                        oldParent.Children.Remove(itemPanel);
                    }
                    parentStack.Children.Add((UIElement)itemPanel);
                }
            }

            // ===== 添加按钮（仅当 AllowAddRemove = true） =====
            if (attr.AllowAddRemove)
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
                    if (elementType != null)
                    {
                        var newItem = CreateDefaultListItem(elementType);
                        if (newItem == null)
                        {
                            MessageBox.Show($"无法为类型 {elementType.Name} 创建默认项，请确认该类型有无参构造函数。", "添加失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        list.Add(newItem);
                        RebuildListField(parentStack, dataObject, prop, attr, depth);
                    }
                };
                addPanel.Children.Add(addBtn);
                parentStack.Children.Add((UIElement)addPanel);
            }

            // ===== 提示信息 =====
            var hintText = attr.AllowAddRemove
                ? "💡 点击 ✕ 删除，点击 ➕ 添加"
                : "💡 修改数值即可更新";
            parentStack.Children.Add(new TextBlock
            {
                Text = hintText,
                Foreground = Brushes.Gray,
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        /// <summary>
        /// 渲染 Pattern 列表项（只显示模式编号）
        /// </summary>
        /// <summary>
        /// 渲染 Pattern 列表项（只显示模式编号）
        /// </summary>
        /// <summary>
        /// 渲染 Pattern 列表项（只显示模式编号）
        /// </summary>
        /// <summary>
        /// 渲染 Pattern 列表项（只显示模式编号）
        /// </summary>
        private static void RenderPatternItem(StackPanel itemPanel, object item, IList list,
            int index, EditableAttribute attr, int depth, object dataObject,
            StackPanel parentStack, PropertyInfo prop, int? expandedPatternIndex = null)
        {
            var pattern = item as Pattern;
            if (pattern == null) return;

            // 确保数据结构完整
            EnsurePatternStructure(pattern);

            // 获取所有技能ID（从实际数据中读取）
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

            // Expander - 默认折叠（若指定当前编辑项，则保持展开）
            var expander = new Expander
            {
                Header = $"▼ 点击展开编辑 模式 {patternNumber}",
                IsExpanded = isInitiallyExpanded,
                Margin = new Thickness(0, 2, 0, 2),
                Foreground = Brushes.DarkGray,
                FontSize = 11
            };

            var skillEditorPanel = new StackPanel { Margin = new Thickness(15, 5, 0, 5) };

            // 显示当前技能数量
            skillEditorPanel.Children.Add(new TextBlock
            {
                Text = $"当前技能数: {skillIds.Count} 个",
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // 技能编辑行 - 显示所有技能ID
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
                        // 更新实际数据中对应位置的技能ID
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

            // ===== 添加按钮（添加到末尾） =====
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
                // 获取当前的 Pattern（直接从 list 中获取，确保数据一致）
                var currentPattern = list[index] as Pattern;
                if (currentPattern != null)
                {
                    // 在末尾添加新技能
                    AddSkillToPattern(currentPattern);
                    // 刷新显示并保持当前模式展开
                    RebuildListFieldWithList(parentStack, dataObject, prop, attr, depth, list, index);
                }
            };
            btnPanel.Children.Add(addBtn);

            // ===== 删除按钮（删除最后一个） =====
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
                    // 获取当前技能数量
                    var currentIds = GetSkillIdsFromPattern(currentPattern);

                    // 最少保留1个（允许少于4个）
                    if (currentIds.Count <= 1)
                    {
                        MessageBox.Show("最少保留1个技能，无法继续删除！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 删除最后一个
                    RemoveLastSkillFromPattern(currentPattern);
                    // 刷新显示并保持当前模式展开
                    RebuildListFieldWithList(parentStack, dataObject, prop, attr, depth, list, index);
                }
            };
            btnPanel.Children.Add(removeBtn);
            skillEditorPanel.Children.Add(btnPanel);

            expander.Content = skillEditorPanel;
            itemPanel.Children.Add(expander);
        }
        /// <summary>
        /// 重建列表字段（传入指定的列表，避免重新获取导致数据不同步）
        /// </summary>
        private static void RebuildListFieldWithList(StackPanel parentStack, object dataObject,
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

                    // 只处理 Pattern 类型（因为这个方法专门用于刷新 Pattern 列表）
                    if (elementType == typeof(Pattern))
                    {
                        RenderPatternItem(itemPanel, item, list, i, attr, depth, dataObject, parentStack, prop, expandedPatternIndex);

                        if (attr.AllowAddRemove)
                        {
                            var deleteBtn = new Button
                            {
                                Content = "✕",
                                Width = 24,
                                Height = 24,
                                FontSize = 10,
                                Padding = new Thickness(2),
                                ToolTip = "删除此项",
                                Margin = new Thickness(6, 0, 0, 0),
                                Background = Brushes.LightPink,
                                BorderBrush = Brushes.Gray,
                                BorderThickness = new Thickness(1),
                                VerticalAlignment = VerticalAlignment.Top
                            };
                            int deleteIdx = i;
                            deleteBtn.Click += (s, e) =>
                            {
                                if (list.Count > deleteIdx)
                                {
                                    list.RemoveAt(deleteIdx);
                                    RebuildListFieldWithList(parentStack, dataObject, prop, attr, depth, list);
                                }
                            };
                            itemPanel.Children.Add(deleteBtn);
                        }

                        parentStack.Children.Add(itemPanel);
                    }
                    else
                    {
                        // 其他类型 - 简单显示
                        itemPanel.Children.Add(new TextBlock
                        {
                            Text = item?.ToString() ?? "null",
                            FontSize = 11,
                            Foreground = Brushes.Gray
                        });
                        parentStack.Children.Add(itemPanel);
                    }
                }
            }

            // 重新添加添加按钮
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
        /// <summary>
        /// 确保 Pattern 的数据结构完整，并清理空的 Slot
        /// </summary>
        private static void EnsurePatternStructure(Pattern pattern)
        {
            if (pattern == null) return;

            if (pattern.slotList == null)
            {
                pattern.slotList = new List<Slot>();
            }

            // 移除所有空的 Slot（没有技能或所有 skillChildList 为空）
            var nonEmptySlots = new List<Slot>();
            foreach (var slot in pattern.slotList)
            {
                if (slot == null) continue;

                if (slot.skillParentList == null)
                {
                    slot.skillParentList = new List<SkillParent>();
                }

                // 清理空的 SkillParent
                var nonEmptyParents = new List<SkillParent>();
                foreach (var parent in slot.skillParentList)
                {
                    if (parent == null) continue;

                    if (parent.skillChildList == null)
                    {
                        parent.skillChildList = new List<SkillChild>();
                    }

                    // 只保留有技能的 SkillParent
                    if (parent.skillChildList.Count > 0)
                    {
                        nonEmptyParents.Add(parent);
                    }
                }
                slot.skillParentList = nonEmptyParents;

                // 如果 Slot 有技能，保留
                if (slot.skillParentList.Count > 0)
                {
                    nonEmptySlots.Add(slot);
                }
            }

            pattern.slotList = nonEmptySlots;

            // 如果没有 Slot 或所有 Slot 都为空，创建一个空的 Slot
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

        /// <summary>
        /// 从 Pattern 中获取所有技能ID（遍历所有 Slot）
        /// </summary>
        private static List<int> GetSkillIdsFromPattern(Pattern pattern)
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

        /// <summary>
        /// 更新 Pattern 中指定位置的技能ID（遍历所有 Slot）
        /// </summary>
        private static void UpdateSkillIdInPattern(Pattern pattern, int index, int newId)
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

        /// <summary>
        /// 向 Pattern 末尾添加一个新技能（新增一个 Slot，保持原始层级结构）
        /// </summary>
        private static void AddSkillToPattern(Pattern pattern)
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

        /// <summary>
        /// 从 Pattern 末尾删除一个技能（按显示顺序从后往前删除）
        /// </summary>
        private static void RemoveLastSkillFromPattern(Pattern pattern)
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
    }
}