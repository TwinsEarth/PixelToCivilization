using System.Collections.Generic;
using UnityEngine;
using PixelToCivilization.Core;

namespace PixelToCivilization.UI
{
    /// <summary>玩家输入控制器：左键选择（建筑/船只/车辆/树木/地面单位）、拖拽平移、滚轮缩放、右键移动军队/建造施工、空格回中。</summary>
    public class GameInputController : MonoBehaviour
    {
        [System.NonSerialized] public UIManager UI;
        [System.NonSerialized] public GameManager GM;
        private World.WorldGenerator _terrain;
        private Camera _cam;
        private bool _dragging;
        private Vector3 _dragStart, _camStart;
        private const float MinCamY = 4f, MaxCamY = 220f;

        void Start()
        {
            _cam = Camera.main ?? Camera.current;
            if (_cam == null) return;
            GM = Object.FindObjectOfType<GameManager>();
            UI = Object.FindObjectOfType<UIManager>();
            _terrain = Object.FindObjectOfType<World.WorldGenerator>();
        }

        void Update()
        {
            if (GM == null || _cam == null) return;
            if (UI != null && (UI.IsAnyModalOpen() || UI.DraggingPanel)) return;   // 面板打开时不吃地图点击
            // 左键
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
                if (UI != null && UI.TryPickUI(Input.mousePosition)) return;       // 命中 UI 元素不拾取
                if (Physics.Raycast(ray, out var hit, 600f))
                {
                    var bld = hit.collider.GetComponentInParent<Buildings.BuildingView>();
                    if (bld != null && bld.Entity != null) { UI?.SelectBuilding(bld.Entity); return; }
                    var shp = hit.collider.GetComponentInParent<Systems.ShipView>();
                    if (shp != null && shp.Ship != null) { UI?.SelectShip(shp.Ship); return; }
                    var tr = hit.collider.GetComponentInParent<Systems.TransportView>();
                    if (tr != null && tr.Data != null) { UI?.SelectVehicle(tr.Data); return; }
                    var gr = hit.collider.GetComponentInParent<Systems.GroundWarfareSystem.GroundUnit>();
                    if (gr != null) { UI?.SelectGround(gr); return; }
                    // 树木：拾取最近树木显示属性
                    var tree = TreeViewFinder(hit.point);
                    if (tree != null) { UI?.SelectTree(tree); return; }
                }
                UI?.ClearSelection();
                _dragging = true; _dragStart = Input.mousePosition; _camStart = _cam.transform.position;
            }
            if (Input.GetMouseButton(0) && _dragging)
            {
                if (Vector3.Distance(_dragStart, Input.mousePosition) > 12f)
                {
                    Vector3 d = _cam.ScreenToWorldPoint(new Vector3(_dragStart.x, _dragStart.y, 10f))
                             - _cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
                    _cam.transform.position = _camStart + new Vector3(d.x, 0, d.z);
                }
            }
            if (Input.GetMouseButtonUp(0)) _dragging = false;
            // 右键：移动军队到该点 / 施工建造
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 600f))
                {
                    if (UI != null && UI.CurrentTool != null && UI.CurrentTool.StartsWith("build_"))
                    {
                        UI.TryPlaceBuilding(UI.CurrentTool, hit.point);
                        return;
                    }
                    if (UI != null && UI.CurrentTool == "plant_tree")
                    {
                        UI.TryPlantTree(hit.point);
                        return;
                    }
                    if (UI != null && UI.CurrentTool == "recruit")
                    {
                        UI.TryRecruitAt(hit.point);
                        return;
                    }
                    UI.MoveSelectedUnits(hit.point);
                }
            }
            // 滚轮缩放
            float wz = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(wz) > 0.001f)
            {
                Vector3 p = _cam.transform.position;
                p.y = Mathf.Clamp(p.y - wz * 24f, MinCamY, MaxCamY);
                _cam.transform.position = p;
            }
            // 空格回中
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_terrain == null) _terrain = Object.FindObjectOfType<World.WorldGenerator>();
                Vector3 c = _terrain != null ? _terrain.HomeCenter : Vector3.zero;
                _cam.transform.position = new Vector3(c.x, _cam.transform.position.y, c.z);
            }
        }

        /// <summary>从世界点找最近树木（Trees 由世界系统维护，取其最近者）</summary>
        private World.TreeData TreeViewFinder(Vector3 wp)
        {
            if (GM == null) return null;
            float best = 3.5f; World.TreeData hit = null;
            foreach (var t in GM.Trees)
            {
                if (t.View == null) continue;
                float d = Vector3.Distance(t.View.transform.position, wp);
                if (d < best) { best = d; hit = t; }
            }
            return hit;
        }
    }
}
