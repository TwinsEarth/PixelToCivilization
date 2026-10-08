using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PixelToCivilization.Core;
using PixelToCivilization.Data;
using PixelToCivilization.AI;
using PixelToCivilization.Systems;
using PixelToCivilization.World;

namespace PixelToCivilization.UI
{
    /// <summary>UIManager 的模态面板部分：科技树/政策/九神/海洋/太空/建筑信息</summary>
    public partial class UIManager
    {
        private GameObject _techModal,_policyModal,_godsModal,_oceanModal,_spaceModal,_buildingModal,_shipModal,_cartModal;
        private GameObject _campaignModal,_colonyModal;   // V6.1.4 群雄讨伐 / V6.1.5 殖民地
        private GameObject _cityModal;                    // V9.0.7 城市治理（等级/财政/税率/地价）
        private GameObject _philosophyModal;              // V9.3.13 百家面板预建（原 CreateModal 每次新建：open 累积多层且点 X 易失焦，永久遮挡后续面板）
        private GameObject _agentModal;                   // V9.8.0 智能体驱动面板（身份浏览/自我介绍/协作消息/模式切换）
        private GameObject _treeModal;                    // V9.3.8 树木属性面板
        private GameObject _groundModal;                  // V9.5.7 地面部队属性面板（坦克/装甲车/导弹车·骑兵/方阵/战车）
        private ShipEntity _selectedShip;
        private CartEntity _selectedCart;
        private TreeRecord _selectedTree;
        private Systems.GroundWarfareSystem.GroundUnit _selectedGround;
        // V9.3.10：AI 密钥改弹窗输入（ApiKeyPrompt），行内输入字段已移除
        private Transform _modalLayer;

        private void BuildModals(Transform parent)
        {
            var layerGo=UITheme.Panel("ModalLayer",parent,new Color(0,0,0,0));
            // 透明容器层不拦射线；阻挡由各 modal 打开时的 55% 遮挡层负责
            layerGo.GetComponent<Image>().raycastTarget=false;
            _modalLayer=layerGo.transform;
            Stretch(_modalLayer.gameObject);
            _techModal=MakeModal("TechModal"," 科技树",out _);
            _policyModal=MakeModal("PolicyModal"," 政策法令",out _);
            _godsModal=MakeModal("GodsModal"," 九神共治 · AI智能体议会",out _);
            _oceanModal=MakeModal("OceanModal"," 海洋大开发",out _);
            _spaceModal=MakeModal("SpaceModal"," 太空探索",out _);
            _buildingModal=MakeModal("BuildingModal","建筑",out _);
            _shipModal=MakeModal("ShipModal","船只详情",out _);
            // 船浮窗为紧凑小窗（对齐 v5.9.9 186×174 船只浮窗）
            var sbox=_shipModal.transform.Find("Box").GetComponent<RectTransform>();
            sbox.sizeDelta=new Vector2(320,400);
            _cartModal=MakeModal("CartModal","车辆详情",out _);
            var cbox=_cartModal.transform.Find("Box").GetComponent<RectTransform>();
            cbox.sizeDelta=new Vector2(320,380);
            // V9.3.8 树木属性面板（紧凑窗对齐船窗）
            _treeModal=MakeModal("TreeModal","树木详情",out _);
            var tbox=_treeModal.transform.Find("Box").GetComponent<RectTransform>();
            tbox.sizeDelta=new Vector2(320,400);
            // V9.5.7 地面部队属性面板（紧凑窗对齐船窗）
            _groundModal=MakeModal("GroundModal","地面部队详情",out _);
            var gbox=_groundModal.transform.Find("Box").GetComponent<RectTransform>();
            gbox.sizeDelta=new Vector2(320,420);
            // V6.1.4 群雄讨伐 / V6.1.5 殖民地面板
            _campaignModal=MakeModal("CampaignModal","【群雄争霸 · 出师讨伐】",out var cab);
            cab.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(580,620);
            _colonyModal=MakeModal("ColonyModal","【殖民时代 · 海外领地】",out var cob);
            cob.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(858,620);   // V9.4.2 660×1.3=858
            // V9.0.7 城市治理（等级/财政预算/税率/地价/公共服务/电力/城市指标）
            _cityModal=MakeModal("CityModal"," 城市治理 · 财政预算",out var cib);
            cib.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(936,660);   // V9.4.2 720×1.3=936
            _philosophyModal=MakeModal("PhilosophyModal"," 诸子百家 · 择国之道",out _);   // V9.3.13 预建百家面板（关闭可复用）
            // V9.8.0 智能体驱动面板
            _agentModal=MakeModal("AgentModal"," 智能体驱动 · 万物协作",out var agb);
            agb.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(960,660);
        }

        private GameObject MakeModal(string name,string title,out RectTransform body,bool destroyOnClose=false)
        {
            var overlay=UITheme.Panel(name,_modalLayer,UITheme.HexA(0x000000,0.55f));Stretch(overlay);
            var box=UITheme.Surface("Box",overlay.transform,new Color(0.985f,0.975f,0.935f,0.99f));
            var rt=box.GetComponent<RectTransform>();
            rt.anchorMin=rt.anchorMax=new Vector2(0.5f,0.5f);rt.sizeDelta=new Vector2(910,580);   // V9.4.2 八系统弹窗左右扩大30%：700×1.3=910
            var vl=box.AddComponent<VerticalLayoutGroup>();vl.spacing=2;vl.padding=new RectOffset(5,5,4,4);   // V9.3.10 8→5 / (18,18,16,16)→(12,12,10,10) 减少留白
            vl.childControlWidth=true;vl.childForceExpandWidth=true;
            vl.childControlHeight=true;vl.childForceExpandHeight=false;   // V9.8.0 fix：必须控制高度，否则 head 不被限制为 24（膨胀到 277）、body 与 sr 被挤塌
            var head=UITheme.Panel("Head",box.transform,new Color(0,0,0,0));
            head.AddComponent<LayoutElement>().preferredHeight=24;   // V9.3.12 26→24
            UITheme.Label("title",head.transform,title,22,TextAnchor.MiddleLeft,UITheme.Gold)
                .SetInset(8,0);
            var close=UITheme.Btn("close",head.transform,"×",16);
            // 小正方框关闭钮（不再纵向拉成长条）
            var crt=close.GetComponent<RectTransform>();crt.anchorMin=new Vector2(1,1);crt.anchorMax=new Vector2(1,1);
            crt.pivot=new Vector2(1,1);crt.sizeDelta=new Vector2(26,26);crt.anchoredPosition=new Vector2(0,2);   // V9.3.10 30→26
            var le=close.GetComponent<LayoutElement>();le.ignoreLayout=true;le.preferredWidth=26;le.preferredHeight=26;
            close.onClick.AddListener(()=>{ if(destroyOnClose) Destroy(overlay); else overlay.SetActive(false); });
            var bodyGo=UITheme.Panel("Body",box.transform,new Color(0,0,0,0));
            body=bodyGo.GetComponent<RectTransform>();
            bodyGo.AddComponent<LayoutElement>().flexibleHeight=1;
            overlay.SetActive(false);
            return overlay;
        }

        private GameObject CreateModal(string title)
        {
            var ov=MakeModal("Tmp_"+Time.frameCount,title,out _,true);
            ov.transform.SetParent(_modalLayer,false);
            ov.SetActive(true);
            return ov;
        }
        private GameObject ModalBody(GameObject overlay)=>overlay.transform.Find("Box/Body").gameObject;
        /// <summary>V9.3.13 结构性去留白：弹窗高度随内容自适应——内容稀疏时窗口 shrink（最小 minH），
        /// 内容超过 maxH 时封顶并滚动。根治"固定大窗口装少量内容"的大片空白。</summary>
        private void FitModal(GameObject overlay, float minH, float maxH)
        {
            if (overlay == null) return;
            var spec = overlay.GetComponent<FitModalSpec>();
            if (spec == null) spec = overlay.AddComponent<FitModalSpec>();
            spec.MinH = minH; spec.MaxH = maxH;
            if (!overlay.activeInHierarchy)
            {
                // V9.3.13 首次打开：FillXxx 在 Open(SetActive) 之前调用，inactive 层级布局未就绪，
                // GetPreferredHeight 会失真。先用最大高度占位防内容裁切，激活后首帧由协程重算真实高度。
                var box0 = overlay.transform.Find("Box");
                var body0 = box0 != null ? box0.Find("Body") as RectTransform : null;
                var bodyLe0 = body0 != null ? body0.GetComponent<LayoutElement>() : null;
                if (bodyLe0 != null) { bodyLe0.flexibleHeight = 0f; bodyLe0.preferredHeight = maxH; }
                if (box0 != null)
                {
                    var fitter0 = box0.GetComponent<ContentSizeFitter>();
                    if (fitter0 == null) fitter0 = box0.gameObject.AddComponent<ContentSizeFitter>();
                    fitter0.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    fitter0.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                StartCoroutine(CoRefitAfterActivate(overlay));
            }
            else
            {
                ApplyFit(overlay, minH, maxH);
                // V9.3.13 hotfix：CreateModal/Open 等 active 路径在布局未就绪时 GetPreferredHeight 失真，
                // 立即 ApplyFit 会把内容压成 minH 造成裁切；激活后首帧协程重算真实高度。
                StartCoroutine(CoRefitAfterActivate(overlay));
            }
        }

        private System.Collections.IEnumerator CoRefitAfterActivate(GameObject overlay)
        {
            yield return null;                    // 等激活后首帧
            yield return new WaitForEndOfFrame(); // 布局系统完成
            if (overlay != null && overlay.activeInHierarchy)
            {
                var spec = overlay.GetComponent<FitModalSpec>();
                if (spec != null) ApplyFit(overlay, spec.MinH, spec.MaxH);
            }
        }

        private void ApplyFit(GameObject overlay, float minH, float maxH)
        {
            var box = overlay.transform.Find("Box") as RectTransform;
            var body = box != null ? box.Find("Body") as RectTransform : null;
            if (box == null || body == null) return;
            var bodyLe = body.GetComponent<LayoutElement>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);
            var blg = body.GetComponent<VerticalLayoutGroup>();
            var sr = body.GetComponentInChildren<ScrollRect>(true);
            float h;
            if (sr != null && sr.content != null)
            {
                var srRt = sr.transform as RectTransform;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(sr.content);
                float scrollH = LayoutUtility.GetPreferredHeight(sr.content) + 6f;
                float fixedH = 0f; int active = 0;
                foreach (RectTransform child in body)
                {
                    if (!child.gameObject.activeInHierarchy) continue;
                    active++;
                    if (child == srRt) continue;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(child);
                    fixedH += LayoutUtility.GetPreferredHeight(child);
                }
                if (blg != null)
                {
                    fixedH += blg.spacing * Mathf.Max(0, active - 1);
                    fixedH += blg.padding.vertical;
                }
                h = fixedH + Mathf.Clamp(scrollH, 0f, maxH - fixedH);
            }
            else
            {
                h = LayoutUtility.GetPreferredHeight(body);
            }
            h = Mathf.Clamp(h, minH, maxH);
            if (bodyLe != null) { bodyLe.flexibleHeight = 0f; bodyLe.preferredHeight = h; }
            var fitter = box.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = box.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(box);
        }
        private void Open(GameObject m){ if(m!=null){m.SetActive(true);RefreshModalContent(m);AutoBindHovers(m.transform);} }
        private void RefreshModalContent(GameObject m)
        {
            if (m==_techModal) FillTech(m);
            else if (m==_policyModal) FillPolicy(m);
            else if (m==_godsModal) FillGods(m);
            else if (m==_oceanModal) FillOcean(m);
            else if (m==_spaceModal) FillSpace(m);
            else if (m==_campaignModal) FillCampaign(m);
            else if (m==_colonyModal) FillColony(m);
            else if (m==_cityModal) FillCity(m);
        }

