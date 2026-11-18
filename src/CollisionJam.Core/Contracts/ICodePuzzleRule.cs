using System.Collections.Generic;

namespace CollisionJam.Core
{
    /// <summary>校验玩家实际提交的有序密码输入；不修改状态、发布通知或保留可变输入。</summary>
    /// <remarks>Session 提供独立只读输入快照；对应 PuzzleId 不能同时注册 IPuzzleRule。</remarks>
    public interface ICodePuzzleRule
    {
        RuleResult Check(IGameStateView state, IReadOnlyList<int> input);
    }
}
