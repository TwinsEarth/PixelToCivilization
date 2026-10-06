#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PixelToCivilization.Core;

namespace PixelToCivilization.EditorTools
{
    /// <summary>
    /// V9.6.4 内存架构冒烟测试（EditMode，不依赖场景；菜单「像素到文明 → ⑨ 内存架构冒烟测试」运行）。
    /// 覆盖四类内存契约：
    ///  1) 对象池预生长 Warmup → Rent/Return 复用同一实例（防 Instantiate 尖峰、防重复新建）
    ///  2) 对象池 ClearAll 后无残留（世界重置资源卸载契约）
    ///  3) 槽位摘要 SlotSummaryData 序列化往返（存档列表 GC 治理的数据契约）
    ///  4) 大档 JSON 序列化分配有界（GC Alloc 校验：2000 实体快照往返托管增量 < 启发阈值）
    /// 输出：每项 PASS/FAIL + 实测数值；任意 FAIL 立即标红并给出退出码线索。
    /// </summary>
    public static class MemorySmokeTest
    {
        static int _pass, _fail;

        [MenuItem("像素到文明/⑨ 内存架构冒烟测试")]
        public static void RunAll()
        {
            _pass = 0; _fail = 0;
            Debug.Log("===== [MEM-TEST] V9.6.4 内存架构冒烟测试开始 =====");

            PoolReuseTest();
            PoolClearTest();
            SummaryRoundTripTest();
            SnapshotAllocTest();

            Debug.Log(_fail == 0
                ? $"===== [MEM-TEST] 全部通过（PASS={_pass} FAIL=0）====="
                : $"===== [MEM-TEST] 存在失败（PASS={_pass} FAIL={_fail}）→ 修复后重跑 =====");
        }

        // 1) 池复用：Warmup 5 个 → Rent 取 1 个记引用 → Return → 再次 Rent 应返回同一实例
        static void PoolReuseTest()
        {
            var root = new GameObject("MemTestRoot");
            try
            {
                GameObjectPool.Warmup("memtest", root.transform, () => new GameObject("mt"), 5);
                var a = GameObjectPool.Rent("memtest", root.transform, () => new GameObject("mt"));
                var id = a.GetInstanceID();
                GameObjectPool.Return("memtest", a);
                var b = GameObjectPool.Rent("memtest", root.transform, () => new GameObject("mt"));
                Check("池复用(Warmup→Rent→Return→Rent 同实例)", b.GetInstanceID() == id, $"first={id} reused={b.GetInstanceID()}");
            }
            finally { GameObjectPool.ClearAll(); UnityEngine.Object.DestroyImmediate(root); }
        }

        // 2) 池清空：Rent 若干 → ClearAll → 分桶统计全空（资源卸载契约）
        static void PoolClearTest()
        {
            var root = new GameObject("MemTestRoot2");
            try
            {
                for (int i = 0; i < 10; i++) GameObjectPool.Rent("memtest2", root.transform, () => new GameObject("mt2"));
                var before = GameObjectPool.StatsDetailed();
                GameObjectPool.ClearAll();
                var after = GameObjectPool.StatsDetailed();
                Check("池清空(ClearAll 无残留)", after.Count == 0 && GameObjectPool.BucketCount() == 0,
                    $"before_buckets={before.Count} after_buckets={after.Count}");
            }
            finally { GameObjectPool.ClearAll(); UnityEngine.Object.DestroyImmediate(root); }
        }

        // 3) 槽位摘要往返：SlotSummaryData 序列化 → 反序列化 → 字段一致（存档列表 GC 治理数据契约）
        static void SummaryRoundTripTest()
        {
            var src = new SlotSummaryData
            {
                Slot = 3, Year = 2026, DynastyName = "新地球联盟",
                BuildingCount = 42, SaveTime = 123456789L, SlotName = "存档3"
            };
            var json = JsonUtility.ToJson(src);
            var back = JsonUtility.FromJson<SlotSummaryData>(json);
            bool ok = back != null && back.Slot == 3 && back.Year == 2026
                   && back.DynastyName == "新地球联盟" && back.BuildingCount == 42
                   && back.SaveTime == 123456789L && back.SlotName == "存档3";
            Check("槽位摘要往返(SlotSummaryData JSON 一致)", ok, $"json_len={json.Length} round={back != null}");
        }

        // 4) 大档序列化分配有界：构造 2000 实体 SaveData → ToJson → FromJson，托管增量 < 启发阈值 16MB
        static void SnapshotAllocTest()
        {
            var d = new SaveData();
            d.AgentCount = 2000;
            d.AgentClass = new string[2000]; d.AgentJob = new string[2000]; d.AgentAge = new int[2000]; d.AgentSeed = new int[2000];
            d.AgentX = new float[2000]; d.AgentZ = new float[2000]; d.AgentHX = new float[2000]; d.AgentHZ = new float[2000];
            for (int i = 0; i < 2000; i++)
            {
                d.AgentClass[i] = "农"; d.AgentJob[i] = "种植";
                d.AgentAge[i] = i % 60; d.AgentSeed[i] = i; d.AgentX[i] = i; d.AgentZ[i] = -i; d.AgentHX[i] = 1; d.AgentHZ[i] = 1;
            }
            d.BuildingCount = 300;
            d.BType = new string[300]; d.BLvl = new int[300]; d.BX = new float[300]; d.BZ = new float[300]; d.BAge = new int[300]; d.BHp = new float[300];
            for (int i = 0; i < 300; i++) { d.BType[i] = "farm"; d.BLvl[i] = 1 + i % 5; d.BX[i] = i; d.BZ[i] = i; d.BAge[i] = 1; d.BHp[i] = 100; }
            d.ShipCount = 200;
            d.ShipType = new string[200]; d.ShipLvl = new int[200]; d.ShipX = new float[200]; d.ShipZ = new float[200];
            for (int i = 0; i < 200; i++) { d.ShipType[i] = "cannon_ship"; d.ShipLvl[i] = 1; d.ShipX[i] = i; d.ShipZ[i] = -i; }

            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long before = GC.GetTotalMemory(true);
            string json = JsonUtility.ToJson(d);
            var back = JsonUtility.FromJson<SaveData>(json);
            long after = GC.GetTotalMemory(true);
            long delta = after - before;
            const long heuristic = 16L * 1024 * 1024;   // 启发阈值：满档快照一次往返托管增量 < 16MB（实际典型 ~5-10MB）
            Check("大档序列化分配有界(2000实体 GC增量<16MB)", back != null && delta < heuristic,
                $"json_len={json.Length} 托管增量={delta}B 阈值={heuristic}B");
        }

        static void Check(string name, bool ok, string detail)
        {
            if (ok) { _pass++; Debug.Log($"[MEM-TEST] PASS  {name}  {detail}"); }
            else    { _fail++; Debug.LogError($"[MEM-TEST] FAIL  {name}  {detail}"); }
        }
    }
}
#endif
