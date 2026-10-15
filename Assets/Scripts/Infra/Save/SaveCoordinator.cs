using System;
using System.Collections.Generic;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Core.Time;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Services;
using PxC.Host;

namespace PxC.Infra.Save
{
    /// <summary>
    /// 存档协调器（v10.4.1 接线）：集中所有存档入口，防止并发写入与多处直接操作 SaveSystem。
    /// 职责：构建 SaveData（顶层全局状态 + 势力快照 + 各插件段）→ 入异步写队列（每帧 ≤1）。
    /// 自动存档：按游戏年间隔写入主槽；手动存档：UI 调 SaveGame；退出存档：OnApplicationQuit。
    /// 本类只做采集与编排，序列化/原子写入/校验由 SaveSystem 负责。
    /// </summary>
    public sealed class SaveCoordinator
    {
        /// <summary>全局存档版本（与 SaveData.Version 对齐；迁移链由该字段驱动）。</summary>
        public const int SaveVersion = 0;

        /// <summary>主槽（自动存档与启动读档均使用槽 0）。</summary>
        public const int MainSlot = 0;

        /// <summary>自动存档间隔（游戏年）。高倍速下按年推进，用 lastAutoSaveYear 防同帧多次。</summary>
        public const int AutoSaveYears = 20;

        private readonly PluginRuntime _runtime;
        private readonly ISaveService _saves;
        private readonly IServiceLocator _services;
        private readonly FactionRegistry _factions;
        private readonly GameClock _clock;
        private readonly Action<string> _log;
        private readonly string _buildVersion;

        private int _lastAutoSaveYear;

        public SaveCoordinator(
            PluginRuntime runtime,
            ISaveService saves,
            IServiceLocator services,
            FactionRegistry factions,
            GameClock clock,
            string buildVersion,
            Action<string> log)
        {
            _runtime = runtime;
            _saves = saves;
            _services = services;
            _factions = factions;
            _clock = clock;
            _buildVersion = buildVersion ?? string.Empty;
            _log = log;
            _lastAutoSaveYear = clock.Year;
        }

        /// <summary>上次自动存档的游戏年（测试/调试用）。</summary>
        public int LastAutoSaveYear => _lastAutoSaveYear;

        // ----------------------------------------------------------------
        // 存档入口
        // ----------------------------------------------------------------
        /// <summary>手动存档到指定槽（异步写队列）。onDone 在落盘完成帧回调。</summary>
        public void SaveGame(int slot, Action<bool> onDone = null)
        {
            SaveData data = BuildData();
            _saves.WriteAsync(slot, data, onDone);
        }

        /// <summary>立即同步落盘（退出/暂停等不可依赖后续帧的场景）。成功返回 true。</summary>
        public bool SaveImmediate(int slot)
        {
            SaveData data = BuildData();
            return _saves.Write(slot, data);
        }

        /// <summary>
        /// 年度回调（GameLoop 年度钩子驱动）：跨过自动存档间隔则写入主槽。
        /// 高倍速同帧多年时，按 gameYear 差值判断，只触发一次。
        /// </summary>
        public void OnYearAdvanced(int gameYear)
        {
            if (gameYear - _lastAutoSaveYear < AutoSaveYears) return;
            _lastAutoSaveYear = gameYear;
            SaveGame(MainSlot);
            _log?.Invoke($"[存档] 自动存档于游戏年 {gameYear}（槽 {MainSlot}）");
        }

        // ----------------------------------------------------------------
        // 构建存档 DTO
        // ----------------------------------------------------------------
        private SaveData BuildData()
        {
            var data = new SaveData
            {
                Version = SaveVersion,
                BuildVersion = _buildVersion,
                GameYear = _clock.Year,
                Day = _clock.Day,
                FactionCount = _factions.Count,
            };

            IPopulationService pop = null;
            bool hasPop = _services != null && _services.TryResolve(out pop);

            int totalPop = 0;
            IReadOnlyList<FactionInfo> all = _factions.All;
            for (int i = 0; i < all.Count; i++)
            {
                var f = all[i];
                if (f == null) continue;

                data.Factions.Add(new FactionSnapshot
                {
                    Name = f.Name,
                    IsPlayer = f.IsPlayer,
                    Alive = f.Alive,
                    Suzerain = f.Suzerain,
                    BecameVassalYear = f.BecameVassalYear,
                    ColorIndex = f.ColorIndex,
                    HomeX = f.HomeX,
                    HomeZ = f.HomeZ,
                });

                if (hasPop) totalPop += pop.Total(f.Id);
            }

            data.Population = totalPop;

            // 各插件存档段（按拓扑顺序采集）
            _runtime.CollectSegments(data);
            return data;
        }
    }
}
