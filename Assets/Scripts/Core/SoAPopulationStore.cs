using System.Text;
using UnityEngine;

namespace PixelToCivilization.Core
{
    /// <summary>
    /// V9.7.0 数据导向人口聚合仓（SoA 连续数组）。
    /// 面向文明模拟"人口/资源/势力"大规模数据：把人口按
    /// 势力 × 年龄段(0幼/1壮/2老) × 职业大类(0农/1工/2商/3文/4军) 聚合进连续 int 桶，
    /// 缓存命中率高、零对象分配，供性能监控/平衡探针/未来 ECS 迁移读取。
    /// 兼容铁律：本仓只做"同源聚合 + 只读统计"，不改造现有模拟写入路径；
    /// 数据源由 GameManager 每 0.5s 喂入（Feed），同一时刻与 State 人口同源一致。
    /// </summary>
    public static class SoAPopulationStore
    {
        public const int MaxFactions = 8;   // 势力桶
        public const int AgeBands = 3;      // 0=幼年 1=壮年 2=老年
        public const int JobClasses = 5;    // 0=农业 1=工业 2=商业 3=文化 4=军事

        static readonly int[] _pop = new int[MaxFactions * AgeBands * JobClasses];
        static int _total;
        static long _lastFeedUtc;
        static int _feedCount;

        /// <summary>聚合桶索引。</summary>
        public static int Index(int faction, int ageBand, int jobClass)
        {
            return (faction * AgeBands + ageBand) * JobClasses + jobClass;
        }

        /// <summary>喂入一行人口（faction 归一化 0..MaxFactions-1；越界自动钳制）。</summary>
        public static void FeedOne(int faction, int ageBand, int jobClass, int amount)
        {
            if (amount <= 0) return;
            int f = Mathf.Clamp(faction, 0, MaxFactions - 1);
            int a = Mathf.Clamp(ageBand, 0, AgeBands - 1);
            int j = Mathf.Clamp(jobClass, 0, JobClasses - 1);
            int i = Index(f, a, j);
            _pop[i] += amount;
            _total += amount;
        }

        /// <summary>重置并喂入一批（每次聚合周期先 Reset 再 Feed）。</summary>
        public static void Reset()
        {
            for (int i = 0; i < _pop.Length; i++) _pop[i] = 0;
            _total = 0;
            _lastFeedUtc = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _feedCount++;
        }

        /// <summary>全量人口（聚合后）。</summary>
        public static int Total => _total;

        /// <summary>某势力人口合计。</summary>
        public static int FactionTotal(int faction)
        {
            int f = Mathf.Clamp(faction, 0, MaxFactions - 1), sum = 0;
            int baseIdx = f * AgeBands * JobClasses;
            for (int i = 0; i < AgeBands * JobClasses; i++) sum += _pop[baseIdx + i];
            return sum;
        }

        /// <summary>某年龄段人口合计（0=幼 1=壮 2=老）。</summary>
        public static int BandTotal(int ageBand)
        {
            int a = Mathf.Clamp(ageBand, 0, AgeBands - 1), sum = 0;
            for (int f = 0; f < MaxFactions; f++)
                for (int j = 0; j < JobClasses; j++) sum += _pop[Index(f, a, j)];
            return sum;
        }

        /// <summary>某职业大类人口合计。</summary>
        public static int JobTotal(int jobClass)
        {
            int j = Mathf.Clamp(jobClass, 0, JobClasses - 1), sum = 0;
            for (int f = 0; f < MaxFactions; f++)
                for (int a = 0; a < AgeBands; a++) sum += _pop[Index(f, a, j)];
            return sum;
        }

        /// <summary>探针：连续数组聚合结果（faction0 分段 + 全量 + 职业分布）。</summary>
        public static string Probe()
        {
            var sb = new StringBuilder();
            sb.Append("soa:total=").Append(_total);
            sb.Append(" f0=").Append(FactionTotal(0));
            sb.Append(" bands=").Append(BandTotal(0)).Append('/').Append(BandTotal(1)).Append('/').Append(BandTotal(2));
            sb.Append(" jobs=").Append(JobTotal(0)).Append('/').Append(JobTotal(1)).Append('/').Append(JobTotal(2))
              .Append('/').Append(JobTotal(3)).Append('/').Append(JobTotal(4));
            sb.Append(" feed=").Append(_feedCount);
            return sb.ToString();
        }
    }
}
