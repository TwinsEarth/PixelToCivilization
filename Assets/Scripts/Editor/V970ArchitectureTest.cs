#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using PixelToCivilization.Core;

namespace PixelToCivilization.EditorTools
{
    /// <summary>
    /// V9.7.0 架构测试（EditMode，纯数据逻辑，不依赖场景；菜单「像素到文明 → ⑪ V9.7.0 架构测试」运行）。
    /// 覆盖 V9.7.0 五件契约：
    ///  1) 对象池 Rent/Return 复用：Rent 后 Rented 计数增、Return 后进空闲栈（无 Instantiate 尖峰路径可验证）
    ///  2) Addressables 适配层镜像配对：Load/Release 配对 → LiveCount 归零；未配对 → LiveCount 保留（泄漏可探）
    ///  3) SoA 人口聚合：连续数组桶 Feed → total/bands/jobs 全对（数据导向聚合正确性）
    ///  4) 存档脏标记：初始脏 → 无变化跳过自动档 → ForceDirty 恢复（dirty flag 节流契约）
    ///  5) 崩溃上下文：SetContext/GetContext 往返 + 落盘标记（崩溃时游戏状态可定位）
    /// 输出：每项 PASS/FAIL + 实测数值；任意 FAIL 立即标红（批处理 -executeMethod 可跑）。
    /// </summary>
    public static class V970ArchitectureTest
    {
        static int _pass, _fail;

        [MenuItem("像素到文明/⑪ V9.7.0 架构测试")]
        public static void RunAll()
        {
            _pass = 0; _fail = 0;
            Debug.Log("===== [V970-TEST] V9.7.0 架构测试开始 =====");

            PoolRentReturn();
            AddressablesMirrorPairing();
            SoAAggregation();
            DirtyFlagContract();
            CrashContextRoundtrip();

            Debug.Log("===== [V970-TEST] 完成：PASS=" + _pass + " FAIL=" + _fail + " =====");
            if (_fail > 0) Debug.LogError("[V970-TEST] 存在失败项，禁止发布");
        }

        // 1) 对象池复用
        static void PoolRentReturn()
        {
            GameObjectPool.ClearAll();
            GameObjectPool.SetCapacity("test-fx", 16);
            var go1 = GameObjectPool.Rent("test-fx", null, () => new GameObject("fx"));
            bool rentedActive = go1 != null && go1.activeSelf;
            GameObjectPool.Return("test-fx", go1);
            bool returnedPooled = !go1.activeSelf;
            var go2 = GameObjectPool.Rent("test-fx", null, () => new GameObject("fx2"));
            bool reuse = go2 == go1;    // 复用同一实例（无新建）
            Assert("1 对象池复用", rentedActive && returnedPooled && reuse,
                "reuse=" + reuse + " rentedActive=" + rentedActive + " returnedPooled=" + returnedPooled);
            GameObjectPool.Return("test-fx", go2);
            GameObjectPool.ClearAll();
        }

        // 2) Addressables 适配层镜像配对（未装包 → Resources 降级路径；配对契约与装包一致）
        static void AddressablesMirrorPairing()
        {
            int before = AddressablesManager.LiveCount;
            var a = AddressablesManager.Load<UnityEngine.Object>("test-tex");
            int afterLoad = AddressablesManager.LiveCount;
            AddressablesManager.Release("test-tex");
            int afterRelease = AddressablesManager.LiveCount;
            AddressablesManager.Load<UnityEngine.Object>("test-tex");
            int leaked = AddressablesManager.LiveCount;   // 故意不释放 → 残留可探
            AddressablesManager.Release("test-tex");
            bool ok = afterLoad == before + 1 && afterRelease == before && leaked == before + 1;
            Assert("2 适配层镜像配对", ok, "load+1=" + (afterLoad == before + 1) + " rel归零=" + (afterRelease == before) + " 泄漏可探=" + (leaked == before + 1));
        }

