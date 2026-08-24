using LethelModHelper.Models;

namespace LethelModHelper.Handlers
{
    public interface IFileHandler
    {
        string HandlerName { get; }
        bool CanHandle(string folderName);
        FileParseResult Parse(string filePath);
    }
}