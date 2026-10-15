using System;

namespace PxC.Core.World
{
    /// <summary>
    /// 实体移动域（分类表，唯一权威）。
    /// 生成系统据此选择出生域与移动逻辑：Land 永不进入水域/船舶牵引。
    /// </summary>
    public enum EntityDomain
    {
        /// <summary>陆地：人员、骑兵、阵兵、战车、坦克、装甲车、导弹车、车辆。</summary>
        Land = 0,
        /// <summary>船舶：小船/帆船/渔船/战船/轮船/航母/潜艇。</summary>
        Naval = 1,
        /// <summary>空中：飞机、舰载机、导弹、运输机。</summary>
        Air = 2,
        /// <summary>静态：建筑、塔防、桥、奇观、树木、资源点。</summary>
        Static = 3
    }

    /// <summary>
    /// 稳定实体句柄。运行时不 new class，仅传递此结构。
    /// Index 为类型存储中的位置，Generation 用于识别已失效的旧引用（槽位复用时递增）。
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public readonly int Index;
        public readonly int Generation;
        public readonly EntityDomain Domain;

        public EntityId(int index, int generation, EntityDomain domain)
        {
            Index = index;
            Generation = generation;
            Domain = domain;
        }

        public bool IsValid => Index >= 0 && Generation != 0;

        public bool Equals(EntityId other) =>
            Index == other.Index && Generation == other.Generation && Domain == other.Domain;

        public override bool Equals(object obj) => obj is EntityId other && Equals(other);

        public override int GetHashCode() => (Index * 397) ^ Generation;

        public static bool operator ==(EntityId a, EntityId b) => a.Equals(b);
        public static bool operator !=(EntityId a, EntityId b) => !a.Equals(b);

        public override string ToString() => $"{Domain}#{Index}.{Generation}";

        public static readonly EntityId Invalid = new(-1, 0, EntityDomain.Land);
    }
}
