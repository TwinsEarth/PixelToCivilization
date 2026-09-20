using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Data;

namespace PixelToCivilization.World
{
    /// <summary>
    /// V9.1.0 真实地球模式（WorldGenerator 分部类）：
    /// 手绘七大洲+主要岛屿多边形 → 等距圆柱投影栅格化 → 纬度气候带着色（雨林/草原/沙漠/温带/寒带/极地冰雪）
    /// → 真实山脉折线峰带/高原/大河/湖泊 → 海岸沙滩与浅海 → 家园固定长江下游 → 全球一次性揭示。
    /// 与经典随机模式互斥：地球模式停用比例硬门控与年代增陆（大陆轮廓固定，不再随机扩张）。
    /// 经典模式冻结规则在地球模式的例外（已在 V910 策划裁决中书面说明）：山脉允许出现在大陆上；
    /// 陆地占比对齐真实地球（约 29%，海约 71%），不套用经典「主大陆≤10%/山只在无人岛」口径。
    /// </summary>
    public partial class WorldGenerator
    {
        /// <summary>V9.1.0 当前地形是否为真实地球（地球生成路径置 true，经典 Regenerate 置 false）</summary>
        public bool EarthMode;

        const float EarthHomeLon = 118.2f, EarthHomeLat = 31.2f;   // 长江下游（南京—上海间），贴长江保证淡水

        /// <summary>重新生成真实地球（先销毁旧地形/水面），返回家园村址</summary>
        public Vector3 RegenerateEarth(int seed)
        {
            var oldT = transform.Find("Terrain"); if (oldT!=null) Destroy(oldT.gameObject);
            var oldW = transform.Find("Water");   if (oldW!=null) Destroy(oldW.gameObject);
            GenerateEarth(seed);
            return SettlementCenter;
        }

