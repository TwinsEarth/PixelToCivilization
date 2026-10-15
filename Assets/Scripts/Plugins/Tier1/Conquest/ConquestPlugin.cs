using System.Collections.Generic;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Entities;

namespace PxC.Plugins.Conquest
{
    /// <summary>
    /// 灭国领土占领插件（Tier-1）。订阅 <see cref="FactionEliminatedEvent"/>：
    /// 败者被灭国后，其全部聚落（SettlementRegistry）与建筑（BuildingRegistry）
    /// 归属转移给胜者。人口绑定在聚落 slot 上，聚落归属改变后人口随之归属胜者。
    /// 转移后发布 <see cref="TerritoryConqueredEvent"/>。无运行时状态、无存档段。
    /// </summary>
    public sealed class ConquestPlugin : IPlugin
    {
        public const string PluginIdValue = "pxc.conquest";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(1)
            {
                new PluginDependency("pxc.warresolution", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

        public string Id => PluginIdValue;
        public string DisplayName => "领土占领";
        public int Version => 1;
        public int MinHostVersion => 10;
        public Tier Tier => Tier.System1;

        public IReadOnlyList<PluginDependency> Dependencies => _deps;
        public IReadOnlyList<CapabilityClaim> RequiredCapabilities => _caps;

        public void OnRegister(IPluginContext context) => _ctx = context;

        public void OnInitialize()
        {
            _ctx.Events.Subscribe<FactionEliminatedEvent>(OnFactionEliminated);
        }

        private void OnFactionEliminated(FactionEliminatedEvent evt)
        {
            var settlements = _ctx.Services.Resolve<SettlementRegistry>();
            var buildings = _ctx.Services.Resolve<BuildingRegistry>();

            int movedSettlements = 0;
            int movedBuildings = 0;

            // 聚落归属转移（人口绑定聚落 slot，随之归属胜者）。
            for (int i = 0; i < settlements.Capacity; i++)
            {
                ref SettlementRecord r = ref settlements.GetSlotRef(i);
                if (r.Alive && r.Faction == evt.Faction)
                {
                    r.Faction = evt.ByFaction;
                    movedSettlements++;
                }
            }

            // 建筑归属转移。
            for (int i = 0; i < buildings.Capacity; i++)
            {
                ref BuildingRecord r = ref buildings.GetSlotRef(i);
                if (r.Alive && r.Faction == evt.Faction)
                {
                    r.Faction = evt.ByFaction;
                    movedBuildings++;
                }
            }

            _ctx.Events.Publish(new TerritoryConqueredEvent
            {
                Loser = evt.Faction,
                Winner = evt.ByFaction,
                Settlements = movedSettlements,
                Buildings = movedBuildings,
                GameYear = evt.GameYear,
            });

            _ctx.Log($"[领土占领] 势力 {evt.ByFaction} 占领势力 {evt.Faction} 的" +
                     $"{movedSettlements} 个聚落、{movedBuildings} 栋建筑");
        }

        public void OnShutdown()
        {
            _ctx.Events.Unsubscribe<FactionEliminatedEvent>(OnFactionEliminated);
        }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }
}
