using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace CollisionJam.Core.Tests
{
    public sealed class GameSessionTests
    {
        private static readonly ViewId InitialView = new ViewId("test.room-a");
        private static readonly ViewId NextView = new ViewId("test.room-b");
        private static readonly ItemId FirstItem = new ItemId("test.item-a");
        private static readonly ItemId SecondItem = new ItemId("test.item-b");
        private static readonly ItemId RewardItem = new ItemId("test.reward");
        private static readonly PuzzleId TargetPuzzle = new PuzzleId("test.puzzle-target");
        private static readonly PuzzleId PreviousPuzzle = new PuzzleId("test.puzzle-previous");

        [TestCase(5)]
        [TestCase(6)]
        [TestCase(8)]
        public void RetiredOrUndefinedSlot_IsRejectedBeforeRulesWithoutChangingState(int value)
        {
            var rules = new TestCommandRules();
            var session = CreateSession(rules);
            var notifications = Observe(session);

            AssertRejected(session.TryEquip(SecondItem, (BodySlot)value), ResultCode.InvalidSlot);
            AssertRejected(session.TryUnequip((BodySlot)value), ResultCode.InvalidSlot);

            AssertInitialState(session.State);
            Assert.That(rules.CallCount, Is.Zero);
            Assert.That(notifications, Is.Empty);
        }

        [Test]
        public void LegsValueSeven_SupportsWholeSetReplacementAndSingleNotifications()
        {
            var session = new GameSession(InitialView, new[] { FirstItem, SecondItem },
                commandRules: new TestCommandRules());
            var notifications = Observe(session);

            Assert.That(session.TryEquip(FirstItem, (BodySlot)7).Changed, Is.True);
            Assert.That(session.TryEquip(SecondItem, BodySlot.Legs).Changed, Is.True);
            Assert.That(session.State.GetEquipped(BodySlot.Legs), Is.EqualTo(SecondItem));
            Assert.That(session.TryEquip(SecondItem, BodySlot.Legs).Code, Is.EqualTo(ResultCode.AlreadyEquipped));
            Assert.That(session.TryUnequip(BodySlot.Legs).Changed, Is.True);
            Assert.That(session.TryUnequip(BodySlot.Legs).Code, Is.EqualTo(ResultCode.SlotEmpty));

            Assert.That(session.State.GetEquipped(BodySlot.Legs), Is.Null);
            Assert.That(session.State.GetOwnedItemsSnapshot(), Is.EquivalentTo(new[] { FirstItem, SecondItem }));
            Assert.That(notifications, Is.EqualTo(new[]
                { StateChange.Equipment, StateChange.Equipment, StateChange.Equipment }));
        }

        [TestCase("equip")]
        [TestCase("unequip")]
        [TestCase("pickup")]
        [TestCase("solve")]
        [TestCase("navigate")]
        public void Command_WhenRuleRejects_PreservesAllStateAndPublishesNothing(string command)
        {
            var rules = new TestCommandRules { Result = RuleResult.Reject(ResultCode.PreconditionsNotMet) };
            var puzzleRule = new TestPuzzleRule { Result = RuleResult.Reject(ResultCode.PreconditionsNotMet) };
            var session = CreateSession(rules, puzzleRule);
            var notifications = Observe(session);

            var result = Invoke(session, command);

            AssertRejected(result, ResultCode.PreconditionsNotMet);
            AssertInitialState(session.State);
            Assert.That(notifications, Is.Empty);
            Assert.That(rules.CallCount + puzzleRule.CallCount, Is.EqualTo(1));
        }

        [TestCase("equip")]
        [TestCase("unequip")]
        [TestCase("pickup")]
        [TestCase("solve")]
        [TestCase("navigate")]
        public void Command_WhenRuleReturnsDefault_RejectsWithoutChangingState(string command)
        {
            var session = CreateSession(new TestCommandRules { Result = default },
                new TestPuzzleRule { Result = default });
            var notifications = Observe(session);

            AssertRejected(Invoke(session, command), ResultCode.Uninitialized);

            AssertInitialState(session.State);
            Assert.That(notifications, Is.Empty);
        }

        [TestCase("equip")]
        [TestCase("unequip")]
        [TestCase("pickup")]
        [TestCase("solve")]
        [TestCase("navigate")]
        public void Command_WithoutConfiguredRule_RejectsWithoutChangingState(string command)
        {
            var session = CreateSession();
            var notifications = Observe(session);

            AssertRejected(Invoke(session, command), ResultCode.RuleUnavailable);

            AssertInitialState(session.State);
            Assert.That(notifications, Is.Empty);
        }

        [TestCase("equip", StateChange.Equipment)]
        [TestCase("unequip", StateChange.Equipment)]
        [TestCase("pickup", StateChange.World)]
        [TestCase("solve", StateChange.World)]
        [TestCase("navigate", StateChange.Navigation)]
        public void Command_WhenAllowed_CommitsBeforeItsSingleNotification(string command, StateChange expectedChange)
        {
            var rules = new TestCommandRules
            {
                EquipCheck = (state, item, slot) =>
                {
                    AssertInitialState(state);
                    Assert.That(command, Is.EqualTo("equip"));
                    Assert.That(item, Is.EqualTo(SecondItem));
                    Assert.That(slot, Is.EqualTo(BodySlot.LeftHand));
                    return RuleResult.Allow();
                },
                UnequipCheck = (state, slot) =>
                {
                    AssertInitialState(state);
                    Assert.That(command, Is.EqualTo("unequip"));
                    Assert.That(slot, Is.EqualTo(BodySlot.LeftHand));
                    return RuleResult.Allow();
                },
                PickupCheck = (state, item) =>
                {
                    AssertInitialState(state);
                    Assert.That(command, Is.EqualTo("pickup"));
                    Assert.That(item, Is.EqualTo(RewardItem));
                    return RuleResult.Allow();
                },
                NavigateCheck = (state, target) =>
                {
                    AssertInitialState(state);
                    Assert.That(command, Is.EqualTo("navigate"));
                    Assert.That(target, Is.EqualTo(NextView));
                    return RuleResult.Allow();
                },
            };
            var puzzleRule = new TestPuzzleRule
            {
                CheckOverride = state =>
                {
                    AssertInitialState(state);
                    Assert.That(command, Is.EqualTo("solve"));
                    return RuleResult.Allow();
                },
            };
            IGameSession session = CreateSession(rules, puzzleRule);
            var notifications = new List<StateChange>();
            var callbackVerified = false;
            session.StateChanged += change =>
            {
                notifications.Add(change);
                AssertAppliedState(session.State, command);
                callbackVerified = true;
            };

            var result = Invoke(session, command);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.True);
            Assert.That(result.Code, Is.EqualTo(ResultCode.None));
            Assert.That(notifications, Is.EqualTo(new[] { expectedChange }));
            Assert.That(callbackVerified, Is.True);
            AssertAppliedState(session.State, command);
            Assert.That(rules.CallCount + puzzleRule.CallCount, Is.EqualTo(1));
        }

        [TestCase("equip", ResultCode.AlreadyEquipped)]
        [TestCase("unequip", ResultCode.SlotEmpty)]
        [TestCase("pickup", ResultCode.AlreadyOwned)]
        [TestCase("solve", ResultCode.AlreadyCompleted)]
        [TestCase("navigate", ResultCode.AlreadyAtView)]
        public void Command_WhenNoChangeIsNeeded_DoesNotCheckRulesOrNotify(string command, ResultCode expectedCode)
        {
            var rules = new TestCommandRules { Result = RuleResult.Reject(ResultCode.PreconditionsNotMet) };
            var puzzleRule = new TestPuzzleRule { Result = RuleResult.Reject(ResultCode.PreconditionsNotMet) };
            var session = CreateSession(rules, puzzleRule);
            var notifications = Observe(session);

            var result = InvokeNoChange(session, command);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Changed, Is.False);
            Assert.That(result.Code, Is.EqualTo(expectedCode));
            AssertInitialState(session.State);
            Assert.That(notifications, Is.Empty);
            Assert.That(rules.CallCount + puzzleRule.CallCount, Is.EqualTo(0));
        }

        [TestCase("equip-invalid-item", ResultCode.InvalidItem)]
        [TestCase("equip-none-slot", ResultCode.InvalidSlot)]
        [TestCase("equip-undefined-slot", ResultCode.InvalidSlot)]
        [TestCase("equip-unowned", ResultCode.ItemNotOwned)]
        [TestCase("equip-elsewhere", ResultCode.ItemEquippedElsewhere)]
        [TestCase("unequip-none-slot", ResultCode.InvalidSlot)]
        [TestCase("unequip-undefined-slot", ResultCode.InvalidSlot)]
        [TestCase("pickup-invalid-item", ResultCode.InvalidItem)]
        [TestCase("solve-invalid-puzzle", ResultCode.InvalidPuzzle)]
        [TestCase("navigate-invalid-view", ResultCode.InvalidView)]
        public void Command_WithInvalidRequest_RejectsBeforeCallingRules(string request, ResultCode expectedCode)
        {
            var rules = new TestCommandRules();
            var puzzleRule = new TestPuzzleRule();
            var session = CreateSession(rules, puzzleRule);
            var notifications = Observe(session);

            var result = InvokeInvalid(session, request);

            AssertRejected(result, expectedCode);
            AssertInitialState(session.State);
            Assert.That(notifications, Is.Empty);
            Assert.That(rules.CallCount + puzzleRule.CallCount, Is.EqualTo(0));
        }

        [Test]
        public void TryEquip_IntoEmptySlot_DoesNotRemoveExistingEquipmentOrOwnership()
        {
            var session = CreateSession(new TestCommandRules());
            var notifications = Observe(session);

            var result = session.TryEquip(SecondItem, BodySlot.RightHand);

            Assert.That(result.Changed, Is.True);
            Assert.That(session.State.GetEquipped(BodySlot.LeftHand), Is.EqualTo(FirstItem));
            Assert.That(session.State.GetEquipped(BodySlot.RightHand), Is.EqualTo(SecondItem));
            Assert.That(session.State.GetOwnedItemsSnapshot(), Is.EquivalentTo(new[] { FirstItem, SecondItem }));
            Assert.That(notifications, Is.EqualTo(new[] { StateChange.Equipment }));
        }

        [Test]
        public void RepeatedSuccessfulCommands_DoNotCommitOrNotifyAgain()
        {
            var rules = new TestCommandRules();
            var puzzleRule = new TestPuzzleRule();
            var session = CreateSession(rules, puzzleRule);
            var notifications = Observe(session);

            Assert.That(session.TryPickup(RewardItem).Changed, Is.True);
            Assert.That(session.TryPickup(RewardItem).Code, Is.EqualTo(ResultCode.AlreadyOwned));
            Assert.That(session.TryEquip(SecondItem, BodySlot.LeftHand).Changed, Is.True);
            Assert.That(session.TryEquip(SecondItem, BodySlot.LeftHand).Code, Is.EqualTo(ResultCode.AlreadyEquipped));
            Assert.That(session.TryUnequip(BodySlot.LeftHand).Changed, Is.True);
            Assert.That(session.TryUnequip(BodySlot.LeftHand).Code, Is.EqualTo(ResultCode.SlotEmpty));
            Assert.That(session.TrySolve(TargetPuzzle).Changed, Is.True);
            Assert.That(session.TrySolve(TargetPuzzle).Code, Is.EqualTo(ResultCode.AlreadyCompleted));
            Assert.That(session.TryNavigate(NextView).Changed, Is.True);
            Assert.That(session.TryNavigate(NextView).Code, Is.EqualTo(ResultCode.AlreadyAtView));

            Assert.That(notifications, Is.EqualTo(new[]
            {
                StateChange.World, StateChange.Equipment, StateChange.Equipment,
                StateChange.World, StateChange.Navigation,
            }));
            Assert.That(rules.CallCount, Is.EqualTo(4));
            Assert.That(puzzleRule.CallCount, Is.EqualTo(1));
            Assert.That(session.State.GetOwnedItemsSnapshot(),
                Is.EquivalentTo(new[] { FirstItem, SecondItem, RewardItem }));
        }

        [Test]
        public void TrySolve_WithBagItemCost_ConsumesItBeforePublishingProgressOnce()
        {
            var session = CreateSession(puzzleRule: new TestItemCostRule(SecondItem));
            var notifications = new List<StateChange>();
            session.StateChanged += change =>
            {
                notifications.Add(change);
                Assert.That(session.State.IsSolved(TargetPuzzle), Is.True);
                Assert.That(session.State.Owns(SecondItem), Is.False);
            };

            Assert.That(session.TrySolve(TargetPuzzle).Changed, Is.True);
            var repeated = session.TrySolve(TargetPuzzle);

            Assert.That(repeated.Succeeded, Is.True);
            Assert.That(repeated.Changed, Is.False);
            Assert.That(repeated.Code, Is.EqualTo(ResultCode.AlreadyCompleted));
            Assert.That(session.State.GetOwnedItemsSnapshot(), Is.EqualTo(new[] { FirstItem }));
            Assert.That(session.State.GetEquipped(BodySlot.LeftHand), Is.EqualTo(FirstItem));
            AssertRejected(session.TryEquip(SecondItem, BodySlot.LeftHand), ResultCode.ItemNotOwned);
            Assert.That(notifications, Is.EqualTo(new[] { StateChange.World }));
        }

        [TestCase(null, ResultCode.InvalidItem)]
        [TestCase("test.reward", ResultCode.ItemNotOwned)]
        [TestCase("test.item-a", ResultCode.PreconditionsNotMet)]
        public void TrySolve_WithUnavailableItemCost_PreservesStateAndEquipment(string itemId, ResultCode expectedCode)
        {
            var cost = itemId == null ? default : new ItemId(itemId);
            var session = CreateSession(puzzleRule: new TestItemCostRule(cost));
            var notifications = Observe(session);

            AssertRejected(session.TrySolve(TargetPuzzle), expectedCode);

            AssertInitialState(session.State);
            Assert.That(notifications, Is.Empty);
        }

        [Test]
        public void PickupRule_CanRequireSolvedProgressAndCurrentView_WithoutAutomaticReward()
        {
            var rules = new TestCommandRules
            {
                PickupCheck = (state, item) => !state.IsSolved(TargetPuzzle)
                    ? RuleResult.Reject(ResultCode.SourceLocked)
                    : state.CurrentView != NextView
                        ? RuleResult.Reject(ResultCode.WrongView)
                        : RuleResult.Allow(),
            };
            var session = CreateSession(rules, new TestPuzzleRule());
            var notifications = Observe(session);

            AssertRejected(session.TryPickup(RewardItem), ResultCode.SourceLocked);
            AssertInitialState(session.State);
            Assert.That(notifications, Is.Empty);

            Assert.That(session.TrySolve(TargetPuzzle).Changed, Is.True);
            Assert.That(session.State.Owns(RewardItem), Is.False, "Solving does not grant a pickup reward.");
            AssertRejected(session.TryPickup(RewardItem), ResultCode.WrongView);
            Assert.That(session.State.Owns(RewardItem), Is.False);
            Assert.That(notifications, Is.EqualTo(new[] { StateChange.World }));

            Assert.That(session.TryNavigate(NextView).Changed, Is.True);
            Assert.That(session.TryPickup(RewardItem).Changed, Is.True);
            Assert.That(session.State.Owns(RewardItem), Is.True);
            Assert.That(notifications, Is.EqualTo(new[]
            {
                StateChange.World, StateChange.Navigation, StateChange.World,
            }));
        }

        [Test]
        public void OwnedSnapshot_TakenBeforeCommands_RemainsUnchanged()
        {
            var session = CreateSession(new TestCommandRules());
            var oldSnapshot = session.State.GetOwnedItemsSnapshot();

            session.TryPickup(RewardItem);
            session.TryEquip(SecondItem, BodySlot.LeftHand);
            session.TryUnequip(BodySlot.LeftHand);
            session.TryNavigate(NextView);

            Assert.That(oldSnapshot, Is.EquivalentTo(new[] { FirstItem, SecondItem }));
            Assert.That(session.State.GetOwnedItemsSnapshot(),
                Is.EquivalentTo(new[] { FirstItem, SecondItem, RewardItem }));
        }

        [Test]
        public void PuzzleRegistry_IsCopiedBeforeCallerChangesIt()
        {
            var originalRule = new TestPuzzleRule();
            var replacementRule = new TestPuzzleRule { Result = RuleResult.Reject(ResultCode.PreconditionsNotMet) };
            var registry = new List<KeyValuePair<PuzzleId, IPuzzleRule>> { Registered(TargetPuzzle, originalRule) };
            var session = new GameSession(InitialView, puzzleRules: registry);

            registry.Clear();
            registry.Add(Registered(TargetPuzzle, replacementRule));

            Assert.That(session.TrySolve(TargetPuzzle).Changed, Is.True);
            Assert.That(originalRule.CallCount, Is.EqualTo(1));
            Assert.That(replacementRule.CallCount, Is.EqualTo(0));
            Assert.That(session.State.IsSolved(TargetPuzzle), Is.True);
        }

        [Test]
        public void PuzzleRegistry_IsEnumeratedOnce()
        {
            var registry = new SingleUseRegistry(new[] { Registered(TargetPuzzle, new TestPuzzleRule()) });
            var session = new GameSession(InitialView, puzzleRules: registry);

            Assert.That(session.TrySolve(TargetPuzzle).Changed, Is.True);
            Assert.That(session.TrySolve(TargetPuzzle).Code, Is.EqualTo(ResultCode.AlreadyCompleted));
            Assert.That(registry.EnumerationCount, Is.EqualTo(1));
        }

        [TestCase("invalid-id")]
        [TestCase("null-rule")]
        [TestCase("duplicate-id")]
        public void Constructor_RejectsInvalidPuzzleRegistry(string invalidCase)
        {
            var rule = new TestPuzzleRule();
            var registry = invalidCase == "duplicate-id"
                ? new[] { Registered(TargetPuzzle, rule), Registered(new PuzzleId(TargetPuzzle.Value), rule) }
                : invalidCase == "invalid-id"
                    ? new[] { Registered(default, rule) }
                    : new[] { Registered(TargetPuzzle, null) };

            var error = Assert.Throws<ArgumentException>(() => new GameSession(InitialView, puzzleRules: registry));

            Assert.That(error.ParamName, Is.EqualTo("puzzleRules"));
        }

        [Test]
        public void TrySolve_WithUnregisteredRule_RejectsWithoutCallingOtherRules()
        {
            var rule = new TestPuzzleRule();
            var session = CreateSession(new TestCommandRules(), rule);
            var notifications = Observe(session);

            AssertRejected(session.TrySolve(new PuzzleId("test.unregistered")), ResultCode.RuleUnavailable);

            AssertInitialState(session.State);
            Assert.That(rule.CallCount, Is.EqualTo(0));
            Assert.That(notifications, Is.Empty);
        }

        [Test]
        public void RuleCallback_CannotReenterAndEquipOneItemIntoTwoSlots()
        {
            GameSession session = null;
            var attemptedReentry = false;
            var nestedResult = default(CommandResult);
            var rules = new TestCommandRules
            {
                EquipCheck = (state, item, slot) =>
                {
                    if (!attemptedReentry)
                    {
                        attemptedReentry = true;
                        nestedResult = session.TryEquip(SecondItem, BodySlot.RightHand);
                    }

                    return RuleResult.Allow();
                },
            };
            session = CreateSession(rules);
            var notifications = Observe(session);

            Assert.That(session.TryEquip(SecondItem, BodySlot.LeftHand).Changed, Is.True);

            AssertRejected(nestedResult, ResultCode.CommandInProgress);
            Assert.That(rules.CallCount, Is.EqualTo(1));
            Assert.That(session.State.GetEquipped(BodySlot.LeftHand), Is.EqualTo(SecondItem));
            Assert.That(session.State.GetEquipped(BodySlot.RightHand), Is.Null);
            Assert.That(notifications, Is.EqualTo(new[] { StateChange.Equipment }));
        }

        [Test]
        public void StateChangedCallback_CannotSubmitAnyNestedCommand()
        {
            var rules = new TestCommandRules();
            var puzzleRule = new TestPuzzleRule();
            var session = CreateSession(rules, puzzleRule);
            var attemptedReentry = false;
            var nestedResults = new List<CommandResult>();
            var notifications = Observe(session);
            session.StateChanged += change =>
            {
                if (attemptedReentry)
                {
                    return;
                }

                attemptedReentry = true;
                nestedResults.Add(session.TryEquip(SecondItem, BodySlot.RightHand));
                nestedResults.Add(session.TryUnequip(BodySlot.LeftHand));
                nestedResults.Add(session.TryPickup(RewardItem));
                nestedResults.Add(session.TrySolve(TargetPuzzle));
                nestedResults.Add(session.TryNavigate(NextView));
            };

            Assert.That(session.TryPickup(RewardItem).Changed, Is.True);

            Assert.That(nestedResults.Count, Is.EqualTo(5));
            foreach (var result in nestedResults)
            {
                AssertRejected(result, ResultCode.CommandInProgress);
            }

            AssertAppliedState(session.State, "pickup");
            Assert.That(notifications, Is.EqualTo(new[] { StateChange.World }));
            Assert.That(rules.CallCount, Is.EqualTo(1));
            Assert.That(puzzleRule.CallCount, Is.EqualTo(0));
        }

        [Test]
        public void CommandRuleException_PropagatesWithoutStateChange_AndClearsExecutionGuard()
        {
            var deliberateFailure = new InvalidOperationException("Test rule failed.");
            var rules = new TestCommandRules { PickupCheck = (state, item) => throw deliberateFailure };
            var session = CreateSession(rules);
            var notifications = Observe(session);

            var actualFailure = Assert.Throws<InvalidOperationException>(() => session.TryPickup(RewardItem));

            Assert.That(actualFailure, Is.SameAs(deliberateFailure));
            AssertInitialState(session.State);
            Assert.That(notifications, Is.Empty);

            rules.PickupCheck = null;
            Assert.That(session.TryPickup(RewardItem).Changed, Is.True);
            Assert.That(notifications, Is.EqualTo(new[] { StateChange.World }));
        }

        [Test]
        public void PuzzleRuleException_PropagatesWithoutStateChange_AndClearsExecutionGuard()
        {
            var deliberateFailure = new InvalidOperationException("Test puzzle rule failed.");
            var rule = new TestPuzzleRule { CheckOverride = state => throw deliberateFailure };
            var session = CreateSession(new TestCommandRules(), rule);
            var notifications = Observe(session);

            var actualFailure = Assert.Throws<InvalidOperationException>(() => session.TrySolve(TargetPuzzle));

            Assert.That(actualFailure, Is.SameAs(deliberateFailure));
            AssertInitialState(session.State);
            Assert.That(notifications, Is.Empty);

            rule.CheckOverride = null;
            Assert.That(session.TrySolve(TargetPuzzle).Changed, Is.True);
            Assert.That(notifications, Is.EqualTo(new[] { StateChange.World }));
        }

        [Test]
        public void SubscriberException_PropagatesAfterCommit_AndClearsExecutionGuard()
        {
            var deliberateFailure = new InvalidOperationException("Test subscriber failed.");
            var session = CreateSession(new TestCommandRules());
            var callbackCount = 0;
            Action<StateChange> throwingSubscriber = change =>
            {
                callbackCount++;
                throw deliberateFailure;
            };
            session.StateChanged += throwingSubscriber;

            var actualFailure = Assert.Throws<InvalidOperationException>(() => session.TryPickup(RewardItem));

            Assert.That(actualFailure, Is.SameAs(deliberateFailure));
            Assert.That(callbackCount, Is.EqualTo(1));
            AssertAppliedState(session.State, "pickup");

            session.StateChanged -= throwingSubscriber;
            Assert.That(session.TryPickup(RewardItem).Code, Is.EqualTo(ResultCode.AlreadyOwned));
            Assert.That(session.TryNavigate(NextView).Changed, Is.True);
        }

        private static GameSession CreateSession(IGameCommandRules rules = null, IPuzzleRule puzzleRule = null)
        {
            return new GameSession(
                InitialView,
                new[] { FirstItem, SecondItem },
                new[] { new KeyValuePair<BodySlot, ItemId>(BodySlot.LeftHand, FirstItem) },
                new[] { PreviousPuzzle },
                commandRules: rules,
                puzzleRules: puzzleRule == null ? null : new[]
                {
                    Registered(TargetPuzzle, puzzleRule), Registered(PreviousPuzzle, puzzleRule),
                });
        }

        private static KeyValuePair<PuzzleId, IPuzzleRule> Registered(PuzzleId puzzle, IPuzzleRule rule)
        {
            return new KeyValuePair<PuzzleId, IPuzzleRule>(puzzle, rule);
        }

        private static List<StateChange> Observe(IGameSession session)
        {
            var changes = new List<StateChange>();
            session.StateChanged += changes.Add;
            return changes;
        }

        private static CommandResult Invoke(IGameSession session, string command)
        {
            switch (command)
            {
                case "equip": return session.TryEquip(SecondItem, BodySlot.LeftHand);
                case "unequip": return session.TryUnequip(BodySlot.LeftHand);
                case "pickup": return session.TryPickup(RewardItem);
                case "solve": return session.TrySolve(TargetPuzzle);
                case "navigate": return session.TryNavigate(NextView);
                default: throw new ArgumentException("Unknown test command.", nameof(command));
            }
        }

        private static CommandResult InvokeNoChange(IGameSession session, string command)
        {
            switch (command)
            {
                case "equip": return session.TryEquip(FirstItem, BodySlot.LeftHand);
                case "unequip": return session.TryUnequip(BodySlot.RightHand);
                case "pickup": return session.TryPickup(FirstItem);
                case "solve": return session.TrySolve(PreviousPuzzle);
                case "navigate": return session.TryNavigate(InitialView);
                default: throw new ArgumentException("Unknown test command.", nameof(command));
            }
        }

        private static CommandResult InvokeInvalid(IGameSession session, string request)
        {
            switch (request)
            {
                case "equip-invalid-item": return session.TryEquip(default, BodySlot.LeftHand);
                case "equip-none-slot": return session.TryEquip(SecondItem, BodySlot.None);
                case "equip-undefined-slot": return session.TryEquip(SecondItem, (BodySlot)999);
                case "equip-unowned": return session.TryEquip(RewardItem, BodySlot.LeftHand);
                case "equip-elsewhere": return session.TryEquip(FirstItem, BodySlot.RightHand);
                case "unequip-none-slot": return session.TryUnequip(BodySlot.None);
                case "unequip-undefined-slot": return session.TryUnequip((BodySlot)999);
                case "pickup-invalid-item": return session.TryPickup(default);
                case "solve-invalid-puzzle": return session.TrySolve(default);
                case "navigate-invalid-view": return session.TryNavigate(default);
                default: throw new ArgumentException("Unknown invalid test request.", nameof(request));
            }
        }

        private static void AssertRejected(CommandResult result, ResultCode expectedCode)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Changed, Is.False);
            Assert.That(result.Code, Is.EqualTo(expectedCode));
        }

        private static void AssertInitialState(IGameStateView state)
        {
            Assert.That(state.CurrentView, Is.EqualTo(InitialView));
            Assert.That(state.GetOwnedItemsSnapshot(), Is.EquivalentTo(new[] { FirstItem, SecondItem }));
            Assert.That(state.IsSolved(PreviousPuzzle), Is.True);
            Assert.That(state.IsSolved(TargetPuzzle), Is.False);
            foreach (BodySlot slot in Enum.GetValues(typeof(BodySlot)))
            {
                Assert.That(state.GetEquipped(slot),
                    slot == BodySlot.LeftHand ? Is.EqualTo(FirstItem) : Is.Null);
            }
        }

        private static void AssertAppliedState(IGameStateView state, string command)
        {
            Assert.That(state.CurrentView, Is.EqualTo(command == "navigate" ? NextView : InitialView));
            Assert.That(state.GetOwnedItemsSnapshot(), Is.EquivalentTo(command == "pickup"
                ? new[] { FirstItem, SecondItem, RewardItem }
                : new[] { FirstItem, SecondItem }));
            Assert.That(state.GetEquipped(BodySlot.LeftHand), command == "unequip"
                ? Is.Null
                : Is.EqualTo(command == "equip" ? SecondItem : FirstItem));
            Assert.That(state.GetEquipped(BodySlot.RightHand), Is.Null);
            Assert.That(state.IsSolved(PreviousPuzzle), Is.True);
            Assert.That(state.IsSolved(TargetPuzzle), Is.EqualTo(command == "solve"));
            Assert.That(state.Owns(RewardItem), Is.EqualTo(command == "pickup"));
        }

        private sealed class TestItemCostRule : IItemCostPuzzleRule
        {
            public ItemId Cost { get; }

            public TestItemCostRule(ItemId cost) => Cost = cost;

            public RuleResult Check(IGameStateView state) => RuleResult.Allow();
        }

        private sealed class SingleUseRegistry : IEnumerable<KeyValuePair<PuzzleId, IPuzzleRule>>
        {
            private readonly IEnumerable<KeyValuePair<PuzzleId, IPuzzleRule>> entries;
            internal int EnumerationCount { get; private set; }

            internal SingleUseRegistry(IEnumerable<KeyValuePair<PuzzleId, IPuzzleRule>> entries)
            {
                this.entries = entries;
            }

            public IEnumerator<KeyValuePair<PuzzleId, IPuzzleRule>> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount != 1)
                {
                    throw new InvalidOperationException("The puzzle registry may only be enumerated once.");
                }

                return entries.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
