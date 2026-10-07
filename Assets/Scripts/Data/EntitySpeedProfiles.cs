using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PixelToCivilization.Data
{
    /// <summary>
    /// V9.7.2 全移动实体速度与参数权威登记总表（只读数据，不改各系统运行逻辑）。
    /// 统一登记【船 / 人员 / 车辆 / 火车 / 飞机 / 地面部队 / 鸟 / 鱼】的十维参数：
    /// 速度·耐久·攻击·防御·射程·射速·载人量·体积·军衔·智能等级，以及随等级的成长系数。
    /// 数值口径全部取自各系统 Def（NavalSystem / CartSystem / ModernTrafficSystem / IntercityNetworkSystem /
    /// TrainSystem / AirLiftSystem / PopulationSystem / MilitarySystem / WildlifeBirds / WildlifeFish / GroundWarfareSystem）。
    ///
    /// 速度口径说明（SpeedUnit）：
    ///  · WU/s    —— 世界单位/秒（真实时间，玩家可见移动速度；1 格 = GameConstants.Tile=4 世界单位）。
    ///  · COEF     —— 系数（系统内再乘满员/帧因子换算，见 NavalSystem.SpeedOf、CartSystem）。
    ///  · RAD/s    —— 角速度（弧度/秒，鸟/鱼绕圈相位推进，线速度 = 角速度 × 编队半径）。
    /// </summary>
    public struct EntitySpeedProfile
    {
        public string Category;      // 大类
        public string TypeId;
        public string Name;
        public string Era;           // 适用年代
        public float Speed;          // 标称速度（口径见 SpeedUnit）
        public string SpeedUnit;
        public float Durability;
        public float Attack;
        public float Defense;
        public float Range;          // 射程/发现（格）
        public float FireInterval;   // 攻击间隔（秒/发；-1 表示无攻击）
        public int Capacity;         // 载人量
        public float Footprint;      // 体积（世界单位）
        public string Rank;          // 军衔
        public int Intel;            // 智能等级
        public int MaxLevel;
        public string Note;
    }

    public static class EntitySpeedProfiles
    {
        // ===== 等级成长系数（与 GroundWarfareSystem 常量对齐；船只为 NavalSystem 内部系数）=====
        public const int GroundMaxLevel = 10;
        public const int ShipMaxLevel = 3;
        public const float LvSpeed = 0.08f;   // 地面部队速度 +8%/级
        public const float LvHp = 0.12f;      // 耐久 +12%/级
        public const float LvAtk = 0.08f;     // 攻击 +8%/级
        public const float LvDef = 0.10f;     // 防御 +10%/级
        public const float LvRadar = 0.06f;   // 雷达 +6%/级
        public const float ShipLvSpeed = 0.10f; // 船速 +10%/级（NavalSystem.SpeedOf）

        static readonly List<EntitySpeedProfile> _all = Build();

        public static IReadOnlyList<EntitySpeedProfile> All => _all;

        public static IEnumerable<EntitySpeedProfile> ByCategory(string category)
        {
            foreach (var p in _all) if (p.Category == category) yield return p;
        }

        /// <summary>等级成长公式说明（供 UI/帮助展示）。</summary>
        public static string GrowthSummary()
        {
            var sb=new StringBuilder();
            sb.Append("地面部队（最高 Lv10）：速度+8%/级、耐久+12%/级、攻击+8%/级、防御+10%/级、雷达+6%/级、射速间隔-3%/级、射程+1（上限+5）；\n");
            sb.Append("船只（最高 Lv3）：速度+10%/级，实际航速=基础×(0.5+满员率×0.5)×等级，空船0.4倍；\n");
            sb.Append("火车：按代（蒸汽7/内燃12/电力18/高铁36/磁悬浮46 WU/s）；\n");
            sb.Append("鸟/鱼：角速度 0.07–0.12 RAD/s，线速度=角速度×编队半径。");
            return sb.ToString();
        }

        static List<EntitySpeedProfile> Build()
        {
            var l=new List<EntitySpeedProfile>();

            // ===== 地面部队（6 型，完整十维；数值= GroundWarfareSystem Def）=====
            AddGround(l,"cavalry","骑兵","古典（1949前）",3.2f,80,12,6,2,1.8f,1,2.1f,"骑兵都尉",2,"古典高速突击骑兵，机动冲击敌阵");
            AddGround(l,"phalanx","列方阵兵","古典（1949前）",1.2f,140,18,14,2,2.2f,3,3.0f,"百夫长",1,"3 人一组重装方阵，高耐久列阵防守");
            AddGround(l,"chariot","马拉战车","古典（1949前）",2.8f,100,15,8,3,1.9f,2,2.6f,"战车校尉",2,"马拉战车，冲锋践踏撕裂敌阵");
            AddGround(l,"tank","坦克","现代（1949起）",3.5f,180,40,20,12,2.4f,3,3.2f,"装甲营长",3,"重型装甲突击，高耐久中射程");
            AddGround(l,"apc","装甲车","现代（1949起）",5.0f,100,15,12,8,1.8f,8,2.8f,"侦察连长",3,"高速运兵装甲，机动侦察");
            AddGround(l,"missile_vehicle","导弹车","现代（1949起）",2.5f,120,70,10,20,3.5f,3,3.0f,"导弹营长",4,"超远程导弹打击，射程20格");

            // ===== 船只（16 型；Speed=COEF，实际=SpeedOf；Defense/Capacity/Range）=====
            // 古典九型（1949前）
            AddShip(l,"small_boat","小木船","古典",0.04f,50,2,0,0f,1,2.0f,"",1);
            AddShip(l,"medium_boat","帆船","古典",0.035f,100,5,0,0f,5,2.6f,"",1);
            AddShip(l,"large_boat","大船","古典",0.025f,200,10,0,0f,20,3.2f,"",1);
            AddShip(l,"treasure_ship","宝船","古典",0.02f,400,20,0,0f,50,4.4f,"",1);
            AddShip(l,"troop_boat","运兵船","古典",0.035f,80,3,15,12f,10,3.0f,"",1);
            AddShip(l,"war_junk","战船","古典",0.04f,150,8,15,12f,15,3.6f,"",1);
            AddShip(l,"cannon_ship","火炮船","古典",0.03f,250,15,40,18f,20,4.0f,"",1);
            AddShip(l,"fire_ship","火船","古典",0.05f,60,2,60,6f,5,2.4f,"",1);
            AddShip(l,"treasure_warship","宝船战舰","古典",0.025f,500,25,50,20f,50,4.6f,"",1);
            // 现代七型（1949起）
            AddShip(l,"steamship","轮船","现代",0.05f,220,8,0,0f,30,3.8f,"",2);
            AddShip(l,"oil_tanker","油轮","现代",0.04f,320,10,0,0f,40,4.0f,"",2);
            AddShip(l,"cruise_liner","邮轮","现代",0.035f,400,12,0,0f,60,4.4f,"",2);
            AddShip(l,"destroyer","驱逐舰","现代",0.045f,400,18,60,22f,30,4.6f,"",2);
            AddShip(l,"submarine","潜水艇","现代",0.05f,300,14,80,12f,20,4.0f,"",2);
            AddShip(l,"missile_ship","导弹舰","现代",0.04f,500,20,90,34f,40,5.0f,"",2);
            AddShip(l,"aircraft_carrier","航空母舰","现代",0.03f,900,30,120,44f,80,6.0f,"",2);

            // ===== 古代马车（3 型；CartDef.Speed=COEF，×60 换算，无驾驶员×0.4）=====
            Add(l,"马车","small_cart","小车","古典",0.05f,"COEF",80,0,0,0,-1f,1,2.2f,"",1);
            Add(l,"马车","medium_cart","马车","古典",0.045f,"COEF",150,0,0,0,-1f,4,2.8f,"",1);
            Add(l,"马车","large_cart","大马车","古典",0.035f,"COEF",250,0,0,0,-1f,10,3.4f,"",1);

            // ===== 现代车辆（CartSystem 九型 + ModernTraffic 道路行驶；标称 3–8 WU/s）=====
            AddModernCar(l,"car_sedan","小车",0.07f,120,2);
            AddModernCar(l,"car_police","警车",0.075f,220,4);
            AddModernCar(l,"car_ambulance","救护车",0.075f,220,4);
            AddModernCar(l,"car_truck","卡车",0.055f,300,6);
            AddModernCar(l,"car_fire","消防车",0.065f,280,5);
            AddModernCar(l,"car_school_bus","校车",0.05f,300,8);
            AddModernCar(l,"car_garbage","垃圾车",0.05f,260,3);
            AddModernCar(l,"car_ladder","云梯消防车",0.06f,340,5);
            AddModernCar(l,"car_aerial","登高车",0.055f,340,5);

            // ===== 火车/高铁（五代；只在大陆之间，WU/s）=====
            Add(l,"火车","train_t1","蒸汽机","1800起",7f,"WU/s",0,0,0,0,-1f,20,3.0f,"",1);
            Add(l,"火车","train_t2","内燃机","1900起",12f,"WU/s",0,0,0,0,-1f,30,3.2f,"",1);
            Add(l,"火车","train_t3","电力机车","1950起",18f,"WU/s",0,0,0,0,-1f,50,3.4f,"",1);
            Add(l,"火车","train_t4","高铁","1990起",36f,"WU/s",0,0,0,0,-1f,100,3.6f,"",1);
            Add(l,"火车","train_t5","磁悬浮","2010起",46f,"WU/s",0,0,0,0,-1f,100,3.6f,"",1);

            // ===== 飞机（AirLift 运输机/直升机）=====
            Add(l,"飞机","airlift","运输机/直升机","现代",24f,"WU/s",0,0,0,0,-1f,0,4.0f,"",1);

            // ===== 人员 =====
            Add(l,"人员","civilian","平民","全年代",1.2f,"WU/s",0,0,0,0,-1f,0,0.6f,"",1);
            Add(l,"人员","infantry","步兵","古典",1.5f,"WU/s",50,6,0,0,2.2f,0,0.7f,"",1);
            Add(l,"人员","cavalryman","骑兵(人)","古典",3.2f,"WU/s",60,9,0,0,1.8f,0,0.9f,"",1);

            // ===== 鸟（RAD/s；上限500）=====
            Add(l,"鸟","bird","飞鸟群","全年代",0.095f,"RAD/s",0,0,0,0,-1f,0,0.6f,"",1);
            // ===== 鱼（RAD/s；上限300）=====
            Add(l,"鱼","fish","鱼群","全年代",0.09f,"RAD/s",0,0,0,0,-1f,0,0.5f,"",1);

            return l;
        }

        static void AddGround(List<EntitySpeedProfile> l,string id,string name,string era,float speed,
            float dur,float atk,float def,int range,float fire,int cap,float foot,string rank,int intel,string note)
        {
            l.Add(new EntitySpeedProfile{
                Category="地面部队",TypeId=id,Name=name,Era=era,Speed=speed,SpeedUnit="WU/s",
                Durability=dur,Attack=atk,Defense=def,Range=range,FireInterval=fire,
                Capacity=cap,Footprint=foot,Rank=rank,Intel=intel,MaxLevel=GroundMaxLevel,Note=note});
        }
        static void AddShip(List<EntitySpeedProfile> l,string id,string name,string era,float speed,
            float dur,float def,float atk,float range,int cap,float foot,string rank,int maxLv)
        {
            l.Add(new EntitySpeedProfile{
                Category="船只",TypeId=id,Name=name,Era=era,Speed=speed,SpeedUnit="COEF",
                Durability=dur,Attack=atk,Defense=def,Range=range<=0?120f:range,FireInterval=range<=0?-1f:2f,
                Capacity=cap,Footprint=foot,Rank=rank,Intel=range<=0?1:3,MaxLevel=ShipMaxLevel,
                Note="实际航速=Speed×(0.5+满员率×0.5)×(1+0.1×(lv-1))，空船0.4倍"});
        }
        static void AddModernCar(List<EntitySpeedProfile> l,string id,string name,float speed,float dur,int cap)
        {
            l.Add(new EntitySpeedProfile{
                Category="车辆",TypeId=id,Name=name,Era="现代（1949起）",Speed=speed,SpeedUnit="COEF",
                Durability=dur,Attack=0,Defense=0,Range=0,FireInterval=-1,
                Capacity=cap,Footprint=2.6f,Rank="",Intel=1,MaxLevel=1,
                Note="ModernTraffic 道路行驶（3–8 WU/s，随机运动/跟车/堵车模式）"});
        }
        static void Add(List<EntitySpeedProfile> l,string cat,string id,string name,string era,float speed,
            string unit,float dur,float atk,float def,float range,float fire,int cap,float foot,string rank,int intel)
        {
            l.Add(new EntitySpeedProfile{
                Category=cat,TypeId=id,Name=name,Era=era,Speed=speed,SpeedUnit=unit,
                Durability=dur,Attack=atk,Defense=def,Range=range,FireInterval=fire,
                Capacity=cap,Footprint=foot,Rank=rank,Intel=intel,MaxLevel=1});
        }
    }
}
