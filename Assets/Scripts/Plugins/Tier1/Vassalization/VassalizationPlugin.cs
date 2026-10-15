using System.Collections.Generic;
using UnityEngine;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;

namespace PxC.Plugins.Vassalization
{
    /// <summary>
    /// 战争附庸与纳贡（Tier-1）。补齐"灭国 / 割地 / 僵持和约"之外的战争结局梯度：
    /// 当交战达到 <see cref="MinimumVassalWarYears"/>、双方都仍保有单位（未灭国），
    /// 但实力极端悬殊（强者/弱者单位数比 &gt; <see cref="VassalizationRatio"/>），
    /// 且弱者已无可割让的非首府聚落（割地由 CoercedPeacePlugin 处理），则弱者向强者
    /// 称臣，成为附庸（政治臣服，保留社稷），发布 <see cref="VassalizedEvent"/>。
    ///
    /// 附庸义务：
    /// 1. 每年向宗主纳贡金币（Capital）；
    /// 2. 外交受约束（AutoDiplomacy 跳过附庸，附庸不主动宣战）；
    /// 3. 宗主保护：第三方宣附庸战时，宗主自动参战（发布 WarDeclaredEvent）。
    ///
    /// 宗主被灭时，其附庸自动恢复独立（订阅 FactionEliminatedEvent）。
    /// 纯决策、无运行时状态，不产生存档段（附庸关系随 FactionSnapshot 顶层保存）。
    /// </summary>
    public sealed class VassalizationPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.vassalization";

        /// <summary>战争至少持续的游戏年数，之后强者才可能逼迫附庸。</summary>
        public const int MinimumVassalWarYears = 3;

        /// <summary>极端碾压判定（强者/弱者作战单位数比 &gt; 该值），高于割地的 3.0。</summary>
        public const float VassalizationRatio = 3.5f;

        /// <summary>每年最低纳贡金币（即使存量很低也要尽力缴纳）。</summary>
        public const float MinimumTribute = 10f;

