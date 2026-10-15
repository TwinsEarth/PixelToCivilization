using System.Collections.Generic;
using PxC.Gameplay.Services;
using PxC.Gameplay.Tech;

namespace PxC.Content.Dynasties
{
    /// <summary>
    /// 中华朝代序列数据（内容包静态表）。
    /// 公历起止：负数=公元前。序列覆盖约前 2070 至当代。
    /// </summary>
    public static class DynastyContent
    {
        /// <summary>朝代序列（按时间正序）。</summary>
        public static readonly DynastyInfo[] Sequence =
        {
            D("夏", -2070, -1600, Era.BronzeAge, "青铜/宗法"),
            D("商", -1600, -1046, Era.BronzeAge, "青铜/甲骨文"),
            D("西周", -1046, -771, Era.BronzeAge, "分封/礼乐"),
            D("春秋", -770, -476, Era.IronAge, "铁器/争霸"),
            D("战国", -475, -221, Era.IronAge, "变法/七雄"),
            D("秦", -221, -207, Era.IronAge, "大一统/郡县"),
            D("西汉", -202, 8, Era.IronAge, "丝路/独尊儒术"),
            D("东汉", 25, 220, Era.IronAge, "光武/造纸"),
            D("三国", 220, 280, Era.IronAge, "鼎立/战乱"),
            D("西晋", 266, 316, Era.Medieval, "短暂统一"),
            D("东晋十六国", 317, 420, Era.Medieval, "南北对峙"),
            D("南北朝", 420, 589, Era.Medieval, "民族融合"),
            D("隋", 581, 618, Era.Medieval, "大运河/科举"),
            D("唐", 618, 907, Era.Medieval, "盛世开放"),
            D("五代十国", 907, 960, Era.HighMiddle, "割据"),
            D("北宋", 960, 1127, Era.HighMiddle, "经济科技高峰"),
            D("南宋", 1127, 1279, Era.HighMiddle, "偏安/海运"),
            D("元", 1271, 1368, Era.HighMiddle, "大一统/欧亚交流"),
            D("明", 1368, 1644, Era.AgeOfSail, "郑和下西洋/火器"),
            D("清", 1644, 1912, Era.AgeOfSail, "康乾盛世/晚期闭关"),
            D("民国", 1912, 1949, Era.Modern, "近代化/抗战"),
            D("现代", 1949, 9999, Era.Information, "工业化/信息化"),
        };

        private static DynastyInfo D(string name, int start, int end, Era era, string note)
        {
            return new DynastyInfo
            {
                Name = name,
                StartAd = start,
                EndAd = end,
                Era = (int)era,
                Note = note,
            };
        }
    }
}
