namespace PxC.Core.World
{
    /// <summary>空间块加载状态。</summary>
    public enum ChunkState
    {
        Unloaded = 0,
        Loading = 1,
        Active = 2,
        Unloading = 3
    }

    /// <summary>
    /// 空间块：按坐标分块，持有该区域内的实体句柄。
    /// 只有 Active 块需要驻留数据/视图，用于按需加载与卸载。
    /// </summary>
    public sealed class Chunk
    {
        public readonly int X;
        public readonly int Y;
        public ChunkState State;

        private StructArray<EntityId> _entities;

        public Chunk(int x, int y, int estimatedEntities = 16)
        {
            X = x;
            Y = y;
            State = ChunkState.Unloaded;
            _entities = new StructArray<EntityId>(estimatedEntities);
        }

        public int EntityCount => _entities.Count;

        public EntityId GetEntity(int index) => _entities[index];

        public void AddEntity(EntityId id) => _entities.Add(id);

        public void RemoveEntity(EntityId id)
        {
            for (int i = 0; i < _entities.Count; i++)
            {
                if (_entities[i] == id)
                {
                    _entities.RemoveSwapBack(i);
                    return;
                }
            }
        }

        public bool Contains(EntityId id)
        {
            for (int i = 0; i < _entities.Count; i++)
                if (_entities[i] == id) return true;
            return false;
        }

        public void OnUnloaded()
        {
            _entities.Clear();
            State = ChunkState.Unloaded;
        }

        public static int Key(int x, int y) => (x * 73856093) ^ (y * 19349663);
    }
}
