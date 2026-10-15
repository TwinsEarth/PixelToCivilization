// UnityEngine stub —— 渲染、组件、UI（仅供 Roslyn 无 Unity 编译验证）
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    // ============================================================
    // 渲染
    // ============================================================
    public class Renderer : Component
    {
        public Bounds bounds;
        public Material material;
        public Material[] sharedMaterials;
        public Material sharedMaterial;
        public bool enabled = true;
        public bool visible;
        public int sortingOrder;
        public int sortingLayerID;
        public int lightmapIndex;
        public Transform rootBone;
        public void SetPropertyBlock(MaterialPropertyBlock b) { }
        public void GetPropertyBlock(MaterialPropertyBlock b) { }
    }

    public class MeshRenderer : Renderer { }
    public class SkinnedMeshRenderer : Renderer
    {
        public Mesh sharedMesh;
        public Transform[] bones;
        public int quality;
        public bool updateWhenOffscreen;
        public bool skinnedMotionVectors;
    }

    public class MeshFilter : Component
    {
        public Mesh sharedMesh;
        public Mesh mesh;
    }

    public sealed class Mesh : Object
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector3[] normals;
        public Vector2[] uv;
        public Vector2[] uv2;
        public Color[] colors;
        public int subMeshCount;
        public int vertexCount;
        public Bounds bounds;
        public bool isReadable = true;
        public void Clear() { }
        public void Clear(bool keepVertexLayout) { }
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
        public void MarkDynamic() { }
        public void UploadMeshData() { }
        public void CombineMeshes(CombineInstance[] combine) { }
    }

    public struct CombineInstance
    {
        public int subMeshIndex;
        public Mesh mesh;
        public Matrix4x4 transform;
    }

    public sealed class Material : Object
    {
        public Material(Shader shader) { }
        public Material(Material source) { }
        public Color color;
        public int renderQueue;
        public Shader shader;
        public string[] GetTexturePropertyNames() => new string[0];
        public Texture GetTexture(string name) => null;
        public void SetTexture(string name, Texture t) { }
        public void SetColor(string name, Color c) { }
        public Color GetColor(string name) => Color.white;
        public void SetFloat(string name, float v) { }
        public float GetFloat(string name) => 0f;
        public void SetInt(string name, int v) { }
        public void SetVector(string name, Vector4 v) { }
        public void EnableKeyword(string k) { }
        public void DisableKeyword(string k) { }
        public bool IsKeywordEnabled(string k) => false;
        public void CopyPropertiesFromMaterial(Material m) { }
    }

    public sealed class MaterialPropertyBlock
    {
        public void SetColor(string name, Color c) { }
        public void SetFloat(string name, float v) { }
        public void SetVector(string name, Vector4 v) { }
        public void SetTexture(string name, Texture t) { }
    }

    public sealed class Shader : Object
    {
        public static Shader Find(string name) => null;
        public int renderQueue;
        public static int PropertyToID(string name) => 0;
        public static void SetGlobalColor(string name, Color c) { }
        public static void SetGlobalFloat(string name, float v) { }
        public static void SetGlobalTexture(string name, Texture t) { }
    }

    public class Texture : Object
    {
        public int width, height;
        public TextureWrapMode wrapMode;
        public FilterMode filterMode;
        public Vector2 texelSize;
        public int anisoLevel;
        public float mipMapBias;
    }

    public sealed class Texture2D : Texture
    {
        public Texture2D(int w, int h) { width = w; height = h; }
        public Texture2D(int w, int h, TextureFormat format, bool mip) { width = w; height = h; }
        public Texture2D(int w, int h, TextureFormat format, bool mip, bool linear) { }
        public void SetPixels(Color[] c) { }
        public void SetPixel(int x, int y, Color c) { }
        public Color GetPixel(int x, int y) => Color.white;
        public void Apply() { }
        public void Apply(bool updateMips) { }
        public Color[] GetPixels() => new Color[0];
        public byte[] EncodeToPNG() => new byte[0];
        public byte[] EncodeToJPG() => new byte[0];
        public void LoadImage(byte[] data) { }
        public static Texture2D whiteTexture;
        public static Texture2D blackTexture;
        public static Texture2D redTexture;
        public void Resize(int w, int h) { }
        public void GetPixels32() { }
    }

    public enum TextureFormat { Alpha8 = 1, RGBA32 = 4, ARGB32 = 5, RGB24 = 3, DXT1 = 10, DXT5 = 12, RGBAHalf = 25, RGBAFloat = 26 }

    public sealed class RenderTexture : Texture
    {
        public RenderTexture(int w, int h, int depth) { width = w; height = h; }
        public void Create() { }
        public void Release() { }
        public bool IsCreated() => false;
        public static RenderTexture active;
        public int depth;
    }

    public sealed class Sprite : Object
    {
        public Texture2D texture;
        public Rect rect;
        public Vector2 pivot;
        public float pixelsPerUnit;
        public Vector4 border;
        public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu) => new Sprite();
        public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, int extrude, int meshType, Vector4 border) => new Sprite();
    }

    public sealed class Camera : Component
    {
        public static Camera main;
        public CameraClearFlags clearFlags = CameraClearFlags.Skybox;
        public Color backgroundColor = Color.blue;
        public int cullingMask = -1;
        public int depth;
        public bool orthographic;
        public float orthographicSize = 5f;
        public float fieldOfView = 60f;
        public float nearClipPlane = 0.3f;
        public float farClipPlane = 1000f;
        public float aspect;
        public RenderTexture targetTexture;
        public Vector3 WorldToScreenPoint(Vector3 pos) => Vector3.zero;
        public Vector3 ScreenToWorldPoint(Vector3 pos) => Vector3.zero;
        public Vector3 ViewportToWorldPoint(Vector3 pos) => Vector3.zero;
        public Vector3 WorldToViewportPoint(Vector3 pos) => Vector3.zero;
        public Ray ScreenPointToRay(Vector3 pos) => new Ray();
        public Ray ViewportPointToRay(Vector3 pos) => new Ray();
        public static Camera[] allCameras;
        public int pixelWidth;
        public int pixelHeight;
    }

    public sealed class Light : Component
    {
        public LightType type;
        public Color color = Color.white;
        public float intensity = 1f;
        public float range = 10f;
        public float spotAngle = 30f;
        public LightShadows shadows;
        public float shadowStrength = 1f;
        public float shadowBias = 0.05f;
        public float shadowNormalBias = 0.4f;
        public int cullingMask = -1;
    }

    // ============================================================
    // 物理
    // ============================================================
    public class Collider : Component
    {
        public bool enabled = true;
        public Bounds bounds;
        public Rigidbody attachedRigidbody;
        public bool isTrigger;
        public PhysicMaterial material;
        public Vector3 ClosestPoint(Vector3 pos) => pos;
    }

    public class BoxCollider : Collider
    {
        public Vector3 center;
        public Vector3 size = Vector3.one;
    }

    public class SphereCollider : Collider
    {
        public Vector3 center;
        public float radius = 0.5f;
    }

    public class CapsuleCollider : Collider
    {
        public Vector3 center;
        public float radius = 0.5f;
        public float height = 2f;
        public int direction = 1;
    }

    public class MeshCollider : Collider
    {
        public Mesh sharedMesh;
        public bool convex;
    }

    public sealed class PhysicMaterial : Object
    {
        public float dynamicFriction = 0.6f;
        public float staticFriction = 0.6f;
        public float bounciness;
        public PhysicMaterial(string name) { }
    }

    public sealed class Rigidbody : Component
    {
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public float mass = 1f;
        public float drag;
        public float angularDrag = 0.05f;
        public bool useGravity = true;
        public bool isKinematic;
        public bool freezeRotation;
        public RigidbodyConstraints constraints;
        public Vector3 position;
        public float detectCollisions;
        public CollisionDetectionMode collisionDetectionMode;
        public void AddForce(Vector3 f, ForceMode m = ForceMode.Force) { }
        public void AddForce(Vector3 f) { }
        public void AddRelativeForce(Vector3 f) { }
        public void AddTorque(Vector3 t) { }
        public void MovePosition(Vector3 p) { }
        public void MoveRotation(Quaternion r) { }
        public void Sleep() { }
        public void WakeUp() { }
    }

    public enum CollisionDetectionMode { Discrete = 0, Continuous = 1, ContinuousDynamic = 2, ContinuousSpeculative = 3 }

    public struct Collision
    {
        public Vector3 relativeVelocity;
        public Rigidbody rigidbody;
        public Collider collider;
        public ContactPoint[] contacts;
        public GameObject gameObject;
        public Transform transform;
    }

    public struct ContactPoint
    {
        public Vector3 point, normal, impulse;
        public Collider thisCollider, otherCollider;
    }

    public sealed class CharacterController : Collider
    {
        public float slopeLimit = 45f;
        public float stepOffset = 0.3f;
        public float skinWidth = 0.08f;
        public float radius = 0.5f;
        public float height = 2f;
        public Vector3 center;
        public float minMoveDistance;
        public bool isGrounded;
        public Vector3 velocity;
        public bool Move(Vector3 motion) { return false; }
        public bool SimpleMove(Vector3 speed) { return false; }
    }

    public sealed class WheelCollider : Collider
    {
        public float radius = 0.5f;
        public float mass = 20f;
        public float motorTorque;
        public float brakeTorque;
        public float steerAngle;
        public bool GetWorldPose(out Vector3 pos, out Quaternion rot) { pos = Vector3.zero; rot = Quaternion.identity; return false; }
    }

    public static class Physics
    {
        public static Vector3 gravity = new Vector3(0, -9.81f, 0);
        public static bool autoSimulation = true;
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction q) => false;
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance) { hit = default(RaycastHit); return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance, int layerMask) { hit = default(RaycastHit); return false; }
        public static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance) { hit = default(RaycastHit); return false; }
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance) => new RaycastHit[0];
        public static int OverlapSphereNonAlloc(Vector3 center, float radius, Collider[] results) => 0;
        public static Collider[] OverlapSphere(Vector3 center, float radius) => new Collider[0];
        public static void IgnoreCollision(Collider a, Collider b, bool ignore) { }
        public static void Simulate(float step) { }
    }

    public struct RaycastHit
    {
        public Vector3 point, normal;
        public Vector3 barycentricCoordinate;
        public float distance;
        public int triangleIndex;
        public Vector2 textureCoord;
        public Transform transform;
        public Collider collider;
        public Rigidbody rigidbody;
        public Vector3 GetContactPoint(int i) => point;
    }

    // ============================================================
    // 动画 / 特效 / 音频
    // ============================================================
    public sealed class Animator : Component
    {
        public RuntimeAnimatorController runtimeAnimatorController;
        public float speed = 1f;
        public int layerCount;
        public bool isHuman;
        public bool enabled = true;
        public void SetFloat(string name, float v) { }
        public float GetFloat(string name) => 0f;
        public void SetInteger(string name, int v) { }
        public int GetInteger(string name) => 0;
        public void SetBool(string name, bool v) { }
        public bool GetBool(string name) => false;
        public void SetTrigger(string name) { }
        public void ResetTrigger(string name) { }
        public void Play(string name) { }
        public void Play(string name, int layer) { }
        public StateInfo GetCurrentAnimatorStateInfo(int layer) => default(StateInfo);
        public bool IsInTransition(int layer) => false;
        public struct StateInfo { public int fullPathHash; public float normalizedTime; public bool IsName(string n) => false; }
    }

    public class RuntimeAnimatorController : Object { }
    public sealed class AnimationClip : Object
    {
        public float length;
        public float frameRate;
        public WrapMode wrapMode;
    }

    public sealed class ParticleSystem : Component
    {
        public ParticleSystemMain main;
        public bool isPlaying;
        public bool isEmitting;
        public struct ParticleSystemMain { public float startSize; public float duration; public bool loop; public float startSpeed; }
        public void Play() { }
        public void Play(bool withChildren) { }
        public void Stop() { }
        public void Stop(bool withChildren, ParticleSystemStopBehavior b) { }
        public void Pause() { }
        public void Clear() { }
        public void Emit(int count) { }
        public bool IsAlive(bool withChildren) => false;
        public int GetParticles(Particle[] p) => 0;
        public struct Particle { public Vector3 position; public Vector3 velocity; public float startSize; }
        public enum ParticleSystemStopBehavior { StopEmittingAndClear = 0, StopEmitting = 1 }
    }

    public sealed class LineRenderer : Renderer
    {
        public int positionCount;
        public bool useWorldSpace = true;
        public float startWidth;
        public float endWidth;
        public void SetPosition(int i, Vector3 pos) { }
        public Vector3 GetPosition(int i) => Vector3.zero;
        public void SetPositions(Vector3[] positions) { }
        public void SetColors(Color a, Color b) { }
    }

    public sealed class TrailRenderer : Renderer
    {
        public float time;
        public float startWidth;
        public void Clear() { }
        public void AddPosition(Vector3 pos) { }
    }

    public sealed class LODGroup : Component
    {
        public LOD[] GetLODs() => new LOD[0];
        public void SetLODs(LOD[] lods) { }
        public void RecalculateBounds() { }
        public float size;
        public int lodCount;
        public void ForceLOD(int i) { }
    }

    public struct LOD
    {
        public float screenRelativeTransitionHeight;
        public float fadeTransitionWidth;
        public Renderer[] renderers;
        public LOD(float s, params Renderer[] r) { screenRelativeTransitionHeight = s; renderers = r; fadeTransitionWidth = 0f; }
    }

    public sealed class AudioSource : Component
    {
        public AudioClip clip;
        public bool loop;
        public bool playOnAwake;
        public float volume = 1f;
        public float pitch = 1f;
        public bool isPlaying;
        public void Play() { }
        public void PlayOneShot(AudioClip c) { }
        public void Stop() { }
        public void Pause() { }
        public static void PlayClipAtPoint(AudioClip c, Vector3 p) { }
    }

    public sealed class AudioClip : Object
    {
        public float length;
        public int samples;
        public int channels;
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) => new AudioClip();
    }

    public sealed class AudioListener : Component
    {
        public static float volume;
        public static bool pause;
    }

    public sealed class Terrain : Component
    {
        public TerrainData terrainData;
        public static Terrain activeTerrain;
    }

    public sealed class TerrainData : Object
    {
        public int heightmapWidth;
        public int heightmapHeight;
        public float[,] GetHeights(int x, int y, int w, int h) => new float[0, 0];
    }

    public sealed class NavMeshAgent : Component
    {
        public float speed;
        public float angularSpeed;
        public float acceleration;
        public float radius;
        public float height;
        public bool isStopped;
        public bool hasPath;
        public Vector3 destination;
        public Vector3 velocity;
        public Vector3 pathEndPosition;
        public bool SetDestination(Vector3 target) => true;
        public void Stop() { }
        public void ResetPath() { }
        public bool Warp(Vector3 newPos) => true;
        public bool isOnNavMesh;
        public float remainingDistance;
    }

    // ============================================================
    // UI（UnityEngine.UI 命名空间，核心类型）
    // ============================================================
}

