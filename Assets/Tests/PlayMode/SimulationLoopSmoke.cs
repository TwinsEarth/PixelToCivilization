using NUnit.Framework;
using PxC.Gameplay.Services;

namespace PxC.Tests.PlayMode
{
    /// <summary>
    /// 模拟循环冒烟测试：验证连续多年推进下，时间步进、年结、人口/经济聚合
    /// 正常运转，核心插件不被故障隔离。覆盖验收项"核心模拟循环正常"。
    /// </summary>
    public class SimulationLoopSmoke
    {
        private const int PlayerFaction = 0;

        [Test]
        public void Simulation_Advances_Years_And_Keeps_Core_Systems_Alive()
        {
            var host = new PlayModeTestHost();
            Assert.IsTrue(host.Ready);

            var pop = host.Resolve<IPopulationService>();
            var eco = host.Resolve<IEconomyService>();

            int startYear = host.GameYear;
            int startPop = pop.Total(PlayerFaction);
            Assert.Greater(startPop, 0, "玩家势力开局应有初始人口");

            // 连续推进 10 个游戏年
            int advanced = host.AdvanceYears(10);
            Assert.AreEqual(10, advanced, "应推进 10 个游戏年");
            Assert.AreEqual(startYear + 10, host.GameYear,
                "时间步进应精确累加 10 年");

            // 玩家势力仍存续，人口为合理非负值
            int endPop = pop.Total(PlayerFaction);
            Assert.GreaterOrEqual(endPop, 0, "人口不应为负");

            // 年度聚合后核心服务仍可解析（没有插件被故障隔离）
            Assert.IsNotNull(host.Resolve<IEconomyService>(), "经济服务应存活");
            Assert.IsNotNull(host.Resolve<IPopulationService>(), "人口服务应存活");
            Assert.IsNotNull(host.Resolve<IBuildingService>(), "建筑服务应存活");
        }

        [Test]
        public void Weather_System_Produces_Valid_Modifiers()
        {
            var host = new PlayModeTestHost();
            Assert.IsTrue(host.Ready);

            bool hasWeather = host.TryResolve<IWeatherService>(out var weather);
            Assert.IsTrue(hasWeather, "天气服务应已注册");

            host.AdvanceYears(2);

            // 农业/移动系数应为有限、非负值（异常 NaN/负无穷会污染聚合）
            float crop = weather.CropYieldModifier(PlayerFaction);
            float move = weather.MovementModifier(PlayerFaction);
            Assert.IsTrue(!float.IsNaN(crop) && !float.IsInfinity(crop) && crop >= 0f,
                $"农业系数应为有限非负值，实际 {crop}");
            Assert.IsTrue(!float.IsNaN(move) && !float.IsInfinity(move) && move >= 0f,
                $"移动系数应为有限非负值，实际 {move}");
        }

        [Test]
        public void LongRun_Advances_Without_Numeric_Overflow()
        {
            var host = new PlayModeTestHost();
            Assert.IsTrue(host.Ready);

            var pop = host.Resolve<IPopulationService>();
            int startYear = host.GameYear;

            // 长跨度推进 50 年，检查数值不溢出、系统不崩溃
            int advanced = host.AdvanceYears(50);
            Assert.AreEqual(50, advanced);
            Assert.AreEqual(startYear + 50, host.GameYear);

            // 人口仍为合理范围内的非负整数（未溢出为负）
            int total = pop.Total(PlayerFaction);
            Assert.GreaterOrEqual(total, 0, "长跨度后人口不应溢出为负");
        }
    }
}