        // ===== 诸子百家（策划书·学派抉择） =====
        private void OpenPhilosophyModal()
        {
            if (GM.Philosophy == null) { Toast("百家系统未就绪", false); return; }
            FillPhilosophy(_philosophyModal);
            Open(_philosophyModal);
        }
        private void FillPhilosophy(GameObject modal)
        {
            var body = ModalBody(modal);
            Clear(body);
            UITheme.VerticalScroll("PhilScroll", body.transform, out var content, 2);   // V9.3.13 hotfix：改用滚动容器（同 FillGods 已验证路径），根治内容塌缩只剩按钮
            // —— 状态条 ——
            StatusBar(content.transform, string.IsNullOrEmpty(S.Philosophy) ? "当前国策：未择学派（择一家立即生效）" : "当前国策："+S.Philosophy);
            // —— 组1：学派列表 ——
            GroupTitle(content.transform,"诸子百家 · 学派抉择");
            foreach (var s in PixelToCivilization.Systems.PhilosophySystem.Schools)
            {
                bool current = S.Philosophy == s.id;
                var row = UITheme.Panel("ph_" + s.id, content.transform,
                    UITheme.HexA(current ? 0xFFD700 : 0xffffff, current ? 0.16f : 0.05f));
                row.AddComponent<LayoutElement>().preferredHeight = 48;   // V9.3.10 60→48
                // V9.3.13 hotfix2：改用 FillGods 验证过的行模式（整行 Label + 右侧 anchor 按钮），弃用行内横排组（n/d/b 塌缩只剩 desc）
                UITheme.Label("i", row.transform,
                    $"{s.name}（{(current ? "当前国策" : "可择")}）　{s.desc}", 13, TextAnchor.MiddleLeft,
                    UITheme.Hex((int)s.color)).SetInset(8, 0.22f);
                var b = UITheme.Btn("adopt", row.transform, current ? "国策" : "择为", 12);
                var brt = b.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.80f, 0.2f); brt.anchorMax = new Vector2(0.98f, 0.8f);
                brt.offsetMin = brt.offsetMax = Vector2.zero;
                b.interactable = !current;
                string id = s.id;
                b.onClick.AddListener(() => { if(GM.Philosophy!=null) GM.Philosophy.Adopt(id); modal.SetActive(false); });
            }
            FitModal(modal, 240, 520);
        }

        // ===== 科技树 =====
        private void OpenTechModal(){ FillTech(_techModal);Open(_techModal); }        private void FillTech(GameObject modal)
        {
            var body=ModalBody(modal);Clear(body);
            UITheme.VerticalScroll("TechScroll",body.transform,out var content,2);
            // —— 状态条 ——
            StatusBar(content,$"已完成科技 {S.ResearchedTechs.Count} / {GM.Techs.Count}　当前研究：{(string.IsNullOrEmpty(S.CurrentResearch)?"无（点击任意科技开始）":S.CurrentResearch)}");
            // —— 组1：分时代科技树 ——
            foreach (var era in GM.Eras)
            {
                UITheme.Label("era",content,$"【{era.Name}】",15,TextAnchor.MiddleLeft,era.ThemeColor);
                foreach (var t in GM.Techs.Values)
                {
                    if (t.Era!=era.Id) continue;
                    bool done=S.ResearchedTechs.Contains(t.Id);
                    bool researching=S.CurrentResearch==t.Id;
                    bool can=GM.Tech.CanResearch(t.Id,out _);
                    var row=UITheme.Panel("t_"+t.Id,content,UITheme.HexA(done?0x2e7d32:0xffffff, done?0.15f:0.05f));
                    var le=row.AddComponent<LayoutElement>();le.preferredHeight=38;   // V9.3.13 44→38
                    var info=UITheme.Label("info",row.transform,$"{t.Name}  ({t.Cost})\n{t.Desc}  前置:{(t.HasRequirement?string.Join(",",t.Requires):"无")}{(researching?"  研究中…":"")}",11,TextAnchor.MiddleLeft);   // V9.3.13 12→11
                    info.rectTransform.anchorMin=new Vector2(0,0);info.rectTransform.anchorMax=new Vector2(0.72f,1);
                    info.rectTransform.offsetMin=new Vector2(8,2);info.rectTransform.offsetMax=new Vector2(-4,-2);
                    var b=UITheme.Btn("btn",row.transform,done?"已研":"研究",12);
                    var brt=b.GetComponent<RectTransform>();brt.anchorMin=new Vector2(0.74f,0.2f);brt.anchorMax=new Vector2(0.99f,0.8f);brt.offsetMin=brt.offsetMax=Vector2.zero;
                    b.interactable=!done&&can; string id=t.Id;
                    b.onClick.AddListener(()=>{GM.Tech.StartResearch(id);FillTech(modal);});
                }
            }
            FitModal(modal, 260, 520);
        }

        // ===== 政策 =====
        private void OpenPolicyModal(){ FillPolicy(_policyModal);Open(_policyModal); }
        private void FillPolicy(GameObject modal)
        {
            var body=ModalBody(modal);Clear(body);
            UITheme.VerticalScroll("PolScroll",body.transform,out var content,2);
            // —— 状态条 ——
            StatusBar(content,$"已推行国策 {S.Policies.Count} / {GM.Policies.Count}　点击行右侧按钮推行或废止（每年自动结算效果）");
            // —— 组1：国策列表 ——
            GroupTitle(content,"国策列表 · 按时代排列");
            foreach (var p in GM.Policies.Values)
            {
                bool on=S.Policies.Contains(p.Id);
                var row=UITheme.Panel("p_"+p.Id,content,UITheme.HexA(on?0xFFD700:0xffffff,on?0.14f:0.05f));
                row.AddComponent<LayoutElement>().preferredHeight=34;   // V9.3.13 40→34
                UITheme.Label("i",row.transform,$"{p.Name}（{GM.Eras[p.Era].Name}）\n{p.Desc}",11,TextAnchor.MiddleLeft)   // V9.3.13 12→11
                    .SetInset(8,0.28f);
                var b=UITheme.Btn("b",row.transform,on?"推行中":"推行",12);
                var brt=b.GetComponent<RectTransform>();brt.anchorMin=new Vector2(0.74f,0.2f);brt.anchorMax=new Vector2(0.99f,0.8f);brt.offsetMin=brt.offsetMax=Vector2.zero;
                string id=p.Id;b.onClick.AddListener(()=>{GM.Policy.Toggle(id);FillPolicy(modal);});
            }
            FitModal(modal, 200, 520);
        }

