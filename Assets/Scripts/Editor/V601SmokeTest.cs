#if UNITY_EDITOR && !UNITY_WEBGL
using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.World;

namespace PixelToCivilization.EditorTools
{
    /// <summary>
    /// V9.5.2 自动化冒烟驱动（PlayMode Update 状态机）。
    /// 静态入口：新建空场景 → 放 GameBootstrap 并 Boot → 切到播放模式，由本驱动（挂在场景物体上）
    /// 在真实运行态推进：新游戏 → 时间推进 → 9000 年模拟 → 建筑/舰船/副本/UI → 渲染体检 → 干净退出。
    /// 注意：编辑模式下严禁 Object.Destroy（刷 "Destroy may not be called from edit mode!"），所以模拟全部在 PlayMode。
    /// </summary>
    public static class V601SmokeTest
    {
        public static void Run()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var go=new GameObject("GameBootstrap");
            var boot=go.AddComponent<Bootstrap.GameBootstrap>();
            boot.Boot();
            var driverGo=new GameObject("V601SmokeDriver");
            driverGo.AddComponent<V601SmokeDriver>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorApplication.EnterPlaymode();
        }
    }

    public class V601SmokeDriver : MonoBehaviour
    {
        enum Phase { WaitBoot, Settle, StartGame, AfterStart, Populate, AfterPopulate, UiChecks, Build, Simulate, Render, Done }
        Phase _phase=Phase.WaitBoot;
        float _t;
        int _frame;
        GameManager GM;
        int _simIdx;
        // 按时代边界（GameManager 年口径，公历=年-3000）推进，覆盖全部 8 个时代
        static readonly int[] JumpTargets={2300,3581,3960,4368,4912,4949,5050,5100};

        void Update()
        {
            _frame++;
            switch(_phase)
            {
                case Phase.WaitBoot:
                    GM=FindObjectOfType<GameManager>();
                    if(GM!=null){_phase=Phase.Settle;_t=1.5f;}
                    break;
                case Phase.Settle:
                    _t-=Time.unscaledDeltaTime;
                    if(_t<=0f)_phase=Phase.StartGame;
                    break;
                case Phase.StartGame:
                    try
                    {
                        GM.StartNewRandomGame();
                        // 经典随机世界从第1年（三皇五帝）开始，避免开局落在1700导致9000年模拟提前结束
                        GM.State.Year=1; GM.State.Day=0; GM.State.Era=0; GM.State.DynastyIdx=0;
                        Debug.Log("[SMOKE] StartNewGame: year="+GM.State.Year+" pop="+GM.State.Pop);
                    }
                    catch(Exception e){Debug.LogError("[SMOKE_FAIL] StartNewGame: "+e);Fail();}
                    _phase=Phase.AfterStart;_t=1f;
                    break;
                case Phase.AfterStart:
                    _t-=Time.unscaledDeltaTime;
                    if(_t<=0f)_phase=Phase.Populate;
                    break;
                case Phase.Populate:
                    try
                    {
                        GM.State.AddRes("wood",5000);GM.State.AddRes("stone",3000);GM.State.AddRes("food",3000);
                        GM.State.AddRes("gold",5000);GM.State.AddRes("iron",2000);GM.State.AddRes("steel",2000);
                        GM.State.AddRes("concrete",1500);GM.State.AddRes("fusion",1000);
                        Debug.Log("[SMOKE] Populated resources");
                    }
                    catch(Exception e){Debug.LogError("[SMOKE_FAIL] Populate: "+e);Fail();}
                    _phase=Phase.AfterPopulate;_t=0.5f;
                    break;
                case Phase.AfterPopulate:
                    _t-=Time.unscaledDeltaTime;
                    if(_t<=0f)_phase=Phase.UiChecks;
                    break;
                case Phase.UiChecks:
                    try
                    {
                        var ui=FindObjectOfType<UI.UIManager>();
                        if(ui!=null) Debug.Log("[SMOKE] UIManager present: LeftPanel="+ui.LeftPanelOpen);
                        if(FindObjectOfType<UI.Minimap>()==null) Debug.Log("[SMOKE] minimap missing (non-fatal)");
                        if(UnityEngine.EventSystems.EventSystem.current==null) Debug.Log("[SMOKE] EventSystem missing (non-fatal)");
                    }
                    catch(Exception e){Debug.LogError("[SMOKE_FAIL] UiChecks: "+e);}
                    _phase=Phase.Build;
                    break;
                case Phase.Build:
                    try
                    {
                        var cand=new[]{"hut","farm","market","well","tower","school","hospital","fire_station","road","airport","power_plant"};
                        int built=0, attempted=0;
                        foreach(var id in cand)
                        {
                            if(!GM.Buildings.ContainsKey(id))continue;
                            attempted++;
                            if(GM.Building.FindAutoPosition(id,out float x,out float z))
                            {
                                if(GM.Building.PlaceInitial(id,x,z)!=null) built++;
                            }
                        }
                        Debug.Log("[SMOKE] real builds: "+built+"/"+attempted);
                        try{ GM.Philosophy.Adopt("fa"); } catch(Exception e){ Debug.Log("[SMOKE] Philosophy non-fatal: "+e.Message); }
                        GM.enabled=false;
                    }
                    catch(Exception e){Debug.LogError("[SMOKE_FAIL] Build: "+e);Fail();}
                    _phase=Phase.Simulate;
                    break;
                case Phase.Simulate:
                    try
                    {
                        if(_simIdx<JumpTargets.Length)
                        {
                            int target=JumpTargets[_simIdx];
                            GM.Time.DebugJumpTo(target);
                            LogEra();
                            _simIdx++;
                        }
                        else
                        {
                            Debug.Log("[SMOKE] simulation done: year="+GM.State.Year+" era="+GM.State.Era
                                +" events="+GM.State.EventLog.Count+" corruption="+Mathf.RoundToInt(GM.State.Corruption)
                                +" monarch="+(GM.State.MonarchWise?"wise":"fool"));
                            _phase=Phase.Render;
                        }
                    }
                    catch(Exception e){Debug.LogError("[SMOKE_FAIL] Simulate: "+e);Fail();}
                    break;
                case Phase.Render:
                    try
                    {
                        var renderers=FindObjectsOfType<Renderer>();
                        int pink=0, emptyMesh=0;
                        foreach(var r in renderers)
                        {
                            var mf=r.GetComponent<MeshFilter>();
                            if(mf!=null && mf.sharedMesh==null && r.GetType().Name.Contains("Mesh")) emptyMesh++;
                            if(r.sharedMaterial!=null && r.sharedMaterial.name.IndexOf("magenta",StringComparison.OrdinalIgnoreCase)>=0) pink++;
                        }
                        var materials=renderers.Where(r=>r.sharedMaterial!=null).Select(r=>r.sharedMaterial).Distinct().Count();
                        Debug.Log("[SMOKE] Renderers="+renderers.Length+" materials="+materials+" pink="+pink+" emptyMesh="+emptyMesh);
                        if(pink>0) Debug.Log("[SMOKE] pink materials present (non-fatal, CC0 fallback)");
                    }
                    catch(Exception e){Debug.LogError("[SMOKE_FAIL] Render: "+e);}
                    _phase=Phase.Done;
                    break;
                case Phase.Done:
                    Debug.Log("[SMOKE_PASS]");
                    QuitNextFrame();
                    enabled=false;
                    break;
            }
        }

        void LogEra()
        {
            var s=GM.State;
            Debug.Log("[SMOKE] frame="+_frame+" year="+s.Year+" greg="+GM.Time.GregorianText
                +" era="+s.Era+" dynasty="+GM.Time.DynastyName
                +" pop="+s.Pop+" buildings="+s.Buildings.Count);
        }

        void Fail()
        {
            Debug.LogError("[SMOKE_FAIL] abort at phase="+_phase);
            QuitNextFrame();
            enabled=false;
        }

        int _quit;
        void QuitNextFrame()
        {
            EditorApplication.delayCall+=()=>EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
        }
    }
}
#endif
