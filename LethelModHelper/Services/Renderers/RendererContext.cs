using System;

namespace LethelModHelper.Services.Renderers
{
    public class RendererContext
    {
        private readonly ModDataService _dataService;

        public RendererContext(ModDataService dataService)
        {
            _dataService = dataService;
        }

        /// <summary>
        /// 保存数据到指定文件路径（推荐使用此方法）
        /// </summary>
        public bool Save(object data, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                System.Diagnostics.Debug.WriteLine("❌ 保存失败: 文件路径为空");
                return false;
            }

            try
            {
                _dataService.Save(filePath, data);
                System.Diagnostics.Debug.WriteLine($"✅ 保存成功: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 保存失败: {ex.Message}");
                return false;
            }
        }
    }
}