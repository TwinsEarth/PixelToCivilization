using System;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Policy
{
    /// <summary>
    /// 政策系数配置（每国策一组，1=无影响）。
    /// </summary>
    public struct PolicyProfile
    {
        public float Food;
        public float Gold;
        public float Birth;
        public float Death;
        public float Culture;
        public float Research;
    }

    /// <summary>
    /// 政策服务：国策切换（冷却）与治理系数查询。
    /// </summary>
    public sealed class PolicyService : IPolicyService
    {
        /// <summary>切换国策的冷却年数。</summary>
        public const int CooldownYears = 20;

        private static readonly PolicyProfile[] _profiles =
        {
            // Rest 休养生息
            new PolicyProfile { Food = 1.05f, Gold = 1.00f, Birth = 1.30f, Death = 0.90f, Culture = 1.00f, Research = 1.00f },
            // Agriculture 重农抑商
            new PolicyProfile { Food = 1.20f, Gold = 0.90f, Birth = 1.05f, Death = 1.00f, Culture = 0.95f, Research = 0.95f },
            // Commerce 通商惠工
            new PolicyProfile { Food = 0.95f, Gold = 1.20f, Birth = 1.00f, Death = 1.00f, Culture = 1.05f, Research = 1.00f },
            // Strict 严刑峻法
            new PolicyProfile { Food = 1.00f, Gold = 1.00f, Birth = 0.95f, Death = 1.10f, Culture = 0.90f, Research = 1.00f },
            // Open 对外开放
            new PolicyProfile { Food = 1.00f, Gold = 1.10f, Birth = 1.00f, Death = 0.95f, Culture = 1.20f, Research = 1.10f },
            // Closed 闭关锁国
            new PolicyProfile { Food = 1.00f, Gold = 0.95f, Birth = 1.00f, Death = 1.00f, Culture = 0.80f, Research = 0.85f },
            // ImperialExam 科举取士
            new PolicyProfile { Food = 1.00f, Gold = 1.00f, Birth = 0.95f, Death = 1.00f, Culture = 1.15f, Research = 1.10f },
            // Martial 整军经武
            new PolicyProfile { Food = 0.95f, Gold = 1.00f, Birth = 0.95f, Death = 1.05f, Culture = 0.95f, Research = 1.00f },
        };

        public const int ProfileCount = 8;

        private readonly PolicyState _state;
        private readonly IServiceLocator _services;
        private readonly Action<string> _log;

        public PolicyService(PolicyState state, IServiceLocator services, Action<string> log)
        {
            _state = state;
            _services = services;
            _log = log;
        }

        public PolicyType Current(int faction) =>
            (PolicyType)_state.Current(faction);

        public int LastSwitchYear(int faction) =>
            _state.LastSwitch(faction);

        public bool CanSwitch(int faction, PolicyType policy)
        {
            if ((int)policy < 0 || (int)policy >= ProfileCount) return false;
            if ((int)policy == _state.Current(faction)) return false;

            int now = ResolveYear();
            int last = _state.LastSwitch(faction);
            if (last != PolicyState.NeverSwitched && now - last < CooldownYears)
                return false;
            return true;
        }

        public bool TrySwitch(int faction, PolicyType policy)
        {
            if (!CanSwitch(faction, policy)) return false;
            int old = _state.Current(faction);
            int year = ResolveYear();
            _state.Set(faction, (int)policy, year);

            _services.Resolve<IEventBus>()?.Publish(new PolicyChangedEvent
            {
                Faction = faction,
                NewPolicy = (int)policy,
                OldPolicy = old,
                GameYear = year,
            });
            _log?.Invoke($"[国策] 势力 {faction} 于 {year} 年改为 {policy}");
            return true;
        }

        public float FoodModifier(int faction) => _profiles[_state.Current(faction)].Food;
        public float GoldModifier(int faction) => _profiles[_state.Current(faction)].Gold;
        public float BirthModifier(int faction) => _profiles[_state.Current(faction)].Birth;
        public float DeathModifier(int faction) => _profiles[_state.Current(faction)].Death;
        public float CultureModifier(int faction) => _profiles[_state.Current(faction)].Culture;
        public float ResearchModifier(int faction) => _profiles[_state.Current(faction)].Research;

        private int ResolveYear()
        {
            if (_services.TryResolve<IClock>(out var clock))
                return clock.GameYear;
            return 0;
        }
    }
}
