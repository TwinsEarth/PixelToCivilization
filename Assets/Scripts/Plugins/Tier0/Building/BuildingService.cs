using System;
using UnityEngine;
using PxC.Core.Services;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Building
{
    /// <summary>
    /// IBuildingService 实现：建造/升级/拆除的唯一入口。
    /// 流程：类型目录校验 → 时代/容量检查 → 原子扣费（IEconomyService）→ 登记 BuildingRegistry。
    /// 建筑上限 SoA=800，逼近 90% 暂停建造（口径裁决）。
    /// </summary>
    public sealed class BuildingService : IBuildingService
    {
        /// <summary>建筑全局上限（设计常量）。</summary>
        public const int BuildingHardCap = 800;

        /// <summary>逼近上限的暂停阈值（90%）。</summary>
        public const float PauseRatio = 0.9f;

        private readonly IServiceLocator _services;
        private readonly BuildingRegistry _registry;
        private readonly Action<string> _log;

        public BuildingService(IServiceLocator services, BuildingRegistry registry, Action<string> log)
        {
            _services = services;
            _registry = registry;
            _log = log;
        }

        public int TotalCount(int faction) => _registry.TotalCount(faction);

        public int CountByType(int faction, int typeId) =>
            _registry.CountByType(faction, typeId);

        public bool CanBuild(int faction, int typeId)
        {
            if (typeId < 0 || typeId >= BuildingTypes.Count) return false;
            var entry = BuildingCatalog.Get((BuildingType)typeId);

            // 时代检查
            if (entry.RequiredEra > 0 && _services.TryResolve<ITechService>(out var tech))
            {
                if (!tech.HasEra(faction, entry.RequiredEra)) return false;
            }

            // 容量检查（全局上限 90% 暂停）
            if (_registry.Count >= BuildingHardCap * PauseRatio) return false;

            // 成本检查
            if (_services.TryResolve<IEconomyService>(out var economy))
            {
                if (!economy.CanAffordBundle(faction, entry.Cost)) return false;
            }
            return true;
        }

        public int TryBuild(int faction, int typeId, Vector3 position)
        {
            if (!CanBuild(faction, typeId)) return -1;
            var entry = BuildingCatalog.Get((BuildingType)typeId);

            // 原子扣费（先全量校验，再一次性扣）
            if (_services.TryResolve<IEconomyService>(out var economy))
            {
                if (!economy.TrySpendBundle(faction, entry.Cost))
                {
                    _log?.Invoke($"[建造] 势力 {faction} 资源不足，{entry.DisplayName} 取消");
                    return -1;
                }
            }

            // 登记
            int slot = _registry.Spawn(faction, typeId, position, entry.MaxHp, entry.IsTower);
            _log?.Invoke($"[建造] 势力 {faction} 建成 {entry.DisplayName}（slot {slot}）");
            return slot;
        }

        public bool TryUpgrade(int faction, int slot)
        {
            var rec = _registry.GetSafe(slot);
            if (!rec.Alive || rec.Faction != faction) return false;

            var entry = BuildingCatalog.Get((BuildingType)rec.TypeId);

            // 升级成本：建造成本的 60% × 当前等级
            if (_services.TryResolve<IEconomyService>(out var economy))
            {
                var upgradeCost = ScaleCost(entry.Cost, 0.6f * rec.Level);
                if (!economy.TrySpendBundle(faction, upgradeCost)) return false;
            }

            // 提升等级（产出与耐久按等级增长）
            ref var r = ref _registry.GetSlotRef(slot);
            r.Level++;
            r.MaxHp = entry.MaxHp * (1f + 0.25f * (r.Level - 1));
            r.Hp = r.MaxHp;
            _log?.Invoke($"[升级] {entry.DisplayName} 升至 {r.Level} 级");
            return true;
        }

        public bool TryDemolish(int faction, int slot)
        {
            var rec = _registry.GetSafe(slot);
            if (!rec.Alive || rec.Faction != faction) return false;

            var entry = BuildingCatalog.Get((BuildingType)rec.TypeId);

            // 返还部分成本
            if (_services.TryResolve<IEconomyService>(out var economy))
            {
                var refund = ScaleCost(entry.Cost, entry.RefundRatio);
                for (int i = 0; i < refund.Length; i++)
                    economy.Add(faction, refund[i].Type, refund[i].Amount);
            }

            _registry.Kill(slot);
            _log?.Invoke($"[拆除] 势力 {faction} 拆除 {entry.DisplayName}");
            return true;
        }

        private static ResourceCost[] ScaleCost(ResourceCost[] baseCost, float factor)
        {
            if (baseCost == null) return Array.Empty<ResourceCost>();
            var scaled = new ResourceCost[baseCost.Length];
            for (int i = 0; i < baseCost.Length; i++)
                scaled[i] = new ResourceCost(baseCost[i].Type, baseCost[i].Amount * factor);
            return scaled;
        }
    }
}
