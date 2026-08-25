using LethelModHelper.Core.Models;
using LethelModHelper.Services;
using LethelModHelper.Services.Renderers;
using LethelModHelper.Services.Renderers.Helpers;
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

        private readonly ModScanner _scanner;
        private readonly ModSession _modSession = new();
        private readonly FileService _fileService;
        private readonly LocaleService _localeService;
        private readonly ModDataService _dataService;
        private readonly RendererContext _rendererContext;
        private readonly RendererRegistry _rendererRegistry;
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

            _modSession = new ModSession();

            _fileService = new FileService();
            _localeService = new LocaleService(_fileService);
            _dataService = new ModDataService(_fileService);

            _rendererContext = new RendererContext(_dataService);
            _rendererRegistry = new RendererRegistry(_rendererContext);

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
                _modSession.SetFileData(filePath, result.Data);
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
            ContentPanel.Children.Clear();

            var element = _rendererRegistry.Render(data,_currentFilePath!);

            if (element != null)
            {
                ContentPanel.Children.Add(element);
            }
            else
            {
                ContentPanel.Children.Add(
                    new TextBlock
                    {
                        Text = $"暂不支持的数据类型: {data.GetType().Name}",
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(10)
                    });
            }
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

            _modSession.RemoveFileData(filePath);

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
    }
}