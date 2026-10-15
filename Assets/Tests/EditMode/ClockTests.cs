using NUnit.Framework;
using PxC.Core.Time;

namespace PxC.Tests.EditMode
{
    public sealed class ClockTests
    {
        [Test]
        public void Advance_60s_AdvancesOneYear()
        {
            var c = new GameClock();
            c.SnapToStart();
            int years = 0;
            c.Advance(60f, y => years++, out _);
            Assert.AreEqual(1, years);
            Assert.AreEqual(GameClock.StartGameYear + 1, c.Year);
        }

        [Test]
        public void Paused_NoAdvance()
        {
            var c = new GameClock();
            c.Paused = true;
            int years = 0;
            c.Advance(100f, y => years++, out _);
            Assert.AreEqual(0, years);
        }

        [Test]
        public void Speed3x_60s_AdvancesThreeYears()
        {
            var c = new GameClock();
            c.SnapToStart();
            c.Speed = 3f;
            int years = 0;
            c.Advance(60f, y => years++, out _);
            Assert.AreEqual(3, years);
            Assert.AreEqual(GameClock.StartGameYear + 3, c.Year);
        }

        [Test]
        public void NotRunning_NoAdvance()
        {
            var c = new GameClock();
            c.Running = false;
            int years = 0;
            c.Advance(60f, y => years++, out _);
            Assert.AreEqual(0, years);
        }

        [Test]
        public void Restore_SetsYearAndDay()
        {
            var c = new GameClock();
            c.Restore(5000, 120f);
            Assert.AreEqual(5000, c.Year);
            Assert.AreEqual(120f, c.Day);
        }
    }
}
