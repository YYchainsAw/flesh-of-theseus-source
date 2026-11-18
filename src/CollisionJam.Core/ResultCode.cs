namespace CollisionJam.Core
{
    /// <summary>稳定的结果码，由表现层映射提示；既有数值不得重排或复用。</summary>
    public enum ResultCode
    {
        /// <summary>默认结果未初始化，按失败处理，不能默认为允许。</summary>
        Uninitialized = 0,
        None = 1,

        AlreadyOwned = 10,
        AlreadyEquipped = 11,
        SlotEmpty = 12,
        AlreadyCompleted = 13,
        AlreadyAtView = 14,

        InvalidItem = 100,
        InvalidSlot = 101,
        InvalidPuzzle = 102,
        InvalidView = 103,
        UnknownItem = 104,
        UnknownPuzzle = 105,
        UnknownView = 106,

        ItemNotOwned = 110,
        ItemEquippedElsewhere = 111,
        SlotNotAllowed = 112,

        PreconditionsNotMet = 120,
        SourceLocked = 121,
        WrongView = 122,
        RuleUnavailable = 130,
        /// <summary>规则或通知回调中嵌套提交命令，该请求被拒绝。</summary>
        CommandInProgress = 131,
        /// <summary>表现层的房间输入被打开的面板屏蔽，未向 Session 提交请求。</summary>
        InputBlocked = 140,
        /// <summary>密码输入为空、长度不符或包含非法稳定选项编号。</summary>
        InvalidCodeInput = 150,
        /// <summary>密码输入格式合法，但有序组合与配置答案不符。</summary>
        CodeMismatch = 151,
    }

    internal static class ResultCodeUtility
    {
        internal static bool IsNoChange(ResultCode code)
        {
            return code == ResultCode.AlreadyOwned
                || code == ResultCode.AlreadyEquipped
                || code == ResultCode.SlotEmpty
                || code == ResultCode.AlreadyCompleted
                || code == ResultCode.AlreadyAtView;
        }

        internal static bool IsRejection(ResultCode code)
        {
            switch (code)
            {
                case ResultCode.Uninitialized:
                case ResultCode.InvalidItem:
                case ResultCode.InvalidSlot:
                case ResultCode.InvalidPuzzle:
                case ResultCode.InvalidView:
                case ResultCode.UnknownItem:
                case ResultCode.UnknownPuzzle:
                case ResultCode.UnknownView:
                case ResultCode.ItemNotOwned:
                case ResultCode.ItemEquippedElsewhere:
                case ResultCode.SlotNotAllowed:
                case ResultCode.PreconditionsNotMet:
                case ResultCode.SourceLocked:
                case ResultCode.WrongView:
                case ResultCode.RuleUnavailable:
                case ResultCode.CommandInProgress:
                case ResultCode.InputBlocked:
                case ResultCode.InvalidCodeInput:
                case ResultCode.CodeMismatch:
                    return true;
                default:
                    return false;
            }
        }
    }
}
