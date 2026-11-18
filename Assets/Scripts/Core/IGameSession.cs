using System;
using System.Collections.Generic;

namespace CollisionJam.Core
{
    /// <summary>由 GameSession 实现的本局操作契约，在 Unity 主线程使用。</summary>
    /// <remarks>
    /// 所有校验在修改状态前完成。拒绝或无变化的请求不写状态、不发布通知；
    /// 实际变化完整提交后只发布一次通知，回调读取到最终状态。
    /// 订阅者只更新表现，不在回调中反向提交命令；嵌套请求返回 CommandInProgress。
    /// 规则异常在写入前传播；订阅者异常在提交后传播，不回滚状态。
    /// </remarks>
    public interface IGameSession
    {
        IGameStateView State { get; }

        event Action<StateChange> StateChanged;

        /// <summary>装备或替换已拥有物品；同槽同物品返回 AlreadyEquipped。</summary>
        /// <remarks>
        /// 一件物品不能占多个槽位；已在其他槽位时拒绝，不隐式移动。
        /// 替换不删除拥有物品。实际变化通知 Equipment。
        /// </remarks>
        CommandResult TryEquip(ItemId item, BodySlot slot);

        /// <summary>卸下物品但保留拥有关系；空槽返回 SlotEmpty。实际变化通知 Equipment。</summary>
        CommandResult TryUnequip(BodySlot slot);

        /// <summary>校验来源、前置条件及当前画面后拾取；已拥有返回 AlreadyOwned。</summary>
        /// <remarks>实际变化通知 World；不能仅凭标识格式有效就授予物品。</remarks>
        CommandResult TryPickup(ItemId item);

        /// <summary>校验并记录永久谜题进度；已完成返回 AlreadyCompleted。</summary>
        /// <remarks>实际变化通知 World；谜题配置的直接奖励在同一次提交中加入拥有物品。</remarks>
        CommandResult TrySolve(PuzzleId puzzle);

        /// <summary>复制输入并交由专用密码规则校验；正确时记录完成并通知一次 World。</summary>
        /// <remarks>密码规则与普通谜题规则不能使用同一 ID；重复完成返回 AlreadyCompleted。</remarks>
        CommandResult TrySubmitCode(PuzzleId puzzle, IReadOnlyList<int> input);

        /// <summary>校验后切换画面；目标为当前画面时返回 AlreadyAtView。</summary>
        /// <remarks>实际变化通知 Navigation；不重建本局状态或清空进度。</remarks>
        CommandResult TryNavigate(ViewId target);
    }
}