        /// <summary>纳贡占附庸金币存量的比例。</summary>
        public const float TributeRate = 0.10f;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(8)
            {
                new PluginDependency("pxc.military", 1),
                new PluginDependency("pxc.economy", 1),
                new PluginDependency("pxc.groundforce", 1),
                new PluginDependency("pxc.naval", 1),
                new PluginDependency("pxc.warresolution", 1),
                new PluginDependency("pxc.conquest", 1),
                new PluginDependency("pxc.autopeace", 1),
                new PluginDependency("pxc.coercedpeace", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

        public string Id => PluginIdValue;
        public string DisplayName => "战争附庸与纳贡";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;
        public SimulationLayer Layer => SimulationLayer.P1Tactical;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context) => _ctx = context;

        public void OnInitialize()
        {
            _ctx.Events.Subscribe<WarDeclaredEvent>(OnWarDeclared);
            _ctx.Events.Subscribe<FactionEliminatedEvent>(OnFactionEliminated);
        }

        public void OnTick(float simDt) { }

        public void OnYear(int gameYear)
        {
            if (!_ctx.Services.TryResolve<IMilitaryService>(out var military)) return;
            if (!_ctx.Services.TryResolve<IEconomyService>(out var economy)) return;
            if (!_ctx.Services.TryResolve<GroundForceService>(out var ground)) return;
            if (!_ctx.Services.TryResolve<NavalService>(out var naval)) return;
            if (!_ctx.Services.TryResolve<SettlementRegistry>(out var settlements)) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            EvaluateVassalization(gameYear, military, ground, naval, settlements, factions);
            CollectTribute(economy, factions);
        }

        // ============================================================
        // 附庸化评估
        // ============================================================
        private void EvaluateVassalization(
            int gameYear, IMilitaryService military,
            GroundForceService ground, NavalService naval,
            SettlementRegistry settlements, FactionRegistry factions)
        {
            for (int a = 0; a < factions.Count; a++)
            {
                for (int b = a + 1; b < factions.Count; b++)
                {
                    if (!military.IsAtWar(a, b)) continue;
                    if (!factions.Get(a).Alive || !factions.Get(b).Alive) continue;

                    int aUnits = ground.CountByFaction(a) + naval.CountByFaction(a);
                    int bUnits = ground.CountByFaction(b) + naval.CountByFaction(b);

                    // 一方已无单位：交由灭国 / 征服路径。
                    if (aUnits <= 0 || bUnits <= 0) continue;

                    int startYear = military.GetWarStartYear(a, b);
                    if (gameYear - startYear < MinimumVassalWarYears) continue;

                    int stronger, weaker, strongerUnits, weakerUnits;
                    if (aUnits >= bUnits)
                    {
                        stronger = a; weaker = b;
                        strongerUnits = aUnits; weakerUnits = bUnits;
                    }
                    else
                    {
                        stronger = b; weaker = a;
                        strongerUnits = bUnits; weakerUnits = aUnits;
                    }

                    if ((float)strongerUnits / (float)weakerUnits <= VassalizationRatio)
                        continue;

                    var weakerInfo = factions.Get(weaker);
                    if (weakerInfo.IsVassal) continue; // 已是附庸，不重复

                    // 弱者仍有可割让非首府聚落：交由 CoercedPeace 割地，保持独立。
                    if (HasCedeableSettlement(settlements, weaker)) continue;

                    // 极端碾压、无可割让、只剩社稷 → 附庸化。
                    military.MakePeace(stronger, weaker);
                    weakerInfo.Suzerain = stronger;
                    weakerInfo.BecameVassalYear = gameYear;

                    _ctx.Events.Publish(new VassalizedEvent
                    {
                        Suzerain = stronger,
                        Vassal = weaker,
                        WarStartYear = startYear,
                        GameYear = gameYear,
                    });
                    _ctx.Log($"[附庸化] 势力 {weaker} 向势力 {stronger} 称臣（单位 " +
                             $"{strongerUnits} vs {weakerUnits}，无可割让，年 {gameYear}）");
                }
            }
        }

        /// <summary>弱者是否还有可割让的非首府聚落（有则由割地路径处理）。</summary>
        private static bool HasCedeableSettlement(SettlementRegistry settlements, int weaker)
        {
            for (int i = 0; i < settlements.Capacity; i++)
            {
                ref SettlementRecord r = ref settlements.GetSlotRef(i);
                if (!r.Alive || r.Faction != weaker) continue;
                if (r.Kind == (int)SettlementKind.Capital) continue;
                return true;
            }
            return false;
        }

        // ============================================================
        // 纳贡：附庸每年向宗主缴纳金币（宗主不存续则附庸独立）
        // ============================================================
        private void CollectTribute(IEconomyService economy, FactionRegistry factions)
        {
            for (int v = 0; v < factions.Count; v++)
            {
                var vInfo = factions.Get(v);
                if (vInfo == null || !vInfo.Alive || vInfo.Suzerain < 0) continue;

                int s = vInfo.Suzerain;
                var sInfo = factions.Get(s);
                if (sInfo == null || !sInfo.Alive)
                {
                    // 宗主已亡：附庸自动恢复独立。
                    vInfo.Suzerain = -1;
                    continue;
                }

                float available = economy.Get(v, ResourceType.Capital);
                float wanted = Mathf.Max(MinimumTribute, available * TributeRate);
                float paid = Mathf.Min(wanted, available);
                if (paid <= 0f) continue;

                if (economy.TrySpend(v, ResourceType.Capital, paid))
                {
                    economy.Add(s, ResourceType.Capital, paid);
                    _ctx.Log($"[附庸纳贡] 势力 {v} 向宗主 {s} 缴纳金币 {paid:F0}");
                }
            }
        }

        // ============================================================
        // 宗主保护：第三方宣附庸的战 → 宗主自动对宣战国策
        // ============================================================
        private void OnWarDeclared(WarDeclaredEvent e)
        {
            if (!_ctx.Services.TryResolve<IMilitaryService>(out var military)) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            var defender = factions.Get(e.DefenderFaction);
            if (defender == null || defender.Suzerain < 0) return;

            int suzerain = defender.Suzerain;
            if (suzerain == e.AttackerFaction) return; // 宗主自己打的，不触发保护

            var sInfo = factions.Get(suzerain);
            if (sInfo == null || !sInfo.Alive) return;

            if (military.IsAtWar(suzerain, e.AttackerFaction)) return; // 已交战

            _ctx.Events.Publish(new WarDeclaredEvent
            {
                AttackerFaction = suzerain,
                DefenderFaction = e.AttackerFaction,
            });
            _ctx.Log($"[宗主保护] 宗主 {suzerain} 为附庸 {e.DefenderFaction} " +
                     $"对势力 {e.AttackerFaction} 参战");
        }

        // ============================================================
        // 宗主被灭：其附庸自动恢复独立
        // ============================================================
        private void OnFactionEliminated(FactionEliminatedEvent e)
        {
            var factions = _ctx.Services.Resolve<FactionRegistry>();
            for (int i = 0; i < factions.Count; i++)
            {
                var info = factions.Get(i);
                if (info != null && info.Suzerain == e.Faction)
                {
                    info.Suzerain = -1;
                    _ctx.Log($"[附庸解放] 宗主 {e.Faction} 被灭，附庸 {i} 恢复独立");
                }
            }
        }

        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }

        public void OnShutdown()
        {
            if (_ctx != null && _ctx.Events != null)
            {
                _ctx.Events.Unsubscribe<WarDeclaredEvent>(OnWarDeclared);
                _ctx.Events.Unsubscribe<FactionEliminatedEvent>(OnFactionEliminated);
            }
        }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }
}
