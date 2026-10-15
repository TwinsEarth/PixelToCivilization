using System.Collections.Generic;

namespace PxC.Core.World
{
    /// <summary>
    /// 数据块世界：管理全部 Chunk 的加载状态与空间查询。
    /// 不含资源加载实现；Streaming 系统根据中心与半径调用本类决定加载/卸载集，
    /// 并订阅 Chunk 状态变化执行实际 IO。
    /// </summary>
    public sealed class ChunkedWorld
    {
        private readonly Dictionary<int, Chunk> _chunks = new Dictionary<int, Chunk>(256);
        private readonly List<Chunk> _active = new List<Chunk>(64);

        public IReadOnlyList<Chunk> ActiveChunks => _active;

        public Chunk GetOrCreateChunk(int x, int y)
        {
            int key = Chunk.Key(x, y);
            if (_chunks.TryGetValue(key, out var c)) return c;
            c = new Chunk(x, y);
            _chunks[key] = c;
            return c;
        }

        public Chunk GetChunk(int x, int y)
        {
            int key = Chunk.Key(x, y);
            return _chunks.TryGetValue(key, out var c) ? c : null;
        }

        public void SetState(int x, int y, ChunkState state)
        {
            var c = GetOrCreateChunk(x, y);
            SetState(c, state);
        }

        public void SetState(Chunk c, ChunkState state)
        {
            bool wasActive = c.State == ChunkState.Active;
            bool nowActive = state == ChunkState.Active;
            c.State = state;
            if (state == ChunkState.Unloaded) c.OnUnloaded();
            if (!wasActive && nowActive && !_active.Contains(c)) _active.Add(c);
            if (wasActive && !nowActive) _active.Remove(c);
        }

        /// <summary>
        /// 计算以 (cx,cy) 为中心、半径 radius 内的块坐标（应保持 Active）。
        /// 结果填入 coords（复用列表，避免每帧分配）。
        /// </summary>
        public void CollectInRadius(int cx, int cy, int radius, List<(int x, int y)> coords)
        {
            coords.Clear();
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // 圆形范围
                    if (dx * dx + dy * dy <= radius * radius + radius)
                        coords.Add((cx + dx, cy + dy));
                }
            }
        }

        /// <summary>清空所有块（卸载/重置世界）。</summary>
        public void Clear()
        {
            _chunks.Clear();
            _active.Clear();
        }
    }
}
