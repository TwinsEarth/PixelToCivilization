using System.Collections.Generic;
using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Core.World;
using PxC.Host;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Plugins.Economy;
using PxC.Plugins.WarReparations;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 战争赔款测试（v10.7.7）：
    /// 割地/附庸化和约后建立分期赔款条约；逐年支付；余额不足全额赔付不超扣；
    /// 到期完成；存档段往返后条约保留并继续履行。
    /// 使用真实 EconomyService（SoA 资源）。
    /// </summary>
    public sealed class WarReparationsTests
    {
        private sealed class Harness
        {
            public ServiceRegistry Services;
            public FactionRegistry Factions;
            public EconomyState EcoState;
            public StrongEventBus Bus;
            public WarReparationsPlugin Plugin;
            public List<ReparationTreatySignedEvent> SignedEvents;
            public List<ReparationsPaymentEvent> PaymentEvents;
            public List<ReparationsCompletedEvent> CompletedEvents;
        }

        private static Harness Build(int factionCount)
        {
            var services = new ServiceRegistry();
            var factions = new FactionRegistry();
            for (int i = 0; i < factionCount; i++)
                factions.Add("F" + i, false, i * 1000f, 0f, i);

            var ecoState = new EconomyState();
            ecoState.Bind(factionCount);
            var economy = new EconomyService(ecoState);
            services.Register<IEconomyService>(economy);
            services.Register<FactionRegistry>(factions);

            var bus = new StrongEventBus();
            var signed = new List<ReparationTreatySignedEvent>();
            var payments = new List<ReparationsPaymentEvent>();
            var completed = new List<ReparationsCompletedEvent>();
            bus.Subscribe<ReparationTreatySignedEvent>(signed.Add);
            bus.Subscribe<ReparationsPaymentEvent>(payments.Add);
            bus.Subscribe<ReparationsCompletedEvent>(completed.Add);

            var plugin = new WarReparationsPlugin();
            var ctx = new PluginContext(
                WarReparationsPlugin.PluginIdValue,
                bus, services, new ChunkedWorld(),
                new CapabilitySystem(), _ => { });
            plugin.OnRegister(ctx);
            plugin.OnInitialize();

            return new Harness
            {
                Services = services,
                Factions = factions,
                EcoState = ecoState,
                Bus = bus,
                Plugin = plugin,
                SignedEvents = signed,
                PaymentEvents = payments,
                CompletedEvents = completed,
            };
        }

        // ------------------------------------------------------------
        // 1. 割地和约 → 金币条约（5 年 × max(5, 存量×5%)）+ 无战略资源不加条约
        // ------------------------------------------------------------
        [Test]
        public void CoercedPeace_SignsGoldTreaty()
        {
            var h = Build(2);
            h.EcoState.Set(1, ResourceType.Gold, 100f); // 弱者存量 100

            h.Bus.Publish(new CoercedPeaceEvent
            {
                Stronger = 0,
                Weaker = 1,
                SettlementsCeded = 1,
                WarStartYear = 1,
                GameYear = 10,
            });

            Assert.AreEqual(1, h.SignedEvents.Count, "弱者无战略资源，应只有金币条约");
            var t = h.SignedEvents[0];
            Assert.AreEqual(ResourceType.Gold, t.Type);
            Assert.AreEqual(1, t.Payer);
            Assert.AreEqual(0, t.Receiver);
            // perYear = max(5, 100×0.05=5) = 5；5 年 → 总额 25
            Assert.AreEqual(25f, t.TotalAmount, 0.001f);
            Assert.AreEqual(5, t.Years);
        }

        // ------------------------------------------------------------
        // 2. 割地和约：弱者有战略资源（铁）→ 追加 3 年×2 铁条约
        // ------------------------------------------------------------
        [Test]
        public void CoercedPeace_SignsGoldAndStrategic()
        {
            var h = Build(2);
            h.EcoState.Set(1, ResourceType.Gold, 100f);
            h.EcoState.Set(1, ResourceType.Iron, 10f);

            h.Bus.Publish(new CoercedPeaceEvent
            {
                Stronger = 0,
                Weaker = 1,
                SettlementsCeded = 1,
                WarStartYear = 1,
                GameYear = 10,
            });

            Assert.AreEqual(2, h.SignedEvents.Count, "应有金币 + 铁两条条约");
            Assert.AreEqual(ResourceType.Iron, h.SignedEvents[1].Type);
            Assert.AreEqual(6f, h.SignedEvents[1].TotalAmount, 0.001f); // 2/年 × 3 年
        }

        // ------------------------------------------------------------
        // 3. 逐年支付：5 年条约，每年付 5；到期发布 Completed
        // ------------------------------------------------------------
        [Test]
        public void Treaty_PaysYearlyThenCompletes()
        {
            var h = Build(2);
            h.EcoState.Set(1, ResourceType.Gold, 100f);
            h.Bus.Publish(new CoercedPeaceEvent
            {
                Stronger = 0, Weaker = 1, SettlementsCeded = 1,
                WarStartYear = 1, GameYear = 10,
            });

            // 第 11~15 年共 5 次年度结算
            for (int y = 11; y <= 15; y++)
                h.Plugin.OnYear(y);

            Assert.AreEqual(5, h.PaymentEvents.Count, "应每年付款一次");
            Assert.AreEqual(5f, h.PaymentEvents[0].Amount, 0.001f);
            Assert.AreEqual(1, h.CompletedEvents.Count, "第 5 年应完成条约");
            Assert.AreEqual(25f, h.CompletedEvents[0].TotalPaid, 0.001f);
            // 弱者 100-25=75，强者 25
            Assert.AreEqual(75f, h.EcoState.Get(1, ResourceType.Gold), 0.001f);
            Assert.AreEqual(25f, h.EcoState.Get(0, ResourceType.Gold), 0.001f);
        }

        // ------------------------------------------------------------
        // 4. 附庸化和约 → 金币 3 年条约（首期称臣赔款）
        // ------------------------------------------------------------
        [Test]
        public void Vassalized_SignsGoldTreaty3Years()
        {
            var h = Build(2);
            h.EcoState.Set(1, ResourceType.Gold, 200f); // perYear = max(5, 10) = 10

            h.Bus.Publish(new VassalizedEvent
            {
                Suzerain = 0,
                Vassal = 1,
                WarStartYear = 1,
                GameYear = 10,
            });

            Assert.AreEqual(1, h.SignedEvents.Count);
            var t = h.SignedEvents[0];
            Assert.AreEqual(ResourceType.Gold, t.Type);
            Assert.AreEqual(3, t.Years);
            Assert.AreEqual(30f, t.TotalAmount, 0.001f); // 10 × 3

            h.Plugin.OnYear(11);
            Assert.AreEqual(1, h.PaymentEvents.Count);
            Assert.AreEqual(10f, h.PaymentEvents[0].Amount, 0.001f);
            Assert.AreEqual(190f, h.EcoState.Get(1, ResourceType.Gold), 0.001f);
            Assert.AreEqual(10f, h.EcoState.Get(0, ResourceType.Gold), 0.001f);
        }

        // ------------------------------------------------------------
        // 5. 余额不足 → 全额赔付（不超扣、不产生负资源），后续年度付 0
        // ------------------------------------------------------------
        [Test]
        public void Shortfall_PaysAllAvailable_NoNegative()
        {
            var h = Build(2);
            h.EcoState.Set(1, ResourceType.Gold, 3f); // 低于年付下限 5
            h.Bus.Publish(new CoercedPeaceEvent
            {
                Stronger = 0, Weaker = 1, SettlementsCeded = 1,
                WarStartYear = 1, GameYear = 10,
            });
            Assert.AreEqual(1, h.SignedEvents.Count);

            h.Plugin.OnYear(11);
            Assert.AreEqual(1, h.PaymentEvents.Count);
            Assert.AreEqual(3f, h.PaymentEvents[0].Amount, 0.001f, "余额不足应全额赔付");
            Assert.AreEqual(0f, h.EcoState.Get(1, ResourceType.Gold), 0.001f, "不得超扣为负");
            Assert.AreEqual(3f, h.EcoState.Get(0, ResourceType.Gold), 0.001f);

            h.Plugin.OnYear(12);
            Assert.AreEqual(1, h.PaymentEvents.Count, "余额为 0 不再产生付款事件");
            Assert.AreEqual(0f, h.EcoState.Get(1, ResourceType.Gold), 0.001f);
        }

        // ------------------------------------------------------------
        // 6. 存档段往返：GetSaveSegment → Restore 后条约保留、继续支付
        // ------------------------------------------------------------
        [Test]
        public void SaveSegment_RoundTrip_ContinuesPayment()
        {
            var h = Build(2);
            h.EcoState.Set(1, ResourceType.Gold, 100f);
            h.Bus.Publish(new CoercedPeaceEvent
            {
                Stronger = 0, Weaker = 1, SettlementsCeded = 1,
                WarStartYear = 1, GameYear = 10,
            });
            h.Plugin.OnYear(11); // 已付 1 年，剩 4 年

            ISaveSegment seg = h.Plugin.GetSaveSegment();
            Assert.IsNotNull(seg, "生效条约必须产生存档段");
            Assert.IsInstanceOf<ReparationSaveSegment>(seg);
            var rs = (ReparationSaveSegment)seg;
            Assert.AreEqual(1, rs.Count);
            Assert.AreEqual(4, rs.YearsLeft[0], "已付 1 年应剩 4 年");

            // 模拟读档：新插件恢复
            var h2 = Build(2);
            h2.EcoState.Set(1, ResourceType.Gold, h.EcoState.Get(1, ResourceType.Gold));
            h2.EcoState.Set(0, ResourceType.Gold, h.EcoState.Get(0, ResourceType.Gold));
            var ctx2 = new PluginContext(
                WarReparationsPlugin.PluginIdValue,
                h2.Bus, h2.Services, new ChunkedWorld(),
                new CapabilitySystem(), _ => { });
            h2.Plugin.OnRegister(ctx2);
            h2.Plugin.OnInitialize();
            h2.Plugin.RestoreSaveSegment(rs, 1);

            // 继续履行剩余 4 年
            for (int y = 12; y <= 15; y++)
                h2.Plugin.OnYear(y);

            Assert.AreEqual(1, h2.CompletedEvents.Count, "恢复后应继续支付直到完成");
            Assert.AreEqual(25f, h2.CompletedEvents[0].TotalPaid, 0.001f,
                "累计实付 = 恢复前 5 + 恢复后 20");
            Assert.AreEqual(75f, h2.EcoState.Get(1, ResourceType.Gold), 0.001f);
            Assert.AreEqual(25f, h2.EcoState.Get(0, ResourceType.Gold), 0.001f);
        }

        // ------------------------------------------------------------
        // 7. 无条约 → 不产生存档段
        // ------------------------------------------------------------
        [Test]
        public void NoTreaties_NoSaveSegment()
        {
            var h = Build(2);
            Assert.IsNull(h.Plugin.GetSaveSegment());
        }
    }
}
