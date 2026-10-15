using NUnit.Framework;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;

namespace PxC.Tests.PlayMode
{
    /// <summary>
    /// 移动安全冒烟测试：验证地面作战部队（骑兵/阵兵/战车）在生成与自主移动后
    /// 始终位于陆地，从回归口径上杜绝"骑兵被拉进大陆湖里困住"。
    /// 注意：当前默认地理为 PermissiveGeography（全陆地占位），真实水陆判定
    /// 需地图插件接入；接入前该测试的"不进湖"为占位口径（未验证）。
    /// </summary>
    public class MovementSafetySmoke
    {
        [Test]
        public void Ground_Forces_Start_And_Stay_On_Land()
        {
            var host = new PlayModeTestHost();
            Assert.IsTrue(host.Ready);

            var geo = host.Resolve<IGeographyQuery>();
            var units = host.Context.Units;

            int groundCount = CountGroundUnits(units);
            Assert.Greater(groundCount, 0, "应存在开局地面作战单位");

            // 初始：所有地面单位位置为陆地（出生点不应在水域）
            AssertGroundUnitsOnLand(units, geo, "初始生成");

            // 逐帧推进（真实帧间隔），模拟约 10 真实秒的自主移动
            host.StepFrames(600, 0.016f);

            // 推进后：所有地面单位仍在陆地（自主移动/寻敌/追击不得进入水域）
            AssertGroundUnitsOnLand(units, geo, "逐帧推进后");
        }

        [Test]
        public void Ground_Forces_Remain_Movable_After_Steps()
        {
            var host = new PlayModeTestHost();
            Assert.IsTrue(host.Ready);

            var units = host.Context.Units;
            int before = CountGroundUnits(units);

            host.StepFrames(300, 0.016f);

            int after = CountGroundUnits(units);
            // 地面单位不应在无战争/无灾害的纯推进中凭空消失
            Assert.AreEqual(before, after,
                "无战斗纯推进时地面单位数量应保持稳定");
        }

        // ------------------------------------------------------------
        // 辅助
        // ------------------------------------------------------------
        private static int CountGroundUnits(UnitRegistry units)
        {
            int n = 0;
            for (int i = 0; i < units.Capacity; i++)
            {
                ref var r = ref units.GetSlotRef(i);
                if (r.Alive && r.Domain == EntityDomain.Land) n++;
            }
            return n;
        }

        private static void AssertGroundUnitsOnLand(
            UnitRegistry units, IGeographyQuery geo, string phase)
        {
            for (int i = 0; i < units.Capacity; i++)
            {
                ref var r = ref units.GetSlotRef(i);
                if (!r.Alive || r.Domain != EntityDomain.Land) continue;

                bool isWater = geo.IsWater(r.Position.x, r.Position.z);
                Assert.IsFalse(isWater,
                    $"[{phase}] 地面单位(势力{r.Faction},型号{r.ModelId})位于水域 " +
                    $"({r.Position.x},{r.Position.z})，骑兵被拉进湖里");
            }
        }
    }
}
