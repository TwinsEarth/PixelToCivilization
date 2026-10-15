// UnityEngine stub —— 仅供 Roslyn 无 Unity 编译验证（签名对齐，非真实实现）
// 真实 Unity 语义以团结引擎许可激活后的编译/运行为准。
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    // ============================================================
    // 枚举
    // ============================================================
    public enum HideFlags { None = 0, HideInHierarchy = 1, HideInInspector = 2, DontSaveInEditor = 4, NotEditable = 8, DontSaveInBuild = 16, DontUnloadUnusedAsset = 32, DontSave = 52, HideAndDontSave = 61 }
    public enum Space { World = 0, Self = 1 }
    public enum SendMessageOptions { RequireReceiver = 0, DontRequireReceiver = 1 }
    public enum LogType { Error = 0, Assert = 1, Warning = 2, Log = 3, Exception = 4 }
    public enum RuntimePlatform { OSXEditor = 0, OSXPlayer = 1, WindowsPlayer = 2, WebGLPlayer = 40, WindowsEditor = 7, IPhonePlayer = 8, Android = 11, LinuxPlayer = 13, LinuxEditor = 16 }
    public enum ThreadPriority { Low = 0, BelowNormal = 1, Normal = 2, High = 4 }
    public enum FullScreenMode { ExclusiveFullScreen = 0, FullScreenWindow = 1, MaximizedWindow = 2, Windowed = 3 }
    public enum DeviceType { Unknown = 0, Handheld = 1, Console = 2, Desktop = 3 }
    public enum OperatingSystemFamily { Other = 0, MacOSX = 1, Windows = 2, Linux = 3 }
    public enum TextAnchor { UpperLeft = 0, UpperCenter = 1, UpperRight = 2, MiddleLeft = 3, MiddleCenter = 4, MiddleRight = 5, LowerLeft = 6, LowerCenter = 7, LowerRight = 8 }
    public enum FontStyle { Normal = 0, Bold = 1, Italic = 2, BoldAndItalic = 3 }
    public enum HorizontalWrapMode { Wrap = 0, Overflow = 1 }
    public enum VerticalWrapMode { Truncate = 0, Overflow = 2 }
    public enum FilterMode { Point = 0, Bilinear = 1, Trilinear = 2 }
    public enum TextureWrapMode { Repeat = 0, Clamp = 1, Mirror = 2 }
    public enum RenderTextureFormat { ARGB32 = 0, Depth = 1, ARGBHalf = 2, Default = 7 }
    public enum CameraClearFlags { Skybox = 1, Color = 2, SolidColor = 2, Depth = 3, Nothing = 4 }
    public enum LightType { Spot = 0, Directional = 1, Point = 2, Area = 3 }
    public enum LightShadows { None = 0, Hard = 1, Soft = 2 }
    public enum PrimitiveType { Sphere = 0, Capsule = 1, Cylinder = 2, Cube = 3, Plane = 4, Quad = 5 }
    public enum KeyCode { None = 0, Backspace = 8, Tab = 9, Return = 13, Escape = 27, Space = 32, Alpha0 = 48, Alpha1 = 49, A = 97, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, F1 = 282, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, LeftArrow = 276, UpArrow, RightArrow, DownArrow, LeftShift = 304, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Mouse0 = 323, Mouse1, Mouse2 }
    public enum TouchPhase { Began = 0, Moved = 1, Stationary = 2, Ended = 3, Canceled = 4 }
    public enum QueryTriggerInteraction { UseGlobal = 0, Ignore = 1, Collide = 2 }
    public enum ForceMode { Force = 0, Acceleration = 5, Impulse = 1, VelocityChange = 2 }
    public enum RigidbodyConstraints { None = 0, FreezePosition = 2, FreezeRotation = 4, FreezeAll = 126 }

    // ============================================================
    // 向量
    // ============================================================
    public struct Vector2 : IEquatable<Vector2>
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float this[int i] { get { return i == 0 ? x : y; } set { if (i == 0) x = value; else y = value; } }
        public Vector2 normalized { get { float m = magnitude; return m > 1e-5f ? new Vector2(x / m, y / m) : zero; } }
        public float magnitude => Mathf.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;
        public void Normalize() { float m = magnitude; if (m > 1e-5f) { x /= m; y /= m; } }
        public void Set(float nx, float ny) { x = nx; y = ny; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public static Vector2 up => new Vector2(0, 1);
        public static Vector2 down => new Vector2(0, -1);
        public static Vector2 left => new Vector2(-1, 0);
        public static Vector2 right => new Vector2(1, 0);
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static float Angle(Vector2 from, Vector2 to) { float d = Mathf.Clamp(Dot(from.normalized, to.normalized), -1f, 1f); return Mathf.Acos(d) * Mathf.Rad2Deg; }
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t); }
        public static Vector2 MoveTowards(Vector2 cur, Vector2 target, float maxStep) { Vector2 d = target - cur; float s = d.magnitude; return s <= maxStep || s < 1e-6f ? target : cur + d / s * maxStep; }
        public static Vector2 Scale(Vector2 a, Vector2 b) => new Vector2(a.x * b.x, a.y * b.y);
        public static Vector2 Perpendicular(Vector2 a) => new Vector2(-a.y, a.x);
        public bool Equals(Vector2 o) => Mathf.Approximately(x, o.x) && Mathf.Approximately(y, o.y);
        public override bool Equals(object o) => o is Vector2 v && Equals(v);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2);
        public override string ToString() => $"({x:F2}, {y:F2})";
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
        public static bool operator ==(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 9.99999944e-11f;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0f);
        public static explicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
    }

    public struct Vector3 : IEquatable<Vector3>
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0f; }
        public float this[int i] { get { return i == 0 ? x : (i == 1 ? y : z); } set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; } }
        public Vector3 normalized { get { float m = magnitude; return m > 1e-5f ? new Vector3(x / m, y / m, z / m) : zero; } }
        public float magnitude => Mathf.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public void Normalize() { float m = magnitude; if (m > 1e-5f) { x /= m; y /= m; z /= m; } }
        public void Set(float nx, float ny, float nz) { x = nx; y = ny; z = nz; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float Angle(Vector3 from, Vector3 to) { float d = Mathf.Clamp(Dot(from.normalized, to.normalized), -1f, 1f); return Mathf.Acos(d) * Mathf.Rad2Deg; }
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t); }
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) => Lerp(a, b, t);
        public static Vector3 MoveTowards(Vector3 cur, Vector3 target, float maxStep) { Vector3 d = target - cur; float s = d.magnitude; return s <= maxStep || s < 1e-6f ? target : cur + d / s * maxStep; }
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3 Project(Vector3 v, Vector3 on) { float s = Dot(on, on); return s < 1e-5f ? zero : on * Dot(v, on) / s; }
        public static Vector3 Exclude(Vector3 exclude, Vector3 v) => v - Project(v, exclude);
        public bool Equals(Vector3 o) => Mathf.Approximately(x, o.x) && Mathf.Approximately(y, o.y) && Mathf.Approximately(z, o.z);
        public override bool Equals(object o) => o is Vector3 v && Equals(v);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static bool operator ==(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 9.99999944e-11f;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
    }

    public struct Vector4 : IEquatable<Vector4>
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public float magnitude => Mathf.Sqrt(x * x + y * y + z * z + w * w);
        public float sqrMagnitude => x * x + y * y + z * z + w * w;
        public static Vector4 zero => new Vector4(0, 0, 0, 0);
        public static Vector4 one => new Vector4(1, 1, 1, 1);
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t) { t = Mathf.Clamp01(t); return new Vector4(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t); }
        public bool Equals(Vector4 o) => Mathf.Approximately(x, o.x) && Mathf.Approximately(y, o.y) && Mathf.Approximately(z, o.z) && Mathf.Approximately(w, o.w);
        public override bool Equals(object o) => o is Vector4 v && Equals(v);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2) ^ (w.GetHashCode() >> 1);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2}, {w:F2})";
        public static Vector4 operator +(Vector4 a, Vector4 b) => new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
        public static Vector4 operator -(Vector4 a, Vector4 b) => new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
        public static Vector4 operator *(Vector4 a, float d) => new Vector4(a.x * d, a.y * d, a.z * d, a.w * d);
        public static Vector4 operator /(Vector4 a, float d) => new Vector4(a.x / d, a.y / d, a.z / d, a.w / d);
        public static bool operator ==(Vector4 a, Vector4 b) => (a - b).sqrMagnitude < 9.99999944e-11f;
        public static bool operator !=(Vector4 a, Vector4 b) => !(a == b);
        public static implicit operator Vector4(Vector3 v) => new Vector4(v.x, v.y, v.z, 0f);
        public static implicit operator Vector3(Vector4 v) => new Vector3(v.x, v.y, v.z);
    }

    public struct Quaternion : IEquatable<Quaternion>
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        public Vector3 eulerAngles { get => new Vector3(x, y, z) * Mathf.Rad2Deg; set { var q = Euler(value); x = q.x; y = q.y; z = q.z; w = q.w; } }
        public float this[int i] { get { return i == 0 ? x : (i == 1 ? y : (i == 2 ? z : w)); } set { if (i == 0) x = value; else if (i == 1) y = value; else if (i == 2) z = value; else w = value; } }
        public static Quaternion Euler(Vector3 e) => Euler(e.x, e.y, e.z);
        public static Quaternion Euler(float x, float y, float z) { x *= Mathf.Deg2Rad; y *= Mathf.Deg2Rad; z *= Mathf.Deg2Rad; float cx = Mathf.Cos(x * .5f), sx = Mathf.Sin(x * .5f), cy = Mathf.Cos(y * .5f), sy = Mathf.Sin(y * .5f), cz = Mathf.Cos(z * .5f), sz = Mathf.Sin(z * .5f); return new Quaternion(sx * cy * cz - cx * sy * sz, cx * sy * cz + sx * cy * sz, cx * cy * sz - sx * sy * cz, cx * cy * cz + sx * sy * sz); }
        public static Quaternion LookRotation(Vector3 fwd, Vector3 up) { fwd.Normalize(); Vector3 right = Vector3.Cross(up, fwd).normalized; up = Vector3.Cross(fwd, right); return new Quaternion(0, 0, 0, 1); }
        public static Quaternion LookRotation(Vector3 fwd) => LookRotation(fwd, Vector3.up);
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => t < .5f ? a : b;
        public static Quaternion AngleAxis(float angle, Vector3 axis) => identity;
        public static Quaternion FromToRotation(Vector3 from, Vector3 to) => identity;
        public static float Dot(Quaternion a, Quaternion b) => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
        public void Normalize() { float m = Mathf.Sqrt(x * x + y * y + z * z + w * w); if (m > 1e-5f) { x /= m; y /= m; z /= m; w /= m; } }
        public void Set(float nx, float ny, float nz, float nw) { x = nx; y = ny; z = nz; w = nw; }
        public void SetFromToRotation(Vector3 from, Vector3 to) { var q = FromToRotation(from, to); x = q.x; y = q.y; z = q.z; w = q.w; }
        public void SetLookRotation(Vector3 view) { var q = LookRotation(view); x = q.x; y = q.y; z = q.z; w = q.w; }
        public bool Equals(Quaternion o) => Mathf.Approximately(x, o.x) && Mathf.Approximately(y, o.y) && Mathf.Approximately(z, o.z) && Mathf.Approximately(w, o.w);
        public override bool Equals(object o) => o is Quaternion v && Equals(v);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2) ^ (w.GetHashCode() >> 1);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2}, {w:F2})";
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y, a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z, a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x, a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        public static Vector3 operator *(Quaternion r, Vector3 p) { var x = r.x * 2f; var y = r.y * 2f; var z = r.z * 2f; var xx = r.x * x; var yy = r.y * y; var zz = r.z * z; var xy = r.x * y; var xz = r.z * x; var yz = r.y * z; var wx = r.w * x; var wy = r.w * y; var wz = r.w * z; Vector3 res; res.x = (1f - (yy + zz)) * p.x + (xy - wz) * p.y + (xz + wy) * p.z; res.y = (xy + wz) * p.x + (1f - (xx + zz)) * p.y + (yz - wx) * p.z; res.z = (xz - wy) * p.x + (yz + wx) * p.y + (1f - (xx + yy)) * p.z; return res; }
        public static bool operator ==(Quaternion a, Quaternion b) => Dot(a, b) > 1f - 1e-5f;
        public static bool operator !=(Quaternion a, Quaternion b) => !(a == b);
    }

    public struct Color : IEquatable<Color>
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public float grayscale => 0.299f * r + 0.587f * g + 0.114f * b;
        public float maxColorComponent => Mathf.Max(Mathf.Max(r, g), b);
        public float this[int i] { get { return i == 0 ? r : (i == 1 ? g : (i == 2 ? b : a)); } set { if (i == 0) r = value; else if (i == 1) g = value; else if (i == 2) b = value; else a = value; } }
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color red => new Color(1, 0, 0, 1);
        public static Color green => new Color(0, 1, 0, 1);
        public static Color blue => new Color(0, 0, 1, 1);
        public static Color yellow => new Color(1, 0.92f, 0.016f, 1);
        public static Color cyan => new Color(0, 1, 1, 1);
        public static Color magenta => new Color(1, 0, 1, 1);
        public static Color gray => new Color(0.5f, 0.5f, 0.5f, 1);
        public static Color grey => gray;
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
        public bool Equals(Color o) => Mathf.Approximately(r, o.r) && Mathf.Approximately(g, o.g) && Mathf.Approximately(b, o.b) && Mathf.Approximately(a, o.a);
        public override bool Equals(object o) => o is Color c && Equals(c);
        public override int GetHashCode() => r.GetHashCode() ^ (g.GetHashCode() << 2) ^ (b.GetHashCode() >> 2) ^ (a.GetHashCode() >> 1);
        public override string ToString() => $"RGBA({r:F2}, {g:F2}, {b:F2}, {a:F2})";
        public static Color operator +(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);
        public static Color operator -(Color a, Color b) => new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a);
        public static Color operator *(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        public static Color operator *(Color a, float d) => new Color(a.r * d, a.g * d, a.b * d, a.a * d);
        public static Color operator /(Color a, float d) => new Color(a.r / d, a.g / d, a.b / d, a.a / d);
        public static bool operator ==(Color a, Color b) => (a.r == b.r) && (a.g == b.g) && (a.b == b.b) && (a.a == b.a);
        public static bool operator !=(Color a, Color b) => !(a == b);
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public override string ToString() => $"RGBA32({r}, {g}, {b}, {a})";
        public static implicit operator Color32(Color c) => new Color32((byte)Mathf.Clamp(c.r * 255f, 0, 255), (byte)Mathf.Clamp(c.g * 255f, 0, 255), (byte)Mathf.Clamp(c.b * 255f, 0, 255), (byte)Mathf.Clamp(c.a * 255f, 0, 255));
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }

    public struct Rect : IEquatable<Rect>
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
        public Rect(Vector2 position, Vector2 size) { x = position.x; y = position.y; width = size.x; height = size.y; }
        public Vector2 position { get => new Vector2(x, y); set { x = value.x; y = value.y; } }
        public Vector2 size { get => new Vector2(width, height); set { width = value.x; height = value.y; } }
        public Vector2 center => new Vector2(x + width / 2f, y + height / 2f);
        public float xMin { get => x; set { width += x - value; x = value; } }
        public float yMin { get => y; set { height += y - value; y = value; } }
        public float xMax { get => x + width; set { width = value - x; } }
        public float yMax { get => y + height; set { height = value - y; } }
        public bool Contains(Vector2 p) => p.x >= xMin && p.x < xMax && p.y >= yMin && p.y < yMax;
        public bool Overlaps(Rect o) => o.xMin < xMax && o.xMax > xMin && o.yMin < yMax && o.yMax > yMin;
        public bool Equals(Rect o) => position == o.position && size == o.size;
        public override bool Equals(object o) => o is Rect r && Equals(r);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (width.GetHashCode() >> 2) ^ (height.GetHashCode() >> 1);
        public override string ToString() => $"(x:{x:F2}, y:{y:F2}, w:{width:F2}, h:{height:F2})";
        public static bool operator ==(Rect a, Rect b) => a.Equals(b);
        public static bool operator !=(Rect a, Rect b) => !a.Equals(b);
    }

    public struct Bounds : IEquatable<Bounds>
    {
        private Vector3 _center, _extents;
        public Bounds(Vector3 center, Vector3 size) { _center = center; _extents = size * 0.5f; }
        public Vector3 center { get => _center; set => _center = value; }
        public Vector3 size { get => _extents * 2f; set => _extents = value * 0.5f; }
        public Vector3 extents { get => _extents; set => _extents = value; }
        public Vector3 min => center - extents;
        public Vector3 max => center + extents;
        public bool Contains(Vector3 p) => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y && p.z >= min.z && p.z <= max.z;
        public bool Intersects(Bounds o) => min.x <= o.max.x && max.x >= o.min.x && min.y <= o.max.y && max.y >= o.min.y && min.z <= o.max.z && max.z >= o.min.z;
        public float SqrDistance(Vector3 p) { var d = Vector3.zero; return d.sqrMagnitude; }
        public void Encapsulate(Vector3 p) { }
        public bool Equals(Bounds o) => center == o.center && extents == o.extents;
        public override bool Equals(object o) => o is Bounds b && Equals(b);
        public override int GetHashCode() => center.GetHashCode() ^ (extents.GetHashCode() << 2);
        public override string ToString() => $"Center: {_center}, Extents: {_extents}";
        public static bool operator ==(Bounds a, Bounds b) => a.Equals(b);
        public static bool operator !=(Bounds a, Bounds b) => !a.Equals(b);
    }

    public struct Ray : IEquatable<Ray>
    {
        private Vector3 _origin, _direction;
        public Ray(Vector3 origin, Vector3 direction) { _origin = origin; _direction = direction.normalized; }
        public Vector3 origin { get => _origin; set => _origin = value; }
        public Vector3 direction { get => _direction; set => _direction = value.normalized; }
        public Vector3 GetPoint(float d) => origin + direction * d;
        public bool Equals(Ray o) => origin == o.origin && direction == o.direction;
        public override bool Equals(object o) => o is Ray r && Equals(r);
        public override int GetHashCode() => origin.GetHashCode() ^ (direction.GetHashCode() << 2);
        public override string ToString() => $"Origin: {_origin}, Dir: {_direction}";
        public static bool operator ==(Ray a, Ray b) => a.Equals(b);
        public static bool operator !=(Ray a, Ray b) => !a.Equals(b);
    }

    public struct Plane
    {
        private Vector3 _normal;
        private float _distance;
        public Plane(Vector3 a, Vector3 b, Vector3 c) { _normal = Vector3.Cross(b - a, c - a).normalized; _distance = -Vector3.Dot(_normal, a); }
        public Plane(Vector3 normal, Vector3 point) { _normal = normal.normalized; _distance = -Vector3.Dot(_normal, point); }
        public Plane(Vector3 normal, float d) { _normal = normal.normalized; _distance = d; }
        public Vector3 normal { get => _normal; set => _normal = value; }
        public float distance { get => _distance; set => _distance = value; }
        public bool Raycast(Ray ray, out float enter) { float v = Vector3.Dot(ray.direction, _normal); enter = 0f; return false; }
    }

    public struct Matrix4x4 : IEquatable<Matrix4x4>
    {
        public float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33;
        public float this[int r, int c] { get { var a = new float[16]; return a[r * 4 + c]; } set { } }
        public static Matrix4x4 identity => new Matrix4x4 { m00 = 1, m11 = 1, m22 = 1, m33 = 1 };
        public Matrix4x4 inverse => identity;
        public Matrix4x4 transpose => this;
        public bool isIdentity => true;
        public Vector3 MultiplyPoint(Vector3 v) => v;
        public Vector3 MultiplyVector(Vector3 v) => v;
        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s) => identity;
        public bool Equals(Matrix4x4 o) => true;
        public override bool Equals(object o) => o is Matrix4x4 m && Equals(m);
        public override int GetHashCode() => 0;
        public static bool operator ==(Matrix4x4 a, Matrix4x4 b) => true;
        public static bool operator !=(Matrix4x4 a, Matrix4x4 b) => false;
    }

    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public int this[int i] { get => i == 0 ? x : y; set { if (i == 0) x = value; else y = value; } }
        public float magnitude => Mathf.Sqrt(x * x + y * y);
        public int sqrMagnitude => x * x + y * y;
        public static Vector2Int zero => new Vector2Int(0, 0);
        public static Vector2Int one => new Vector2Int(1, 1);
        public static Vector2Int up => new Vector2Int(0, 1);
        public static Vector2Int right => new Vector2Int(1, 0);
        public static Vector2Int FloorToInt(Vector2 v) => new Vector2Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
        public static Vector2Int CeilToInt(Vector2 v) => new Vector2Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y));
        public static Vector2Int RoundToInt(Vector2 v) => new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        public bool Equals(Vector2Int o) => x == o.x && y == o.y;
        public override bool Equals(object o) => o is Vector2Int v && Equals(v);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2);
        public override string ToString() => $"({x}, {y})";
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new Vector2Int(a.x - b.x, a.y - b.y);
        public static Vector2Int operator *(Vector2Int a, Vector2Int b) => new Vector2Int(a.x * b.x, a.y * b.y);
        public static Vector2Int operator *(Vector2Int a, int d) => new Vector2Int(a.x * d, a.y * d);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        public static implicit operator Vector2(Vector2Int v) => new Vector2(v.x, v.y);
    }

    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public int x, y, z;
        public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public int this[int i] { get => i == 0 ? x : (i == 1 ? y : z); set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; } }
        public float magnitude => Mathf.Sqrt(x * x + y * y + z * z);
        public int sqrMagnitude => x * x + y * y + z * z;
        public static Vector3Int zero => new Vector3Int(0, 0, 0);
        public static Vector3Int one => new Vector3Int(1, 1, 1);
        public static Vector3Int up => new Vector3Int(0, 1, 0);
        public static Vector3Int right => new Vector3Int(1, 0, 0);
        public static Vector3Int forward => new Vector3Int(0, 0, 1);
        public static Vector3Int FloorToInt(Vector3 v) => new Vector3Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z));
        public static Vector3Int CeilToInt(Vector3 v) => new Vector3Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y), Mathf.CeilToInt(v.z));
        public static Vector3Int RoundToInt(Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        public bool Equals(Vector3Int o) => x == o.x && y == o.y && z == o.z;
        public override bool Equals(object o) => o is Vector3Int v && Equals(v);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        public override string ToString() => $"({x}, {y}, {z})";
        public static Vector3Int operator +(Vector3Int a, Vector3Int b) => new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3Int operator -(Vector3Int a, Vector3Int b) => new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3Int operator *(Vector3Int a, Vector3Int b) => new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3Int operator *(Vector3Int a, int d) => new Vector3Int(a.x * d, a.y * d, a.z * d);
        public static bool operator ==(Vector3Int a, Vector3Int b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(Vector3Int a, Vector3Int b) => !(a == b);
        public static implicit operator Vector3(Vector3Int v) => new Vector3(v.x, v.y, v.z);
    }

    public struct BoundsInt
    {
        public Vector3Int position, size;
        public BoundsInt(Vector3Int position, Vector3Int size) { this.position = position; this.size = size; }
        public Vector3Int min => position;
        public Vector3Int max => position + size;
        public Bounds bounds => new Bounds(position, size);
    }

    public struct RectInt
    {
        public Vector2Int position, size;
        public RectInt(int x, int y, int width, int height) { position = new Vector2Int(x, y); size = new Vector2Int(width, height); }
        public Vector2Int min => position;
        public Vector2Int max => position + size;
        public Rect rect => new Rect(position.x, position.y, size.x, size.y);
    }

    // ============================================================
    // Mathf
    // ============================================================
    public struct Mathf
    {
        public const float PI = 3.14159274f;
        public const float Deg2Rad = 0.0174532924f;
        public const float Rad2Deg = 57.29578f;
        public const float Epsilon = 1.401298e-45f;
        public const float MaxValue = 3.40282347e+38f;
        public const float MinValue = -3.40282347e+38f;
        public const float NegativeInfinity = float.NegativeInfinity;
        public const float PositiveInfinity = float.PositiveInfinity;
        public static float Abs(float f) => Math.Abs(f);
        public static int Abs(int v) => Math.Abs(v);
        public static float Acos(float f) => (float)Math.Acos(f);
        public static float Asin(float f) => (float)Math.Asin(f);
        public static float Atan(float f) => (float)Math.Atan(f);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Ceil(float f) => (float)Math.Ceiling(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
        public static long Clamp(long v, long min, long max) => v < min ? min : (v > max ? max : v);
        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Tan(float f) => (float)Math.Tan(f);
        public static float Exp(float p) => (float)Math.Exp(p);
        public static float Floor(float f) => (float)Math.Floor(f);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static float Round(float f) => (float)Math.Round(f, MidpointRounding.AwayFromZero);
        public static int RoundToInt(float f) => (int)Math.Round(f, MidpointRounding.AwayFromZero);
        public static float Log(float f, float p) => (float)Math.Log(f, p);
        public static float Log(float f) => (float)Math.Log(f);
        public static float Log10(float f) => (float)Math.Log10(f);
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Max(params float[] v) { float m = v[0]; for (int i = 1; i < v.Length; i++) if (v[i] > m) m = v[i]; return m; }
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Min(params float[] v) { float m = v[0]; for (int i = 1; i < v.Length; i++) if (v[i] < m) m = v[i]; return m; }
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Sign(float f) => f >= 0f ? 1f : -1f;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float InverseLerp(float a, float b, float v) => a != b ? Clamp01((v - a) / (b - a)) : 0f;
        public static float SmoothStep(float from, float to, float t) { t = Clamp01((t - from) / (to - from)); return t * t * (3f - 2f * t); }
        public static float Repeat(float t, float length) => Clamp(t - Floor(t / length) * length, 0f, length);
        public static float DeltaAngle(float cur, float target) { float d = Repeat(target - cur, 360f); if (d > 180f) d -= 360f; return d; }
        public static float MoveTowards(float cur, float target, float maxDelta) => Math.Abs(target - cur) <= maxDelta ? target : cur + Sign(target - cur) * maxDelta;
        public static float MoveTowardsAngle(float cur, float target, float maxDelta) => MoveTowards(cur, target, maxDelta);
        public static bool Approximately(float a, float b) => Abs(b - a) < Max(1e-06f * Max(Abs(a), Abs(b)), 1.17549435e-38f) * 8f;
        public static float PerlinNoise(float x, float y) => 0.5f;
        public static int ClosestPowerOfTwo(int v) => v <= 0 ? 1 : (int)Math.Pow(2, Math.Ceiling(Math.Log(v, 2)));
        public static bool IsPowerOfTwo(int v) => v > 0 && (v & (v - 1)) == 0;
    }
}
