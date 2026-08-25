using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LethelModHelper.Services.Renderers.Helpers
{
    internal class RendererUIHelper
    {
        public static TextBox CreateSelectableText(string text, bool isBold = false,
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

        /// <summary>
        /// 创建带保存逻辑的统一按钮
        /// </summary>
        /// <param name="context">RendererContext 实例</param>
        /// <param name="data">要保存的数据对象</param>
        /// <param name="buttonText">按钮显示文字，默认 "💾 保存所有修改"</param>
        /// <param name="marginLeft">左边距，默认 10</param>
        /// <returns>配置好的 Button</returns>
        public static Button CreateSaveButton(RendererContext? context, object data, string filePath,
     string buttonText = "💾 保存所有修改", double marginLeft = 10)
        {
            var button = new Button
            {
                Content = buttonText,
                Margin = new Thickness(marginLeft, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5),
                VerticalAlignment = VerticalAlignment.Center
            };

            button.Click += (s, e) =>
            {
                if (context == null)
                {
                    MessageBox.Show("保存功能未初始化，请重新加载文件", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var success = context.Save(data, filePath);
                if (success)
                {
                    MessageBox.Show("保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("保存失败，请查看日志或检查文件是否被占用", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            return button;
        }
    }
}