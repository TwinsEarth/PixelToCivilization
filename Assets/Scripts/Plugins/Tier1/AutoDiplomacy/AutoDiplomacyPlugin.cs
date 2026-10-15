using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.AutoDiplomacy
{
    /// <summary>
    /// 自动外交 / 主动宣战（Tier-1）。年度对每个存活势力评估：
    /// 若攻方火力达到最低门槛、且相对目标存在显著军力优势，
    /// 同时尚未多线作战，则发布 <see cref="WarDeclaredEvent"/>
    /// （由 military 插件订阅后建立战争矩阵、history 插件记录）。
    /// 玩家势力默认跳过（但仍可被 AI 宣战）；<see cref="EnablePlayerAutomation"/> 可开启。
    /// 纯决策、无运行时状态，不产生存档段。
    /// </summary>
    public sealed class AutoDiplomacyPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.autodiplomacy";

        /// <summary>攻方发起战争所需的最低火力（太弱不发动战争）。</summary>
        public const int MinimumFirepower = 20;

        /// <summary>攻方火力相对守方所需的优势比（attacker / defender）。</summary>
        public const float PowerAdvantageRatio = 1.5f;

        /// <summary>每势力最多同时进行的战争数量（避免多线作战）。</summary>
        public const int MaxConcurrentWars = 1;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(2)
            {
                new PluginDependency("pxc.military", 1),
                new PluginDependency("pxc.automilitary", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

        /// <summary>是否让玩家势力也主动宣战（默认 false）。</summary>
        public bool EnablePlayerAutomation;

        /// <summary>
        /// 是否允许 AI 主动对人类玩家（faction 0）宣战（默认 false：保护主角，
        /// 玩家不会因军力暂时落后就被 AI 无故灭国）。玩家主动宣战后，AI 仍可正常反击。
        /// 开启后 AI 会把弱军力的玩家视为可宣战目标（更高难度）。
        /// </summary>
        public bool AiMayDeclareOnHumanPlayer;

        public string Id => PluginIdValue;
        public string DisplayName => "自动外交";
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
            var factions = _ctx.Services.Resolve<FactionRegistry>();

            for (int attacker = 0; attacker < factions.Count; attacker++)
            {
                FactionInfo attackerInfo = factions.Get(attacker);
                if (!attackerInfo.Alive) continue;
                if (attackerInfo.IsPlayer && !EnablePlayerAutomation) continue;

                // 已多线作战 → 不再宣战
                if (CountCurrentWars(attacker, military, factions) >= MaxConcurrentWars)
                    continue;

                int attackerPower = military.Firepower(attacker);
                if (attackerPower < MinimumFirepower) continue;

                if (TryChooseDefender(attacker, attackerInfo.IsPlayer, attackerPower,
                                      military, factions, out int defender))
                {
                    _ctx.Events.Publish(new WarDeclaredEvent
                    {
                        AttackerFaction = attacker,
                        DefenderFaction = defender,
                    });
                    _ctx.Log($"[自动外交] 势力 {attacker}（火力 {attackerPower}）" +
                             $"对势力 {defender}（火力 {military.Firepower(defender)}）宣战");
                }
            }
        }

        public void OnEra(int newEra, int oldEra) { }
        public void OnDynasty(int dynastyIndex, int gameYear) { }
        public void OnShutdown() { }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }

        /// <summary>
        /// 选择首个满足军力优势条件的目标。守方火力至少按 1 计，
        /// 优势比 = attackerPower / max(1, defenderPower)。
        /// </summary>
        private bool TryChooseDefender(
            int attacker, bool attackerIsPlayer, int attackerPower,
            IMilitaryService military, FactionRegistry factions,
            out int defender)
        {
            for (int other = 0; other < factions.Count; other++)
            {
                if (other == attacker) continue;
                FactionInfo otherInfo = factions.Get(other);
                if (!otherInfo.Alive) continue;
                if (military.IsAtWar(attacker, other)) continue;

                // 保护人类玩家：AI 攻方默认不主动对人类玩家宣战（除非显式开启）。
                if (otherInfo.IsPlayer && !attackerIsPlayer && !AiMayDeclareOnHumanPlayer)
                    continue;

                int defenderPower = System.Math.Max(1, military.Firepower(other));
                float ratio = (float)attackerPower / (float)defenderPower;
                if (ratio >= PowerAdvantageRatio)
                {
                    defender = other;
                    return true;
                }
            }

            defender = -1;
            return false;
        }

        private static int CountCurrentWars(
            int faction, IMilitaryService military, FactionRegistry factions)
        {
            int n = 0;
            for (int other = 0; other < factions.Count; other++)
            {
                if (other == faction) continue;
                if (military.IsAtWar(faction, other)) n++;
            }
            return n;
        }
    }
}
