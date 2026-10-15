namespace PxC.Core.Capabilities
{
    /// <summary>
    /// 插件能力标识。插件遵循零默认原则：未显式申请并被授权的能力，运行时一律拒绝。
    /// </summary>
    public enum Capability
    {
        None = 0,

        // ---- 资源类 ----
        FileSystemRead = 10,
        FileSystemWrite = 11,
        IndexedDB = 12,
        Clipboard = 13,
        Download = 14,

        // ---- 网络类 ----
        Network = 20,          // 配合 CapabilityClaim.Scope 限定白名单域名
        CloudSync = 21,

        // ---- 功能类 ----
        SpawnEntity = 30,
        CreateProjectile = 31,
        UI = 32,
        Audio = 33,
        TimeControl = 34,
        Save = 35,

        // ---- 系统类（默认全禁，仅桌面/授权）----
        AssemblyLoad = 90,
        Process = 91,
        Unsafe = 92
    }
}