        public void GenerateEarth(int seed)
        {
            Seed = seed; EarthMode = true;
            int n = G;
            HeightMap = new float[n,n];
            WaterMap = new float[n,n];
            Biome = new BiomeKind[n,n];
            ContinentMap = new int[n,n];
            OceanMask = new byte[n,n];
            _peaks.Clear(); _deserts.Clear(); _lands.Clear(); GrownLands.Clear(); _plateaus.Clear();
            ContinentCenters.Clear(); _revealedLands.Clear();

            var rng = new System.Random(seed*31+7);

            // ① 初始全深海
            for (int z=0;z<n;z++) for (int x=0;x<n;x++) HeightMap[z,x] = -2.8f;

            // ② 大洲/岛屿多边形扫描线栅格化（ContinentMap 直接赋固定 Id，不做连通分量重映射）
            foreach (var L in EarthMapData.Lands) RasterizeEarthPolygon(L, n);

            // ②.5 关键海峡拓宽到至少 1~2 格：栅格分辨率 0.3°，直布罗陀/多佛尔等真实窄海峡会被封成内水，
            //      必须人工开海门，保证地中海/黑海/红海/波斯湾为外海（可通航、鱼群与海军可进入）
            CarveSeaGate(-5.6f,35.95f,0.62f,n);   // 直布罗陀海峡
            CarveSeaGate(28.9f,41.1f,0.56f,n);    // 博斯普鲁斯海峡（黑海↔地中海）
            CarveSeaGate(26.4f,40.1f,0.8f,n);     // 达达尼尔海峡（马尔马拉↔爱琴海）
            CarveSeaGate(43.5f,12.7f,0.56f,n);    // 曼德海峡（红海↔亚丁湾）
            CarveSeaGate(56.4f,26.5f,0.52f,n);    // 霍尔木兹海峡（波斯湾）
            CarveSeaGate(1.4f,50.9f,0.52f,n);     // 多佛尔海峡（不列颠↔欧洲）
            CarveSeaGate(-5.9f,55.2f,0.52f,n);    // 北海峡（爱尔兰↔不列颠）
            CarveSeaGate(5.8f,51.9f,0.6f,n);      // 圣乔治海峡（爱尔兰海南口）
            CarveSeaGate(80.1f,9.3f,0.52f,n);     // 保克海峡（斯里兰卡↔印度）
            CarveSeaGate(11f,54.8f,0.9f,n);       // 丹麦海峡（北海→波罗的海）
            CarveSeaGate(-81.5f,24.2f,1.2f,n);    // 佛罗里达海峡（墨西哥湾→大西洋）
            CarveSeaGate(-86.5f,21.8f,1.2f,n);    // 尤卡坦海峡（墨西哥湾→加勒比）
            CarveSeaGate(129.3f,34.2f,0.8f,n);    // 对马海峡（东海→日本海）
            CarveSeaGate(140.8f,41.5f,0.6f,n);    // 津轻海峡（日本海→太平洋）
            CarveSeaGate(100.2f,1.6f,0.6f,n);     // 马六甲海峡

            // ②.6 内海/海湾椭圆挖凿（粗糙大陆多边形会把这些海面桥接成陆地）
            CarveSeaEllipseDeg(34.5f,44f,7.5f,3.8f,n);    // 黑海
            CarveSeaEllipseDeg(38f,46f,2.4f,1.3f,n);      // 亚速海
            CarveSeaEllipseDeg(28.2f,40.6f,2.6f,1.1f,n);  // 马尔马拉海（含博斯普鲁斯/达达尼尔水道）
            CarveSeaEllipseDeg(24.5f,38.8f,3.2f,2.2f,n);  // 爱琴海（粗糙多边形桥接处）
            CarveSeaEllipseDeg(19.5f,58f,5.8f,4f,n);      // 波罗的海
            CarveSeaEllipseDeg(22f,62.5f,2.4f,3.6f,n);    // 波的尼亚湾
            CarveSeaEllipseDeg(26.5f,60.2f,2.8f,3f,n);    // 芬兰湾
            CarveSeaEllipseDeg(2.5f,56f,8f,3.6f,n);       // 北海
            CarveSeaEllipseDeg(11.5f,56.5f,2.5f,2f,n);    // 卡特加特海峡
            CarveSeaEllipseDeg(-5.6f,53.3f,1.8f,2.6f,n);  // 爱尔兰海
            CarveSeaEllipseDeg(-86f,59f,8f,6f,n);         // 哈得孙湾
            CarveSeaEllipseDeg(-92f,24.5f,8f,4.5f,n);     // 墨西哥湾
            CarveSeaEllipseDeg(-80f,16f,10f,7f,n);        // 加勒比海
            CarveSeaEllipseDeg(135f,40f,6f,6.5f,n);       // 日本海
            CarveSeaEllipseDeg(130.5f,34.5f,2.5f,1.2f,n); // 朝鲜海峡
            CarveSeaEllipseDeg(149f,54f,6.5f,5f,n);       // 鄂霍次克海
            CarveSeaEllipseDeg(88f,12f,9f,9f,n);          // 孟加拉湾
            CarveSeaEllipseDeg(102f,8f,3.5f,3f,n);        // 泰国湾
            CarveSeaEllipseDeg(139f,-15f,4f,3f,n);        // 卡奔塔利亚湾
            // 线形狭海（红海/波斯湾/加利福尼亚湾）
            CarveSeaChannelDeg(new[]{33.8f,35.5f,37.5f,39.5f,41.5f,43.3f},
                               new[]{29.5f,27f,23.5f,20f,16f,12.8f},1.0f,n); // 红海
            CarveSeaChannelDeg(new[]{47.8f,49f,50.8f,53f,55.2f,56.4f,57.6f,59f,60.5f},
                               new[]{30.4f,28.8f,27.6f,26.8f,26.5f,26.4f,25.6f,24f,22.5f},0.9f,n); // 波斯湾→阿曼湾
            CarveSeaChannelDeg(new[]{-114.7f,-113.2f,-111.6f,-110f},
                               new[]{31.8f,29f,26f,23.2f},0.7f,n); // 加利福尼亚湾

            // ③ 陆地基底高度（极地压低为冰原），叠加 FBM 起伏
            for (int z=0;z<n;z++) for (int x=0;x<n;x++)
            {
                if (ContinentMap[z,x]<=0) continue;
                float lat = EarthMapData.ZToLat(C2WZ(z));
                float f = TerrainPainter.FbmRidge(x,z,Seed,5);
                bool polar = Mathf.Abs(lat)>=66f || IsPolarLand(ContinentMap[z,x]);
                HeightMap[z,x] = polar ? 0.55f+f*0.35f : 1.05f+f*1.1f;
            }

            // ④ 高原（宽缓抬升，椭圆经纬度半径）
            foreach (var p in EarthMapData.Plateaus)
                EarthGaussianDeg(p.Lon,p.Lat,p.R,p.R*0.7f,p.Amount,1,true,false);

            // ⑤ 真实山脉折线峰带
            foreach (var r in EarthMapData.Ranges)
            {
                for (int i=0;i<r.Lon.Length-1;i++)
                {
                    float lon0=r.Lon[i],lat0=r.Lat[i],lon1=r.Lon[i+1],lat1=r.Lat[i+1];
                    float dlon=lon1-lon0,dlat=lat1-lat0;
                    float len=Mathf.Sqrt(dlon*dlon+dlat*dlat);
                    int steps=Mathf.Max(1,Mathf.CeilToInt(len/r.Step));
                    for (int s=0;s<=steps;s++)
                    {
                        float t=s/(float)steps;
                        float lon=lon0+dlon*t+(float)(rng.NextDouble()-0.5)*0.5f;
                        float lat=lat0+dlat*t+(float)(rng.NextDouble()-0.5)*0.5f;
                        float hh=r.H*(0.72f+(float)rng.NextDouble()*0.45f);
                        // 数据 R 为世界单位单峰半径（~7），而峰间距 Step≈1.2~1.8°（约 16~24 世界单位）；
                        // ×2.2 让相邻峰底相交形成连续山带，否则只是零星小土包（山带占地约 1~3%）
                        float rr=r.R*(0.8f+(float)rng.NextDouble()*0.45f)*3.0f;
                        float wx=EarthMapData.LonToX(lon), wz=EarthMapData.LatToZ(lat);
                        int cx=W2CX(wx),cz=W2CZ(wz);
                        int land = InBounds(cx,cz,n)?ContinentMap[cz,cx]:0;
                        EarthGaussianCell(cx,cz,rr,hh,true);
                        if(land>0) _peaks.Add((land,wx,wz,rr,hh));
                    }
                }
            }

            // ⑥ 岛屿（Kind=1 非极地）确定性少量山峰
            foreach (var L in EarthMapData.Lands)
            {
                if (L.Kind!=1 || L.Polar) continue;
                float mlon=0f,mlat=0f; for(int i=0;i<L.Lon.Length;i++){mlon+=L.Lon[i];mlat+=L.Lat[i];}
                mlon/=L.Lon.Length; mlat/=L.Lat.Length;
                var rr2=new System.Random(seed+L.Id*131+9);
                int pk=1+rr2.Next(3);
                for(int k=0;k<pk;k++)
                {
                    float lon=mlon+(float)(rr2.NextDouble()-0.5)*4f;
                    float lat=mlat+(float)(rr2.NextDouble()-0.5)*5f;
                    float wx=EarthMapData.LonToX(lon),wz=EarthMapData.LatToZ(lat);
                    EarthGaussianCell(W2CX(wx),W2CZ(wz),(5f+(float)rr2.NextDouble()*3f)*1.8f,4f+(float)rr2.NextDouble()*3f,true);
                }
            }

            // ⑦ 湖泊（高斯凹陷，仅陆地）
            foreach (var lk in EarthMapData.Lakes)
                EarthGaussianDeg(lk.Lon,lk.Lat,lk.R, lk.EllipseZ?lk.R*1.6f:lk.R, -lk.Amount,1,true,false);

            // ⑧ 大河压槽（淡水河，宽约 2.3 世界单位）
            foreach (var rv in EarthMapData.Rivers) StampEarthRiver(rv,n);

            // ⑨ 海岸沙滩两圈 + 浅海两圈
            var beach1=new bool[n,n];
            for(int z=1;z<n-1;z++)for(int x=1;x<n-1;x++)
            {
                if(ContinentMap[z,x]<=0 || HeightMap[z,x]<GameConstants.WaterLevel) continue;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dz==0)continue;
                    int nx=x+dx,nz=z+dz;
                    if(ContinentMap[nz,nx]==0 && HeightMap[nz,nx]<GameConstants.WaterLevel){beach1[z,x]=true;dx=2;dz=2;}
                }
            }
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                if(beach1[z,x]){ HeightMap[z,x]=0.07f+TerrainPainter.FbmRidge(x,z,Seed,3)*0.06f; continue; }
                if(ContinentMap[z,x]>0 && HeightMap[z,x]>0.34f)
                {
                    bool near=false;
                    for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                    { int nx=x+dx,nz=z+dz; if(nx<0||nz<0||nx>=n||nz>=n)continue;
                      if(beach1[nz,nx]){near=true;dx=2;dz=2;} }
                    if(near) HeightMap[z,x]=0.34f+TerrainPainter.FbmRidge(x,z,Seed,3)*0.08f;
                }
            }
            // 浅海：邻陆 1 圈 -1.0，2 圈 -1.8（鲜亮浅海蓝过渡）
            for(int pass=0;pass<2;pass++)
            for(int z=1;z<n-1;z++)for(int x=1;x<n-1;x++)
            {
                if(ContinentMap[z,x]>0) continue;
                bool touchLand=false, touchShallow=false;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dz==0)continue; int nx=x+dx,nz=z+dz;
                    if(nx<0||nz<0||nx>=n||nz>=n)continue;
                    if(ContinentMap[nz,nx]>0 && HeightMap[nz,nx]>=GameConstants.WaterLevel) touchLand=true;
                    if(ContinentMap[nz,nx]==0 && HeightMap[nz,nx]>-2.5f && HeightMap[nz,nx]<-0.5f) touchShallow=true;
                }
                if(pass==0 && touchLand) HeightMap[z,x]=-1.0f;
                else if(pass==1 && !touchLand && touchShallow && HeightMap[z,x]<-1.5f) HeightMap[z,x]=-1.8f;
            }

            // ⑩ 沙漠标记（不覆盖河道/水体）
            foreach (var d in EarthMapData.Deserts)
            {
                int cx=LonGX(d.Lon),cz=LatGZ(d.Lat);
                int rx=Mathf.CeilToInt(DegToWorldX(d.R)/Tile)+1, rz=Mathf.CeilToInt(DegToWorldZ(d.R)/TZ)+1;
                for(int dz=-rz;dz<=rz;dz++)for(int dx=-rx;dx<=rx;dx++)
                {
                    int x=cx+dx,z=cz+dz; if(!InBounds(x,z,n))continue;
                    if(ContinentMap[z,x]<=0) continue;
                    float wx=C2WX(x),wz=C2WZ(z);
                    float ex=(wx-EarthMapData.LonToX(d.Lon))/DegToWorldX(d.R);
                    float ez=(wz-EarthMapData.LatToZ(d.Lat))/DegToWorldZ(d.R);
                    if(ex*ex+ez*ez>1f) continue;
                    if(HeightMap[z,x]<GameConstants.WaterLevel || Biome[z,x]==BiomeKind.River) continue;
                    Biome[z,x]=BiomeKind.Desert;
                }
            }

            // ⑪ 水体落定 + 外海掩码 + 内陆淡水
            for(int z=0;z<n;z++)for(int x=0;x<n;x++) WaterMap[z,x]=HeightMap[z,x]<GameConstants.WaterLevel?1f:0f;
            ComputeOceanMask(n);
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                if(HeightMap[z,x]>=GameConstants.WaterLevel) continue;
                if(OceanMask[z,x]==0 && Biome[z,x]!=BiomeKind.River) Biome[z,x]=BiomeKind.FreshWater;
            }

            // ⑫ 各洲质心 + Landmass 包围盒（RandomPointOnContinent / 9.1.1 每洲一国依赖）
            BuildEarthLandmasses(n);

            // ⑬ 家园：长江下游，近淡水平地
            int hx=LonGX(EarthHomeLon), hz=LatGZ(EarthHomeLat);
            if(!(InBounds(hx,hz,n)&&ContinentMap[hz,hx]==1&&HeightMap[hz,hx]>=GameConstants.WaterLevel))
            {
                int bx=hx,bz=hz,best=999;
                for(int r=1;r<=18 && best>0;r++)for(int dz=-r;dz<=r;dz++)for(int dx=-r;dx<=r;dx++)
                {
                    int x=hx+dx,z=hz+dz; if(!InBounds(x,z,n))continue;
                    if(ContinentMap[z,x]==1&&HeightMap[z,x]>=GameConstants.WaterLevel&&Biome[z,x]!=BiomeKind.Desert){bx=x;bz=z;best=0;break;}
                }
                hx=bx;hz=bz;
            }
            FlattenAround(hx,hz,n);
            SettlementCenter=new Vector3(C2WX(hx),0,C2WZ(hz));
            HomeContinent=1;

            // ⑭ 全画布真实海陆统计
            ComputeEarthRatios(n);
            LandRadius=620f;   // 欧亚大陆等效半径（仅经典 10% 门控/UI 口径使用）

            // ⑮ 全球一次性揭示（地球模式无年代迷雾扩张）
            RevealBase=HalfX; Expansion=1f; RevealTarget=RevealRadius=HalfX;
            _revealedLands.Clear();
            foreach(var L in EarthMapData.Lands) _revealedLands.Add(L.Id);

            BuildTerrainMesh(n);
            BuildWater(n);
            Debug.Log($"[World/Earth] V9.1.0 真实地球 K={ContinentCount} 海{SeaRatio:P1} 纯陆{LandOnlyRatio:P1} 山{MountainRatio:P1} 沙{DesertRatio:P1} 淡水{FreshWaterRatio:P1} 家园=({EarthHomeLon},{EarthHomeLat})");
        }

        // ---------- 栅格化：逐行（纬度）扫描线偶数-奇数填充 ----------
        void RasterizeEarthPolygon(EarthLand L,int n)
        {
            int m=L.Lon.Length;
            var cross=new List<int>();
            for(int gz=0;gz<n;gz++)
            {
                float lat=EarthMapData.ZToLat(C2WZ(gz));
                cross.Clear();
                for(int i=0;i<m;i++)
                {
                    int j=(i+1)%m;
                    float la0=L.Lat[i],la1=L.Lat[j];
                    if((la0<=lat && la1>lat)||(la1<=lat && la0>lat))
                    {
                        float t=(lat-la0)/(la1-la0);
                        float lon=L.Lon[i]+(L.Lon[j]-L.Lon[i])*t;
                        cross.Add(LonGX(lon));
                    }
                }
                cross.Sort();
                for(int k=0;k+1<cross.Count;k+=2)
                {
                    int a=Mathf.Clamp(cross[k],0,n-1), b=Mathf.Clamp(cross[k+1],0,n-1);
                    for(int gx=a;gx<=b;gx++) if(ContinentMap[gz,gx]==0) ContinentMap[gz,gx]=(byte)L.Id;
                }
            }
        }

        /// <summary>在(lon,lat)半径 rDeg 内挖出海门（须在陆地基底填充之前调用，挖空格保持初始深海高）</summary>
        void CarveSeaGate(float lon,float lat,float rDeg,int n)
        {
            int cx=LonGX(lon),cz=LatGZ(lat);
            int rx=Mathf.CeilToInt(DegToWorldX(rDeg)/Tile)+1, rz=Mathf.CeilToInt(DegToWorldZ(rDeg)/TZ)+1;
            for(int dz=-rz;dz<=rz;dz++)for(int dx=-rx;dx<=rx;dx++)
            {
                int x=cx+dx,z=cz+dz; if(!InBounds(x,z,n))continue;
                float ex=(C2WX(x)-EarthMapData.LonToX(lon))/DegToWorldX(rDeg);
                float ez=(C2WZ(z)-EarthMapData.LatToZ(lat))/DegToWorldZ(rDeg);
                if(ex*ex+ez*ez<=1f){ ContinentMap[z,x]=0; HeightMap[z,x]=-1.4f; Biome[z,x]=BiomeKind.Default; }
            }
        }

        // 椭圆内海挖凿（经纬度半径，度），在陆地基底填充前执行
        void CarveSeaEllipseDeg(float lon,float lat,float rxDeg,float rzDeg,int n)
        {
            int cx=LonGX(lon),cz=LatGZ(lat);
            float rxw=DegToWorldX(rxDeg),rzw=DegToWorldZ(rzDeg);
            int rx=Mathf.CeilToInt(rxw/Tile)+1, rz=Mathf.CeilToInt(rzw/TZ)+1;
            for(int dz=-rz;dz<=rz;dz++)for(int dx=-rx;dx<=rx;dx++)
            {
                int x=cx+dx,z=cz+dz; if(!InBounds(x,z,n))continue;
                float ex=(C2WX(x)-EarthMapData.LonToX(lon))/rxw;
                float ez=(C2WZ(z)-EarthMapData.LatToZ(lat))/rzw;
                if(ex*ex+ez*ez<=1f){ ContinentMap[z,x]=0; HeightMap[z,x]=-1.4f; Biome[z,x]=BiomeKind.Default; }
            }
        }

        // 线形狭海挖凿：沿经纬度折线逐点椭圆采样
        void CarveSeaChannelDeg(float[] lons,float[] lats,float halfDeg,int n)
        {
            for(int k=0;k<lons.Length-1;k++)
                for(int s=0;s<=20;s++)
                {
                    float t=s/20f;
                    CarveSeaEllipseDeg(lons[k]+(lons[k+1]-lons[k])*t,lats[k]+(lats[k+1]-lats[k])*t,halfDeg,halfDeg,n);
                }
        }

        // ---------- 高斯抬升/凹陷（世界单位半径，仅陆地可选） ----------
        void EarthGaussianCell(int cx,int cz,float rWorld,float amount,bool landOnly)
        {
            int n=G; if(!InBounds(cx,cz,n))return;
            int rx=Mathf.CeilToInt(rWorld/Tile)+1, rz=Mathf.CeilToInt(rWorld/TZ)+1;
            float sigma=rWorld*0.46f;
            for(int dz=-rz;dz<=rz;dz++)for(int dx=-rx;dx<=rx;dx++)
            {
                int x=cx+dx,z=cz+dz; if(!InBounds(x,z,n))continue;
                if(landOnly && ContinentMap[z,x]<=0) continue;
                float wx=dx*Tile,wz=dz*TZ; float d=Mathf.Sqrt(wx*wx+wz*wz);
                if(d>rWorld) continue;
                float g=Mathf.Exp(-(d*d)/(2f*sigma*sigma));
                float nh=HeightMap[z,x]+amount*g;
                if(amount<0f) HeightMap[z,x]=Mathf.Min(HeightMap[z,x],nh);
                else HeightMap[z,x]=nh;
            }
        }
        // 经纬度圆/椭圆特征（rxDeg、rzDeg 分别为经纬半径）
        void EarthGaussianDeg(float lon,float lat,float rxDeg,float rzDeg,float amount,int pass,bool landOnly,bool ellipse)
        {
            float rw=Mathf.Max(DegToWorldX(rxDeg),DegToWorldZ(rzDeg));
            int cx=LonGX(lon),cz=LatGZ(lat);
            int rx=Mathf.CeilToInt(DegToWorldX(rxDeg)/Tile)+1, rz=Mathf.CeilToInt(DegToWorldZ(rzDeg)/TZ)+1;
            int n=G;
            for(int dz=-rz;dz<=rz;dz++)for(int dx=-rx;dx<=rx;dx++)
            {
                int x=cx+dx,z=cz+dz; if(!InBounds(x,z,n))continue;
                if(landOnly && ContinentMap[z,x]<=0) continue;
                float wx=C2WX(x),wz=C2WZ(z);
                float ex=(wx-EarthMapData.LonToX(lon))/DegToWorldX(rxDeg);
                float ez=(wz-EarthMapData.LatToZ(lat))/DegToWorldZ(rzDeg);
                float e=ex*ex+ez*ez; if(e>1f) continue;
                float g=Mathf.Exp(-e*1.6f);
                float nh=HeightMap[z,x]+amount*g;
                HeightMap[z,x] = amount<0f ? Mathf.Min(HeightMap[z,x],nh) : nh;
            }
        }

        void StampEarthRiver(EarthRiver rv,int n)
        {
            for(int i=0;i<rv.Lon.Length-1;i++)
            {
                Vector2 a=new(EarthMapData.LonToX(rv.Lon[i]),EarthMapData.LatToZ(rv.Lat[i]));
                Vector2 b=new(EarthMapData.LonToX(rv.Lon[i+1]),EarthMapData.LatToZ(rv.Lat[i+1]));
                float len=Vector2.Distance(a,b); int steps=Mathf.Max(1,Mathf.CeilToInt(len/3f));
                for(int s=0;s<=steps;s++)
                {
                    float t=s/(float)steps;
                    float wx=a.x+(b.x-a.x)*t, wz=a.y+(b.y-a.y)*t;
                    int cx=W2CX(wx),cz=W2CZ(wz);
                    for(int dz=-2;dz<=2;dz++)for(int dx=-2;dx<=2;dx++)
                    {
                        int x=cx+dx,z=cz+dz; if(!InBounds(x,z,n))continue;
                        if(ContinentMap[z,x]<=0) continue;
                        float ddx=C2WX(x)-wx,ddz=C2WZ(z)-wz;
                        if(ddx*ddx+ddz*ddz>2.4f*2.4f) continue;
                        HeightMap[z,x]=-0.5f; Biome[z,x]=BiomeKind.River;
                    }
                }
            }
        }

        void BuildEarthLandmasses(int n)
        {
            var sx=new Dictionary<int,long>(); var szm=new Dictionary<int,long>(); var sc=new Dictionary<int,int>();
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                int id=ContinentMap[z,x]; if(id<=0)continue;
                sx[id]=sx.TryGetValue(id,out var a)?a+x:x;
                szm[id]=szm.TryGetValue(id,out var b)?b+z:z;
                sc[id]=sc.TryGetValue(id,out var c)?c+1:1;
            }
            ContinentCenters.Clear(); _lands.Clear();
            int maxId=0; foreach(var L in EarthMapData.Lands) maxId=Mathf.Max(maxId,L.Id);
            ContinentCount=maxId;
            var rng=new System.Random(Seed+9091);
            for(int id=1;id<=maxId;id++)
            {
                ContinentCenters.Add(Vector3.zero);
                if(!sc.ContainsKey(id)) continue;
                EarthLand EL=null; foreach(var L0 in EarthMapData.Lands) if(L0.Id==id){EL=L0;break;}
                if(EL==null) continue;
                int cnt=sc[id];
                float cxw=(sx[id]/(float)cnt)*Tile-HalfX, czw=(szm[id]/(float)cnt)*TZ-HalfZ;
                ContinentCenters[id-1]=new Vector3(cxw,0,czw);
                int kind=EL.Kind;
                float br=20f;
                for(int i=0;i<EL.Lon.Length;i++)
                {
                    float d=Vector2.Distance(new Vector2(cxw,czw),
                        new Vector2(EarthMapData.LonToX(EL.Lon[i]),EarthMapData.LatToZ(EL.Lat[i])));
                    br=Mathf.Max(br,d*0.82f);
                }
                _lands.Add(new Landmass{Cx=cxw,Cz=czw,Br=br,Id=id,Kind=kind,
                    P1=(float)rng.NextDouble()*9f,P2=(float)rng.NextDouble()*9f,P3=(float)rng.NextDouble()*9f});
            }
        }

        void ComputeEarthRatios(int n)
        {
            long sea=0,mtn=0,desert=0,fresh=0,land=0;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float h=HeightMap[z,x];
                if(h<GameConstants.WaterLevel){ sea++; if(Biome[z,x]==BiomeKind.FreshWater||Biome[z,x]==BiomeKind.River) fresh++; }
                else { land++; if(h>=5.4f) mtn++; if(Biome[z,x]==BiomeKind.Desert) desert++; }
            }
            float tot=n*(float)n;
            SeaRatio=sea/tot; LandRatio=land/tot; LandOnlyRatio=(land-mtn)/tot;
            MountainRatio=mtn/tot; MaxMountainBlob=MountainRatio;
            DesertRatio=desert/tot; FreshWaterRatio=fresh/tot;
        }

        // ---------- 地球气候带着色 ----------
        Color EarthBiomeColor(float h,int gx,int gz)
        {
            float lon=(gx-G*0.5f)*Tile/HalfX*180f;
            float lat=(gz-G*0.5f)*TZ/EarthMapData.EquatHalfZ*90f;
            float aLat=Mathf.Abs(lat);
            if(h<GameConstants.WaterLevel)
            {
                if(gx>=0&&gz>=0&&gx<G&&gz<G&&Biome[gz,gx]==BiomeKind.River) return new Color(0.36f,0.66f,0.90f);
                Color deep=new(0.05f,0.30f,0.72f), shallow=new(0.12f,0.60f,0.92f);
                Color c=Color.Lerp(shallow,deep,Mathf.InverseLerp(GameConstants.WaterLevel,-2.6f,h));
                if(aLat>66f) c=Color.Lerp(c,new Color(0.62f,0.80f,0.92f),Mathf.Clamp01((aLat-66f)/12f)); // 冰海
                return c;
            }
            int id=(gx>=0&&gz>=0&&gx<G&&gz<G)?ContinentMap[gz,gx]:0;
            bool polarLand = aLat>=66f || IsPolarLand(id);
            if(polarLand) // 极地冰原：低处苔原白、高处岩雪
                return h<3f ? Color.Lerp(new Color(0.86f,0.90f,0.93f),new Color(0.96f,0.97f,0.99f),Mathf.Clamp01((aLat-66f)/12f))
                            : Color.Lerp(new Color(0.70f,0.70f,0.72f),new Color(0.96f,0.97f,0.99f),Mathf.InverseLerp(5f,9f,h));
            if(gx>=0&&gz>=0&&gx<G&&gz<G&&Biome[gz,gx]==BiomeKind.Desert)
                return Color.Lerp(new Color(0.83f,0.66f,0.45f),new Color(0.69f,0.50f,0.29f),Mathf.InverseLerp(0.12f,4f,h));
            if(h<=0.14f) return Color.Lerp(new Color(1f,0.97f,0.80f),new Color(0.93f,0.86f,0.62f),
                Mathf.InverseLerp(GameConstants.WaterLevel,0.14f,h));
            // 雨林（亚马逊/刚果/东南亚）：深翠绿
            if(InJungle(lon,lat) && h<3.2f)
            {
                float v=TerrainPainter.FbmRidge(gx,gz,Seed,3)*0.08f-0.04f;
                return new Color(Mathf.Clamp01(0.11f+v),Mathf.Clamp01(0.48f+v),Mathf.Clamp01(0.19f+v));
            }
            if(h<3f)
            {
                Color c;
                if(aLat>=50f) c=Color.Lerp(new Color(0.26f,0.48f,0.30f),new Color(0.18f,0.38f,0.24f),(aLat-50f)/16f); // 寒带针叶林
                else if(aLat>=12f && aLat<24f) c=Color.Lerp(new Color(0.46f,0.66f,0.32f),new Color(0.55f,0.62f,0.30f),(aLat-12f)/12f); // 热带草原
                else c=Color.Lerp(new Color(0.34f,0.85f,0.36f),new Color(0.18f,0.68f,0.24f),Mathf.InverseLerp(0.14f,3f,h)*0.7f); // 温带/赤道翠绿
                float v=TerrainPainter.FbmRidge(gx,gz,Seed,3)*0.10f-0.05f;
                return new Color(Mathf.Clamp01(c.r+v),Mathf.Clamp01(c.g+v),Mathf.Clamp01(c.b+v));
            }
            if(h<5.4f) return Color.Lerp(new Color(0.55f,0.57f,0.33f),new Color(0.62f,0.55f,0.40f),Mathf.InverseLerp(3f,5.4f,h));
            if(h<8.5f) return Color.Lerp(new Color(0.55f,0.55f,0.58f),new Color(0.70f,0.68f,0.66f),Mathf.InverseLerp(5.4f,8.5f,h));
            return Color.Lerp(new Color(0.70f,0.68f,0.66f),new Color(0.95f,0.96f,0.98f),Mathf.InverseLerp(8.5f,11.5f,h));
        }

        static bool InJungle(float lon,float lat)
        {
            foreach(var j in EarthMapData.Jungles)
            {
                float ex=(lon-j.Lon)/j.R, ez=(lat-j.Lat)/(j.R*0.72f);
                if(ex*ex+ez*ez<=1f) return true;
            }
            return false;
        }
        static bool IsPolarLand(int id)
        {
            foreach(var L in EarthMapData.Lands) if(L.Id==id) return L.Polar;
            return false;
        }
        static int EarthLandKind(int id)
        {
            foreach(var L in EarthMapData.Lands) if(L.Id==id) return L.Kind;
            return 0;
        }
        static bool InBounds(int x,int z,int n)=>x>=0&&z>=0&&x<n&&z<n;
        int LonGX(float lon)=>Mathf.Clamp(Mathf.RoundToInt(G*0.5f+EarthMapData.LonToX(lon)/Tile),0,G-1);
        int LatGZ(float lat)=>Mathf.Clamp(Mathf.RoundToInt(G*0.5f+EarthMapData.LatToZ(lat)/TZ),0,G-1);
        static float DegToWorldX(float deg)=>deg/180f*2400f;
        static float DegToWorldZ(float deg)=>deg/90f*EarthMapData.EquatHalfZ;

        /// <summary>大洲 Id→真实名称（经典模式返回空串）</summary>
        public string EarthContinentName(int id)=>EarthMode?EarthMapData.ContinentName(id):"";
    }
}