        // ===== V9.0.7 城市治理：等级/财政预算/税率/地价/公共服务/电力/城市指标 =====
        private void OpenCityModal(){ FillCity(_cityModal);Open(_cityModal); }
        private void FillCity(GameObject modal)
        {
            var body=ModalBody(modal);Clear(body);
            UITheme.VerticalScroll("CityScroll",body.transform,out var content,2);   // V9.3.13 3→2
            var vl=content.GetComponent<VerticalLayoutGroup>();
            vl.spacing=2;vl.childControlWidth=true;vl.childForceExpandWidth=true;   // V9.3.13 4→2

            var fin=GM.CityFinance;
            if(fin==null){ UITheme.Label("no",content,"城市财政系统未装配",13); return; }
            GM.CityServices?.RecomputeCoverage();
            fin.Recompute(false);

            // —— 状态条：城市等级 / 地价 / 国库 ——
            StatusBar(content,
                $"【{fin.TierName}】人口 {S.Pop}　升格阈值 村落120/集镇350/县城800/都市1400/大都市1600　地价 {fin.LandPrice:F0}　国库 {fin.Treasury:F0}　污染 {S.Pollution:F0}　拥堵 {(GM.ModernTraffic!=null?GM.ModernTraffic.AvgCongestion:0f):F1}");

            // —— 组1：财政（税率 + 年度预算） ——
            GroupTitle(content,"财政 · 税率与年度预算");
            var tr=Row(content,26);   // V9.3.13 32→28
            UITheme.Label("tl",tr.transform,"税率政策：",13,TextAnchor.MiddleRight);
            UITheme.Btn("tax",tr.transform,CityFinanceSystem.TaxNames[fin.TaxLevel]+"（点击切换）",12)
                .onClick.AddListener(()=>{fin.CycleTax();FillCity(modal);});
            BudgetRow(content,"人头税收入",fin.HeadRevenue,true);
            BudgetRow(content,"现代商税（超市/写字楼）",fin.CommerceRevenue,true);
            BudgetRow(content,"公共服务运维",fin.ServiceCost,false);
            BudgetRow(content,"道路运维",fin.RoadCost,false);
            var netRow=UITheme.Panel("net",content,UITheme.HexA(fin.NetAnnual>=0?0x2e7d32:0xc62828,0.12f));
            netRow.AddComponent<LayoutElement>().preferredHeight=18;   // V9.3.13 24→20
            UITheme.Label("nl",netRow.transform,"年度净额",13,TextAnchor.MiddleLeft).SetInset(10,0.3f);
            UITheme.Label("nv",netRow.transform,(fin.NetAnnual>=0?"+":"")+fin.NetAnnual.ToString("F0")+
                (fin.NetAnnual<0&&fin.Treasury<50?"　⚠ 财政破产，停俸减民心":""),13,TextAnchor.MiddleRight,
                fin.NetAnnual>=0?UITheme.Good:UITheme.Bad).rectTransform.SetInsetRight(10);

            // —— 组2：公共服务覆盖率 ——
            var cs=GM.CityServices;
            if(cs!=null)
            {
                GroupTitle(content,"公共服务覆盖率");
                Gauge(content,"消防",cs.FireCov);
                Gauge(content,"治安",cs.PoliceCov);
                Gauge(content,"医疗",cs.HospitalCov);
                Gauge(content,"教育",cs.SchoolCov);
                Gauge(content,"公园",cs.ParkCov);
                Gauge(content,"商业",cs.MarketCov);
            }
            // —— 组3：能源与城市指标 ——
            GroupTitle(content,"能源与城市指标");
            Gauge(content,"电力供应",S.PowerRatio);
            Gauge(content,"健康",S.CityHealth/100f);
            Gauge(content,"教育",S.CityEducation/100f);
            Gauge(content,"治安",S.CitySafety/100f);
            Gauge(content,"就业",S.CityEmployment/100f);

            UITheme.Label("tip",content,"说明：人头税随时代货币化程度提高；低税增民心、重税减民心；公共设施与道路每年产生运维支出；"+
                "地价由时代、城市等级、服务覆盖、城市指标、污染与拥堵综合决定。财政数据每年结算一次并随存档保存。",
                11,TextAnchor.UpperLeft,UITheme.Sub).gameObject.AddComponent<LayoutElement>().preferredHeight=26;   // V9.3.13 30→26
            FitModal(modal, 200, 520);
        }
        private void SectionTitle(Transform content,string text)
        {
            UITheme.Label("sec",content,text,13,TextAnchor.MiddleLeft,UITheme.Gold)
                .gameObject.AddComponent<LayoutElement>().preferredHeight=17;   // V9.3.13 14→13 / 20→17
        }
        /// <summary>V9.3.13 功能组标题条：深底金边（左侧金色竖条+金色粗体标题），
        /// 把面板切成"状态条/操作组/列表组/说明"多个可扫读分区，根治线性堆叠的"垃圾场"感。</summary>
        private void GroupTitle(Transform content,string text)
        {
            var bar=UITheme.Panel("gt",content,UITheme.HexA(0x1b2440,0.85f));
            var le=bar.AddComponent<LayoutElement>();le.preferredHeight=18;le.flexibleWidth=1;
            var hl=bar.AddComponent<HorizontalLayoutGroup>();
            hl.padding=new RectOffset(6,6,0,0);hl.spacing=4;hl.childAlignment=TextAnchor.MiddleLeft;
            hl.childControlHeight=true;hl.childForceExpandHeight=false;hl.childControlWidth=true;hl.childForceExpandWidth=false;
            var strip=UITheme.Panel("strip",bar.transform,UITheme.Gold);
            var sle=strip.AddComponent<LayoutElement>();sle.preferredWidth=3;sle.preferredHeight=12;sle.minWidth=3;sle.minHeight=12;
            var tl=UITheme.Label("t",bar.transform,text,13,TextAnchor.MiddleLeft,UITheme.Gold,FontStyle.Bold);
            tl.gameObject.AddComponent<LayoutElement>().flexibleWidth=1;
        }
        /// <summary>V9.3.13 面板状态条：顶部一行核心指标（深底金字 28px），每面板唯一。</summary>
        private void StatusBar(Transform content,string text)
        {
            var bar=UITheme.Surface("stb",content,UITheme.HexA(0x232e50,0.9f));
            var le=bar.AddComponent<LayoutElement>();le.preferredHeight=28;le.flexibleWidth=1;
            UITheme.Label("t",bar.transform,text,13,TextAnchor.MiddleLeft,UITheme.Gold).SetInset(10,0);
        }
        private void BudgetRow(Transform content,string name,float val,bool income)
        {
            var row=UITheme.Panel("b_"+name,content,UITheme.HexA(0xffffff,0.05f));
            row.AddComponent<LayoutElement>().preferredHeight=18;   // V9.3.13 21→18
            UITheme.Label("n",row.transform,name,12,TextAnchor.MiddleLeft).SetInset(12,0.3f);
            UITheme.Label("v",row.transform,(income?"+":"−")+val.ToString("F0"),12,TextAnchor.MiddleRight,
                income?UITheme.Good:UITheme.Bronze).rectTransform.SetInsetRight(12);
        }
        /// <summary>0~1 仪表条：名称（左 30%）+ 轨道（30%~84%）+ 百分比（右）</summary>
        private void Gauge(Transform parent,string name,float ratio01)
        {
            float r=Mathf.Clamp01(ratio01);
            var row=UITheme.Panel("g_"+name,parent,UITheme.HexA(0xffffff,0.05f));
            row.AddComponent<LayoutElement>().preferredHeight=17;   // V9.3.13 19→17
            UITheme.Label("n",row.transform,name,12,TextAnchor.MiddleLeft).SetInset(12,0.5f);
            var track=UITheme.Panel("track",row.transform,UITheme.HexA(0x000000,0.16f));
            var trt=track.GetComponent<RectTransform>();
            trt.anchorMin=new Vector2(0.30f,0.16f);trt.anchorMax=new Vector2(0.57f,0.84f);trt.offsetMin=trt.offsetMax=Vector2.zero;   // V9.4.2 图表宽度缩50%：0.84→0.57（轨道 54%→27%）
            Color fc = r>=0.6f?UITheme.Good : r>=0.3f?UITheme.Gold : UITheme.Bad;
            var fill=UITheme.Panel("fill",track.transform,fc);
            var frt=fill.GetComponent<RectTransform>();
            frt.anchorMin=Vector2.zero;frt.anchorMax=new Vector2(Mathf.Max(0.02f,r),1);frt.offsetMin=frt.offsetMax=Vector2.zero;
            var val=UITheme.Label("v",row.transform,(ratio01*100f).ToString("F0")+"%",12,TextAnchor.MiddleRight,fc);
            val.rectTransform.SetInsetRight(12);
        }

        // ===== V6.1.8 九智能体共治（AI 多智能体议会；底部保留 v5.9.9 神话九神赐福） =====
        private void OpenGodsModal(){ FillGods(_godsModal);Open(_godsModal); }
        private void FillGods(GameObject modal)
        {
            var body=ModalBody(modal);Clear(body);
            UITheme.VerticalScroll("GodScroll",body.transform,out var content,2);   // V9.3.13 3→2
            var cou=GM.Council;
            if(cou==null){ UITheme.Label("noai",content,"九智能体系统未装配",13); return; }
            cou.ComputeContinuity();
            // —— 状态条：文明存续健康分 + 核心人口/资源指标 ——
            float cv=cou.Continuity;
            Color cvCol = cv>=70?UITheme.Good : cv>=40?UITheme.Gold : UITheme.Bad;
            int foodAmt=Mathf.FloorToInt(S.GetRes("food"));   // V9.2.3 C#9 插值孔洞不可嵌套引号，提取局部
            StatusBar(content,$"文明存续健康分　{cv:F0} / 100　人口{S.Pop}/{S.MaxPop}　粮{foodAmt}　民心{S.Happiness:F0}　天命{S.DynastyMorale:F0}　兜底续命{cou.SafetyCount}次");
            // —— 组1：共治控制 ——
            GroupTitle(content,"共治控制 · 神庭运行");
            var r1=Row(content,26);
            UITheme.Btn("en",r1.transform,cou.Enabled?"◉ 共治开启（自动）":"○ 共治已停（手动）",12).onClick.AddListener(()=>{cou.ToggleEnabled();FillGods(modal);});
            UITheme.Btn("mode",r1.transform,cou.Online?"🌐 联网·DeepSeek":"💾 离线·规则自治",12).onClick.AddListener(()=>{cou.SetOnline(!cou.Online);FillGods(modal);});
            UITheme.Btn("now",r1.transform,"⚡ 立即议政",12).onClick.AddListener(()=>{cou.CouncilNow();FillGods(modal);});
            string net = cou.Online ? (cou.NetOk?"联网正常":("联网失败→已自动离线："+cou.LastNetError)) : "离线自治（不耗Token，保证不断绝）";
            UITheme.Label("meta",content,
                $"Token {cou.TokensUsed}｜间隔 每{cou.IntervalYears}年｜上次 第{cou.LastCouncilYear}年｜{net}｜允许饥荒/灾难/动乱倒退，触红线强制托底，5000~10000年不断绝",
                11,TextAnchor.MiddleLeft,UITheme.Sky).gameObject.AddComponent<LayoutElement>().preferredHeight=18;
            // —— 组2：AI 密钥（弹窗输入） ——
            GroupTitle(content,"AI 密钥 · 九神联网议政");
            string keyShow = string.IsNullOrEmpty(cou.ApiKey) ? "未配置·离线规则自治" : "已配置 " + cou.ApiKey.Substring(0, Mathf.Min(5, cou.ApiKey.Length)) + "…";
            UITheme.Label("ais",content,"密钥："+keyShow+"｜模型 "+cou.Model+"｜"+cou.Endpoint.Replace("https://",""),11,TextAnchor.MiddleLeft,UITheme.Sky)
                .gameObject.AddComponent<LayoutElement>().preferredHeight=18;
            var rk=Row(content,24);
            var kbtn=UITheme.Btn("aikey",rk.transform,"设置 AI 密钥（弹窗输入）",12);
            kbtn.AddComponent<LayoutElement>().flexibleWidth=1;
            kbtn.onClick.AddListener(()=>{ var pr=gameObject.AddComponent<ApiKeyPrompt>(); pr.Show(GM, _=>{ if(_godsModal!=null && _godsModal.activeSelf) FillGods(_godsModal); }, _hud!=null?_hud.transform:null); });
            // —— 组3：九智能体 ——
            GroupTitle(content,"九智能体 · 职能议会");
            foreach (var g in cou.Gods)
            {
                Color lamp = !cou.Enabled?UITheme.HexA(0x888888,0.6f) : g.Urgency>=0.7f?UITheme.Bad : g.Urgency>=0.45f?UITheme.Gold : UITheme.Good;
                var row=UITheme.Panel("ag_"+g.Id,content,UITheme.HexA((int)g.Color,0.10f));
                row.AddComponent<LayoutElement>().preferredHeight=32;
                string dot = !cou.Enabled?"●休眠":(g.Urgency>=0.7f?"●预警":g.Urgency>=0.45f?"●治理":"●平稳");
                UITheme.Label("i",row.transform,
                    $"{g.Name}　{g.Domain}　{dot} 紧迫{g.Urgency:F0%}　第{g.LastYear}年·累计{g.Actions}次\n关注：{g.Focus}｜最近：{(string.IsNullOrEmpty(g.Note)?"—":g.Note)}",
                    11,TextAnchor.MiddleLeft,lamp).SetInset(8,0.04f);
            }
            // —— 组4：神话赐福（v5.9.9 保留） ——
            GroupTitle(content,"神话赐福 · v5.9.9 九神（保留）");
            foreach (var g in GM.Gods.Gods)
            {
                int last=GM.Gods.LastDecision.Or(g.Id);
                var row=UITheme.Panel("lg_"+g.Id,content,UITheme.HexA((int)g.Color,0.10f));
                row.AddComponent<LayoutElement>().preferredHeight=30;
                UITheme.Label("i",row.transform,$"{g.Name} · {g.Domain}（{g.Desc}）　上次显灵：第{last}年",12,TextAnchor.MiddleLeft).SetInset(8,0.24f);
                var b=UITheme.Btn("b",row.transform,"祈求显灵",11);
                var brt=b.GetComponent<RectTransform>();brt.anchorMin=new Vector2(0.77f,0.2f);brt.anchorMax=new Vector2(0.99f,0.8f);brt.offsetMin=brt.offsetMax=Vector2.zero;
                string id=g.Id;b.onClick.AddListener(()=>{GM.Gods.MakeDecision(id);FillGods(modal);});
            }
            FitModal(modal, 200, 520);
        }
        private GameObject Row(Transform parent,float h)
        {
            var r=UITheme.Panel("r",parent,new Color(0,0,0,0));
            r.AddComponent<LayoutElement>().preferredHeight=h;
            var hl=r.AddComponent<HorizontalLayoutGroup>();hl.spacing=6;hl.childForceExpandWidth=true;hl.childControlWidth=true;
            return r;
        }

