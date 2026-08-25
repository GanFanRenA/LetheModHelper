// Handlers/SkillHandler.cs

using LethelModHelper.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace LethelModHelper.Handlers
{
    /// <summary>
    /// 处理 skill 文件夹下的 JSON 文件
    /// </summary>
    public class SkillHandler : BaseJsonHandler<SkillData>
    {
        public override string TargetFolderName => "skill";

        public override string HandlerName => "技能处理器";

        protected override List<string> Validate(SkillData data)
        {
            var warnings = new List<string>();

            if (data.list == null || data.list.Count == 0)
            {
                warnings.Add("技能列表为空");
                return warnings;
            }

            foreach (var entry in data.list)
            {
                if (entry.id <= 0)
                    warnings.Add($"技能 ID {entry.id} 无效，必须大于0");

                // 简化：检查 skillData 是否存在
                if (entry.skillData == null)
                    warnings.Add($"技能 {entry.id} 没有技能数据");

                // 检查硬币
                if (entry.skillData == null || entry.skillData.Count == 0 || entry.skillData[0].coinList == null || entry.skillData[0].coinList.Count == 0)
                    warnings.Add($"技能 {entry.id} 没有硬币");
            }

            return warnings;
        }
    }
}