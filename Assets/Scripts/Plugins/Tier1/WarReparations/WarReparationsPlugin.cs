using System.Collections.Generic;
using UnityEngine;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;

namespace PxC.Plugins.WarReparations
{
    /// <summary>
    /// 战争赔款（Tier-1，v10.7.7）。补齐战争结局梯度中的经济结算：
    /// 割地（<see cref="CoercedPeaceEvent"/>）与附庸化（<see cref="VassalizedEvent"/>）
    /// 之后，败方必须向胜方支付多年分期赔款，作为和约的经济代价。
    ///
    /// 赔款规则（可复算）：
    /// 1. 割地赔款（CoercedPeace）：
    ///    - 金币条约：每年 = max(5, 弱者金币存量 × 5%)，持续 5 年（总额 ≈ 存量 25%）；
    ///    - 战略资源条约：若弱者持有任一战略资源（青铜/铁/煤/石油/钢）存量 &gt; 0，
    ///      取弱者存量最高的战略资源，每年 2 单位，持续 3 年（总额 6 单位）。
    /// 2. 附庸化赔款（Vassalized，与年度纳贡叠加的"称臣首期"）：
    ///    - 金币条约：每年 = max(5, 弱者金币存量 × 5%)，持续 3 年（总额 ≈ 存量 15%）。
    ///      叠加纳贡后峰值约 15%/年，可承受且足以形成经济代价。
    ///
    /// 结算（OnYear，按条约建立顺序，先金币后资源）：
    /// - 每年 TrySpend 当年份额；余额不足则支付全部可用余额（不超扣、不产生负资源）；
    /// - 到期条约发布 <see cref="ReparationsCompletedEvent"/> 并移除。
    ///
    /// 条约是跨年度持续状态，故本插件产生存档段（<see cref="ReparationSaveSegment"/>），
    /// 读档后继续履行。灭国没收（国库直接转移）由 ConfiscationPlugin 处理，不在此插件。
    /// </summary>
    public sealed class WarReparationsPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.warreparations";

        /// <summary>割地赔款金币条约年数。</summary>
        public const int GoldReparationYears = 5;

        /// <summary>附庸化赔款金币条约年数（短于割地：附庸还有年度纳贡）。</summary>
        public const int VassalGoldReparationYears = 3;

        /// <summary>金币年付 = 弱者金币存量 × 该比例。</summary>
        public const float GoldReparationRate = 0.05f;

        /// <summary>金币年付下限（即使存量很低也要尽力赔付）。</summary>
        public const float MinimumGoldReparationPerYear = 5f;

        /// <summary>战略资源条约年数。</summary>
        public const int ResourceReparationYears = 3;

        /// <summary>战略资源年付单位数。</summary>
        public const float ResourceReparationPerYear = 2f;

