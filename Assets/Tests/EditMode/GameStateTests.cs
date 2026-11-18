using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace CollisionJam.Core.Tests
{
    public sealed class GameStateTests
    {
        private static readonly ViewId InitialView = new ViewId("test.room-a");
        private static readonly ItemId FirstItem = new ItemId("test.item-a");
        private static readonly ItemId SecondItem = new ItemId("test.item-b");
        private static readonly PuzzleId FirstPuzzle = new PuzzleId("test.puzzle-a");

        [Test]
        public void Constructor_WithOnlyView_CreatesEmptyState()
        {
            IGameStateView state = new GameSession(InitialView).State;

            Assert.That(state.CurrentView, Is.EqualTo(InitialView));
            Assert.That(state.GetOwnedItemsSnapshot(), Is.Empty);
            Assert.That(state.Owns(FirstItem), Is.False);
            Assert.That(state.GetEquipped(BodySlot.Head), Is.Null);
            Assert.That(state.IsSolved(FirstPuzzle), Is.False);
        }

        [Test]
        public void Constructor_WithExplicitNullCollections_CreatesEmptyState()
        {
            var state = new GameSession(InitialView, null, null, null).State;

            Assert.That(state.GetOwnedItemsSnapshot(), Is.Empty);
            Assert.That(state.GetEquipped(BodySlot.LeftHand), Is.Null);
            Assert.That(state.IsSolved(FirstPuzzle), Is.False);
        }

        [Test]
        public void Constructor_WithValidData_ExposesAllFourStateCategories()
        {
            var state = new GameSession(
                InitialView,
                new[] { FirstItem, SecondItem },
                new[] { Equipped(BodySlot.LeftHand, FirstItem) },
                new[] { FirstPuzzle }).State;

            Assert.That(state.CurrentView, Is.EqualTo(InitialView));
            Assert.That(state.Owns(FirstItem), Is.True, "Equipped items remain owned.");
            Assert.That(state.Owns(SecondItem), Is.True);
            Assert.That(state.GetOwnedItemsSnapshot(), Is.EquivalentTo(new[] { FirstItem, SecondItem }));
            Assert.That(state.GetEquipped(BodySlot.LeftHand), Is.EqualTo(FirstItem));
            Assert.That(state.GetEquipped(BodySlot.RightHand), Is.Null);
            Assert.That(state.IsSolved(FirstPuzzle), Is.True);
        }

        [Test]
        public void Constructor_RejectsDefaultView()
        {
            AssertParameterError("initialView", () => new GameSession(default));
        }

        [Test]
        public void Constructor_RejectsDefaultOwnedItem()
        {
            AssertParameterError("initialOwnedItems", () =>
                new GameSession(InitialView, new[] { FirstItem, default }));
        }

        [Test]
        public void Constructor_RejectsDuplicateOwnedItemIds()
        {
            AssertParameterError("initialOwnedItems", () =>
                new GameSession(InitialView, new[] { FirstItem, new ItemId(FirstItem.Value) }));
        }

        [Test]
        public void Constructor_RejectsDefaultSolvedPuzzle()
        {
            AssertParameterError("initialSolvedPuzzles", () =>
                new GameSession(InitialView, initialSolvedPuzzles: new[] { FirstPuzzle, default }));
        }

        [Test]
        public void Constructor_RejectsDuplicateSolvedPuzzleIds()
        {
            AssertParameterError("initialSolvedPuzzles", () =>
                new GameSession(InitialView, initialSolvedPuzzles:
                    new[] { FirstPuzzle, new PuzzleId(FirstPuzzle.Value) }));
        }

        [TestCase(BodySlot.None)]
        [TestCase((BodySlot)5)]
        [TestCase((BodySlot)6)]
        [TestCase((BodySlot)999)]
        [TestCase((BodySlot)(-1))]
        public void Constructor_RejectsInvalidEquipmentSlot(BodySlot slot)
        {
            AssertParameterError("initialEquipment", () => new GameSession(
                InitialView, new[] { FirstItem }, new[] { Equipped(slot, FirstItem) }));
        }

        [Test]
        public void Constructor_RejectsTwoRetiredLegRecordsWithoutChoosingAnItem()
        {
            AssertParameterError("initialEquipment", () => new GameSession(
                InitialView, new[] { FirstItem, SecondItem }, new[]
                {
                    Equipped((BodySlot)5, FirstItem),
                    Equipped((BodySlot)6, SecondItem)
                }));
        }

        [Test]
        public void Constructor_RejectsDefaultEquippedItem()
        {
            AssertParameterError("initialEquipment", () => new GameSession(
                InitialView, new[] { FirstItem }, new[] { Equipped(BodySlot.Head, default) }));
        }

        [Test]
        public void Constructor_RejectsEquipmentWithoutOwnership()
        {
            AssertParameterError("initialEquipment", () => new GameSession(
                InitialView, initialEquipment: new[] { Equipped(BodySlot.Head, FirstItem) }));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Constructor_RejectsDuplicateSlotEvenWhenItemMatches(bool sameItem)
        {
            AssertParameterError("initialEquipment", () => new GameSession(
                InitialView,
                new[] { FirstItem, SecondItem },
                new[]
                {
                    Equipped(BodySlot.LeftHand, FirstItem),
                    Equipped(BodySlot.LeftHand, sameItem ? FirstItem : SecondItem),
                }));
        }

        [Test]
        public void Constructor_RejectsOneItemInMultipleSlots()
        {
            AssertParameterError("initialEquipment", () => new GameSession(
                InitialView,
                new[] { FirstItem },
                new[]
                {
                    Equipped(BodySlot.LeftHand, FirstItem),
                    Equipped(BodySlot.RightHand, FirstItem),
                }));
        }

        [Test]
        public void Constructor_CopiesAllInputCollections()
        {
            var items = new List<ItemId> { FirstItem };
            var equipment = new List<KeyValuePair<BodySlot, ItemId>> { Equipped(BodySlot.LeftHand, FirstItem) };
            var puzzles = new List<PuzzleId> { FirstPuzzle };
            var state = new GameSession(InitialView, items, equipment, puzzles).State;

            items.Clear();
            items.Add(SecondItem);
            equipment.Clear();
            equipment.Add(Equipped(BodySlot.RightHand, SecondItem));
            puzzles.Clear();
            puzzles.Add(new PuzzleId("test.puzzle-b"));

            Assert.That(state.GetOwnedItemsSnapshot(), Is.EquivalentTo(new[] { FirstItem }));
            Assert.That(state.Owns(SecondItem), Is.False);
            Assert.That(state.GetEquipped(BodySlot.LeftHand), Is.EqualTo(FirstItem));
            Assert.That(state.GetEquipped(BodySlot.RightHand), Is.Null);
            Assert.That(state.IsSolved(FirstPuzzle), Is.True);
            Assert.That(state.IsSolved(new PuzzleId("test.puzzle-b")), Is.False);
        }

        [Test]
        public void Constructor_WhenLaterValidationFails_DoesNotModifyAnyInputCollection()
        {
            var items = new List<ItemId> { FirstItem, SecondItem };
            var equipment = new List<KeyValuePair<BodySlot, ItemId>> { Equipped(BodySlot.LeftHand, FirstItem) };
            var puzzles = new List<PuzzleId> { FirstPuzzle, new PuzzleId(FirstPuzzle.Value) };
            var originalItems = items.ToArray();
            var originalEquipment = equipment.ToArray();
            var originalPuzzles = puzzles.ToArray();

            AssertParameterError("initialSolvedPuzzles", () => new GameSession(InitialView, items, equipment, puzzles));

            Assert.That(items, Is.EqualTo(originalItems));
            Assert.That(equipment, Is.EqualTo(originalEquipment));
            Assert.That(puzzles, Is.EqualTo(originalPuzzles));
        }

        [Test]
        public void Constructor_ConsumesInputEnumerablesOnce_AndQueriesNeverReenumerateThem()
        {
            var items = new SingleUseEnumerable<ItemId>(new[] { FirstItem });
            var equipment = new SingleUseEnumerable<KeyValuePair<BodySlot, ItemId>>(
                new[] { Equipped(BodySlot.LeftHand, FirstItem) });
            var puzzles = new SingleUseEnumerable<PuzzleId>(new[] { FirstPuzzle });

            var state = new GameSession(InitialView, items, equipment, puzzles).State;
            Assert.That(state.Owns(FirstItem), Is.True);
            Assert.That(state.GetOwnedItemsSnapshot(), Is.EquivalentTo(new[] { FirstItem }));
            Assert.That(state.GetOwnedItemsSnapshot(), Is.EquivalentTo(new[] { FirstItem }));
            Assert.That(state.GetEquipped(BodySlot.LeftHand), Is.EqualTo(FirstItem));
            Assert.That(state.IsSolved(FirstPuzzle), Is.True);
            Assert.That(items.EnumerationCount, Is.EqualTo(1));
            Assert.That(equipment.EnumerationCount, Is.EqualTo(1));
            Assert.That(puzzles.EnumerationCount, Is.EqualTo(1));
        }

        [Test]
        public void Snapshot_IfConcreteObjectAllowsModification_DoesNotChangeState()
        {
            var state = new GameSession(InitialView, new[] { FirstItem }).State;
            var snapshot = state.GetOwnedItemsSnapshot();

            TryReplaceFirstItem(snapshot, SecondItem);

            Assert.That(state.Owns(FirstItem), Is.True);
            Assert.That(state.Owns(SecondItem), Is.False);
            Assert.That(state.GetOwnedItemsSnapshot(), Is.EquivalentTo(new[] { FirstItem }));
        }

        [Test]
        public void Snapshots_AreIndependentOfEachOther()
        {
            var state = new GameSession(InitialView, new[] { FirstItem }).State;
            var firstSnapshot = state.GetOwnedItemsSnapshot();
            var secondSnapshot = state.GetOwnedItemsSnapshot();

            TryReplaceFirstItem(firstSnapshot, SecondItem);
            Assert.That(secondSnapshot, Is.EquivalentTo(new[] { FirstItem }));
            Assert.That(state.GetOwnedItemsSnapshot(), Is.EquivalentTo(new[] { FirstItem }));
        }

        [Test]
        public void Sessions_HaveIndependentStateAndSnapshots()
        {
            var firstSession = new GameSession(InitialView, new[] { FirstItem });
            var secondView = new ViewId("test.room-b");
            var secondSession = new GameSession(secondView, new[] { SecondItem },
                initialSolvedPuzzles: new[] { FirstPuzzle });

            TryReplaceFirstItem(firstSession.State.GetOwnedItemsSnapshot(), SecondItem);

            Assert.That(firstSession.State, Is.Not.SameAs(secondSession.State));
            Assert.That(firstSession.State.CurrentView, Is.EqualTo(InitialView));
            Assert.That(firstSession.State.Owns(FirstItem), Is.True);
            Assert.That(firstSession.State.Owns(SecondItem), Is.False);
            Assert.That(firstSession.State.IsSolved(FirstPuzzle), Is.False);
            Assert.That(secondSession.State.CurrentView, Is.EqualTo(secondView));
            Assert.That(secondSession.State.GetOwnedItemsSnapshot(), Is.EquivalentTo(new[] { SecondItem }));
            Assert.That(secondSession.State.IsSolved(FirstPuzzle), Is.True);
        }

        [Test]
        public void Queries_WithDefaultItemOrPuzzle_ReturnFalse()
        {
            var state = new GameSession(InitialView, new[] { FirstItem },
                initialSolvedPuzzles: new[] { FirstPuzzle }).State;

            Assert.That(state.Owns(default), Is.False);
            Assert.That(state.IsSolved(default), Is.False);
        }

        [TestCase(BodySlot.None)]
        [TestCase((BodySlot)999)]
        [TestCase((BodySlot)(-1))]
        public void GetEquipped_WithInvalidSlot_ReturnsNull(BodySlot slot)
        {
            var state = new GameSession(InitialView, new[] { FirstItem },
                new[] { Equipped(BodySlot.LeftHand, FirstItem) }).State;

            Assert.That(state.GetEquipped(slot), Is.Null);
        }

        private static KeyValuePair<BodySlot, ItemId> Equipped(BodySlot slot, ItemId item)
        {
            return new KeyValuePair<BodySlot, ItemId>(slot, item);
        }

        private static void AssertParameterError(string expectedParameter, TestDelegate operation)
        {
            var error = Assert.Throws<ArgumentException>(operation);
            Assert.That(error.ParamName, Is.EqualTo(expectedParameter));
        }

        private static void TryReplaceFirstItem(IReadOnlyList<ItemId> snapshot, ItemId replacement)
        {
            var potentiallyWritable = snapshot as IList<ItemId>;
            if (potentiallyWritable == null)
            {
                return;
            }

            try
            {
                potentiallyWritable[0] = replacement;
            }
            catch (NotSupportedException)
            {
                // Immutable snapshots are valid; mutable snapshots must still be isolated.
            }
        }

        private sealed class SingleUseEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> source;

            internal SingleUseEnumerable(IEnumerable<T> source)
            {
                this.source = source;
            }

            internal int EnumerationCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount != 1)
                {
                    throw new InvalidOperationException("This source can only be enumerated once.");
                }

                return source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
