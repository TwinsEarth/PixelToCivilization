using PxC.Context;
using PxC.Core;
using PxC.Core.Events;
using PxC.Core.Services;
using PxC.Core.Time;
using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using PxC.Gameplay.Resources;
using PxC.Gameplay.Services;
using UnityEngine;
using UnityEngine.UI;

namespace PxC.UI
{
    /// <summary>
    /// v10.8.0 UI 表现层主组件：在 v10 纯逻辑架构之上补全新画面层。
    /// 由 GameBootstrap.Boot 在插件装配完成后创建（TestContext 已就绪），
    /// 纯代码构建 uGUI（无 Prefab），只读逻辑层数据、通过既有接口交互，不侵入 31 个逻辑插件。
    ///
    /// 布局（1920×1080 参考）：
    ///   顶部条：游戏名+版本 | 公历年份+朝代+时代 | 时间控制（⏸/1x/5x/20x/100x）
    ///   中央：世界地图（势力色块+聚落+建筑+单位+血条点）
    ///   左上：玩家势力面板（人口分层/关键资源/军队·建筑·聚落统计）
    ///   左下：全部势力列表
    ///   右下：历史事件日志
    /// </summary>
    public sealed class UiHud : MonoBehaviour
    {
        // ---- 逻辑层引用（TestContext 注入）----
        private GameClock _clock;
        private IEventBus _events;
        private IServiceLocator _services;
        private FactionRegistry _factions;
        private UnitRegistry _units;
        private BuildingRegistry _buildings;
        private SettlementRegistry _settlements;

        // ---- 服务 ----
        private IEconomyService _economy;
        private IPopulationService _population;
        private IDynastyService _dynasty;

        // ---- UI 引用 ----
        private Text _titleText;
        private Text _dateText;
        private Text _speedText;
        private Text _playerText;
        private Text _factionsText;
        private WorldMapView _map;
        private EventLogView _log;

        private float _refreshTimer;
        private const float RefreshInterval = 0.25f; // HUD 文本 4Hz 刷新（地图单位层每帧由 WorldMapView 处理）

        private void Awake()
        {
            var ctx = GameContext.TestContext;
            if (ctx == null)
            {
                Debug.LogWarning("[UI] TestContext 未就绪，UI 表现层跳过（测试/异常环境）");
                enabled = false;
                return;
            }

            _clock = ctx.Clock;
            _events = ctx.Events;
            _services = ctx.Services;
            _factions = ctx.Factions;
            _units = ctx.Units;
            _buildings = ctx.Buildings;
            _settlements = ctx.Settlements;

            _services.TryResolve(out _economy);
            _services.TryResolve(out _population);
            _services.TryResolve(out _dynasty);

            BuildUi(ctx);
            Debug.Log($"[UI] 表现层就绪（{AppInfo.Version}）");
        }

        // ============================================================
        // UI 构建
        // ============================================================
        private void BuildUi(BootstrapTestContext ctx)
        {
            UiFactory.EnsureEventSystem(transform);
            var canvas = UiFactory.CreateCanvas("PxC_Canvas", transform);

            // ---------- 地图（最底，全屏） ----------
            var mapPanel = UiFactory.CreatePanel("WorldMap", canvas.transform, UiPalette.MapLand);
            UiFactory.Anchor(mapPanel, new Vector2(0f, 0f), new Vector2(1f, 1f), 0, 0, 0, 0);
            _map = gameObject.AddComponent<WorldMapView>();
            _map.Bind(_factions, _units, _buildings, _settlements);
            var mapImage = _map.Build(mapPanel);
            var mrt = (RectTransform)mapImage.transform;
            mrt.anchorMin = Vector2.zero;
            mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero;
            mrt.offsetMax = Vector2.zero;

            // ---------- 顶部条 ----------
            BuildTopBar(canvas);

            // ---------- 左上：玩家面板 ----------
            BuildPlayerPanel(canvas);

            // ---------- 左下：势力列表 ----------
            BuildFactionPanel(canvas);

            // ---------- 右下：事件日志 ----------
            var logPanel = UiFactory.CreatePanel("EventLog", canvas.transform, UiPalette.PanelDark);
            UiFactory.Anchor(logPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), 0, 0, 0, 0);
            ((RectTransform)logPanel).sizeDelta = new Vector2(480f, 260f);
            _log = gameObject.AddComponent<EventLogView>();
            _log.Bind(_events, _factions);
            _log.Build(logPanel);
        }

