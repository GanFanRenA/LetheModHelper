# LethelModHelper

LethelModHelper 是一个基于 **.NET 8 / WPF** 的《边狱巴士》Mod 数据辅助工具，面向 Mod 制作与维护场景，用于扫描 Mod 目录、解析 JSON 数据、动态生成编辑界面，并同步处理部分本地化文件。

## 项目目标

- 可视化查看和编辑 Mod 数据文件
- 自动识别并解析指定类型的游戏数据
- 支持 Buff 本地化、Keyword 本地化的读取、编辑、保存、删除与创建
- 尽量通过模型标记自动生成编辑界面，减少重复 UI 编写

## 逻辑层结构

### 1. UI 层

主要文件：

- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `CreateBuffDialog.xaml`
- `CreateBuffDialog.xaml.cs`

职责：

- 打开 Mod 文件夹
- 显示左侧文件树和右侧内容区
- 根据文件类型展示不同的编辑/查看界面
- 提供刷新、删除、新建 Buff、保存修改等入口
- 展示脚本、列表、嵌套对象、本地化内容等

### 2. Services 层

主要文件：

- `Services/ModScanner.cs`
- `Services/EditorGenerator.cs`
- `Services/ScriptParser.cs`

职责：

- 扫描 Mod 目录并调用对应 Handler 解析文件
- 读取 Buff / Keyword 本地化文件并放入缓存
- 根据模型上的 `EditableAttribute` 动态生成编辑控件
- 解析脚本字符串并拆分成可读结构

### 3. Handlers 层

主要文件：

- `Handlers/BaseJsonHandler.cs`
- `Handlers/BuffHandler.cs`
- `Handlers/AbnormalityHandler.cs`
- `Handlers/PassiveHandler.cs`
- `Handlers/PersonalityHandler.cs`
- `Handlers/PersonalityPassiveHandler.cs`
- `Handlers/IFileHandler.cs`

职责：

- 统一读取 JSON 文件
- 将不同文件夹下的 JSON 映射到对应模型
- 对解析结果做基础校验
- 解析完成后自动处理脚本字段缓存

### 4. Models 层

主要文件：

- `Models/*.cs`

职责：

- 定义 JSON 数据结构
- 定义本地化数据结构
- 定义脚本解析结果
- 定义可编辑属性标记与缓存

---

## 已实现功能

### 文件扫描与树形浏览

- 选择 Mod 根目录后自动扫描
- 左侧文件树仅显示解析成功的文件
- 按目录分组展示
- 支持右键打开上下文菜单
- 支持刷新当前 Mod

### 支持的数据类型

当前已注册的处理器支持以下目录类型：

- `personality`
- `personality-passive`
- `passive`
- `buff`
- `abnormality-unit`

### 动态编辑器生成

`EditorGenerator` 会根据模型上的 `EditableAttribute` 自动生成控件，支持：

- 文本框
- 数值输入
- 布尔勾选
- 下拉选项
- 嵌套对象
- 列表编辑
- 速度区间显示/编辑
- 抗性项编辑
- 技能列表编辑

### 数据展示能力

- `Personality`：展示基础信息、HP、速度、抗性、技能列表等
- `Passive`：展示 `requireIDList`
- `Buff`：展示 Buff 数据、本地化文本、脚本内容
- `Abnormality`：展示异常体数据、嵌套对象、模式和技能结构

### 脚本解析

- 自动识别字符串脚本字段
- 将脚本拆分为多个片段显示
- 对 `List<string>` 类型脚本字段也支持解析和展示
- 提供脚本缓存，避免重复解析

### Buff 本地化支持

- 自动加载 `custom_limbus_locale/EN/bufList` 下的本地化文件
- 自动加载 `custom_limbus_locale/EN/keywordList` 下的本地化文件
- 可在 Buff 页面直接编辑本地化字段
- 保存时同步写回本地化文件
- 删除 Buff 时会同步删除对应本地化条目

### 创建与删除

- 在 Buff 文件夹上右键可新建 Buff
- 新建时会同时生成 Buff JSON、Buff 本地化条目、Keyword 本地化条目
- 在 JSON 文件上右键可删除
- 删除时会同步清理对应本地化条目

### 保存机制