        /// <summary>战略资源（青铜/铁/煤/石油/钢）。</summary>
        private static readonly ResourceType[] _strategic =
            { ResourceType.Bronze, ResourceType.Iron, ResourceType.Coal,
              ResourceType.Oil, ResourceType.Steel };

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(4)
            {
                new PluginDependency("pxc.warresolution", 1),
                new PluginDependency("pxc.coercedpeace", 1),
                new PluginDependency("pxc.vassalization", 1),
                new PluginDependency("pxc.economy", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(1)
            {
                new CapabilityClaim(Capability.Save),
            };

        /// <summary>生效中的赔款条约（按建立顺序；同一付款方多条约按序支付）。</summary>
        private readonly List<ReparationTreaty> _treaties = new List<ReparationTreaty>(4);

        private IPluginContext _ctx;

        public string Id => PluginIdValue;
        public string DisplayName => "战争赔款";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;
        public SimulationLayer Layer => SimulationLayer.P1Tactical;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context) => _ctx = context;

        public void OnInitialize()
        {
            _ctx.Events.Subscribe<CoercedPeaceEvent>(OnCoercedPeace);
            _ctx.Events.Subscribe<VassalizedEvent>(OnVassalized);
        }

        public void OnTick(float simDt) { }

        public void OnYear(int gameYear)
        {
            if (_treaties.Count == 0) return;
            if (!_ctx.Services.TryResolve<IEconomyService>(out var economy)) return;

            // 逆序遍历以便安全移除已到期条约。
            for (int i = _treaties.Count - 1; i >= 0; i--)
            {
                ReparationTreaty t = _treaties[i];

                float paid = 0f;
                if (economy.TrySpend(t.Payer, t.Type, t.PerYear))
                {
                    paid = t.PerYear;
                }
                else
                {
                    float available = economy.Get(t.Payer, t.Type);
                    if (available > 0f && economy.TrySpend(t.Payer, t.Type, available))
                        paid = available; // 余额不足则全额赔付，不欠款
                }

                if (paid > 0f)
                {
                    economy.Add(t.Receiver, t.Type, paid);
                    _ctx.Events.Publish(new ReparationsPaymentEvent
                    {
                        Payer = t.Payer,
                        Receiver = t.Receiver,
                        Type = t.Type,
                        Amount = paid,
                        GameYear = gameYear,
                    });
                }

                t.TotalPaid += paid;
                t.YearsLeft--;

                if (t.YearsLeft <= 0)
                {
                    _treaties.RemoveAt(i);
                    _ctx.Events.Publish(new ReparationsCompletedEvent
                    {
                        Payer = t.Payer,
                        Receiver = t.Receiver,
                        Type = t.Type,
                        TotalPaid = t.TotalPaid,
                        GameYear = gameYear,
                    });
                    _ctx.Log($"[战争赔款] 势力 {t.Payer} 对势力 {t.Receiver} 的" +
                             $"{t.Type} 赔款履行完毕（共 {t.TotalPaid:F0}）");
                }
            }
        }

        // ============================================================
        // 割地（压制性和约）→ 金币 5 年 + 战略资源 3 年
        // ============================================================
        private void OnCoercedPeace(CoercedPeaceEvent e)
        {
            if (!_ctx.Services.TryResolve<IEconomyService>(out var economy)) return;
            if (economy == null) return;

            float weakerGold = economy.Get(e.Weaker, ResourceType.Gold);
            if (weakerGold > 0f)
            {
                float perYear = Mathf.Max(MinimumGoldReparationPerYear,
                    weakerGold * GoldReparationRate);
                SignTreaty(e.Weaker, e.Stronger, ResourceType.Gold,
                    perYear, GoldReparationYears, e.GameYear);
            }

            ResourceType strategic = BestStrategic(economy, e.Weaker);
            if (strategic != ResourceType.None)
            {
                SignTreaty(e.Weaker, e.Stronger, strategic,
                    ResourceReparationPerYear, ResourceReparationYears, e.GameYear);
            }
        }

        // ============================================================
        // 附庸化（称臣）→ 金币 3 年首期赔款（与年度纳贡叠加）
        // ============================================================
        private void OnVassalized(VassalizedEvent e)
        {
            if (!_ctx.Services.TryResolve<IEconomyService>(out var economy)) return;
            if (economy == null) return;

            float weakerGold = economy.Get(e.Vassal, ResourceType.Gold);
            if (weakerGold <= 0f) return;

            float perYear = Mathf.Max(MinimumGoldReparationPerYear,
                weakerGold * GoldReparationRate);
            SignTreaty(e.Vassal, e.Suzerain, ResourceType.Gold,
                perYear, VassalGoldReparationYears, e.GameYear);
        }

        // ============================================================
        // 条约建立
        // ============================================================
        private void SignTreaty(
            int payer, int receiver, ResourceType type,
            float perYear, int years, int gameYear)
        {
            var treaty = new ReparationTreaty
            {
                Payer = payer,
                Receiver = receiver,
                Type = type,
                PerYear = perYear,
                YearsLeft = years,
                TotalYears = years,
                StartYear = gameYear,
            };
            _treaties.Add(treaty);

            _ctx.Events.Publish(new ReparationTreatySignedEvent
            {
                Payer = payer,
                Receiver = receiver,
                Type = type,
                TotalAmount = perYear * years,
                Years = years,
                GameYear = gameYear,
            });
            _ctx.Log($"[战争赔款] 势力 {payer} 向势力 {receiver} 赔付 " +
                     $"{type} {perYear:F0}/年 × {years} 年（{gameYear} 年）");
        }

        /// <summary>弱者存量最高的战略资源（无存量返回 None）。</summary>
        private static ResourceType BestStrategic(IEconomyService economy, int faction)
        {
            ResourceType best = ResourceType.None;
            float bestAmount = 0f;
            for (int i = 0; i < _strategic.Length; i++)
            {
                float amount = economy.Get(faction, _strategic[i]);
                if (amount > bestAmount)
                {
                    bestAmount = amount;
                    best = _strategic[i];
                }
            }
            return best;
        }

        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }

