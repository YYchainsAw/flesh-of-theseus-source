namespace CollisionJam.Core
{
    /// <summary>装备、卸下、拾取及导航的只读玩法校验，由后续任务提供具体实现。</summary>
    /// <remarks>
    /// Session 先检查基础不变量，再调用规则。规则不能修改状态、发布通知或控制场景。
    /// 默认规则结果表示拒绝；规则缺失时 Session 必须拒绝，不能默认允许。
    /// </remarks>
    public interface IGameCommandRules
    {
        /// <summary>校验物品允许槽位及装备相关约束。</summary>
        RuleResult CheckEquip(IGameStateView state, ItemId item, BodySlot slot);

        /// <summary>校验卸下相关约束，包括由玩法规定的头与头饰关系。</summary>
        RuleResult CheckUnequip(IGameStateView state, BodySlot slot);

        /// <summary>校验物品注册、来源解锁、前置条件及当前画面。</summary>
        RuleResult CheckPickup(IGameStateView state, ItemId item);

        /// <summary>校验目标画面已注册、可达及导航前置条件。</summary>
        RuleResult CheckNavigate(IGameStateView state, ViewId target);
    }
}
