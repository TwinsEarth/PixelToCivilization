using System;
using PxC.Core.Services;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Population
{
    /// <summary>
    /// IPopulationService 实现（v10.4.0 治本统一）。
    /// 人口唯一存储为聚落 × 分层；出生/成年/自然死亡都在聚落粒度结算，
    /// 势力人口为该势力所有聚落的聚合。移民是聚落间空间转移（总量守恒）。
    /// 出生率/死亡率含政策系数；饥荒死亡由 FamineEvent 单独结算（按 FamineOrder）。
    /// </summary>
    public sealed class PopulationService : IPopulationService
    {
        // ---- 年度人口口径 ----
        private const float BaseBirthRate = 0.035f;       // 自然出生率
        private const float NaturalDeathRate = 0.015f;   // 自然死亡率
        private const float MatureRatio = 0.12f;         // 每年儿童成年比例
        private const float PopulationCapacityPerSettlement = 550f; // 每聚落人口容量
        private const float MinimumBirthFactor = 0.30f;  // 饱和时出生率最低比例
        private const int HardCap = 5000;                // 聚落人口硬安全阀（正常 550 先饱和）

        // 首都初始人口（合计 100）：Children/Laborer/Artisan/Soldier/Scholar/Elder
        private static readonly int[] CapitalLayers = { 20, 55, 8, 8, 4, 5 };

        /// <summary>自然死亡顺序：老人 → 儿童 → 劳力 → 工匠 → 学者 → 士兵。</summary>
        private static readonly PopulationLayer[] NaturalDeathOrder =
        {
            PopulationLayer.Elder, PopulationLayer.Children,
            PopulationLayer.Laborer, PopulationLayer.Artisan,
            PopulationLayer.Scholar, PopulationLayer.Soldier
        };

        /// <summary>饥荒死亡顺序：老人 → 儿童 → 士兵 → 工匠 → 学者 → 劳力（保住劳力）。</summary>
        private static readonly PopulationLayer[] FamineOrder =
        {
            PopulationLayer.Elder, PopulationLayer.Children,
            PopulationLayer.Soldier, PopulationLayer.Artisan,
            PopulationLayer.Scholar, PopulationLayer.Laborer
        };

        /// <summary>灾害死亡顺序：老人 → 儿童 → 劳力 → 工匠 → 学者 → 士兵。</summary>
        private static readonly PopulationLayer[] DisasterOrder =
        {
            PopulationLayer.Elder, PopulationLayer.Children,
            PopulationLayer.Laborer, PopulationLayer.Artisan,
            PopulationLayer.Scholar, PopulationLayer.Soldier
        };

        private readonly PopulationState _state;
        private readonly SettlementRegistry _settlements;
        private readonly IServiceLocator _services;

        public PopulationService(
            PopulationState state,
            SettlementRegistry settlements,
            IServiceLocator services)
        {
            _state = state;
            _settlements = settlements;
            _services = services;
        }

        // ============================================================
        // 势力聚合口径
        // ============================================================
        public int Total(int faction)
        {
            int sum = 0;
            int cap = _settlements.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                var r = _settlements.GetSafe(slot);
                if (r.Alive && r.Faction == faction) sum += _state.SlotTotal(slot);
            }
            return sum;
        }

        public int ByLayer(int faction, PopulationLayer layer)
        {
            int sum = 0;
            int cap = _settlements.Capacity;
            for (int slot = 0; slot < cap; slot++)
            {
                var r = _settlements.GetSafe(slot);
                if (r.Alive && r.Faction == faction) sum += _state.Get(slot, layer);
            }
            return sum;
        }

        public float BirthRate(int faction) => BaseBirthRate;

        public float DeathRate(int faction) => NaturalDeathRate;

        public float StarvationRisk(int faction)
        {
            // 实时饥荒风险在 economy 年结后以 FamineEvent 体现；此处基准为 0。
            return 0f;
        }

        public bool TryAssign(int faction, PopulationLayer from, PopulationLayer to, int count)
        {
            if (count <= 0) return false;
            if (ByLayer(faction, from) < count) return false;

            int remaining = count;
            int cap = _settlements.Capacity;
            for (int slot = 0; slot < cap && remaining > 0; slot++)
            {
                var r = _settlements.GetSafe(slot);
                if (!r.Alive || r.Faction != faction) continue;
                int have = _state.Get(slot, from);
                if (have <= 0) continue;
                int take = have < remaining ? have : remaining;
                _state.Add(slot, from, -take);
                _state.Add(slot, to, take);
                remaining -= take;
            }
            return remaining == 0;
        }

        public int ApplyExcessDeaths(int faction, int count)
        {
            if (count <= 0) return 0;
            return RemoveInOrderAcrossFaction(faction, count, DisasterOrder);
        }

        // ============================================================
        // 聚落粒度口径
        // ============================================================
        public int SettlementTotal(int slot) => _state.SlotTotal(slot);

        public int SettlementByLayer(int slot, PopulationLayer layer) =>
            _state.Get(slot, layer);

        public void ClearSettlement(int slot)
        {
            _state.EnsureCapacity(slot + 1);
            _state.ClearSlot(slot);
        }

        public void SeedCapital(int slot)
        {
            _state.EnsureCapacity(slot + 1);
            _state.ClearSlot(slot);
            for (int layer = 0; layer < PopulationState.LayerCount; layer++)
                _state.Set(slot, (PopulationLayer)layer, CapitalLayers[layer]);
        }

        public bool SendMigrants(int fromSlot, int toSlot, PopulationLayer layer, int count)
        {
            if (count <= 0) return false;
            if (!_state.TryRemove(fromSlot, layer, count)) return false;
            _state.Add(toSlot, layer, count);
            return true;
        }

        /// <summary>饥荒扣人（FamineEvent 结算）：按 FamineOrder 从该势力各聚落扣。</summary>
        public int ApplyFamineDeaths(int faction, int count)
        {
            if (count <= 0) return 0;
            return RemoveInOrderAcrossFaction(faction, count, FamineOrder);
        }

        // ============================================================
        // 年度人口结算（聚落粒度）
        // ============================================================
        /// <summary>
        /// 遍历所有存活聚落，结算出生/成年/自然死亡；按势力累计到 births/deaths 输出。
        /// 由 PopulationPlugin.OnYear 调用（在 settlement 发展结算之前）。
        /// </summary>
        public void SettlePopulationYear(int factionCount, int[] birthsByFaction, int[] deathsByFaction)
        {
            int cap = _settlements.Capacity;
            _state.EnsureCapacity(cap);

            // 每势力政策系数（缺失=1）
            float[] birthMod = new float[factionCount];
            float[] deathMod = new float[factionCount];
            for (int f = 0; f < factionCount; f++)
            {
                birthMod[f] = 1f;
                deathMod[f] = 1f;
            }
            if (_services != null && _services.TryResolve<IPolicyService>(out var pol))
            {
                for (int f = 0; f < factionCount; f++)
                {
                    birthMod[f] = SafeOne(pol.BirthModifier(f));
                    deathMod[f] = SafeOne(pol.DeathModifier(f));
                }
            }

            for (int slot = 0; slot < cap; slot++)
            {
                if (!_settlements.IsAlive(slot, _settlements.GetGeneration(slot))) continue;
                int faction = _settlements.GetSafe(slot).Faction;
                float bm = faction >= 0 && faction < factionCount ? birthMod[faction] : 1f;
                float dm = faction >= 0 && faction < factionCount ? deathMod[faction] : 1f;

                SettleOneSettlement(slot, bm, dm, out int b, out int d);

                if (faction >= 0 && faction < birthsByFaction.Length)
                {
                    birthsByFaction[faction] += b;
                    deathsByFaction[faction] += d;
                }
            }
        }

        private void SettleOneSettlement(
            int slot, float birthMod, float deathMod,
            out int birthsOut, out int deathsOut)
        {
            birthsOut = 0;
            deathsOut = 0;

            int total = _state.SlotTotal(slot);
            if (total <= 0) return;

            // 出生：logistic 按聚落容量（550），接近容量时出生率降到最低系数。
            float sat = Math.Max(0f, 1f - total / PopulationCapacityPerSettlement);
            float effBirth = BaseBirthRate *
                (MinimumBirthFactor + (1f - MinimumBirthFactor) * sat);
            int births = (int)Math.Floor(total * effBirth * SafeOne(birthMod));
            if (births > 0)
            {
                _state.Add(slot, PopulationLayer.Children, births);
                birthsOut = births;
            }

            // 成年：儿童 → 劳力
            int children = _state.Get(slot, PopulationLayer.Children);
            int mature = (int)Math.Floor(children * MatureRatio);
            if (mature > 0)
            {
                mature = Math.Min(mature, _state.Get(slot, PopulationLayer.Children));
                _state.Add(slot, PopulationLayer.Children, -mature);
                _state.Add(slot, PopulationLayer.Laborer, mature);
            }

            // 自然死亡（按 NaturalDeathOrder：老人/儿童优先）
            int current = _state.SlotTotal(slot);
            int naturalDeaths = (int)Math.Floor(current * NaturalDeathRate * SafeOne(deathMod));
            if (naturalDeaths > 0)
            {
                int removed = _state.RemoveInOrder(slot, naturalDeaths, NaturalDeathOrder);
                deathsOut += removed;
            }

            // 硬安全阀：聚落人口超过 HardCap 时，强制移除超出部分（正常 550 不会触发）。
            int over = _state.SlotTotal(slot) - HardCap;
            if (over > 0)
            {
                int removed = _state.RemoveInOrder(slot, over, NaturalDeathOrder);
                deathsOut += removed;
            }
        }

        // ============================================================
        // 辅助
        // ============================================================
        private int RemoveInOrderAcrossFaction(int faction, int amount, PopulationLayer[] order)
        {
            int remaining = amount;
            int cap = _settlements.Capacity;
            for (int oi = 0; oi < order.Length && remaining > 0; oi++)
            {
                PopulationLayer layer = order[oi];
                for (int slot = 0; slot < cap && remaining > 0; slot++)
                {
                    var r = _settlements.GetSafe(slot);
                    if (!r.Alive || r.Faction != faction) continue;
                    int have = _state.Get(slot, layer);
                    int take = have < remaining ? have : remaining;
                    if (take > 0) _state.Add(slot, layer, -take);
                    remaining -= take;
                }
            }
            return amount - remaining;
        }

        /// <summary>政策系数兜底：非有限/非正返回 1（避免 NaN/负数污染）。</summary>
        private static float SafeOne(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v) || v <= 0f) return 1f;
            return v;
        }
    }
}
