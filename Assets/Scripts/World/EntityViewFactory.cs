using UnityEngine;
using PixelToCivilization.Actors;
using PixelToCivilization.Core;
using PixelToCivilization.Audio;

namespace PixelToCivilization.World
{
    /// <summary>移动实体（敌军/战船/投射物/舰队/太空基地）的简单程序化视图工厂</summary>
    public static class EntityViewFactory
    {
        public static Transform EnsureRoot(string name, Transform parent=null)
        {
            var found=GameObject.Find(name);
            if (found!=null) return found.transform;
            var go=new GameObject(name);
            if (parent!=null) go.transform.SetParent(parent);
            return go.transform;
        }

        public static GameObject Spawn(string name, Transform parent, PrimitiveType type, Color c, float scale=1f)
        {
            var go=GameObject.CreatePrimitive(type);
            go.name=name;
            if (parent!=null) go.transform.SetParent(parent);
            var col=go.GetComponent<Collider>(); if (col!=null) Object.Destroy(col);
            var r=go.GetComponent<Renderer>();
            if (r!=null) r.sharedMaterial=ShaderHelper.Mat(c);
            go.transform.localScale=Vector3.one*scale;
            return go;
        }

        // —— V7.0.3 基础体对象池（投射物等短生命周期视图复用，材质按颜色缓存无泄漏）——
        const string PrimPrefix = "prim_";
        static string PrimKey(PrimitiveType type) => PrimPrefix + type;

        public static GameObject SpawnPooled(string name, Transform parent, PrimitiveType type, Color c, float scale=1f)
        {
            string key=PrimKey(type);
            var go=GameObjectPool.Rent(key,parent,()=>
            {
                var g=GameObject.CreatePrimitive(type);
                var cc=g.GetComponent<Collider>(); if(cc!=null) Object.Destroy(cc);
                return g;
            });
            go.name=name;
            var r=go.GetComponent<Renderer>();
            if(r!=null) r.sharedMaterial=ShaderHelper.Mat(c);
            go.transform.localScale=Vector3.one*scale;
            return go;
        }

        public static void RecyclePooled(GameObject go, PrimitiveType type)
        {
            if(go==null) return;
            GameObjectPool.Return(PrimKey(type),go);
        }

        /// <summary>V6.1.1 程序化人形士兵/居民（带代码骨骼动画）</summary>
        public static GameObject SpawnHumanoid(string name, Transform parent, Color outfit, float scale=1f, bool armored=false)
        {
            var go=new GameObject(name);
            if(parent!=null) go.transform.SetParent(parent);
            // V7.0.2 士兵统一走 soldier 职业（铁盔/长矛/盾/军旗），Q版比例，时代军服在 Build 内按 armored 偏铁灰
            HumanoidFactory.Build(go,outfit,scale,armored,"soldier","commoner",1,0,0,24);
            return go;
        }

        /// <summary>V6.1.1 程序化多时代载具/舰船（轮子/桨可动）；V6.1.3 sub 区分具体车型/船型</summary>
        public static GameObject SpawnVehicle(string name, Transform parent, VehicleKind kind, Color hull, float scale=1f, string sub=null)
        {
            var go=new GameObject(name);
            if(parent!=null) go.transform.SetParent(parent);
            VehicleFactory.Build(go,kind,hull,scale,sub);
            AttachSound(go,kind,sub);   // V7.1.0 船/车实体音效
            return go;
        }

        // V7.1.0 船(8 型)/车(3 型)各自独立音效；缺资源或现代载具则不挂
        static readonly System.Collections.Generic.HashSet<string> ShipRes = new System.Collections.Generic.HashSet<string>{
            "small_boat","medium_boat","large_boat","treasure_ship","troop_boat","cannon_ship","fire_ship","treasure_warship" };
        static readonly System.Collections.Generic.HashSet<string> CartRes = new System.Collections.Generic.HashSet<string>{
            "small_cart","medium_cart","large_cart" };
        static void AttachSound(GameObject go, VehicleKind kind, string sub)
        {
            if (string.IsNullOrEmpty(sub)) return;
            if (kind==VehicleKind.SailBoat || kind==VehicleKind.Warship)
            {
                string res = ShipRes.Contains(sub) ? ("ship_"+sub) : (sub=="war_junk" ? "ship_medium_boat" : null);
                if (res==null) return;
                var es=go.AddComponent<EntitySound>();
                es.Cat=SoundCat.Ship; es.Res=res;
                bool big = sub=="treasure_ship" || sub=="treasure_warship" || sub=="large_boat";
                es.MaxDistance = big ? 130f : 95f;
                es.BaseVolume = big ? 1.1f : 0.95f;
            }
            else if (kind==VehicleKind.Cart && CartRes.Contains(sub))
            {
                var es=go.AddComponent<EntitySound>();
                es.Cat=SoundCat.Cart; es.Res="cart_"+sub;
                es.MaxDistance = sub=="large_cart" ? 90f : 65f;
            }
        }

        public static Color Hex(long h)=>new(((h>>16)&255)/255f,((h>>8)&255)/255f,(h&255)/255f);

        /// <summary>把视图贴到地形表面（若有地形查询）</summary>
        public static void Place(GameObject view, WorldGenerator terrain, float x, float z, float yOffset=0f)
        {
            if (view==null) return;
            float y = terrain!=null ? terrain.HeightAt(x,z) : 0f;
            view.transform.position=new Vector3(x,y+yOffset,z);
        }
    }
}
