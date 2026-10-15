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
}
