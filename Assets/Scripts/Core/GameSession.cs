using System;
using System.Collections.Generic;

namespace CollisionJam.Core
{
    /// <summary>本局唯一的命令与状态提交入口，在 Unity 主线程使用。</summary>
    public sealed class GameSession : IGameSession
    {
        private readonly GameState state;
        private readonly IGameCommandRules commandRules;
        private readonly Dictionary<PuzzleId, IPuzzleRule> puzzleRules;
        private readonly Dictionary<PuzzleId, ICodePuzzleRule> codePuzzleRules;
        private bool isExecutingCommand;

        public IGameStateView State => state;

        /// <summary>返回已提交的永久谜题进度副本，顺序不作保证。</summary>
        public IReadOnlyList<PuzzleId> GetSolvedPuzzlesSnapshot() => state.GetSolvedPuzzlesSnapshot();

        /// <summary>完整提交后通知一次；订阅者异常向外传播，不回滚已提交状态。</summary>
        public event Action<StateChange> StateChanged;

        /// <summary>复制并验证初始数据；可选集合为 null 时按空集合处理。</summary>
        /// <remarks>
        /// 这里只校验标识格式和状态结构，不规定正式目录、槽位兼容或玩法前置条件。
        /// 重复数据视为初始化配置错误，不静默合并。缺失的运行时规则不会默认放行。
        /// </remarks>
        /// <exception cref="ArgumentException">初始标识或集合违反状态不变量。</exception>
        public GameSession(
            ViewId initialView,
            IEnumerable<ItemId> initialOwnedItems = null,
            IEnumerable<KeyValuePair<BodySlot, ItemId>> initialEquipment = null,
            IEnumerable<PuzzleId> initialSolvedPuzzles = null,
            IGameCommandRules commandRules = null,
            IEnumerable<KeyValuePair<PuzzleId, IPuzzleRule>> puzzleRules = null,
            IEnumerable<KeyValuePair<PuzzleId, ICodePuzzleRule>> codePuzzleRules = null)
        {
            state = new GameState(initialView, initialOwnedItems, initialEquipment, initialSolvedPuzzles);
            this.commandRules = commandRules;
            this.puzzleRules = CopyPuzzleRules(puzzleRules);
            this.codePuzzleRules = CopyCodePuzzleRules(codePuzzleRules, this.puzzleRules);
        }

        public CommandResult TryEquip(ItemId item, BodySlot slot)
        {
            return ExecuteCommand(() =>
            {
                if (!item.IsValid)
                {
                    return CommandResult.Rejected(ResultCode.InvalidItem);
                }

                if (!IsValidSlot(slot))
                {
                    return CommandResult.Rejected(ResultCode.InvalidSlot);
                }

                if (!state.Owns(item))
                {
                    return CommandResult.Rejected(ResultCode.ItemNotOwned);
                }

                if (state.GetEquipped(slot) == item)
                {
                    return CommandResult.NoChange(ResultCode.AlreadyEquipped);
                }

                if (state.IsEquipped(item))
                {
                    return CommandResult.Rejected(ResultCode.ItemEquippedElsewhere);
                }

                if (commandRules == null)
                {
                    return CommandResult.Rejected(ResultCode.RuleUnavailable);
                }

                var rule = commandRules.CheckEquip(state, item, slot);
                if (!rule.Succeeded)
                {
                    return CommandResult.Rejected(rule.Code);
                }

                state.Equip(item, slot);
                return NotifyChange(StateChange.Equipment);
            });
        }

        public CommandResult TryUnequip(BodySlot slot)
        {
            return ExecuteCommand(() =>
            {
                if (!IsValidSlot(slot))
                {
                    return CommandResult.Rejected(ResultCode.InvalidSlot);
                }

                if (!state.GetEquipped(slot).HasValue)
                {
                    return CommandResult.NoChange(ResultCode.SlotEmpty);
                }

                if (commandRules == null)
                {
                    return CommandResult.Rejected(ResultCode.RuleUnavailable);
                }

                var rule = commandRules.CheckUnequip(state, slot);
                if (!rule.Succeeded)
                {
                    return CommandResult.Rejected(rule.Code);
                }

                state.Unequip(slot);
                return NotifyChange(StateChange.Equipment);
            });
        }

        public CommandResult TryPickup(ItemId item)
        {
            return ExecuteCommand(() =>
            {
                if (!item.IsValid)
                {
                    return CommandResult.Rejected(ResultCode.InvalidItem);
                }

                if (state.Owns(item))
                {
                    return CommandResult.NoChange(ResultCode.AlreadyOwned);
                }

                if (commandRules == null)
                {
                    return CommandResult.Rejected(ResultCode.RuleUnavailable);
                }

                var rule = commandRules.CheckPickup(state, item);
                if (!rule.Succeeded)
                {
                    return CommandResult.Rejected(rule.Code);
                }

                state.AddOwnedItem(item);
                return NotifyChange(StateChange.World);
            });
        }

