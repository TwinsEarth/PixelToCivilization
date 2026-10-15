namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 地形/水陆查询（地图生成插件实现）。地面单位移动前据此校验目标点是否为陆地，
    /// 从根本上杜绝"骑兵被拉进湖里"。真实水陆数据由地图插件绑定；
    /// 未接线时使用 <see cref="PermissiveGeography"/>（全部按陆地处理，未验证）。
    /// </summary>
    public interface IGeographyQuery
    {
        /// <summary>该点是否为陆地（可通行）。</summary>
        bool IsLand(float x, float z);

        /// <summary>该点是否为水域（陆地单位禁入）。</summary>
        bool IsWater(float x, float z);

        /// <summary>该点的地表高度（用于单位贴地）。</summary>
        float TerrainHeight(float x, float z);
    }

    /// <summary>
    /// 宽松占位实现：全部视为高度 0 的陆地。仅在地图插件未接线时使用，
    /// 此时"不进湖"完全依赖调用方不给出水域坐标（未验证，需地图插件补齐）。
    /// </summary>
    public sealed class PermissiveGeography : IGeographyQuery
    {
        public bool IsLand(float x, float z) => true;
        public bool IsWater(float x, float z) => false;
        public float TerrainHeight(float x, float z) => 0f;
    }
}
