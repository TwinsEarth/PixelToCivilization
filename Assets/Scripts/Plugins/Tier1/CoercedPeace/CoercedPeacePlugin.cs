using System.Collections.Generic;
using UnityEngine;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;

namespace PxC.Plugins.CoercedPeace
{
    /// <summary>
    /// 压制性和约（Tier-1）。年度对每对交战势力评估：
    /// 若战争持续达到 <see cref="MinimumCoercionWarYears"/>、双方仍都保有作战单位
    /// （未灭国），但实力悬殊（强者/弱者单位数比超过 <see cref="CoercionRatio"/>），
    /// 且弱者还有可割让的非首府聚落，则强者强制弱者割让离强者最近的
    /// <see cref="CedeCount"/> 个聚落（含其附近建筑，人口随之转移）并停战，
    /// 发布 <see cref="CoercedPeaceEvent"/>。
    /// 实力接近的僵持由 AutoPeacePlugin 处理；一方归零的灭国由
    /// WarResolutionPlugin 处理。纯决策、无运行时状态，不产生存档段。
    /// </summary>
    public sealed class CoercedPeacePlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.coercedpeace";

        /// <summary>战争至少持续的游戏年数，之后强者才可能逼迫割地。</summary>
        public const int MinimumCoercionWarYears = 3;

        /// <summary>实力悬殊判定（强者/弱者作战单位数比 &gt; 该值）。</summary>
        public const float CoercionRatio = 3.0f;

        /// <summary>每次割让的聚落数。</summary>
        public const int CedeCount = 1;

        /// <summary>聚落割让时，其附近建筑随之转移的半径（世界单位）。</summary>
        public const float BuildingCedeRadius = 6f;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(6)
            {
                new PluginDependency("pxc.military", 1),
                new PluginDependency("pxc.groundforce", 1),
                new PluginDependency("pxc.naval", 1),
                new PluginDependency("pxc.warresolution", 1),
                new PluginDependency("pxc.conquest", 1),
                new PluginDependency("pxc.autopeace", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

        public string Id => PluginIdValue;
        public string DisplayName => "压制性和约";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;
        public SimulationLayer Layer => SimulationLayer.P1Tactical;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context) => _ctx = context;
        public void OnInitialize() { }
        public void OnTick(float simDt) { }

        public void OnYear(int gameYear)
        {
            if (!_ctx.Services.TryResolve<IMilitaryService>(out var military)) return;
            if (!_ctx.Services.TryResolve<GroundForceService>(out var ground)) return;
            if (!_ctx.Services.TryResolve<NavalService>(out var naval)) return;
            if (!_ctx.Services.TryResolve<SettlementRegistry>(out var settlements)) return;
            if (!_ctx.Services.TryResolve<BuildingRegistry>(out var buildings)) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            for (int a = 0; a < factions.Count; a++)
            {
                for (int b = a + 1; b < factions.Count; b++)
                {
                    if (!military.IsAtWar(a, b)) continue;
                    if (!factions.Get(a).Alive || !factions.Get(b).Alive) continue;

                    int aUnits = ground.CountByFaction(a) + naval.CountByFaction(a);
                    int bUnits = ground.CountByFaction(b) + naval.CountByFaction(b);

                    // 一方已无单位：交由灭国/征服路径。
                    if (aUnits <= 0 || bUnits <= 0) continue;

                    int startYear = military.GetWarStartYear(a, b);
                    if (gameYear - startYear < MinimumCoercionWarYears) continue;

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

                    if ((float)strongerUnits / (float)weakerUnits <= CoercionRatio)
                        continue;

                    int ceded = CedeNearestSettlements(
                        settlements, buildings, stronger, weaker);

                    if (ceded <= 0) continue; // 弱者无可割让聚落，维持战争

                    military.MakePeace(a, b);
                    _ctx.Events.Publish(new CoercedPeaceEvent
                    {
                        Stronger = stronger,
                        Weaker = weaker,
                        SettlementsCeded = ceded,
                        WarStartYear = startYear,
                        GameYear = gameYear,
                    });
                    _ctx.Log($"[压制性和约] 势力 {stronger} 逼迫势力 {weaker} " +
                             $"割让 {ceded} 个聚落后停战（单位 {strongerUnits} vs {weakerUnits}）");
                }
            }
        }

        /// <summary>
        /// 将弱者离强者首府最近的至多 <see cref="CedeCount"/> 个非首府聚落
        /// （及其附近建筑、绑定人口）转移给强者。返回实际转移的聚落数。
        /// </summary>
        private int CedeNearestSettlements(
            SettlementRegistry settlements, BuildingRegistry buildings,
            int stronger, int weaker)
        {
            int capitalSlot = settlements.FindCapital(stronger);
            Vector3 strongCenter;
            if (capitalSlot >= 0)
                strongCenter = settlements.GetSafe(capitalSlot).Position;
            else
                strongCenter = Vector3.zero;

            int ceded = 0;
            for (int n = 0; n < CedeCount; n++)
            {
                int bestSlot = -1;
                float bestD = float.MaxValue;

                for (int i = 0; i < settlements.Capacity; i++)
                {
                    ref SettlementRecord r = ref settlements.GetSlotRef(i);
                    if (!r.Alive || r.Faction != weaker) continue;
                    if (r.Kind == (int)SettlementKind.Capital) continue; // 不割首府

                    float dx = r.Position.x - strongCenter.x;
                    float dz = r.Position.z - strongCenter.z;
                    float d = dx * dx + dz * dz;
                    if (d < bestD)
                    {
                        bestD = d;
                        bestSlot = i;
                    }
                }

                if (bestSlot < 0) break;

                var cededPos = settlements.GetSafe(bestSlot).Position;
                settlements.GetSlotRef(bestSlot).Faction = stronger;
                TransferNearbyBuildings(buildings, weaker, stronger, cededPos);
                ceded++;
            }

            return ceded;
        }

        /// <summary>转移指定聚落半径内、属于弱者的建筑给强者。</summary>
        private void TransferNearbyBuildings(
            BuildingRegistry buildings, int weaker, int stronger, Vector3 center)
        {
            float r2 = BuildingCedeRadius * BuildingCedeRadius;
            for (int i = 0; i < buildings.Capacity; i++)
            {
                ref BuildingRecord r = ref buildings.GetSlotRef(i);
                if (!r.Alive || r.Faction != weaker) continue;
                float dx = r.Position.x - center.x;
                float dz = r.Position.z - center.z;
                if (dx * dx + dz * dz <= r2)
                    r.Faction = stronger;
            }
        }

        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }
        public void OnShutdown() { }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }
}
