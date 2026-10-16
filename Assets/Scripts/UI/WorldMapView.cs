using PxC.Gameplay.Entities;
using PxC.Gameplay.Factions;
using UnityEngine;
using UnityEngine.UI;

namespace PxC.UI
{
    /// <summary>
    /// v10.8.0 世界地图视图：将 v10 逻辑层世界坐标（约 [-80,80]²）渲染为 256×256 色块地图。
    /// 分层渲染：
    ///   - 静态层（势力领地色块 + 聚落点 + 建筑点）：脏标记或每 1 秒重建；
    ///   - 动态层（单位点 + 血条）：每帧叠加。
    /// 渲染到单一 Texture2D，一次 SetPixels 提交，避免逐 UI 元素更新。
    /// </summary>
    public sealed class WorldMapView : MonoBehaviour
    {
        public const float WorldExtent = 85f;   // 世界范围 [-85, 85]，覆盖开局 ±60 布局
        public const int TexSize = 256;

        private RawImage _image;
        private Texture2D _tex;
        private Color[] _staticPixels;
        private Color[] _framePixels;
        private bool _staticDirty = true;
        private float _staticTimer;

        private FactionRegistry _factions;
        private UnitRegistry _units;
        private BuildingRegistry _buildings;
        private SettlementRegistry _settlements;

        public void Bind(FactionRegistry factions, UnitRegistry units,
            BuildingRegistry buildings, SettlementRegistry settlements)
        {
            _factions = factions;
            _units = units;
            _buildings = buildings;
            _settlements = settlements;
        }

        public RawImage Build(Transform parent)
        {
            var go = new GameObject("WorldMap", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            _image = go.GetComponent<RawImage>();
            _tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _staticPixels = new Color[TexSize * TexSize];
            _framePixels = new Color[TexSize * TexSize];
            _image.texture = _tex;
            _image.color = Color.white;
            return _image;
        }

        private void LateUpdate()
        {
            if (_tex == null || _factions == null) return;

            // 静态层：脏标记或 1 秒定时重建（聚落/建筑/领地变化低频）
            _staticTimer += Time.deltaTime;
            if (_staticDirty || _staticTimer >= 1f)
            {
                _staticTimer = 0f;
                RebuildStatic();
                _staticDirty = false;
            }

            // 动态层：每帧叠加单位
            RenderFrame();
        }

        public void MarkDirty() => _staticDirty = true;

        // ---------------- 渲染 ----------------

        private void RebuildStatic()
        {
            // 底色：陆地网格 + 世界边缘海洋
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    bool edge = x < 2 || y < 2 || x >= TexSize - 2 || y >= TexSize - 2;
                    bool grid = (x % 16 == 0) || (y % 16 == 0);
                    _staticPixels[y * TexSize + x] =
                        edge ? UiPalette.MapSea :
                        grid ? UiPalette.MapGrid : UiPalette.MapLand;
                }
            }

            // 势力领地色块：以出生点为中心扩散（半径 ≈ 8 格），未存续势力压暗
            if (_factions != null)
            {
                for (int i = 0; i < _factions.Count; i++)
                {
                    var f = _factions.Get(i);
                    if (f == null) continue;
                    Color c = UiPalette.Faction(f.ColorIndex);
                    if (!f.Alive) c = UiPalette.Dead;
                    int cx = ToTex(f.HomeX);
                    int cy = ToTex(f.HomeZ);
                    int r = 7;
                    for (int dy = -r; dy <= r; dy++)
                    {
                        for (int dx = -r; dx <= r; dx++)
                        {
                            if (dx * dx + dy * dy > r * r) continue;
                            int px = cx + dx, py = cy + dy;
                            if (px < 0 || py < 0 || px >= TexSize || py >= TexSize) continue;
                            int idx = py * TexSize + px;
                            // 中心深色、边缘淡色（势力色淡染）
                            float t = 1f - Mathf.Sqrt(dx * dx + dy * dy) / (r + 1f);
                            _staticPixels[idx] = Color.Lerp(_staticPixels[idx], c, 0.35f + 0.45f * t);
                        }
                    }
                }
            }

            // 聚落点（方形据点）：势力色实心
            if (_settlements != null)
            {
                for (int s = 0; s < _settlements.Count; s++)
                {
                    var rec = _settlements.GetSafe(s);
                    if (!rec.Alive) continue;
                    var f = _factions != null ? _factions.Get(rec.Faction) : null;
                    Color c = f != null ? UiPalette.Faction(f.ColorIndex) : UiPalette.Faction(0);
                    if (f != null && !f.Alive) c = UiPalette.Dead;
                    FillSquare(ToTex(rec.Position.x), ToTex(rec.Position.z), 2, c, _staticPixels);
                }
            }

            // 建筑点（含塔防）：势力色小点
            if (_buildings != null)
            {
                for (int b = 0; b < _buildings.Count; b++)
                {
                    var rec = _buildings.GetSafe(b);
                    if (!rec.Alive) continue;
                    var f = _factions != null ? _factions.Get(rec.Faction) : null;
                    Color c = f != null ? UiPalette.Faction(f.ColorIndex) : UiPalette.Faction(0);
                    if (f != null && !f.Alive) c = UiPalette.Dead;
                    int px = ToTex(rec.Position.x), py = ToTex(rec.Position.z);
                    if (px < 0 || py < 0 || px >= TexSize || py >= TexSize) continue;
                    _staticPixels[py * TexSize + px] = c;
                    if (rec.IsTower) // 塔防加描边亮角
                    {
                        _staticPixels[py * TexSize + px] = Color.Lerp(c, Color.white, 0.35f);
                    }
                }
            }
        }

        private void RenderFrame()
        {
            // 拷贝静态层 → 叠加单位
            System.Array.Copy(_staticPixels, _framePixels, _staticPixels.Length);

            if (_units != null)
            {
                for (int u = 0; u < _units.Capacity; u++)
                {
                    var rec = _units.GetSlotRef(u);
                    if (!rec.Alive) continue;
                    var f = _factions != null ? _factions.Get(rec.Faction) : null;
                    Color c = f != null ? UiPalette.Faction(f.ColorIndex) : Color.white;
                    if (f != null && !f.Alive) c = UiPalette.Dead;
                    int px = ToTex(rec.Position.x), py = ToTex(rec.Position.z);
                    if (px < 0 || py < 0 || px >= TexSize || py >= TexSize) continue;
                    // 单位点（1px 亮 + 2px 暗边 = 3×3 十字）
                    _framePixels[py * TexSize + px] = Color.white;
                    if (px + 1 < TexSize) _framePixels[py * TexSize + px + 1] = c;
                    if (px - 1 >= 0) _framePixels[py * TexSize + px - 1] = c;
                    if (py + 1 < TexSize) _framePixels[(py + 1) * TexSize + px] = c;
                    if (py - 1 >= 0) _framePixels[(py - 1) * TexSize + px] = c;
                }
            }

            _tex.SetPixels(_framePixels);
            _tex.Apply(false, false);
        }

        private static void FillSquare(int cx, int cy, int half, Color c, Color[] px)
        {
            for (int dy = -half; dy <= half; dy++)
            {
                for (int dx = -half; dx <= half; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= TexSize || y >= TexSize) continue;
                    px[y * TexSize + x] = c;
                }
            }
        }

        private static int ToTex(float world)
        {
            float t = (world + WorldExtent) / (WorldExtent * 2f);
            int v = Mathf.FloorToInt(t * TexSize);
            return Mathf.Clamp(v, 0, TexSize - 1);
        }
    }
}
