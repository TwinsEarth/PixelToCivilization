// WebGL 平台下该编辑器冒烟工具不参与编译（其强引用的 EventSystems 在切平台增量编译时偶发缺失，且与网页构建无关）；其余平台保持可用。
#if UNITY_EDITOR && !UNITY_WEBGL
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using PixelToCivilization.Core;

namespace PixelToCivilization.EditorTools
{
    /// <summary>
    /// V601 runtime smoke test for batchmode (-executeMethod, 不带 -quit)：
    /// 编辑模式只新建空场景并放入 <see cref="V601SmokeDriver"/>，随后进入播放模式；
    /// 由驱动在真正的运行态（Application.isPlaying=true）完成：
    /// 自动启动 → 开新局（上古 year1·era0 开局）→ 建 11 类建筑 →
    /// 用 DebugJumpTo 按时代边界逐年补结到 year5100，覆盖全部 8 个时代 →
    /// 检测异常、历史事件、空网格、粉色（InternalError/FallbackError）材质。
    /// 必须在播放模式运行，否则生产代码里的 Object.Destroy 在编辑模式会刷屏
    /// “Destroy may not be called from edit mode!” 并使对象堆积、日志暴涨。
    /// 长年代模拟走 DebugJumpTo（逐年 OnYearAdvanced 补结），与浏览器万年压测同一快路径，
    /// 不逐帧跑全系统实时 Tick（后者过重，会把主线程卡住数十分）。
    /// 结束时驱动以退出码 0（[SMOKE_PASS]）/ 1（[SMOKE_FAIL]）结束编辑器。
    /// </summary>
    public static class V601SmokeTest
    {
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var driverGo = new GameObject("V601SmokeDriver");
            driverGo.AddComponent<V601SmokeDriver>();
            // 进入播放模式后，驱动负责执行全部检查并调用 EditorApplication.Exit；
            // 因此批处理命令行不要加 -quit（否则静态方法一返回编辑器就退出）。
            EditorApplication.EnterPlaymode();
        }
    }

    /// <summary>播放模式冒烟驱动（Update 状态机）：在真实运行态完成启动/建造/长年代模拟/渲染体检。</summary>
    public class V601SmokeDriver : MonoBehaviour
    {
        enum Stage { WaitBoot, Settle, StartGame, AfterStart, Populate, AfterPopulate,
                     UiChecks, Build, Simulate, Render, Done }

        Stage _stage = Stage.WaitBoot;
        int _frame, _built;
        // 时代边界（游戏年）：跳至该年时对应时代切到下一 era；末尾 5100 已在 era7
        static readonly int[] JumpTargets = { 2300, 3581, 3960, 4368, 4912, 4949, 5050, 5100 };
        int _jumpIdx = -1;
        int _pinkMat, _totalMat, _emptyMesh, _totalRenderers;
        bool _finished;

        void Update()
        {
            if (_finished) return;
            try
            {
                GameManager gm;
                switch (_stage)
                {
                    case Stage.WaitBoot:
                        // 等待 [RuntimeInitializeOnLoadMethod] AutoBoot 完成（它在 AfterSceneLoad 同步 Boot）
                        if (GameManager.Instance != null) { _stage = Stage.Settle; }
                        else if (++_frame > 60) throw new Exception("GameManager never bootstrapped in play mode");
                        break;

                    case Stage.Settle:
                        if (++_frame >= 1) { _stage = Stage.StartGame; _frame = 0; }
                        break;

                    case Stage.StartGame:
                        gm = GameManager.Instance;
                        if (gm == null) throw new Exception("GameManager vanished before StartNewGame");
                        gm.StartNewGame();
                        // 覆盖为上古开局（游戏年1·era0），使后续 DebugJumpTo 覆盖全部 8 个时代。
                        // SnapToStartYear 只静默改 Year/Era/DynastyIdx、不授任何资源，故回退是干净的。
                        gm.State.Year = 1; gm.State.Day = 0; gm.State.Era = 0; gm.State.DynastyIdx = 0;
                        _stage = Stage.AfterStart;
                        break;

                    case Stage.AfterStart:
                        if (++_frame >= 1) { _stage = Stage.Populate; _frame = 0; }
                        break;

                    case Stage.Populate:
                        gm = GameManager.Instance;
                        gm.Env.PopulateInitial();
                        Debug.Log("[SMOKE] scene + initial forest/population done (era0 ancient start)");
                        _stage = Stage.AfterPopulate;
                        break;

                    case Stage.AfterPopulate:
                        if (++_frame >= 1) { _stage = Stage.UiChecks; _frame = 0; }
                        break;

                    case Stage.UiChecks:
                        RunUiChecks();
                        RunDesignDocChecks();
                        _stage = Stage.Build;
                        break;

                    case Stage.Build:
                        RunBuilds();
                        // 禁用 GameManager 自身 Update（它会驱动 Time/各系统 Tick 干扰模拟）；
                        // 不能用 State.Paused=true——GameTime.Tick 开头检测到 Paused 会直接早退、年份不推进。
                        GameManager.Instance.enabled = false;
                        _stage = Stage.Simulate;
                        _jumpIdx = -1;
                        LogEra(0); // 记录初始 era0
                        break;

                    case Stage.Simulate:
                        gm = GameManager.Instance;
                        // 每帧跳一个时代边界，逐帧记录 era 推进，避免一次长跳阻塞主线程
                        _jumpIdx++;
                        if (_jumpIdx < JumpTargets.Length)
                        {
                            gm.Time.DebugJumpTo(JumpTargets[_jumpIdx]);
                            LogEra(gm.State.Era);
                        }
                        if (_jumpIdx >= JumpTargets.Length - 1)
                        {
                            if (gm.State.Year < 5100) throw new Exception("simulation did not reach year 5100");
                            if (gm.State.Era != 7) throw new Exception("final era not 7 (Earth Alliance), got " + gm.State.Era);
                            Debug.Log("[SMOKE] simulation done: year=" + gm.State.Year +
                                      " era=" + gm.State.Era + " events=" + gm.State.FiredEvents.Count +
                                      " corruption=" + Mathf.RoundToInt(gm.State.Corruption) +
                                      " monarch=" + (gm.State.MonarchWise ? "wise" : "foolish"));
                            if (gm.State.FiredEvents.Count == 0)
                                throw new Exception("no history events fired across 5100 years");
                            _stage = Stage.Render;
                        }
                        break;

                    case Stage.Render:
                        RunRenderChecks();
                        _stage = Stage.Done;
                        break;

                    case Stage.Done:
                        Debug.Log("[SMOKE_PASS] V601 runtime smoke all passed");
                        Finish(true);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[SMOKE_FAIL] " + e);
                Finish(false);
            }
        }

        void LogEra(int era)
        {
            var gm = GameManager.Instance;
            Debug.Log("[SMOKE] era=" + era + " year=" + gm.State.Year +
                      " greg=" + (gm.Time != null ? gm.Time.GregorianText : "-") +
                      " pop=" + gm.State.Pop);
        }

        // UI raycast 体检：全屏容器层不得拦截指针，否则地图点击/相机旋转失效
        void RunUiChecks()
        {
            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                throw new Exception("EventSystem missing - UI cannot receive clicks");
            var hudGo = GameObject.Find("HUD");
            if (hudGo == null) throw new Exception("HUD not found (should be active after StartNewGame)");
            var hudImg = hudGo.GetComponent<UnityEngine.UI.Image>();
            if (hudImg != null && hudImg.raycastTarget)
                throw new Exception("HUD fullscreen Image raycastTarget=true - blocks all map clicks");
            var mlGo = GameObject.Find("ModalLayer");
            if (mlGo != null)
            {
                var mlImg = mlGo.GetComponent<UnityEngine.UI.Image>();
                if (mlImg != null && mlImg.raycastTarget)
                    throw new Exception("ModalLayer fullscreen Image raycastTarget=true - blocks all map clicks");
            }
            Debug.Log("[SMOKE] UI raycast sanity passed (EventSystem + transparent layers)");
        }

        // 设计文档扩展系统：学派 / 灾荒 / 历史事件 / 胜利
        void RunDesignDocChecks()
        {
            var gm = GameManager.Instance;
            if (gm.Philosophy == null || gm.Disaster == null || gm.HistoryEvent == null || gm.Victory == null)
                throw new Exception("design-doc systems not installed (Philosophy/Disaster/HistoryEvent/Victory)");
            gm.Philosophy.Adopt("fa");
            if (gm.State.Philosophy != "fa") throw new Exception("Philosophy.Adopt failed");
            Debug.Log("[SMOKE] design-doc systems installed (philosophy adopted=fa)");
        }

        // 跨类别真实建造
        void RunBuilds()
        {
            var gm = GameManager.Instance;
            string[] types = { "hut", "farm", "palace", "arrow_tower", "pagoda", "road", "canal",
                               "watchtower", "great_wall", "skyscraper", "space_elevator" };
            foreach (var t in types)
            {
                for (int k = 0; k < 6; k++)
                    if (gm.Building.FindAutoPosition(t, out var x, out var z) &&
                        gm.Building.PlaceBuilding(t, x, z)) { _built++; break; }
            }
            Debug.Log("[SMOKE] real builds=" + _built + "/" + types.Length);
            if (_built < types.Length)
                Debug.LogWarning("[SMOKE] some types not built (era/tech/resource gated, non-fatal)");
        }

        // 渲染资产健康检查
        void RunRenderChecks()
        {
            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>(true))
            {
                _totalRenderers++;
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.vertexCount == 0) _emptyMesh++;
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    _totalMat++;
                    if (m.shader == null || m.shader.name.Contains("InternalError") ||
                        m.shader.name.Contains("FallbackError"))
                    {
                        _pinkMat++;
                        Debug.LogWarning("[SMOKE] pink material: " + r.name +
                                         " shader=" + (m.shader ? m.shader.name : "null"));
                    }
                }
            }
            Debug.Log("[SMOKE] Renderers=" + _totalRenderers + " materials=" + _totalMat +
                      " pink=" + _pinkMat + " emptyMesh=" + _emptyMesh);
            if (_pinkMat > 0) throw new Exception("pink materials: " + _pinkMat);
            if (_emptyMesh > 0) throw new Exception("empty meshes: " + _emptyMesh);
        }

        void Finish(bool pass)
        {
            if (_finished) return;
            _finished = true;
            StartCoroutine(QuitNextFrame(pass));
        }

        System.Collections.IEnumerator QuitNextFrame(bool pass)
        {
            yield return null;
            yield return null;
            EditorApplication.Exit(pass ? 0 : 1);
        }
    }
}
#endif
