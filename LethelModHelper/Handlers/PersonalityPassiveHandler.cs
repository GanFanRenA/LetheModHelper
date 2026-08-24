using LethelModHelper.Core.Models;

namespace LethelModHelper.Handlers
{
    /// <summary>
    /// 处理 personality_passive 文件夹下的 JSON
    /// </summary>
    public class PersonalityPassiveHandler : BaseJsonHandler<PersonalityPassiveData>
    {
        public override string TargetFolderName => "personality-passive";
    }
}