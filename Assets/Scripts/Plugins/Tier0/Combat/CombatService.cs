using System;
using UnityEngine;
using PxC.Core.Events;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;
using PxC.Plugins.GroundForce;
using PxC.Plugins.Naval;

namespace PxC.Plugins.Combat
{
    /// <summary>
    /// ICombatService 实现：统一伤害公式、护甲减免、伤害类型克制、经验升级与击杀事件。
    /// 所有攻击都经 ResolveAttack → DamageUnit，所有击杀都经本服务，
    /// 发布 ProjectileFired/ProjectileHit/EntityDied，保证战斗口径唯一。
    /// </summary>
    public sealed class CombatService : ICombatService
    {
        /// <summary>护甲减伤曲线常数（mitig = def/(def+K)）。</summary>
        private const float ArmorK = 100f;

        /// <summary>同域规则：陆地打陆地、海军打海军（不跨域）。</summary>
        private const bool EnforceSameDomain = true;

        private readonly UnitRegistry _units;
        private readonly GroundForceService _ground;
        private readonly NavalService _naval;
        private readonly IEventBus _events;
        private readonly Action<string> _log;

        public CombatService(
            UnitRegistry units,
            GroundForceService ground,
            NavalService naval,
            IEventBus events,
            Action<string> log)
        {
            _units = units;
            _ground = ground;
            _naval = naval;
            _events = events;
            _log = log;
        }

        public void ResolveAttack(EntityId source, EntityId target)
        {
            if (!source.IsValid || !target.IsValid) return;
            ref var s = ref _units.GetRef(source);
            ref var t = ref _units.GetRef(target);
            if (!s.Alive || !t.Alive) return;
            if (EnforceSameDomain && s.Domain != t.Domain) return;

            float attack = GetAttack(source.Index, s.Domain);
            if (attack <= 0f) return;
            var type = GetDamageType(s.ModelId, s.Domain);

            _events.PublishDeferred(new ProjectileFiredEvent
            {
                ProjectileType = (int)type,
                Source = source,
                Target = target,
            });

            DamageUnit(target, attack, type, source);
            AwardExperience(source, 5);
        }

        public void DamageUnit(EntityId target, float amount, DamageType type, EntityId source)
        {
            ref var t = ref _units.GetRef(target);
            if (!t.Alive) return;

            float defense = GetDefense(target.Index, t.Domain);
            float mitig = defense / (defense + ArmorK);
            if (type == DamageType.ArmorPiercing) mitig *= 0.5f; // 穿甲弹减半护甲
            float dmg = Mathf.Max(amount * (1f - mitig), 1f);

            float maxHpBeforeKill = t.MaxHp;
            t.Hp -= dmg;

            _events.PublishDeferred(new ProjectileHitEvent
            {
                ProjectileType = (int)type,
                Target = target,
                Damage = dmg,
            });

            if (t.Hp <= 0f)
            {
                _units.Kill(target);
                _events.PublishDeferred(new EntityDiedEvent
                {
                    Id = target,
                    Reason = (int)type,
                });

                if (source.IsValid)
                {
                    int killXp = Mathf.Max(10, (int)(maxHpBeforeKill * 0.5f));
                    AwardExperience(source, killXp);
                }
            }
        }

        public void AwardExperience(EntityId unit, int experience)
        {
            if (!unit.IsValid || experience <= 0) return;
            ref var u = ref _units.GetRef(unit);
            if (!u.Alive) return;
            switch (u.Domain)
            {
                case EntityDomain.Land:
                    _ground?.AddExperience(unit.Index, experience);
                    break;
                case EntityDomain.Naval:
                    _naval?.AddExperience(unit.Index, experience);
                    break;
            }
        }

        // ------------------------------------------------------------
        // 口径查询（按 domain 分发到对应 service / profile）
        // ------------------------------------------------------------
        private float GetAttack(int slot, EntityDomain domain)
        {
            switch (domain)
            {
                case EntityDomain.Land:
                    return _ground != null ? _ground.CurrentAttack(slot) : 0f;
                case EntityDomain.Naval:
                    return _naval != null ? _naval.CurrentAttack(slot) : 0f;
                default:
                    return 0f;
            }
        }

        private float GetDefense(int slot, EntityDomain domain)
        {
            switch (domain)
            {
                case EntityDomain.Land:
                    return _ground != null ? _ground.CurrentDefense(slot) : 0f;
                case EntityDomain.Naval:
                    return _naval != null ? _naval.CurrentDefense(slot) : 0f;
                default:
                    return 0f;
            }
        }

        private static DamageType GetDamageType(int modelId, EntityDomain domain)
        {
            if (domain == EntityDomain.Land)
                return GroundForceProfile.Get((GroundForceModel)modelId).Damage;
            if (domain == EntityDomain.Naval)
                return NavalProfile.Get((NavalModel)modelId).Damage;
            return DamageType.Blunt;
        }
    }
}
