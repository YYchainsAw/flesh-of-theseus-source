using System;
using System.Collections.Generic;

namespace CollisionJam.Core
{
    /// <summary>由 Session 独占持有的本局状态，不向其他程序集暴露实现类型。</summary>
    internal sealed class GameState : IGameStateView
    {
        private readonly HashSet<ItemId> ownedItems;
        private readonly Dictionary<BodySlot, ItemId> equipment;
        private readonly HashSet<PuzzleId> solvedPuzzles;

        public ViewId CurrentView { get; private set; }

        internal GameState(
            ViewId initialView,
            IEnumerable<ItemId> initialOwnedItems,
            IEnumerable<KeyValuePair<BodySlot, ItemId>> initialEquipment,
            IEnumerable<PuzzleId> initialSolvedPuzzles)
        {
            if (!initialView.IsValid)
            {
                throw new ArgumentException("The initial view ID must be valid.", nameof(initialView));
            }

            ownedItems = CopyOwnedItems(initialOwnedItems);
            equipment = CopyEquipment(initialEquipment, ownedItems);
            solvedPuzzles = CopySolvedPuzzles(initialSolvedPuzzles);
            CurrentView = initialView;
        }

        public bool Owns(ItemId item)
        {
            return item.IsValid && ownedItems.Contains(item);
        }

        public IReadOnlyList<ItemId> GetOwnedItemsSnapshot()
        {
            var snapshot = new ItemId[ownedItems.Count];
            ownedItems.CopyTo(snapshot);
            return snapshot;
        }

        public ItemId? GetEquipped(BodySlot slot)
        {
            return equipment.TryGetValue(slot, out var item) ? item : (ItemId?)null;
        }

        internal bool IsEquipped(ItemId item)
        {
            return equipment.ContainsValue(item);
        }

        public bool IsSolved(PuzzleId puzzle)
        {
            return puzzle.IsValid && solvedPuzzles.Contains(puzzle);
        }

        internal IReadOnlyList<PuzzleId> GetSolvedPuzzlesSnapshot()
        {
            var snapshot = new PuzzleId[solvedPuzzles.Count];
            solvedPuzzles.CopyTo(snapshot);
            return snapshot;
        }

        // 以下入口仅供 Session 在完成全部校验后提交，不发布事件。
        internal void AddOwnedItem(ItemId item)
        {
            ownedItems.Add(item);
        }

        internal void RemoveOwnedItem(ItemId item)
        {
            ownedItems.Remove(item);
        }

        internal void Equip(ItemId item, BodySlot slot)
        {
            equipment[slot] = item;
        }

        internal void Unequip(BodySlot slot)
        {
            equipment.Remove(slot);
        }

        internal void MarkSolved(PuzzleId puzzle)
        {
            solvedPuzzles.Add(puzzle);
        }

        internal void NavigateTo(ViewId target)
        {
            CurrentView = target;
        }

        private static HashSet<ItemId> CopyOwnedItems(IEnumerable<ItemId> initialOwnedItems)
        {
            var items = new HashSet<ItemId>();
            if (initialOwnedItems == null)
            {
                return items;
            }

            foreach (var item in initialOwnedItems)
            {
                if (!item.IsValid)
                {
                    throw new ArgumentException("Initial owned item IDs must be valid.", nameof(initialOwnedItems));
                }

                if (!items.Add(item))
                {
                    throw new ArgumentException("Initial owned items must be unique.", nameof(initialOwnedItems));
                }
            }

            return items;
        }

        private static Dictionary<BodySlot, ItemId> CopyEquipment(
            IEnumerable<KeyValuePair<BodySlot, ItemId>> initialEquipment,
            HashSet<ItemId> ownedItems)
        {
            var slots = new Dictionary<BodySlot, ItemId>();
            if (initialEquipment == null)
            {
                return slots;
            }

            var equippedItems = new HashSet<ItemId>();
            foreach (var entry in initialEquipment)
            {
                if (entry.Key == BodySlot.None || !Enum.IsDefined(typeof(BodySlot), entry.Key))
                {
                    throw new ArgumentException("Initial equipment slots must be valid.", nameof(initialEquipment));
                }

                if (!entry.Value.IsValid)
                {
                    throw new ArgumentException("Initial equipped item IDs must be valid.", nameof(initialEquipment));
                }

                if (!ownedItems.Contains(entry.Value))
                {
                    throw new ArgumentException("Initial equipped items must be owned.", nameof(initialEquipment));
                }

                if (slots.ContainsKey(entry.Key))
                {
                    throw new ArgumentException("Each initial equipment slot must occur only once.", nameof(initialEquipment));
                }

                if (!equippedItems.Add(entry.Value))
                {
                    throw new ArgumentException("An item cannot occupy multiple initial equipment slots.", nameof(initialEquipment));
                }

                slots.Add(entry.Key, entry.Value);
            }

            return slots;
        }

        private static HashSet<PuzzleId> CopySolvedPuzzles(IEnumerable<PuzzleId> initialSolvedPuzzles)
        {
            var puzzles = new HashSet<PuzzleId>();
            if (initialSolvedPuzzles == null)
            {
                return puzzles;
            }

            foreach (var puzzle in initialSolvedPuzzles)
            {
                if (!puzzle.IsValid)
                {
                    throw new ArgumentException("Initial solved puzzle IDs must be valid.", nameof(initialSolvedPuzzles));
                }

                if (!puzzles.Add(puzzle))
                {
                    throw new ArgumentException("Initial solved puzzles must be unique.", nameof(initialSolvedPuzzles));
                }
            }

            return puzzles;
        }
    }
}