        // ===== 海洋 =====
        private void OpenOceanModal(){ FillOcean(_oceanModal);Open(_oceanModal); }
        private void FillOcean(GameObject modal)
        {
            var body=ModalBody(modal);Clear(body);
            UITheme.VerticalScroll("OceanScroll",body.transform,out var content,2);   // V9.3.13 3→2
            var vl=content.GetComponent<VerticalLayoutGroup>();
            vl.spacing=4;vl.childControlWidth=true;vl.childForceExpandWidth=true;   // V9.3.12 6→4
            var sb=new StringBuilder();
            foreach (var k in OceanExpansionSystem.OceanResIds)
                sb.Append(OceanExpansionSystem.ResNames[k]).Append("：").Append(Mathf.FloorToInt(S.OceanResources.Or(k))).Append("  ");
            // —— 状态条 ——
            StatusBar(content,$"已开辟航线 {S.OceanDiscovered.Count} 条　殖民地 {S.Colonies.Count} 处　"+sb.ToString());
            // —— 组1：宝船行动 ——
            GroupTitle(content,"宝船 · 远洋贸易");
            var row=UITheme.Panel("row",content,new Color(0,0,0,0));
            var h=row.AddComponent<HorizontalLayoutGroup>();h.spacing=8;row.AddComponent<LayoutElement>().preferredHeight=26;
            UITheme.Btn("send",row.transform," 派遣宝船（木50金30）",12).onClick.AddListener(()=>{GM.Ocean.SendFleet();FillOcean(modal);});
            UITheme.Btn("build",row.transform," 建造宝船（木100铁10）",12).onClick.AddListener(()=>{GM.Naval.BuildTreasureShip();});
            UITheme.Btn("sell",row.transform," 出售特产",12).onClick.AddListener(()=>{GM.Ocean.SellOceanResources();FillOcean(modal);});
            // —— 组2：海洋探索副本 ——
            GroupTitle(content,"海洋探索副本 · 9×9 迷雾");
            BuildExpedition(content,"ocean",modal);
            UITheme.Label("tip2",content,"提示：明·清/大航海时代解锁；宝船远航发现港口，踩到⚓良港可建立殖民地，海怪/风暴有风险，战力补给耗尽自动返航。",11,TextAnchor.UpperLeft,UITheme.Sky);
            FitModal(modal, 260, 520);
        }

        // ===== 太空 =====
        private void OpenSpaceModal(){ FillSpace(_spaceModal);Open(_spaceModal); }
        private void FillSpace(GameObject modal)
        {
            var body=ModalBody(modal);Clear(body);
            UITheme.VerticalScroll("SpaceScroll",body.transform,out var content,2);   // V9.3.13 3→2
            var vl=content.GetComponent<VerticalLayoutGroup>();vl.spacing=4;vl.childControlWidth=true;vl.childForceExpandWidth=true;   // V9.3.12 6→4
            // —— 状态条 ——
            StatusBar(content,
                $"太空电梯 {Mathf.RoundToInt(S.SpElevator)}%　飞船 {S.SpShips}艘　戴森云 {Mathf.RoundToInt(S.SpDyson)}%　月球 {Mathf.RoundToInt(S.SpLunar)}%　火星 {Mathf.RoundToInt(S.SpMars)}%");
            // —— 组1：五大太空工程 ——
            GroupTitle(content,"太空工程 · 五大项目");
            var grid=UITheme.Panel("grid",content,new Color(0,0,0,0));
            var g=grid.AddComponent<GridLayoutGroup>();g.constraint=GridLayoutGroup.Constraint.FixedColumnCount;g.constraintCount=2;g.cellSize=new Vector2(168,26);g.spacing=new Vector2(2,1);   // V9.4.2 太空工程图表宽度缩50%：336→168
            ProjBtn(grid.transform," 太空电梯(钢50碳20)","elevator");
            ProjBtn(grid.transform," 宇宙飞船(钢30聚变10)","ship");
            ProjBtn(grid.transform," 戴森云(聚变50)","dyson");
            ProjBtn(grid.transform," 月球基地(钢60聚变15)","lunar");
            ProjBtn(grid.transform," 火星移民(钢100聚变30)","mars");
            // —— 组2：星际探索副本 ——
            GroupTitle(content,"星际探索副本 · 9×9 星图");
            BuildExpedition(content,"space",modal);
            FitModal(modal, 260, 520);
        }

