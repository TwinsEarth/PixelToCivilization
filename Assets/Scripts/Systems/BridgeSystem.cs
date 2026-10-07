using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V6.3.9 桥梁系统（建筑-交通）。
    /// 规则：随时代材料进化 木→石→钢铁→混凝土，建桥技术（跨距/宽度/高度/结构）同步升级；
    /// 同一阵营（玩家）在相邻两块【预期陆地】（主大陆/次大陆/岛，以 WorldGenerator.Landmass 为准，
    /// 不使用浅水连通分量编号，避免近岸浅滩把两块陆地误判为同一块）都有据点时，工程司按【最短跨距】自动建桥，
    /// 仅当当前时代技术跨距足以跨越才执行，否则不建；造价随实际跨距与材料增长。桥面可供车辆越水通行。
    /// </summary>
    public class BridgeSystem : GameSystemBase
    {
        private WorldGenerator _w;
        private Transform _root;
        private float _scanTimer;
        private const float ScanInterval = 3f;     // 现实秒：周期性评估（也在每个游戏年补一次）
        private const int MaxBridges = 350;        // 性能上限（普通300+跨海50）
        // V6.5.7 岸线全量参与配对（原 ShoreCap=120 截断会漏掉真正最近点，已移除）
        private int[] _landGrid;                   // 预期陆地网格（按 Landmass 圆盘栅格化，独立于浅水连通分量）
        private int _landBuilt=-1;                 // 已栅格化的陆地数量（变化则重建）

        // 时代 → 技术档：0木 / 1石 / 2钢铁 / 3混凝土
        private static readonly float[] Span = { 28f, 50f, 75f, 100f };     // 各档最大跨距；V6.5.4 硬上限100
        private const float HardMaxSpan = 100f;   // 大陆之间桥最大跨距，超过一律不建
        private const float GrandSpan = 50f;      // 跨距>50 记为跨海大桥
        private const int MainCap = 5, SecCap = 3, IslandCap = 2;  // 单块陆地接桥数：主大陆/次大陆/岛
        // V6.5.5 时间额度 + 寿命：普通桥每10年获1座额度、上限300、寿命10~30年；跨海大桥每50年获1座额度、上限50、寿命50~100年
        private const int NormalEveryYears=10, NormalCapTotal=300, NormalLifeMin=10, NormalLifeMax=30;
        private const int GrandEveryYears=50, GrandCapTotal=50, GrandLifeMin=50, GrandLifeMax=100;
        private const int RunStride=8;   // 每桥8整数：ax,az,bx,bz,tier,span,birthYear,lifeSpan
        private readonly System.Random _rng=new System.Random();
        private static readonly float[] Width = { 4f, 6f, 8f, 12f };         // 桥宽
        private static readonly float[] DeckY = { 0.75f, 1.15f, 1.5f, 1.35f };// 桥面高
        // V9.4.6 高架桥：玩家手动建柱→系统自动与最近柱连片布设高架桥面（统一净空、跨距上限60、寿命100~150年）
        private const float ViaductDeckY = 8f;
        private const float ViaductMaxSpan = 60f;
        private const int ViaductLifeMin = 100, ViaductLifeMax = 150;
        private static readonly Color ViaductColor = new(0.68f,0.68f,0.72f);
        private readonly List<(float x,float z,float since)> _pierPending = new();   // 高架柱放置后 5 秒重试队列
        private float _lastPierRetry;
        private static readonly string[] TName = { "木桥", "石拱桥", "钢铁桁架桥", "混凝土大桥" };
        private static readonly Color[] DeckColor = {
            new(0.45f,0.30f,0.16f), new(0.62f,0.60f,0.56f),
            new(0.42f,0.46f,0.52f), new(0.72f,0.72f,0.74f) };
        private Material[] _deckMat;
        private Material _viaductMat;   // V9.4.6 高架桥/高架柱材质
        private readonly Dictionary<int,int> _cellTier = new();   // 桥面格 → 技术档（供车辆取桥面高度）
        // V9.4.5 玩家桥建筑 5 秒重试队列：bridge_* 放置后 PlayerBuildBridge 失败（潮汐/地图扩展/时机未到）→ 入队每 5 秒重试
        private readonly List<(float x,float z,float since)> _pendingBridge = new();
        private float _lastRetry;

        private int G => _w!=null?_w.G:GameConstants.MaxMapSize;
        private int Idx(int gx,int gz)=>gz*G+gx;

        /// <summary>V9.5.4 高架柱位吸附到格中心：柱视觉与桥面格（DeckView 用格中心）严格对齐，消除"柱桥错位/桥面切柱"错乱</summary>
        private (float x,float z) SnapCell(float wx,float wz)
        {
            if(_w==null) return (wx,wz);
            int gx=Mathf.Clamp(_w.W2CX(wx),0,G-1), gz=Mathf.Clamp(_w.W2CZ(wz),0,G-1);
            return (CellX(gx), CellZ(gz));
        }

        public override void Init(GameManager gm)
        {
            base.Init(gm);
            _w=Object.FindObjectOfType<WorldGenerator>();
            _root=EntityViewFactory.EnsureRoot("Bridges",gm.transform);
            _deckMat=new Material[4];
            for(int i=0;i<4;i++) _deckMat[i]=ShaderHelper.Pbr(DeckColor[i], i==2?0.6f:0f, i>=2?0.5f:0.25f, 700+i);
            _viaductMat=ShaderHelper.Pbr(ViaductColor,0.4f,0.5f,740);
        }

        public override void Tick(float dt)
        {
            if(S.CurrentMap!="home") return;
            // V9.4.4 禁止系统自动建桥：移除 AutoBuildScan，桥梁完全由玩家点击岸边建造（PlayerBuildBridge）
            _scanTimer = ScanInterval;
            // V9.4.5 5 秒重试队列：放置后未即时成桥的点，每 5 秒静默重试（潮汐/地图扩展后可达即自动连接&建造）
            if(Time.time-_lastRetry>=5f)
            {
                _lastRetry=Time.time;
                for(int i=_pendingBridge.Count-1;i>=0;i--)
                {
                    var p=_pendingBridge[i];
                    if(PlayerBuildBridge(p.x,p.z,true)) _pendingBridge.RemoveAt(i);
                    else if(Time.time-p.since>60f) _pendingBridge.RemoveAt(i);   // 60秒仍失败则放弃，避免死循环
                }
            }
            // V9.4.6 高架柱重试队列：放置后未即时连片的柱，每 5 秒静默重试（后续柱补齐后自动无缝连片）
            if(Time.time-_lastPierRetry>=5f)
            {
                _lastPierRetry=Time.time;
                for(int i=_pierPending.Count-1;i>=0;i--)
                {
                    var p=_pierPending[i];
                    if(PlayerBuildPier(p.x,p.z,true)) _pierPending.RemoveAt(i);
                    else if(Time.time-p.since>120f) _pierPending.RemoveAt(i);
                }
            }
        }
        /// <summary>V9.4.5 玩家放置 bridge_* 建筑后注册 5 秒重试</summary>
        public void QueueBridgeRetry(float x,float z){ _pendingBridge.Add((x,z,Time.time)); }
        public override void OnYear(int year){ if(S.CurrentMap!="home")return; DecaySweep(year); }   // V9.4.4 只保留寿命衰减，不再自动扫描建桥
        public override void OnEra(int n,int o){ _scanTimer=0f; }

        /// <summary>V6.5.5 寿命到期拆除：普通桥寿命10~30年、跨海大桥50~100年、高架桥100~150年。
        /// V9.7.1 修复（高架桥地图错乱根因）：桥面拆除时同步清除派生桥面格（BridgeCells）、_cellTier、
        /// 废弃桥墩（S.Piers/PierView）——旧实现只删 BridgeRuns 记录，旧桥面格/柱永久残留，
        /// 车辆悬空行驶、残留柱参与新桥面连片导致新旧桥面交错、主地图错乱闪烁。</summary>
        void DecaySweep(int year)
        {
            if(S.BridgeRuns.Count<RunStride)return;
            // 拆除前先记录所有高架桥面端点：用于区分"待连片柱（玩家已立、尚未成桥）"与"随桥面拆除的废弃柱"
            var oldConnected = new List<(float x,float z)>();
            for(int i=0;i+RunStride-1<S.BridgeRuns.Count;i+=RunStride)
                if(S.BridgeRuns[i+4]==4)
                {
                    oldConnected.Add((CellX(S.BridgeRuns[i]),CellZ(S.BridgeRuns[i+1])));
                    oldConnected.Add((CellX(S.BridgeRuns[i+2]),CellZ(S.BridgeRuns[i+3])));
                }
            bool changed=false;
            var keep=new List<int>();
            for(int i=0;i+RunStride-1<S.BridgeRuns.Count;i+=RunStride)
            {
                int birth=S.BridgeRuns[i+6], life=S.BridgeRuns[i+7];
                int span=S.BridgeRuns[i+5];
                if(life>0 && year-birth>=life)
                {
                    changed=true;
                    GM.AddEvent("info",$"🌉 一座{(span>GrandSpan?"跨海大桥":"桥梁")}已达{life}年使用年限，老化拆除（桥面格、桥墩与额度同步释放）");
                    continue;
                }
                for(int k=0;k<RunStride;k++)keep.Add(S.BridgeRuns[i+k]);
            }
            if(changed){ S.BridgeRuns=keep; RebuildViews(oldConnected); }
        }

        private static int TierOf(int era){ if(era<=0)return 0; if(era<=2)return 1; if(era<=4)return 2; return 3; }

        // 按 Landmass 圆盘把"预期陆地"栅格化（与浅水连通分量解耦），陆地数量变化时重建
        private void EnsureLandGrid()
        {
            if(_w==null)return;
            int lm=_w.Landmasses.Count;
            if(_landGrid!=null && _landBuilt==lm)return;
            // V6.5.8 直接复制权威 ContinentMap（已按 X/Z 轴正确栅格化，且含运行时增长/读档恢复的陆地）
            _landGrid=new int[G*G];
            var cm=_w.ContinentMap;
            for(int gz=0;gz<G;gz++)for(int gx=0;gx<G;gx++)_landGrid[gz*G+gx]=cm[gz,gx];
            _landBuilt=lm;
        }
        private int LandOfCell(int gx,int gz)
        {
            if(gx<0||gx>=G||gz<0||gz>=G)return 0;
            return _landGrid[gz*G+gx];
        }

        // ============ 自动建桥扫描 ============
        private void AutoBuildScan()
        {
            if(_w==null||S.BridgeRuns.Count/RunStride>=MaxBridges) return;
            EnsureLandGrid();
            int tier=TierOf(S.Era);
            float maxSpan=Mathf.Min(Span[tier],HardMaxSpan);
            var occupied=new HashSet<int>();
            foreach(var b in S.Buildings){int lid=LandOfWorld(b.X,b.Z);if(lid>0)occupied.Add(lid);}
            if(occupied.Count<2) return;
            var shore=CollectShores(occupied);
            if(shore.Count<2) return;
            // 既有桥统计：陆地接桥数、普通/跨海现存数
            var incident=new Dictionary<int,int>();
            int usedNormal=0,usedGrand=0;
            for(int i=0;i+RunStride-1<S.BridgeRuns.Count;i+=RunStride)
            {
                int a=LandOfWorld(CellX(S.BridgeRuns[i]),CellZ(S.BridgeRuns[i+1]));
                int b2=LandOfWorld(CellX(S.BridgeRuns[i+2]),CellZ(S.BridgeRuns[i+3]));
                if(a>0)incident[a]=incident.TryGetValue(a,out var ia)?ia+1:1;
                if(b2>0)incident[b2]=incident.TryGetValue(b2,out var ib)?ib+1:1;
                if(S.BridgeRuns[i+5]>GrandSpan)usedGrand++;else usedNormal++;
            }
            // V9.3.8 跨海大桥可达性：人口阈值 pop/100 → pop/50（主村 StartPop80 即 ≥1 座额度，原 pop/100 使早期永远 0 座）
            int capNormal=Mathf.Min(NormalCapTotal,S.Year/NormalEveryYears,Mathf.Max(1,S.Pop/10));
            int capGrand =Mathf.Min(GrandCapTotal,S.Year/GrandEveryYears,Mathf.Max(1,S.Pop/50));
            var paired=new HashSet<long>();
            for(int i=0;i+RunStride-1<S.BridgeRuns.Count;i+=RunStride)
            {
                int a=LandOfWorld(CellX(S.BridgeRuns[i]),CellZ(S.BridgeRuns[i+1]));
                int b2=LandOfWorld(CellX(S.BridgeRuns[i+2]),CellZ(S.BridgeRuns[i+3]));
                if(a>0&&b2>0) paired.Add(PairKey(a,b2));
            }
            var ids=new List<int>(shore.Keys);
            int bestA=-1,bestB=-1,bax=0,baz=0,bbx=0,bbz=0; float bestD=float.MaxValue;
            Dictionary<string,int> bestCost=null; bool bestGrand=false;
            for(int i=0;i<ids.Count;i++)
                for(int j=i+1;j<ids.Count;j++)
                {
                    int A=ids[i],B=ids[j];
                    if(paired.Contains(PairKey(A,B)))continue;
                    if(!LandHasQuota(A,incident)||!LandHasQuota(B,incident))continue;   // 单陆接桥上限
                    if(ClosestShore(shore[A],shore[B],out int ax,out int az,out int bx,out int bz,out float dw))
                    {
                        if(dw<GameConstants.Tile*2f||dw>maxSpan) continue;
                        bool grand=dw>GrandSpan;
                        if(grand){if(usedGrand>=capGrand)continue;}else if(usedNormal>=capNormal)continue; // 时间额度
                        var c=CostOf(tier,dw);
                        if(!CanAfford(c)) continue;
                        if(dw<bestD){bestD=dw;bestA=A;bestB=B;bax=ax;baz=az;bbx=bx;bbz=bz;bestCost=c;bestGrand=grand;}
                    }
                }
            if(bestA<0) return;
            BuildBridge(bax,baz,bbx,bbz,tier,bestCost);
        }

        /// <summary>单块陆地接桥数上限：主大陆(Br≥130)5、次大陆3、无人岛2</summary>
        private bool LandHasQuota(int lid,Dictionary<int,int> incident)
        {
            int cap=IslandCap;
            foreach(var L in _w.Landmasses) if(L.Id==lid){ cap = L.Kind==1?IslandCap : (L.Br>=130f?MainCap:SecCap); break; }
            int used=incident.TryGetValue(lid,out var u)?u:0;
            return used<cap;
        }

        private int LandOfWorld(float wx,float wz)
        {
            int gx=_w.W2CX(wx),gz=_w.W2CZ(wz);
            return LandOfCell(gx,gz);
        }

        /// <summary>对 occupied 每块预期陆地，沿其圆盘包围盒在【全图】取岸线格（陆地且四邻有真海），
        /// 不依赖初始活动疆域——次大陆/岛可能落在尚未展开的外圈。</summary>
        private Dictionary<int,List<Vector2Int>> CollectShores(HashSet<int> occupied)
        {
            var res=new Dictionary<int,List<Vector2Int>>();
            foreach(var L in _w.Landmasses)
            {
                if(!occupied.Contains(L.Id))continue;
                int cx=_w.W2CX(L.Cx),cz=_w.W2CZ(L.Cz);
                int rcx=Mathf.CeilToInt(L.Br*1.15f/GameConstants.Tile),rcz=Mathf.CeilToInt(L.Br*1.15f/_w.TZ);
                var list=new List<Vector2Int>();
                int x0=Mathf.Max(1,cx-rcx),x1=Mathf.Min(G-2,cx+rcx),z0=Mathf.Max(1,cz-rcz),z1=Mathf.Min(G-2,cz+rcz);
                // V6.6.0 端点必须是【真实陆地】：ContinentMap 归属该陆 + 高度高于水面；四邻至少一格真海（纯高度判定，不受圆盘预期陆地干扰）
                for(int gz=z0;gz<=z1;gz++)
                    for(int gx=x0;gx<=x1;gx++)
                    {
                        if(_w.ContinentMap[gz,gx]!=L.Id)continue;
                        if(_w.HeightMap[gz,gx]<GameConstants.WaterLevel)continue;   // 端点绝不能是水面
                        if(!SeaCell(gx+1,gz)&&!SeaCell(gx-1,gz)&&!SeaCell(gx,gz+1)&&!SeaCell(gx,gz-1))continue;
                        list.Add(new Vector2Int(gx,gz));
                    }
                if(list.Count>0)res[L.Id]=list;
            }
            return res;
        }
        private bool IsOpenWater(int gx,int gz)
        {
            if(gx<0||gx>=G||gz<0||gz>=G)return false;
            if(_landGrid[gz*G+gx]!=0)return false;
            return _w.HeightMap[gz,gx]<GameConstants.WaterLevel;
        }
        // V6.6.0 纯高度海面判定（不看圆盘预期陆地），保证桥两端之间确实隔水、端点本身为陆
        private bool SeaCell(int gx,int gz){ if(gx<0||gx>=G||gz<0||gz>=G)return false; return _w.HeightMap[gz,gx]<GameConstants.WaterLevel; }
        private bool DryLand(int gx,int gz){ if(gx<0||gx>=G||gz<0||gz>=G)return false; return _w.HeightMap[gz,gx]>=GameConstants.WaterLevel; }
        private static long PairKey(int a,int b){if(a>b)(a,b)=(b,a);return (long)a*100000+b;}

        private bool ClosestShore(List<Vector2Int> A,List<Vector2Int> B,out int ax,out int az,out int bx,out int bz,out float worldDist)
        {
            ax=az=bx=bz=0;worldDist=float.MaxValue;bool any=false;
            // V6.5.7 对完整岸线做穷举精确最近点（不再跳点），保证选出两块陆地距离最短的连接两点
            for(int i=0;i<A.Count;i++)
                for(int j=0;j<B.Count;j++)
                {
                    float dx=(A[i].x-B[j].x)*GameConstants.Tile,dz=(A[i].y-B[j].y)*_w.TZ;float d=dx*dx+dz*dz; // V7.0.6 X/Z 轴格距不同，按世界单位算真实跨距
                    if(d<worldDist){worldDist=d;ax=A[i].x;az=A[i].y;bx=B[j].x;bz=B[j].y;any=true;}
                }
            worldDist=Mathf.Sqrt(worldDist);
            return any;
        }

        // ============ 造价：随跨距与材料增长 ============
        private static Dictionary<string,int> CostOf(int tier,float L)
        {
            var c=new Dictionary<string,int>();
            switch(tier)
            {
                case 0: c["wood"]=Mathf.CeilToInt(L*1.2f); break;
                case 1: c["stone"]=Mathf.CeilToInt(L*1.5f); c["wood"]=Mathf.CeilToInt(L*0.4f); break;
                case 2: c["steel"]=Mathf.CeilToInt(L*0.6f); c["iron"]=Mathf.CeilToInt(L*0.8f); break;
                default: c["concrete"]=Mathf.CeilToInt(L*0.8f); c["steel"]=Mathf.CeilToInt(L*0.4f); break;
            }
            return c;
        }
        private bool CanAfford(Dictionary<string,int> c){foreach(var kv in c)if(S.GetRes(kv.Key)<kv.Value)return false;return true;}

        // ============ 建桥 ============
        private void BuildBridge(int ax,int az,int bx,int bz,int tier,Dictionary<string,int> cost)
        {
            // V6.6.0 最终保险：任一端点不是干燥陆地（水面）则放弃，绝不以水面为起终点
            if(!DryLand(ax,az)||!DryLand(bx,bz)){ Debug.Log("[Bridge] 端点含水，取消建桥"); return; }
            float sx=CellX(ax),sz=CellZ(az),ex=CellX(bx),ez=CellZ(bz);
            float dx=ex-sx,dz=ez-sz;float len=Mathf.Sqrt(dx*dx+dz*dz);
            float yaw=Mathf.Atan2(dx,dz)*Mathf.Rad2Deg;
            int landA=LandOfCell(ax,az),landB=LandOfCell(bx,bz);
            var marked=new List<int>();
            int steps=Mathf.CeilToInt(len/(GameConstants.Tile*0.5f));
            for(int k=0;k<=steps;k++)
            {
                float t=steps==0?0f:(float)k/steps;
                float wx=sx+dx*t,wz=sz+dz*t;
                int gx=_w.W2CX(wx),gz=_w.W2CZ(wz);
                if(gx<0||gx>=G||gz<0||gz>=G)continue;
                int lid=LandOfCell(gx,gz);
                if(lid>0){ if(lid!=landA&&lid!=landB){Rollback(marked);return;} continue; }
                // V9.1.1 桥只能跨真海：中段若为淡水（内陆湖/河）则不建，杜绝“陆地上的大桥 / 跨湖桥”
                if(_w.BiomeAt(wx,wz)==BiomeKind.FreshWater){Rollback(marked);return;}
                int idx=Idx(gx,gz);
                if(S.BridgeCells.Add(idx)){marked.Add(idx);_cellTier[idx]=tier;}
            }
            if(marked.Count==0)return;
            foreach(var kv in cost) S.AddRes(kv.Key,-kv.Value);
            int spanI=Mathf.RoundToInt(len);
            bool grand=len>GrandSpan;
            int birth=S.Year;
            int life=grand?GrandLifeMin+_rng.Next(GrandLifeMax-GrandLifeMin+1)
                          :NormalLifeMin+_rng.Next(NormalLifeMax-NormalLifeMin+1);
            S.BridgeRuns.Add(ax);S.BridgeRuns.Add(az);S.BridgeRuns.Add(bx);S.BridgeRuns.Add(bz);
            S.BridgeRuns.Add(tier);S.BridgeRuns.Add(spanI);S.BridgeRuns.Add(birth);S.BridgeRuns.Add(life);
            foreach(var idx in marked) DeckView(idx,yaw,tier);
            var cs=new System.Text.StringBuilder();foreach(var kv in cost){if(cs.Length>0)cs.Append('、');cs.Append(kv.Value).Append(ResName(kv.Key));}
            string kind=grand?"跨海大桥":"桥梁";
            GM.AddEvent("good",$"🌉 建成{TName[tier]}{kind}，连接两块陆地（跨距{len:0}单位，寿命{life}年，耗{cs}）");
        }
        private void Rollback(List<int> marked){foreach(var i in marked){S.BridgeCells.Remove(i);_cellTier.Remove(i);}}
        private static string ResName(string k)=>k switch{"wood"=>"木","stone"=>"石","iron"=>"铁","steel"=>"钢","concrete"=>"水泥",_=>k};

        private float CellX(int gx)=>_w.C2WX(gx)+GameConstants.Tile*0.5f;
        private float CellZ(int gz)=>_w.C2WZ(gz)+_w.TZ*0.5f;

        // ============ 车辆通行查询 ============
        public bool IsBridgeAt(float wx,float wz)
        {
            int gx=_w.W2CX(wx),gz=_w.W2CZ(wz);
            if(gx<0||gx>=G||gz<0||gz>=G)return false;
            return S.BridgeCells.Contains(Idx(gx,gz));
        }
        public float DeckHeightAt(float wx,float wz)
        {
            int gx=_w.W2CX(wx),gz=_w.W2CZ(wz);
            if(!_cellTier.TryGetValue(Idx(gx,gz),out var t))return 0f;
            return t==4?ViaductDeckY:DeckY[t];
        }

        // ============ 视图：随技术档改变结构 ============
        private void DeckView(int idx,float yaw,int tier)
        {
            int gx=idx%G,gz=idx/G;
            var cell=new GameObject($"Bridge_{tier}_{gx}_{gz}");
            cell.transform.SetParent(_root);
            // V9.5.4 修复：DeckY 仅 4 元素（0..3），高架档 tier=4 越界抛 IndexOutOfRangeException
            // → BuildViaduct/读档 RebuildViews 中断，主地图错乱&闪烁。高架档取 ViaductDeckY=8。
            float dy = tier==4 ? ViaductDeckY : DeckY[tier];
            cell.transform.position=new Vector3(CellX(gx),dy,CellZ(gz));
            cell.transform.rotation=Quaternion.Euler(0,yaw,0);
            // V9.5.4 修复：tier==4（高架档）时 Width/DeckMat 仅 4 元素，直接索引越界抛 IndexOutOfRangeException，
            // 建高架桥面中断（BridgeCells/BridgeRuns 已写入而视图缺失）→ 主地图错乱&闪烁（每 5 秒重试反复抛异常）。
            float w  = tier==4 ? 6f : Width[tier];
            Material dm = tier==4 ? _viaductMat : _deckMat[tier];
            Part(cell,PrimitiveType.Cube,new Vector3(0,0,0),new Vector3(w,0.4f,GameConstants.Tile*1.02f),dm);
            if(tier==0)
            {
                Part(cell,PrimitiveType.Cube,new Vector3(-w/2,0.45f,0),new Vector3(0.18f,0.9f,0.18f),_deckMat[0]);
                Part(cell,PrimitiveType.Cube,new Vector3( w/2,0.45f,0),new Vector3(0.18f,0.9f,0.18f),_deckMat[0]);
            }
            else if(tier==1)
            {
                Part(cell,PrimitiveType.Cylinder,new Vector3(0,-1.0f,0),new Vector3(0.9f,2.0f,0.9f),_deckMat[1]);
            }
            else if(tier==2)
            {
                Part(cell,PrimitiveType.Cube,new Vector3(-w/2,0.8f,0),new Vector3(0.12f,1.6f,0.12f),_deckMat[2]);
                Part(cell,PrimitiveType.Cube,new Vector3( w/2,0.8f,0),new Vector3(0.12f,1.6f,0.12f),_deckMat[2]);
                Part(cell,PrimitiveType.Cube,new Vector3(0,1.55f,0),new Vector3(w,0.1f,0.1f),_deckMat[2]);
            }
            else if(tier==3)
            {
                Part(cell,PrimitiveType.Cube,new Vector3(0,0.21f,0),new Vector3(0.3f,0.04f,GameConstants.Tile),ShaderHelper.Mat(new Color(0.95f,0.82f,0.3f)));
                if(((gx+gz)&3)==0)
                {
                    Part(cell,PrimitiveType.Cylinder,new Vector3(-w/2,0.9f,0),new Vector3(0.12f,1.8f,0.12f),ShaderHelper.Mat(new Color(0.3f,0.3f,0.32f)));
                    Part(cell,PrimitiveType.Sphere,new Vector3(-w/2,1.85f,0),Vector3.one*0.22f,ShaderHelper.Emissive(new Color(1f,0.95f,0.7f),new Color(1f,0.9f,0.5f)));
                }
            }
            // V9.4.6 高架档（tier=4）：统一净空 ViaductDeckY=8，宽6 双向车道+护栏+钢架
            else if(tier==4)
            {
                float w6=6f;
                Part(cell,PrimitiveType.Cube,new Vector3(0,0,0),new Vector3(w6,0.5f,GameConstants.Tile*1.02f),_viaductMat);
                Part(cell,PrimitiveType.Cube,new Vector3(-w6/2,0.5f,0),new Vector3(0.14f,0.6f,GameConstants.Tile),_viaductMat);
                Part(cell,PrimitiveType.Cube,new Vector3( w6/2,0.5f,0),new Vector3(0.14f,0.6f,GameConstants.Tile),_viaductMat);
                // V9.5.4 修复：删除内置圆柱支撑柱——旧实现与玩家柱位 PierView 方柱双重重叠，
                // 柱底固定 y=0 在水面/低地穿地，造成"闪烁"。支撑柱统一由 PierView 提供。
            }
        }
        private static GameObject Part(GameObject parent,PrimitiveType t,Vector3 localPos,Vector3 scale,Material mat)
        {
            var p=GameObject.CreatePrimitive(t);
            Object.Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(parent.transform,false);
            p.transform.localPosition=localPos;p.transform.localScale=scale;
            if(mat!=null)p.GetComponent<Renderer>().material=mat;
            return p;
        }

        /// <summary>V9.4.4 玩家单点建桥：以点击点为锚（所属陆地），找最近可配对陆地建桥；遵守跨距/配额/成本/单陆上限。V9.4.5 增加 silent 静默重试模式。</summary>
        public bool PlayerBuildBridge(float wx, float wz, bool silent=false)
        {
            if (_w==null) return false;
            EnsureLandGrid();
            int anchor=LandOfWorld(wx,wz);
            if (anchor<=0){ if(!silent) GM.AddEvent("info","桥梁必须点在岸边陆地上"); return false; }
            int tier=TierOf(S.Era);
            float maxSpan=Mathf.Min(Span[tier],HardMaxSpan);
            var occupied=new HashSet<int>();
            foreach(var b in S.Buildings){int l=LandOfWorld(b.X,b.Z);if(l>0)occupied.Add(l);}
            occupied.Add(anchor);
            var shore=CollectShores(occupied);
            if (shore.Count<2) return false;
            var incident=new Dictionary<int,int>();
            int usedNormal=0,usedGrand=0;
            for(int i=0;i+RunStride-1<S.BridgeRuns.Count;i+=RunStride)
            {
                int a=LandOfWorld(CellX(S.BridgeRuns[i]),CellZ(S.BridgeRuns[i+1]));
                int b2=LandOfWorld(CellX(S.BridgeRuns[i+2]),CellZ(S.BridgeRuns[i+3]));
                if(a>0)incident[a]=incident.TryGetValue(a,out var ia)?ia+1:1;
                if(b2>0)incident[b2]=incident.TryGetValue(b2,out var ib)?ib+1:1;
                if(S.BridgeRuns[i+5]>GrandSpan)usedGrand++;else usedNormal++;
            }
            int capNormal=Mathf.Min(NormalCapTotal,S.Year/NormalEveryYears,Mathf.Max(1,S.Pop/10));
            int capGrand =Mathf.Min(GrandCapTotal,S.Year/GrandEveryYears,Mathf.Max(1,S.Pop/50));
            var paired=new HashSet<long>();
            for(int i=0;i+RunStride-1<S.BridgeRuns.Count;i+=RunStride)
            {
                int a=LandOfWorld(CellX(S.BridgeRuns[i]),CellZ(S.BridgeRuns[i+1]));
                int b2=LandOfWorld(CellX(S.BridgeRuns[i+2]),CellZ(S.BridgeRuns[i+3]));
                if(a>0&&b2>0) paired.Add(PairKey(a,b2));
            }
            if(!shore.TryGetValue(anchor,out var A)) return false;
            int bestB=-1;int bax=0,baz=0,bbx=0,bbz=0;float bestD=float.MaxValue;
            foreach(var kv in shore)
            {
                int B=kv.Key; if(B==anchor) continue;
                if(paired.Contains(PairKey(anchor,B)))continue;
                if(!LandHasQuota(anchor,incident)||!LandHasQuota(B,incident))continue;
                if(ClosestShore(A,shore[B],out int ax,out int az,out int bx,out int bz,out float dw))
                {
                    if(dw<GameConstants.Tile*2f||dw>maxSpan) continue;
                    bool grand=dw>GrandSpan;
                    if(grand){if(usedGrand>=capGrand)continue;}else if(usedNormal>=capNormal)continue;
                    var c=CostOf(tier,dw);
                    if(!CanAfford(c)) continue;
                    if(dw<bestD){bestD=dw;bestB=B;bax=ax;baz=az;bbx=bx;bbz=bz;}
                }
            }
            if(bestB<0)
            {
                if(!silent) GM.AddEvent("info","该处无法建桥：跨距超技术上限/额度不足/资源不足/单陆接桥达上限");
                return false;
            }
            BuildBridge(bax,baz,bbx,bbz,tier,CostOf(tier,bestD));
            GM.AddEvent("info",$"🌉 玩家建桥：陆{anchor}↔陆{bestB} 跨距{bestD:0}（{(bestD>GrandSpan?"跨海大桥":"普通桥")}）tier{tier}");
            return true;
        }

        // ============ V9.4.6 高架桥：玩家手动建柱 → 系统自动与最近柱连片布设高架桥面 ============
        /// <summary>放置高架柱：era≥6(1949) 解锁；成本 钢80 混凝土40；柱位吸附格中心（与桥面严格对齐）；
        /// 地形高度>6.5 拒绝立柱（杜绝柱顶穿出桥面/柱立山巅的视觉错乱）；同一格不重复；自动寻找最近已建柱（≤60）生成高架桥面（无缝连片）。</summary>
        public bool PlayerBuildPier(float wx, float wz, bool silent=false)
        {
            if(_w==null) return false;
            if(S.Era<6){ if(!silent) GM.AddEvent("info","高架桥需 1949 年（新中国·现代工程）后解锁"); return false; }
            // V9.5.4 吸附格中心：柱位=桥面格中心，杜绝柱桥错位
            (float px,float pz)=SnapCell(wx,wz);
            // 防重复：距已有柱 < 4 世界单位视为同一柱位
            for(int i=0;i+1<S.Piers.Count;i+=2)
                if(Mathf.Abs(S.Piers[i]-px)<4f && Mathf.Abs(S.Piers[i+1]-pz)<4f) return true;
            // V9.5.4 地形高度校验：柱顶净空 8，地形高于 6.5（高原/山）柱会穿出/支撑不足，拒绝立柱
            float ground=_w.HeightAt(px,pz);
            if(ground>ViaductDeckY-1.5f)
            { if(!silent) GM.AddEvent("bad","该处地形过高（海拔"+ground.ToString("0.0")+"），无法架设高架柱，请选海边/低地"); return false; }
            var cost=new Dictionary<string,int>{["steel"]=80,["concrete"]=40};
            if(!CanAfford(cost)){ if(!silent) GM.AddEvent("info","资源不足：高架柱需 钢80 混凝土40"); return false; }
            // 找最近已建柱（≤ViaductMaxSpan 60）自动连片
            int gx=_w.W2CX(px),gz=_w.W2CZ(pz);
            if(gx<0||gx>=G||gz<0||gz>=G){ if(!silent) GM.AddEvent("info","高架柱超出地图范围"); return false; }
            int bestI=-1; float bestD=ViaductMaxSpan;
            for(int i=0;i+1<S.Piers.Count;i+=2)
            {
                float dx=S.Piers[i]-px,dz=S.Piers[i+1]-pz;
                float d=Mathf.Sqrt(dx*dx+dz*dz);
                if(d<bestD){ bestD=d; bestI=i; }
            }
            foreach(var kv in cost) S.AddRes(kv.Key,-kv.Value);
            S.Piers.Add(px); S.Piers.Add(pz);
            PierView(px,pz);
            if(bestI>=0)
            {
                int ax=_w.W2CX(S.Piers[bestI]),az=_w.W2CZ(S.Piers[bestI+1]);
                if(!BuildViaduct(ax,az,gx,gz))
                {
                    // V9.5.4 连片失败（中段被高地阻断）：回滚本柱，退资源，下次点击重试
                    S.Piers.RemoveAt(S.Piers.Count-2); S.Piers.RemoveAt(S.Piers.Count-1);
                    S.AddRes("steel",80); S.AddRes("concrete",40);
                    if(silent){ Object.DestroyImmediate(_root.Find($"ViaductPier_{px:0}_{pz:0}")?.gameObject); }
                    if(!silent) GM.AddEvent("bad","高架路线被高地阻断，请调整柱位（沿低地/海边布设）");
                    return false;
                }
                GM.AddEvent("good",$"🏗️ 高架柱就位并自动连片：与相邻柱生成高架桥面（跨距{bestD:0}单位，净空{ViaductDeckY:0}）");
            }
            else if(!silent)
                GM.AddEvent("info","高架柱已立：再放置一根相邻柱（≤60单位）将自动无缝连片布设高架桥面");
            return true;
        }

        /// <summary>两柱间生成高架桥面段：桥面格进 BridgeCells（车辆通行）+ BridgeRuns 记录（tier=4 高架档，寿命100~150年）。
        /// V9.5.4 中段途经高地（海拔≥7，山/高原）整段回滚拒绝，杜绝桥面穿山穿高原。</summary>
        private bool BuildViaduct(int ax,int az,int bx,int bz)
        {
            float sx=CellX(ax),sz=CellZ(az),ex=CellX(bx),ez=CellZ(bz);
            float dx=ex-sx,dz=ez-sz;float len=Mathf.Sqrt(dx*dx+dz*dz);
            float yaw=Mathf.Atan2(dx,dz)*Mathf.Rad2Deg;
            var marked=new List<int>();
            int steps=Mathf.CeilToInt(len/(GameConstants.Tile*0.5f));
            for(int k=0;k<=steps;k++)
            {
                float t=steps==0?0f:(float)k/steps;
                float wx=sx+dx*t,wz=sz+dz*t;
                int gx2=_w.W2CX(wx),gz2=_w.W2CZ(wz);
                if(gx2<0||gx2>=G||gz2<0||gz2>=G)continue;
                // V9.5.4 地形冲突：中段格海拔 ≥ ViaductDeckY-1（7）则桥面将穿山/穿高原，整段拒绝
                if(_w.HeightMap[gz2,gx2]>=ViaductDeckY-1f){ Rollback(marked); return false; }
                int idx=Idx(gx2,gz2);
                if(S.BridgeCells.Add(idx)){ marked.Add(idx); _cellTier[idx]=4; }
            }
            if(marked.Count==0)return false;
            int spanI=Mathf.RoundToInt(len);
            int birth=S.Year;
            int life=ViaductLifeMin+_rng.Next(ViaductLifeMax-ViaductLifeMin+1);
            S.BridgeRuns.Add(ax);S.BridgeRuns.Add(az);S.BridgeRuns.Add(bx);S.BridgeRuns.Add(bz);
            S.BridgeRuns.Add(4);S.BridgeRuns.Add(spanI);S.BridgeRuns.Add(birth);S.BridgeRuns.Add(life);
            foreach(var idx in marked) DeckView(idx,yaw,4);
            return true;
        }

        /// <summary>高架柱视觉：混凝土方柱，柱位吸附格中心，柱顶与高架桥面净空对齐（V9.5.4 吸附兼容旧存档任意点柱位）</summary>
        private void PierView(float wx,float wz)
        {
            (float px,float pz)=SnapCell(wx,wz);
            float groundY=_w.HeightAt(px,pz);
            float h=ViaductDeckY-groundY; if(h<3f)h=3f;
            var pier=new GameObject($"ViaductPier_{px:0}_{pz:0}");
            pier.transform.SetParent(_root);
            pier.transform.position=new Vector3(px,groundY+h*0.5f,pz);
            Part(pier,PrimitiveType.Cube,Vector3.zero,new Vector3(1.6f,h,1.6f),_viaductMat);
            Part(pier,PrimitiveType.Cube,new Vector3(0,h*0.5f,0),new Vector3(2.4f,0.4f,2.4f),_viaductMat);
        }

        /// <summary>Debug：无视跨距与资源，为最近两块有据点陆地建当前时代桥（回归验证用）</summary>
        public bool ForceNearest()
        {
            if(_w==null)return false;
            EnsureLandGrid();
            int tier=TierOf(S.Era);
            var occ=new HashSet<int>();
            foreach(var bld in S.Buildings){int l=LandOfWorld(bld.X,bld.Z);if(l>0)occ.Add(l);}
            var shore=CollectShores(occ);var ids=new List<int>(shore.Keys);
            int ba=-1,bb=-1;int ax=0,az=0,bx=0,bz=0;float bd=float.MaxValue;
            for(int i=0;i<ids.Count;i++)for(int j=i+1;j<ids.Count;j++)
                if(ClosestShore(shore[ids[i]],shore[ids[j]],out var x1,out var z1,out var x2,out var z2,out var dw)&&dw<bd)
                {bd=dw;ba=ids[i];bb=ids[j];ax=x1;az=z1;bx=x2;bz=z2;}
            if(ba<0){Debug.Log("[Bridge] ForceNearest 找不到两块有据点陆地");return false;}
            BuildBridge(ax,az,bx,bz,tier,CostOf(tier,bd));
            Debug.Log($"[Bridge] ForceNearest 已连接 陆{ba}-陆{bb} 跨距{bd:0} tier{tier}");
            return true;
        }

        /// <summary>浏览器自证：打印有据点陆地两两最短岸线跨距 vs 当前时代技术跨距</summary>
        public void DebugProbe()
        {
            if(_w==null){Debug.Log("[Bridge] world null");return;}
            EnsureLandGrid();
            var lm=new System.Text.StringBuilder("[Bridge] LANDS ");
            foreach(var L in _w.Landmasses) lm.Append($"#{L.Id}(k{L.Kind},{L.Cx:0},{L.Cz:0},r{L.Br:0}) ");
            Debug.Log(lm.ToString());
            var vc=new System.Text.StringBuilder("[Bridge] VILLAGES ");
            for(int i=0;i<S.VillageX.Count;i++)
            {
                float vx=S.VillageX[i],vz=S.VillageZ[i];
                vc.Append($"[{i}] comp={_w.ContinentAt(vx,vz)} disk={LandOfWorld(vx,vz)} ({vx:0},{vz:0}) ");
            }
            Debug.Log(vc.ToString());
            var hist=new Dictionary<int,int>();
            foreach(var b in S.Buildings){int l=LandOfWorld(b.X,b.Z);hist[l]=hist.TryGetValue(l,out var hh)?hh+1:1;}
            var hh2=new System.Text.StringBuilder("[Bridge] BLD-DISK ");
            foreach(var kv in hist)hh2.Append($"land{kv.Key}:{kv.Value} ");
            Debug.Log(hh2.ToString());
            int tier=TierOf(S.Era);
            var occ=new HashSet<int>();
            foreach(var bld in S.Buildings){int l=LandOfWorld(bld.X,bld.Z);if(l>0)occ.Add(l);}
            var shore=CollectShores(occ);
            var ids=new List<int>(shore.Keys);
            var sb=new System.Text.StringBuilder();
            sb.Append($"[Bridge] era={S.Era} tier{tier}({TName[tier]}) 技术跨距≤{Mathf.Min(Span[tier],HardMaxSpan):0} 已建桥={S.BridgeRuns.Count/RunStride} 年{S.Year}(普通额{Mathf.Min(NormalCapTotal,S.Year/NormalEveryYears)}/跨海额{Mathf.Min(GrandCapTotal,S.Year/GrandEveryYears)}) 有据点陆地={ids.Count}");
            for(int i=0;i<ids.Count;i++)for(int j=i+1;j<ids.Count;j++)
                if(ClosestShore(shore[ids[i]],shore[ids[j]],out _,out _,out _,out _,out var dw))
                    sb.Append($" | 陆{ids[i]}-陆{ids[j]} 跨距{dw:0}"+(dw<=Span[tier]?"[可建]":"[跨距不足]"));
            Debug.Log(sb.ToString());
        }

        /// <summary>V9.7.1 浏览器回归：高架桥完整生命周期实证（建柱连片→老化拆除→桥面格/废弃柱同步清理）。
        /// 判定标准：连片成功后 tier=4 桥 run 增加、BridgeCells 出现桥面格；老化触发后该 run 被拆除、
        /// 仅属于该桥的桥面格清除、仅连接该桥的废弃柱被 SyncPiers 删除（待连片柱保留）。</summary>
        [UnityEngine.Scripting.Preserve]
        public void WebViaductLifecycleTest()
        {
            if(_w==null){Debug.Log("[VCT] world null");return;}
            EnsureLandGrid();
            // 保证立柱成本（钢80 混凝土40/柱，两柱共 钢160 混凝土80）
            S.AddRes("steel",5000); S.AddRes("concrete",5000);
            if(S.Era<6) S.Era=6;
            int piersBefore=S.Piers.Count/2;
            int cellsBefore=S.BridgeCells.Count;
            int runsBefore=S.BridgeRuns.Count/RunStride;
            // 1) 第一根柱：扫描世界网格找低地/海面格
            float fx=0,fz=0; bool first=false;
            for(float wz=-600;wz<=600&&!first;wz+=24)
                for(float wx=-600;wx<=600&&!first;wx+=24)
                {
                    if(_w.HeightAt(wx,wz)>ViaductDeckY-1.5f)continue;
                    if(PlayerBuildPier(wx,wz,true)){fx=wx;fz=wz;first=true;}
                }
            // 2) 第二根柱：在第一根半径 20~55 环内找可连片点
            bool second=false; float sx2=0,sz2=0;
            if(first)
            {
                for(float wz=fz-60;wz<=fz+60&&!second;wz+=12)
                    for(float wx=fx-60;wx<=fx+60&&!second;wx+=12)
                    {
                        float d=Mathf.Sqrt((wx-fx)*(wx-fx)+(wz-fz)*(wz-fz));
                        if(d<20||d>55)continue;
                        if(_w.HeightAt(wx,wz)>ViaductDeckY-1.5f)continue;
                        // 与第一根柱相邻连片
                        if(PlayerBuildPier(wx,wz,true)){second=true;sx2=wx;sz2=wz;}
                    }
            }
            int piersAfter=S.Piers.Count/2, cellsAfter=S.BridgeCells.Count, runsAfter=S.BridgeRuns.Count/RunStride;
            Debug.Log($"[VCT] BUILD first={first} second={second} | piers {piersBefore}->{piersAfter} | cells {cellsBefore}->{cellsAfter} | runs {runsBefore}->{runsAfter}");
            if(!second)
            {
                Debug.Log("[VCT] RESULT: FAIL 高架桥未连片（无法进入老化阶段）");
                return;
            }
            // 3) 令全部 tier=4 高架 run 到期（birth=今年-200，life=100），触发 DecaySweep
            int piersMid=S.Piers.Count/2, cellsMid=S.BridgeCells.Count, runsMid=S.BridgeRuns.Count/RunStride;
            for(int i=0;i+RunStride-1<S.BridgeRuns.Count;i+=RunStride)
                if(S.BridgeRuns[i+4]==4){ S.BridgeRuns[i+6]=S.Year-200; S.BridgeRuns[i+7]=100; }
            DecaySweep(S.Year);
            int piersEnd=S.Piers.Count/2, cellsEnd=S.BridgeCells.Count, runsEnd=S.BridgeRuns.Count/RunStride;
            bool runsRemoved = runsEnd<runsMid;
            bool cellsCleared = cellsEnd<cellsMid;
            bool piersRemoved = piersEnd<piersMid;
            Debug.Log($"[VCT] DECAY piers {piersMid}->{piersEnd} | cells {cellsMid}->{cellsEnd} | runs {runsMid}->{runsEnd}");
            bool ok = runsRemoved && cellsCleared && piersRemoved;
            Debug.Log($"[VCT] RESULT: "+(ok?"PASS-桥面格/废弃柱随老化同步清理（地图错乱根因已消除）":"FAIL-清理不完整")+
                      $" | runsRemoved={runsRemoved} cellsCleared={cellsCleared} piersRemoved={piersRemoved}");
        }

        /// <summary>读档后按 S.BridgeRuns / BridgeCells 重建全部桥体</summary>
        /// <param name="oldConnectedPiers">V9.7.1 寿命拆除前的高架桥面端点集合（用于识别待拆除废弃柱）；null（读档/普通重建）时柱位全部保留。</param>
        public void RebuildViews(List<(float x,float z)> oldConnectedPiers = null)
        {
            if(_w==null)return;
            EnsureLandGrid();
            // V9.5.4 修复"闪烁"根因：Object.Destroy 延迟到帧末，与同帧新建的同名桥体重叠 → z-fighting 闪烁；
            // 桥体/柱均为运行时程序化生成的独立 GameObject（无序列化引用），DestroyImmediate 立即销毁安全。
            for(int i=_root.childCount-1;i>=0;i--)Object.DestroyImmediate(_root.GetChild(i).gameObject);
            _cellTier.Clear();
            S.BridgeCells.Clear();   // V9.7.1 桥面格为派生数据：先清空再按保留桥重算，寿命拆除后旧格不残留（防车辆悬空、地图错乱）
            for(int r=0;r+RunStride-1<S.BridgeRuns.Count;r+=RunStride)
            {
                int ax=S.BridgeRuns[r],az=S.BridgeRuns[r+1],bx=S.BridgeRuns[r+2],bz=S.BridgeRuns[r+3],tier=S.BridgeRuns[r+4];
                float sx=CellX(ax),sz=CellZ(az),dx=CellX(bx)-sx,dz=CellZ(bz)-sz;
                float yaw=Mathf.Atan2(dx,dz)*Mathf.Rad2Deg;
                int steps=Mathf.CeilToInt(Mathf.Sqrt(dx*dx+dz*dz)/(GameConstants.Tile*0.5f));
                for(int k=0;k<=steps;k++)
                {
                    float tt=steps==0?0f:(float)k/steps;
                    float wx=sx+dx*tt,wz=sz+dz*tt;
                    int gx=_w.W2CX(wx),gz=_w.W2CZ(wz);
                    if(gx<0||gx>=G||gz<0||gz>=G)continue;
                    // V9.5.4 修复高架跨陆段"老化重建后断裂"：普通桥中段成陆应消失（桥本不该在陆上）；
                    // 高架桥(tier=4)跨陆是设计内（柱可立低地、桥面高架过陆），陆地格照常建格不断裂。
                    if(tier!=4 && LandOfCell(gx,gz)>0)continue;
                    int idx=Idx(gx,gz);
                    S.BridgeCells.Add(idx);
                    if(!_cellTier.ContainsKey(idx)){_cellTier[idx]=tier;DeckView(idx,yaw,tier);}
                }
            }
            // V9.7.1 桥墩同步：保留桥面端点柱 + 待连片柱，移除已拆除桥面的废弃柱
            SyncPiers(oldConnectedPiers);
            for(int i=0;i+1<S.Piers.Count;i+=2) PierView(S.Piers[i],S.Piers[i+1]);
        }

        /// <summary>V9.7.1 按保留高架桥面端点重算 S.Piers：
        /// 柱仍在某条保留高架桥面端点 → 保留（多段桥面共享柱）；
        /// 柱不在任何桥面端点、且拆除前也不在（玩家刚立的待连片柱）→ 保留；
        /// 柱是被拆除桥面的端点、且不被任何保留桥面使用 → 废弃删除。</summary>
        private void SyncPiers(List<(float x,float z)> oldConnectedPiers)
        {
            if(S.Piers.Count==0)return;
            var connected=new List<(float x,float z)>();
            for(int r=0;r+RunStride-1<S.BridgeRuns.Count;r+=RunStride)
                if(S.BridgeRuns[r+4]==4)
                {
                    connected.Add((CellX(S.BridgeRuns[r]),CellZ(S.BridgeRuns[r+1])));
                    connected.Add((CellX(S.BridgeRuns[r+2]),CellZ(S.BridgeRuns[r+3])));
                }
            var oldConn = oldConnectedPiers ?? connected;
            bool Near(float x,float z,List<(float x,float z)> list) =>
                list.Any(p => Mathf.Abs(p.x-x)<4f && Mathf.Abs(p.z-z)<4f);
            var next=new List<float>();
            for(int i=0;i+1<S.Piers.Count;i+=2)
            {
                float px=S.Piers[i],pz=S.Piers[i+1];
                if(Near(px,pz,connected)){ next.Add(px); next.Add(pz); continue; }
                if(!Near(px,pz,oldConn)){ next.Add(px); next.Add(pz); }   // 待连片柱保留
                // else: 所属桥面已拆除且无保留桥面使用 → 废弃柱不加入
            }
            S.Piers=next;
        }
    }
}
