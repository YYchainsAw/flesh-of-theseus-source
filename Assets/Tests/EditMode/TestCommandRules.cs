using System;

namespace CollisionJam.Core.Tests
{
    internal sealed class TestCommandRules : IGameCommandRules
    {
        internal RuleResult Result { get; set; } = RuleResult.Allow();
        internal Func<IGameStateView, ItemId, BodySlot, RuleResult> EquipCheck { get; set; }
        internal Func<IGameStateView, BodySlot, RuleResult> UnequipCheck { get; set; }
        internal Func<IGameStateView, ItemId, RuleResult> PickupCheck { get; set; }
        internal Func<IGameStateView, ViewId, RuleResult> NavigateCheck { get; set; }
        internal int CallCount { get; private set; }
        internal IGameStateView LastState { get; private set; }

        public RuleResult CheckEquip(IGameStateView state, ItemId item, BodySlot slot)
        {
            CallCount++;
            LastState = state;
            return EquipCheck?.Invoke(state, item, slot) ?? Result;
        }

        public RuleResult CheckUnequip(IGameStateView state, BodySlot slot)
        {
            CallCount++;
            LastState = state;
            return UnequipCheck?.Invoke(state, slot) ?? Result;
        }

        public RuleResult CheckPickup(IGameStateView state, ItemId item)
        {
            CallCount++;
            LastState = state;
            return PickupCheck?.Invoke(state, item) ?? Result;
        }

        public RuleResult CheckNavigate(IGameStateView state, ViewId target)
        {
            CallCount++;
            LastState = state;
            return NavigateCheck?.Invoke(state, target) ?? Result;
        }
    }

    internal sealed class TestPuzzleRule : IPuzzleRule
    {
        internal RuleResult Result { get; set; } = RuleResult.Allow();
        internal Func<IGameStateView, RuleResult> CheckOverride { get; set; }
        internal int CallCount { get; private set; }
        internal IGameStateView LastState { get; private set; }

        public RuleResult Check(IGameStateView state)
        {
            CallCount++;
            LastState = state;
            return CheckOverride?.Invoke(state) ?? Result;
        }
    }
}