        public void OnShutdown()
        {
            if (_ctx != null && _ctx.Events != null)
            {
                _ctx.Events.Unsubscribe<CoercedPeaceEvent>(OnCoercedPeace);
                _ctx.Events.Unsubscribe<VassalizedEvent>(OnVassalized);
            }
        }

        public ISaveSegment GetSaveSegment()
        {
            if (_treaties.Count == 0) return null;

            var seg = new ReparationSaveSegment
            {
                PluginId = PluginIdValue,
                SegmentVersion = 1,
                Count = _treaties.Count,
                Payers = new int[_treaties.Count],
                Receivers = new int[_treaties.Count],
                Types = new int[_treaties.Count],
                PerYears = new float[_treaties.Count],
                YearsLeft = new int[_treaties.Count],
                TotalYears = new int[_treaties.Count],
                StartYears = new int[_treaties.Count],
                TotalPaid = new float[_treaties.Count],
            };
            for (int i = 0; i < _treaties.Count; i++)
            {
                seg.Payers[i] = _treaties[i].Payer;
                seg.Receivers[i] = _treaties[i].Receiver;
                seg.Types[i] = (int)_treaties[i].Type;
                seg.PerYears[i] = _treaties[i].PerYear;
                seg.YearsLeft[i] = _treaties[i].YearsLeft;
                seg.TotalYears[i] = _treaties[i].TotalYears;
                seg.StartYears[i] = _treaties[i].StartYear;
                seg.TotalPaid[i] = _treaties[i].TotalPaid;
            }
            return seg;
        }

        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion)
        {
            if (!(segment is ReparationSaveSegment s)) return;
            if (s == null || s.Count <= 0) return;

            _treaties.Clear();
            for (int i = 0; i < s.Count; i++)
            {
                _treaties.Add(new ReparationTreaty
                {
                    Payer = s.Payers[i],
                    Receiver = s.Receivers[i],
                    Type = (ResourceType)s.Types[i],
                    PerYear = s.PerYears[i],
                    YearsLeft = s.YearsLeft[i],
                    TotalYears = s.TotalYears[i],
                    StartYear = s.StartYears[i],
                    TotalPaid = (s.TotalPaid != null && i < s.TotalPaid.Length)
                        ? s.TotalPaid[i] : 0f,
                });
            }
            _ctx.Log($"[战争赔款] 恢复 {_treaties.Count} 条生效赔款条约");
        }

        /// <summary>单条赔款条约（运行时状态）。</summary>
        private sealed class ReparationTreaty
        {
            public int Payer;
            public int Receiver;
            public ResourceType Type;
            public float PerYear;
            public int YearsLeft;
            public int TotalYears;
            public int StartYear;
            public float TotalPaid;
        }
    }
}