        // ===== V6.1.6 统一副本网格（海图/星图）=====
        private void BuildExpedition(Transform content,string type,GameObject modal)
        {
            GM.Expedition.Prepare(type);   // 首次打开初始化网格（不切换 CurrentMap）
            var e=type=="space"?S.SpaceExp:S.OceanExp;
            UITheme.Label("epos",content,$"坐标({e.PosX},{e.PosY})  战力{Mathf.RoundToInt(e.Power)}/{Mathf.RoundToInt(e.MaxPower)}  补给{Mathf.RoundToInt(e.Supply)}",11,TextAnchor.MiddleLeft);   // V9.3.13 12→11
            // 9×9 网格
            var gp=UITheme.Panel("egrid",content,new Color(0,0,0,0));
            gp.AddComponent<LayoutElement>().preferredHeight=250;   // V9.3.13 9×9 网格 30→28px
            var eg=gp.AddComponent<GridLayoutGroup>();
            eg.constraint=GridLayoutGroup.Constraint.FixedColumnCount;eg.constraintCount=e.N;
            eg.cellSize=new Vector2(28,28);eg.spacing=new Vector2(0,0);   // V9.3.13 30→28 / 1→0
            for(int y=e.N-1;y>=0;y--)
                for(int x=0;x<e.N;x++)
                {
                    int idx=e.Idx(x,y); bool seen=e.Seen!=null&&idx<e.Seen.Length&&e.Seen[idx]==1;
                    bool cur=x==e.PosX&&y==e.PosY;
                    string node=seen?e.NodeKind[idx]:null;
                    string txt=cur?"★":(seen?(string.IsNullOrEmpty(node)?"·":ExpeditionSystem.NodeIcon(node)):"");
                    var cell=UITheme.Btn("c"+idx,eg.transform,txt,13);
                    cell.interactable=false;
                    var img=cell.GetComponent<Image>();
                    if(!seen) img.color=UITheme.HexA(0x000000,0.55f);
                    else if(cur) img.color=UITheme.HexA(0xFFD700,0.35f);
                    else if(!string.IsNullOrEmpty(node)) img.color=UITheme.HexA(type=="space"?0x2b3a67:0x1f4d5c,0.85f);
                    else img.color=UITheme.HexA(0xffffff,0.06f);
                }
            // 方向控制
            var d1=DirRow(content);
            UITheme.Btn("up",d1.transform,"⬆ 北",12).onClick.AddListener(()=>{EpMove(type,0,1,modal);});
            var d2=DirRow(content);
            UITheme.Btn("left",d2.transform,"⬅ 西",12).onClick.AddListener(()=>{EpMove(type,-1,0,modal);});
            UITheme.Btn("auto",d2.transform,"🔭 自动探索",12).onClick.AddListener(()=>{EpAuto(type,modal);});
            UITheme.Btn("right",d2.transform,"➡ 东",12).onClick.AddListener(()=>{EpMove(type,1,0,modal);});
            var d3=DirRow(content);
            UITheme.Btn("down",d3.transform,"⬇ 南",12).onClick.AddListener(()=>{EpMove(type,0,-1,modal);});
            UITheme.Btn("return",d3.transform,"🏠 返航整补",12).onClick.AddListener(()=>{GM.Expedition.ReturnHome(type);Refill(modal,type);});
            // 节点动作
            string here=GM.Expedition.CurrentNode(e);
            var d4=DirRow(content);
            if(type=="ocean")
            {
                var cb=UITheme.Btn("colonize",d4.transform,"⚓ 于此建立殖民地(金200木100)",12);
                cb.interactable=here=="port";
                cb.onClick.AddListener(()=>{GM.Expedition.ColonizeHere();Refill(modal,type);});
            }
            else
            {
                var ob=UITheme.Btn("outpost",d4.transform,here=="mars"?"🔴 建火星前哨":"🌙 建月球前哨",12);
                ob.interactable=here=="moon"||here=="mars";
                ob.onClick.AddListener(()=>{GM.Expedition.BuildOutpostHere();Refill(modal,type);});
            }
            UITheme.Label("elog",content,"探险日志："+(string.IsNullOrEmpty(e.LastEvent)?"（起航）":e.LastEvent),11,TextAnchor.UpperLeft,UITheme.Sky);
        }
        private GameObject DirRow(Transform parent)
        {
            var r=UITheme.Panel("dir",parent,new Color(0,0,0,0));
            r.AddComponent<LayoutElement>().preferredHeight=28;   // V9.3.13 32→28
            var h=r.AddComponent<HorizontalLayoutGroup>();h.spacing=6;h.childForceExpandWidth=true;h.childControlWidth=true;
            return r;
        }
        private void EpMove(string type,int dx,int dy,GameObject modal){ var msg=GM.Expedition.Move(type,dx,dy); if(!string.IsNullOrEmpty(msg))Toast(msg); Refill(modal,type); }
        private void EpAuto(string type,GameObject modal){ var msg=GM.Expedition.AutoExplore(type); if(!string.IsNullOrEmpty(msg))Toast(msg); Refill(modal,type); }
        private void Refill(GameObject modal,string type){ if(type=="space")FillSpace(modal); else FillOcean(modal); }
        private void ProjBtn(Transform grid,string label,string proj)
        { UITheme.Btn("p",grid,label,12).onClick.AddListener(()=>GM.Space.BuildProject(proj)); }

        // ===== V6.1.4 群雄争霸·讨伐 =====
        private void OpenCampaignModal(){ FillCampaign(_campaignModal);Open(_campaignModal); }
        private void FillCampaign(GameObject modal)
        {
            var body=ModalBody(modal);Clear(body);
            UITheme.VerticalScroll("CampScroll",body.transform,out var content,2);   // V9.3.13 3→2
            // V6.3.4 征兵/讨伐界面实拍内视图（Resources/Portraits/military/campaign_1.jpg，缺失回退矢量军营图）
            var campTex=PortraitLoader.Load("military","campaign",1);
            if(campTex!=null) UITheme.Portrait(content,campTex,120);   // V9.3.10 140→120
            else{
                var camph=UITheme.Surface("CampPortrait",content,UITheme.HexA(0x1b2440,0.92f));
                camph.AddComponent<LayoutElement>().preferredHeight=88;   // V9.3.13 92→88
                UITheme.SetOutline(camph,UITheme.Gold,1);
                var chh=camph.AddComponent<HorizontalLayoutGroup>();chh.childAlignment=TextAnchor.MiddleCenter;chh.spacing=12;chh.childControlHeight=true;chh.childForceExpandHeight=false;
                var cimg=UITheme.Icon(camph.transform,"military",64);cimg.rectTransform.sizeDelta=new Vector2(64,64);
                var ile=cimg.gameObject.AddComponent<LayoutElement>();ile.preferredWidth=64;ile.preferredHeight=64;ile.minWidth=64;ile.minHeight=64;
                UITheme.Label("cap",camph.transform,"军营内景 · 征兵 / 训练骑兵 / 出师讨伐",14,TextAnchor.MiddleLeft,UITheme.Gold);
            }
            int inf=0,cav=0; foreach(var u in S.FriendlyUnits){ if(u.IsCavalry)cav++;else inf++; }
            // —— 状态条 ——
            StatusBar(content,$"我方兵力：士兵{Mathf.RoundToInt(S.MilSoldiers)} 骑兵{Mathf.RoundToInt(S.MilCavalry)}　机动部队：步兵队{inf} 骑兵队{cav}");
            // —— 组1：征兵训练 ——
            GroupTitle(content,"征兵与训练");
            var tr=UITheme.Panel("tr",content,new Color(0,0,0,0));tr.AddComponent<LayoutElement>().preferredHeight=26;
            var th=tr.AddComponent<HorizontalLayoutGroup>();th.spacing=8;
            UITheme.Btn("train1",tr.transform,"征兵（粮20/人5）",12).onClick.AddListener(()=>{GM.Military.TrainSoldiers();FillCampaign(modal);});
            UITheme.Btn("train2",tr.transform,"训练骑兵（需马厩·粮30金20）",12).onClick.AddListener(()=>{GM.Military.TrainCavalry();FillCampaign(modal);});
            UITheme.Label("rule",content,"克制：骑兵克步兵（×1.5），箭塔/炮塔克骑兵；先征兵/训骑组建机动部队，再出师讨伐。攻克据点即兼并其地。",11,TextAnchor.UpperLeft,UITheme.Sky);
            // —— 组2：割据势力 · 讨伐 ——
            GroupTitle(content,"割据势力 · 讨伐兼并");
            var mil=GM.Military;
            if(!mil.FactionsInited||mil.Factions.Count==0)
                UITheme.Label("nf",content,"当前暂无割据势力（进入春秋战国·秦汉时代后群雄并起）。",13,TextAnchor.MiddleCenter);
            foreach(var f in mil.Factions)
            {
                var row=UITheme.Panel("f_"+f.Id,content,UITheme.HexA((int)f.ColorHex,f.Destroyed?0.06f:0.14f));
                row.AddComponent<LayoutElement>().preferredHeight=40;   // V9.3.13 46→40
                UITheme.Label("i",row.transform,
                    $"{f.Name}　{(f.Destroyed?"已覆灭":"人口"+Mathf.RoundToInt(f.Population)+" 守军"+f.Army.Count+" 军力"+Mathf.RoundToInt(f.Power))}",
                    12,TextAnchor.MiddleLeft).SetInset(8,0.24f);   // V9.3.13 13→12
                var b=UITheme.Btn("atk",row.transform,f.Destroyed?"已灭":"出师讨伐",12);
                var brt=b.GetComponent<RectTransform>();brt.anchorMin=new Vector2(0.76f,0.18f);brt.anchorMax=new Vector2(0.99f,0.82f);brt.offsetMin=brt.offsetMax=Vector2.zero;
                b.interactable=!f.Destroyed;
                string fid=f.Id;
                b.onClick.AddListener(()=>{ if(GM.Military.LaunchCampaign(fid)){FillCampaign(modal);} });
            }
            FitModal(modal, 180, 520);
        }

        // ===== V6.1.5 殖民时代 =====
        private void OpenColonyModal(){ FillColony(_colonyModal);Open(_colonyModal); }
        private void FillColony(GameObject modal)
        {
            var body=ModalBody(modal);Clear(body);
            UITheme.VerticalScroll("ColScroll",body.transform,out var content,2);   // V9.3.13 3→2
            var col=GM.Colonization;
            bool open=col.EraOpen;
            // —— 状态条 ——
            StatusBar(content,
                (open?"殖民时代已开启（大航海/明·清）":"尚未进入殖民时代（公元1000年大航海、明·清时代开启）")+
                $"　海外领地 {S.Colonies.Count} 处（每处 +2% 金币，8处达成日不落）");
            // —— 组1：殖民行动 ——
            GroupTitle(content,"殖民行动");
            var fr=UITheme.Panel("fr",content,new Color(0,0,0,0));fr.AddComponent<LayoutElement>().preferredHeight=26;
            var fh=fr.AddComponent<HorizontalLayoutGroup>();fh.spacing=8;
            var found=UITheme.Btn("found",fr.transform,"建立殖民地（金200木100·需1船）",11);
            found.interactable=open;
            found.onClick.AddListener(()=>{col.FoundColony();FillColony(modal);});
            // —— 组2：海外领地列表 ——
            GroupTitle(content,"海外领地 · 贸易站→殖民地→领地");
            if(S.Colonies.Count==0)
                UITheme.Label("empty",content,"尚无殖民地。可直接远航建立，或在海洋探索副本踩到⚓良港时建立。",11,TextAnchor.UpperLeft);
            foreach(var c in S.Colonies)
            {
                var row=UITheme.Panel("c_"+c.Id,content,UITheme.HexA(0xFFD700,0.08f));
                row.AddComponent<LayoutElement>().preferredHeight=52;
                string resName=OceanExpansionSystem.ResNames.TryGetValue(c.ResId,out var rn)?rn:c.ResId;
                UITheme.Label("i",row.transform,
                    $"🌍 {c.Name}　Lv.{c.Level}({(c.Level==1?"贸易站":c.Level==2?"殖民地":"领地")})　特产:{resName}　人口{Mathf.RoundToInt(c.Pop)}　安定{Mathf.RoundToInt(c.Loyalty)}%",
                    12,TextAnchor.UpperLeft).SetInset(8,0.36f);
                var ub=UITheme.Btn("up",row.transform,c.Level>=3?"已领地":"升格",11);
                var urt=ub.GetComponent<RectTransform>();urt.anchorMin=new Vector2(0.64f,0.12f);urt.anchorMax=new Vector2(0.81f,0.88f);urt.offsetMin=urt.offsetMax=Vector2.zero;
                ub.interactable=c.Level<3;
                ub.onClick.AddListener(()=>{col.Upgrade(c);FillColony(modal);});
                var sb=UITheme.Btn("sup",row.transform,"镇压(粮50)",11);
                var srt=sb.GetComponent<RectTransform>();srt.anchorMin=new Vector2(0.83f,0.12f);srt.anchorMax=new Vector2(1f,0.88f);srt.offsetMin=srt.offsetMax=Vector2.zero;
                sb.onClick.AddListener(()=>{col.Suppress(c);FillColony(modal);});
            }
            FitModal(modal, 180, 520);
        }

