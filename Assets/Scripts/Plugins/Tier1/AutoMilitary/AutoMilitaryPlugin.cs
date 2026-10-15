using System.Collections.Generic;
using UnityEngine;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;
using PxC.Plugins.Building;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;

namespace PxC.Plugins.AutoMilitary
{
    /// <summary>
    /// 自动军事建设（Tier-1）。年度对每个存活势力执行：
    /// ① 自动征兵，维持士兵占比（劳动力转士兵，保留基础劳力）；
    /// ② 按时代付费建造陆军单位（骑兵/战车/坦克等，经 GroundForceService.TryBuildUnit 扣费）；
    /// ③ 战争时自动建造防御箭塔。
    /// 玩家势力默认跳过；<see cref="EnablePlayerAutomation"/> 可开启。
    /// 纯决策、无运行时状态，不产生存档段。
    /// </summary>
    public sealed class AutoMilitaryPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.automilitary";

        // ---- 征兵口径 ----
        /// <summary>目标士兵占总人口比例。</summary>
        public const float SoldierTargetRatio = 0.15f;
        /// <summary>每年每势力最多征兵数。</summary>
        public const int RecruitsPerYear = 5;
        /// <summary>保留不征的最低劳动力。</summary>
        public const int ReserveLabor = 20;

        // ---- 陆军单位口径 ----
        // 目标陆军单位数随时代增长（见 TargetGroundUnitsForEra），
        // 原始时代只保留少量初始编队，工业/现代才维持大规模常备军。

        // ---- 防御塔口径 ----
        /// <summary>战争时目标防御建筑数（1 基础箭塔 + 1 进阶火塔/炮塔 + 1 工业时代碉堡）。</summary>
        public const int TargetWarTowers = 3;

        // ---- 船艇口径 ----
        // 目标船艇数随时代增长（见 TargetNavalUnitsForEra），内陆势力始终为 0。

        // ---- 航母口径 ----
        /// <summary>目标航母数（现代、已有基础舰队后才尝试建造；成本极高）。</summary>
        public const int TargetAircraftCarriers = 1;

        /// <summary>
        /// 按时代返回目标陆军单位数（含初始编队）：
        /// 早期兵少、少战；工业/现代/信息时代逐步维持更大规模常备军。
        /// </summary>
        public static int TargetGroundUnitsForEra(int era)
        {
            if (era >= (int)Era.Space) return 18;
            if (era >= (int)Era.Information) return 15;
            if (era >= (int)Era.Modern) return 12;
            if (era >= (int)Era.Industrial) return 9;
            if (era >= (int)Era.AgeOfSail) return 7;
            if (era >= (int)Era.HighMiddle) return 6;
            if (era >= (int)Era.Medieval) return 6;
            if (era >= (int)Era.IronAge) return 5;
            if (era >= (int)Era.BronzeAge) return 4;
            return 3;
        }

        /// <summary>
        /// 按时代返回目标船艇数：早期只有少量渔船/木筏，
        /// 大航海后逐步扩大，现代/信息时代维持较大舰队。
        /// </summary>
        public static int TargetNavalUnitsForEra(int era)
        {
            if (era >= (int)Era.Space) return 10;
            if (era >= (int)Era.Information) return 8;
            if (era >= (int)Era.Modern) return 6;
            if (era >= (int)Era.Industrial) return 4;
            if (era >= (int)Era.AgeOfSail) return 3;
            if (era >= (int)Era.IronAge) return 2;
            if (era >= (int)Era.BronzeAge) return 1;
            return 0;
        }

