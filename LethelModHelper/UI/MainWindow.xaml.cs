using LethelModHelper.Core.Models;
using LethelModHelper.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LethelModHelper
{
    public partial class MainWindow : Window
    {
        #region 字段和属性

        private ModScanner _scanner;
        private Dictionary<string, object> _fileDataMap = new();
        private ScriptParser _scriptParser = new();
        private readonly FileService _fileService;
        private readonly LocaleService _localeService;
        private readonly ModDataService _dataService;
        private string? _currentFilePath;
        

        // 罪人名称映射
        private static readonly Dictionary<int, string> SinnerNames = new()
        {
            { 1, "Yi Sang" }, { 2, "Faust" }, { 3, "Don Quixote" },
            { 4, "Ryoshu" }, { 5, "Meursault" }, { 6, "Hong Lu" },
            { 7, "Heathcliff" }, { 8, "Ishmael" }, { 9, "Rodion" },
            { 10, "Sinclair" }, { 11, "Outis" }, { 12, "Gregor" }
        };

        #endregion

        #region 构造函数

        public MainWindow()
        {
            InitializeComponent();
            _scanner = new ModScanner();
            _scanner.FileParsed += OnFileParsed;
            _scanner.FileParseFailed += OnFileParseFailed;

            _fileService = new FileService();
            _localeService = new LocaleService(_fileService);
            _dataService = new ModDataService(_fileService);
        }

        #endregion

        #region 事件处理

        private void OpenModButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "选择Mod文件夹" };
            if (dialog.ShowDialog() != true) return;

            try
            {
                _scanner.OpenMod(dialog.FolderName);
                UpdateFileTree();
                UpdateStatusAfterLoad(dialog.FolderName);
            }
            catch (Exception ex)
            {
                ShowError("加载失败", ex);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_scanner.CurrentModPath))
            {
                MessageBox.Show("请先打开一个Mod", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                RefreshMod();
            }
            catch (Exception ex)
            {
                ShowError("刷新失败", ex);
            }
        }

        private void OnFileParsed(object? sender, string filePath)
        {
            if (_scanner.ParsedFiles.TryGetValue(filePath, out var result) &&
                result.Success && result.Data != null)
            {
                _fileDataMap[filePath] = result.Data;
            }
        }

        private void OnFileParseFailed(object? sender, string errorMessage)
        {
            Dispatcher.Invoke(() => FooterStatusTextBlock.Text = $"⚠️ {errorMessage}");
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem { Tag: string filePath } && _fileService.Exists(filePath))
            {
                DisplayFileContent(filePath);
            }
        }

        private void FileTreeView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var clickedNode = FindParentTreeViewItem(
                VisualTreeHelper.HitTest(FileTreeView, e.GetPosition(FileTreeView))?.VisualHit as DependencyObject);

            if (clickedNode == null || clickedNode.Tag is not string tag) return;

            clickedNode.IsSelected = true;
            ShowContextMenu(tag);
        }

        #endregion

        #region 文件树操作

        /// <summary>
        /// 刷新左侧文件树：只显示解析成功的文件，并按目录分组。
        /// </summary>
        private void UpdateFileTree()
        {
            FileTreeView.Items.Clear();
            if (string.IsNullOrEmpty(_scanner.CurrentModPath)) return;

            var rootNode = BuildFileTreeRootNode(_scanner.CurrentModPath);
            foreach (var folderGroup in GetSuccessfulFileGroupsByFolder())
            {
                rootNode.Items.Add(BuildFolderNode(folderGroup));
            }

            FileTreeView.Items.Add(rootNode);
            rootNode.IsExpanded = true;
        }

        private TreeViewItem BuildFileTreeRootNode(string modPath)
        {
            return CreateFileTreeNode(Path.GetFileName(modPath), modPath, "📁", true);
        }

        /// <summary>
        /// 获取所有解析成功文件，并按其所在目录分组。
        /// </summary>
        /// <returns>按目录路径分组后的文件集合。</returns>
        private IEnumerable<IGrouping<string, KeyValuePair<string, FileParseResult>>> GetSuccessfulFileGroupsByFolder()
        {
            return _scanner.ParsedFiles
                .Where(kvp => kvp.Value.Success)
                .Where(kvp => !kvp.Key.Contains("\\personality-passive\\", StringComparison.OrdinalIgnoreCase))
                .Where(kvp => !kvp.Key.Contains("\\personality_passive\\", StringComparison.OrdinalIgnoreCase))
                .GroupBy(kvp => Path.GetDirectoryName(kvp.Key) ?? "")
                .OrderBy(group => group.Key);
        }

        /// <summary>
        /// 根据目录分组构建一个文件夹节点，并挂载其文件子节点。
        /// </summary>
        /// <param name="folderGroup">同一目录下的文件分组。</param>
        /// <returns>已填充子文件节点的目录节点。</returns>
        private TreeViewItem BuildFolderNode(IGrouping<string, KeyValuePair<string, FileParseResult>> folderGroup)
        {
            var folderName = string.IsNullOrEmpty(Path.GetFileName(folderGroup.Key))
                ? "根目录"
                : Path.GetFileName(folderGroup.Key);

            var folderNode = CreateFileTreeNode(folderName, folderGroup.Key, "📁", true);
            foreach (var fileEntry in folderGroup.OrderBy(entry => Path.GetFileName(entry.Key)))
            {
                folderNode.Items.Add(BuildFileNode(fileEntry.Key));
            }

            return folderNode;
        }

        private TreeViewItem BuildFileNode(string filePath)
        {
            return CreateFileTreeNode(Path.GetFileName(filePath), filePath, "📄", false);
        }

        private TreeViewItem CreateFileTreeNode(string header, string tag, string icon, bool isExpanded)
        {
            return new TreeViewItem
            {
                Header = $"{icon} {header}",
                IsExpanded = isExpanded,
                Tag = tag
            };
        }

        private void SelectFileInTree(string filePath)
        {
            foreach (TreeViewItem rootItem in FileTreeView.Items)
            {
                if (TrySelectFileNodeRecursively(rootItem, filePath)) return;
            }
        }

        /// <summary>
        /// 在树节点中递归查找并选中目标文件节点。
        /// </summary>
        /// <param name="parent">当前递归起点节点。</param>
        /// <param name="filePath">目标文件完整路径。</param>
        /// <returns>找到并选中返回 true；否则 false。</returns>
        private bool TrySelectFileNodeRecursively(TreeViewItem parent, string filePath)
        {
            if (parent.Tag is string tag && tag == filePath)
            {
                parent.IsSelected = true;
                parent.BringIntoView();
                FileTreeView_SelectedItemChanged(parent,
                    new RoutedPropertyChangedEventArgs<object>(null, parent));
                return true;
            }

            return parent.Items.Cast<TreeViewItem>().Any(child => TrySelectFileNodeRecursively(child, filePath));
        }

        private TreeViewItem? FindParentTreeViewItem(DependencyObject? child)
        {
            while (child != null)
            {
                if (child is TreeViewItem item) return item;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        #endregion

        #region 内容显示

        /// <summary>
        /// 显示指定文件的解析结果内容。
        /// </summary>
        /// <param name="filePath">需要显示的文件路径。</param>
        private void DisplayFileContent(string filePath)
        {
            InitializeFileDisplayContext(filePath);

            if (!TryGetValidParseResult(filePath, out var parseResult)) return;

            DisplayParseWarnings(parseResult.Warnings);
            DisplayDataByType(parseResult.Data!);
        }

        private void InitializeFileDisplayContext(string filePath)
        {
            _currentFilePath = filePath;
            ContentPanel.Children.Clear();
            FileTitleTextBlock.Text = Path.GetFileName(filePath);
        }

        /// <summary>
        /// 校验文件解析结果是否可用于展示，失败时直接输出提示文本。
        /// </summary>
        /// <param name="filePath">待校验文件路径。</param>
        /// <param name="parseResult">输出解析结果。</param>
        /// <returns>可展示返回 true，否则返回 false。</returns>
        private bool TryGetValidParseResult(string filePath, out FileParseResult parseResult)
        {
            if (!_scanner.ParsedFiles.TryGetValue(filePath, out parseResult!))
            {
                AddTextToContent("没有找到该文件的解析数据", Brushes.Red);
                return false;
            }

            if (!parseResult.Success)
            {
                AddTextToContent($"❌ 解析失败: {parseResult.ErrorMessage}", Brushes.Red, 14);
                return false;
            }

            if (parseResult.Data == null)
            {
                AddTextToContent("数据为空", Brushes.Gray);
                return false;
            }

            return true;
        }

        private void DisplayParseWarnings(List<string> warnings)
        {
            if (warnings.Count == 0) return;

            AddTextToContent(
                $"⚠️ 警告 ({warnings.Count}条):\n{string.Join("\n", warnings)}",
                Brushes.Orange, 12, new Thickness(0, 0, 0, 10), true);
        }

        /// <summary>
        /// 根据数据运行时类型分发到对应渲染流程。
        /// </summary>
        /// <param name="data">解析得到的数据对象。</param>
        private void DisplayDataByType(object data)
        {
            switch (data)
            {
                case PersonalityData personalityData:
                    DisplayPersonalityData(personalityData);
                    break;                case PassiveData passiveTriggerData:
                    DisplayPassiveTriggerData(passiveTriggerData);
                    break;
                case BuffData buffData:
                    DisplayBuffData(buffData);
                    break;
                case AbnormalityData abnormalityData:
                    DisplayAbnormalityData(abnormalityData);
                    break;
                default:
                    AddTextToContent(data.ToString() ?? "（无数据）", Brushes.Black, 12,
                        new Thickness(0), true);
                    break;
            }
        }

        private bool PrepareListContent<T>(List<T>? items, string emptyMessage)
        {
            ContentPanel.Children.Clear();
            return !IsListEmpty(items, emptyMessage);
        }

        /// <summary>
        /// 通用可编辑列表渲染模板：头部 + 保存按钮 + 条目展开编辑器。
        /// </summary>
        /// <typeparam name="T">列表条目类型。</typeparam>
        /// <param name="entries">待渲染条目集合。</param>
        /// <param name="headerText">顶部摘要文本。</param>
        /// <param name="saveAction">保存按钮行为。</param>
        /// <param name="headerBuilder">单条目标题构建器。</param>
        private void RenderEditableDataList<T>(List<T> entries, string headerText, Action saveAction, Func<T, string> headerBuilder)
        {
            AddHeaderWithSaveButton(headerText, saveAction);

            foreach (var entry in entries)
            {
                ContentPanel.Children.Add(CreateDataExpander(headerBuilder(entry), entry!));
            }
        }

        private void DisplayPersonalityData(PersonalityData data)
        {
            if (!PrepareListContent(data.list, "没有 Personality 数据")) return;

            RenderEditableDataList(
                data.list,
                $"共 {data.list.Count} 个 Personality 条目",
                () => SaveData(data, "Personality 数据"),
                entry => $"ID: {entry.id} | 罪人: {GetSinnerName(entry.characterId)} | 星级: {GetStarText(entry.rank)}");
        }
        private void DisplayPassiveTriggerData(PassiveData data)
        {
            if (IsListEmpty(data.list, "没有 passive 数据")) return;

            AddTextToContent($"共 {data.list.Count} 个 passive 条目", Brushes.Black, 14,
                new Thickness(0, 0, 0, 10), true);

            foreach (var entry in data.list)
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
                ContentPanel.Children.Add(expander);
            }
        }

        private void DisplayBuffData(BuffData data)
        {
            if (!PrepareListContent(data.list, "没有 Buff 数据")) return;

            ContentPanel.Children.Add(CreateBuffHeaderPanel(data));

            foreach (var entry in data.list)
            {
                DisplayBuffEntry(entry);
            }
        }

        private StackPanel CreateBuffHeaderPanel(BuffData data)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            headerPanel.Children.Add(CreateSelectableText(
                $"共 {data.list.Count} 个 Buff", true, null, 14));

            AddButton(headerPanel, "💾 保存本地化", Brushes.LightGreen, () =>
            {
                LocaleCache.SaveBuffLocaleData();
                LocaleCache.SaveKeywordLocaleData();
                MessageBox.Show("本地化已保存！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });

            AddButton(headerPanel, "💾 保存所有修改", null, () => SaveData(data, "Buff 数据"));
            return headerPanel;
        }

        private void DisplayBuffEntry(BuffEntry entry)
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
            ContentPanel.Children.Add(expander);
        }

        private void DisplayAbnormalityData(AbnormalityData data)
        {
            if (!PrepareListContent(data.list, "没有异常体数据")) return;

            RenderEditableDataList(
                data.list,
                $"共 {data.list.Count} 个异常体",
                () => SaveData(data, "异常体数据"),
                entry => $"ID: {entry.id} | 类型: {entry.classType ?? "未知"}");
        }

        #endregion

        #region UI 辅助方法

        private Expander CreateDataExpander(string header, object data)
        {
            var expander = new Expander
            {
                Header = header,
                IsExpanded = false,
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(10)
            };

            var contentStack = new StackPanel();
            contentStack.Children.Add(EditorGenerator.GenerateEditor(data));
            expander.Content = contentStack;

            return expander;
        }

        private void AddHeaderWithSaveButton(string headerText, Action saveAction)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            headerPanel.Children.Add(CreateSelectableText(headerText, true, null, 14));
            AddButton(headerPanel, "💾 保存所有修改", null, saveAction);
            ContentPanel.Children.Add(headerPanel);
        }

        private void AddButton(StackPanel panel, string content, Brush? background, Action onClick)
        {
            var button = new Button
            {
                Content = content,
                Margin = new Thickness(10, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5),
                VerticalAlignment = VerticalAlignment.Center
            };

            if (background != null)
            {
                button.Background = background;
            }

            button.Click += (s, e) => onClick();
            panel.Children.Add(button);
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

        private void AddPassiveList(StackPanel stack, string title, List<PassiveGroup>? passiveList)
        {
            if (passiveList == null || passiveList.Count == 0) return;

            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 5, 0, 3)
            });

            foreach (var group in passiveList)
            {
                var ids = string.Join(", ", group.passiveIDList ?? new List<int>());
                stack.Children.Add(new TextBlock
                {
                    Text = $"  Uptie {group.level}: [{ids}]",
                    Margin = new Thickness(15, 0, 0, 2)
                });
            }
        }

        private TextBox CreateSelectableText(string text, bool isBold = false,
            Brush? foreground = null, double fontSize = 12,
            double marginLeft = 0, double marginTop = 2)
        {
            return new TextBox
            {
                Text = text,
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                FontSize = fontSize,
                Margin = new Thickness(marginLeft, marginTop, 0, 2),
                Cursor = Cursors.IBeam,
                IsTabStop = false,
                Foreground = foreground ?? Brushes.Black,
                IsReadOnlyCaretVisible = false
            };
        }

        private void AddTextToContent(string text, Brush foreground,
            double fontSize = 12, Thickness? margin = null, bool wrap = false)
        {
            ContentPanel.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = foreground,
                FontSize = fontSize,
                Margin = margin ?? new Thickness(0),
                TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap
            });
        }

        private bool IsListEmpty<T>(List<T>? list, string emptyMessage)
        {
            if (list != null && list.Count > 0) return false;

            AddTextToContent(emptyMessage, Brushes.Gray);
            return true;
        }

        #endregion

        #region 数据保存

        private void SaveData<T>(T data, string dataTypeName)
        {
            try
            {
                if (!TryGetCurrentFilePath(out var currentFilePath)) return;

                _dataService.Save(currentFilePath, data);
                FooterStatusTextBlock.Text = $"✅ 已保存: {Path.GetFileName(currentFilePath)}";
                RefreshCurrentView();
                MessageBox.Show($"{dataTypeName}保存成功！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("保存失败", ex);
            }
        }

        private bool TryGetCurrentFilePath(out string currentFilePath)
        {
            currentFilePath = _currentFilePath ?? string.Empty;
            if (!string.IsNullOrEmpty(currentFilePath)) return true;

            MessageBox.Show("找不到当前文件的路径", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        #endregion

        #region 辅助方法

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

        private string GetPartDisplayText(ScriptPart part)
        {
            var display = $"[{part.Type}] {part.Name}";
            if (part.Arguments.Count > 0)
            {
                display += $"({string.Join(", ", part.Arguments)})";
            }
            return display;
        }

        private UIElement CreateScriptDisplayBlock(string rawScript, ParsedScript parsed)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };
            panel.Children.Add(CreateSelectableText(
                $"  📜 脚本: {rawScript}", false, Brushes.Gray, 11, 10));

            DisplayParsedScript(parsed, panel, 15);
            return panel;
        }

        private void DisplayParsedScript(ParsedScript? parsed, StackPanel container, double marginLeft)
        {
            if (parsed == null)
            {
                container.Children.Add(CreateSelectableText(
                    "  (解析失败)", false, Brushes.Red, 11, marginLeft));
                return;
            }

            if (!parsed.IsValid)
            {
                container.Children.Add(CreateSelectableText(
                    $"  ⚠️ {parsed.ErrorMessage}", false, Brushes.Red, 11, marginLeft));
                return;
            }

            if (parsed.Parts.Count == 0)
            {
                container.Children.Add(CreateSelectableText(
                    "  (空脚本)", false, Brushes.Gray, 11, marginLeft));
                return;
            }

            foreach (var part in parsed.Parts)
            {
                var color = GetPartColor(part.Type);
                container.Children.Add(CreateSelectableText(
                    $"  • {GetPartDisplayText(part)}", false, color, 11, marginLeft));
            }
        }

        private void RefreshCurrentView()
        {
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                DisplayFileContent(_currentFilePath);
            }
        }

        private void RefreshMod()
        {
            _scanner.OpenMod(_scanner.CurrentModPath);
            UpdateFileTree();
            FooterStatusTextBlock.Text = $"刷新完成，共解析 {_scanner.ParsedFiles.Count} 个文件";
        }

        private void UpdateStatusAfterLoad(string modPath)
        {
            StatusTextBlock.Text = $"已加载: {modPath}";
            FooterStatusTextBlock.Text = $"加载完成，共解析 {_scanner.ParsedFiles.Count} 个文件";
        }

        private void ShowError(string title, Exception ex)
        {
            MessageBox.Show($"{title}: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowContextMenu(string tag)
        {
            var contextMenu = new ContextMenu();

            if (Directory.Exists(tag))
            {
                AddFolderContextMenu(contextMenu, tag);
            }
            else if (_fileService.Exists(tag) && Path.GetExtension(tag).ToLower() == ".json")
            {
                AddFileContextMenu(contextMenu, tag);
            }

            if (contextMenu.Items.Count > 0)
            {
                contextMenu.IsOpen = true;
            }
        }

        private void AddFolderContextMenu(ContextMenu menu, string folderPath)
        {
            var folderName = Path.GetFileName(folderPath);
            if (folderName != "buff" && !folderPath.Contains("\\buff\\")) return;

            var menuItem = new MenuItem { Header = "📁 新建 Buff", Tag = folderPath };
            menuItem.Click += CreateNewBuff_Click;
            menu.Items.Add(menuItem);
        }

        private void AddFileContextMenu(ContextMenu menu, string filePath)
        {
            var deleteItem = new MenuItem
            {
                Header = "🗑️ 删除",
                Tag = filePath,
                Foreground = Brushes.Red
            };
            deleteItem.Click += DeleteJsonFile_Click;
            menu.Items.Add(deleteItem);
        }

        private string? GetModPathFromBuffFolder(string buffFolderPath)
        {
            var directory = new DirectoryInfo(buffFolderPath);
            while (directory != null)
            {
                var customLimbusData = Path.Combine(directory.FullName, "custom_limbus_data");
                if (Directory.Exists(customLimbusData))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }
            return null;
        }

        #endregion

        #region 删除和创建操作

        private void DeleteJsonFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: string filePath } || !_fileService.Exists(filePath))
            {
                MessageBox.Show("文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除以下文件及其本地化条目吗？\n\n{Path.GetFileName(filePath)}\n\n此操作不可撤销！",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                DeleteFileAndLocale(filePath);
                RefreshAfterDelete();
                MessageBox.Show($"已删除: {Path.GetFileName(filePath)}", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("删除失败", ex);
            }
        }

        private void DeleteFileAndLocale(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var folderPath = Path.GetDirectoryName(filePath);

            _localeService.DeleteLocale(filePath);
            System.Diagnostics.Debug.WriteLine($"✅ 已删除文件: {filePath}");

            var modPath = GetModPathFromBuffFolder(folderPath);
            if (string.IsNullOrEmpty(modPath)) return;

            DeleteFromLocaleFile(
                Path.Combine(modPath, "custom_limbus_locale", "EN", "bufList"),
                fileName, typeof(BuffLocaleEntry));

            DeleteFromLocaleFile(
                Path.Combine(modPath, "custom_limbus_locale", "EN", "keywordList"),
                fileName, typeof(KeywordLocaleEntry));

            _fileDataMap.Remove(filePath);
            LocaleCache.BuffLocaleMap.Remove(fileName);
            LocaleCache.KeywordLocaleMap.Remove(fileName);
        }

        private void DeleteFromLocaleFile(string localeFolder, string entryId, Type entryType)
        {
            if (!Directory.Exists(localeFolder))
            {
                System.Diagnostics.Debug.WriteLine($"本地化文件夹不存在: {localeFolder}");
                return;
            }

            var jsonFiles = Directory.GetFiles(localeFolder, "*.json");
            foreach (var filePath in jsonFiles)
            {
                try
                {
                    if (DeleteEntryFromFile(filePath, entryId, entryType))
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ 从 {Path.GetFileName(filePath)} 删除了 {entryId}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ 处理文件 {Path.GetFileName(filePath)} 失败: {ex.Message}");
                }
            }
        }

        private bool DeleteEntryFromFile(string filePath, string entryId, Type entryType)
        {
            if (entryType == typeof(BuffLocaleEntry))
            {
                var data = _dataService.LoadLocaleDictionary<BuffLocaleEntry>(filePath);
                if (!data.Remove(entryId)) return false;

                _dataService.SaveLocaleDictionary(filePath, data);
                return true;
            }

            if (entryType == typeof(KeywordLocaleEntry))
            {
                var data = _dataService.LoadLocaleDictionary<KeywordLocaleEntry>(filePath);
                if (!data.Remove(entryId)) return false;

                _dataService.SaveLocaleDictionary(filePath, data);
                return true;
            }

            return false;
        }

        private void RefreshAfterDelete()
        {
            try
            {
                if (!string.IsNullOrEmpty(_scanner.CurrentModPath))
                {
                    RefreshMod();
                }

                UpdateFileTree();
                ContentPanel.Children.Clear();
                FileTitleTextBlock.Text = "请选择一个文件";
                _currentFilePath = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 刷新失败: {ex.Message}");
            }
        }

        private void CreateNewBuff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: string folderPath })
            {
                MessageBox.Show("无法获取文件夹路径", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new CreateBuffDialog();
            if (dialog.ShowDialog() != true || string.IsNullOrEmpty(dialog.BuffId))
            {
                MessageBox.Show("Buff ID 不能为空", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                CreateBuff(folderPath, dialog.BuffId, dialog.BuffName, dialog.BuffDesc);
                RefreshAfterCreate(folderPath, dialog.BuffId);
                MessageBox.Show($"Buff '{dialog.BuffId}' 创建成功！", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("创建失败", ex);
            }
        }

        private void CreateBuff(string folderPath, string buffId, string buffName, string buffDesc)
        {
            CreateBuffFile(folderPath, buffId);
            UpdateLocaleFile(folderPath, buffId, buffName, buffDesc, typeof(BuffLocaleEntry));
            UpdateLocaleFile(folderPath, buffId, buffName, buffDesc, typeof(KeywordLocaleEntry));
        }

        private void CreateBuffFile(string folderPath, string buffId)
        {
            var filePath = Path.Combine(folderPath, $"{buffId}.json");
            if (_fileService.Exists(filePath))
            {
                throw new Exception($"文件 {buffId}.json 已存在");
            }

            var buffData = new BuffData
            {
                list = new List<BuffEntry>
        {
            new BuffEntry
            {
                id = buffId,
                iconId = buffId,
                buffType = "Negative",
                maxStack = 10,
                maxTurn = 0,
                canBeDespelled = false,
                destroyableOnZero = false,
                list = new List<BuffAbility>()
            }
        }
            };

            _dataService.Save(filePath, buffData);
        }

        private void UpdateLocaleFile(string buffFolderPath, string buffId,
            string buffName, string buffDesc, Type entryType)
        {
            var modPath = GetModPathFromBuffFolder(buffFolderPath);
            if (string.IsNullOrEmpty(modPath))
            {
                throw new Exception("找不到 Mod 根目录");
            }

            var localeFolder = entryType == typeof(BuffLocaleEntry)
                ? Path.Combine(modPath, "custom_limbus_locale", "EN", "bufList")
                : Path.Combine(modPath, "custom_limbus_locale", "EN", "keywordList");

            Directory.CreateDirectory(localeFolder);

            var jsonFiles = Directory.GetFiles(localeFolder, "*.json");
            var targetFile = jsonFiles.FirstOrDefault() ?? Path.Combine(localeFolder, $"{buffId}.json");

            if (entryType == typeof(BuffLocaleEntry))
            {
                var data = _dataService.LoadLocaleDictionary<BuffLocaleEntry>(targetFile);
                data[buffId] = CreateBuffLocaleEntry(buffId, buffName, buffDesc);
                _dataService.SaveLocaleDictionary(targetFile, data);
                LocaleCache.BuffLocaleMap = data;
                LocaleCache.CurrentBuffLocaleFilePath = targetFile;
            }
            else
            {
                var data = _dataService.LoadLocaleDictionary<KeywordLocaleEntry>(targetFile);
                data[buffId] = CreateKeywordLocaleEntry(buffId, buffName, buffDesc);
                _dataService.SaveLocaleDictionary(targetFile, data);
                LocaleCache.KeywordLocaleMap = data;
                LocaleCache.CurrentKeywordLocaleFilePath = targetFile;
            }
        }

        private BuffLocaleEntry CreateBuffLocaleEntry(string buffId, string buffName, string buffDesc)
        {
            return new BuffLocaleEntry
            {
                id = buffId,
                name = buffName,
                desc = buffDesc?.Replace("\n", "\\n") ?? "",
                summary = "",
                flavor = ""
            };
        }

        private KeywordLocaleEntry CreateKeywordLocaleEntry(string buffId, string buffName, string buffDesc)
        {
            return new KeywordLocaleEntry
            {
                id = buffId,
                name = buffName,
                desc = buffDesc?.Replace("\n", "\\n") ?? "",
                flavor = ""
            };
        }

        private void RefreshAfterCreate(string folderPath, string fileId)
        {
            try
            {
                if (!string.IsNullOrEmpty(_scanner.CurrentModPath))
                {
                    RefreshMod();
                }

                UpdateFileTree();

                var filePath = Path.Combine(folderPath, $"{fileId}.json");
                if (_fileService.Exists(filePath))
                {
                    Dispatcher.BeginInvoke(new Action(() => SelectFileInTree(filePath)),
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 刷新失败: {ex.Message}");
                MessageBox.Show($"刷新失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region 脚本字段显示（保留原有功能）

        private void DisplayScriptFields(object data, StackPanel container, int indent = 0)
        {
            if (data == null) return;

            var type = data.GetType();
            var properties = type.GetProperties();

            foreach (var prop in properties)
            {
                var propValue = prop.GetValue(data);
                if (propValue == null) continue;

                var margin = new Thickness(indent * 15, 2, 0, 2);

                if (prop.PropertyType == typeof(string) && ScriptFieldCache.HasScript(data, prop.Name))
                {
                    var parsed = ScriptFieldCache.Get(data, prop.Name);
                    container.Children.Add(CreateSelectableText($"📜 {prop.Name}:", true, null, 12, margin.Left));
                    DisplayParsedScript(parsed, container, margin.Left + 10);
                }
                else if (prop.PropertyType == typeof(List<string>))
                {
                    DisplayStringList(propValue, prop, data, container, margin);
                }
                else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                {
                    DisplayNestedObject(propValue, prop.PropertyType, container, margin, indent);
                }
            }
        }

        private void DisplayStringList(object propValue, System.Reflection.PropertyInfo prop,
            object data, StackPanel container, Thickness margin)
        {
            var key = $"{data.GetHashCode()}_{prop.Name}_LIST";
            if (ScriptFieldCache.HasList(key))
            {
                var parsedList = ScriptFieldCache.GetList(key);
                if (parsedList != null && parsedList.Count > 0)
                {
                    container.Children.Add(CreateSelectableText(
                        $"📜 {prop.Name} ({parsedList.Count} 个脚本):", true, null, 12, margin.Left));

                    for (int i = 0; i < parsedList.Count; i++)
                    {
                        container.Children.Add(CreateSelectableText($"  [{i}]", true, null, 11, margin.Left + 10));
                        DisplayParsedScript(parsedList[i], container, margin.Left + 25);
                    }
                }
            }
            else if (propValue is System.Collections.IEnumerable list)
            {
                DisplayEnumerable(list, container, margin, 1);
            }
        }

        private void DisplayNestedObject(object propValue, Type propType,
            StackPanel container, Thickness margin, int indent)
        {
            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (propValue is System.Collections.IEnumerable list)
                {
                    DisplayEnumerable(list, container, margin, indent + 1);
                }
            }
            else
            {
                DisplayScriptFields(propValue, container, indent);
            }
        }

        private void DisplayEnumerable(System.Collections.IEnumerable list,
            StackPanel container, Thickness margin, int indent)
        {
            int index = 0;
            foreach (var item in list)
            {
                container.Children.Add(CreateSelectableText($"  [{index++}]", true, null, 11, margin.Left));
                DisplayScriptFields(item, container, indent);
            }
        }

        #endregion
    }
}