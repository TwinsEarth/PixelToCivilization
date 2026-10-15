using System;
using PxC.Core.Persistence;

namespace PxC.Core.Services
{
    /// <summary>
    /// 全局存档版本迁移：按 version 字段逐版本升级（v0→v1→v2…）。
    /// 迁移链每步独立，Read 时自动检测并按链执行。
    /// </summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        SaveData Migrate(SaveData data);
    }

    /// <summary>
    /// 存档服务：原子写入（.tmp→正式→.bak）、FNV-1a 校验、多槽位、
    /// 5 手动 + 3 回滚槽、异步写队列（每帧 ≤1）、读时自动迁移。
    /// </summary>
    public interface ISaveService
    {
        // ---- 写入 ----
        bool Write(int slot, SaveData data);
        void WriteAsync(int slot, SaveData data, Action<bool> onDone = null);

        // ---- 读取（含自动版本迁移）----
        SaveData Read(int slot);
        void ReadAsync(int slot, Action<SaveData> onDone);

        // ---- 槽位管理 ----
        bool HasSave(int slot);
        void Delete(int slot);
        DateTime? GetTimestamp(int slot);

        // ---- 备份 / 回滚 ----
        /// <summary>用 .bak 回滚该槽，成功返回 true。</summary>
        bool Rollback(int slot);

        // ---- 校验 ----
        bool Verify(int slot, out string reason);

        // ---- 配置 ----
        int ManualSlots { get; }
        int RollbackSlots { get; }
    }
}
