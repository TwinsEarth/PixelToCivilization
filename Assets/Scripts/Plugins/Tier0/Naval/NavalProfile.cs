using PxC.Gameplay.Services;

namespace PxC.Plugins.Naval
{
    /// <summary>
    /// 船艇 10 维参数 + 感知/伤害口径（型号目录）。
    /// 速度相对口径（世界单位/秒）；海军感知/攻击最大不超过 100 格。
    /// </summary>
    public sealed class NavalProfile
    {
        public float Speed;
        public float Durability;
        public float Attack;
        public float Defense;
        public float AttackRange;
        public float FireRate;
        public int CarryCapacity;
        public int Size;
        public int Rank;
        public int Intelligence;
        public float PerceptionRadius;
        public DamageType Damage;
        public float LevelGrowth;
        /// <summary>是否无武装（渔船/木筏不主动攻击）。</summary>
        public bool Civilian;

        public static NavalProfile Get(NavalModel model) => _table[(int)model];

        private static readonly NavalProfile[] _table = Build();

        private static NavalProfile[] Build()
        {
            var t = new NavalProfile[NavalModels.Count];

            t[(int)NavalModel.Raft] = new NavalProfile
            {
                Speed = 0.02f, Durability = 40f, Attack = 0f, Defense = 2f,
                AttackRange = 0f, FireRate = 0f, CarryCapacity = 2,
                Size = 1, Rank = 1, Intelligence = 2,
                PerceptionRadius = 8f, Damage = DamageType.Blunt, LevelGrowth = 0.05f, Civilian = true,
            };

            t[(int)NavalModel.FishingBoat] = new NavalProfile
            {
                Speed = 0.03f, Durability = 80f, Attack = 0f, Defense = 4f,
                AttackRange = 0f, FireRate = 0f, CarryCapacity = 4,
                Size = 2, Rank = 1, Intelligence = 2,
                PerceptionRadius = 10f, Damage = DamageType.Blunt, LevelGrowth = 0.05f, Civilian = true,
            };

            t[(int)NavalModel.Warship] = new NavalProfile
            {
                Speed = 0.04f, Durability = 250f, Attack = 20f, Defense = 14f,
                AttackRange = 3f, FireRate = 0.6f, CarryCapacity = 60,
                Size = 4, Rank = 3, Intelligence = 4,
                PerceptionRadius = 18f, Damage = DamageType.Slashing, LevelGrowth = 0.10f, Civilian = false,
            };

            t[(int)NavalModel.SailingShip] = new NavalProfile
            {
                Speed = 0.05f, Durability = 300f, Attack = 25f, Defense = 16f,
                AttackRange = 8f, FireRate = 0.5f, CarryCapacity = 80,
                Size = 4, Rank = 3, Intelligence = 4,
                PerceptionRadius = 20f, Damage = DamageType.Explosive, LevelGrowth = 0.10f, Civilian = false,
            };

            t[(int)NavalModel.Steamship] = new NavalProfile
            {
                Speed = 0.10f, Durability = 500f, Attack = 40f, Defense = 24f,
                AttackRange = 15f, FireRate = 0.5f, CarryCapacity = 100,
                Size = 5, Rank = 4, Intelligence = 6,
                PerceptionRadius = 30f, Damage = DamageType.Explosive, LevelGrowth = 0.08f, Civilian = false,
            };

            t[(int)NavalModel.Submarine] = new NavalProfile
            {
                Speed = 0.08f, Durability = 400f, Attack = 70f, Defense = 20f,
                AttackRange = 20f, FireRate = 0.3f, CarryCapacity = 40,
                Size = 4, Rank = 5, Intelligence = 7,
                PerceptionRadius = 35f, Damage = DamageType.Piercing, LevelGrowth = 0.08f, Civilian = false,
            };

            t[(int)NavalModel.AircraftCarrier] = new NavalProfile
            {
                Speed = 0.06f, Durability = 900f, Attack = 50f, Defense = 30f,
                AttackRange = 60f, FireRate = 0.2f, CarryCapacity = 500,
                Size = 6, Rank = 6, Intelligence = 8,
                PerceptionRadius = 80f, Damage = DamageType.Explosive, LevelGrowth = 0.07f, Civilian = false,
            };

            return t;
        }
    }
}
