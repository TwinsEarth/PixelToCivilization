namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 灾害类型。
    /// </summary>
    public enum DisasterType
    {
        /// <summary>无灾害。</summary>
        None = 0,

        /// <summary>洪水（暴雨引发，冲毁建筑、损失粮食）。</summary>
        Flood = 1,

        /// <summary>干旱（长期无雨，粮食绝收，引发饥荒）。</summary>
        Drought = 2,

        /// <summary>地震（摧毁建筑）。</summary>
        Earthquake = 3,

        /// <summary>瘟疫（人口大量死亡）。</summary>
        Plague = 4,

        /// <summary>台风（低纬度，毁船毁建筑、损粮食）。</summary>
        Typhoon = 5,

        /// <summary>寒潮（高纬度，冻死人、损粮）。</summary>
        ColdWave = 6,
    }

    /// <summary>
    /// 灾害服务（disaster 插件实现，Tier-1）。
    /// 每年依据天气（暴风雨/连续晴/雪）、纬度与随机概率检测灾害，
    /// 施加人口/建筑/粮食影响并发布 DisasterOccurredEvent。
    /// </summary>
    public interface IDisasterService
    {
        /// <summary>当前（本年）灾害（None 表示无）。</summary>
        DisasterType Active { get; }

        /// <summary>当前灾害严重度 [0,1]。</summary>
        float Severity { get; }

        /// <summary>当前灾害对应的游戏年份。</summary>
        int ActiveYear { get; }

        /// <summary>
        /// 年度检测：根据天气/纬度/概率触发并施加影响，返回是否发生灾害。
        /// 应在天气年度推进之后调用（拿到的是当年天气）。
        /// </summary>
        bool YearlyCheck(int gameYear);
    }
}
