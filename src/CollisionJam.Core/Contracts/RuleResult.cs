using System;

namespace CollisionJam.Core
{
    /// <summary>只读规则的校验结果，不表示状态已提交。默认值为拒绝。</summary>
    public readonly struct RuleResult
    {
        public bool Succeeded { get; }
        public ResultCode Code { get; }

        private RuleResult(bool succeeded, ResultCode code)
        {
            Succeeded = succeeded;
            Code = code;
        }

        public static RuleResult Allow()
        {
            return new RuleResult(true, ResultCode.None);
        }

        /// <summary>只接受失败码；无变化请求由 Session 处理，不作为规则结果。</summary>
        public static RuleResult Reject(ResultCode code)
        {
            if (!ResultCodeUtility.IsRejection(code))
            {
                throw new ArgumentOutOfRangeException(nameof(code), code, "Expected a rejection result code.");
            }

            return new RuleResult(false, code);
        }
    }
}
