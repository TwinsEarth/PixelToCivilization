using UnityEngine;

namespace PxC.Gameplay.Services
{
    /// <summary>
    /// 建筑服务（building 插件实现）。负责建造/升级/拆除（成本校验→原子扣费→登记）、
    /// 建筑类型目录与多层收分形制。建筑实体写入 BuildingRegistry。
    /// </summary>
    public interface IBuildingService
    {
        /// <summary>某势力建筑总数。</summary>
        int TotalCount(int faction);

        /// <summary>某势力某类型建筑数量。</summary>
        int CountByType(int faction, int typeId);

        /// <summary>是否可建造（类型存在、已解锁、成本足够、容量未满）。</summary>
        bool CanBuild(int faction, int typeId);

        /// <summary>尝试建造：校验→原子扣费→登记到 BuildingRegistry，返回建筑 slot（失败 -1）。</summary>
        int TryBuild(int faction, int typeId, Vector3 position);

        /// <summary>尝试升级（消耗资源，提升产出/耐久）。</summary>
        bool TryUpgrade(int faction, int slot);

        /// <summary>尝试拆除（返还部分资源，从 BuildingRegistry 移除）。</summary>
        bool TryDemolish(int faction, int slot);

        /// <summary>
        /// 灾害/战争损毁：摧毁某势力至多 count 栋建筑（不返还资源），返回实际摧毁数。
        /// 供 disaster 插件调用（地震/洪水/台风）。
        /// </summary>
        int DestroyByDisaster(int faction, int count);
    }
}
