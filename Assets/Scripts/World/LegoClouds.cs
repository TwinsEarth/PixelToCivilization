// V8.0.1 乐高积木像素风：白色高光滑立方体拼成的积云，漂浮在相机高空并缓慢漂移。
using System.Collections.Generic;
using UnityEngine;

namespace PixelToCivilization.World
{
    /// <summary>纯装饰积木云：无碰撞、无音效、无存档状态；随相机包裹移动，始终分布在视野高空。</summary>
    public class LegoClouds : MonoBehaviour
    {
        static LegoClouds _i;
        public static void Spawn(Transform parent)
        {
            if (_i != null) return;
            var go = new GameObject("LegoClouds");
            if (parent != null) go.transform.SetParent(parent, false);
            _i = go.AddComponent<LegoClouds>();
        }

        const int Clusters = 10;
        const float Range = 180f;      // 相对相机的分布半径
        readonly List<Transform> _roots = new List<Transform>();
        readonly List<float> _ox = new List<float>(), _oy = new List<float>(), _oz = new List<float>(), _spd = new List<float>();
        Material _white;
        float _drift;

        void Awake()
        {
            _white = ShaderHelper.Mat(new Color(0.96f, 0.97f, 0.99f)); // 白色高光滑塑料
            for (int c = 0; c < Clusters; c++)
            {
                var rng = new System.Random(101 + c * 13);
                var cl = new GameObject("Cloud" + c).transform;
                cl.SetParent(transform, false);
                int puffs = 4 + (c % 3); // 4~6 块立方体
                for (int p = 0; p < puffs; p++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    var col = cube.GetComponent<Collider>(); if (col) Destroy(col);
                    cube.transform.SetParent(cl, false);
                    float sx = 5f + (float)rng.NextDouble() * 8f;
                    float sy = 2f + (float)rng.NextDouble() * 1.8f;
                    float sz = 3.5f + (float)rng.NextDouble() * 5f;
                    cube.transform.localPosition = new Vector3((float)(rng.NextDouble() * 13f - 6.5f),
                                                               (float)(rng.NextDouble() * 1.6f),
                                                               (float)(rng.NextDouble() * 9f - 4.5f));
                    cube.transform.localScale = new Vector3(sx, sy, sz);
                    var r = cube.GetComponent<Renderer>();
                    r.sharedMaterial = _white;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                _roots.Add(cl);
                _ox.Add((float)(rng.NextDouble() * 2 - 1) * Range);
                _oy.Add(78f + (float)rng.NextDouble() * 26f);
                _oz.Add((float)(rng.NextDouble() * 2 - 1) * Range);
                _spd.Add(0.5f + (c % 5) * 0.22f);
            }
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam == null) return;
            _drift += Time.deltaTime * 1.4f; // 整体缓慢东移
            Vector3 cp = cam.transform.position;
            for (int k = 0; k < _roots.Count; k++)
            {
                float wx = cp.x + Mathf.Repeat(_ox[k] + _drift * _spd[k] + Range, Range * 2f) - Range;
                float wz = cp.z + _oz[k];
                float wy = cp.y + _oy[k];
                _roots[k].position = new Vector3(wx, wy, wz);
            }
        }
    }
}
