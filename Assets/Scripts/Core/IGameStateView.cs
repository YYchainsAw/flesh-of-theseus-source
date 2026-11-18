using System.Collections.Generic;

namespace CollisionJam.Core
{
    /// <summary>本局状态的只读查询入口，不暴露可写状态或内部集合。</summary>
    public interface IGameStateView
    {
        ViewId CurrentView { get; }

        /// <summary>是否已拥有该物品；已装备物品仍属于已拥有物品，无效 ID 返回 false。</summary>
        bool Owns(ItemId item);

        /// <summary>返回包含已装备物品的独立快照，顺序不作保证。</summary>
        /// <remarks>
        /// 每次调用都与内部集合隔离。修改返回对象或之后执行命令，
        /// 均不得让快照与内部状态相互影响。可用背包应从快照中排除已装备物品。
        /// </remarks>
        IReadOnlyList<ItemId> GetOwnedItemsSnapshot();

        /// <summary>查询槽位中的物品；空槽、None 或未定义槽位返回 null。</summary>
        ItemId? GetEquipped(BodySlot slot);

        /// <summary>查询永久谜题进度，不表示线索当前是否可见；无效 ID 返回 false。</summary>
        bool IsSolved(PuzzleId puzzle);
    }
}
