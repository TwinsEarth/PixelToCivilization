using NUnit.Framework;
using UnityEngine;
using PxC.Core.Capabilities;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Core.World;
using PxC.Host;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;
using PxC.Plugins.AutoMilitary;
using PxC.Plugins.Building;
using PxC.Plugins.Economy;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Military;
using PxC.Plugins.Naval;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 自动军事建设测试：自动征兵 / 付费建造陆军单位 / 战时自动建箭塔。
    /// 使用真实 GroundForceService（真实陆地校验+扣费）、EconomyService、BuildingService、
    /// MilitaryService，FakePopulation（可控人口）/ FakeTech（可控时代）。
    /// </summary>
    public sealed class AutoMilitaryTests
    {
        private const int DefaultLabor = 100;
        private const int DefaultSoldiers = 0;

        private sealed class Harness
        {
            public ServiceRegistry Services;
            public FactionRegistry Factions;
            public UnitRegistry Units;
            public GridGeography Geo;
            public GroundForceService Ground;
            public NavalService Naval;
            public BuildingRegistry Buildings;
            public EconomyState EcoState;
            public FakeTech Tech;
            public MilitaryService Military;
            public AutoMilitaryPlugin Plugin;
        }

        private static Harness BuildHarness(
            int factionCount, bool playerAt0,
            int labor = DefaultLabor, int soldiers = DefaultSoldiers)
        {
            var services = new ServiceRegistry();
            var factions = new FactionRegistry();
            for (int i = 0; i < factionCount; i++)
                factions.Add("F" + i, playerAt0 && i == 0, i * 1000f, 0f, i);

            var units = new UnitRegistry();
            var geo = new GridGeography();
            services.Register<UnitRegistry>(units);
            services.Register<IGeographyQuery>(geo);

            var groundState = new GroundForceState(units.Capacity);
            var ground = new GroundForceService(units, groundState, geo, services, _ => { });
            services.Register<GroundForceService>(ground);

            var ecoState = new EconomyState();
            ecoState.Bind(factionCount);
            var economy = new EconomyService(ecoState);
            services.Register<IEconomyService>(economy);

            var buildings = new BuildingRegistry();
            var buildingService = new BuildingService(services, buildings, _ => { });
            services.Register<IBuildingService>(buildingService);

            var pop = new FakePopulation(factionCount, labor, soldiers);
            services.Register<IPopulationService>(pop);

            var tech = new FakeTech();
            services.Register<ITechService>(tech);

            // 真实海军服务（船艇需水域；测试用 GridGeography 控制）
            var navalState = new NavalState(units.Capacity);
            var naval = new NavalService(units, navalState, geo, services, _ => { });
            services.Register<NavalService>(naval);

            services.Register<FactionRegistry>(factions);
            var military = new MilitaryService(ground, naval, pop);
            services.Register<IMilitaryService>(military);

            var plugin = new AutoMilitaryPlugin();
            var ctx = new PluginContext(
                AutoMilitaryPlugin.PluginIdValue,
                new StrongEventBus(),
                services,
                new ChunkedWorld(),
                new CapabilitySystem(),
                _ => { });
            plugin.OnRegister(ctx);
            plugin.OnInitialize();

            return new Harness
            {
                Services = services,
                Factions = factions,
                Units = units,
                Geo = geo,
                Ground = ground,
                Naval = naval,
                Buildings = buildings,
                EcoState = ecoState,
                Tech = tech,
                Military = military,
                Plugin = plugin,
            };
        }

        // ------------------------------------------------------------
        // 1. 士兵占比低于 15% → 自动征兵（每年最多 5，保留 20 劳力）
        // ------------------------------------------------------------
        [Test]
        public void SoldiersBelowTarget_AutoRecruits()
        {
            var h = BuildHarness(1, false);
            h.Plugin.OnYear(1);

            Assert.AreEqual(5, h.Services.Resolve<IPopulationService>().ByLayer(0, PopulationLayer.Soldier),
                "应征兵 5（100×15%=15，每年上限 5）");
            Assert.AreEqual(95, h.Services.Resolve<IPopulationService>().ByLayer(0, PopulationLayer.Laborer));
        }

        // ------------------------------------------------------------
        // 2. 士兵达标（15%）→ 不征兵
        // ------------------------------------------------------------
        [Test]
        public void SoldiersAtTarget_NoRecruit()
        {
            var h = BuildHarness(1, false, labor: 85, soldiers: 15);
            h.Plugin.OnYear(1);

            Assert.AreEqual(15, h.Services.Resolve<IPopulationService>().ByLayer(0, PopulationLayer.Soldier));
            Assert.AreEqual(85, h.Services.Resolve<IPopulationService>().ByLayer(0, PopulationLayer.Laborer));
        }

        // ------------------------------------------------------------
        // 3. 青铜时代、资源充足 → 付费建造骑兵（扣 Food 25 + Bronze 8）
        // ------------------------------------------------------------
        [Test]
        public void BronzeEra_Affordable_BuildsCavalry()
        {
            var h = BuildHarness(1, false);
            h.Tech.EraValue = (int)Era.BronzeAge;
            h.EcoState.Set(0, ResourceType.Food, 100f);
            h.EcoState.Set(0, ResourceType.Bronze, 100f);

            h.Plugin.OnYear(1);

            Assert.AreEqual(1, h.Ground.CountByFaction(0));
            Assert.AreEqual(75f, h.EcoState.Get(0, ResourceType.Food), 0.001f);
            Assert.AreEqual(92f, h.EcoState.Get(0, ResourceType.Bronze), 0.001f);
        }

        // ------------------------------------------------------------
        // 4. 资源不足 → 不建单位（不扣费、不生成）
        // ------------------------------------------------------------
        [Test]
        public void BronzeEra_CannotAfford_NoUnit()
        {
            var h = BuildHarness(1, false);
            h.Tech.EraValue = (int)Era.BronzeAge;
            // 无 Food / Bronze
            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Ground.CountByFaction(0));
        }

        // ------------------------------------------------------------
        // 5. 战争时 → 自动建箭塔（扣 Wood 30 + Stone 10）
        // ------------------------------------------------------------
        [Test]
        public void AtWar_BuildsArrowTower()
        {
            var h = BuildHarness(2, false);
            h.Tech.EraValue = (int)Era.BronzeAge;
            for (int f = 0; f < 2; f++)
            {
                h.EcoState.Set(f, ResourceType.Wood, 200f);
                h.EcoState.Set(f, ResourceType.Stone, 200f);
                h.EcoState.Set(f, ResourceType.Food, 200f);
                h.EcoState.Set(f, ResourceType.Bronze, 200f);
            }
            h.Military.DeclareWar(0, 1);

            h.Plugin.OnYear(1);

            Assert.AreEqual(1, h.Buildings.CountByType(0, (int)BuildingType.ArrowTower),
                "势力 0 战时应自动建 1 个箭塔");
        }

        // ------------------------------------------------------------
        // 6. 和平时期 → 不建箭塔
        // ------------------------------------------------------------
        [Test]
        public void AtPeace_NoTower()
        {
            var h = BuildHarness(1, false);
            h.Tech.EraValue = (int)Era.BronzeAge;
            h.EcoState.Set(0, ResourceType.Wood, 200f);
            h.EcoState.Set(0, ResourceType.Stone, 200f);

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Buildings.CountByType(0, (int)BuildingType.ArrowTower));
        }

        // ------------------------------------------------------------
        // 7. 玩家势力默认跳过（不征兵、不建单位、不建塔）
        // ------------------------------------------------------------
        [Test]
        public void PlayerFaction_SkippedByDefault()
        {
            var h = BuildHarness(1, true);
            h.Tech.EraValue = (int)Era.BronzeAge;
            h.EcoState.Set(0, ResourceType.Food, 200f);
            h.EcoState.Set(0, ResourceType.Bronze, 200f);
            h.EcoState.Set(0, ResourceType.Wood, 200f);
            h.EcoState.Set(0, ResourceType.Stone, 200f);

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Services.Resolve<IPopulationService>().ByLayer(0, PopulationLayer.Soldier));
            Assert.AreEqual(0, h.Ground.CountByFaction(0));
            Assert.AreEqual(0, h.Buildings.CountByType(0, (int)BuildingType.ArrowTower));
        }

        // ------------------------------------------------------------
        // 8. 原始时代：无 Iron → 阵兵无法付费建造（靠初始免费编队）
        // ------------------------------------------------------------
        [Test]
        public void PrimitiveEra_NoIron_NoUnit()
        {
            var h = BuildHarness(1, false);
            h.EcoState.Set(0, ResourceType.Food, 100f);
            // Iron 缺失
            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Ground.CountByFaction(0));
        }

        // ------------------------------------------------------------
        // 9. 陆军单位达目标（5）→ 不再建造
        // ------------------------------------------------------------
        [Test]
        public void GroundUnitsAtTarget_NoMoreBuild()
        {
            var h = BuildHarness(1, false, labor: 200, soldiers: 0);
            h.Tech.EraValue = (int)Era.BronzeAge;
            // 预先免费生成 5 个骑兵（陆地点错开）
            for (int k = 0; k < 5; k++)
            {
                var pos = new Vector3(10f + k * 2f, 0f, 10f);
                var id = h.Ground.Spawn(0, GroundForceModel.Cavalry, pos);
                Assert.IsTrue(id.IsValid);
            }

            h.Plugin.OnYear(1);

            Assert.AreEqual(5, h.Ground.CountByFaction(0),
                "已达目标 5 个单位，当年不应再建");
        }

        // ------------------------------------------------------------
        // 10. 青铜时代、首府附近有水 → 付费造渔船（扣 Wood 25）
        // ------------------------------------------------------------
        [Test]
        public void BronzeEra_NearWater_BuildsFishingBoat()
        {
            var h = BuildHarness(1, false);
            h.Tech.EraValue = (int)Era.BronzeAge;
            h.Geo.SetWaterRect(5, 5, 25, 25);
            h.EcoState.Set(0, ResourceType.Wood, 100f);

            h.Plugin.OnYear(1);

            Assert.AreEqual(1, h.Naval.CountByFaction(0),
                "靠水势力应在青铜时代造 1 艘渔船");
            Assert.AreEqual(75f, h.EcoState.Get(0, ResourceType.Wood), 0.001f);
        }

        // ------------------------------------------------------------
        // 11. 内陆势力（无水域）→ 不造船
        // ------------------------------------------------------------
        [Test]
        public void InlandFaction_NoWater_NoShip()
        {
            var h = BuildHarness(1, false);
            h.Tech.EraValue = (int)Era.BronzeAge;
            h.EcoState.Set(0, ResourceType.Wood, 100f);
            // 默认 GridGeography 全陆地

            h.Plugin.OnYear(1);

            Assert.AreEqual(0, h.Naval.CountByFaction(0));
            Assert.AreEqual(100f, h.EcoState.Get(0, ResourceType.Wood), 0.001f,
                "不造船则不扣木材");
        }

        // ------------------------------------------------------------
        // 12. 铁器时代、靠水 → 造战船（扣 Wood 80 + Iron 20）
        // ------------------------------------------------------------
        [Test]
        public void IronEra_NearWater_BuildsWarship()
        {
            var h = BuildHarness(1, false);
            h.Tech.EraValue = (int)Era.IronAge;
            h.Geo.SetWaterRect(5, 5, 25, 25);
            h.EcoState.Set(0, ResourceType.Wood, 200f);
            h.EcoState.Set(0, ResourceType.Iron, 100f);

            h.Plugin.OnYear(1);

            Assert.AreEqual(1, h.Naval.CountByFaction(0));
            Assert.AreEqual(120f, h.EcoState.Get(0, ResourceType.Wood), 0.001f);
            Assert.AreEqual(80f, h.EcoState.Get(0, ResourceType.Iron), 0.001f);
        }

        // ------------------------------------------------------------
        // 13. 船艇达目标（3）→ 不再建造
        // ------------------------------------------------------------
        [Test]
        public void NavalUnitsAtTarget_NoMoreBuild()
        {
            var h = BuildHarness(1, false, labor: 200, soldiers: 0);
            h.Tech.EraValue = (int)Era.BronzeAge;
            h.Geo.SetWaterRect(5, 5, 25, 25);
            // 预先在水域免费生成 3 艘木筏（位置错开）
            for (int k = 0; k < 3; k++)
            {
                var pos = new Vector3(6f + k * 2f, 0f, 6f);
                var id = h.Naval.Spawn(0, NavalModel.Raft, pos);
                Assert.IsTrue(id.IsValid);
            }

            h.Plugin.OnYear(1);

            Assert.AreEqual(3, h.Naval.CountByFaction(0),
                "已达目标 3 艘，当年不应再建");
        }
    }
}
