namespace PxC.Gameplay.Tech
{
    /// <summary>
    /// 时代（文明阶段）。科技与历史推进逐阶解锁，
    /// 单位分代（骑兵→坦克、木船→航母）、电力/工业/信息均以此为闸门。
    /// </summary>
    public enum Era
    {
        /// <summary>远古（史前/部落，石器木器）。</summary>
        Primitive = 0,

        /// <summary>先秦（青铜/宗法）。</summary>
        BronzeAge = 1,

        /// <summary>秦汉（铁器/大一统）。</summary>
        IronAge = 2,

        /// <summary>隋唐（盛世/开放）。</summary>
        Medieval = 3,

        /// <summary>宋元（经济科技高峰）。</summary>
        HighMiddle = 4,

        /// <summary>明清至大航海（帆船/火器）。</summary>
        AgeOfSail = 5,

        /// <summary>工业革命（蒸汽/电力，煤钢）。</summary>
        Industrial = 6,

        /// <summary>现代（内燃机/两次世界大战/石油）。</summary>
        Modern = 7,

        /// <summary>信息（数据/AI/全球网络）。</summary>
        Information = 8,

        /// <summary>太空（星际/轨道/地外殖民）。</summary>
        Space = 9,
    }
}
