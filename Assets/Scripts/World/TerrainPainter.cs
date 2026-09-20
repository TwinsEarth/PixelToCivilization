using UnityEngine;
using PixelToCivilization.Data;

namespace PixelToCivilization.World
{
    /// <summary>
    /// V6.1.1 地形绘制器：把 120×120 高度场烘焙成 PBR 生物群系大贴图
    /// （Albedo / Normal / Mask），并提供 FBM 自然地貌与平滑法线，替代 V5.9.9 的硬顶点色。
    /// 生物群系按高度 + 坡度 + 噪声在沙/草/岩/雪/岸滩间自然过渡。
    /// </summary>
    public static class TerrainPainter
    {
        // —— V9.0.1 模拟城市式干净明亮生物群系基色（Linear 友好）——
        static readonly Color Sand = new(0.93f, 0.84f, 0.60f);   // 暖米色沙滩
        static readonly Color GrassA = new(0.40f, 0.70f, 0.27f);  // 饱和草坪绿
        static readonly Color GrassB = new(0.49f, 0.77f, 0.33f);  // 亮草坪绿
        static readonly Color Rock = new(0.63f, 0.63f, 0.61f);    // 中性灰岩
        static readonly Color RockDark = new(0.50f, 0.50f, 0.49f);
        static readonly Color Snow = new(0.94f, 0.97f, 1.00f);
        static readonly Color Underwater = new(0.46f, 0.74f, 0.82f); // 浅海沙青
        static readonly Color Dirt = new(0.66f, 0.53f, 0.36f);   // 暖棕土

        // ---------- 可平铺 value noise / fbm ----------
        static int Hash(int x,int y,int seed){
            int h=x*374761393+y*668265263+seed*144269504; h=(h^(h>>13))*1274126177; h^=h>>16; return h&0x7fffffff; }
        static float Vn(float x,float y,int period,int seed){
            int xi=Mathf.FloorToInt(x),yi=Mathf.FloorToInt(y); float xf=x-xi,yf=y-yi;
            int x0=xi%period,x1=(xi+1)%period,y0=yi%period,y1=(yi+1)%period;
            if(x0<0)x0+=period;if(x1<0)x1+=period;if(y0<0)y0+=period;if(y1<0)y1+=period;
            float a=Hash(x0,y0,seed)/2147483647f,b=Hash(x1,y0,seed)/2147483647f,
                  cc=Hash(x0,y1,seed)/2147483647f,d=Hash(x1,y1,seed)/2147483647f;
            float u=xf*xf*(3-2*xf),v=yf*yf*(3-2*yf);
            return Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(cc,d,u),v);
        }
        /// <summary>多倍频 FBM 自然起伏（输出约 -1..1）</summary>
        public static float FbmRidge(float x,float z,int seed,int oct=5){
            float amp=1,freq=0.018f,sum=0,norm=0;
            for(int o=0;o<oct;o++){ sum+=Vn(x*freq+o*9.1f,z*freq-o*7.3f,Mathf.Max(4,Mathf.RoundToInt(40*freq*200)),seed+o*131)*amp; norm+=amp; amp*=0.52f; freq*=2.05f; }
            return (sum/norm-0.5f)*2f;
        }

        /// <summary>双线性采样高度场</summary>
        static float SampleH(float[,] h,float u,float v){
            int n=h.GetLength(0);
            float fx=Mathf.Clamp01(u)*(n-1), fz=Mathf.Clamp01(v)*(n-1);
            int x0=Mathf.FloorToInt(fx),z0=Mathf.FloorToInt(fz);
            int x1=Mathf.Min(x0+1,n-1),z1=Mathf.Min(z0+1,n-1);
            float tx=fx-x0,tz=fz-z0;
            return Mathf.Lerp(Mathf.Lerp(h[z0,x0],h[z0,x1],tx),Mathf.Lerp(h[z1,x0],h[z1,x1],tx),tz);
        }

