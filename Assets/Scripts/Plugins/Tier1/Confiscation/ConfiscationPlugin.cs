using System.Collections.Generic;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Confiscation
{
    /// <summary>
    /// 灭国没收（Tier-1，v10.7.7）。订阅 <see cref="FactionEliminatedEvent"/>：
    /// 败者被灭国后，其全部国库资源（15 种）被胜者没收，而不是随败者消失成死账。
    /// 领土/人口/建筑转移由 ConquestPlugin 处理；本插件只处理资源（二者互补）。
    /// 即时一次性结算，无跨年度状态，不产生存档段。
    /// 发布 <see cref="ConfiscationEvent"/>（四大类合计：金币/资金/粮食/战略资源）。
    /// </summary>
    public sealed class ConfiscationPlugin : IPlugin
    {
        public const string PluginIdValue = "pxc.confiscation";

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(2)
            {
                new PluginDependency("pxc.warresolution", 1),
                new PluginDependency("pxc.economy", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

        public string Id => PluginIdValue;
        public string DisplayName => "灭国没收";
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
            if (!_ctx.Services.TryResolve<IEconomyService>(out var economy)) return;

            // 注意：WarResolutionPlugin 在发布本事件前已将败者 Alive=false，
            // 故这里不能以 Alive 判断（否则永远跳过），以事件本身为灭国权威信号。

            float gold = 0f, capital = 0f, food = 0f, strategic = 0f, total = 0f;

            // 遍历全部资源类型（None=-1 跳过）：余额 > 0 才转移（胜者 +amount，败者 -amount 清零），
            // 绝不过账负值，也不产生"复制"（只加不减）或死账（只减不加）。
            for (int type = 0; type < ResourceTypes.Count; type++)
            {
                float amount = economy.Get(evt.Faction, (ResourceType)type);
                if (amount <= 0f) continue;

                economy.Add(evt.ByFaction, (ResourceType)type, amount);
                economy.Add(evt.Faction, (ResourceType)type, -amount); // 败者国库清零
                total += amount;
                _ctx.Log($"[灭国没收] 势力 {evt.ByFaction} 没收势力 {evt.Faction} 的 " +
                         $"{ResourceTypes.DisplayNames[type]} {amount:F0}");

                switch ((ResourceType)type)
                {
                    case ResourceType.Gold: gold += amount; break;
                    case ResourceType.Capital: capital += amount; break;
                    case ResourceType.Food: food += amount; break;
                    case ResourceType.Bronze:
                    case ResourceType.Iron:
                    case ResourceType.Coal:
                    case ResourceType.Oil:
                    case ResourceType.Steel:
                        strategic += amount; break;
                }
            }

            // 无任何资源可没收：不发事件（避免零值噪音）。
            if (total <= 0f) return;

            _ctx.Events.Publish(new ConfiscationEvent
            {
                Loser = evt.Faction,
                Winner = evt.ByFaction,
                GameYear = evt.GameYear,
                Gold = gold,
                Capital = capital,
                Food = food,
                Strategic = strategic,
            });
            _ctx.Log($"[灭国没收] 势力 {evt.ByFaction} 没收势力 {evt.Faction} 国库" +
                     $"(金币 {gold:F0} / 资金 {capital:F0} / 粮食 {food:F0} / " +
                     $"战略资源 {strategic:F0})");
        }

        public void OnShutdown()
        {
            _ctx.Events.Unsubscribe<FactionEliminatedEvent>(OnFactionEliminated);
        }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }
    }
}
