using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.0.7 城市财政与等级：
    /// ① 城市等级按常住人口五级：村落→集镇→县城→都市→大都市；
    /// ② 年度财政预算：人头税（随货币化程度与税率档位）+ 现代商税（超市/摩天楼）− 公共服务运维（消防/警察/医院/学校/公园）− 道路运维；
    ///    净额年度一次性入国库（产业产出仍归 EconomySystem，本系统只承接居民/商业税收与公共运维，避免重复计税）；
    /// ③ 税率三档（低税/标准/重税），低税增幸福、重税减幸福；财政破产（净亏且国库<50）额外减幸福；
    /// ④ 地价指数：时代×城市等级×服务覆盖×城市指标×污染×拥堵综合定价，供面板展示；
    /// 全部数值年度结算，税率档位进存档；派生指标不存档，读档后首次 OnYear/打开面板自然重算。
    /// </summary>
    public class CityFinanceSystem : GameSystemBase
    {
        // 城市等级（常住人口阈值，达到即升）
        private static readonly (int pop, string name)[] Tiers =
        {
            (120, "村落"), (350, "集镇"), (800, "县城"), (1400, "都市"), (1600, "大都市"),
        };
        public static readonly string[] TaxNames = { "低税 · 休养生息", "标准税", "重税 · 与民争利" };
        private static readonly float[] HeadTaxPerPop = { 0.04f, 0.08f, 0.14f };
        private static readonly float[] TaxHappiness = { 1.5f, 0f, -2.5f };

        // 现代商业建筑年度商税（按等级倍率）
        private static readonly Dictionary<string, float> CommerceTax = new()
        {
            {"supermarket", 16f}, {"skyscraper", 50f}, {"bank", 24f},
        };
        // 公共服务建筑年度运维
        private static readonly Dictionary<string, float> ServiceUpkeep = new()
        {
            {"fire_station", 6f}, {"police_station", 6f}, {"hospital", 7f},
            {"modern_school", 6f}, {"park", 2f},
        };
        // 道路/交通年度运维（V9.0.8 地铁站年运维 8 金）
        private static readonly Dictionary<string, float> RoadUpkeep = new()
        {
            {"road", 0.3f}, {"highway", 0.5f}, {"arterial", 0.8f},
            {"highway_modern", 1.5f}, {"interchange", 4f}, {"subway", 8f},
        };

        public int TierIndex { get; private set; }
        public string TierName = "村落";
        public int TaxLevel => Mathf.Clamp(S != null ? S.CityTaxLevel : 1, 0, 2);
        public void CycleTax() { S.CityTaxLevel = (TaxLevel + 1) % 3; Recompute(false); }

        public float HeadRevenue, CommerceRevenue, ServiceCost, RoadCost, NetAnnual, LandPrice;
        public float Treasury => S != null ? S.GetRes("gold") : 0f;
        private int _lastTier = -1;

        public override void OnYear(int year)
        {
            Recompute(true);
        }

        /// <summary>重算等级/预算/地价。apply=true 时把净额计入国库并结算幸福、升级事件。</summary>
        public void Recompute(bool apply)
        {
            if (S == null) return;
            // —— 城市等级 ——
            int tier = 0;
            for (int i = 0; i < Tiers.Length; i++) if (S.Pop >= Tiers[i].pop) tier = i;
            TierIndex = tier; TierName = Tiers[tier].name;

            // —— 收入：人头税（货币化程度随时代抬升，古代以实物为主折算很低）——
            float monet = Mathf.Clamp(0.25f + 0.15f * S.Era, 0.25f, 1.30f);
            HeadRevenue = S.Pop * HeadTaxPerPop[TaxLevel] * monet;

            CommerceRevenue = 0f; ServiceCost = 0f; RoadCost = 0f;
            int services = 0;
            foreach (var b in S.Buildings)
            {
                if (b == null || b.MapId != "home" || b.Def == null) continue;
                if (CommerceTax.TryGetValue(b.Type, out var ct)) CommerceRevenue += ct * b.LevelMult;
                if (ServiceUpkeep.TryGetValue(b.Type, out var su)) { ServiceCost += su * b.LevelMult; services++; }
                if (RoadUpkeep.TryGetValue(b.Type, out var ru)) RoadCost += ru;
            }
            NetAnnual = HeadRevenue + CommerceRevenue - ServiceCost - RoadCost;

            if (apply)
            {
                S.AddRes("gold", NetAnnual);
                float dh = TaxHappiness[TaxLevel];
                if (NetAnnual < 0f && S.GetRes("gold") < 50f) dh -= 2f;          // 财政破产：欠俸停摆
                else if (NetAnnual > 30f && TaxLevel != 2) dh += 0.5f;          // 财政宽裕
                if (dh != 0f) S.Happiness = Mathf.Clamp(S.Happiness + dh, 0f, 100f);

                if (_lastTier >= 0 && tier > _lastTier)
                    GM.AddEvent("good", "🏙️ 城市升格：" + Tiers[_lastTier].name + " → " + TierName + "（人口 " + S.Pop + "）");
            }
            _lastTier = tier;

            // —— 地价指数 ——
            float cov = 0.3f;
            if (GM.CityServices != null)
            {
                cov = (GM.CityServices.FireCov + GM.CityServices.PoliceCov +
                       GM.CityServices.HospitalCov + GM.CityServices.SchoolCov +
                       GM.CityServices.ParkCov) / 5f;
            }
            float metric = (S.CityHealth + S.CityEducation + S.CitySafety + S.CityEmployment) / 400f; // 0~1
            float cong = GM.ModernTraffic != null ? GM.ModernTraffic.AvgCongestion : 0f;
            float lp = 10f * (1f + 0.35f * S.Era) * (1f + 0.5f * tier)
                       * (1f + 0.25f * cov) * (0.6f + 0.6f * metric)
                       * (1f - Mathf.Min(0.6f, S.Pollution / 100f * 0.3f))
                       * (1f - Mathf.Min(0.2f, cong * 0.01f));
            LandPrice = Mathf.Max(1f, lp);
        }

        public string Diagnose()
        {
            Recompute(false);
            return $"[FIN] {TierName}(T{TierIndex}) pop={S.Pop} 税率[{TaxNames[TaxLevel]}] | " +
                   $"收入 人头{HeadRevenue:F0}+商税{CommerceRevenue:F0} 支出 服务{ServiceCost:F0}+道路{RoadCost:F0} 净{NetAnnual:F0} " +
                   $"库{Treasury:F0} | 地价{LandPrice:F0}";
        }
    }
}