namespace UnityEngine.UI
{
    public class CanvasRenderer : Component
    {
        public void SetColor(Color c) { }
        public void SetTexture(Texture t) { }
        public void SetAlpha(float a) { }
    }

    public abstract class Graphic : Component
    {
        public Color color = Color.white;
        public Material material;
        public RaycastTarget raycastTarget;
        public void SetAllDirty() { }
        public void SetVerticesDirty() { }
        public void SetMaterialDirty() { }
        public float raycastPadding;
        public bool maskable;
        public enum RaycastTarget { }
    }

    public sealed class Image : Graphic
    {
        public Sprite sprite;
        public float fillAmount;
        public Type type;
        public enum Type { Simple = 0, Sliced = 1, Tiled = 2, Filled = 3 }
        public void SetNativeSize() { }
    }

    public sealed class Text : Graphic
    {
        public string text;
        public Font font;
        public int fontSize = 14;
        public FontStyle fontStyle;
        public bool supportRichText = true;
        public TextAnchor alignment;
        public float lineSpacing = 1f;
        public HorizontalWrapMode horizontalOverflow;
        public VerticalWrapMode verticalOverflow;
        public bool resizeTextForBestFit;
        public int resizeTextMinSize;
        public int resizeTextMaxSize;
        public int cachedTextGenerator;
        public int pixelsPerUnit;
        public int preferredWidth;
        public int preferredHeight;
        public void SetNativeSize() { }
    }

