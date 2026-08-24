using LethelModHelper.Models;

namespace LethelModHelper.Handlers
{
    /// <summary>
    /// 处理 passive 文件夹下的 JSON
    /// </summary>
    public class PassiveHandler : BaseJsonHandler<PassiveData>
    {
        public override string TargetFolderName => "passive";
    }
}
