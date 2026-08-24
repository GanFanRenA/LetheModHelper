using LethelModHelper.Models;

namespace LethelModHelper.Handlers
{
    /// <summary>
    /// 处理 buff 文件夹下的 JSON 文件
    /// </summary>
    public class BuffHandler : BaseJsonHandler<BuffData>
    {
        public override string TargetFolderName => "buff";
    }
}