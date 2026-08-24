using System.IO;
using System.Text.Json;
//负责文件的读取、写入、删除等操作
namespace LethelModHelper.Services
{
    public class FileService
    {

        public string ReadText(string path)
        {
            return File.ReadAllText(path);
        }


        public void WriteText(string path, string content)
        {
            File.WriteAllText(path, content);
        }


        public void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }


        public bool Exists(string path)
        {
            return File.Exists(path);
        }


        public void SaveJson<T>(string path, T data)
        {
            var json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            WriteText(path, json);
        }

    }
}