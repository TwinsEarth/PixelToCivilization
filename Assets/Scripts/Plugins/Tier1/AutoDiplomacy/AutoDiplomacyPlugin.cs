using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;

namespace PxC.Plugins.AutoDiplomacy
{
    /// <summary>
    /// 自动外交 / 主动宣战（Tier-1）。年度对每个存活势力评估：
    /// 若攻方火力达到最低门槛、且相对目标存在显著军力优势，
    /// 同时尚未多线作战，则发布 <see cref="WarDeclaredEvent"/>
    /// （由 military 插件订阅后建立战争矩阵、history 插件记录）。
    /// 玩家势力默认跳过（但仍可被 AI 宣战）；<see cref="EnablePlayerAutomation"/> 可开启。
    /// 节奏控制：①防围殴（目标已在战争中不趁火打劫）；②战后冷却（防连续灭国滚雪球）；
    /// ③代差优势（强时代势力可凭技术领先扩张）。仅维护战后冷却年份，不产生存档段。
    /// </summary>
    public sealed class AutoDiplomacyPlugin : ISimPlugin
    {
        public const string PluginIdValue = "pxc.autodiplomacy";

        /// <summary>攻方发起战争所需的最低火力（太弱不发动战争）。</summary>
        public const int MinimumFirepower = 20;

        /// <summary>攻方火力相对守方所需的优势比（attacker / defender）。</summary>
        public const float PowerAdvantageRatio = 1.5f;

        /// <summary>攻方时代领先守方达到该值（时代差）时，可凭代差优势宣战。</summary>
        public const int EraAdvantageGap = 2;

        /// <summary>存在显著代差优势时，所需的更低军力比门槛。</summary>
        public const float EraAdvantageRatio = 1.1f;

        /// <summary>每势力最多同时进行的战争数量（避免多线作战）。</summary>
        public const int MaxConcurrentWars = 1;

        /// <summary>
        /// 战后冷却年数：一场战争结束（灭国/和约/压制和约）后，
        /// 攻方需休整该年数才能再次宣战，防止连续灭国"滚雪球"形成单极统治。
        /// </summary>
        public const int PostWarCooldownYears = 6;

        private static readonly IReadOnlyList<PluginDependency> _deps =
            new List<PluginDependency>(2)
            {
                new PluginDependency("pxc.military", 1),
                new PluginDependency("pxc.automilitary", 1),
            };

        private static readonly IReadOnlyList<CapabilityClaim> _caps =
            new List<CapabilityClaim>(0);

        private IPluginContext _ctx;

        /// <summary>各势力上一场战争结束的游戏年份（0=从未结束过战争）。战后冷却判定用。</summary>
        private readonly int[] _lastWarEndYear = new int[8];

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

        public void OnInitialize()
        {
            // 订阅战争结束事件，记录"上一场战争结束年份"用于战后冷却（防连续滚雪球）。
            _ctx.Events.Subscribe<FactionEliminatedEvent>(OnFactionEliminated);
            _ctx.Events.Subscribe<PeaceTreatyEvent>(OnPeaceTreaty);
            _ctx.Events.Subscribe<CoercedPeaceEvent>(OnCoercedPeace);
        }

        public void OnTick(float simDt) { }

        /// <summary>灭国：胜者进入战后冷却（败者已亡，不记录）。</summary>
        private void OnFactionEliminated(FactionEliminatedEvent e)
        {
            if (e.ByFaction >= 0 && e.ByFaction < _lastWarEndYear.Length)
                _lastWarEndYear[e.ByFaction] = e.GameYear;
        }

        /// <summary>自动和约：双方进入战后冷却。</summary>
        private void OnPeaceTreaty(PeaceTreatyEvent e)
        {
            MarkWarEnd(e.FactionA, e.GameYear);
            MarkWarEnd(e.FactionB, e.GameYear);
        }

        /// <summary>压制性和约：双方进入战后冷却。</summary>
        private void OnCoercedPeace(CoercedPeaceEvent e)
        {
            MarkWarEnd(e.Stronger, e.GameYear);
            MarkWarEnd(e.Weaker, e.GameYear);
        }

        private void MarkWarEnd(int faction, int gameYear)
        {
            if (faction >= 0 && faction < _lastWarEndYear.Length)
                _lastWarEndYear[faction] = gameYear;
        }