        private void BuildTopBar(Canvas canvas)
        {
            var bar = UiFactory.CreatePanel("TopBar", canvas.transform, UiPalette.Panel);
            UiFactory.Anchor(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), 0, 0, 0, 0);
            ((RectTransform)bar).sizeDelta = new Vector2(0f, 54f);

            _titleText = UiFactory.CreateText("Title", bar,
                $"像素文明 · {AppInfo.Version}", 16, UiPalette.Accent);
            UiFactory.Anchor((RectTransform)_titleText.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), 14, 0, 0, 0);
            ((RectTransform)_titleText.transform).sizeDelta = new Vector2(230f, 30f);
            _titleText.alignment = TextAnchor.MiddleLeft;

            _dateText = UiFactory.CreateText("Date", bar, string.Empty, 17, UiPalette.AccentBright);
            UiFactory.Anchor((RectTransform)_dateText.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0, 0, 0, 0);
            ((RectTransform)_dateText.transform).sizeDelta = new Vector2(460f, 30f);
            _dateText.alignment = TextAnchor.MiddleCenter;

            // 时间控制按钮组（从右向左）
            BuildSpeedButton(bar, "×100", 5, 100f);
            BuildSpeedButton(bar, "×20", 4, 20f);
            BuildSpeedButton(bar, "×5", 3, 5f);
            BuildSpeedButton(bar, "1x", 2, 1f);
            BuildPauseButton(bar, 1);

            _speedText = UiFactory.CreateText("Speed", bar, string.Empty, 14, UiPalette.TextDim);
            UiFactory.Anchor((RectTransform)_speedText.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), 0, 0, 0, 0);
            ((RectTransform)_speedText.transform).sizeDelta = new Vector2(90f, 30f);
            _speedText.alignment = TextAnchor.MiddleRight;
            var srt = (RectTransform)_speedText.transform;
            srt.anchorMin = new Vector2(1f, 0.5f);
            srt.anchorMax = new Vector2(1f, 0.5f);
            srt.offsetMin = new Vector2(-460f, -15f);
            srt.offsetMax = new Vector2(-370f, 15f);
        }

        private void BuildSpeedButton(Transform parent, string label, int index, float speed)
        {
            var btn = UiFactory.CreateButton("Btn" + label, parent, label, 14, UiPalette.ButtonBg,
                () => SetSpeed(speed));
            var rt = (RectTransform)btn.transform;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            float w = 56f, gap = 6f;
            float right = 14f + index * (w + gap);
            rt.offsetMin = new Vector2(-right - w, -19f);
            rt.offsetMax = new Vector2(-right, 19f);
        }

        private void BuildPauseButton(Transform parent, int index)
        {
            var btn = UiFactory.CreateButton("BtnPause", parent, "⏸", 14, UiPalette.ButtonBg, TogglePause);
            var rt = (RectTransform)btn.transform;
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            float w = 56f, gap = 6f;
            float right = 14f + index * (w + gap);
            rt.offsetMin = new Vector2(-right - w, -19f);
            rt.offsetMax = new Vector2(-right, 19f);
        }

        private void BuildPlayerPanel(Canvas canvas)
        {
            var panel = UiFactory.CreatePanel("PlayerPanel", canvas.transform, UiPalette.Panel);
            UiFactory.Anchor(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), 8, 62, 0, 0);
            ((RectTransform)panel).sizeDelta = new Vector2(360f, 300f);

            var title = UiFactory.CreateText("Title", panel, "玩家文明", 15, UiPalette.Accent);
            UiFactory.Anchor((RectTransform)title.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), 12, 8, 12, 0);
            ((RectTransform)title.transform).sizeDelta = new Vector2(0f, 22f);

            _playerText = UiFactory.CreateText("Info", panel, string.Empty, 14, UiPalette.TextMain);
            UiFactory.Anchor((RectTransform)_playerText.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), 12, 36, 12, 0);
            ((RectTransform)_playerText.transform).sizeDelta = new Vector2(0f, -36f);
            _playerText.alignment = TextAnchor.UpperLeft;
            _playerText.lineSpacing = 1.35f;
        }

        private void BuildFactionPanel(Canvas canvas)
        {
            var panel = UiFactory.CreatePanel("FactionPanel", canvas.transform, UiPalette.Panel);
            UiFactory.Anchor(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), 8, 372, 0, 0);
            ((RectTransform)panel).sizeDelta = new Vector2(360f, 250f);

            var title = UiFactory.CreateText("Title", panel, "天下势力", 15, UiPalette.Accent);
            UiFactory.Anchor((RectTransform)title.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), 12, 8, 12, 0);
            ((RectTransform)title.transform).sizeDelta = new Vector2(0f, 22f);

            _factionsText = UiFactory.CreateText("List", panel, string.Empty, 14, UiPalette.TextMain);
            UiFactory.Anchor((RectTransform)_factionsText.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), 12, 36, 12, 0);
            ((RectTransform)_factionsText.transform).sizeDelta = new Vector2(0f, -36f);
            _factionsText.alignment = TextAnchor.UpperLeft;
            _factionsText.lineSpacing = 1.3f;
        }

        // ============================================================
        // 时间控制
        // ============================================================
        private void SetSpeed(float speed)
        {
            if (_clock == null) return;
            _clock.Speed = speed;
            _clock.Paused = false;
            _clock.Running = true;
        }

        private void TogglePause()
        {
            if (_clock == null) return;
            _clock.Paused = !_clock.Paused;
            if (!_clock.Paused) _clock.Running = true;
        }

        // ============================================================
        // 每帧刷新（4Hz 文本 + 状态）
        // ============================================================
        private void Update()
        {
            if (!enabled || _clock == null) return;

            _refreshTimer += Time.deltaTime;
            if (_refreshTimer < RefreshInterval) return;
            _refreshTimer = 0f;

            RefreshDate();
            RefreshPlayer();
            RefreshFactions();
            RefreshStats();
        }

        private void RefreshDate()
        {
            int year = _clock.Year;
            int ad = _dynasty != null ? _dynasty.ToAdYear(year) : year - 3000;
            string eraName = "上古";
            if (_dynasty != null)
            {
                var d = _dynasty.ForGameYear(year);
                if (!string.IsNullOrEmpty(d.Name)) eraName = d.Name;
            }
            string adStr = ad <= 0 ? $"公元前 {-ad + 1} 年" : $"公元 {ad} 年";
            string speedStr = _clock.Paused ? "已暂停" : $"速度 ×{_clock.Speed:0.##}";
            _dateText.text = $"{adStr} · {eraName} · 游戏年 {year}";
            _speedText.text = speedStr;
        }

        private void RefreshPlayer()
        {
            var p = _factions != null ? _factions.Player : null;
            if (p == null) return;

            int fid = p.Id;
            var sb = new System.Text.StringBuilder(256);
            sb.AppendLine($"<color=#{ColorToHex(UiPalette.Faction(p.ColorIndex))}>{p.Name}</color>");

            if (_population != null)
            {
                sb.AppendLine($"人口 {FormatInt(_population.Total(fid))}  " +
                              $"劳力 {FormatInt(_population.ByLayer(fid, PopulationLayer.Laborer))}  " +
                              $"士兵 {FormatInt(_population.ByLayer(fid, PopulationLayer.Soldier))}");
            }
            if (_economy != null)
            {
                sb.AppendLine($"粮 {FormatFloat(_economy.Get(fid, ResourceType.Food))}  " +
                              $"金 {FormatFloat(_economy.Get(fid, ResourceType.Gold))}  " +
                              $"木 {FormatFloat(_economy.Get(fid, ResourceType.Wood))}");
                sb.AppendLine($"石 {FormatFloat(_economy.Get(fid, ResourceType.Stone))}  " +
                              $"铁 {FormatFloat(_economy.Get(fid, ResourceType.Iron))}  " +
                              $"煤 {FormatFloat(_economy.Get(fid, ResourceType.Coal))}  " +
                              $"钢 {FormatFloat(_economy.Get(fid, ResourceType.Steel))}");
            }
            sb.AppendLine($"军队 {CountUnits(fid)}  建筑 {CountBuildings(fid)}  " +
                          $"聚落 {(_settlements != null ? _settlements.CountByFaction(fid) : 0)}");
            _playerText.text = sb.ToString();
        }

        private void RefreshFactions()
        {
            if (_factions == null) return;
            var sb = new System.Text.StringBuilder(256);
            for (int i = 0; i < _factions.Count; i++)
            {
                var f = _factions.Get(i);
                if (f == null) continue;
                string color = ColorToHex(UiPalette.Faction(f.ColorIndex));
                string status = !f.Alive ? "（已灭国）" : f.IsVassal ? $"（{_factions.Get(f.Suzerain)?.Name} 附庸）" : "（独立）";
                string pop = _population != null ? $" 人口 {FormatInt(_population.Total(i))}" : "";
                string tag = f.IsPlayer ? "◆" : "·";
                sb.AppendLine($"<color=#{color}>{tag} {f.Name}</color>{status}{pop}");
            }
            _factionsText.text = sb.ToString();
        }

        private void RefreshStats()
        {
            int unitCount = 0, buildingCount = 0, settlementCount = 0;
            if (_units != null)
                for (int u = 0; u < _units.Capacity; u++)
                    if (_units.GetSlotRef(u).Alive) unitCount++;
            if (_buildings != null)
                for (int b = 0; b < _buildings.Count; b++)
                    if (_buildings.GetSafe(b).Alive) buildingCount++;
            if (_settlements != null)
                for (int s = 0; s < _settlements.Count; s++)
                    if (_settlements.GetSafe(s).Alive) settlementCount++;

            _statsTextRef = EnsureStatsText();
            if (_statsTextRef != null)
                _statsTextRef.text = $"单位 {unitCount} · 建筑 {buildingCount} · 聚落 {settlementCount}";
        }

        private Text _statsTextRef;
        private Text EnsureStatsText()
        {
            if (_statsTextRef != null) return _statsTextRef;
            // 右下角统计浮层（地图上方，日志面板左侧）
            var panel = UiFactory.CreatePanel("Stats", transform.Find("PxC_Canvas"), UiPalette.Panel);
            UiFactory.Anchor(panel, new Vector2(1f, 0f), new Vector2(1f, 0f), 0, 0, 0, 0);
            ((RectTransform)panel).sizeDelta = new Vector2(220f, 30f);
            _statsTextRef = UiFactory.CreateText("Text", panel, string.Empty, 13, UiPalette.TextDim);
            UiFactory.Anchor((RectTransform)_statsTextRef.transform, Vector2.zero, Vector2.one, 8, 0, 8, 0);
            _statsTextRef.alignment = TextAnchor.MiddleLeft;
            return _statsTextRef;
        }

        // ============================================================
        // 工具
        // ============================================================
        private int CountUnits(int faction)
        {
            if (_units == null) return 0;
            int n = 0;
            for (int u = 0; u < _units.Capacity; u++)
            {
                var r = _units.GetSlotRef(u);
                if (r.Alive && r.Faction == faction) n++;
            }
            return n;
        }

        private int CountBuildings(int faction)
        {
            if (_buildings == null) return 0;
            int n = 0;
            for (int b = 0; b < _buildings.Count; b++)
            {
                var r = _buildings.GetSafe(b);
                if (r.Alive && r.Faction == faction) n++;
            }
            return n;
        }

        private static string FormatFloat(float v) => v >= 10000f ? $"{v / 10000f:0.#}万" : $"{v:0.#}";
        private static string FormatInt(int v) => v >= 10000 ? $"{v / 10000f:0.#}万" : v.ToString();

        private static string ColorToHex(Color c)
        {
            int r = Mathf.RoundToInt(c.r * 255f);
            int g = Mathf.RoundToInt(c.g * 255f);
            int b = Mathf.RoundToInt(c.b * 255f);
            return $"{r:x2}{g:x2}{b:x2}";
        }
    }
}
