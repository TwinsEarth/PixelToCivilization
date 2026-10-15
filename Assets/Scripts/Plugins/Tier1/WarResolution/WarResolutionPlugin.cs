using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;

namespace PxC.Plugins.WarResolution
{
    /// <summary>
    /// 战争结果 / 灭国闭环（Tier-1）。年度对每一对正在交战的势力，
    /// 统计双方剩余作战单位（地面 + 船艇）：
    /// 一方作战单位归零、而对方仍有作战单位时，败者被灭国
    /// （<see cref="FactionInfo.Alive"/> = false 并发布 <see cref="FactionEliminatedEvent"/>）。
    /// 双方均有单位（战斗继续）或双方均为 0（平局，互不灭国）时不处理。
    /// 纯决策、无运行时状态，不产生存档段。
    /// </summary>
    public sealed class WarResolutionPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.warresolution";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(5)
            {
                new PluginDependency("pxc.military", 1),
                new PluginDependency("pxc.groundforce", 1),
                new PluginDependency("pxc.naval", 1),
                new PluginDependency("pxc.automilitary", 1),
                new PluginDependency("pxc.autodiplomacy", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

        public string Id => PluginIdValue;
        public string DisplayName => "战争结果";
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
            _ctx.Services.TryResolve<NavalService>(out var naval);
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            // 遍历交战对（a < b，每对只处理一次）。
            for (int a = 0; a < factions.Count; a++)
            {
                FactionInfo aInfo = factions.Get(a);
                if (!aInfo.Alive) continue;

                for (int b = a + 1; b < factions.Count; b++)
                {
                    FactionInfo bInfo = factions.Get(b);
                    if (!bInfo.Alive) continue;
                    if (!military.IsAtWar(a, b)) continue;

                    int aUnits = CountUnits(a, ground, naval);
                    int bUnits = CountUnits(b, ground, naval);

                    if (aUnits == 0 && bUnits > 0)
                        Eliminate(a, b, gameYear);
                    else if (bUnits == 0 && aUnits > 0)
                        Eliminate(b, a, gameYear);
                    // 双方均为 0 → 平局，不处理；双方均 > 0 → 战斗继续。
                }
            }
        }

        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }
        public void OnShutdown() { }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }

        /// <summary>剩余作战单位总数（地面单位 + 船艇）。</summary>
        private static int CountUnits(
            int faction, GroundForceService ground, NavalService naval)
        {
            int n = ground.CountByFaction(faction);
            if (naval != null) n += naval.CountByFaction(faction);
            return n;
        }

        private void Eliminate(int loser, int winner, int gameYear)
        {
            FactionInfo loserInfo = _ctx.Services.Resolve<FactionRegistry>().Get(loser);
            if (!loserInfo.Alive) return;
            loserInfo.Alive = false;

            _ctx.Events.Publish(new FactionEliminatedEvent
            {
                Faction = loser,
                ByFaction = winner,
                GameYear = gameYear,
            });
            _ctx.Log($"[战争结果] 势力 {loser} 作战单位被全部消灭，" +
                     $"被势力 {winner} 灭国（{gameYear} 年）");
        }
    }
}
