using System;

namespace CollisionJam.Core
{
    /// <summary>命令结果。默认值为失败、无变化，Code 为 Uninitialized。</summary>
    public readonly struct CommandResult
    {
        public bool Succeeded { get; }
        public bool Changed { get; }
        public ResultCode Code { get; }

        private CommandResult(bool succeeded, bool changed, ResultCode code)
        {
            Succeeded = succeeded;
            Changed = changed;
            Code = code;
        }

        /// <summary>状态已经完整提交；由 Session 随后发布一次分类通知。</summary>
        public static CommandResult Applied()
        {
            return new CommandResult(true, true, ResultCode.None);
        }

        /// <summary>请求成功但没有变化；不发布通知。只接受约定的五种无变化码。</summary>
        public static CommandResult NoChange(ResultCode code)
        {
            if (!ResultCodeUtility.IsNoChange(code))
            {
                throw new ArgumentOutOfRangeException(nameof(code), code, "Expected a no-change result code.");
            }

            return new CommandResult(true, false, code);
        }

        /// <summary>请求被拒绝；状态不变，不发布通知。只接受失败码。</summary>
        /// <remarks>工厂参数用错属于编程错误；正常操作失败通过返回结果表示。</remarks>
        public static CommandResult Rejected(ResultCode code)
        {
            if (!ResultCodeUtility.IsRejection(code))
            {
                throw new ArgumentOutOfRangeException(nameof(code), code, "Expected a rejection result code.");
            }

            return new CommandResult(false, false, code);
        }
    }
}
