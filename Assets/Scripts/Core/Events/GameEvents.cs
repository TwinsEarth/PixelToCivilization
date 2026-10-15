using UnityEngine;
using PxC.Core.World;

namespace PxC.Core.Events
{
    // 所有事件均为 struct，按类型订阅，杜绝字符串弱类型与每事件 GC。

    public struct YearAdvancedEvent
    {
        public int GameYear;
    }

    public struct EraChangedEvent
    {
        public int NewEra;
        public int OldEra;
        public int GameYear;
    }

    public struct DynastyChangedEvent
    {
        public int DynastyIndex;
        public int GameYear;
    }

    public struct EntitySpawnedEvent
    {
        public EntityId Id;
        public EntityDomain Domain;
    }

    public struct EntityDiedEvent
    {
        public EntityId Id;
        public int Reason;
    }

    public struct WarDeclaredEvent
    {
        public int AttackerFaction;
        public int DefenderFaction;
    }

        /// <summary>势力被灭国：战争中一方作战单位被全部消灭（Faction=败者，ByFaction=胜者）。</summary>
    public struct FactionEliminatedEvent
    {
        public int Faction;
        public int ByFaction;
        public int GameYear;
    }

    /// <summary>灭国领土占领：败者聚落/建筑归属胜者（Settlements/Buildings 为转移数量）。</summary>
    public struct TerritoryConqueredEvent
    {
        public int Loser;
        public int Winner;
        public int Settlements;
        public int Buildings;
        public int GameYear;
    }

    /// <summary>和约/停战：僵持战争在持续若干年后双方停战（无胜负，仅结束战争）。</summary>
    public struct PeaceTreatyEvent
    {
        public int FactionA;
        public int FactionB;
        public int WarStartYear;
        public int GameYear;
    }

    public struct ProjectileFiredEvent
    {
        public int ProjectileType;
        public EntityId Source;
        public EntityId Target;
    }

    public struct ProjectileHitEvent
    {
        public int ProjectileType;
        public EntityId Target;
        public float Damage;
    }

    public struct ChunkStateChangedEvent
    {
        public int X;
        public int Y;
        public ChunkState State;
    }

    public enum SaveRequestType { Manual, Auto, Rollback, Checkpoint }

    public struct SaveRequestedEvent
    {
        public int Slot;
        public SaveRequestType Type;
    }

    /// <summary>集结令：Sequence 为单调递增序号（幂等/失效旧命令），(X,Y) 为集结目标坐标。</summary>
    public struct RallyOrderedEvent
    {
        public int Sequence;
        public int X;
        public int Y;
    }

    /// <summary>集结令撤销/清理（插新旗前先清理旧命令与临时视图）。</summary>
    public struct RallyCancelledEvent
    {
        public int Sequence;
    }

    /// <summary>饥荒：粮食库存无法满足年度需求，Shortfall 为未满足粮食量（population 据此结算死亡）。</summary>
    public struct FamineEvent
    {
        public int Faction;
        public float Shortfall;
        public int GameYear;
    }

    /// <summary>人口年度结算后：Births/Deaths 为当年出生/死亡数（含饥荒）。</summary>
    public struct PopulationChangedEvent
    {
        public int Faction;
        public int Births;
        public int Deaths;
        public int NewTotal;
    }

    /// <summary>天气变化：当年主导天气切换（视图据此播放晴/雨/雪/风粒子，按需加载）。</summary>
    public struct WeatherChangedEvent
    {
        public int NewWeather;
        public float Intensity;
        public int GameYear;
    }

    /// <summary>灾害发生：Type=DisasterType，Severity[0,1]，附影响（人/建筑/粮食）。</summary>
    public struct DisasterOccurredEvent
    {
        public int Type;
        public int Faction;
        public float Severity;
        public int GameYear;
        public int Deaths;
        public int BuildingsDestroyed;
        public float FoodLost;
    }

    /// <summary>国策切换：OldPolicy→NewPolicy。</summary>
    public struct PolicyChangedEvent
    {
        public int Faction;
        public int NewPolicy;
        public int OldPolicy;
        public int GameYear;
    }

    /// <summary>文化等级提升。</summary>
    public struct CultureLevelUpEvent
    {
        public int Faction;
        public int NewLevel;
        public int OldLevel;
        public int GameYear;
    }

    /// <summary>胜利达成：Type=VictoryType。</summary>
    public struct VictoryEvent
    {
        public int Faction;
        public int Type;
        public int GameYear;
        public string VictoryName;
    }

    /// <summary>重大历史事件触发（EventsPack 内容包，按公历年份匹配）。</summary>
    public struct HistoricalEventEvent
    {
        public int GameYear;
        public int AdYear;
        public int Kind;
        public string EventName;
        public string Description;
    }

    /// <summary>新聚落建立（殖民/扩张/自动扩张）：Kind=SettlementKind，含位置与聚落名。</summary>
    public struct SettlementFoundedEvent
    {
        public int Faction;
        public int Kind;
        public int GameYear;
        public Vector3 Position;
        public string SettlementName;
    }
}