        /// <summary>烘焙一整套地形 PBR 贴图。res 建议 512(手机)/1024(PC)。
        /// V8.0.1 乐高底板：按地形格切面化(facet)着色/法线 + 每格圆形凸点(亮盘/AO环/格缝) + 塑料光滑度。</summary>
        public static void Bake(float[,] height, float tile, int seed, int res,
            out Texture2D albedo, out Texture2D normal, out Texture2D mask)
        {
            int n=height.GetLength(0);
            albedo=new Texture2D(res,res,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
            normal=new Texture2D(res,res,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
            mask  =new Texture2D(res,res,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var ca=new Color32[res*res]; var cn=new Color32[res*res]; var cm=new Color32[res*res];
            float water=GameConstants.WaterLevel;

            // V9.0.1 平滑现代地表：双线性连续高度 + 有限差分连续法线，跨格无切面/无凸点/无量化色阶
            const float duv=0.9f;                            // 法线差分步长（约 1 格，uv 单位 1/n）
            float invSpan=1f/(2f*(duv/n)*n*tile);            // 世界高差→法线斜率
            for(int y=0;y<res;y++)for(int x=0;x<res;x++){
                float u=x/(float)(res-1), v=y/(float)(res-1);
                float h=SampleH(height,u,v);
                float hl=SampleH(height,Mathf.Clamp01(u-duv/n),v), hr=SampleH(height,Mathf.Clamp01(u+duv/n),v);
                float hd=SampleH(height,u,Mathf.Clamp01(v-duv/n)), hu=SampleH(height,u,Mathf.Clamp01(v+duv/n));
                Vector3 fn=new Vector3((hl-hr)*invSpan,1f,(hd-hu)*invSpan).normalized;
                float slope=1f-fn.y;
                float grain=Mathf.Lerp(0.94f,1.06f,Mathf.PerlinNoise(u*n*1.7f+seed*0.37f,v*n*1.7f+seed*0.19f));
                Color col=BiomeColor(h,slope,grain,seed);
                if(h<water){
                    float depth=Mathf.Clamp01((water-h)/2.2f);
                    col=Color.Lerp(Underwater,col,Mathf.Lerp(0.82f,0.40f,depth));
                    if(depth<0.12f) col=Color.Lerp(col,Sand,0.35f); // 近岸湿润沙晕
                }
                col*=0.92f+fn.y*0.10f;                         // 连续向光面微提亮、坡面微压
                ca[y*res+x]=(Color32)col;
                cn[y*res+x]=new Color32((byte)Mathf.Clamp(Mathf.RoundToInt((fn.x*0.5f+0.5f)*255f),0,255),
                                         (byte)Mathf.Clamp(Mathf.RoundToInt((fn.y*0.5f+0.5f)*255f),0,255),
                                         (byte)Mathf.Clamp(Mathf.RoundToInt((fn.z*0.5f+0.5f)*255f),0,255),255);
                float sm=BiomeSmoothness(h,slope);
                if(h<water) sm=Mathf.Max(sm,0.45f);            // 水面微亮，草坪/沙/岩保持哑光
                cm[y*res+x]=new Color32(0,235,0,(byte)Mathf.Clamp(Mathf.RoundToInt(sm*255),0,255));
            }
            albedo.SetPixels32(ca);normal.SetPixels32(cn);mask.SetPixels32(cm);
            albedo.Apply(true,false);normal.Apply(true,false);mask.Apply(true,false);
            albedo.anisoLevel=8;normal.anisoLevel=8;
        }

        static Color BiomeColor(float h,float slope,float grain,int seed)
        {
            Color col;
            if (h < GameConstants.WaterLevel-0.6f) col = Color.Lerp(Underwater,Dirt,Mathf.Clamp01((h+2f)));
            else if (h <= 0.35f) col = Color.Lerp(Sand,Dirt,0.25f+grain*0.2f);             // 沙滩
            else if (h < 3.0f){                                                             // 草原（两种绿斑驳）
                Color g=Color.Lerp(GrassA,GrassB,grain);
                col=Color.Lerp(g,Dirt,Mathf.Clamp01((h-2.4f)/1.2f)*0.15f); // V7.0.1 草地少混土
            }
            else if (h < 7.0f) col=Color.Lerp(GrassB,Rock,Mathf.Clamp01((h-3f)/4f));       // 丘陵转岩
            else col=Color.Lerp(Rock,Snow,Mathf.Clamp01((h-7f)/2.5f));                      // 雪线
            // 陡坡强制裸露岩石
            float rockAmt=Mathf.Clamp01((slope-0.28f)/0.22f);
            col=Color.Lerp(col,Color.Lerp(Rock,RockDark,grain*0.4f),rockAmt);
            // 雪线以上且非极陡覆雪
            if(h>7.5f) col=Color.Lerp(col,Snow,Mathf.Clamp01(1f-rockAmt));
            return col;
        }
        static float BiomeSmoothness(float h,float slope){
            if(h>7.5f) return 0.55f;
            if(h<GameConstants.WaterLevel) return 0.4f;
            if(h<=0.35f) return 0.22f;
            if(slope>0.4f) return 0.28f;
            return 0.08f; // 草地哑光
        }

        /// <summary>中心差分平滑法线（替代 Mesh.RecalculateNormals 的硬面法线）</summary>
        public static Vector3[] SmoothNormals(float[,] height,float tile,float worldStep)
        {
            int n=height.GetLength(0);
            var normals=new Vector3[(n+1)*(n+1)];
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){
                float hl=height[Mathf.Clamp(z,0,n-1),Mathf.Clamp(x-1,0,n-1)];
                float hr=height[Mathf.Clamp(z,0,n-1),Mathf.Clamp(x+1,0,n-1)];
                float hd=height[Mathf.Clamp(z-1,0,n-1),Mathf.Clamp(x,0,n-1)];
                float hu=height[Mathf.Clamp(z+1,0,n-1),Mathf.Clamp(x,0,n-1)];
                normals[z*(n+1)+x]=new Vector3((hl-hr)*tile,2.4f,(hd-hu)*tile).normalized;
            }
            return normals;
        }
    }
}
