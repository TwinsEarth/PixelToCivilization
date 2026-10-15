using PxC.Core.Services;
using PxC.Core.Time;

namespace PxC.Infra.Time
{
    /// <summary>IClock 服务：包装 GameClock（插件访问时间的唯一入口）。</summary>
    public sealed class ClockAdapter : IClock
    {
        private readonly GameClock _clock;

        public ClockAdapter(GameClock clock)
        {
            _clock = clock;
        }

        public int GameYear => _clock.Year;
        public float YearProgress => _clock.YearProgress;
        public float Speed => _clock.Speed;
        public bool Paused => _clock.Paused;
        public bool Running => _clock.Running;

        public void SetSpeed(float speed) => _clock.Speed = speed;
        public void SetPaused(bool paused) => _clock.Paused = paused;
    }
}