        // ===== 建筑信息 =====
public void ShowBuilding(BuildingEntity b)
        {
            if (_buildingModal==null || b==null || b.Def==null) return;   // V9.1.3 防御：面板未装配或实体/定义缺失时不裸访问
            Debug.Log("[UI] ShowBuilding "+b.Def.Id+" Lv."+b.Level);   // V9.3.7 选中路径探针（浏览器回归观测点）
            _buildingModal.SetActive(true);
            var box=_buildingModal.transform.Find("Box").GetComponent<RectTransform>();
            box.sizeDelta=new Vector2(460,620);
            var ht=_buildingModal.transform.Find("Box/Head/title");
            if(ht!=null) ht.GetComponent<Text>().text=$"{b.Def.Icon} {b.Def.Name}  Lv.{b.Level}";
            var body=ModalBody(_buildingModal);Clear(body);
            var vl=body.AddComponent<VerticalLayoutGroup>();vl.spacing=7;vl.padding=new RectOffset(6,6,4,4);
            vl.childControlWidth=true;vl.childForceExpandWidth=true;vl.childControlHeight=false;
            // V6.1.9(j) 实拍内视图（随等级切换，缺失回退大 Emoji 占位）
            var btex=PortraitLoader.Load("building",b.Type,b.Level);
            if(btex!=null) UITheme.Portrait(body.transform,btex,176);
            else { var ph=UITheme.Panel("ph",body.transform,UITheme.HexA(0x1b2440,0.9f));ph.AddComponent<LayoutElement>().preferredHeight=150;UITheme.SetOutline(ph,UITheme.Gold,1);var pe=UITheme.Label("e",ph.transform,b.Def.Icon,56,TextAnchor.MiddleCenter);pe.rectTransform.anchorMin=Vector2.zero;pe.rectTransform.anchorMax=Vector2.one;pe.rectTransform.offsetMin=pe.rectTransform.offsetMax=Vector2.zero; }
            int designLife=50+b.Level*30;
            AgentIntroBlock(AI.Agents.AgentDirector.BuildingTypeToAgent(b.Type), body.transform);
            float house=b.Def.GetFunc("housing");
            var sb=new StringBuilder();
            sb.Append("分类：").Append(b.Def.Cat).Append("　时代：").Append(GM.Eras[b.Def.Era].Name).Append('\n');
            sb.Append("寿命：设计约 ").Append(designLife).Append(" 年起　已使用 ").Append(b.Age).Append(" 年\n");
            sb.Append("耐久：").Append(Mathf.RoundToInt(b.Hp)).Append("%\n");
            if(house>0) sb.Append("可居住 ").Append(Mathf.RoundToInt(house*b.LevelMult)).Append(" 人（随等级提升）\n");
            float atk=b.Def.GetFunc("attack");
            if(atk>0) sb.Append("攻击 ").Append(Mathf.RoundToInt(atk*(1+(b.Level-1)*0.3f)))
                      .Append("　射程 ").Append(Mathf.RoundToInt(b.Def.GetFunc("range")))
                      .Append("　防御 ").Append(Mathf.RoundToInt(b.Def.GetFunc("defense"))).Append('\n');
            if(!string.IsNullOrEmpty(b.Def.Desc)) sb.Append(b.Def.Desc);
            UITheme.Label("meta",body.transform,sb.ToString(),13,TextAnchor.UpperLeft)
                .gameObject.AddComponent<LayoutElement>().preferredHeight=118;
            var row=UITheme.Panel("row",body.transform,new Color(0,0,0,0));
            var h=row.AddComponent<HorizontalLayoutGroup>();h.spacing=8;row.AddComponent<LayoutElement>().preferredHeight=40;
            var upCost=GM.Building.UpgradeCost(b);
            var up=UITheme.Btn("up",row.transform,upCost==null?" 已达满级":(" 升级（"+CostText(upCost)+"）"),13);
            up.interactable=GM.Building.CanUpgrade(b);
            BuildingEntity cap=b;
            up.onClick.AddListener(()=>{GM.Building.Upgrade(cap);ShowBuilding(cap);});
            UITheme.Btn("del",row.transform," 拆除(返50%)",13).onClick.AddListener(()=>{GM.Building.Demolish(cap);_buildingModal.SetActive(false);});
        }

        // ===== 船只浮窗（对齐 v5.9.9 船只小窗：等级/船员/耐久/攻击/居住，可升级/解散，ESC·右键·点空白关闭）=====
        public void ShowShip(ShipEntity s)
        {
            _selectedShip=s; FillShip(s); _shipModal.SetActive(true);
        }
        // V9.8.0 智能体身份区块：在属性面板显示该对象的自我介绍
        private void AgentIntroBlock(string agentId, Transform parent)
        {
            var director = GM.Agents;
            if (director==null) return;
            var a = director.Registry.Get(agentId);
            if (a==null) return;
            UITheme.Label("agent",parent,"💬 "+a.Intro,12,TextAnchor.UpperLeft,new Color(0.80f,0.92f,1f));
        }
        public void CloseShipCard()
        {
            _selectedShip=null;
            if (_shipModal!=null) _shipModal.SetActive(false);
        }
        private void FillShip(ShipEntity s)
        {
            var body=ModalBody(_shipModal);Clear(body);
            var vl=body.AddComponent<VerticalLayoutGroup>();vl.spacing=7;vl.padding=new RectOffset(10,10,8,8);
            vl.childControlWidth=true;vl.childForceExpandWidth=true;
            var naval=GM.Naval;
            bool hasDef=naval.Defs.TryGetValue(s.ShipTypeId,out var d);
            string icon=hasDef?d.Icon:"🚢";
            UITheme.Label("name",body.transform,$"{icon} {s.Name} · {naval.LevelName(s)}",19,TextAnchor.MiddleCenter,UITheme.Gold);
            AgentIntroBlock(AI.Agents.AgentDirector.ShipTypeToAgent(s.ShipTypeId), body.transform);
            var stex=PortraitLoader.Load("ship",s.ShipTypeId,s.Level);
            if(stex!=null) UITheme.Portrait(body.transform,stex);
            var sb=new StringBuilder();
            sb.Append("类型：").Append(s.Military?"军用舰船":"民用船只").Append('\n');
            sb.Append("船员：").Append(s.Crew).Append("/").Append(naval.Capacity(s)).Append("人\n");
            if(s.Level>=2){ if(s.Military) sb.Append("<color=#8be9fd>⚔ 作战载人：").Append(s.Crew).Append("/").Append(naval.Capacity(s)).Append("（Lv2已激活）</color>\n");
                else sb.Append("<color=#8be9fd>👥 载客：").Append(s.Passengers).Append("/").Append(s.EffectiveHousing).Append("（Lv2已激活，附近居民自动登船）</color>\n"); }
            sb.Append("耐久：").Append(Mathf.Max(0,Mathf.RoundToInt(s.Hp))).Append("/").Append(Mathf.RoundToInt(s.MaxHp)).Append('\n');
            if (s.Military) sb.Append("攻击：").Append(naval.AttackOf(s)).Append("  射程：").Append(Mathf.RoundToInt(naval.RangeOf(s))).Append("  发现：").Append(Mathf.RoundToInt(naval.DetectRangeOf(s))).Append("格\n");   // V9.3.4 射程/发现随等级成长
            sb.Append("速度：").Append(naval.SpeedOf(s).ToString("0.00")).Append('\n');
            if(s.ShipTypeId=="aircraft_carrier") sb.Append("<color=#8be9fd>🛫 挂载舰载机/直升机/喷气机 ").Append(s.CarrierAir).Append(" 架</color>\n");   // V9.3.3 航母载机
            if(s.FactionId.Length>0) sb.Append("<color=#ff7a45>⛵ 敌对阵营：").Append(s.FactionId).Append("</color>\n");   // V9.3.3 敌舰阵营
            sb.Append("<color=#8be9fd>🏠 居住 ").Append(s.EffectiveHousing).Append(" 人</color>");
            UITheme.Label("meta",body.transform,sb.ToString(),13,TextAnchor.UpperLeft);
            var row=UITheme.Panel("row",body.transform,new Color(0,0,0,0));
            var h=row.AddComponent<HorizontalLayoutGroup>();h.spacing=8;row.AddComponent<LayoutElement>().preferredHeight=40;
            var cost=naval.UpgradeCost(s);
            string upLabel = cost==null ? " 已达传奇" : (" 升级("+CostText(cost)+")");
            var up=UITheme.Btn("up",row.transform,upLabel,13);
            up.interactable=cost!=null;
            up.onClick.AddListener(()=>{ if(naval.UpgradeShip(s)) FillShip(s); });
            UITheme.Btn("scuttle",row.transform," 解散",13).onClick.AddListener(()=>{naval.ScuttleShip(s);CloseShipCard();});
        }
        // ===== 车辆浮窗（V6.1.3，对齐船只小窗：等级/运力/耐久，可升级，实拍图随等级切换）=====
        public void ShowCart(CartEntity c)
        {
            _selectedCart=c; FillCart(c); _cartModal.SetActive(true);
        }
        public void CloseCart()
        {
            _selectedCart=null;
            if(_cartModal!=null) _cartModal.SetActive(false);
        }
        private void FillCart(CartEntity c)
        {
            var body=ModalBody(_cartModal);Clear(body);
            var vl=body.AddComponent<VerticalLayoutGroup>();vl.spacing=7;vl.padding=new RectOffset(10,10,8,8);
            vl.childControlWidth=true;vl.childForceExpandWidth=true;
            var cart=GM.Cart;
            bool hasDef=cart.Defs.TryGetValue(c.CartTypeId,out var cd);
            string icon=hasDef?cd.Icon:"🛒";
            UITheme.Label("name",body.transform,$"{icon} {c.Name} · {cart.CartLevelName(c)}",19,TextAnchor.MiddleCenter,UITheme.Gold);
            AgentIntroBlock(AI.Agents.AgentDirector.CartTypeToAgent(c.CartTypeId), body.transform);
            var ctex=PortraitLoader.Load("cart",c.CartTypeId,c.Level);
            if(ctex!=null) UITheme.Portrait(body.transform,ctex);
            var sb=new StringBuilder();
            sb.Append("类型：陆地车辆\n");
            sb.Append("运力：").Append(c.Capacity).Append(" 单位\n");
            sb.Append("耐久：").Append(Mathf.Max(0,c.Durability)).Append("/").Append(c.MaxDurability).Append('\n');
            sb.Append("驾驶员：").Append(c.HasDriver?"有":"无");
            if(c.Level>=2) sb.Append("\n<color=#8be9fd>👥 载客：").Append(c.Passengers).Append("/").Append(c.Capacity).Append("（Lv2已激活，附近居民自动登车）</color>");
            UITheme.Label("meta",body.transform,sb.ToString(),13,TextAnchor.UpperLeft);
            var row=UITheme.Panel("row",body.transform,new Color(0,0,0,0));
            var h=row.AddComponent<HorizontalLayoutGroup>();h.spacing=8;row.AddComponent<LayoutElement>().preferredHeight=40;
            var cost=cart.CartUpgradeCost(c);
            string upLabel=cost==null?" 已达传奇":(" 升级("+CostText(cost)+")");
            var up=UITheme.Btn("up",row.transform,upLabel,13); up.interactable=cost!=null;
            up.onClick.AddListener(()=>{ if(cart.UpgradeCart(c)) FillCart(c); });
            UITheme.Btn("close",row.transform," 关闭",13).onClick.AddListener(CloseCart);
        }

