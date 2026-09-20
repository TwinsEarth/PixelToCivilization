using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.0.5 城市指标与现代人口结构：健康/教育/治安/就业四项 0~100 指标（幸福沿用 State.Happiness），
    /// 劳动力=青年+中年扣除在校学生，岗位=农业兜底(随时代骤减)+工业/商业/公共建筑岗位，
    /// 道路拥堵（ModernTraffic.AvgCongestion 0~10）按通勤折减有效岗位；失业压幸福、劳动力短缺压商品产出。
    /// 指标年度结算并向目标值平滑，全部进存档；读档后下一次评估自然恢复。
    /// </summary>
    public class CityMetricsSystem : GameSystemBase
    {
        // 每座建筑提供的城镇就业岗位（工业/商业/公共服务），随建筑等级增长
        private static readonly Dictionary<string, int> JobTable = new()
        {
            // 采集/加工
            {"lumbermill",6},{"mine",6},{"workshop",8},{"iron_smelter",10},{"brick_works",10},{"porcelain_kiln",10},
            // 工业/能源/交通
            {"factory_pre",24},{"factory_modern",36},{"modern_arsenal",20},{"dockyard_modern",16},
            {"power_plant",10},{"fusion_plant",12},{"data_center",12},{"ai_lab",16},
            {"high_speed_rail",12},{"airport",14},{"subway",20},   // V9.0.8 地铁站 20 岗位
            // 商业/公共服务/住宅管理
            {"market",6},{"supermarket",14},{"skyscraper",45},{"apartment",2},
            {"hospital",10},{"modern_school",12},{"police_station",10},{"fire_station",10},{"park",2},
        };

        public float Jobs, Workforce, Students, Employed, Unemployed, LaborFill = 1f;
        public float CommutePenalty;   // 通勤折减率 0~0.2

        /// <summary>劳动力短缺对工业产出的乘数：满员 1.0，极端缺工最低 0.8</summary>
        public float LaborFillMult => 0.8f + 0.2f * LaborFill;

        public override void Tick(float dt)
        {
            if (S == null) return;
            // 轻量：每现实约 4 秒重算一次岗位/就业（指标本身年度平滑）
            _cd -= dt;
            if (_cd > 0f) return;
            _cd = 4f;
            Recompute();
        }

        private float _cd;

        /// <summary>重算人口结构、岗位、通勤与四项指标（OnYear 与探针共用）。</summary>
        public void Recompute()
        {
            if (S == null) return;
            bool modern = S.Era >= 5;

            // —— 人口结构 → 学生 / 劳动力 ——
            // 古代沿用既有年龄桶（高出生高死亡）；现代用人口转变模型的确定性结构份额，
            // 不受“住房封顶后出生停滞、年龄晋升把儿童/青年抽干”这一旧管线退化影响。
            float child, young, middle;
            float schoolCov = GM.CityServices != null ? GM.CityServices.SchoolCov : 0f;
            if (modern)
            {
                child = S.Pop * 0.20f; young = S.Pop * 0.26f; middle = S.Pop * 0.38f;   // 老 0.16
                float studentRate = Mathf.Clamp01(schoolCov);
                Students = Mathf.Round((child + young * 0.35f) * studentRate);
            }
            else
            {
                GM.Population?.NormalizeAge();
                child = S.Children; young = S.Young; middle = S.Middle;
                float studentRate = S.SchoolFounded ? 0.08f : 0f;
                Students = Mathf.Round((child + young * 0.35f) * studentRate);
            }
            Workforce = Mathf.Round(Mathf.Max(0f, young + middle - Students));

            // —— 岗位：农业兜底（现代机械化后大幅下降）+ 建筑岗位 ——
            float agrarian = S.Pop * (modern ? 0.18f : 0.62f);
            float buildingJobs = 0f;
            foreach (var b in S.Buildings)
            {
                if (b == null || b.MapId != "home" || b.Def == null) continue;
                if (JobTable.TryGetValue(b.Type, out var j)) buildingJobs += j * b.LevelMult;
            }
            float rawJobs = agrarian + buildingJobs;

            // —— 通勤：道路平均拥堵 0~10，每点折减 2% 有效岗位（最多 -20%）—
            float cong = GM.ModernTraffic != null ? GM.ModernTraffic.AvgCongestion : 0f;
            CommutePenalty = Mathf.Clamp01(cong * 0.02f);
            Jobs = rawJobs * (1f - CommutePenalty);

            // —— 就业 / 失业 / 劳动力充足度 ——
            float empRate = Workforce <= 0f ? 1f : Mathf.Clamp01(Jobs / Workforce);
            Employed = Mathf.Round(Workforce * empRate);
            Unemployed = Mathf.Round(Workforce - Employed);
            LaborFill = rawJobs <= 0f ? 1f : Mathf.Clamp01(Workforce / rawJobs);
            float empTarget = empRate * 100f;

            // —— 四项指标目标值 ——
            float covH = GM.CityServices != null ? GM.CityServices.HospitalCov : 0f;
            float covS = GM.CityServices != null ? GM.CityServices.SchoolCov : 0f;
            float covP = GM.CityServices != null ? GM.CityServices.PoliceCov : 0f;
            float covF = GM.CityServices != null ? GM.CityServices.FireCov : 0f;
            float covPark = GM.CityServices != null ? GM.CityServices.ParkCov : 0f;
            float poll = S.Pollution;

            float healthT = Mathf.Clamp(38f + S.Era * 3.5f + 48f * covH - Mathf.Max(0f, poll - 40f) * 0.6f - (covPark < 0.4f ? 4f : 0f), 5f, 100f);
            float eduT    = Mathf.Clamp(30f + S.Era * 4f + 52f * covS + (S.SchoolFounded ? 6f : 0f), 5f, 100f);
            float safeT   = Mathf.Clamp(34f + 42f * covP + 18f * covF - S.Corruption * 0.12f, 5f, 100f);

            // 首次直接落位，之后年度平滑 35%
            if (S.CityHealth <= 0f && S.CityEducation <= 0f && S.CitySafety <= 0f)
            { S.CityHealth = healthT; S.CityEducation = eduT; S.CitySafety = safeT; }
            S.CityHealth = Mathf.Lerp(S.CityHealth, healthT, 0.35f);
            S.CityEducation = Mathf.Lerp(S.CityEducation, eduT, 0.35f);
            S.CitySafety = Mathf.Lerp(S.CitySafety, safeT, 0.35f);
            S.CityEmployment = empTarget;
        }

        public override void OnYear(int year)
        {
            if (S == null) return;
            GM.CityServices?.RecomputeCoverage();

            // 现代人口结构年度自愈：住房封顶会让旧出生/年龄管线把儿童、青年抽干，
            // 每年把四个年龄桶向人口转变模型份额靠拢 30%，再归一到总人口，UI/个体与指标一致。
            if (S.Era >= 5)
            {
                float tC = S.Pop * 0.20f, tY = S.Pop * 0.26f, tM = S.Pop * 0.38f, tO = S.Pop * 0.16f;
                S.Children = Mathf.Lerp(S.Children, tC, 0.3f);
                S.Young    = Mathf.Lerp(S.Young, tY, 0.3f);
                S.Middle   = Mathf.Lerp(S.Middle, tM, 0.3f);
                S.Old      = Mathf.Lerp(S.Old, tO, 0.3f);
                GM.Population?.NormalizeAge();
            }
            Recompute();

            // —— 指标对幸福的年度反馈（小幅、可读）——
            float dh = 0f;
            if (S.CityEmployment < 70f) dh -= (70f - S.CityEmployment) * 0.12f;   // 失业：70%→0，50%→-2.4
            else if (S.CityEmployment >= 95f) dh += 1f;                           // 充分就业
            if (S.CityHealth < 50f) dh -= 1f; else if (S.CityHealth >= 80f) dh += 1f;
            if (S.CitySafety < 50f) dh -= 1f; else if (S.CitySafety >= 85f) dh += 1f;
            if (CommutePenalty >= 0.14f) dh -= 1f;                                // 严重拥堵
            S.Happiness = Mathf.Clamp(S.Happiness + dh, 0f, 100f);
        }

        public string Diagnose()
        {
            Recompute();
            return $"[METRIC] era={S.Era} pop={S.Pop} | 学生{Students} 劳力{Workforce} 岗位{Jobs:F0}(通勤折{CommutePenalty*100:F0}%) " +
                   $"就业{Employed:F0}/失业{Unemployed:F0}={S.CityEmployment:F0}% 缺工Fill={LaborFill:F2} | " +
                   $"健康{S.CityHealth:F0} 教育{S.CityEducation:F0} 治安{S.CitySafety:F0} 幸福{S.Happiness:F0}";
        }
    }
}