        public void OnYear(int gameYear)
        {
            if (!_ctx.Services.TryResolve<IMilitaryService>(out var military)) return;
            var factions = _ctx.Services.Resolve<FactionRegistry>();
            // 科技为可选服务：用于判定"代差优势"（强时代势力可凭技术领先扩张）。
            _ctx.Services.TryResolve<ITechService>(out var tech);

            for (int attacker = 0; attacker < factions.Count; attacker++)
            {
                FactionInfo attackerInfo = factions.Get(attacker);
                if (!attackerInfo.Alive) continue;
                if (attackerInfo.IsPlayer && !EnablePlayerAutomation) continue;
                if (attackerInfo.IsVassal) continue; // 附庸外交受宗主约束，不主动宣战

                // 战后冷却：上一场战争结束未满 PostWarCooldownYears → 休整，不再宣战
                // （防止连续灭国滚雪球形成单极统治）。
                int lastEnd = _lastWarEndYear[attacker];
                if (lastEnd > 0 && gameYear - lastEnd < PostWarCooldownYears)
                    continue;

                // 已多线作战 → 不再宣战
                if (CountCurrentWars(attacker, military, factions) >= MaxConcurrentWars)
                    continue;

                int attackerPower = military.Firepower(attacker);
                if (attackerPower < MinimumFirepower) continue;

                int attackerEra = tech != null ? (int)tech.GetEra(attacker) : 0;

                if (TryChooseDefender(attacker, attackerInfo.IsPlayer, attackerPower,
                                      attackerEra, tech, military, factions,
                                      out int defender))
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

        public void OnShutdown()
        {
            if (_ctx == null || _ctx.Events == null) return;
            _ctx.Events.Unsubscribe<FactionEliminatedEvent>(OnFactionEliminated);
            _ctx.Events.Unsubscribe<PeaceTreatyEvent>(OnPeaceTreaty);
            _ctx.Events.Unsubscribe<CoercedPeaceEvent>(OnCoercedPeace);
        }

        public ISaveSegment GetSaveSegment() => null;
        public void RestoreSaveSegment(ISaveSegment segment, int globalSaveVersion) { }

        /// <summary>
        /// 选择首个满足军力优势条件的目标。守方火力至少按 1 计，
        /// 优势比 = attackerPower / max(1, defenderPower)。
        /// 当攻方时代领先守方 <see cref="EraAdvantageGap"/> 以上（代差），
        /// 可凭更低的 <see cref="EraAdvantageRatio"/> 门槛宣战（技术代差扩张）。
        /// </summary>
        private bool TryChooseDefender(
            int attacker, bool attackerIsPlayer, int attackerPower, int attackerEra,
            ITechService tech, IMilitaryService military, FactionRegistry factions,
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

                // 防围殴：候选目标若已在进行任何一场战争（与任何第三方交战），
                // 不趁火打劫（战争以"单挑"为主，避免多方雪球碾压同一弱者）。
                if (IsAlreadyInWar(other, military, factions)) continue;

                int defenderPower = System.Math.Max(1, military.Firepower(other));
                float ratio = (float)attackerPower / (float)defenderPower;

                // 计算代差：攻方时代领先守方的时代数（仅在科技服务可用时）。
                int eraGap = 0;
                if (tech != null)
                    eraGap = attackerEra - (int)tech.GetEra(other);
                bool hasEraAdvantage = eraGap >= EraAdvantageGap;

                // 常规军力优势，或显著代差优势（更低门槛）。
                float required = hasEraAdvantage ? EraAdvantageRatio : PowerAdvantageRatio;
                if (ratio >= required)
                {
                    defender = other;
                    return true;
                }
            }

            defender = -1;
            return false;
        }

        /// <summary>该势力是否已在与任意第三方交战（排除自身；用于防止"围殴"）。</summary>
        private static bool IsAlreadyInWar(
            int faction, IMilitaryService military, FactionRegistry factions)
        {
            for (int other = 0; other < factions.Count; other++)
            {
                if (other == faction) continue;
                if (military.IsAtWar(faction, other)) return true;
            }
            return false;
        }

        private static int CountCurrentWars(
            int faction, IMilitaryService military, FactionRegistry factions)
        {
            int n = 0;
            for (int other = 0; other < factions.Count; other++)
            {
                if (other == faction) continue;
                // 已灭亡势力不计入战争数（灭国矩阵已清；此为防御性双保险）。
                if (!factions.Get(other).Alive) continue;
                if (military.IsAtWar(faction, other)) n++;
            }
            return n;
        }
    }
}
