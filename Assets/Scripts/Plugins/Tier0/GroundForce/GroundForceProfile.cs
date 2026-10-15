using PxC.Gameplay.Services;

namespace PxC.Plugins.GroundForce
{
    /// <summary>
    /// 地面单位 10 维参数 + 感知/伤害口径（型号目录，数据驱动）。
    /// 十维：速度 / 耐久 / 攻击 / 防御 / 射程 / 射速 / 载人量 / 体积 / 军衔 / 智能等级。
    /// 数值随等级按 <see cref="LevelGrowth"/> 提升；速度为相对口径（世界单位/秒）。
    /// </summary>
    public sealed class GroundForceProfile
    {
        /// <summary>移动速度（世界单位/秒，相对口径）。</summary>
        public float Speed;

        /// <summary>耐久度（最大生命）。</summary>
        public float Durability;

        /// <summary>攻击力。</summary>
        public float Attack;

        /// <summary>防御力（护甲）。</summary>
        public float Defense;

        /// <summary>攻击射程（世界单位）。</summary>
        public float AttackRange;

        /// <summary>射速（次/秒）。</summary>
        public float FireRate;

        /// <summary>载人量（编制人数）。</summary>
        public int CarryCapacity;

        /// <summary>体积（占位/碰撞相对等级，1-5）。</summary>
        public int Size;

        /// <summary>军衔（指挥相对等级，1-9）。</summary>
        public int Rank;

        /// <summary>智能等级（AI 自主程度，1-10）。</summary>
        public int Intelligence;

        /// <summary>感知/视野半径（寻敌用，世界单位）。</summary>
        public float PerceptionRadius;

        /// <summary>伤害类型（护甲克制）。</summary>
        public DamageType Damage;

        /// <summary>每级各维成长比例（攻击/耐久/防御），速度不成长。</summary>
        public float LevelGrowth;

        /// <summary>取得某型号的权威参数。</summary>
        public static GroundForceProfile Get(GroundForceModel model) => _table[(int)model];

        private static readonly GroundForceProfile[] _table = Build();

        private static GroundForceProfile[] Build()
        {
            var t = new GroundForceProfile[GroundForceModels.Count];

            t[(int)GroundForceModel.Cavalry] = new GroundForceProfile
            {
                Speed = 0.06f, Durability = 120f, Attack = 18f, Defense = 8f,
                AttackRange = 1.6f, FireRate = 0.8f, CarryCapacity = 1,
                Size = 2, Rank = 2, Intelligence = 3,
                PerceptionRadius = 12f, Damage = DamageType.Slashing, LevelGrowth = 0.12f,
            };

            t[(int)GroundForceModel.Formation] = new GroundForceProfile
            {
                Speed = 0.03f, Durability = 150f, Attack = 14f, Defense = 14f,
                AttackRange = 1.8f, FireRate = 1.0f, CarryCapacity = 3,
                Size = 2, Rank = 2, Intelligence = 3,
                PerceptionRadius = 8f, Damage = DamageType.Piercing, LevelGrowth = 0.10f,
            };

            t[(int)GroundForceModel.Chariot] = new GroundForceProfile
            {
                Speed = 0.05f, Durability = 200f, Attack = 22f, Defense = 10f,
                AttackRange = 2.2f, FireRate = 0.7f, CarryCapacity = 3,
                Size = 3, Rank = 3, Intelligence = 3,
                PerceptionRadius = 10f, Damage = DamageType.Slashing, LevelGrowth = 0.12f,
            };

            t[(int)GroundForceModel.Tank] = new GroundForceProfile
            {
                Speed = 0.10f, Durability = 600f, Attack = 60f, Defense = 40f,
                AttackRange = 18f, FireRate = 0.4f, CarryCapacity = 4,
                Size = 4, Rank = 5, Intelligence = 6,
                PerceptionRadius = 25f, Damage = DamageType.ArmorPiercing, LevelGrowth = 0.08f,
            };

            t[(int)GroundForceModel.Armor] = new GroundForceProfile
            {
                Speed = 0.12f, Durability = 350f, Attack = 30f, Defense = 22f,
                AttackRange = 12f, FireRate = 1.2f, CarryCapacity = 8,
                Size = 3, Rank = 4, Intelligence = 6,
                PerceptionRadius = 20f, Damage = DamageType.Piercing, LevelGrowth = 0.08f,
            };

            t[(int)GroundForceModel.MissileVehicle] = new GroundForceProfile
            {
                Speed = 0.08f, Durability = 280f, Attack = 80f, Defense = 16f,
                AttackRange = 35f, FireRate = 0.2f, CarryCapacity = 3,
                Size = 3, Rank = 5, Intelligence = 7,
                PerceptionRadius = 40f, Damage = DamageType.Explosive, LevelGrowth = 0.07f,
            };

            return t;
        }
    }
}
