using System;

namespace PxC.Plugins.Naval
{
    /// <summary>
    /// 船艇领域状态（SoA，按 UnitRegistry slot 索引）。
    /// 位置/HP/速度/等级/AI 状态在 UnitRegistry，本类只持有船艇额外字段。
    /// </summary>
    public sealed class NavalState
    {
        private bool[] _isNaval;
        private int[] _xp;
        private float[] _cooldown;

        public NavalState(int initialCapacity = 128)
        {
            int cap = Math.Max(32, initialCapacity);
            _isNaval = new bool[cap];
            _xp = new int[cap];
            _cooldown = new float[cap];
        }

        public int Capacity => _isNaval.Length;

        public void EnsureCapacity(int requiredSlot)
        {
            if (requiredSlot < _isNaval.Length) return;
            int newCap = _isNaval.Length;
            while (newCap <= requiredSlot) newCap *= 2;
            Array.Resize(ref _isNaval, newCap);
            Array.Resize(ref _xp, newCap);
            Array.Resize(ref _cooldown, newCap);
        }

        public void BindSpawn(int slot)
        {
            EnsureCapacity(slot);
            _isNaval[slot] = true;
            _xp[slot] = 0;
            _cooldown[slot] = 0f;
        }

        public void OnKill(int slot)
        {
            if (slot < 0 || slot >= _isNaval.Length) return;
            _isNaval[slot] = false;
            _xp[slot] = 0;
            _cooldown[slot] = 0f;
        }

        public bool IsNaval(int slot) =>
            slot >= 0 && slot < _isNaval.Length && _isNaval[slot];

        public int GetXp(int slot) => _xp[slot];
        public void AddXp(int slot, int v) => _xp[slot] += v;
        public void SetXp(int slot, int v) => _xp[slot] = v;

        public float GetCooldown(int slot) => _cooldown[slot];
        public void SetCooldown(int slot, float v) => _cooldown[slot] = v;

        public void Clear()
        {
            Array.Clear(_isNaval, 0, _isNaval.Length);
            Array.Clear(_xp, 0, _xp.Length);
            Array.Clear(_cooldown, 0, _cooldown.Length);
        }
    }
}
