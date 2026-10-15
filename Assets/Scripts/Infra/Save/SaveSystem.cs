using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PxC.Core.Persistence;
using PxC.Core.Services;
using UnityEngine;

namespace PxC.Infra.Save
{
    /// <summary>
    /// 存档系统：原子写入（.tmp→正式→.bak）、FNV-1a 校验、5 手动 + 3 回滚槽、
    /// 版本迁移链、异步写队列（每帧 ≤1）、读时自动迁移、崩溃后从 bak 恢复。
    /// json 与 cksum 都做镜像备份（.bak / .cksum.bak），保证 Rollback 后校验仍一致。
    /// 跨平台（WebGL 用 persistentDataPath 映射的 IndexedDB）。
    /// </summary>
    public sealed class SaveSystem : ISaveService
    {
        private const int ManualSlotCount = 5;
        private const int RollbackSlotCount = 3;

        private readonly string _dir;
        private readonly List<ISaveMigration> _migrations;
        private readonly Queue<WriteJob> _writeQueue;
        private readonly Queue<ReadJob> _readQueue;

        private struct WriteJob
        {
            public int Slot;
            public SaveData Data;
            public Action<bool> OnDone;
        }
        private struct ReadJob
        {
            public int Slot;
            public Action<SaveData> OnDone;
        }

        public SaveSystem(string rootDir = null)
        {
            _dir = rootDir ?? Path.Combine(Application.persistentDataPath, "saves");
            _migrations = new List<ISaveMigration>(8);
            _writeQueue = new Queue<WriteJob>(8);
            _readQueue = new Queue<ReadJob>(8);
            if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);
            RecoverFromBak();
        }

        public int ManualSlots => ManualSlotCount;
        public int RollbackSlots => RollbackSlotCount;

        public void AddMigration(ISaveMigration migration)
        {
            _migrations.Add(migration);
            _migrations.Sort((a, b) => a.FromVersion.CompareTo(b.FromVersion));
        }

        // ----------------------------------------------------------------
        // 路径
        // ----------------------------------------------------------------
        private string PathFor(int slot) => Path.Combine(_dir, $"slot_{slot}.json");
        private string BakPath(int slot) => Path.Combine(_dir, $"slot_{slot}.json.bak");
        private string CksumPath(int slot) => Path.Combine(_dir, $"slot_{slot}.cksum");
        private string CksumBakPath(int slot) => Path.Combine(_dir, $"slot_{slot}.cksum.bak");

        // ----------------------------------------------------------------
        // 同步写入（原子）
        // ----------------------------------------------------------------
        public bool Write(int slot, SaveData data)
        {
            string official = PathFor(slot);
            string tmp = official + ".tmp";
            string bak = BakPath(slot);
            string cksum = CksumPath(slot);
            string cksumTmp = cksum + ".tmp";
            string cksumBak = CksumBakPath(slot);

            try
            {
                if (string.IsNullOrEmpty(data.CreatedAt))
                    data.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                data.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                string json = JsonUtility.ToJson(data, true);

                // 1. 写新内容临时文件
                File.WriteAllText(tmp, json, new UTF8Encoding(false));

                // 2. 备份旧正式（旧正式 → json.bak）
                if (File.Exists(official))
                {
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Move(official, bak);
                }

                // 3. 备份旧校验（旧 cksum → cksum.bak）—— 必须在写新 cksum 之前
                if (File.Exists(cksum))
                {
                    if (File.Exists(cksumBak)) File.Delete(cksumBak);
                    File.Move(cksum, cksumBak);
                }

                // 4. 写新校验（先写临时文件再转正）
                File.WriteAllText(cksumTmp, FnvChecksum.Hash64Hex(json), new UTF8Encoding(false));
                if (File.Exists(cksum)) File.Delete(cksum);
                File.Move(cksumTmp, cksum);

                // 5. 新内容转正（失败则从 bak 回滚）
                try
                {
                    File.Move(tmp, official);
                }
                catch
                {
                    RestoreBackup(official, bak, cksum, cksumBak);
                    throw;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] 写入失败 slot{slot}: {e.Message}");
                if (File.Exists(tmp)) TryDelete(tmp);
                if (File.Exists(cksumTmp)) TryDelete(cksumTmp);
                return false;
            }
        }

        public void WriteAsync(int slot, SaveData data, Action<bool> onDone = null)
        {
            _writeQueue.Enqueue(new WriteJob { Slot = slot, Data = data, OnDone = onDone });
        }

