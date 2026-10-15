using System;
using System.Collections.Generic;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;

namespace PxC.Plugins.Military
{
    /// <summary>
    /// IMilitaryService 实现：聚合兵力 / 火力 / 征兵 / 战争状态。
    /// 战争矩阵记录每对交战的开始年份（0 = 未交战），用于和约/停战判定。
    /// 具体作战单位由 groundforce / naval 持有，本服务只做高层聚合与征兵口径。
    /// </summary>
    public sealed class MilitaryService : IMilitaryService
    {
        /// <summary>势力上限（FactionRegistry=8）。</summary>
        private const int MaxFactions = 8;

        private readonly GroundForceService _ground;
        private readonly NavalService _naval;
        private readonly IPopulationService _pop;

        /// <summary>交战开始年份矩阵：0 = 未交战，>0 = 开始的游戏年份。</summary>
        private readonly int[,] _warStartYear;

        public MilitaryService(
            GroundForceService ground, NavalService naval, IPopulationService pop)
        {
            _ground = ground;
            _naval = naval;
            _pop = pop;
            _warStartYear = new int[MaxFactions, MaxFactions];
        }

        /// <summary>
        /// 总兵力 = 士兵人口 + 地面单位数 + 船艇数。
        /// （编队/船员折算简化为单位数；精算待后续版本。）
        /// </summary>
        public int TotalSoldiers(int faction)
        {
            int soldiers = _pop != null ? _pop.ByLayer(faction, PopulationLayer.Soldier) : 0;
            int ground = _ground != null ? _ground.CountByFaction(faction) : 0;
            int naval = _naval != null ? _naval.CountByFaction(faction) : 0;
            return soldiers + ground + naval;
        }

        /// <summary>火力评分 = 地面 + 海军当前攻击力之和（取整）。</summary>
        public int Firepower(int faction)
        {
            float ground = _ground != null ? _ground.TotalFirepower(faction) : 0f;
            float naval = _naval != null ? _naval.TotalFirepower(faction) : 0f;
            return (int)Math.Round(ground + naval);
        }

        /// <summary>征兵：把 count 名劳动力转为士兵。来源不足返回 false。</summary>
        public bool TryRecruit(int faction, int count)
        {
            if (_pop == null || count <= 0) return false;
            return _pop.TryAssign(
                faction, PopulationLayer.Laborer, PopulationLayer.Soldier, count);
        }

        public bool IsAtWar(int faction, int otherFaction)
        {
            if (faction < 0 || faction >= MaxFactions ||
                otherFaction < 0 || otherFaction >= MaxFactions)
                return false;
            return _warStartYear[faction, otherFaction] > 0;
        }

        public int GetWarStartYear(int faction, int otherFaction)
        {
            if (faction < 0 || faction >= MaxFactions ||
                otherFaction < 0 || otherFaction >= MaxFactions)
                return 0;
            return _warStartYear[faction, otherFaction];
        }

        /// <summary>记录交战状态及开始年份（由 WarDeclaredEvent 接线）。</summary>
        public void DeclareWar(int faction, int otherFaction, int startYear)
        {
            if (faction < 0 || faction >= MaxFactions ||
                otherFaction < 0 || otherFaction >= MaxFactions ||
                faction == otherFaction)
                return;
            // 已交战则不覆盖开始年份（保持原始开战时间）。
            if (_warStartYear[faction, otherFaction] > 0) return;
            int year = startYear > 0 ? startYear : 1;
            _warStartYear[faction, otherFaction] = year;
            _warStartYear[otherFaction, faction] = year;
        }

        /// <summary>结束战争（和约/停战）：清除交战关系。</summary>
        public void MakePeace(int faction, int otherFaction)
        {
            if (faction < 0 || faction >= MaxFactions ||
                otherFaction < 0 || otherFaction >= MaxFactions)
                return;
            _warStartYear[faction, otherFaction] = 0;
            _warStartYear[otherFaction, faction] = 0;
        }

        /// <summary>
        /// 导出交战对（每对相邻两项，仅 i&lt;j）与对应开始年份。
        /// pairs 长度 = 2*对数；startYears 长度 = 对数。
        /// </summary>
        public int[] GetWarPairs(out int[] startYears)
        {
            var pairs = new List<int>(8);
            var years = new List<int>(8);
            for (int i = 0; i < MaxFactions; i++)
            {
                for (int j = i + 1; j < MaxFactions; j++)
                {
                    if (_warStartYear[i, j] > 0)
                    {
                        pairs.Add(i);
                        pairs.Add(j);
                        years.Add(_warStartYear[i, j]);
                    }
                }
            }
            startYears = years.ToArray();
            return pairs.ToArray();
        }

        /// <summary>从交战对 + 开始年份恢复战争状态。</summary>
        public void RestoreWarPairs(int[] pairs, int[] startYears)
        {
            if (pairs == null) return;
            for (int k = 0; k + 1 < pairs.Length; k += 2)
            {
                int year = (startYears != null && k / 2 < startYears.Length)
                    ? startYears[k / 2]
                    : 1;
                DeclareWar(pairs[k], pairs[k + 1], year);
            }
        }
    }
}
