using PxC.Core.World;

namespace PxC.Gameplay.Services
{
    /// <summary>伤害类型（对应不同兵种与护甲克制）。</summary>
    public enum DamageType
    {
        /// <summary>箭矢/穿刺（克轻甲，被重甲削减）。</summary>
        Piercing = 0,

        /// <summary>刀剑/近战劈砍。</summary>
        Slashing = 1,

        /// <summary>钝击/冲撞（克重甲）。</summary>
        Blunt = 2,

        /// <summary>火焰（持续燃烧，面杀伤）。</summary>
        Fire = 3,

        /// <summary>爆炸（炮弹/范围）。</summary>
        Explosive = 4,

        /// <summary>穿甲弹（坦克/现代，克制重型装甲）。</summary>
        ArmorPiercing = 5,
    }

    /// <summary>
    /// 战斗结算服务（combat 插件实现）。统一伤害公式、护甲克制、经验与升级，
    /// 所有击杀都经由本服务，发布 ProjectileHit/EntityDied 事件，保证战斗口径唯一。
    /// </summary>
    public interface ICombatService
    {
        /// <summary>对可移动单位造成伤害（含护甲减免、死亡结算）。</summary>
        void DamageUnit(EntityId target, float amount, DamageType type, EntityId source);

        /// <summary>请求一次攻击（由 source 对 target，按射速/射程在模拟层结算，发射投射物事件）。</summary>
        void ResolveAttack(EntityId source, EntityId target);

        /// <summary>给单位增加经验（达到阈值升级，提升 10 维参数）。</summary>
        void AwardExperience(EntityId unit, int experience);
    }
}
