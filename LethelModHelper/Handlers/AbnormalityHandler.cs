using LethelModHelper.Core.Models;

namespace LethelModHelper.Handlers
{
    /// <summary>
    /// 处理 abnormality-unit 文件夹下的 JSON
    /// </summary>
    public class AbnormalityHandler : BaseJsonHandler<AbnormalityData>
    {
        public override string TargetFolderName => "abnormality-unit";
    }
}