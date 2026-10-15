using System.Collections.Generic;
using NUnit.Framework;
using PxC.Core.World;
using PxC.Host;

namespace PxC.Tests.EditMode
{
    public sealed class TopologyTests
    {
        private static PluginRuntime MakeRuntime()
        {
            return new PluginRuntime(
                new StrongEventBus(),
                new ServiceRegistry(),
                new ChunkedWorld(),
                new CapabilitySystem(),
                GameBootstrapHostVersion,
                UnityEngine.Debug.Log);
        }

        private const int GameBootstrapHostVersion = 10;

        private static int IndexOf(PluginRuntime rt, string id)
        {
            var sims = rt.SimPlugins;
            for (int i = 0; i < sims.Count; i++)
                if (sims[i].Id == id) return i;
            return -1;
        }

        [Test]
        public void BuildOrder_RespectsDependencies()
        {
            var rt = MakeRuntime();
            rt.Discover(new FakePlugin("b", deps: Deps.Of(("a", 1))));
            rt.Discover(new FakePlugin("a"));
            Assert.IsTrue(rt.BuildOrder(out _));

            int ia = IndexOf(rt, "a");
            int ib = IndexOf(rt, "b");
            Assert.GreaterOrEqual(ia, 0);
            Assert.GreaterOrEqual(ib, 0);
            Assert.Less(ia, ib);
        }

        [Test]
        public void BuildOrder_DetectsCycle()
        {
            var rt = MakeRuntime();
            rt.Discover(new FakePlugin("a", deps: Deps.Of(("b", 1))));
            rt.Discover(new FakePlugin("b", deps: Deps.Of(("a", 1))));
            Assert.IsFalse(rt.BuildOrder(out var errors));
            CollectionAssert.IsNotEmpty(errors);
        }

        [Test]
        public void BuildOrder_MissingDependency_Fails()
        {
            var rt = MakeRuntime();
            rt.Discover(new FakePlugin("a", deps: Deps.Of(("missing", 1))));
            Assert.IsFalse(rt.BuildOrder(out _));
        }

        [Test]
        public void BuildOrder_InsufficientVersion_Fails()
        {
            var rt = MakeRuntime();
            var a = new FakePlugin("a") { Version = 1 };
            var b = new FakePlugin("b", deps: Deps.Of(("a", 2)));
            rt.Discover(a);
            rt.Discover(b);
            Assert.IsFalse(rt.BuildOrder(out _));
        }

        [Test]
        public void InitializeAll_StopsWhenDependencyFails()
        {
            var rt = MakeRuntime();
            var a = new FakePlugin("a") { InitializeShouldFail = true };
            var b = new FakePlugin("b", deps: Deps.Of(("a", 1)));
            rt.Discover(a);
            rt.Discover(b);
            Assert.IsTrue(rt.BuildOrder(out _));
            rt.InitializeAll();
            Assert.IsTrue(a.Initialized, "a 已进入初始化但抛异常（被标记故障）");
            Assert.IsFalse(b.Initialized, "依赖失败时 b 不应被初始化");
        }
    }
}
