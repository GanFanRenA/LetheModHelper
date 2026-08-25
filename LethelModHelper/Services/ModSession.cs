using System.Collections.Generic;

namespace LethelModHelper.Services
{
    public class ModSession
    {
        // 核心数据存储：文件路径 → 解析后的数据对象
        private readonly Dictionary<string, object> _fileDataMap = new();

        /// <summary>
        /// 存储文件解析数据
        /// </summary>
        public void SetFileData(string filePath, object data)
        {
            _fileDataMap[filePath] = data;
        }

        /// <summary>
        /// 尝试获取文件解析数据
        /// </summary>
        public bool TryGetFileData(string filePath, out object? data)
        {
            return _fileDataMap.TryGetValue(filePath, out data);
        }

        /// <summary>
        /// 清空所有缓存数据（切换 Mod 时调用）
        /// </summary>
        public void ClearFileData()
        {
            _fileDataMap.Clear();
        }

        public void RemoveFileData(string filePath)
        {
            _fileDataMap.Remove(filePath);
        }
    }
}