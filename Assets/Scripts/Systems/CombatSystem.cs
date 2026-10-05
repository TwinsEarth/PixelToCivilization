using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.4.7 统一战斗目录：把五类作战单位（军舰/军车/步兵/塔防/骑兵）与可破坏建筑纳入同一套
    /// 阵营判定、跨类型索敌、伤害分发。目录为派生缓存，4Hz 从各系统重建，不进存档；读档/Reset 后 Tick 自动恢复。
    /// 敌对判定：FactionKey 不同即敌对（我方="PLAYER"，敌方各派=各自派系名；敌方各派之间也互相攻击）。
    /// </summary>
    public class CombatSystem : GameSystemBase
    {
        public const int K_SHIP = 0;      // 军舰（ShipEntity）
        public const int K_GROUND = 1;    // 军车（GroundWarfareSystem.GroundUnit：坦克/装甲车/导弹车）
        public const int K_INF = 2;       // 步兵（EnemyUnit / FriendlyUnit）
        public const int K_TOWER = 3;     // 防御塔（BuildingEntity，attack>0）
        public const int K_CAV = 4;       // 骑兵（EnemyUnit / FriendlyUnit）
        public const int K_BUILDING = 5;  // 普通建筑（BuildingEntity）

        public const string PlayerKey = "PLAYER";

        public readonly List<CombatTarget> Targets = new();
        float _nextRebuild;

        public override void Tick(float dt)
        {
            if (Time.unscaledTime < _nextRebuild) return;
            _nextRebuild = Time.unscaledTime + 0.25f;   // 4Hz
            Rebuild();
        }

        void Rebuild()
        {
            Targets.Clear();
            try { GM.Naval?.RegisterCombat(this); }
            catch (System.Exception e) { Debug.LogError("[Combat:naval] " + e.GetType().Name + ": " + e.Message); }
            try { GM.Ground?.RegisterCombat(this); }
            catch (System.Exception e) { Debug.LogError("[Combat:ground] " + e.GetType().Name + ": " + e.Message); }
            try { GM.Military?.RegisterCombat(this); }
            catch (System.Exception e) { Debug.LogError("[Combat:military] " + e.GetType().Name + ": " + e.Message); }
            try { RegisterBuildings(); }
            catch (System.Exception e) { Debug.LogError("[Combat:building] " + e.GetType().Name + ": " + e.Message); }
        }

        /// <summary>各系统注册一个作战单位/建筑（range 为世界单位；key 传阵营键，我方传 PlayerKey）</summary>
        public CombatTarget Add(int kind, object @ref, float x, float z, float hp, float attack, float range, string key)
        {
            var t = new CombatTarget
            {
                Kind = kind, Ref = @ref, X = x, Z = z, Hp = hp,
                Attack = attack, Range = range, Key = string.IsNullOrEmpty(key) ? PlayerKey : key
            };
            Targets.Add(t);
            return t;
        }

        /// <summary>注册建筑为战斗目标：只注册防御工事（attack>0 塔 / defense>0 城墙碉堡）为 K_TOWER。
        /// V9.5.6 治本：生产建筑（hut/farm/market 等）不再注册进统一战斗目录——否则敌方势力士兵平时即自动索敌锁定并
        /// 拆光全部生产建筑（开局 4 势力 × 常备兵在高倍速下 20 秒可拆光 24 栋），产出归零导致粮食/金币/文化/科技
        /// "看似乱扣"地慢性跌光。战争状态（WarActive）下 MilitarySystem.ChooseGoal 攻城逻辑仍可拆任意建筑，
        /// 保留 V9.4.7「建筑可被破坏」语义。树木不在 S.Buildings，天然不注册。</summary>
        void RegisterBuildings()
        {
            foreach (var b in S.Buildings)
            {
                if (b == null || b.Def == null) continue;
                if (b.Def.GetFunc("attack") <= 0 && b.Def.GetFunc("defense") <= 0) continue;
                int lv = Mathf.Max(1, b.Level);
                float atk = b.Def.GetFunc("attack") * (1f + (lv - 1) * 0.3f);
                float rng = b.Def.GetFunc("range") * (1f + (lv - 1) * 0.08f) * GameConstants.Tile;
                Add(K_TOWER, b, b.X, b.Z, b.Hp, atk, rng, PlayerKey);
            }
        }

        /// <summary>统一索敌：返回阵营键不同、距离 ≤ range（世界单位）的最近目标；无则 null</summary>
        public CombatTarget NearestHostile(float x, float z, float range, string myKey)
        {
            CombatTarget best = null;
            float bd = range * range;
            string key = string.IsNullOrEmpty(myKey) ? PlayerKey : myKey;
            foreach (var t in Targets)
            {
                if (t.Key == key || t.Hp <= 0) continue;
                float dx = t.X - x, dz = t.Z - z, d = dx * dx + dz * dz;
                if (d < bd) { bd = d; best = t; }
            }
            return best;
        }

        /// <summary>对某点范围（世界单位）内所有敌对目标造成伤害（火炮溅射/火塔 AOE 用）</summary>
        public int DamageArea(float x, float z, float radius, float dmg, string myKey)
        {
            int hit = 0;
            string key = string.IsNullOrEmpty(myKey) ? PlayerKey : myKey;
            foreach (var t in Targets)
            {
                if (t.Key == key || t.Hp <= 0) continue;
                float dx = t.X - x, dz = t.Z - z;
                if (dx * dx + dz * dz <= radius * radius) { Damage(t, dmg); hit++; }
            }
            return hit;
        }

        /// <summary>统一伤害分发：按 Kind 回到各系统扣血/摧毁</summary>
        public void Damage(CombatTarget t, float dmg)
        {
            if (t == null || dmg <= 0f || t.Hp <= 0f) return;
            switch (t.Kind)
            {
                case K_SHIP:
                    if (t.Ref is ShipEntity sh) GM.Naval?.DamageShip(sh, dmg);
                    break;
                case K_GROUND:
                    if (t.Ref is GroundWarfareSystem.GroundUnit gu) GM.Ground?.DamageGround(gu, dmg);
                    break;
                case K_INF:
                case K_CAV:
                    GM.Military?.DamageUnit(t.Ref, dmg);
                    break;
                case K_TOWER:
                case K_BUILDING:
                    if (t.Ref is BuildingEntity be) DamageBuilding(be, dmg);
                    break;
            }
        }

        void DamageBuilding(BuildingEntity b, float dmg)
        {
            if (b == null || b.Hp <= 0f) return;
            b.Hp -= dmg;
            if (b.Hp <= 0f)
            {
                try { GM.Building?.DestroyByEnemy(b); }
                catch (System.Exception e) { Debug.LogError("[Combat:destroy] " + e.Message); }
            }
        }

        /// <summary>军舰阵营键：我方船（FactionId 空）= PLAYER；敌舰 = 其 FactionId</summary>
        public static string KeyOf(ShipEntity s) => string.IsNullOrEmpty(s?.FactionId) ? PlayerKey : s.FactionId;
    }

    /// <summary>统一战斗目标（派生缓存，字段由各系统注册时刷新）</summary>
    public class CombatTarget
    {
        public int Kind;
        public object Ref;
        public float X, Z;
        public float Hp;
        public float Attack;
        public float Range;
        public string Key;
    }
}
