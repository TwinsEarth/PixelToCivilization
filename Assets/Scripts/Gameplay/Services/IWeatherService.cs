namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 天气状况（年主导天气）。用于农业产出、灾害触发与（远期）移动效率。
    /// </summary>
    public enum WeatherCondition
    {
        /// <summary>晴（风调雨顺，基准）。</summary>
        Clear = 0,

        /// <summary>多云（轻微影响）。</summary>
        Cloudy = 1,

        /// <summary>雨（利于作物，降低野外作业效率）。</summary>
        Rain = 2,

        /// <summary>雪（高纬度冬季，影响出行，作物休眠）。</summary>
        Snow = 3,

        /// <summary>大风（降低海上效率，可能助燃）。</summary>
        Wind = 4,

        /// <summary>雾（降低能见度，远战射程下降）。</summary>
        Fog = 5,

        /// <summary>暴风雨（强降雨，可能引发洪水/台风）。</summary>
        Storm = 6,
    }

    /// <summary>
    /// 天气服务（weather 插件实现，Tier-1 可选）。
    /// 经济/军事/灾害系统在年度结算时查询本服务获得系数；天气插件缺失时系数恒为 1。
    /// 注意：天气按年提前一年生成，保证被依赖的 Economy 在 OnYear 取到当年系数。
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>当前（本年）主导天气。</summary>
        WeatherCondition Current { get; }

        /// <summary>当前天气强度 [0,1]（降雨/降雪/风力大小）。</summary>
        float Intensity { get; }

        /// <summary>当前天气对应的游戏年份。</summary>
        int CurrentYear { get; }

        /// <summary>某势力当年农业（粮食）产出系数（雨利好、雪/旱/暴风不利）。</summary>
        float CropYieldModifier(int faction);

        /// <summary>某势力当年移动/作业效率系数（风/雾/雪降低，远期给海/陆单位）。</summary>
        float MovementModifier(int faction);

        /// <summary>当前天气是否为降水（雨/雪/暴风雨）。</summary>
        bool IsPrecipitating { get; }
    }
}