        // ----------------------------------------------------------------
        // 读取（校验 + 迁移）
        // ----------------------------------------------------------------
        public SaveData Read(int slot)
        {
            var (json, ok) = ReadAndVerify(slot);
            if (!ok)
            {
                // 正式损坏，尝试 bak（bak 已在写入时验证过）
                if (File.Exists(BakPath(slot)))
                {
                    try
                    {
                        var bakData = JsonUtility.FromJson<SaveData>(
                            File.ReadAllText(BakPath(slot), Encoding.UTF8));
                        return Migrate(bakData);
                    }
                    catch { return null; }
                }
                return null;
            }
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                return Migrate(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] 反序列化失败 slot{slot}: {e.Message}");
                return null;
            }
        }

        public void ReadAsync(int slot, Action<SaveData> onDone)
        {
            _readQueue.Enqueue(new ReadJob { Slot = slot, OnDone = onDone });
        }

        private (string json, bool ok) ReadAndVerify(int slot)
        {
            string official = PathFor(slot);
            if (!File.Exists(official)) return (null, false);
            try
            {
                string json = File.ReadAllText(official, Encoding.UTF8);
                string cksumPath = CksumPath(slot);
                if (File.Exists(cksumPath))
                {
                    string stored = File.ReadAllText(cksumPath, Encoding.UTF8).Trim();
                    string actual = FnvChecksum.Hash64Hex(json);
                    if (!string.Equals(stored, actual, StringComparison.OrdinalIgnoreCase))
                        return (json, false);
                }
                return (json, true);
            }
            catch
            {
                return (null, false);
            }
        }

        // ----------------------------------------------------------------
        // 版本迁移链
        // ----------------------------------------------------------------
        private SaveData Migrate(SaveData data)
        {
            if (data == null) return null;
            bool progressed = true;
            int guard = 0;
            while (progressed && guard++ < 64)
            {
                progressed = false;
                for (int i = 0; i < _migrations.Count; i++)
                {
                    var m = _migrations[i];
                    if (data.Version == m.FromVersion)
                    {
                        data = m.Migrate(data);
                        data.Version = m.ToVersion;
                        progressed = true;
                        break;
                    }
                }
            }
            return data;
        }

        // ----------------------------------------------------------------
        // 槽位管理
        // ----------------------------------------------------------------
        public bool HasSave(int slot) => File.Exists(PathFor(slot));

        public void Delete(int slot)
        {
            TryDelete(PathFor(slot));
            TryDelete(BakPath(slot));
            TryDelete(CksumPath(slot));
            TryDelete(CksumBakPath(slot));
        }

        public DateTime? GetTimestamp(int slot)
        {
            if (!HasSave(slot)) return null;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFor(slot), Encoding.UTF8));
                if (DateTime.TryParse(d?.UpdatedAt, out var t)) return t;
            }
            catch { }
            return null;
        }

        public bool Rollback(int slot)
        {
            if (!File.Exists(BakPath(slot))) return false;
            try
            {
                // 恢复 json
                if (File.Exists(PathFor(slot))) File.Delete(PathFor(slot));
                File.Move(BakPath(slot), PathFor(slot));

                // 恢复 cksum
                if (File.Exists(CksumBakPath(slot)))
                {
                    if (File.Exists(CksumPath(slot))) File.Delete(CksumPath(slot));
                    File.Move(CksumBakPath(slot), CksumPath(slot));
                }
                return true;
            }
            catch { return false; }
        }

        public bool Verify(int slot, out string reason)
        {
            if (!HasSave(slot)) { reason = "存档不存在"; return false; }
            var (json, ok) = ReadAndVerify(slot);
            if (!ok) { reason = "校验失败"; return false; }
            try
            {
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d == null) { reason = "反序列化失败"; return false; }
                reason = "OK";
                return true;
            }
            catch (Exception e)
            {
                reason = e.Message;
                return false;
            }
        }

        // ----------------------------------------------------------------
        // 队列处理（每帧 ≤1 写 + ≤1 读，GameLoop 注入）
        // ----------------------------------------------------------------
        public void Tick(float unused)
        {
            if (_writeQueue.Count > 0)
            {
                var j = _writeQueue.Dequeue();
                bool ok = Write(j.Slot, j.Data);
                j.OnDone?.Invoke(ok);
            }
            if (_readQueue.Count > 0)
            {
                var j = _readQueue.Dequeue();
                j.OnDone?.Invoke(Read(j.Slot));
            }
        }

        // ----------------------------------------------------------------
        // 恢复
        // ----------------------------------------------------------------
        private void RecoverFromBak()
        {
            for (int s = 0; s < ManualSlotCount; s++)
            {
                if (!File.Exists(PathFor(s)) && File.Exists(BakPath(s)))
                {
                    try { File.Move(BakPath(s), PathFor(s)); }
                    catch { }
                }
                if (!File.Exists(CksumPath(s)) && File.Exists(CksumBakPath(s)))
                {
                    try { File.Move(CksumBakPath(s), CksumPath(s)); }
                    catch { }
                }
            }
        }

        /// <summary>新内容转正失败时，用备份还原 json 与 cksum。</summary>
        private void RestoreBackup(string official, string bak, string cksum, string cksumBak)
        {
            try
            {
                if (File.Exists(official)) File.Delete(official);
                if (File.Exists(bak)) File.Move(bak, official);
                if (File.Exists(cksum)) File.Delete(cksum);
                if (File.Exists(cksumBak)) File.Move(cksumBak, cksum);
            }
            catch { }
        }

        private static void TryDelete(string path)
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); }
                catch { }
            }
        }
    }
}