        public CommandResult TrySolve(PuzzleId puzzle)
        {
            return ExecuteCommand(() =>
            {
                if (!puzzle.IsValid)
                {
                    return CommandResult.Rejected(ResultCode.InvalidPuzzle);
                }

                if (state.IsSolved(puzzle))
                {
                    return CommandResult.NoChange(ResultCode.AlreadyCompleted);
                }

                if (!puzzleRules.TryGetValue(puzzle, out var puzzleRule))
                {
                    return CommandResult.Rejected(ResultCode.RuleUnavailable);
                }

                var rule = puzzleRule.Check(state);
                if (!rule.Succeeded)
                {
                    return CommandResult.Rejected(rule.Code);
                }

                var reward = (puzzleRule as IItemRewardPuzzleRule)?.Reward;
                if (reward.HasValue && !reward.Value.IsValid)
                {
                    return CommandResult.Rejected(ResultCode.InvalidItem);
                }

                var cost = (puzzleRule as IItemCostPuzzleRule)?.Cost;
                if (cost.HasValue)
                {
                    var item = cost.Value;
                    if (!item.IsValid)
                    {
                        return CommandResult.Rejected(ResultCode.InvalidItem);
                    }

                    if (!state.Owns(item))
                    {
                        return CommandResult.Rejected(ResultCode.ItemNotOwned);
                    }

                    if (state.IsEquipped(item))
                    {
                        return CommandResult.Rejected(ResultCode.PreconditionsNotMet);
                    }

                    state.RemoveOwnedItem(item);
                }

                state.MarkSolved(puzzle);
                if (reward.HasValue)
                {
                    state.AddOwnedItem(reward.Value);
                }

                return NotifyChange(StateChange.World);
            });
        }

        public CommandResult TrySubmitCode(PuzzleId puzzle, IReadOnlyList<int> input)
        {
            return ExecuteCommand(() =>
            {
                if (!puzzle.IsValid)
                {
                    return CommandResult.Rejected(ResultCode.InvalidPuzzle);
                }

                if (state.IsSolved(puzzle))
                {
                    return CommandResult.NoChange(ResultCode.AlreadyCompleted);
                }

                if (!codePuzzleRules.TryGetValue(puzzle, out var puzzleRule))
                {
                    return CommandResult.Rejected(ResultCode.RuleUnavailable);
                }

                if (input == null)
                {
                    return CommandResult.Rejected(ResultCode.InvalidCodeInput);
                }

                // Never expose the caller's mutable selection array to a rule or retain it in state.
                var values = new int[input.Count];
                for (var index = 0; index < values.Length; index++)
                    values[index] = input[index];
                var rule = puzzleRule.Check(state, Array.AsReadOnly(values));
                if (!rule.Succeeded)
                {
                    return CommandResult.Rejected(rule.Code);
                }

                state.MarkSolved(puzzle);
                return NotifyChange(StateChange.World);
            });
        }

        public CommandResult TryNavigate(ViewId target)
        {
            return ExecuteCommand(() =>
            {
                if (!target.IsValid)
                {
                    return CommandResult.Rejected(ResultCode.InvalidView);
                }

                if (state.CurrentView == target)
                {
                    return CommandResult.NoChange(ResultCode.AlreadyAtView);
                }

                if (commandRules == null)
                {
                    return CommandResult.Rejected(ResultCode.RuleUnavailable);
                }

                var rule = commandRules.CheckNavigate(state, target);
                if (!rule.Succeeded)
                {
                    return CommandResult.Rejected(rule.Code);
                }

                state.NavigateTo(target);
                return NotifyChange(StateChange.Navigation);
            });
        }

        private CommandResult ExecuteCommand(Func<CommandResult> command)
        {
            if (isExecutingCommand)
            {
                return CommandResult.Rejected(ResultCode.CommandInProgress);
            }

            isExecutingCommand = true;
            try
            {
                return command();
            }
            finally
            {
                isExecutingCommand = false;
            }
        }

        private CommandResult NotifyChange(StateChange change)
        {
            StateChanged?.Invoke(change);
            return CommandResult.Applied();
        }

        private static bool IsValidSlot(BodySlot slot)
        {
            return slot != BodySlot.None && Enum.IsDefined(typeof(BodySlot), slot);
        }

        private static Dictionary<PuzzleId, IPuzzleRule> CopyPuzzleRules(
            IEnumerable<KeyValuePair<PuzzleId, IPuzzleRule>> puzzleRules)
        {
            var copy = new Dictionary<PuzzleId, IPuzzleRule>();
            if (puzzleRules == null)
            {
                return copy;
            }

            foreach (var entry in puzzleRules)
            {
                if (!entry.Key.IsValid)
                {
                    throw new ArgumentException("Registered puzzle IDs must be valid.", nameof(puzzleRules));
                }

                if (entry.Value == null)
                {
                    throw new ArgumentException("Registered puzzle rules must not be null.", nameof(puzzleRules));
                }

                if (copy.ContainsKey(entry.Key))
                {
                    throw new ArgumentException("Each puzzle ID can only have one registered rule.", nameof(puzzleRules));
                }

                copy.Add(entry.Key, entry.Value);
            }

            return copy;
        }

        private static Dictionary<PuzzleId, ICodePuzzleRule> CopyCodePuzzleRules(
            IEnumerable<KeyValuePair<PuzzleId, ICodePuzzleRule>> codePuzzleRules,
            Dictionary<PuzzleId, IPuzzleRule> ordinaryRules)
        {
            var copy = new Dictionary<PuzzleId, ICodePuzzleRule>();
            if (codePuzzleRules == null)
            {
                return copy;
            }

            foreach (var entry in codePuzzleRules)
            {
                if (!entry.Key.IsValid)
                {
                    throw new ArgumentException("Registered code puzzle IDs must be valid.", nameof(codePuzzleRules));
                }

                if (entry.Value == null)
                {
                    throw new ArgumentException("Registered code puzzle rules must not be null.", nameof(codePuzzleRules));
                }

                if (copy.ContainsKey(entry.Key) || ordinaryRules.ContainsKey(entry.Key))
                {
                    throw new ArgumentException("A puzzle ID must have exactly one rule type and one registration.", nameof(codePuzzleRules));
                }

                copy.Add(entry.Key, entry.Value);
            }

            return copy;
        }
    }
}
