namespace PxC.Core.Persistence
{
    /// <summary>
    /// 插件存档段。每插件拥有一个可序列化数据对象，携带自身段版本，便于独立迁移。
    /// 实现类应为 [System.Serializable] 且字段为 JsonUtility 可序列化类型。
    /// </summary>
    public interface ISaveSegment
    {
        /// <summary>所属插件 id（与 IPlugin.Id 一致）。</summary>
        string PluginId { get; }
        /// <summary>本段数据版本（用于该插件段的独立迁移）。</summary>
        int SegmentVersion { get; }
    }

    /// <summary>
    /// 插件段迁移器：把某段从 FromVersion 升级到 ToVersion。
    /// 运行在插件内部，宿主在加载旧存档后按链逐段调用。
    /// </summary>
    public interface ISegmentMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        /// <summary>输入旧版本 JSON，输出新版本 JSON。失败应抛异常由上层回滚。</summary>
        string Migrate(string json);
    }
}
