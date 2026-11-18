namespace CollisionJam.Core
{
    /// <summary>实际提交后的单一变更分类；订阅者读取完整当前状态刷新。</summary>
    public enum StateChange
    {
        /// <summary>保留的默认值，不作为状态变化通知发布。</summary>
        None = 0,
        Equipment = 1,
        World = 2,
        Navigation = 3,
    }
}
