using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Core.World;
using PxC.Host;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;
using PxC.Plugins.AutoGovernor;
using PxC.Plugins.Building;
using PxC.Plugins.Economy;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 自动发展 / 自动建造战略 AI 测试。
    /// 使用真实 EconomyService / BuildingService（真实扣费 + 登记），
    /// FakePopulation（固定人口）/ FakeTech（控制时代）。
    /// </summary>
    public sealed class AutoGovernorTests
    {
        private const int LaborPerFaction = 100;
        private const int SoldiersPerFaction = 0;

        private sealed class Harness
        {
            public ServiceRegistry Services;
            public FactionRegistry Factions;
            public BuildingRegistry Buildings;
            public EconomyState EcoState;
            public FakeTech Tech;
            public AutoGovernorPlugin Governor;
        }

        // ------------------------------------------------------------
        // 装配
        // ------------------------------------------------------------
        private static Harness BuildHarness(int factionCount, bool playerAt0)
        {
            var services = new ServiceRegistry();
            var factions = new FactionRegistry();
            for (int i = 0; i < factionCount; i++)
            {
                // 首府放在不同位置，避免环布重叠区互相干扰。
                factions.Add("F" + i, playerAt0 && i == 0, i * 1000f, 0f, i);
            }

            var ecoState = new EconomyState();
            ecoState.Bind(factionCount);
            var economy = new EconomyService(ecoState);

            var buildings = new BuildingRegistry();
            var buildingService = new BuildingService(services, buildings, _ => { });

            var population = new FakePopulation(factionCount, LaborPerFaction, SoldiersPerFaction);
            var tech = new FakeTech();

            services.Register<FactionRegistry>(factions);
            services.Register<IEconomyService>(economy);
            services.Register<IBuildingService>(buildingService);
            services.Register<IPopulationService>(population);
            services.Register<ITechService>(tech);

            var governor = new AutoGovernorPlugin();
            var ctx = new PluginContext(
                AutoGovernorPlugin.PluginIdValue,
                new StrongEventBus(),
                services,
                new ChunkedWorld(),
                new CapabilitySystem(),
                _ => { });
            governor.OnRegister(ctx);
            governor.OnInitialize();

            return new Harness
            {
                Services = services,
                Factions = factions,
                Buildings = buildings,
                EcoState = ecoState,
                Tech = tech,
                Governor = governor,
            };
        }

        // ------------------------------------------------------------
        // 1. 粮食安全：库存低于 1.5 年口粮 → 建农田（扣木材）
        // ------------------------------------------------------------
        [Test]
        public void FoodShortage_BuildsFarm()
        {
            var h = BuildHarness(1, false);
            h.EcoState.Set(0, ResourceType.Food, 100f);   // 100 < 100*1.5
            h.EcoState.Set(0, ResourceType.Wood, 80f);

            h.Governor.OnYear(1);

            Assert.AreEqual(1, h.Buildings.CountByType(0, (int)BuildingType.Farm));
            // 农田成本 Wood 15
            Assert.AreEqual(65f, h.EcoState.Get(0, ResourceType.Wood), 0.001f);
        }

        // ------------------------------------------------------------
        // 2. 粮食充足、木材不足 → 建伐木场
        // ------------------------------------------------------------
        [Test]
        public void FoodAdequate_WoodShortage_BuildsLumberMill()
        {
            var h = BuildHarness(1, false);
            h.EcoState.Set(0, ResourceType.Food, 300f);
            h.EcoState.Set(0, ResourceType.Wood, 30f);       // 30 < 40

            h.Governor.OnYear(1);

            Assert.AreEqual(1, h.Buildings.CountByType(0, (int)BuildingType.LumberMill));
        }

        // ------------------------------------------------------------
        // 3. 每势力每年最多自动建造 1 个（多势力各 1 个）
        // ------------------------------------------------------------
        [Test]
        public void OnlyOneBuildPerFactionPerYear()
        {
            var h = BuildHarness(3, false);
            for (int f = 0; f < 3; f++)
            {
                h.EcoState.Set(f, ResourceType.Food, 100f);
                h.EcoState.Set(f, ResourceType.Wood, 80f);
            }

            h.Governor.OnYear(1);

            for (int f = 0; f < 3; f++)
                Assert.AreEqual(1, h.Buildings.TotalCount(f), $"势力 {f} 应只建 1 个");
            Assert.AreEqual(3, h.Buildings.Count);
        }

        // ------------------------------------------------------------
        // 4. 玩家势力默认跳过（保留玩家控制权）
        // ------------------------------------------------------------
        [Test]
        public void PlayerFaction_SkippedByDefault()
        {
            var h = BuildHarness(1, true);
            h.EcoState.Set(0, ResourceType.Food, 100f);
            h.EcoState.Set(0, ResourceType.Wood, 80f);

            h.Governor.OnYear(1);

            Assert.AreEqual(0, h.Buildings.TotalCount(0));
        }

        // ------------------------------------------------------------
        // 5. 开启玩家自动化后，玩家势力也会自动建造
        // ------------------------------------------------------------
        [Test]
        public void PlayerFaction_AutomationEnabled_Builds()
        {
            var h = BuildHarness(1, true);
            h.Governor.EnablePlayerAutomation = true;
            h.EcoState.Set(0, ResourceType.Food, 100f);
            h.EcoState.Set(0, ResourceType.Wood, 80f);

            h.Governor.OnYear(1);

            Assert.AreEqual(1, h.Buildings.CountByType(0, (int)BuildingType.Farm));
        }

        // ------------------------------------------------------------
        // 6. 想建农田但木材不足 → 不建（不负债、不跳类型强行建别的）
        // ------------------------------------------------------------
        [Test]
        public void CannotAffordFarm_NoBuild()
        {
            var h = BuildHarness(1, false);
            h.EcoState.Set(0, ResourceType.Food, 100f);
            h.EcoState.Set(0, ResourceType.Wood, 5f);        // < 农田成本 15

            h.Governor.OnYear(1);

            Assert.AreEqual(0, h.Buildings.TotalCount(0));
            Assert.AreEqual(5f, h.EcoState.Get(0, ResourceType.Wood), 0.001f);
        }

        // ------------------------------------------------------------
        // 7. 石料不足且已进入青铜时代 → 建采石场
        // ------------------------------------------------------------
        [Test]
        public void StoneShortage_BronzeEra_BuildsQuarry()
        {
            var h = BuildHarness(1, false);
            h.Tech.EraValue = (int)Era.BronzeAge;
            h.EcoState.Set(0, ResourceType.Food, 300f);
            h.EcoState.Set(0, ResourceType.Wood, 80f);
            h.EcoState.Set(0, ResourceType.Stone, 10f);       // 10 < 30

            h.Governor.OnYear(1);

            Assert.AreEqual(1, h.Buildings.CountByType(0, (int)BuildingType.Quarry));
        }

        // ------------------------------------------------------------
        // 8. 石料不足但仍在原始时代 → 采石场时代门控（不建 Quarry）
        //    发展型 AI 可转而建立原始时代即可得的伐木场（属正常基建覆盖，非降级乱建）
        // ------------------------------------------------------------
        [Test]
        public void StoneShortage_PrimitiveEra_QuarryBlocked()
        {
            var h = BuildHarness(1, false);
            // Tech 默认 EraValue=0（原始）
            h.EcoState.Set(0, ResourceType.Food, 300f);
            h.EcoState.Set(0, ResourceType.Wood, 80f);
            h.EcoState.Set(0, ResourceType.Stone, 10f);

            h.Governor.OnYear(1);

            // 核心：采石场被时代门控，绝不建在原始时代。
            Assert.AreEqual(0, h.Buildings.CountByType(0, (int)BuildingType.Quarry));
            // 若有建造，只可能是原始时代可得的伐木场（不跨时代乱建）。
            Assert.AreEqual(h.Buildings.Count,
                h.Buildings.CountByType(0, (int)BuildingType.LumberMill));
        }

        // ------------------------------------------------------------
        // 9. 储备均高于安全线但无任何基建 → 发展型 AI 主动补基建覆盖
        //    （首个覆盖目标为伐木场，而非"储备够就完全不建"）
        // ------------------------------------------------------------
        [Test]
        public void AllReservesAdequate_NoCoverage_StartsInfrastructure()
        {
            var h = BuildHarness(1, false);
            h.EcoState.Set(0, ResourceType.Food, 300f);
            h.EcoState.Set(0, ResourceType.Wood, 80f);
            h.EcoState.Set(0, ResourceType.Stone, 80f);
            h.EcoState.Set(0, ResourceType.Gold, 100f);

            h.Governor.OnYear(1);

            // 储备充足但无覆盖：发展型 AI 主动建第一个覆盖目标（伐木场）。
            Assert.AreEqual(1, h.Buildings.CountByType(0, (int)BuildingType.LumberMill));
            Assert.AreEqual(1, h.Buildings.TotalCount(0));
        }
    }
}
