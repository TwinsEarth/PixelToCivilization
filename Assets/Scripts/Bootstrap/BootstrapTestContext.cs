using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Core.Time;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Host;

namespace PxC.Bootstrap
{
    /// <summary>
    /// 启动后的只读测试上下文（供 PlayMode 冒烟测试验证装配结果，不暴露业务规则）。
    /// 由 GameBootstrap 在成功启动后填充。
    /// </summary>
    public sealed class BootstrapTestContext
    {
        public BootstrapTestContext(
            GameClock clock,
            IEventBus events,
            IServiceLocator services,
            ChunkedWorld world,
            PluginRuntime runtime,
            FactionRegistry factions,
            UnitRegistry units,
            BuildingRegistry buildings)
        {
            Clock = clock;
            Events = events;
            Services = services;
            World = world;
            Runtime = runtime;
            Factions = factions;
            Units = units;
            Buildings = buildings;
        }

        public GameClock Clock { get; }
        public IEventBus Events { get; }
        public IServiceLocator Services { get; }
        public ChunkedWorld World { get; }
        public PluginRuntime Runtime { get; }
        public FactionRegistry Factions { get; }
        public UnitRegistry Units { get; }
        public BuildingRegistry Buildings { get; }
    }
}
