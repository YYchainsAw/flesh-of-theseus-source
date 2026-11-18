namespace CollisionJam.Core
{
    /// <summary>按 PuzzleId 显式注册的只读谜题条件，不负责提交进度或发放奖励。</summary>
    public interface IPuzzleRule
    {
        RuleResult Check(IGameStateView state);
    }

    /// <summary>声明解谜成功时由 Session 一次性发放的物品。</summary>
    public interface IItemRewardPuzzleRule : IPuzzleRule
    {
        ItemId? Reward { get; }
    }

    /// <summary>声明解谜成功时由 Session 从背包移除的物品。</summary>
    public interface IItemCostPuzzleRule : IPuzzleRule
    {
        ItemId Cost { get; }
    }
}