    public sealed class Font : Object
    {
        public Font(string name) { }
        public static Font CreateDynamicFontFromOSFont(string name, int size) => new Font(name);
    }

    public abstract class Selectable : Component
    {
        public bool interactable = true;
        public Navigation navigation;
        public bool IsInteractable() => interactable;
        public void Select() { }
        public struct Navigation { public int mode; }
    }

    public sealed class Button : Selectable
    {
        public event Action onClick;
        public void OnPointerClick() { }
    }

    public sealed class Slider : Selectable
    {
        public float value;
        public float minValue;
        public float maxValue = 1f;
        public bool wholeNumbers;
        public float normalizedValue;
        public event Action<float> onValueChanged;
        public void Set(float v, bool send) { }
    }

    public sealed class Toggle : Selectable
    {
        public bool isOn;
        public event Action<bool> onValueChanged;
    }

    public sealed class Scrollbar : Selectable
    {
        public float value;
        public float size;
        public event Action<float> onValueChanged;
    }

    public sealed class ScrollRect : Selectable
    {
        public RectTransform content;
        public bool horizontal;
        public bool vertical = true;
        public float horizontalScrollbar;
        public float verticalScrollbar;
        public void StopMovement() { }
        public void OnScroll(Vector2 d) { }
        public Vector2 velocity;
    }

    public sealed class Mask : Component
    {
        public bool showMaskGraphic;
    }

    public sealed class Outline : Component
    {
        public Color effectColor;
        public Vector2 effectDistance;
    }
}

namespace UnityEngine
{
    public sealed class Canvas : Component
    {
        public RenderMode renderMode;
        public Camera worldCamera;
        public float scaleFactor;
        public float referencePixelsPerUnit;
        public int sortingOrder;
        public int sortingLayerID;
        public enum RenderMode { ScreenSpaceOverlay = 0, ScreenSpaceCamera = 1, WorldSpace = 2 }
        public static void ForceUpdateCanvases() { }
    }

    public sealed class CanvasGroup : Component
    {
        public float alpha = 1f;
        public bool interactable = true;
        public bool blocksRaycasts = true;
        public bool ignoreParentGroups;
    }

    public sealed class RectTransformUtility
    {
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 p, Camera cam) => false;
    }
}
