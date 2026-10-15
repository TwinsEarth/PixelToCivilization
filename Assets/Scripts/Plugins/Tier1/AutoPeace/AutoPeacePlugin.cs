using System.Collections.Generic;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;

namespace PxC.Plugins.AutoPeace
{
    /// <summary>
    /// 自动和约（白和约，Tier-1）。年度对每对交战势力评估：
    /// 若战争持续达到 <see cref="MinimumWarYears"/>、双方仍都保有作战单位
    /// （即谁也无法快速灭国），且作战单位数量接近（优势比不超过
    /// <see cref="BalanceRatio"/>），则结束战争（无胜负）并发布
    /// <see cref="PeaceTreatyEvent"/>。
    /// 一方单位归零的灭国路径由 WarResolutionPlugin 处理，不在此插件。
    /// 纯决策、无运行时状态，不产生存档段。
    /// </summary>
    public sealed class AutoPeacePlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.autopeace";

        /// <summary>战争至少持续的游戏年数，之后才可能签和约。</summary>
        public const int MinimumWarYears = 5;

        /// <summary>双方作战单位数量接近的判定上限（max/min ≤ 该值）。</summary>
        public const float BalanceRatio = 1.5f;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(3)
            {
                new PluginDependency("pxc.military", 1),
                new PluginDependency("pxc.groundforce", 1),
                new PluginDependency("pxc.naval", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

        public string Id => PluginIdValue;
        public string DisplayName => "自动和约";
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
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            for (int a = 0; a < factions.Count; a++)
            {
                for (int b = a + 1; b < factions.Count; b++)
                {
                    if (!military.IsAtWar(a, b)) continue;
                    if (!factions.Get(a).Alive || !factions.Get(b).Alive) continue;

                    int aUnits = ground.CountByFaction(a) + naval.CountByFaction(a);
                    int bUnits = ground.CountByFaction(b) + naval.CountByFaction(b);

                    // 一方已无单位：交由灭国/征服路径，不签和约。
                    if (aUnits <= 0 || bUnits <= 0) continue;

                    int startYear = military.GetWarStartYear(a, b);
                    if (gameYear - startYear < MinimumWarYears) continue;

                    int hi = System.Math.Max(aUnits, bUnits);
                    int lo = System.Math.Min(aUnits, bUnits);
                    if ((float)hi / (float)lo > BalanceRatio) continue;

                    military.MakePeace(a, b);
                    _ctx.Events.Publish(new PeaceTreatyEvent
                    {
                        FactionA = a,
                        FactionB = b,
                        WarStartYear = startYear,
                        GameYear = gameYear,
                    });
                    _ctx.Log($"[自动和约] 势力 {a} 与势力 {b} 僵持 {gameYear - startYear} " +
                             $"年后签署和约（单位 {aUnits} vs {bUnits}）");
                }
            }
        }

        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }
        public void OnShutdown() { }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }
}
