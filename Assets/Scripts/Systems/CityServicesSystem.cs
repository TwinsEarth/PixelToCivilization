using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.0.1 现代城市公共设施（SimCity BuildIt）：组织神随人口与时代自动配套——
    /// 消防站/警察局/医院/现代学校/公园/现代超市/居民小区。
    /// V9.0.2 实效化：每类设施按“服务半径”计算对居民建筑的覆盖率（0~1），覆盖率决定城市微观事件
    /// （火灾/犯罪潮/疫情）是受控还是失控；公园/学校/超市按覆盖给幸福、科研、供给。
    /// 覆盖率由建筑位置现算、事件瞬时结算，不新增存档字段；读档后下一年度自然恢复。
    /// </summary>
    public class CityServicesSystem : GameSystemBase
    {
        private float _cd;

        // ===== V9.0.2 服务半径（世界单位，1 Tile=4）=====
        public const float R_FIRE = 60f, R_POLICE = 70f, R_HOSPITAL = 75f,
                          R_SCHOOL = 65f, R_PARK = 45f, R_MARKET = 55f;
        public const float COV_GOOD = 0.8f, COV_MID = 0.4f;   // 充足 / 一般 阈值

        // 最近一次年度结算的覆盖率（供 UI/探针读取）
        public float FireCov, PoliceCov, HospitalCov, SchoolCov, ParkCov, MarketCov;

        public int Count(string id)
        {
            int n = 0;
            foreach (var b in S.Buildings) if (b.Type == id) n++;
            return n;
        }

        public bool HasFireCoverage => Count("fire_station") > 0;
        public bool HasPolice => Count("police_station") > 0;
        public bool HasHospital => Count("hospital") > 0;

        public override void Tick(float dt)
        {
            if (S == null) return;
            _cd -= dt;
            if (_cd > 0f) return;
            _cd = 4f; // 现实约 4 秒评估一次，每次至多建 1 座
            TryProvision();
        }

        private bool FloorRes()
        {
            // 保底资源，避免 AI 把城市建空：木/石/金留底，工业后还要水泥/钢
            if (S.GetRes("wood") < 50 || S.GetRes("stone") < 50 || S.GetRes("gold") < 40) return false;
            if (S.Era >= 6 && (S.GetRes("concrete") < 100 || S.GetRes("steel") < 50)) return false;
            return true;
        }

        private void TryProvision()
        {
            if (S.Buildings.Count == 0) return;
            int pop = Mathf.Max(1, S.Pop);

            // ① 住房缺口优先（era6 小区，每套 +40 住房）
            if (S.Era >= 6 && S.Housing - S.Pop < 30f && Count("apartment") < Mathf.CeilToInt(pop / 120f) + 2)
            { Build("apartment"); return; }

            // ② 治安/消防（每 150~200 人一座）
            if (Count("fire_station") < Mathf.Min(6, Mathf.CeilToInt(pop / 150f)))   { Build("fire_station"); return; }
            if (Count("police_station") < Mathf.Min(5, Mathf.CeilToInt(pop / 200f))){ Build("police_station"); return; }

            // ③ 教育/医疗
            if (Count("modern_school") < Mathf.Min(6, Mathf.CeilToInt(pop / 150f))) { Build("modern_school"); return; }
            if (Count("hospital") < Mathf.Min(5, Mathf.CeilToInt(pop / 200f)))      { Build("hospital"); return; }

            // ④ 商业/绿地
            if (Count("supermarket") < Mathf.Min(6, Mathf.CeilToInt(pop / 120f)))  { Build("supermarket"); return; }
            if (Count("park") < Mathf.Min(10, Mathf.CeilToInt(pop / 80f)))         { Build("park"); return; }

            // ⑤ V9.0.8 地铁：每 300 人一座、全城上限 4 座；拥堵超 0.55 或人口达 900 的大都市优先建设
            if (S.Era >= 6 && Count("subway") < Mathf.Min(4, Mathf.CeilToInt(pop / 300f)))
            {
                float cong = GM.ModernTraffic != null ? GM.ModernTraffic.AvgCongestion : 0f;
                if (cong > 0.55f || pop >= 900) { Build("subway"); return; }
            }
        }

        private void Build(string id)
        {
            var def = GM.Def(id);
            if (def == null || S.Era < def.Era) return;
            if (!FloorRes()) return;
            if (!S.CanAfford(def.Cost)) return;
            if (GM.Building.FindAutoPosition(id, out float x, out float z) &&
                GM.Building.PlaceBuilding(id, x, z))
            {
                GM.AddEvent("good", "🏙️ 城市配套：建成" + def.Name);
            }
        }

        // ===== V9.0.2 覆盖率：被至少一座该类设施半径覆盖的居民建筑占比 =====
        private static bool IsResidential(BuildingEntity b)
            => b != null && b.MapId == "home" && b.Def != null && b.Def.GetFunc("housing") > 0;

        public float Coverage(string serviceId, float radius)
        {
            if (S == null) return 0f;
            var stations = S.Buildings.FindAll(b => b != null && b.Type == serviceId && b.MapId == "home");
            if (stations.Count == 0) return 0f;
            var homes = S.Buildings.FindAll(IsResidential);
            if (homes.Count == 0) return 1f;   // 尚无居民建筑时视为全覆盖，避免除零
            float r2 = radius * radius;
            int covered = 0;
            foreach (var h in homes)
            {
                foreach (var st in stations)
                    if ((st.X - h.X) * (st.X - h.X) + (st.Z - h.Z) * (st.Z - h.Z) <= r2) { covered++; break; }
            }
            return Mathf.Clamp01((float)covered / homes.Count);
        }

        public void RecomputeCoverage()
        {
            FireCov = Coverage("fire_station", R_FIRE);
            PoliceCov = Coverage("police_station", R_POLICE);
            HospitalCov = Coverage("hospital", R_HOSPITAL);
            SchoolCov = Coverage("modern_school", R_SCHOOL);
            ParkCov = Coverage("park", R_PARK);
            MarketCov = Coverage("supermarket", R_MARKET);
        }

        public override void OnYear(int year)
        {
            if (S == null) return;
            RecomputeCoverage();

            // ① 公园/医院/警局/超市的软性年度收益（按覆盖率分档，钳制小幅）
            float happy = 0f;
            if (ParkCov >= COV_GOOD) happy += 2f; else if (ParkCov >= COV_MID) happy += 1f;
            if (HospitalCov >= COV_MID) happy += 1f;
            if (PoliceCov >= COV_MID) happy += 1f;
            if (MarketCov >= COV_MID) happy += 1f;       // 商业可达性
            if (happy > 0f) S.Happiness = Mathf.Min(100f, S.Happiness + happy);

            // ② 警局按覆盖抑制腐败（覆盖越高越有效）
            if (PoliceCov > 0f)
            {
                float cut = Mathf.Min(3f, Count("police_station") * 1.2f) * Mathf.Max(0.4f, PoliceCov);
                S.Corruption = Mathf.Max(0f, S.Corruption - cut);
            }
            // ③ 学校覆盖给科研年度加成
            if (SchoolCov >= COV_MID)
                S.AddRes("research", Mathf.Max(2, Mathf.RoundToInt(S.Pop / 100f)));

            // ④ 城市微观事件：近代(era≥4)起每年至多 1 起，与宏观灾害互不替代
            if (S.Era >= 4) RollCityIncident();
        }

        /// <summary>城市微观事件检定（年度，同帧唯一结算）。force 供回归探针绕过随机必触发。</summary>
        public void RollCityIncident(bool force = false)
        {
            // 火灾 10% / 犯罪 9% / 疫情 7%；force 时按当前最薄弱项必触发一次
            float roll = Random.value;
            if (force)
            {
                // 选覆盖率最低的可处置事件
                float min = Mathf.Min(FireCov, PoliceCov, HospitalCov);
                if (min == FireCov) DoFire();
                else if (min == PoliceCov) DoCrime();
                else DoOutbreak();
                return;
            }
            // V9.0.4 疫情基础 7%，污染指数放大/缩小概率（清洁×0.7，重污染最高×2）
            float epi = 0.07f * (GM.Infra != null ? GM.Infra.EpidemicFactor() : 1f);
            if (roll < 0.10f) DoFire();
            else if (roll < 0.19f) DoCrime();
            else if (roll < 0.19f + epi) DoOutbreak();
        }

        /// <summary>V9.0.6：事件发生时从最近站点派出应急车（纯表现，数值结算仍按覆盖率）。</summary>
        private void Dispatch(string stationType, BuildingEntity target)
        {
            if (target == null || Count(stationType) == 0) return;
            GM.ModernTraffic?.DispatchEmergency(stationType, target.X, target.Z);
        }
        private BuildingEntity RandomHome()
        {
            var homes = S.Buildings.FindAll(IsResidential);
            return homes.Count > 0 ? homes[Random.Range(0, homes.Count)] : null;
        }
        private BuildingEntity AnyBuilding()
            => S.Buildings.Count > 0 ? S.Buildings[Random.Range(0, S.Buildings.Count)] : null;

        private void DoFire()
        {
            Dispatch("fire_station", PickBurnable() ?? AnyBuilding());
            if (FireCov >= COV_GOOD)
            { GM.AddEvent("good", "🚒 城市火情被消防站及时扑灭，未造成损失（消防覆盖 " + Pct(FireCov) + "）"); return; }
            bool burns = FireCov < COV_MID || Random.value < 0.30f;   // 中间档 30% 失控，不足档必失控
            if (!burns)
            { GM.AddEvent("info", "🚒 消防站出动，控制住一场火情（消防覆盖 " + Pct(FireCov) + "）"); return; }
            var target = PickBurnable();
            if (target == null)
            { int g = Mathf.RoundToInt(S.GetRes("gold") * 0.03f); S.AddRes("gold", -g);
              GM.AddEvent("bad", "🔥 消防覆盖不足（" + Pct(FireCov) + "），火灾蔓延，灾后重建耗金 " + g); return; }
            GM.Building.DestroyByDisaster(target, "🔥", "消防覆盖不足（" + Pct(FireCov) + "），火灾失控");
        }

        private void DoCrime()
        {
            Dispatch("police_station", AnyBuilding());
            if (PoliceCov >= COV_GOOD)
            { GM.AddEvent("good", "👮 治安巡防到位，未形成犯罪潮（治安覆盖 " + Pct(PoliceCov) + "）"); return; }
            bool full = PoliceCov < COV_MID;
            float goldPct = full ? 0.08f : 0.04f;
            float happyD = full ? 4f : 2f;
            float corr = full ? 3f : 1.5f;
            int g = Mathf.RoundToInt(S.GetRes("gold") * goldPct);
            S.AddRes("gold", -g);
            S.Happiness = Mathf.Max(0f, S.Happiness - happyD);
            S.Corruption = Mathf.Min(100f, S.Corruption + corr);
            GM.AddEvent("bad", (full ? "🚔 治安覆盖不足（" : "🚔 治安一般（") + Pct(PoliceCov) + "），犯罪潮：金币-" + g + "、民心-" + happyD + "、腐败+" + corr);
        }

        private void DoOutbreak()
        {
            Dispatch("hospital", RandomHome() ?? AnyBuilding());
            if (HospitalCov >= COV_GOOD)
            {
                int mild = Mathf.Max(0, Mathf.RoundToInt(S.Pop * 0.005f));
                if (mild > 0) S.Pop = Mathf.Max(20, S.Pop - mild);
                GM.AddEvent("good", "🏥 医疗卫生网快速响应，疫情仅轻症 " + mild + " 例（医疗覆盖 " + Pct(HospitalCov) + "）"); return;
            }
            float ratio = HospitalCov < COV_MID ? 0.03f : 0.015f;
            ratio *= GM.Infra != null ? GM.Infra.EpidemicFactor() : 1f;   // V9.0.4 污染加重疫情
            int loss = Mathf.RoundToInt(S.Pop * ratio);
            S.Pop = Mathf.Max(20, S.Pop - loss);
            S.Happiness = Mathf.Max(0f, S.Happiness - 5f);
            GM.AddEvent("bad", "🦠 医疗覆盖不足（" + Pct(HospitalCov) + "），疫情暴发：人口-" + loss + "、民心-5");
        }

        /// <summary>选取可被火灾焚毁的建筑：非住宅、非军事/防御、非奇观，优先产业/商业类。</summary>
        private BuildingEntity PickBurnable()
        {
            var cand = new List<BuildingEntity>();
            foreach (var b in S.Buildings)
            {
                if (b == null || b.MapId != "home" || b.Def == null) continue;
                var d = b.Def;
                if (d.GetFunc("housing") > 0 || d.GetFunc("soldiers") > 0 ||
                    d.GetFunc("defense") > 0 || d.GetFunc("cavalry") > 0) continue;
                if (b.Type == "fire_station" || b.Type == "hospital" || b.Type == "police_station") continue;
                cand.Add(b);
            }
            if (cand.Count == 0) return null;
            // 优先产业/商业
            var prefer = cand.FindAll(b =>
                b.Type.Contains("factory") || b.Type.Contains("mill") || b.Type.Contains("mine") ||
                b.Type.Contains("market") || b.Type.Contains("power") || b.Type == "lumbermill" || b.Type == "supermarket");
            var pool = prefer.Count > 0 ? prefer : cand;
            return pool[Random.Range(0, pool.Count)];
        }

        private static string Pct(float c) => Mathf.RoundToInt(c * 100f) + "%";

        /// <summary>V9.0.2 回归探针：回报七设施数量与六项覆盖率</summary>
        public string Diagnose()
        {
            RecomputeCoverage();
            return $"[CITY] pop={S.Pop} era={S.Era} | fire={Count("fire_station")}@{Pct(FireCov)} " +
                   $"police={Count("police_station")}@{Pct(PoliceCov)} hospital={Count("hospital")}@{Pct(HospitalCov)} " +
                   $"school={Count("modern_school")}@{Pct(SchoolCov)} park={Count("park")}@{Pct(ParkCov)} " +
                   $"market={Count("supermarket")}@{Pct(MarketCov)} apt={Count("apartment")} buildings={S.Buildings.Count}";
        }
    }
}