        // ===== V9.5.7 地面部队属性面板（点击可查看属性/升级/战绩；战斗击杀自动战功升级）=====
        public void ShowGround(Systems.GroundWarfareSystem.GroundUnit u)
        {
            _selectedGround=u; FillGround(u); _groundModal.SetActive(true);
        }
        public void CloseGround()
        {
            _selectedGround=null;
            if(_groundModal!=null) _groundModal.SetActive(false);
        }
        private void FillGround(Systems.GroundWarfareSystem.GroundUnit u)
        {
            var body=ModalBody(_groundModal);Clear(body);
            var vl=body.AddComponent<VerticalLayoutGroup>();vl.spacing=7;vl.padding=new RectOffset(10,10,8,8);
            vl.childControlWidth=true;vl.childForceExpandWidth=true;
            var gw=GM.Ground;
            bool hasDef=gw.Defs.TryGetValue(u.TypeId,out var d);
            string icon=hasDef?d.Icon:"🛡️";
            string era=u.TypeId is "cavalry" or "phalanx" or "chariot" ? "古典（1949前）":"现代（1949起）";
            UITheme.Label("name",body.transform,$"{icon} {u.Name} · Lv{u.Level}",19,TextAnchor.MiddleCenter,UITheme.Gold);
            AgentIntroBlock(AI.Agents.AgentDirector.GroundTypeToAgent(u.TypeId), body.transform);
            var sb=new StringBuilder();
            // V9.7.2 十维参数：速度/雷达/射速显示【实际生效值】（EffectiveSpeed 等），与实际移动/开火口径唯一
            float effSpd=gw.EffectiveSpeed(u), effRadar=gw.EffectiveRadar(u), effFire=gw.EffectiveFireInterval(u);
            sb.Append("编制：").Append(u.Side=="ours"?"我方":"敌方").Append("　").Append(era);
            sb.Append("　").Append(u.Rank).Append("　智能Lv").Append(u.Intel).Append('\n');
            sb.Append("耐久：").Append(Mathf.Max(0,Mathf.RoundToInt(u.Hp))).Append("/").Append(Mathf.RoundToInt(u.MaxHp));
            sb.Append("　防御：").Append(Mathf.RoundToInt(u.Defense)).Append('\n');
            sb.Append("攻击：").Append(u.BaseAttack).Append("　射程：").Append(u.Range).Append("格");
            sb.Append("　射速：").Append(effFire.ToString("0.00")).Append("秒/发\n");
            sb.Append("速度：").Append(effSpd.ToString("0.00")).Append("（世界单位/秒）");
            sb.Append("　雷达：").Append(effRadar.ToString("0.0")).Append("格\n");
            sb.Append("载人：").Append(u.Capacity).Append("　体积：").Append(u.Footprint.ToString("0.0"));
            sb.Append("　战绩：").Append(u.Kills).Append(" 杀\n");
            if(u.FactionId.Length>0) sb.Append("<color=#ff7a45>⚑ 敌对阵营：").Append(u.FactionId).Append("</color>\n");
            if(hasDef && !string.IsNullOrEmpty(d.Desc)) sb.Append(d.Desc);
            UITheme.Label("meta",body.transform,sb.ToString(),13,TextAnchor.UpperLeft);
            if(u.Side=="ours")
            {
                var row=UITheme.Panel("row",body.transform,new Color(0,0,0,0));
                var h=row.AddComponent<HorizontalLayoutGroup>();h.spacing=8;row.AddComponent<LayoutElement>().preferredHeight=40;
                var cost=gw.UpgradeGroundCost(u);
                string upLabel=cost==null?" 已达最高等级":(" 升级("+CostText(cost)+")");
                var up=UITheme.Btn("up",row.transform,upLabel,13); up.interactable=cost!=null;
                up.onClick.AddListener(()=>{ if(gw.UpgradeGround(u)) FillGround(u); });
                UITheme.Btn("close",row.transform," 关闭",13).onClick.AddListener(CloseGround);
            }
            else
            {
                var row=UITheme.Panel("row",body.transform,new Color(0,0,0,0));
                row.AddComponent<LayoutElement>().preferredHeight=40;
                UITheme.Btn("close",row.transform," 关闭",13).onClick.AddListener(CloseGround);
            }
        }

        // ===== V9.8.0 智能体驱动面板（身份浏览/自我介绍/协作消息/模式切换）=====
        public void ShowAgent(){ FillAgent(); _agentModal.SetActive(true); }
        public void CloseAgent(){ if(_agentModal!=null) _agentModal.SetActive(false); }

        private int _agentFilter = -1;   // -1 全部；否则 AgentKind

        private void FillAgent()
        {
            var body=ModalBody(_agentModal); Clear(body);
            var vl=body.AddComponent<VerticalLayoutGroup>();
            vl.spacing=4; vl.padding=new RectOffset(8,8,6,6);
            vl.childControlWidth=true; vl.childForceExpandWidth=true;
            vl.childControlHeight=true; vl.childForceExpandHeight=false;   // V9.8.0 fix：必须控制高度，否则滚动区不拉伸（参照 Debug 面板）
            var dir=GM.Agents;
            if(dir==null){ UITheme.Label("x",body.transform,"智能体层未就绪",13); return; }

            // 状态行
            UITheme.Label("stat",body.transform,
                $"模式：{dir.Mode}｜智能体 {dir.Registry.Count}｜自主决策 {dir.TotalDecisions}｜LLM {dir.LlmCalls}｜行为库 {dir.Library.Entries.Count}",
                13,TextAnchor.MiddleLeft,new Color(0.35f,0.5f,0.6f));

            // 操作按钮行
            var top=UITheme.Panel("top",body.transform,new Color(0,0,0,0));
            var tle=top.AddComponent<LayoutElement>(); tle.preferredHeight=32; tle.layoutPriority=2;
            var th=top.AddComponent<HorizontalLayoutGroup>();
            th.spacing=6; th.padding=new RectOffset(4,4,2,2);
            th.childControlWidth=true; th.childForceExpandWidth=true;
            th.childControlHeight=true; th.childForceExpandHeight=false;
            UITheme.Btn("mode",top.transform," 切换模式",12).onClick.AddListener(()=>{dir.CycleMode();FillAgent();});
            UITheme.Btn("help",top.transform," 模拟求助",12).onClick.AddListener(()=>{
                dir.CallForHelp("ship_destroyer","combat","我方驱逐舰在战斗中被围攻，请求附近支援！");
                dir.ResolvePendingProbe(); FillAgent();
            });
            UITheme.Btn("save",top.transform," 保存行为库",12).onClick.AddListener(()=>{dir.SaveLibrary();});
            UITheme.Btn("close",top.transform," 关闭",12).onClick.AddListener(CloseAgent);

            // 类别过滤行
            var filt=UITheme.Panel("filt",body.transform,new Color(0,0,0,0));
            var fle=filt.AddComponent<LayoutElement>(); fle.preferredHeight=28; fle.layoutPriority=2;
            var fh=filt.AddComponent<HorizontalLayoutGroup>();
            fh.spacing=4; fh.padding=new RectOffset(2,2,2,2);
            fh.childControlWidth=true; fh.childForceExpandWidth=true;
            fh.childControlHeight=true; fh.childForceExpandHeight=false;
            AddFilterBtn(filt.transform,"全部",-1);
            foreach(AI.Agents.AgentKind k in Enum.GetValues(typeof(AI.Agents.AgentKind)))
                AddFilterBtn(filt.transform,k.ToString(),(int)k);

            // 滚动列表（直接挂 body，参照 Debug 面板；多套一层裸 Panel 会塌缩）
            var sr=UITheme.VerticalScroll("sr",body.transform,out var content,3);
            sr.gameObject.AddComponent<LayoutElement>().flexibleHeight=1;
            sr.movementType=ScrollRect.MovementType.Clamped; sr.scrollSensitivity=30f;
            var cvlg=content.GetComponent<VerticalLayoutGroup>();
            cvlg.childControlHeight=true; cvlg.childForceExpandHeight=false;

            var all=new List<AI.Agents.AgentIdentity>();
            if(_agentFilter<0) all.AddRange(dir.Registry.All);
            else all.AddRange(dir.Registry.OfKind((AI.Agents.AgentKind)_agentFilter));
            all.Sort((a,b)=>a.Id.CompareTo(b.Id));
            foreach(var a in all)
            {
                var row=UITheme.Panel("arow",content,new Color(0.15f,0.25f,0.3f,0.10f));
                var rle=row.AddComponent<LayoutElement>(); rle.preferredHeight=42;   // V9.8.0 fix：固定行高（参照 Debug 面板），content 高度确定可滚动
                var rvl=row.AddComponent<VerticalLayoutGroup>();
                rvl.spacing=1; rvl.padding=new RectOffset(6,6,3,3);
                rvl.childControlWidth=true; rvl.childForceExpandWidth=true;
                rvl.childControlHeight=true; rvl.childForceExpandHeight=false;
                UITheme.Label("n",row.transform,$"[{a.Kind}] {a.Name}（{a.Domain}）",12,TextAnchor.UpperLeft,UITheme.Gold);
                UITheme.Label("i",row.transform,a.Intro,11,TextAnchor.UpperLeft,new Color(0.2f,0.25f,0.3f));
            }
        }

