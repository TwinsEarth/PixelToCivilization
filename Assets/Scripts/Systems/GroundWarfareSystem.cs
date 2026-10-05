// V9.5.4 地面作战部队：1949前古典三型（骑兵/列方阵兵/马拉战车）→ 1949后现代三型（坦克/装甲车/导弹车）直接替换
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PixelToCivilization.Systems
{
    // 地面作战单位定义（V9.4.6 1949 装甲部队 → V9.5.4 分代三型替换）
    public class GroundDef
    {
        public string Id;
        public string Name;
        public bool Modern;             // true=1949后现代型；false=1949前古典型
        public int CostSteel, CostGold, CostWood, CostFood;
        public int Hp, Atk, Speed, Range, Crew;
        public string AttackType;
        public int MatPbr;
        public string WeaponKind;       // cannon/missile/melee
        public int ShellPbr, BodyPbr, WheelPbr, TurretPbr;
    }

    public class GroundUnit
    {
        public string TypeId;
        public int Faction;             // 阵营键（与 MilitarySystem 统一）
        public bool Enemy;
        public Vector3 Pos;
        public int Hp, MaxHp, Atk, Speed, Range;
        public float Cooldown;
        public int Level;
        public int Age;
        public int TargetFaction = -1;
        public GameObject View;
        public Vector3 TargetPos;
        public bool HasTarget;
        public float WanderT;
        public Vector3 WanderDir;
        public int State;               // 0待机 1行军 2攻击 3追击
        public string Name;
        public int Crew;                // 载员（坦克/装甲车）
        public int MaxCrew;
        public float FireT;
        public Vector3 FireDir;
        public bool Fired;
        public int OwnerPlayer;         // 1=我方 0=敌方
    }

    public class GroundWarfareSystem : MonoBehaviour
    {
        public static GroundWarfareSystem I;
        public Dictionary<string, GroundDef> Defs = new Dictionary<string, GroundDef>();
        public List<GroundUnit> Units = new List<GroundUnit>();
        public List<GroundUnit> Pending = new List<GroundUnit>();
        public const int MaxUnits = 300;

        void Awake()
        {
            I = this;
            BuildDefs();
        }

        void BuildDefs()
        {
            // 古典三型（1949 前，耗木/粮/金，不耗钢）
            Defs["cavalry"] = new GroundDef { Id="cavalry", Name="骑兵", Modern=false, CostWood=40, CostGold=10, CostFood=20, Hp=60, Atk=9, Speed=6, Range=1, Crew=1, AttackType="melee", MatPbr=9621, WeaponKind="melee", BodyPbr=9621, WheelPbr=9621, TurretPbr=9621 };
            Defs["phalanx"] = new GroundDef { Id="phalanx", Name="列方阵兵", Modern=false, CostWood=20, CostGold=10, CostFood=30, Hp=140, Atk=5, Speed=2, Range=1, Crew=1, AttackType="melee", MatPbr=9622, WeaponKind="melee", BodyPbr=9622, WheelPbr=9622, TurretPbr=9622 };
            Defs["chariot"] = new GroundDef { Id="chariot", Name="马拉战车", Modern=false, CostWood=80, CostGold=30, CostFood=20, Hp=110, Atk=12, Speed=5, Range=1, Crew=2, AttackType="melee", MatPbr=9623, WeaponKind="melee", BodyPbr=9623, WheelPbr=9623, TurretPbr=9623 };
            // 现代三型（1949 后，耗钢/金）
            Defs["tank"] = new GroundDef { Id="tank", Name="坦克", Modern=true, CostSteel=120, CostGold=40, Hp=400, Atk=40, Speed=4, Range=3, Crew=4, AttackType="cannon", MatPbr=9624, WeaponKind="cannon", ShellPbr=9624, BodyPbr=9624, WheelPbr=9624, TurretPbr=9624 };
            Defs["apc"] = new GroundDef { Id="apc", Name="装甲车", Modern=true, CostSteel=60, CostGold=20, Hp=250, Atk=18, Speed=6, Range=3, Crew=8, AttackType="cannon", MatPbr=9625, WeaponKind="cannon", ShellPbr=9625, BodyPbr=9625, WheelPbr=9625, TurretPbr=9625 };
            Defs["missile_vehicle"] = new GroundDef { Id="missile_vehicle", Name="导弹车", Modern=true, CostSteel=100, CostGold=60, Hp=200, Atk=70, Speed=4, Range=8, Crew=3, AttackType="missile", MatPbr=9626, WeaponKind="missile", ShellPbr=9626, BodyPbr=9626, WheelPbr=9626, TurretPbr=9626 };
        }

        // 当前世代可列装清单（替换而非解锁）：modern == (S.Year>=4949)
        public List<GroundDef> AvailableDefs()
        {
            bool modern = Core.GameState.S.Year >= 4949;
            return Defs.Values.Where(d => d.Modern == modern).ToList();
        }

        public bool EraMatch(string typeId)
        {
            if (!Defs.TryGetValue(typeId, out var d)) return false;
            return d.Modern == (Core.GameState.S.Year >= 4949);
        }

        public bool HasResources(GroundDef d, out string why)
        {
            var s = Core.GameState.S;
            if (d.Modern)
            {
                if (s.Res["steel"] < d.CostSteel) { why = "钢铁不足"; return false; }
                if (s.Res["gold"] < d.CostGold) { why = "金币不足"; return false; }
            }
            else
            {
                if (s.Res["wood"] < d.CostWood) { why = "木材不足"; return false; }
                if (s.Res["gold"] < d.CostGold) { why = "金币不足"; return false; }
                if (s.Res["food"] < d.CostFood) { why = "粮食不足"; return false; }
            }
            why = null;
            return true;
        }

        public void Spend(GroundDef d)
        {
            var s = Core.GameState.S;
            if (d.Modern)
            {
                s.Res["steel"] -= d.CostSteel;
                s.Res["gold"] -= d.CostGold;
            }
            else
            {
                s.Res["wood"] -= d.CostWood;
                s.Res["gold"] -= d.CostGold;
                s.Res["food"] -= d.CostFood;
            }
        }

        // 玩家列装地面部队（UI 调用）
        public bool BuildGround(string typeId, int count, out string msg)
        {
            if (!Defs.TryGetValue(typeId, out var d)) { msg = "未知兵种"; return false; }
            if (!EraMatch(typeId))
            {
                bool needModern = Core.GameState.S.Year >= 4949;
                if (needModern) msg = "已进入现代纪元，古典部队已换代，请列装坦克/装甲车/导弹车";
                else msg = "尚处古典纪元，还需 " + (4949 - Core.GameState.S.Year) + " 年才可列装现代装备";
                return false;
            }
            if (!HasResources(d, out string why)) { msg = why; return false; }
            Spend(d);
            for (int i = 0; i < count; i++)
            {
                var u = new GroundUnit();
                u.TypeId = d.Id; u.Name = d.Name; u.Faction = Core.GameState.S.FactionKey; u.Enemy = false;
                u.OwnerPlayer = 1;
                u.MaxHp = u.Hp = d.Hp + 10 * u.Level; u.Atk = d.Atk; u.Speed = d.Speed; u.Range = d.Range;
                u.MaxCrew = d.Crew; u.Crew = 0;
                var v = PlayerSpawnPos();
                u.Pos = v; u.TargetPos = v;
                u.View = BuildView(d, v, out Vector3 size);
                Units.Add(u);
            }
            msg = "已列装 " + count + " " + d.Name;
            return true;
        }

        // Debug 快速生成：按年份换型
        public bool DebugBuildOwn(int count, out string msg)
        {
            bool modern = Core.GameState.S.Year >= 4949;
            string[] ids = modern ? new[]{"tank","apc","missile_vehicle"} : new[]{"cavalry","phalanx","chariot"};
            string id = ids[UnityEngine.Random.Range(0, ids.Length)];
            return BuildGround(id, count, out msg);
        }

        // 敌方镜像生成：与我方数量等比例（V9.4.3）
        public void SyncEnemyCount()
        {
            int mine = Units.Count(u => !u.Enemy);
            int foe = Units.Count(u => u.Enemy);
            if (foe < mine)
            {
                bool modern = Core.GameState.S.Year >= 4949;
                string[] ids = modern ? new[]{"tank","apc","missile_vehicle"} : new[]{"cavalry","phalanx","chariot"};
                for (int i = foe; i < mine && i < MaxUnits; i++)
                {
                    string id = ids[UnityEngine.Random.Range(0, ids.Length)];
                    if (!Defs.TryGetValue(id, out var d)) continue;
                    var u = new GroundUnit();
                    u.TypeId = d.Id; u.Name = d.Name; u.Enemy = true; u.OwnerPlayer = 0;
                    u.Faction = UnityEngine.Random.Range(1, 5); // 敌方阵营 1..4
                    u.MaxHp = u.Hp = d.Hp; u.Atk = d.Atk; u.Speed = d.Speed; u.Range = d.Range;
                    u.MaxCrew = d.Crew; u.Crew = 0;
                    var v = EnemySpawnPos();
                    u.Pos = v; u.TargetPos = v;
                    u.View = BuildView(d, v, out _);
                    Units.Add(u);
                }
            }
        }

        Vector3 PlayerSpawnPos()
        {
            var s = Core.GameState.S;
            Vector3 c = s.HomeVillage != Vector3.zero ? s.HomeVillage : new Vector3(0, 0, 0);
            return c + new Vector3(UnityEngine.Random.Range(-14, 14), 0, UnityEngine.Random.Range(-14, 14));
        }

        Vector3 EnemySpawnPos()
        {
            var s = Core.GameState.S;
            Vector3 c = s.HomeVillage != Vector3.zero ? s.HomeVillage : new Vector3(0, 0, 0);
            return c + new Vector3(UnityEngine.Random.Range(-60, -40), 0, UnityEngine.Random.Range(-60, -40));
        }

        // 程序化几何：分代三型外观
        public GameObject BuildView(GroundDef d, Vector3 pos, out Vector3 size)
        {
            var root = new GameObject("ground_" + d.Id);
            root.transform.position = pos;
            var r = root.AddComponent<MeshRenderer>();
            var mf = root.AddComponent<MeshFilter>();
            var mat = new Material(Shader.Find("Standard"));
            if (mat == null) mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat == null) mat = new Material(Shader.Find("Legacy Shaders/Diffuse"));
            mat.color = d.Modern ? new Color(0.45f,0.5f,0.42f) : new Color(0.72f,0.42f,0.3f);
            mat.mainTextureScale = Vector2.one;
            var mesh = new Mesh();
            var verts = new List<Vector3>(); var idx = new List<int>(); var cols = new List<Color>();
            Color body = d.Modern ? new Color(0.5f,0.55f,0.5f) : new Color(0.65f,0.45f,0.35f);
            if (d.Id == "cavalry")
            {
                // 骑兵：马身 + 骑手 + 长矛
                AddBox(verts, idx, cols, new Vector3(-0.8f, 0.8f, -0.35f), new Vector3(1.6f, 0.7f, 1.4f), body, null);
                AddBox(verts, idx, cols, new Vector3(0.5f, 1.6f, 0), new Vector3(0.7f, 0.8f, 0.6f), new Color(0.9f,0.7f,0.4f), null);
                AddBox(verts, idx, cols, new Vector3(1.2f, 1.9f, 0), new Vector3(0.1f, 1.2f, 0.1f), new Color(0.8f,0.8f,0.8f), null);
                AddBox(verts, idx, cols, new Vector3(-1.0f, 0.4f, 0), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.5f,0.3f,0.2f), null);
            }
            else if (d.Id == "phalanx")
            {
                AddBox(verts, idx, cols, new Vector3(0, 1.0f, 0), new Vector3(0.9f, 1.8f, 0.7f), new Color(0.5f,0.55f,0.6f), null);
                AddBox(verts, idx, cols, new Vector3(0.45f, 1.2f, 0), new Vector3(0.7f, 1.4f, 0.12f), new Color(0.75f,0.25f,0.2f), null);
                AddBox(verts, idx, cols, new Vector3(0.9f, 1.6f, 0), new Vector3(0.08f, 1.5f, 0.08f), new Color(0.7f,0.7f,0.6f), null);
            }
            else if (d.Id == "chariot")
            {
                // 马拉战车：双马 + 车架 + 车夫
                AddBox(verts, idx, cols, new Vector3(-1.4f, 0.9f, -0.4f), new Vector3(1.2f, 0.6f, 0.9f), new Color(0.7f,0.5f,0.4f), null);
                AddBox(verts, idx, cols, new Vector3(-1.4f, 0.9f, 0.4f), new Vector3(1.2f, 0.6f, 0.9f), new Color(0.6f,0.45f,0.35f), null);
                AddBox(verts, idx, cols, new Vector3(0.4f, 1.1f, 0), new Vector3(1.4f, 0.5f, 1.2f), new Color(0.55f,0.35f,0.2f), null);
                AddBox(verts, idx, cols, new Vector3(1.4f, 1.5f, 0), new Vector3(0.6f, 0.6f, 0.5f), new Color(0.9f,0.7f,0.4f), null);
            }
            else if (d.Id == "tank")
            {
                AddBox(verts, idx, cols, new Vector3(0, 0.45f, 0), new Vector3(2.4f, 0.9f, 1.6f), new Color(0.35f,0.4f,0.35f), null);
                AddBox(verts, idx, cols, new Vector3(0, 0.9f, 0), new Vector3(1.8f, 0.6f, 1.2f), new Color(0.4f,0.45f,0.4f), null);
                AddBox(verts, idx, cols, new Vector3(0.9f, 1.15f, 0), new Vector3(0.9f, 0.5f, 0.5f), new Color(0.3f,0.35f,0.3f), null);
                AddBox(verts, idx, cols, new Vector3(-1.2f, 0.45f, 0), new Vector3(0.5f, 0.9f, 1.6f), new Color(0.3f,0.35f,0.3f), null);
            }
            else if (d.Id == "apc")
            {
                AddBox(verts, idx, cols, new Vector3(0, 0.6f, 0), new Vector3(2.0f, 1.2f, 1.4f), new Color(0.45f,0.5f,0.42f), null);
                AddBox(verts, idx, cols, new Vector3(0, 1.0f, 0), new Vector3(1.2f, 0.4f, 1.0f), new Color(0.35f,0.4f,0.35f), null);
                AddBox(verts, idx, cols, new Vector3(-1.1f, 0.6f, 0), new Vector3(0.3f, 1.2f, 1.4f), new Color(0.3f,0.35f,0.3f), null);
            }
            else if (d.Id == "missile_vehicle")
            {
                AddBox(verts, idx, cols, new Vector3(0, 0.5f, 0), new Vector3(2.2f, 0.9f, 1.5f), new Color(0.4f,0.45f,0.38f), null);
                AddBox(verts, idx, cols, new Vector3(0, 1.0f, 0), new Vector3(1.4f, 0.7f, 1.0f), new Color(0.5f,0.55f,0.5f), null);
                AddBox(verts, idx, cols, new Vector3(0.7f, 1.6f, 0), new Vector3(0.5f, 1.2f, 0.5f), new Color(0.2f,0.25f,0.3f), null);
            }
            else
            {
                AddBox(verts, idx, cols, new Vector3(0, 0.5f, 0), new Vector3(1.2f, 1.0f, 1.2f), body, null);
            }
            mesh.vertices = verts.ToArray(); mesh.colors = cols.ToArray();
            var tris = new List<int>();
            for (int i = 0; i < idx.Count; i += 4) { tris.Add(idx[i]); tris.Add(idx[i+1]); tris.Add(idx[i+2]); tris.Add(idx[i]); tris.Add(idx[i+2]); tris.Add(idx[i+3]); }
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;
            r.sharedMaterial = mat;
            size = new Vector3(2.5f, 2.0f, 2.0f);
            return root;
        }

        // 挂 parent 参数：部件挂单位节点（V9.5.4 修复部件钉原点）
        void AddBox(List<Vector3> verts, List<int> idx, List<Color> cols, Vector3 center, Vector3 size, Color col, Transform parent)
        {
            int baseIdx = verts.Count;
            var half = size * 0.5f;
            Vector3[] v = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float sx = (i & 1) == 0 ? -half.x : half.x;
                float sy = (i & 2) == 0 ? -half.y : half.y;
                float sz = (i & 4) == 0 ? -half.z : half.z;
                v[i] = center + new Vector3(sx, sy, sz);
            }
            int[,] quads = new int[,] { {0,1,3,2},{4,5,7,6},{0,1,5,4},{2,3,7,6},{0,2,6,4},{1,3,7,5} };
            for (int q = 0; q < 6; q++)
            {
                for (int k = 0; k < 4; k++) { verts.Add(v[quads[q,k]]); idx.Add(baseIdx + k); cols.Add(col); }
                baseIdx += 4;
            }
        }

        void Update()
        {
            if (Core.GameState.S == null || Core.GameState.S.Paused) return;
            float dt = Time.deltaTime;
            SyncEnemyCount();
            for (int i = Units.Count - 1; i >= 0; i--)
            {
                var u = Units[i];
                if (u == null || u.View == null) { Units.RemoveAt(i); continue; }
                u.Age++;
                // 索敌：统一跨阵营（V9.4.7）
                GroundUnit target = null; float best = float.MaxValue;
                for (int j = 0; j < Units.Count; j++)
                {
                    var o = Units[j];
                    if (o == u || o.Enemy == u.Enemy) continue;
                    float dx = o.Pos.x - u.Pos.x, dz = o.Pos.z - u.Pos.z;
                    float dist = dx*dx + dz*dz;
                    if (dist < best) { best = dist; target = o; }
                }
                float atkRange = u.Range * 4f;
                if (target != null)
                {
                    float dist = Mathf.Sqrt(best);
                    if (dist < atkRange * 1.4f)
                    {
                        u.State = 3; u.TargetPos = target.Pos;
                        Vector3 dir = (target.Pos - u.Pos).normalized;
                        u.FireDir = dir;
                        if (dist <= atkRange)
                        {
                            u.State = 2;
                            u.Cooldown -= dt;
                            if (u.Cooldown <= 0)
                            {
                                u.Cooldown = 1.2f;
                                target.Hp -= u.Atk;
                                u.Fired = true; u.FireT = 0.2f;
                                if (target.Hp <= 0)
                                {
                                    if (target.View != null) UnityEngine.Object.Destroy(target.View);
                                    Units.Remove(target);
                                    if (!u.Enemy && Core.GameState.S != null)
                                        Core.GameState.S.Log("我方 " + u.Name + " 击毁了敌方 " + target.Name);
                                }
                            }
                        }
                    }
                }
                if (u.State == 3 || u.State == 1)
                {
                    Vector3 to = u.TargetPos - u.Pos;
                    float dist = to.magnitude;
                    if (dist > 0.5f)
                    {
                        u.Pos += to.normalized * u.Speed * dt * 1.5f;
                        u.View.transform.position = u.Pos;
                    }
                    else u.State = 0;
                }
                else if (u.State == 0)
                {
                    u.WanderT -= dt;
                    if (u.WanderT <= 0)
                    {
                        u.WanderT = UnityEngine.Random.Range(2, 5);
                        u.WanderDir = new Vector3(UnityEngine.Random.Range(-1f,1f), 0, UnityEngine.Random.Range(-1f,1f)).normalized;
                    }
                    u.Pos += u.WanderDir * u.Speed * dt * 0.4f;
                    u.View.transform.position = u.Pos;
                }
                u.FireT -= dt;
            }
        }
    }
}
