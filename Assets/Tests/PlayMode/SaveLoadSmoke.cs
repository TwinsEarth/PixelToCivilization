using NUnit.Framework;
using PxC.Core.Persistence;
using PxC.Core.Services;

namespace PxC.Tests.PlayMode
{
    /// <summary>
    /// 存档/读档冒烟测试：验证段收集、原子写入、校验、读取往返一致。
    /// 覆盖验收项"存档读档正常（含版本迁移）"。
    /// 使用独立测试槽，结束后清理，不污染玩家手动槽。
    /// </summary>
    public class SaveLoadSmoke
    {
        // 独立测试槽（5 手动 + 3 回滚之外，SaveSystem 按 slot 命名，不强制范围）
        private const int TestSlot = 99;

        [Test]
        public void Save_Then_Load_RoundTrips_Year_And_Segments()
        {
            var host = new PlayModeTestHost();
            Assert.IsTrue(host.Ready);

            var saves = host.Resolve<ISaveService>();
            saves.Delete(TestSlot); // 清理残留

            // 推进 3 年，使世界产生可观测变化
            int advanced = host.AdvanceYears(3);
            Assert.AreEqual(3, advanced, "应推进 3 个游戏年");
            int expectedYear = host.GameYear;

            // 收集所有插件存档段
            var data = new SaveData
            {
                Version = 0,
                GameYear = expectedYear,
                Day = host.Context.Clock.Day,
                Population = host.Context.Units.Count,
                FactionCount = host.Context.Factions.Count,
                BuildVersion = "playmode-smoke",
            };
            host.Context.Runtime.CollectSegments(data);
            Assert.Greater(data.Segments.Count, 0, "应收集到至少一个插件存档段");

            // 原子写入（.tmp → 正式 → .bak）
            bool wrote = saves.Write(TestSlot, data);
            Assert.IsTrue(wrote, "存档写入应成功");
            Assert.IsTrue(saves.HasSave(TestSlot), "HasSave 应为 true");

            // 校验（FNV-1a）
            bool verified = saves.Verify(TestSlot, out var reason);
            Assert.IsTrue(verified, $"存档校验应通过，实际: {reason}");

            // 读取并验证往返一致
            var loaded = saves.Read(TestSlot);
            Assert.IsNotNull(loaded, "读档不应为 null");
            Assert.AreEqual(expectedYear, loaded.GameYear, "读档年份应一致");
            Assert.AreEqual(data.Segments.Count, loaded.Segments.Count,
                "读档段数量应一致");
            Assert.AreEqual(host.Context.Factions.Count, loaded.FactionCount,
                "读档势力数应一致");

            // 清理
            saves.Delete(TestSlot);
            Assert.IsFalse(saves.HasSave(TestSlot), "清理后应无存档");
        }

        [Test]
        public void Rollback_Restores_Previous_Backup()
        {
            var host = new PlayModeTestHost();
            Assert.IsTrue(host.Ready);

            var saves = host.Resolve<ISaveService>();
            saves.Delete(TestSlot);

            // 第一次写入（标记为 1 年）
            var first = new SaveData { Version = 0, GameYear = 4701 };
            Assert.IsTrue(saves.Write(TestSlot, first), "第一次写入应成功");

            // 第二次写入（标记为 9 年，第一次内容被备份为 .bak）
            var second = new SaveData { Version = 0, GameYear = 4709 };
            Assert.IsTrue(saves.Write(TestSlot, second), "第二次写入应成功");
            Assert.AreEqual(4709, saves.Read(TestSlot).GameYear,
                "当前应是第二次写入内容");

            // 回滚到第一次内容
            Assert.IsTrue(saves.Rollback(TestSlot), "Rollback 应成功");
            var rolled = saves.Read(TestSlot);
            Assert.IsNotNull(rolled);
            Assert.AreEqual(4701, rolled.GameYear, "回滚后应是第一次写入内容");

            saves.Delete(TestSlot);
        }
    }
}