        private void AddFilterBtn(Transform parent,string label,int v)
        {
            var b=UITheme.Btn("f",parent,label,11);
            b.onClick.AddListener(()=>{ _agentFilter=v; FillAgent(); });
        }

        // V9.8.0 诊断探针：读取面板 viewport / content 真实尺寸，定位滚动问题
        [UnityEngine.Scripting.Preserve]
        public void WebAgentPanel()
        {
            if (_agentModal==null){ Debug.Log("[PANEL] modal null"); return; }
            Debug.Log($"[PANEL] Screen={Screen.width}x{Screen.height}");
            var box=_agentModal.transform.Find("Box") as RectTransform;
            Debug.Log("[PANEL] Box  "+RectScreen(box));
            var head=_agentModal.transform.Find("Box/Head") as RectTransform;
            Debug.Log("[PANEL] Head "+RectScreen(head)+" localSize="+head.sizeDelta+" comps="+string.Join(",",head.GetComponents<Component>().Select(c=>c.GetType().Name)));
            var body=_agentModal.transform.Find("Box/Body") as RectTransform;
            Debug.Log("[PANEL] Body "+RectScreen(body));
            var top=body.Find("top") as RectTransform;
            var filt=body.Find("filt") as RectTransform;
            Debug.Log("[PANEL] top localSize="+top.sizeDelta+" comps="+string.Join(",",top.GetComponents<Component>().Select(c=>c.GetType().Name)));
            Debug.Log("[PANEL] filt localSize="+filt.sizeDelta+" comps="+string.Join(",",filt.GetComponents<Component>().Select(c=>c.GetType().Name)));
            var srT=_agentModal.transform.Find("Box/Body/sr") as RectTransform;
            if (srT==null){ Debug.Log("[PANEL] sr not found"); return; }
            var sr=srT.GetComponent<ScrollRect>();
            Debug.Log("[PANEL] sr   "+RectScreen(srT));
            var content=sr.content;
            Debug.Log($"[PANEL] Content h={content.rect.height:0} childCount={content.childCount} scrollable={content.rect.height>srT.rect.height} normPos={sr.verticalNormalizedPosition:0.00}");
        }

        private static string RectScreen(RectTransform rt)
        {
            if(rt==null) return "null";
            var corners=new Vector3[4]; rt.GetWorldCorners(corners);
            var bl=RectTransformUtility.WorldToScreenPoint(null,corners[0]);
            var tr=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            // 转成从顶部算（y_top = H - y）
            return $"top=({bl.x:0},{Screen.height-tr.y:0}) size=({tr.x-bl.x:0}x{tr.y-bl.y:0})";
        }

        // V9.8.0 诊断探针：列出某屏幕坐标处 GraphicRaycaster 命中的对象（含层级深度）
        [UnityEngine.Scripting.Preserve]
        public void WebAgentRaycast(string xy)
        {
            var p=xy.Split(',');
            var pos=new Vector2(float.Parse(p[0]),float.Parse(p[1]));
            var gr=_hud!=null?_hud.GetComponentInParent<GraphicRaycaster>():null;
            if(gr==null){ Debug.Log("[HIT] 未找到 GraphicRaycaster"); return; }
            var ped=new PointerEventData(EventSystem.current){ position=pos };
            var results=new List<RaycastResult>();
            gr.Raycast(ped,results);
            var sb=new System.Text.StringBuilder();
            sb.AppendLine($"[HIT] at ({pos.x},{pos.y}) count={results.Count}");
            foreach(var r in results) sb.AppendLine("  - "+r.gameObject.name+" depth="+r.depth);
            Debug.Log(sb.ToString());
        }

        // V9.8.0 诊断：Unity 实际读取到的鼠标输入状态
        [UnityEngine.Scripting.Preserve]
        public void WebInputCheck()
        {
            Debug.Log($"[INPUT] mousePos={Input.mousePosition.x:0},{Input.mousePosition.y:0} btn0={Input.GetMouseButton(0)} btn0Down={Input.GetMouseButtonDown(0)} screen={Screen.width}x{Screen.height} es={(EventSystem.current==null?"null":EventSystem.current.name)}");
        }

        // ===== V9.3.8 树木属性面板（查看树龄/高度/健康/培育等级；培育升级：金10+粮5 → 等级+1、+2×等级文化）=====
        public void ShowTree(TreeRecord r)
        {
            _selectedTree=r; FillTree(r); _treeModal.SetActive(true);
        }
        public void CloseTree()
        {
            _selectedTree=null;
            if(_treeModal!=null) _treeModal.SetActive(false);
        }
        private void FillTree(TreeRecord r)
        {
            var body=ModalBody(_treeModal);Clear(body);
            var vl=body.AddComponent<VerticalLayoutGroup>();vl.spacing=7;vl.padding=new RectOffset(10,10,8,8);
            vl.childControlWidth=true;vl.childForceExpandWidth=true;
            var veg=UnityEngine.Object.FindObjectOfType<VegetationSystem>();
            string icon=r.Kind==2?"🌳":"🌲";
            string name=VegetationSystem.TreeName(r);
            string lv=veg!=null?veg.TreeLevelName(r):(r.Level>=3?"巨木":(r.Level==2?"壮株":"幼株"));
            UITheme.Label("name",body.transform,$"{icon} {name} · {lv}",19,TextAnchor.MiddleCenter,UITheme.Gold);
            AgentIntroBlock(r.Kind==2?"nat_banyan":(r.Kind==1?"nat_pine":"nat_tree"), body.transform);
            var sb=new StringBuilder();
            sb.Append("分类：乔木（").Append(r.Kind==2?"村落榕树":(r.Kind==1?"针叶":"阔叶")).Append("）\n");
            int age=veg!=null?veg.TreeAge(r):0;
            sb.Append("树龄：").Append(age).Append(" 游戏年\n");
            sb.Append("高度：约 ").Append(Mathf.RoundToInt((veg!=null?veg.TreeHeightM(r):1f)*10f)/10f).Append(" 米\n");
            sb.Append("健康：").Append(Mathf.RoundToInt(r.Health)).Append("%\n");
            if(r.Kind==2) sb.Append("寿命：300~800 游戏年（枯荣更替，原地萌发）\n");
            sb.Append("<color=#8be9fd>🌿 培育成长：每级 +2 文化实力</color>");
            UITheme.Label("meta",body.transform,sb.ToString(),13,TextAnchor.UpperLeft);
            var row=UITheme.Panel("row",body.transform,new Color(0,0,0,0));
            var h=row.AddComponent<HorizontalLayoutGroup>();h.spacing=8;row.AddComponent<LayoutElement>().preferredHeight=40;
            var up=UITheme.Btn("up",row.transform,r.Level>=3?" 已成材，福荫四方":" 培育(10金+5粮)",13);
            up.interactable=r.Level<3;
            TreeRecord cap=r;
            up.onClick.AddListener(()=>{ if(veg!=null && veg.NurtureTree(cap)) FillTree(cap); });
            UITheme.Btn("close",row.transform," 关闭",13).onClick.AddListener(CloseTree);
        }

        private static string CostText(Dictionary<string,int> cost)
        {
            var sb=new StringBuilder();bool first=true;
            foreach(var kv in cost){ if(!first)sb.Append(' ');first=false;sb.Append(ResourceDatabase.Icons.TryGetValue(kv.Key,out var ic)?ic:"").Append(kv.Value); }
            return sb.ToString();
        }

        private void Clear(GameObject body)
        {
            for(int i=body.transform.childCount-1;i>=0;i--)Destroy(body.transform.GetChild(i).gameObject);
            // 移除上一次填充挂载的布局组件，避免重复叠加
            foreach(var lg in body.GetComponents<HorizontalOrVerticalLayoutGroup>())DestroyImmediate(lg);
            foreach(var g in body.GetComponents<GridLayoutGroup>())DestroyImmediate(g);
        }
    }

    /// <summary>V9.3.13 FitModal 尺寸参数记录（面板首次激活后协程重放用）</summary>
    public class FitModalSpec : MonoBehaviour
    {
        public float MinH, MaxH;
    }

    internal static class RectExt
    {
        public static Text SetInset(this Text t,float left,float rightRatio)
        {
            t.rectTransform.anchorMin=new Vector2(0,0);
            t.rectTransform.anchorMax=new Vector2(Mathf.Max(0.05f,1f-rightRatio),1f);
            t.rectTransform.offsetMin=new Vector2(left,2);
            t.rectTransform.offsetMax=new Vector2(-4,-2);
            return t;
        }
        /// <summary>右对齐数值文字：锚定父容器右侧，垂直居中，留 right 像素右边距</summary>
        public static RectTransform SetInsetRight(this RectTransform t,float right)
        {
            t.anchorMin=new Vector2(0.70f,0f);t.anchorMax=new Vector2(1f,1f);
            t.offsetMin=new Vector2(0,2f);t.offsetMax=new Vector2(-right,-2f);
            return t;
        }
    }
}
