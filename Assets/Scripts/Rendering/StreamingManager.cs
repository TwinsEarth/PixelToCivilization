using UnityEngine;

namespace PixelToCivilization.Rendering
{
    /// <summary>
    /// V9.8.9 按需加载与内存回收中枢：
    /// · 按需加载（流式开关）：装饰/生态层实体（鸟群、鱼群等）离相机 &gt; ActiveRadius 即休眠
    ///   （SetActive(false)——渲染与 Update 天然跳过，等于卸载视图），进入半径自动唤醒；
    ///   数据层保留在内存，模拟状态不丢失（"记忆共享机制"兜底），唤醒后无缝续跑。
    /// · 空间分区：CellSize 网格划分世界，Cell() 用于探针/性能神统计活跃与休眠实体分布
    ///   （激活判定直接用距离，避免网格边界抖动导致频繁开关）。
    /// · 用完即卸：休眠视图按"最远优先"回收到对象池（配合 GameObjectPool.ShrinkIdle 全局缩容空闲实例）；
    ///   特效层已由 WeaponFxSystem.Visible 85 世界单位闸门 + FxLifetime 回收（V9.5.3/V9.6.4 已有）。
    /// 阈值口径：ActiveRadius=1100 世界单位（≈275 格）≈ 相机视野 + 预加载余量，普通笔记本可流畅。
    /// </summary>
    public static class StreamingManager
    {
        /// <summary>生态层活跃半径（世界单位）。相机在此半径内实体保持完整模拟+渲染，之外休眠。</summary>
        public const float ActiveRadius = 1100f;

        /// <summary>空间分区边长（世界单位）。仅用于统计与报告，不参与激活判定。</summary>
        public const float CellSize = 400f;

        /// <summary>位置是否位于活跃区（距相机 &lt; radius）。无相机（编辑器/启动期）恒活跃，保证兼容。</summary>
        public static bool IsActive(Vector3 p, float radius = ActiveRadius)
        {
            var cam = Camera.main;
            if (cam == null) return true;
            return Vector3.Distance(cam.transform.position, p) < radius;
        }

        /// <summary>当前位置所在分区名（探针/报告用），如 "3_-2"。</summary>
        public static string Cell(Vector3 p)
        {
            return ((int)(p.x / CellSize)) + "_" + ((int)(p.z / CellSize));
        }

        /// <summary>
        /// 供调用方在 1Hz 节流中执行的休眠/唤醒开关：
        /// go 当前激活 &amp; 位置出活跃区 → SetActive(false) 返回 true（本次发生了休眠）；
        /// go 当前休眠 &amp; 位置进活跃区 → SetActive(true) 返回 false（本次发生了唤醒）。
        /// 状态未变化时返回 null。
        /// </summary>
        public static bool? StreamToggle(GameObject go, Vector3 pos, float radius = ActiveRadius)
        {
            if (go == null) return null;
            bool active = StreamingManager.IsActive(pos, radius);
            if (go.activeSelf && !active) { go.SetActive(false); return true; }   // 休眠（卸载视图）
            if (!go.activeSelf && active) { go.SetActive(true); return false; }   // 唤醒（按需加载）
            return null;
        }

        /// <summary>一行文本报告：活跃半径与当前相机分区（探针/帮助用）。</summary>
        public static string Report()
        {
            var cam = Camera.main;
            string cell = cam != null ? Cell(cam.transform.position) : "?";
            return "[Streaming] activeR=" + ActiveRadius + " cellSize=" + CellSize + " camCell=" + cell;
        }
    }
}
