// UnityEngine stub —— 对象层级（仅供 Roslyn 无 Unity 编译验证）
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    // ============================================================
    // Object
    // ============================================================
    public class Object
    {
        public string name = "";
        public HideFlags hideFlags = HideFlags.None;
        private int _iid;
        public Object() { _iid = new System.Random().Next(); }
        public int GetInstanceID() => _iid;
        public override int GetHashCode() => _iid;
        public override string ToString() => name;
        public override bool Equals(object o) => o is Object x && x._iid == _iid;
        public static bool operator ==(Object a, Object b) { var an = ReferenceEquals(a, null); var bn = ReferenceEquals(b, null); if (an && bn) return true; if (an || bn) return false; return a._iid == b._iid; }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public static implicit operator bool(Object o) => !ReferenceEquals(o, null);

        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static void DestroyImmediate(Object o) { }
        public static void DestroyImmediate(Object o, bool allowAssets) { }
        public static void DontDestroyOnLoad(Object target) { }
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent, bool worldPositionStays) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 pos, Quaternion rot) where T : Object => original;
        public static T Instantiate<T>(T original, Vector3 pos, Quaternion rot, Transform parent) where T : Object => original;
        public static Object Instantiate(Object original) => original;
        public static Object Instantiate(Object original, Transform parent) => original;
        public static Object Instantiate(Object original, Transform parent, bool worldPositionStays) => original;
    }

    // ============================================================
    // Component
    // ============================================================
    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }
        public Transform transform => gameObject != null ? gameObject.transform : null;
        public string tag { get => gameObject != null ? gameObject.tag : "Untagged"; set { } }
        public T GetComponent<T>() where T : Component => null;
        public Component GetComponent(Type t) => null;
        public bool TryGetComponent<T>(out T component) where T : Component { component = null; return false; }
        public T GetComponentInChildren<T>(bool includeInactive = false) where T : Component => null;
        public T GetComponentInChildren<T>() where T : Component => null;
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component => new T[0];
        public T[] GetComponentsInChildren<T>() where T : Component => new T[0];
        public T GetComponentInParent<T>() where T : Component => null;
        public T[] GetComponentsInParent<T>() where T : Component => new T[0];
        public T[] GetComponents<T>() where T : Component => new T[0];
        public void GetComponents<T>(List<T> results) where T : Component { results.Clear(); }
        public void SendMessage(string name) { }
        public void SendMessage(string name, object value) { }
        public void SendMessageUpwards(string name) { }
        public void BroadcastMessage(string name) { }
        public bool CompareTag(string t) => tag == t;
    }

    // ============================================================
    // Behaviour / MonoBehaviour
    // ============================================================
    public class Behaviour : Component
    {
        public bool enabled { get; set; } = true;
        public bool isActiveAndEnabled => enabled && gameObject != null && gameObject.activeInHierarchy;
    }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) => new Coroutine();
        public Coroutine StartCoroutine(string method, object value = null) => new Coroutine();
        public Coroutine StartCoroutine(string method) => new Coroutine();
        public void StopCoroutine(Coroutine routine) { }
        public void StopCoroutine(IEnumerator routine) { }
        public void StopCoroutine(string method) { }
        public void StopAllCoroutines() { }
        public void Invoke(string method, float time) { }
        public void InvokeRepeating(string method, float time, float rate) { }
        public void CancelInvoke() { }
        public void CancelInvoke(string method) { }
        public bool IsInvoking() => false;
        public bool IsInvoking(string method) => false;
        public bool useGUILayout { get; set; }
    }

    // ============================================================
    // ScriptableObject
    // ============================================================
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject => (T)CreateInstance(typeof(T));
        public static ScriptableObject CreateInstance(Type t) => (ScriptableObject)Activator.CreateInstance(t, true);
        public void SetDirty() { }
    }

    // ============================================================
    // GameObject
    // ============================================================
    public sealed class GameObject : Object
    {
        public GameObject() { _transform = new Transform(this); }
        public GameObject(string name) : this() { this.name = name; }
        public GameObject(string name, params Type[] components) : this() { this.name = name; }

        private readonly Transform _transform;
        public Transform transform => _transform;
        private readonly List<Component> _components = new List<Component>();
        private bool _active = true;
        public int layer { get; set; }
        public string tag { get; set; } = "Untagged";
        public bool activeSelf => _active;
        public bool activeInHierarchy => _active;
        public SceneManagement.Scene scene { get; internal set; }
        public bool isStatic { get; set; }

        public void SetActive(bool v) { _active = v; }
        public T AddComponent<T>() where T : Component { var c = (T)Activator.CreateInstance(typeof(T)); c.gameObject = this; _components.Add(c); return c; }
        public Component AddComponent(Type t) { var c = (Component)Activator.CreateInstance(t); c.gameObject = this; _components.Add(c); return c; }
        public T GetComponent<T>() where T : Component { foreach (var c in _components) if (c is T t) return t; return null; }
        public Component GetComponent(Type t) { foreach (var c in _components) if (t.IsInstanceOfType(c)) return c; return null; }
        public bool TryGetComponent<T>(out T component) where T : Component { component = GetComponent<T>(); return component != null; }
        public T GetComponentInChildren<T>(bool includeInactive = false) where T : Component => GetComponent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component { var c = GetComponent<T>(); return c != null ? new[] { c } : new T[0]; }
        public T GetComponentInParent<T>() where T : Component => GetComponent<T>();
        public T[] GetComponents<T>() where T : Component { var list = new List<T>(); foreach (var c in _components) if (c is T t) list.Add(t); return list.ToArray(); }
        public void SendMessage(string name, object value, SendMessageOptions o) { }
        public void SendMessage(string name) { }
        public void BroadcastMessage(string name) { }
        public bool CompareTag(string t) => tag == t;

        public static GameObject CreatePrimitive(PrimitiveType type) => new GameObject(type.ToString());
        public static GameObject Find(string name) => null;
        public static GameObject FindGameObjectWithTag(string tag) => null;
        public static GameObject[] FindGameObjectsWithTag(string tag) => new GameObject[0];
        public static GameObject FindObjectOfType(Type t) => null;
        public static T FindObjectOfType<T>() where T : Object => null;
        public static T FindObjectOfType<T>(bool includeInactive) where T : Object => null;
        public static T[] FindObjectsOfType<T>() where T : Object => new T[0];
        public static T[] FindObjectsOfType<T>(bool includeInactive) where T : Object => new T[0];
    }

    // ============================================================
    // Transform / RectTransform
    // ============================================================
    public class Transform : Component
    {
        internal Transform(GameObject go) { gameObject = go; }
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; } = Quaternion.identity;
        public Quaternion localRotation { get; set; } = Quaternion.identity;
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 localScale { get; set; } = Vector3.one;
        public Vector3 right { get => rotation * Vector3.right; }
        public Vector3 up { get => rotation * Vector3.up; }
        public Vector3 forward { get => rotation * Vector3.forward; }
        public Transform parent { get; private set; }
        public int childCount { get; private set; }
        public int hierarchyCapacity { get; set; }
        public int hierarchyCount { get; }
        public Transform root => parent == null ? this : parent.root;
        public bool hasChanged { get; set; }
        public Matrix4x4 worldToLocalMatrix => Matrix4x4.identity;
        public Matrix4x4 localToWorldMatrix => Matrix4x4.identity;
        private readonly List<Transform> _children = new List<Transform>();

        public void SetParent(Transform p) { parent = p; if (p != null) { p.childCount++; p._children.Add(this); } }
        public void SetParent(Transform p, bool worldPositionStays) { SetParent(p); }
        public void SetPositionAndRotation(Vector3 pos, Quaternion rot) { position = pos; rotation = rot; }
        public void SetLocalPositionAndRotation(Vector3 pos, Quaternion rot) { localPosition = pos; localRotation = rot; }
        public void Translate(Vector3 t) { position += t; }
        public void Translate(Vector3 t, Space s) { if (s == Space.Self) position += rotation * t; else position += t; }
        public void Translate(float x, float y, float z) { position += new Vector3(x, y, z); }
        public void Translate(float x, float y, float z, Space s) { }
        public void Rotate(Vector3 e, Space s = Space.Self) { rotation *= Quaternion.Euler(e); }
        public void Rotate(Vector3 e) { rotation *= Quaternion.Euler(e); }
        public void Rotate(float x, float y, float z) { rotation *= Quaternion.Euler(x, y, z); }
        public void Rotate(float x, float y, float z, Space s) { }
        public void RotateAround(Vector3 point, Vector3 axis, float angle) { }
        public void LookAt(Transform target) { }
        public void LookAt(Vector3 worldPosition) { }
        public void LookAt(Vector3 worldPosition, Vector3 up) { }
        public Transform GetChild(int i) => i >= 0 && i < _children.Count ? _children[i] : null;
        public Transform Find(string n) { foreach (var c in _children) if (c.name == n) return c; return null; }
        public void DetachChildren() { _children.Clear(); childCount = 0; }
        public Vector3 TransformPoint(Vector3 p) => p;
        public Vector3 TransformDirection(Vector3 d) => d;
        public Vector3 InverseTransformPoint(Vector3 p) => p;
        public Vector3 InverseTransformDirection(Vector3 d) => d;
        public void GetPositionAndRotation(out Vector3 pos, out Quaternion rot) { pos = position; rot = rotation; }
        public void GetLocalPositionAndRotation(out Vector3 pos, out Quaternion rot) { pos = localPosition; rot = localRotation; }
        public static bool operator ==(Transform a, Transform b) => ReferenceEquals(a, b) || (!ReferenceEquals(a, null) && !ReferenceEquals(b, null) && a.GetInstanceID() == b.GetInstanceID());
        public static bool operator !=(Transform a, Transform b) => !(a == b);
        public override bool Equals(object o) => o is Transform t && GetInstanceID() == t.GetInstanceID();
        public override int GetHashCode() => GetInstanceID();
    }

    public sealed class RectTransform : Transform
    {
        internal RectTransform(GameObject go) : base(go) { }
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 pivot { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector3 anchoredPosition3D { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public float rectWidth { get; set; }
        public float rectHeight { get; set; }
        public Rect rect => new Rect(0, 0, sizeDelta.x, sizeDelta.y);
        public void SetSizeWithCurrentAnchors(int axis, float size) { }
        public void SetInsetAndSizeFromParentEdge(int edge, int inset, float size) { }
        public void ForceUpdateRectTransforms() { }
        public void GetLocalCorners(Vector3[] corners) { }
        public void GetWorldCorners(Vector3[] corners) { }
        public enum Axis { Horizontal = 0, Vertical = 1 }
        public enum Edge { Left = 0, Right = 1, Top = 2, Bottom = 3 }
    }
}
