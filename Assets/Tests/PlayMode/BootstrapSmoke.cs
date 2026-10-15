using NUnit.Framework;
using PxC.Bootstrap;
using PxC.Core.Services;
using PxC.Core.Time;
using PxC.Gameplay.Services;

namespace PxC.Tests.PlayMode
{
    /// <summary>
    /// 启动冒烟测试：验证宿主成功组装、基础服务可用、开局世界正确。
    /// 覆盖验收项"启动场景可运行、无阻断性异常"。
    /// </summary>
    public class BootstrapSmoke
    {
        [Test]
        public void Host_Starts_And_Core_Services_Are_Registered()
        {
            var host = new PlayModeTestHost();

            // 宿主成功启动（安全模式/装配失败时 TestContext 为 null）
            Assert.IsTrue(host.Ready, "宿主应成功启动（GameBootstrap.TestContext 非空）");
            Assert.Greater(host.PluginCount, 0, "应发现并装配至少一个插件");

            // 核心服务全部可用
            Assert.IsNotNull(host.Resolve<IClock>(), "IClock 未注册");
            Assert.IsNotNull(host.Resolve<IGeographyQuery>(), "IGeographyQuery 未注册");
            Assert.IsNotNull(host.Resolve<ISaveService>(), "ISaveService 未注册");

            // 玩法服务全部可用（Tier-0 九大插件）
            Assert.IsNotNull(host.Resolve<IEconomyService>(), "IEconomyService 未注册");
            Assert.IsNotNull(host.Resolve<IPopulationService>(), "IPopulationService 未注册");
            Assert.IsNotNull(host.Resolve<IBuildingService>(), "IBuildingService 未注册");
            Assert.IsNotNull(host.Resolve<ICombatService>(), "ICombatService 未注册");
        }

        [Test]
        public void NewGame_Starts_At_Start_Year()
        {
            var host = new PlayModeTestHost();
            Assert.IsTrue(host.Ready);

            // 开局对齐公元 1700 = 游戏年 4700，不触发年结
            Assert.AreEqual(GameClock.StartGameYear, host.GameYear,
                "新游戏应静默对齐到开局游戏年");
            Assert.AreEqual(4700, host.GameYear);
        }

        [Test]
        public void NewGame_Has_Five_Factions_And_Initial_Ground_Forces()
        {
            var host = new PlayModeTestHost();
            Assert.IsTrue(host.Ready);

            // 开局 5 势力：华夏（玩家）+ 四裔
            var factions = host.Context.Factions;
            Assert.AreEqual(NewGameSetup.InitialFactionCount, factions.Count,
                "开局势力数应为 5");
            Assert.IsNotNull(factions.Player, "应存在玩家势力");
            Assert.AreEqual("华夏部落", factions.Player.Name);

            // 每势力开局 3 名阵兵 → 至少 15 个地面单位
            var units = host.Context.Units;
            Assert.GreaterOrEqual(units.Count,
                NewGameSetup.InitialFactionCount * 3,
                "开局每个势力应编组 3 个阵兵");
        }

        [Test]
        public void ContentPacks_Are_Registered()
        {
            var host = new PlayModeTestHost();
            Assert.IsTrue(host.Ready);

            // 朝代内容包（22 朝代）
            bool hasDynasty = host.TryResolve<IDynastyService>(out var dynasty);
            Assert.IsTrue(hasDynasty, "IDynastyService 应已注册");
            Assert.AreEqual(22, dynasty.Dynasties.Count, "朝代序列应为 22 个");

            // 公历换算：游戏年 4949 = 公元 1949；游戏年 3000 = 公元 0
            Assert.AreEqual(1949, dynasty.ToAdYear(4949), "游戏年→公历换算错误");
            Assert.AreEqual(0, dynasty.ToAdYear(3000), "游戏年 3000 应为公元 0");
            Assert.AreEqual(4949, dynasty.ToGameYear(1949), "公历→游戏年换算错误");
        }
    }
}
