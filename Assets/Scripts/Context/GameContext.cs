using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Core.Time;
using PxC.Core.World;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Host;
using PxC.Infra.Save;

namespace PxC.Context
{
    /// <summary>
    /// 启动后的只读测试上下文（供 PlayMode 冒烟测试验证装配结果，不暴露业务规则）。
    /// 由 GameBootstrap 在成功启动后填充（经 GameContext 静态出口，UI 表现层可读）。
    /// v10.8.0 独立程序集 PxC.Context：打破 Host↔Gameplay/Infra 与 Bootstrap↔UI 的依赖环。
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
            BuildingRegistry buildings,
            SettlementRegistry settlements,
            SaveCoordinator saves)
        {
            Clock = clock;
            Events = events;
            Services = services;
            World = world;
            Runtime = runtime;
            Factions = factions;
            Units = units;
            Buildings = buildings;
            Settlements = settlements;
            Saves = saves;
        }

        public GameClock Clock { get; }
        public IEventBus Events { get; }
        public IServiceLocator Services { get; }
        public ChunkedWorld World { get; }
        public PluginRuntime Runtime { get; }
        public FactionRegistry Factions { get; }
        public UnitRegistry Units { get; }
        public BuildingRegistry Buildings { get; }
        public SettlementRegistry Settlements { get; }
        public SaveCoordinator Saves { get; }
    }

    /// <summary>
    /// v10.8.0 宿主全局静态出口：启动后的上下文经此处暴露，
    /// 供 UI 表现层（PxC.UI）与测试读取，避免程序集循环依赖。
    /// </summary>
    public static class GameContext
    {
        /// <summary>启动完成后的只读测试上下文（未启动/安全模式下为 null）。</summary>
        public static BootstrapTestContext TestContext { get; set; }
    }
}
