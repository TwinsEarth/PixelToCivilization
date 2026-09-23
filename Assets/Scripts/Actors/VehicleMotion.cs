using UnityEngine;

namespace PixelToCivilization.Actors
{
    /// <summary>载具运动件驱动：轮式滚动、螺旋桨/旋翼旋转、悬浮器起伏；由 Velocity 驱动。</summary>
    [RequireComponent(typeof(VehicleRig))]
    public class VehicleMotion : MonoBehaviour
    {
        public VehicleRig Rig;
        public float WheelRadius = 0.34f;
        float _hover;
        Vector3 _prevPos;
        bool _hasPrev;
        void LateUpdate()
        {
            if(Rig==null) return;
            float dt=Mathf.Max(Time.deltaTime,0.0001f);
            float udt=Mathf.Max(Time.unscaledDeltaTime,0.0001f);
            // V7.0.5 车轮按载具真实位移自驱滚动（外部 Velocity 与实测取较大者），避免外部漏写导致轮子不转
            Vector3 cur=transform.position;
            Vector3 measured=_hasPrev ? (cur-_prevPos)/udt : Vector3.zero;
            _prevPos=cur; _hasPrev=true;
            Vector3 ext=new Vector3(Rig.Velocity.x,0f,Rig.Velocity.z);
            Vector3 mea=new Vector3(measured.x,0f,measured.z);
            Vector3 moveH=mea.sqrMagnitude>=ext.sqrMagnitude?mea:ext;
            float speed=moveH.magnitude;
            float roll=speed*dt/Mathf.Max(0.01f,WheelRadius)*Mathf.Rad2Deg;   // V9.1.3 修复：WheelRadius 可被 Inspector 置 0 导致除零 NaN
            foreach(var w in Rig.Wheels) if(w) w.Rotate(Vector3.right,roll,Space.Self);
            if(Rig.Propeller) Rig.Propeller.Rotate(Vector3.forward,dt*1400f,Space.Self);
            if(Rig.Rotor) Rig.Rotor.Rotate(Vector3.up,dt*900f,Space.Self);
            if(Rig.Kind==VehicleKind.Hover)
            {
                _hover+=dt*2f;
                transform.localPosition += new Vector3(0,Mathf.Sin(_hover)*0.0015f,0);
            }
        }
    }
}
