using System;
using UnityEngine;
using PxC.Gameplay.Tech;
using PxC.Gameplay.Services;

namespace PxC.Plugins.Tech
{
    /// <summary>
    /// ITechService 实现：包装 <see cref="TechState"/>。
    /// 当前模型按"时代"推进（研究点累计到阈值即跨入下一时代），
    /// 科技 id 采用 "era:N" 口径；细粒度科技树留待后续版本。
    /// </summary>
    public sealed class TechService : ITechService
    {
        /// <summary>进入时代 N 所需研究点 = 100 + N*50。</summary>
        public const int BaseProgressRequired = 100;
        public const int ProgressPerEra = 50;

        /// <summary>最高时代（Space）。</summary>
        private const int MaxEra = 9;

        private readonly TechState _state;

        public TechService(TechState state)
        {
            _state = state;
        }

        public Era GetEra(int faction) => (Era)_state.GetEra(faction);

        public bool HasEra(int faction, Era era) =>
            _state.GetEra(faction) >= (int)era;

        public bool IsResearched(int faction, string techId)
        {
            if (TryParseEraId(techId, out int era))
                return _state.GetEra(faction) >= era;
            // 无细粒度科技树：未知 id 一律视为未研究
            return false;
        }

        public float GetProgress(int faction, string techId)
        {
            int current = _state.GetEra(faction);
            if (current >= MaxEra) return 1f;
            float required = ProgressRequired(current);
            return Mathf.Clamp01(_state.GetProgress(faction) / required);
        }

        /// <summary>
        /// 年度推进：把研究点投入当前时代，达阈值则跨时代（溢出结转）。
        /// 返回本次跨入的新时代数。
        /// </summary>
        public int AdvanceResearch(int faction, float researchPoints)
        {
            int levels = 0;
            _state.AddProgress(faction, researchPoints);
            while (_state.GetEra(faction) < MaxEra)
            {
                int current = _state.GetEra(faction);
                float required = ProgressRequired(current);
                if (_state.GetProgress(faction) < required) break;

                float carry = _state.GetProgress(faction) - required;
                _state.SetEra(faction, current + 1, carry);
                levels++;
            }
            return levels;
        }

        /// <summary>进入当前时代所需研究点。</summary>
        public static float ProgressRequired(int currentEra) =>
            BaseProgressRequired + currentEra * ProgressPerEra;

        private static bool TryParseEraId(string techId, out int era)
        {
            era = 0;
            if (string.IsNullOrEmpty(techId)) return false;
            if (techId.StartsWith("era:", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(techId.Substring(4), out era);
            return false;
        }
    }
}