        // 3) SoA 人口聚合
        static void SoAAggregation()
        {
            SoAPopulationStore.Reset();
            SoAPopulationStore.FeedOne(0, 1, 0, 100);  // 势力0 壮年 农业 100
            SoAPopulationStore.FeedOne(0, 0, 2, 30);   // 势力0 幼年 商业 30
            SoAPopulationStore.FeedOne(1, 2, 4, 20);   // 势力1 老年 军事 20
            SoAPopulationStore.FeedOne(0, 1, 0, 50);   // 再喂 50 → 农业合计150
            bool ok = SoAPopulationStore.Total == 200
                      && SoAPopulationStore.FactionTotal(0) == 180
                      && SoAPopulationStore.BandTotal(0) == 30
                      && SoAPopulationStore.BandTotal(1) == 150
                      && SoAPopulationStore.BandTotal(2) == 20
                      && SoAPopulationStore.JobTotal(0) == 150
                      && SoAPopulationStore.JobTotal(4) == 20;
            Assert("3 SoA人口聚合", ok, SoAPopulationStore.Probe());
        }

        // 4) 存档脏标记契约
        static void DirtyFlagContract()
        {
            SaveDirtySystem.Enabled = true;
            var s = new GameState();
            s.Year = 1700; s.Pop = 500; s.Day = 1f; s.Housing = 10; s.DynastyIdx = 3;
            s.Res["gold"] = 1000000d; s.Res["food"] = 500000d;
            SaveDirtySystem.UpdateCheck(s);                 // 首次：脏
            bool initiallyDirty = SaveDirtySystem.IsDirty;
            SaveDirtySystem.UpdateCheck(s);                 // 无变化
            bool skip = SaveDirtySystem.ShouldSkipAutoSave(); // 应跳过
            SaveDirtySystem.MarkClean();
            s.Pop = 501;                                    // 变化
            SaveDirtySystem.UpdateCheck(s);
            bool dirtyAfterChange = SaveDirtySystem.IsDirty;
            bool noSkip = !SaveDirtySystem.ShouldSkipAutoSave();
            Assert("4 存档脏标记", initiallyDirty && skip && dirtyAfterChange && noSkip,
                "initDirty=" + initiallyDirty + " skip=" + skip + " afterChangeDirty=" + dirtyAfterChange + " noSkip=" + noSkip);
        }

        // 5) 崩溃上下文往返
        static void CrashContextRoundtrip()
        {
            LocalCrashReporter.SetContext("dynasty", "测试朝");
            LocalCrashReporter.SetContext("year", "1700");
            LocalCrashReporter.SetContext("pop", "500");
            bool ok = LocalCrashReporter.GetContext("dynasty") == "测试朝"
                      && LocalCrashReporter.GetContext("year") == "1700"
                      && LocalCrashReporter.GetContext("pop") == "500"
                      && LocalCrashReporter.Probe().Contains("ctx:dyn=测试朝");
            Assert("5 崩溃上下文", ok, LocalCrashReporter.Probe());
            // 清理测试键（不污染真实上下文）
            PlayerPrefs.DeleteKey("PxC_CrashCtx_dynasty");
            PlayerPrefs.DeleteKey("PxC_CrashCtx_year");
            PlayerPrefs.DeleteKey("PxC_CrashCtx_pop");
            PlayerPrefs.DeleteKey("PxC_CrashCtx_factions");
            PlayerPrefs.DeleteKey("PxC_CrashCtx_era");
            PlayerPrefs.DeleteKey("PxC_CrashCtx_speed");
            PlayerPrefs.Save();
        }

        static void Assert(string name, bool ok, string detail)
        {
            if (ok) { _pass++; Debug.Log("[V970-TEST] PASS " + name + " | " + detail); }
            else { _fail++; Debug.LogError("[V970-TEST] FAIL " + name + " | " + detail); }
        }
    }
}
#endif
