using UnityEngine;
using PxC.Gameplay.Entities;

namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 殖民/扩张服务（colonization 插件实现）。集中一切"建立新聚落"的入口：
    /// 检查时代门控、地理（陆地）、资源与移民人口、聚落间距；
    /// 年度自动扩张；并提供领土归属查询（GetOwnerAt）。
    /// </summary>
    public interface IColonizationService
    {
        /// <summary>
        /// 尝试在 position 建立某类型聚落：校验时代、地理、资源、人口、间距，
        /// 全部满足则扣减资源/人口并建立聚落，返回 true。
        /// </summary>
        bool TryFoundSettlement(int faction, SettlementKind kind, Vector3 position, string name);

        /// <summary>
        /// 查询能否建立某类型聚落（不实际建立）；不能时 reason 给出原因。
        /// </summary>
        bool CanFound(int faction, SettlementKind kind, Vector3 position, out string reason);

        /// <summary>
        /// 年度自动扩张：各存活势力按发展能力在合适位置建立新聚落，
        /// 返回本年度新建立的聚落总数。
        /// </summary>
        int AutoExpand(int gameYear);

        /// <summary>
        /// 领土归属查询：返回 position 所在领土的势力 id（取最近覆盖聚落）；
        /// 不在任何聚落领土内返回 -1。
        /// </summary>
        int GetOwnerAt(Vector3 position);

        /// <summary>某势力当前聚落数量。</summary>
        int SettlementCount(int faction);
    }
}
