using System.Collections.Generic;

namespace LethelModHelper.Models
{
    public class FileParseResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public object? Data { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }
}