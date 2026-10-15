using System;
using NUnit.Framework;
using PxC.Core.Events;
using PxC.Host;

namespace PxC.Tests.EditMode
{
    public sealed class EventBusTests
    {
        [Test]
        public void Publish_NotifiesSubscriber()
        {
            var bus = new StrongEventBus();
            int received = 0;
            bus.Subscribe<YearAdvancedEvent>(e => received++);
            bus.Publish(new YearAdvancedEvent { GameYear = 100 });
            Assert.AreEqual(1, received);
        }

        [Test]
        public void PublishDeferred_OnlyDispatchesOnDispatchPending()
        {
            var bus = new StrongEventBus();
            int received = 0;
            bus.Subscribe<YearAdvancedEvent>(e => received++);
            bus.PublishDeferred(new YearAdvancedEvent());
            Assert.AreEqual(0, received);
            bus.DispatchPending();
            Assert.AreEqual(1, received);
        }

        [Test]
        public void Unsubscribe_StopsNotifying()
        {
            var bus = new StrongEventBus();
            int received = 0;
            Action<YearAdvancedEvent> handler = e => received++;
            bus.Subscribe(handler);
            bus.Unsubscribe(handler);
            bus.Publish(new YearAdvancedEvent());
            Assert.AreEqual(0, received);
        }

        [Test]
        public void MultipleSubscribers_AllNotified()
        {
            var bus = new StrongEventBus();
            int a = 0, b = 0;
            bus.Subscribe<YearAdvancedEvent>(e => a++);
            bus.Subscribe<YearAdvancedEvent>(e => b++);
            bus.Publish(new YearAdvancedEvent());
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }

        [Test]
        public void HasSubscribers_ReflectsState()
        {
            var bus = new StrongEventBus();
            Assert.IsFalse(bus.HasSubscribers<YearAdvancedEvent>());
            bus.Subscribe<YearAdvancedEvent>(e => { });
            Assert.IsTrue(bus.HasSubscribers<YearAdvancedEvent>());
        }
    }
}