        // ---- 首府周边环布 ----
        private const float UnitInnerRadius = 6f;
        private const int UnitRingCount = 5;
        private const float UnitRingSpacing = 2f;
        private const float TowerRadius = 5f;
        private const float GoldenAngle = 2.399963f;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(7)
            {
                new PluginDependency("pxc.economy", 1),
                new PluginDependency("pxc.population", 1),
                new PluginDependency("pxc.tech", 1),
                new PluginDependency("pxc.groundforce", 1),
                new PluginDependency("pxc.naval", 1),
                new PluginDependency("pxc.military", 1),
                new PluginDependency("pxc.building", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

        /// <summary>是否对玩家势力也自动军事建设（默认 false）。</summary>
        public bool EnablePlayerAutomation;

        public string Id => PluginIdValue;
        public string DisplayName => "自动军事建设";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;
        public SimulationLayer Layer => SimulationLayer.P1Tactical;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context) => _ctx = context;
        public void OnInitialize() { }
        public void OnTick(float simDt) { }

        public void OnYear(int gameYear)
        {
            if (!_ctx.Services.TryResolve<IMilitaryService>(out var military)) return;
            if (!_ctx.Services.TryResolve<IPopulationService>(out var population)) return;
            if (!_ctx.Services.TryResolve<GroundForceService>(out var ground)) return;
            if (!_ctx.Services.TryResolve<ITechService>(out var tech)) return;
            // 船艇服务可选（无海军插件时不造船）
            _ctx.Services.TryResolve<NavalService>(out var naval);
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            for (int f = 0; f < factions.Count; f++)
            {
                FactionInfo info = factions.Get(f);
                if (!info.Alive) continue;
                if (info.IsPlayer && !EnablePlayerAutomation) continue;

                RecruitStep(f, military, population);
                BuildGroundUnitStep(f, info, ground, tech, population);
                if (naval != null)
                {
                    NavalStep(f, info, naval, tech);
                    AircraftCarrierStep(f, info, naval, tech);
                }
                DefenseTowerStep(f, info, military, factions, tech);
            }
        }

        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }
        public void OnShutdown() { }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }

        // ============================================================
        // ① 自动征兵
        // ============================================================
        private void RecruitStep(
            int faction, IMilitaryService military, IPopulationService population)
        {
            int total = population.Total(faction);
            int soldiers = population.ByLayer(faction, PopulationLayer.Soldier);
            int target = Mathf.RoundToInt(total * SoldierTargetRatio);
            if (soldiers >= target) return;

            int want = Mathf.Min(RecruitsPerYear, target - soldiers);
            int labor = population.ByLayer(faction, PopulationLayer.Laborer);
            int available = labor - ReserveLabor;
            int n = Mathf.Min(want, Mathf.Max(0, available));
            if (n <= 0) return;

            if (military.TryRecruit(faction, n))
                _ctx.Log($"[自动军事] 势力 {faction} 征兵 {n}（士兵 {soldiers}→{soldiers + n}）");
        }

        // ============================================================
        // ② 付费建造陆军单位
        // ============================================================
        private void BuildGroundUnitStep(
            int faction, FactionInfo info, GroundForceService ground,
            ITechService tech, IPopulationService population)
        {
            int existing = ground.CountByFaction(faction);
            int era = (int)tech.GetEra(faction);
            int target = TargetGroundUnitsForEra(era);
            if (existing >= target) return;

            GroundForceModel model = ChooseGroundModel(era);
            // 位置环布（含初始编队：用现有单位总数作索引，避免与初始阵兵重叠）
            Vector3 position = NextUnitPosition(info, existing);
            var id = ground.TryBuildUnit(faction, model, position);
            if (id.IsValid)
                _ctx.Log($"[自动军事] 势力 {faction} 建造 {model}（{id}）");
        }

        /// <summary>按时代选择陆军单位型号；原始时代无付费单位（靠初始编队）。</summary>
        private static GroundForceModel ChooseGroundModel(int era)
        {
            if (era >= (int)Era.Information) return GroundForceModel.MissileVehicle;
            if (era >= (int)Era.Modern) return GroundForceModel.Tank;
            if (era >= (int)Era.IronAge) return GroundForceModel.Chariot;
            if (era >= (int)Era.BronzeAge) return GroundForceModel.Cavalry;
            return GroundForceModel.Formation;
        }

        // ============================================================
        // ②b 付费建造船艇（仅靠水势力；内陆势力 TryBuildUnit 找水域失败而跳过）
        // ============================================================
        private void NavalStep(
            int faction, FactionInfo info, NavalService naval, ITechService tech)
        {
            int existing = naval.CountByFaction(faction);
            int era = (int)tech.GetEra(faction);
            int target = TargetNavalUnitsForEra(era);
            if (existing >= target) return;

            NavalModel model = ChooseNavalModel(era);
            // 找水域 + 扣费 + 生成均在 TryBuildUnit 内完成；内陆势力返回 Invalid。
            var id = naval.TryBuildUnit(faction, model, info.HomeX, info.HomeZ);
            if (id.IsValid)
                _ctx.Log($"[自动军事] 势力 {faction} 建造 {model}（{id}）");
        }

        /// <summary>按时代选择船艇型号；达不到某型时代门槛时退回上一型。</summary>
        private static NavalModel ChooseNavalModel(int era)
        {
            if (era >= (int)Era.Modern) return NavalModel.Submarine;
            if (era >= (int)Era.Industrial) return NavalModel.Steamship;
            if (era >= (int)Era.AgeOfSail) return NavalModel.SailingShip;
            if (era >= (int)Era.IronAge) return NavalModel.Warship;
            if (era >= (int)Era.BronzeAge) return NavalModel.FishingBoat;
            return NavalModel.Raft;
        }

        // ============================================================
        // ②c 航母（现代、已有基础舰队后才尝试；成本极高，资源不足安全失败）
        // ============================================================
        private void AircraftCarrierStep(
            int faction, FactionInfo info, NavalService naval, ITechService tech)
        {
            int era = (int)tech.GetEra(faction);
            if (era < (int)Era.Modern) return;
            if (naval.CountByFaction(faction) < TargetNavalUnitsForEra(era)) return;
            if (naval.CountByModel(faction, NavalModel.AircraftCarrier) >= TargetAircraftCarriers)
                return;

            var id = naval.TryBuildUnit(
                faction, NavalModel.AircraftCarrier, info.HomeX, info.HomeZ);
            if (id.IsValid)
                _ctx.Log($"[自动军事] 势力 {faction} 建造航空母舰（{id}）");
        }

        // ============================================================
        // ③ 战争时自动建防御塔（首座箭塔，进阶按时代升级火塔/炮塔）
        // ============================================================
        private void DefenseTowerStep(
            int faction, FactionInfo info,
            IMilitaryService military, FactionRegistry factions, ITechService tech)
        {
            if (!IsAtWarWithAnyone(faction, military, factions)) return;
            if (!_ctx.Services.TryResolve<IBuildingService>(out var buildings)) return;

            int towers = CountTowers(faction, buildings);
            if (towers >= TargetWarTowers) return;

            int era = (int)tech.GetEra(faction);
            // 第 3 座（碉堡）仅工业时代建造；时代不足则保持 2 座，不再补建。
            if (towers >= 2 && era < (int)Era.Industrial) return;

            BuildingType towerType = ChooseTowerType(towers, era);
            if (!buildings.CanBuild(faction, (int)towerType)) return;

            Vector3 position = NextTowerPosition(info, towers);
            int slot = buildings.TryBuild(faction, (int)towerType, position);
            if (slot >= 0)
                _ctx.Log($"[自动军事] 势力 {faction} 战时建{towerType}（slot {slot}）");
        }

        /// <summary>
        /// 按已建塔数与时代选塔型：
        /// 第 1 座为基础箭塔；
        /// 第 2 座起，铁器时代→炮塔（CannonTower），青铜时代→火塔（FireTower），
        /// 否则继续箭塔；
        /// 第 3 座起，工业时代→碉堡（Bunker，重防御），否则继续箭塔。
        /// </summary>
        private static BuildingType ChooseTowerType(int existingTowers, int era)
        {
            if (existingTowers <= 0) return BuildingType.ArrowTower;
            if (existingTowers >= 2)
            {
                if (era >= (int)Era.Industrial) return BuildingType.Bunker;
                return BuildingType.ArrowTower;
            }
            if (era >= (int)Era.IronAge) return BuildingType.CannonTower;
            if (era >= (int)Era.BronzeAge) return BuildingType.FireTower;
            return BuildingType.ArrowTower;
        }

        private static bool IsAtWarWithAnyone(
            int faction, IMilitaryService military, FactionRegistry factions)
        {
            for (int other = 0; other < factions.Count; other++)
            {
                if (other == faction) continue;
                if (military.IsAtWar(faction, other)) return true;
            }
            return false;
        }

        private static int CountTowers(int faction, IBuildingService buildings)
        {
            int n = 0;
            n += buildings.CountByType(faction, (int)BuildingType.ArrowTower);
            n += buildings.CountByType(faction, (int)BuildingType.FireTower);
            n += buildings.CountByType(faction, (int)BuildingType.CannonTower);
            n += buildings.CountByType(faction, (int)BuildingType.Bunker);
            return n;
        }

        // ============================================================
        // 位置
        // ============================================================
        private static Vector3 NextUnitPosition(FactionInfo info, int existing)
        {
            float angle = existing * GoldenAngle;
            float radius = UnitInnerRadius +
                (existing % UnitRingCount) * UnitRingSpacing;
            return new Vector3(
                info.HomeX + Mathf.Cos(angle) * radius,
                0f,
                info.HomeZ + Mathf.Sin(angle) * radius);
        }

        private static Vector3 NextTowerPosition(FactionInfo info, int towerCount)
        {
            // 防御塔贴首府，与陆军单位错开（角度偏移 0.6 弧度）。
            float angle = towerCount * GoldenAngle + 0.6f;
            return new Vector3(
                info.HomeX + Mathf.Cos(angle) * TowerRadius,
                0f,
                info.HomeZ + Mathf.Sin(angle) * TowerRadius);
        }
    }
}