- 支持保存当前数据对象的修改
- 支持保存本地化数据
- 右侧编辑内容修改后可刷新当前视图重新渲染

---

## 如何新增“显示”

这里的“新增显示”指的是：让一个新的模型字段或新的数据结构出现在界面中，并且能够被自动编辑或展示。

### 方式一：给模型字段加 `EditableAttribute`

适合大多数普通字段。

1. 在对应 `Models/*.cs` 中新增属性
2. 给属性加上 `[Editable(...)]`
3. 选择合适的 `ControlType`：
   - `Text`
   - `Numeric`
   - `Boolean`
   - `Dropdown`
   - `Nested`
   - `List`
   - `SpeedRange`
4. 重新打开或刷新 Mod，界面会由 `EditorGenerator` 自动生成该字段的编辑控件

示例思路：

- 新增一个数值字段：`[Editable(Label = "新字段", ControlType = "Numeric", Order = 100)]`
- 新增一个下拉字段：`[Editable(Label = "状态", ControlType = "Dropdown", Options = "A,B,C", Order = 101)]`

### 方式二：新增一个完整数据块的展示逻辑

适合新增一种新的顶层数据类型。

1. 在 `Models` 中定义数据结构
2. 在 `Handlers` 中新增对应 `BaseJsonHandler<T>` 子类
3. 在 `ModScanner` 中注册新的 Handler
4. 在 `MainWindow.xaml.cs` 的 `DisplayDataByType` 中增加 `case`
5. 如果需要特殊 UI，新增对应的 `DisplayXXXData` 方法

### 方式三：新增特殊展示控件

如果某个字段不能直接用现有控件表示：

1. 在 `EditorGenerator` 中增加新的 `ControlType` 分支
2. 或在 `MainWindow.xaml.cs` 中增加专门的展示方法
3. 在模型字段上使用新类型标记

---

## 如何将另一份本地化文件合并到单文件

当前项目的本地化逻辑是“**按文件夹加载，按单个目标 JSON 文件写回**”。

### 现有行为

- 启动时会读取：
  - `custom_limbus_locale/EN/bufList`
  - `custom_limbus_locale/EN/keywordList`
- 目录里如果有多个 JSON，工具会选择一个目标文件作为写回对象
- 新建 Buff 时，会把对应条目写入当前 locale 文件
- 保存本地化时，会把缓存整体写回 `CurrentBuffLocaleFilePath` / `CurrentKeywordLocaleFilePath`

### 合并另一份本地化文件的操作思路

如果你要把另一份 locale 文件并到当前单文件中，建议按下面做：

1. 打开目标 Mod 文件夹
2. 确保 `bufList` 或 `keywordList` 目录存在
3. 让工具读取当前目录下的 locale 文件
4. 将另一份文件中的条目导入到当前缓存中
5. 点击“保存本地化”或执行保存逻辑
6. 工具会把合并后的字典写回当前目标 JSON 文件

### 关键点

- `BuffLocaleEntry` 和 `KeywordLocaleEntry` 都是字典式存储
- 保存时不会拆散成多个业务对象，而是直接序列化整个字典
- 删除条目时也会同步从对应 locale 文件中移除

---

## 目录结构概览

- `MainWindow.xaml(.cs)`：主界面与交互逻辑
- `CreateBuffDialog.xaml(.cs)`：创建 Buff 的输入对话框
- `Handlers/`：不同数据类型的 JSON 解析器
- `Models/`：数据模型、脚本模型、缓存、本地化实体
- `Services/`：扫描、编辑器生成、脚本解析

---

## 设计约束与注意事项

- `Models` 中的类需要尽量保持与游戏 JSON 结构一致
- 新增功能建议遵循 `Handler → Model → Service → UI` 的依赖方向
- 游戏数据路径不要硬编码，优先由用户选择
- `obj/` 和 `bin/` 目录属于生成内容，不要手动修改
- XAML 修改后要注意绑定路径和控件命名一致性

---

## 快速使用

1. 启动程序
2. 点击“打开Mod”选择 Mod 根目录
3. 在左侧选择文件查看内容
4. 在右侧直接编辑数据
5. 对 Buff 页面可同步编辑本地化内容
6. 需要时点击保存按钮写回文件

---

## 技术栈

- .NET 8
- WPF
- C#
- JSON 序列化 / 反序列化
