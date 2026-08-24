using System.Text.Json;

namespace LethelModHelper.Services
{
    public class LocaleService
    {
        private readonly FileService _fileService;


        public LocaleService(FileService fileService)
        {
            _fileService = fileService;
        }


        public string LoadLocale(string path)
        {
            if (!_fileService.Exists(path))
            {
                return string.Empty;
            }

            return _fileService.ReadText(path);
        }


        public void SaveLocale(
            string path,
            string content)
        {
            _fileService.WriteText(
                path,
                content);
        }


        public void DeleteLocale(
            string path)
        {
            _fileService.Delete(path);
        }
    }
}