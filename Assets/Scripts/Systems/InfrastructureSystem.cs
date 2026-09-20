using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// 基础设施系统 —— V9.0.4 工业与能源链。
    /// ①连续电网供需（取代旧“有电厂=全覆盖”开关）：供电=Σ电厂容量+奇观，用电=Σ现代建筑耗电，
    ///   PowerRatio=供/需（钳0~1）；缺电时工业产出系数 0.35+0.65×ratio，高楼居民幸福下降。
    /// ②产业链年度账本：采集(伐木/矿)→加工(作坊/冶铁/砖窑/瓷窑)→工厂(近代厂/现代厂/制造局/船政)→零售(市场/超市)，
    ///   缺环惩罚、齐套加成，商品产出乘数钳 0.7~1.25。
    /// ③污染指数：工厂/电厂/高等级道路产污、公园消解，年度趋向平衡值；高污染压民心并放大城市疫情概率。
    /// 电力/污染/产业链为年度持续态，进存档；不新增不可见资源。
    /// </summary>
    public class InfrastructureSystem : GameSystemBase
    {
        // 各类现代建筑耗电（世界单位 MW；仅 era≥6 电网时代计入）
        private static readonly Dictionary<string, float> DemandTable = new()
        {
            ["factory_modern"]=20f,["factory_pre"]=6f,["modern_arsenal"]=12f,["dockyard_modern"]=8f,
            ["data_center"]=25f,["ai_lab"]=30f,
            ["skyscraper"]=14f,["apartment"]=5f,
            ["supermarket"]=3f,["hospital"]=4f,["modern_school"]=3f,["police_station"]=2f,["fire_station"]=2f,
            ["high_speed_rail"]=15f,["airport"]=20f,["highway_modern"]=2f,["interchange"]=2f,["arterial"]=0.5f,
            ["subway"]=12f,   // V9.0.8 地铁站耗电 12MW
            ["power_plant"]=3f,["fusion_plant"]=5f,
            ["space_elevator"]=40f,["orbital_station"]=30f,["spaceship_yard"]=30f
        };
        // 污染源年负荷
        private static readonly Dictionary<string, float> PollutionSrc = new()
        {
            ["power_plant"]=4f,["factory_modern"]=3f,["factory_pre"]=2f,["modern_arsenal"]=2f,
            ["brick_works"]=1f,["porcelain_kiln"]=1f,["iron_smelter"]=1f,["dockyard_modern"]=1f,
            ["highway_modern"]=0.5f,["interchange"]=0.5f,["arterial"]=0.2f
        };

        private int _nExtract, _nProcess, _nFactory, _nRetail, _nHighrise, _nPark;
        private float _eventCd = 6f;

        /// <summary>缺电工业产出系数：满供=1，全黑=0.35（不完全停摆，留补救窗口）。</summary>
        public float BrownoutMult => (S.Era >= 6 && S.PowerDemand > 0f) ? 0.35f + 0.65f * S.PowerRatio : 1f;

        /// <summary>污染对城市疫情概率/损失的乘数：清洁(&lt;30)×0.7，污染(&gt;50)线性升至×2。</summary>
        public float EpidemicFactor()
            => S.Pollution < 30f ? 0.7f : S.Pollution > 50f ? Mathf.Min(2f, 1f + (S.Pollution - 50f) / 50f) : 1f;

        public override void Tick(float dt)
        {
            RecomputeGrid();
            // 缺电压高楼居民幸福
            if (S.Era >= 6 && S.PowerDemand > 0f && S.PowerRatio < 0.5f && _nHighrise > 0)
                S.Happiness = Mathf.Max(0f, S.Happiness - dt * 0.06f);
            // 高污染连续压民心
            if (S.Pollution > 60f)
                S.Happiness = Mathf.Max(0f, S.Happiness - dt * 0.05f * (S.Pollution - 60f) / 40f);
            _eventCd -= dt;
            if (_eventCd <= 0f) { _eventCd = 8f; MaybeWarn(); }
        }

        /// <summary>建筑建成时的特殊产能效果（电网容量改为按电厂现算，这里仅保留奇观/AI 与提示）。</summary>
        public void OnBuildingBuilt(string type)
        {
            switch (type)
            {
                case "power_plant": GM.AddEvent("good", "⚡ 发电站并网（+50 供电）"); break;
                case "fusion_plant": GM.AddEvent("good", "☢️ 聚变电站并网（+200 供电）"); break;
                case "ai_lab":
                    S.AiBonus = 0.3f; GM.AddEvent("good", "🤖 AI实验室建成！所有产出+30%"); break;
                case "dyson_swarm":
                    S.AddRes("fusion", 9999);
                    S.ElectricGrid += 10000;
                    GM.AddEvent("good", "☀️ 戴森云建成！能源无限！"); break;
            }
            RecomputeGrid();
        }

        /// <summary>每帧/年度重算电网供需比与各产业链环节计数（O(建筑数)，MaxBuildings=300，开销可忽略）。</summary>
        public void RecomputeGrid()
        {
            float supply = S.ElectricGrid;   // 戴森云等奇观注入的容量
            float demand = 0f;
            int e = 0, p = 0, f = 0, r = 0, hi = 0, park = 0;
            foreach (var b in S.Buildings)
            {
                if (b?.Def == null) continue;
                float lv = b.LevelMult;
                if (b.Type == "power_plant") supply += 50f * lv;
                else if (b.Type == "fusion_plant") supply += 200f * lv;
                if (S.Era >= 6 && DemandTable.TryGetValue(b.Type, out var dm)) demand += dm * lv;
                switch (b.Type)
                {
                    case "lumbermill": case "mine": e++; break;
                    case "workshop": case "iron_smelter": case "brick_works": case "porcelain_kiln": p++; break;
                    case "factory_pre": case "factory_modern": case "modern_arsenal": case "dockyard_modern": f++; break;
                    case "market": case "supermarket": r++; break;
                    case "skyscraper": case "apartment": hi++; break;
                    case "park": park++; break;
                }
            }
            _nExtract = e; _nProcess = p; _nFactory = f; _nRetail = r; _nHighrise = hi; _nPark = park;
            S.PowerSupply = supply; S.PowerDemand = demand;
            bool wonderPower = GM.Wonder != null && GM.Wonder.ForcePower;
            S.PowerRatio = (S.Era < 6 || demand <= 0f || wonderPower) ? 1f : Mathf.Clamp01(supply / demand);
            S.PowerCoverage = S.PowerRatio * 100f;
        }

        public override void OnYear(int year)
        {
            RecomputeGrid();
            UpdateChain();
            UpdatePollution();
        }

        /// <summary>产业链完整度乘数：缺环惩罚、相邻环节齐套加成，钳 0.7~1.25。</summary>
        private void UpdateChain()
        {
            float m = 1f;
            if (_nFactory > 0)
            {
                if (_nProcess == 0) m -= 0.15f;   // 有工厂无加工支撑
                if (_nExtract == 0) m -= 0.10f;   // 无原料采集
                if (_nRetail == 0) m -= 0.08f;    // 商品无销路
            }
            if (_nProcess > 0 && _nExtract == 0) m -= 0.10f;
            if (_nExtract > 0 && _nProcess > 0) m += 0.06f;
            if (_nProcess > 0 && _nFactory > 0) m += 0.06f;
            if (_nFactory > 0 && _nRetail > 0) m += 0.06f;
            S.IndustryChainMult = Mathf.Clamp(m, 0.7f, 1.25f);
        }

        /// <summary>污染指数年度趋向平衡值：平衡=100×污染源/(污染源+2×公园消解+4)，每年靠拢 25%。</summary>
        private void UpdatePollution()
        {
            float load = 0f;
            foreach (var b in S.Buildings)
                if (b != null && PollutionSrc.TryGetValue(b.Type, out var v)) load += v * b.LevelMult;
            float green = _nPark * 2f;
            float eq = 100f * load / (load + 2f * green + 4f);
            S.Pollution = Mathf.Lerp(S.Pollution, eq, 0.25f);
        }

        private void MaybeWarn()
        {
            if (S.Era >= 6 && S.PowerDemand > 0f && S.PowerRatio < 0.5f)
                GM.AddEvent("bad", "⚡ 电力缺口 " + Mathf.RoundToInt((1f - S.PowerRatio) * 100f) + "%，工业减产、高层居民不满");
            else if (S.Pollution >= 75f)
                GM.AddEvent("bad", "🏭 空气污染指数 " + Mathf.RoundToInt(S.Pollution) + "，健康受损、疫情风险上升");
        }

        /// <summary>V9.0.4 回归探针：电网供需/缺电系数/产业链/污染/各环节计数。</summary>
        public string Diagnose()
        {
            RecomputeGrid(); UpdateChain();
            return $"[IND] era={S.Era} supply={S.PowerSupply:F0} demand={S.PowerDemand:F0} " +
                   $"ratio={S.PowerRatio:F2} brown={BrownoutMult:F2} chain={S.IndustryChainMult:F2} " +
                   $"poll={S.Pollution:F0} E/P/F/R={_nExtract}/{_nProcess}/{_nFactory}/{_nRetail} " +
                   $"park={_nPark} highrise={_nHighrise}";
        }
    }
}
