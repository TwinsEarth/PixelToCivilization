using System.Collections.Generic;
using PxC.Core.Events;
using PxC.Gameplay.Factions;
using UnityEngine;
using UnityEngine.UI;

namespace PxC.UI
{
    /// <summary>
    /// v10.8.0 事件日志视图：订阅 IEventBus 的关键历史事件（战争/灭国/占领/和约/附庸/时代/朝代），
    /// 格式化为中文日志追加到滚动面板。单位级细碎事件（生成/死亡/年推进）不订阅，避免刷屏。
    /// </summary>
    public sealed class EventLogView : MonoBehaviour
    {
        private const int MaxLines = 80;

        private IEventBus _events;
        private FactionRegistry _factions;
        private Text _text;
        private ScrollRect _scroll;
        private readonly List<string> _lines = new List<string>(64);

        public void Bind(IEventBus events, FactionRegistry factions)
        {
            _events = events;
            _factions = factions;
            Subscribe();
        }

        public ScrollRect Build(Transform parent)
        {
            // 面板底
            var panel = UiFactory.CreatePanel("EventLogPanel", parent, UiPalette.PanelDark);
            UiFactory.Anchor(panel, new Vector2(0f, 1f), new Vector2(1f, 1f), 0, 0, 0, 0);

            // 标题
            var title = UiFactory.CreateText("Title", panel, "历史日志", 15, UiPalette.Accent);
            var trt = (RectTransform)title.transform;
            UiFactory.Anchor(trt, new Vector2(0f, 1f), new Vector2(1f, 1f), 12, 8, 12, 0);
            trt.sizeDelta = new Vector2(0f, 22f);

            // ScrollRect：Viewport + Content + Text
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect),
                typeof(RectMask2D), typeof(Image));
            scrollGo.transform.SetParent(panel, false);
            _scroll = scrollGo.GetComponent<ScrollRect>();
            var srt = (RectTransform)scrollGo.transform;
            UiFactory.Anchor(srt, new Vector2(0f, 1f), new Vector2(1f, 1f), 8, 34, 8, 0);
            srt.sizeDelta = new Vector2(0f, -34f); // 占满标题以下
            _scroll.vertical = true;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            scrollGo.GetComponent<Image>().color = UiPalette.Panel;
            scrollGo.GetComponent<RectMask2D>().enabled = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(srt, false);
            var vrt = (RectTransform)viewport.transform;
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            _scroll.viewport = vrt;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0f, 0f);
            var cg = content.AddComponent<ContentSizeFitter>();
            cg.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            cg.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            _scroll.content = crt;

            _text = UiFactory.CreateText("Log", content.transform, string.Empty, 14, UiPalette.TextMain);
            var trt2 = (RectTransform)_text.transform;
            trt2.anchorMin = new Vector2(0f, 1f);
            trt2.anchorMax = new Vector2(1f, 1f);
            trt2.pivot = new Vector2(0.5f, 1f);
            trt2.sizeDelta = new Vector2(0f, 0f);
            trt2.offsetMin = new Vector2(6f, 0f);
            trt2.offsetMax = new Vector2(-6f, 0f);
            _text.alignment = TextAnchor.UpperLeft;

            return _scroll;
        }

        // ---------------- 事件订阅 ----------------

        private void Subscribe()
        {
            if (_events == null) return;
            _events.Subscribe<EraChangedEvent>(OnEraChanged);
            _events.Subscribe<DynastyChangedEvent>(OnDynastyChanged);
            _events.Subscribe<WarDeclaredEvent>(OnWarDeclared);
            _events.Subscribe<FactionEliminatedEvent>(OnFactionEliminated);
            _events.Subscribe<TerritoryConqueredEvent>(OnTerritoryConquered);
            _events.Subscribe<PeaceTreatyEvent>(OnPeaceTreaty);
            _events.Subscribe<CoercedPeaceEvent>(OnCoercedPeace);
            _events.Subscribe<VassalizedEvent>(OnVassalized);
        }

        public void Append(string line)
        {
            _lines.Add(line);
            while (_lines.Count > MaxLines) _lines.RemoveAt(0);
            _text.text = string.Join("\n", _lines);
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f; // 滚到底（最新在最下）
        }

        private string FactionName(int id)
        {
            var f = _factions != null ? _factions.Get(id) : null;
            return f != null ? f.Name : $"势力{id}";
        }

        // ---------------- 事件处理 ----------------

        private void OnEraChanged(EraChangedEvent e) => Append($"[时代 {e.GameYear}] 文明进入第 {e.NewEra + 1} 时代（旧 {e.OldEra + 1}）");

        private void OnDynastyChanged(DynastyChangedEvent e) => Append($"[朝代 {e.GameYear}] 朝代更替，进入新纪元");

        private void OnWarDeclared(WarDeclaredEvent e) =>
            Append($"[战争] {FactionName(e.AttackerFaction)} 向 {FactionName(e.DefenderFaction)} 宣战");

        private void OnFactionEliminated(FactionEliminatedEvent e) =>
            Append($"[灭国 {e.GameYear}] {FactionName(e.Faction)} 被 {FactionName(e.ByFaction)} 所灭");

        private void OnTerritoryConquered(TerritoryConqueredEvent e) =>
            Append($"[占领 {e.GameYear}] {FactionName(e.Winner)} 夺取 {FactionName(e.Loser)} 的 {e.Settlements} 聚落 / {e.Buildings} 建筑");

        private void OnPeaceTreaty(PeaceTreatyEvent e) =>
            Append($"[和约 {e.GameYear}] {FactionName(e.FactionA)} 与 {FactionName(e.FactionB)} 停战");

        private void OnCoercedPeace(CoercedPeaceEvent e) =>
            Append($"[压制和约 {e.GameYear}] {FactionName(e.Stronger)} 迫使 {FactionName(e.Weaker)} 割让 {e.SettlementsCeded} 聚落");

        private void OnVassalized(VassalizedEvent e) =>
            Append($"[附庸 {e.GameYear}] {FactionName(e.Vassal)} 向 {FactionName(e.Suzerain)} 称臣纳贡");

        private static int ClockAd(int gameYear) => gameYear - 3000;
    }
}
