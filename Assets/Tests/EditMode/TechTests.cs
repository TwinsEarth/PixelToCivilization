using NUnit.Framework;
using PxC.Gameplay.Tech;
using PxC.Plugins.Tech;

namespace PxC.Tests.EditMode
{
    public sealed class TechTests
    {
        [Test]
        public void AdvanceResearch_ReachesThreshold_AdvancesEra()
        {
            var state = new TechState();
            state.Bind(1);
            var service = new TechService(state);

            float required = TechService.ProgressRequired(0);
            int levels = service.AdvanceResearch(0, required);

            Assert.AreEqual(1, levels);
            Assert.AreEqual(Era.BronzeAge, service.GetEra(0));
        }

        [Test]
        public void AdvanceResearch_InsufficientPoints_DoesNotAdvance()
        {
            var state = new TechState();
            state.Bind(1);
            var service = new TechService(state);

            int levels = service.AdvanceResearch(0, 50f); // 需要 100
            Assert.AreEqual(0, levels);
            Assert.AreEqual(Era.Primitive, service.GetEra(0));
        }

        [Test]
        public void HasEra_ReflectsCurrentEra()
        {
            var state = new TechState();
            state.Bind(1);
            var service = new TechService(state);

            Assert.IsTrue(service.HasEra(0, Era.Primitive));
            Assert.IsFalse(service.HasEra(0, Era.BronzeAge));

            service.AdvanceResearch(0, TechService.ProgressRequired(0));
            Assert.IsTrue(service.HasEra(0, Era.BronzeAge));
        }

        [Test]
        public void IsResearched_EraId_ChecksUnlockedEra()
        {
            var state = new TechState();
            state.Bind(1);
            var service = new TechService(state);

            Assert.IsFalse(service.IsResearched(0, "era:1"));
            service.AdvanceResearch(0, TechService.ProgressRequired(0));
            Assert.IsTrue(service.IsResearched(0, "era:1"));
        }
    }
}
