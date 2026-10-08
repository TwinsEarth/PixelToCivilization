using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelToCivilization.Core
{
    /// <summary>
    /// V9.6.6 存档架构：
    /// ISaveable 契约 + FNV-1a 64 校验和 + 存档信封（写入附校验 / 读取验校验）+ 版本迁移链。
    /// 纯数据逻辑，无 Unity 场景依赖，可被 EditMode 单元测试直接驱动。
    /// </summary>

    /// <summary>可存档系统契约。注册到 SaveSystem 后，每次存档把 Serialize() 结果写入 SaveData 扩展区
    /// （ExtKeys/ExtVals），读档时逐个 Deserialize 恢复。Serialize/Deserialize 必须为纯数据：
    /// 不触碰 Unity 对象、不调用主线程专属 API（可在后台线程执行）。</summary>
    public interface ISaveable
    {
        /// <summary>系统唯一键（存档扩展区索引；同名冲突后注册覆盖先注册）</summary>
        string SaveKey { get; }
        /// <summary>把系统运行时状态序列化为 JSON 字符串；无状态返回 null/空串（不写入扩展区）。</summary>
        string Serialize();
        /// <summary>从 JSON 恢复；损坏/异常必须自行容错（记录警告并跳过，不影响读档主流程）。</summary>
        void Deserialize(string json);
    }

    /// <summary>V9.6.6 存档校验和：FNV-1a 64-bit（确定性、零依赖、后台线程可算）。
    /// 对"已清空 ChecksumHex 字段"的完整存档 JSON 计算；JsonUtility 序列化字段序稳定 → 同数据必同 hash。</summary>
    public static class SaveChecksum
    {
        public static string Fnv1a64Hex(string s)
        {
            ulong h = 14695981039346656037UL;   // FNV-1a 64 offset basis
            if (s == null) s = "";
            for (int i = 0; i < s.Length; i++)
            {
                h ^= (byte)s[i];
                h *= 1099511628211UL;           // FNV-1a 64 prime
            }
            return h.ToString("x16");
        }

        /// <summary>计算存档校验和（内部清空 ChecksumHex，不污染调用方对象）。</summary>
        public static string Compute(SaveData d)
        {
            d.ChecksumHex = "";
            return Fnv1a64Hex(JsonUtility.ToJson(d));
        }
    }

    /// <summary>V9.6.6 存档信封：写入前附加校验和（存储 JSON 含 ChecksumHex），读取时验证。
    /// 校验规则：新档（带校验和）必须 hash 精确匹配，否则视为损坏拒绝；
    /// 旧档（无校验和，V9.6.5 及以前）放行，交给版本迁移 + 结构校验（空快照拒绝）双保险。</summary>
    public static class SaveEnvelope
    {
        /// <summary>
        /// V9.7.3 存档序列化往返归一化（写入与读取共用同一口径，保证 checksum 可复现）。
        /// 背景：IL2CPP / WebGL 下 JsonUtility 对 double 的 ToJson/FromJson 往返可能落到相邻 double
        /// （实测 214.16164753772319 读回后重写为 214.1616475377232），直接对首次快照算 checksum，
        /// 读回重算必然不匹配 → 所有存档被误判损坏。归一化迭代 JSON 往返直到字符串稳定（固定点），
        /// checksum 只对该稳定点计算。返回稳定点（对象 + 规范 JSON）；含 NaN/Infinity 或不收敛返回 null。
        /// </summary>
        private sealed class Canonical
        {
            public SaveData Data;
            public string Json;
        }

        private static Canonical Canonicalize(SaveData src)
        {
            SaveData cur = src;
            for (int k = 0; k < 6; k++)
            {
                cur.ChecksumHex = "";
                string s = JsonUtility.ToJson(cur);
                // 哨兵：合法 JSON 数字不可能含 NaN/Infinity（字段名为 ASCII 标识符）
                if (s.IndexOf("NaN", StringComparison.Ordinal) >= 0 ||
                    s.IndexOf("Infinity", StringComparison.Ordinal) >= 0)
                {
                    Debug.LogWarning("[SaveEnv] 快照含 NaN/Infinity，拒绝写入（脏档防护）");
                    return null;
                }
                SaveData rt = JsonUtility.FromJson<SaveData>(s);
                if (rt == null) { Debug.LogWarning("[SaveEnv] 归一化反序列化失败"); return null; }
                rt.ChecksumHex = "";
                string s2 = JsonUtility.ToJson(rt);
                if (s2 == s) return new Canonical { Data = rt, Json = s };  // 到达固定点
                cur = rt;
            }
            Debug.LogWarning("[SaveEnv] 序列化归一化未收敛（double 往返振荡）");
            return null;
        }

        /// <summary>写入路径：snapshot → 往返归一化 → 对稳定点附加校验和 → 返回最终存储 JSON；
        /// 序列化失败、含 NaN/Infinity 或归一化不收敛返回 null（脏档防护）。</summary>
        public static string AttachChecksum(SaveData d)
        {
            try
            {
                Canonical c = Canonicalize(d);
                if (c == null) return null;
                string hex = SaveChecksum.Fnv1a64Hex(c.Json);
                c.Data.ChecksumHex = hex;
                return JsonUtility.ToJson(c.Data);
            }
            catch (Exception e) { Debug.LogWarning("[SaveEnv] attach fail: " + e.Message); return null; }
        }

        /// <summary>读取路径：存储 JSON → 往返归一化 → 验证校验和 → 通过返回 SaveData；校验失败（篡改/损坏）返回 null。
        /// 旧档（ChecksumHex 空）返回对象本身（放行，由结构校验 + 迁移处理）。</summary>
        public static SaveData Verify(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                SaveData d0 = JsonUtility.FromJson<SaveData>(json);
                if (d0 == null) return null;
                string saved = d0.ChecksumHex ?? "";
                Canonical c = Canonicalize(d0);
                if (c == null) return null;
                if (saved.Length == 0) { c.Data.ChecksumHex = saved; return c.Data; }  // 旧档（无校验字段）→ 放行
                string recompute = SaveChecksum.Fnv1a64Hex(c.Json);
                if (saved == recompute) { c.Data.ChecksumHex = saved; return c.Data; }   // 校验匹配 → 放行
                return null;                                                           // 校验不匹配 → 损坏/篡改
            }
            catch { return null; }
        }
    }

    /// <summary>V9.6.6 存档版本迁移链。SaveData.SaveSchema：0=旧档（V9.6.5 及以前）→ 3=当前。
    /// 迁移做数据级修复（安全默认前置到数据层），GameManager.Apply 保留防御兜底（双保险）。
    /// 迁移失败返回 false → 加载拒绝（避免半迁移脏数据）。</summary>
    public static class SaveVersionMigrator
    {
        public const int CurrentSchema = 3;
        public const string FormatId = "PxC-SAVE-V1";

        private static readonly List<Func<SaveData, bool>> _steps = new List<Func<SaveData, bool>>
        {
            Step0_To1,   // schema 0 → 1：格式打标（V9.6.5 及以前全为 0）
            Step1_To2,   // schema 1 → 2：经济安全默认（电力比值/产业链倍率）
            Step2_To3,   // schema 2 → 3：城市指标/人口周期律/冷冻安全默认
        };

        public static bool Step0_To1(SaveData d) { d.SaveFormat = FormatId; return true; }

        public static bool Step1_To2(SaveData d)
        {
            if (d.PowerRatio <= 0f) d.PowerRatio = 1f;
            if (d.IndustryChainMult <= 0f) d.IndustryChainMult = 1f;
            return true;
        }

        public static bool Step2_To3(SaveData d)
        {
            if (d.CityHealth <= 0f) d.CityHealth = 60f;
            if (d.CityEducation <= 0f) d.CityEducation = 40f;
            if (d.CitySafety <= 0f) d.CitySafety = 55f;
            if (d.CityEmployment <= 0f) d.CityEmployment = 85f;
            if (d.CityTaxLevel < 0 || d.CityTaxLevel > 2) d.CityTaxLevel = 1;
            if (d.LandIntegrity <= 0f) d.LandIntegrity = 1.2f;
            if (d.PeakPop <= 0f) d.PeakPop = Mathf.Max(100, d.Pop);
            if (d.CryoRemain < 0f) d.CryoRemain = 0f;
            return true;
        }

        /// <summary>把任意旧 schema 存档迁移到当前；成功返回 true（SaveSchema=CurrentSchema + MigrationChain 记录）。
        /// 已当前版本直接返回 true（幂等）。</summary>
        public static bool Migrate(SaveData d)
        {
            if (d == null) return false;
            int from = Mathf.Clamp(d.SaveSchema, 0, CurrentSchema);
            if (from == CurrentSchema) return true;
            for (int i = from; i < _steps.Count; i++)
            {
                try { if (!_steps[i](d)) return false; }
                catch (Exception e) { Debug.LogWarning("[SaveMig] step " + i + " fail: " + e.Message); return false; }
            }
            d.SaveSchema = CurrentSchema;
            d.MigrationChain = (d.MigrationChain ?? "") +
                (string.IsNullOrEmpty(d.MigrationChain) ? "" : ",") + from + "->" + CurrentSchema;
            return true;
        }
    }
}
