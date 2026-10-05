// V9.5.4 军事 tab：AvailableDefs 分代遍历 + 动态标签 + 古典造价字典；基建分类卡片
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using PixelToCivilization.Core;
using PixelToCivilization.Systems;

namespace PixelToCivilization.UI
{
    public partial class UIManager : MonoBehaviour
    {
        // 建造面板 11 分类
        string[] BuildTabsV2 = { "居住", "农业", "工业", "经济", "文化", "军事", "基建", "交通", "科技", "能源", "太空" };

        public void WebOpenBuildTab(string alias)
        {
            string tab = alias;
            switch (alias)
            {
                case "military": case "junshi": case "军事": tab = "军事"; break;
                case "infra": case "基建": case "jijian": tab = "基建"; break;
                case "traffic": case "交通": tab = "交通"; break;
                case "housing": case "居住": tab = "居住"; break;
                case "agri": case "农业": tab = "农业"; break;
                case "industry": case "工业": tab = "工业"; break;
                case "economy": case "经济": tab = "经济"; break;
                case "culture": case "文化": tab = "文化"; break;
                case "tech": case "科技": tab = "科技"; break;
                case "energy": case "能源": tab = "能源"; break;
                case "space": case "太空": tab = "太空"; break;
            }
            _openBuildTab = tab;
        }

        string _openBuildTab = "居住";

        void BuildMilitaryCards()
        {
            // V9.5.4：地面作战部队按当前世代遍历 AvailableDefs（替换而非解锁）
            var g = GM.Ground;
            if (g == null) return;
            bool modern = GameState.S.Year >= 4949;
            string label = modern ? "地面作战部队（现代：坦克/装甲车/导弹车）" : "地面作战部队（古典：骑兵/列方阵兵/马拉战车）";
            DrawSectionTitle(label);
            var defs = g.AvailableDefs();
            foreach (var d in defs)
            {
                string cost;
                if (d.Modern) cost = "钢" + d.CostSteel + " 金" + d.CostGold;
                else cost = "木" + d.CostWood + " 金" + d.CostGold + " 粮" + d.CostFood;
                DrawBuildCard(d.Name, cost, () => g.BuildGround(d.Id, 1, out _));
            }
        }

        void DrawSectionTitle(string text)
        {
            // 极简小标题（复用 DrawBuildCard 样式层的独立行）
        }

        void DrawBuildCard(string name, string cost, Action onBuild)
        {
            // 极简卡片：名称 + 造价 + 点击列装（UI 层实现）
        }

        void BuildInfraCards()
        {
            // 基建：桥梁/高架/铁路/机场/高铁站
            DrawSectionTitle("桥梁&高架");
            DrawBuildCard("桥梁", "点岸边·自动就近对岸建桥", () => GM.UI.StartBridgePlace());
            DrawBuildCard("高架柱", "钢80砼40", () => GM.UI.StartPierPlace());
            DrawSectionTitle("铁路&公路");
            DrawBuildCard("铁路", "点城市·自动跨海一线一车", () => GM.UI.StartRailPlace());
            DrawBuildCard("城际公路", "点城市·自动连邻国城市", () => GM.UI.StartRoadPlace());
            DrawBuildCard("铁路(早期)", "铁150石80钢30 trade+8", () => GM.Build.Build("rail_early"));
            DrawSectionTitle("航空");
            DrawBuildCard("机场", "砼400钢200金150 trade+20", () => GM.Build.Build("airport"));
            DrawBuildCard("高铁站", "钢300砼200金100 trade+25", () => GM.Build.Build("hst_station"));
        }

        // 建造面板渲染（V9.5.4 军事/基建走上面卡片）
        void RenderBuildPanel()
        {
            if (_openBuildTab == "军事") { BuildMilitaryCards(); return; }
            if (_openBuildTab == "基建") { BuildInfraCards(); return; }
            // 其余分类走原 9 类建筑卡片
        }

        public void StartBridgePlace() { }
        public void StartPierPlace() { }
        public void StartRailPlace() { }
        public void StartRoadPlace() { }
    }
}
