# AGENTS.md - AI 辅助开发指南

## 项目身份
这是 **LetheModHelper** —— 一个《边狱巴士》Mod 制作辅助工具的 WPF 桌面应用。
目标：帮助Mod作者可视化地编辑和生成游戏数据文件（Buff/异常/人格等）。

## 技术栈
- **框架**: .NET 8.0 + WPF (Windows Presentation Foundation)
- **语言**: C# (使用最新的语言特性)
- **UI模式**: MVVM (但当前未使用第三方MVVM框架，直接使用Code-Behind)
- **数据格式**: JSON (游戏数据序列化/反序列化)

## 核心架构（三层结构）
UI层 (XAML + Code-Behind)
↓ 调用
Services层 (业务逻辑/扫描/解析)
↓ 调用
Handlers层 (具体数据格式的读写)
↓ 使用
Models层 (数据实体类)


## 重要文件说明
| 文件 | 职责 |
|------|------|
| `MainWindow.xaml.cs` | 主界面逻辑，入口控制器 |
| `CreateBuffDialog.xaml.cs` | 创建Buff的对话框 |
| `ModScanner.cs` | 扫描游戏Mod目录，发现可用数据 |
| `ScriptParser.cs` | 解析游戏脚本文件 |
| `EditorGenerator.cs` | 动态生成UI编辑控件 |
| `*Handler.cs` | 每种数据类型的JSON读写器 |

## 编码规范
- **命名**: 使用PascalCase (类/方法) 和 camelCase (局部变量)
- **异步**: 使用 `async/await` 处理文件IO
- **注释**: 公共方法必须带 XML 文档注释 (`/// <summary>`)
- **XAML**: 使用 `x:Name` 而非 `Name`，命名与后台变量一致

AI 需要注意的事项
禁止修改 obj/ 和 bin/ 目录下的任何文件（自动生成）

Model类 必须保持与游戏JSON结构完全一致，不可随意改名

修改 XAML 时注意 Binding 路径是否正确

新增功能时，遵循 Handler → Model → Service → UI 的依赖方向（下层不能依赖上层）

游戏数据路径通常由用户选择，不要硬编码

给AI的回答格式要求
当被问到代码问题时，请先说明：

涉及哪些文件

修改建议（给出具体代码）

可能的副作用或影响范围

