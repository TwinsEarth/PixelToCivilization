// V9.1.1 廊道往返移动器：列车 / 汽车在固定廊道 A→B 之间来回行驶。
//  纯视图行为：位置严格限制在廊道中线上（公路/跨海铁路），因此车辆/列车绝不会离开道路或驶入水中。
//  A、B 已携带正确高度（公路=地表+偏移，跨海铁路=桥面高度），这里只做线性插值与朝向，不重新采样地形。
using UnityEngine;

namespace PixelToCivilization.Systems
{
    public class CorridorMover : MonoBehaviour
    {
        public Vector3 A, B;
        public float L = 10f;
        public float Speed = 6f;
        public bool AxisX;
        float _t;
        int _dir = 1;

        /// <summary>由城际网络每帧驱动；terrain 仅用于接口统一，位置以 A/B 为准。</summary>
        public void Tick(float dt, PixelToCivilization.World.WorldGenerator terrain)
        {
            if (L <= 0.01f) L = Vector3.Distance(A, B);
            float span = Mathf.Max(1f, L);
            _t += _dir * Speed * dt / span;
            if (_t >= 1f) { _t = 1f; _dir = -1; }
            else if (_t <= 0f) { _t = 0f; _dir = 1; }

            Vector3 pos = Vector3.Lerp(A, B, _t);
            transform.position = pos;

            Vector3 travel = _dir > 0 ? (B - A) : (A - B);
            if (travel.sqrMagnitude > 0.01f)
            {
                var target = Quaternion.LookRotation(travel.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 0.2f);
            }
        }
    }
}
