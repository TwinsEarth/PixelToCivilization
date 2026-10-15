using System;
using PxC.Core.Services;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Weather
{
    /// <summary>
    /// 天气服务：年度天气流转（加权随机 + 马尔可夫持续性 + 纬度），
    /// 以及对农业产出、移动效率的系数。实现 <see cref="IWeatherService"/>。
    /// 天气为"全局主导"（以玩家势力纬度为基准），按 faction 的系数接口保留区域扩展。
    /// </summary>
    public sealed class WeatherService : IWeatherService
    {
        // 天气种类数（与 WeatherCondition 枚举一致）
        private const int KindCount = 7;

        private readonly WeatherState _state;
        private readonly IServiceLocator _services;
        private readonly Action<string> _log;

        public WeatherService(WeatherState state, IServiceLocator services, Action<string> log)
        {
            _state = state;
            _services = services;
            _log = log;
        }

        public WeatherCondition Current => (WeatherCondition)_state.Condition;
        public float Intensity => _state.Intensity;
        public int CurrentYear => _state.CurrentYear;

        public bool IsPrecipitating
        {
            get
            {
                var c = (WeatherCondition)_state.Condition;
                return c == WeatherCondition.Rain || c == WeatherCondition.Snow || c == WeatherCondition.Storm;
            }
        }

        // ------------------------------------------------------------
        // 初始化与年度推进
        // ------------------------------------------------------------

        /// <summary>生成初始年（开局）天气。</summary>
        public void Initialize(int startYear)
        {
            float lat = PlayerLatitude01();
            _state.CurrentYear = startYear;
            RollWeather(lat);
            _state.Initialized = true;
            _log?.Invoke($"[天气] {startYear} 年初始天气：{Describe()}");
        }

        /// <summary>
        /// 推进到下一年（在 OnYear 末尾调用，提前为下一年备好，供 Economy 下一年取系数）。
        /// </summary>
        public void AdvanceTo(int nextYear)
        {
            float lat = PlayerLatitude01();
            _state.CurrentYear = nextYear;
            RollWeather(lat);
        }

        private void RollWeather(float lat)
        {
            // 基础权重（索引 = WeatherCondition）
            float[] w = new float[KindCount];
            w[(int)WeatherCondition.Clear] = 0.30f;
            w[(int)WeatherCondition.Cloudy] = 0.18f;
            w[(int)WeatherCondition.Rain] = 0.22f;
            w[(int)WeatherCondition.Snow] = 0.05f + 0.45f * lat;     // 高纬度多雪
            w[(int)WeatherCondition.Wind] = 0.10f;
            w[(int)WeatherCondition.Fog] = 0.05f;
            w[(int)WeatherCondition.Storm] = 0.10f - 0.05f * lat;    // 低纬度多台风

            // 马尔可夫持续性：上一年同类型权重提升
            int prev = _state.Condition;
            if (prev >= 0 && prev < KindCount) w[prev] *= 1.6f;

            float total = 0f;
            for (int i = 0; i < KindCount; i++) total += w[i];

            float pick = _state.Next() * total;
            int chosen = 0;
            float acc = 0f;
            for (int i = 0; i < KindCount; i++)
            {
                acc += w[i];
                if (pick < acc) { chosen = i; break; }
            }

            _state.Condition = chosen;
            _state.Intensity = RollIntensity((WeatherCondition)chosen);
        }

        private float RollIntensity(WeatherCondition c)
        {
            switch (c)
            {
                case WeatherCondition.Rain: return _state.Range(0.20f, 0.90f);
                case WeatherCondition.Snow: return _state.Range(0.20f, 0.90f);
                case WeatherCondition.Storm: return _state.Range(0.60f, 1.00f);
                case WeatherCondition.Wind: return _state.Range(0.20f, 0.90f);
                case WeatherCondition.Fog: return _state.Range(0.20f, 0.70f);
                case WeatherCondition.Cloudy: return _state.Range(0.20f, 0.60f);
                default: return _state.Range(0.00f, 0.20f);
            }
        }

        // ------------------------------------------------------------
        // 系数
        // ------------------------------------------------------------
        public float CropYieldModifier(int faction)
        {
            float i = _state.Intensity;
            switch ((WeatherCondition)_state.Condition)
            {
                case WeatherCondition.Cloudy: return 0.98f;
                case WeatherCondition.Rain: return 1.0f + 0.15f * i;      // 适量雨利好
                case WeatherCondition.Snow: return 0.55f;                 // 作物休眠
                case WeatherCondition.Wind: return 0.92f;
                case WeatherCondition.Fog: return 0.95f;
                case WeatherCondition.Storm: return 0.70f - 0.30f * i;    // 暴风减产
                default: return 1.0f;
            }
        }

        public float MovementModifier(int faction)
        {
            float i = _state.Intensity;
            switch ((WeatherCondition)_state.Condition)
            {
                case WeatherCondition.Rain: return 0.90f;
                case WeatherCondition.Snow: return 0.50f;
                case WeatherCondition.Wind: return 0.80f;
                case WeatherCondition.Fog: return 0.70f;
                case WeatherCondition.Storm: return 0.40f - 0.10f * i;
                default: return 1.0f;
            }
        }

        // ------------------------------------------------------------
        // 辅助
        // ------------------------------------------------------------
        public string Describe()
        {
            return $"{Current}(强度{Intensity:0.00})";
        }

        /// <summary>
        /// 玩家势力纬度 [0,1]：0=热带，1=寒带。以 HomeZ 映射（HomeZ 越负越靠北）。
        /// 缺失 faction 服务时返回 0.5（温带）。
        /// </summary>
        private float PlayerLatitude01()
        {
            var factions = _services.Resolve<FactionRegistry>();
            if (factions == null || factions.Count <= 0) return 0.5f;
            var info = factions.Get(0);
            if (info == null) return 0.5f;
            // HomeZ 名义范围 [-60, 60]；映射 z=-60 → 1（北），z=60 → 0（南）
            float lat = (-info.HomeZ + 60f) / 120f;
            if (lat < 0f) lat = 0f;
            if (lat > 1f) lat = 1f;
            return lat;
        }
    }
}
