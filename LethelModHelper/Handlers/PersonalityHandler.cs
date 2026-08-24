using LethelModHelper.Core.Models;

namespace LethelModHelper.Handlers
{
    /// <summary>
    /// 处理 personality 文件夹下的 JSON
    /// </summary>
    public class PersonalityHandler : BaseJsonHandler<PersonalityData>
    {
        public override string TargetFolderName => "personality";
    }
}