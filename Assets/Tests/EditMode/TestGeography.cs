using System;
using PxC.Gameplay.Services;

namespace PxC.Tests.EditMode
{
    /// <summary>
    /// 可控水陆地形（测试用）：以整数格子为单位标记水域。
    /// 浮点坐标按四舍五入取整判定。
    /// </summary>
    public sealed class GridGeography : IGeographyQuery
    {
        private const long Offset = 100000L;
        private readonly System.Collections.Generic.HashSet<long> _water =
            new System.Collections.Generic.HashSet<long>();

        public void SetWater(int x, int z) => _water.Add(Key(x, z));

        public void SetWaterRect(int x0, int z0, int x1, int z1)
        {
            for (int x = x0; x <= x1; x++)
                for (int z = z0; z <= z1; z++)
                    SetWater(x, z);
        }

        public void Clear() => _water.Clear();

        public bool IsWater(float x, float z) =>
            _water.Contains(Key((int)Math.Round(x), (int)Math.Round(z)));

        public bool IsLand(float x, float z) => !IsWater(x, z);

        public float TerrainHeight(float x, float z) => 0f;

        private static long Key(int x, int z) => (x + Offset) * (Offset * 2L) + (z + Offset);
    }
}
