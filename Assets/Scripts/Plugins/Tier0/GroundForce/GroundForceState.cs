using System;

namespace PxC.Plugins.GroundForce
{
    /// <summary>
    /// 地面部队领域状态（SoA，按 UnitRegistry 的 slot 索引）。
    /// 位置/HP/速度/等级/AI 状态在 UnitRegistry（权威），本类只持有地面单位额外字段：
    /// 是否地面单位、经验、攻击冷却、编队 id。数组随 UnitRegistry 扩容而镜像扩容。
    /// </summary>
    public sealed class GroundForceState
    {
        private bool[] _isGround;
        private int[] _xp;
        private float[] _cooldown;
        private int[] _squad;
        private float[] _pursuitTime;
        private int _cap;

        public GroundForceState(int initialCapacity = 128)
        {
            _cap = Math.Max(32, initialCapacity);
            _isGround = new bool[_cap];
            _xp = new int[_cap];
            _cooldown = new float[_cap];
            _squad = new int[_cap];
            _pursuitTime = new float[_cap];
        }

        /// <summary>当前数组容量（=可索引 slot 上界）。</summary>
        public int Capacity => _cap;

        /// <summary>确保可容纳到 slot <paramref name="requiredSlot"/>（镜像 UnitRegistry 扩容）。</summary>
        public void EnsureCapacity(int requiredSlot)
        {
            if (requiredSlot < _cap) return;
            int newCap = _cap;
            while (newCap <= requiredSlot) newCap *= 2;
            Array.Resize(ref _isGround, newCap);
            Array.Resize(ref _xp, newCap);
            Array.Resize(ref _cooldown, newCap);
            Array.Resize(ref _squad, newCap);
            Array.Resize(ref _pursuitTime, newCap);
            _cap = newCap;
        }

        /// <summary>标记一个新生成的地面单位 slot。</summary>
        public void BindSpawn(int slot, int squadId)
        {
            EnsureCapacity(slot);
            _isGround[slot] = true;
            _xp[slot] = 0;
            _cooldown[slot] = 0f;
            _squad[slot] = squadId;
            _pursuitTime[slot] = 0f;
        }

        /// <summary>单位死亡/移除时清理（slot 可能被 UnitRegistry 复用）。</summary>
        public void OnKill(int slot)
        {
            if (slot < 0 || slot >= _cap) return;
            _isGround[slot] = false;
            _xp[slot] = 0;
            _cooldown[slot] = 0f;
            _squad[slot] = 0;
            _pursuitTime[slot] = 0f;
        }

        public bool IsGround(int slot) =>
            slot >= 0 && slot < _cap && _isGround[slot];

        public int GetXp(int slot) => _xp[slot];
        public void AddXp(int slot, int amount) => _xp[slot] += amount;
        public void SetXp(int slot, int value) => _xp[slot] = value;

        public float GetCooldown(int slot) => _cooldown[slot];
        public void SetCooldown(int slot, float value) => _cooldown[slot] = value;

        public int GetSquad(int slot) => _squad[slot];
        public void SetSquad(int slot, int squadId) => _squad[slot] = squadId;

        /// <summary>追击累计时间（秒）。进入 Pursuing 时重置，用于超时放弃绕圈目标。</summary>
        public float GetPursuitTime(int slot) => _pursuitTime[slot];
        public void AddPursuitTime(int slot, float dt) => _pursuitTime[slot] += dt;
        public void ResetPursuitTime(int slot) => _pursuitTime[slot] = 0f;

        /// <summary>清空全部（新游戏/卸载）。</summary>
        public void Clear()
        {
            Array.Clear(_isGround, 0, _cap);
            Array.Clear(_xp, 0, _cap);
            Array.Clear(_cooldown, 0, _cap);
            Array.Clear(_squad, 0, _cap);
            Array.Clear(_pursuitTime, 0, _cap);
        }
    }
}
