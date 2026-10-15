// UnityEngine stub —— 系统类（仅供 Roslyn 无 Unity 编译验证）
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    // ============================================================
    // Debug
    // ============================================================
    public static class Debug
    {
        public static bool isDebugBuild = true;
        public static ILogger logger { get; set; }
        public static void Log(object o) { }
        public static void Log(object o, Object ctx) { }
        public static void LogFormat(string f, params object[] a) { }
        public static void LogWarning(object o) { }
        public static void LogWarning(object o, Object ctx) { }
        public static void LogWarningFormat(string f, params object[] a) { }
        public static void LogError(object o) { }
        public static void LogError(object o, Object ctx) { }
        public static void LogErrorFormat(string f, params object[] a) { }
        public static void LogException(Exception e) { }
        public static void LogException(Exception e, Object ctx) { }
        public static void LogAssertion(object o) { }
        public static void Assert(bool cond) { }
        public static void Assert(bool cond, object o) { }
        public static void AssertFormat(bool cond, string f, params object[] a) { }
        public static void DrawLine(Vector3 s, Vector3 e, Color c, float duration, bool depthTest) { }
        public static void DrawLine(Vector3 s, Vector3 e, Color c) { }
        public static void DrawLine(Vector3 s, Vector3 e) { }
        public static void DrawRay(Vector3 s, Vector3 dir, Color c) { }
        public static void DrawRay(Vector3 s, Vector3 dir) { }
    }

    // ============================================================
    // Time
    // ============================================================
    public sealed class Time
    {
        public static int frameCount;
        public static float time;
        public static float deltaTime;
        public static float unscaledTime;
        public static float unscaledDeltaTime;
        public static float fixedTime;
        public static float fixedDeltaTime = 0.02f;
        public static float fixedUnscaledTime;
        public static float fixedUnscaledDeltaTime = 0.02f;
        public static float timeScale = 1f;
        public static float realtimeSinceStartup;
        public static float captureDeltaTime;
        public static int captureFramerate;
        public static float maximumDeltaTime = 0.333f;
        public static float smoothDeltaTime;
        public static float timeSinceLevelLoad;
        public static bool inFixedTimeStep;
    }

    // ============================================================
    // Application
    // ============================================================
    public sealed class Application
    {
        public static event Action<string, string, LogType> logMessageReceived;
        public static event Action<string, string, LogType> logMessageReceivedThreaded;
        public static event Action lowMemory;
        public static bool isPlaying;
        public static bool isEditor;
        public static bool isFocused = true;
        public static bool isMobilePlatform;
        public static bool isConsolePlatform;
        public static bool genuine;
        public static bool genuineCheckAvailable;
        public static RuntimePlatform platform;
        public static string dataPath;
        public static string persistentDataPath;
        public static string temporaryCachePath;
        public static string streamingAssetsPath;
        public static string companyName;
        public static string productName;
        public static string version;
        public static string unityVersion;
        public static string installMode;
        public static string absoluteURL;
        public static int targetFrameRate = -1;
        public static SystemLanguage systemLanguage;
        public static ThreadPriority backgroundLoadingPriority = ThreadPriority.BelowNormal;
        public static float backgroundLoadingTimeout;
        public static void Quit() { }
        public static void Quit(int exitCode) { }
        public static void OpenURL(string url) { }
        public static bool CanStreamedLevelBeLoaded(string name) => false;
        public static bool HasProLicense() => false;
        public static void GetStackTraceLogType(LogType t) { }
        public static void SetStackTraceLogType(LogType t, int stackTrace) { }
        public static bool RequestUserAuthorization(int mode) => false;
        public static void Internal_Log() { }
    }

    // ============================================================
    // RuntimeInitializeOnLoadMethod
    // ============================================================
    public enum RuntimeInitializeLoadType { AfterSceneLoad = 0, BeforeSceneLoad = 1, AfterAssembliesLoaded = 2, BeforeSplashScreen = 3, SubsystemRegistration = 4 }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeLoadType loadType;
        public RuntimeInitializeOnLoadMethodAttribute() { loadType = RuntimeInitializeLoadType.AfterSceneLoad; }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { loadType = type; }
    }

    public enum RuntimeInitializeLoadTypeLegacy { }

    // ============================================================
    // Input
    // ============================================================
    public sealed class Input
    {
        public static Vector3 mousePosition;
        public static bool mousePresent;
        public static int touchCount;
        public static float GetAxis(string axis) => 0f;
        public static float GetAxisRaw(string axis) => 0f;
        public static bool GetButton(string name) => false;
        public static bool GetButtonDown(string name) => false;
        public static bool GetButtonUp(string name) => false;
        public static bool GetKey(KeyCode key) => false;
        public static bool GetKey(string name) => false;
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetKeyDown(string name) => false;
        public static bool GetKeyUp(KeyCode key) => false;
        public static bool GetMouseButton(int b) => false;
        public static bool GetMouseButtonDown(int b) => false;
        public static bool GetMouseButtonUp(int b) => false;
        public static bool anyKey;
        public static bool anyKeyDown;
        public static float scrollDelta;
        public static void ResetInputAxes() { }
    }

    // ============================================================
    // Screen
    // ============================================================
    public sealed class Screen
    {
        public static int width = 1920;
        public static int height = 1080;
        public static float dpi = 96f;
        public static bool fullScreen = true;
        public static FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
        public static int currentResolution;
        public static Resolution[] resolutions;
        public static int sleepTimeout = -1;
        public static bool autorotateToPortrait;
        public static bool autorotateToLandscapeLeft;
        public static float brightness;
        public static void SetResolution(int w, int h, bool full) { }
        public static void SetResolution(int w, int h, FullScreenMode mode) { }
    }

    public struct Resolution
    {
        public int width, height, refreshRate;
    }

    public enum SystemLanguage { Afrikaans = 0, Arabic = 1, ChineseSimplified = 26, ChineseTraditional = 27, English = 10, Japanese = 50, Unknown = 0 }

    // ============================================================
    // SystemInfo
    // ============================================================
    public sealed class SystemInfo
    {
        public static int processorCount;
        public static int systemMemorySize;
        public static int graphicsMemorySize;
        public static string processorType;
        public static string graphicsDeviceName;
        public static string graphicsDeviceVendor;
        public static string graphicsDeviceVersion;
        public static string operatingSystem;
        public static string deviceModel;
        public static string deviceName;
        public static DeviceType deviceType;
        public static OperatingSystemFamily operatingSystemFamily;
        public static bool supportsWebGL1;
        public static bool supportsWebGL2;
        public static bool supportsComputeShaders;
        public static int maxTextureSize;
        public static bool supportsInstancing;
        public static bool supportsGraphicsFence;
        public static int supportedRenderTargetCount;
    }

    // ============================================================
    // QualitySettings
    // ============================================================
    public sealed class QualitySettings
    {
        public static int pixelLightCount;
        public static ShadowQuality shadows;
        public static float shadowDistance;
        public static int shadowCascades;
        public static int vSyncCount;
        public static int antiAliasing;
        public static int maxQueuedFrames = 2;
        public static int activeColorSpace;
        public static string[] names;
        public static void IncreaseLevel() { }
        public static void DecreaseLevel() { }
        public static void SetQualityLevel(int index) { }
    }

    public enum ShadowQuality { Disable = 0, HardOnly = 1, All = 2 }
    public enum ShadowResolution { Low = 0, Medium = 1, High = 2, VeryHigh = 3 }
    public enum ShadowProjection { CloseFit = 0, StableFit = 1 }
    public enum ShadowmaskMode { DistanceShadowmask = 0, Shadowmask = 1 }

    // ============================================================
    // Resources
    // ============================================================
    public sealed class Resources
    {
        public static T Load<T>(string path) where T : Object => null;
        public static Object Load(string path) => null;
        public static Object Load(string path, Type t) => null;
        public static ResourceRequest LoadAsync<T>(string path) where T : Object => new ResourceRequest();
        public static ResourceRequest LoadAsync(string path) => new ResourceRequest();
        public static ResourceRequest LoadAsync(string path, Type t) => new ResourceRequest();
        public static void UnloadAsset(Object asset) { }
        public static AsyncOperation UnloadUnusedAssets() => new AsyncOperation();
        public static T GetBuiltinExtraResource<T>(string path) where T : Object => null;
        public static T FindObjectsOfTypeAll<T>() where T : Object => null;
    }

    public class ResourceRequest : AsyncOperation
    {
        public Object asset { get; set; }
    }

    // ============================================================
    // PlayerPrefs
    // ============================================================
    public sealed class PlayerPrefs
    {
        private static readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
        private static readonly Dictionary<string, float> _floats = new Dictionary<string, float>();
        private static readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
        public static int GetInt(string key, int def = 0) => _ints.TryGetValue(key, out var v) ? v : def;
        public static void SetInt(string key, int v) => _ints[key] = v;
        public static float GetFloat(string key, float def = 0f) => _floats.TryGetValue(key, out var v) ? v : def;
        public static void SetFloat(string key, float v) => _floats[key] = v;
        public static string GetString(string key, string def = "") => _strings.TryGetValue(key, out var v) ? v : def;
        public static string GetString(string key) => _strings.TryGetValue(key, out var v) ? v : "";
        public static void SetString(string key, string v) => _strings[key] = v;
        public static bool HasKey(string key) => _ints.ContainsKey(key) || _floats.ContainsKey(key) || _strings.ContainsKey(key);
        public static void Save() { }
        public static void DeleteKey(string key) { _ints.Remove(key); _floats.Remove(key); _strings.Remove(key); }
        public static void DeleteAll() { _ints.Clear(); _floats.Clear(); _strings.Clear(); }
    }

    // JsonUtility 已移至独立文件 JsonUtility.cs（真实反射实现）

    // ============================================================
    // Random
    // ============================================================
    public sealed class Random
    {
        public static float value => 0.5f;
        public static Vector3 insideUnitSphere => Vector3.zero;
        public static Vector2 insideUnitCircle => Vector2.zero;
        public static Vector3 onUnitSphere => Vector3.zero;
        public static Quaternion rotation => Quaternion.identity;
        public static Quaternion rotationUniform => Quaternion.identity;
        public static int seed { get; set; }
        public static void InitState(int seed) { }
        public static float Range(float min, float max) => (min + max) * 0.5f;
        public static int Range(int min, int max) => min;
        public static Color ColorHSV(float h, float s, float v) => Color.white;
    }

    // ============================================================
    // Graphics
    // ============================================================
    public sealed class Graphics
    {
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer) { }
        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submesh) { }
        public static void DrawMeshInstanced(Mesh mesh, int sub, Material mat, Matrix4x4[] matrices, int count) { }
        public static void Blit(Texture src, RenderTexture dst) { }
        public static void Blit(Texture src, RenderTexture dst, Material mat) { }
        public static void DrawTexture(Texture t, Matrix4x4 matrix) { }
        public static int renderTextureCount;
    }

    // ============================================================
    // 协程等待
    // ============================================================
    public sealed class Coroutine
    {
        internal Coroutine() { }
    }
    public sealed class WaitForSeconds : IEnumerator
    {
        public WaitForSeconds(float seconds) { }
        public object Current => null;
        public bool MoveNext() => false;
        public void Reset() { }
    }
    public sealed class WaitForSecondsRealtime : IEnumerator
    {
        public WaitForSecondsRealtime(float seconds) { }
        public object Current => null;
        public bool MoveNext() => false;
        public void Reset() { }
    }
    public sealed class WaitForEndOfFrame : IEnumerator
    {
        public object Current => null;
        public bool MoveNext() => false;
        public void Reset() { }
    }
    public sealed class WaitForFixedUpdate : IEnumerator
    {
        public object Current => null;
        public bool MoveNext() => false;
        public void Reset() { }
    }
    public sealed class WaitUntil : IEnumerator
    {
        private Func<bool> _pred;
        public WaitUntil(Func<bool> p) { _pred = p; }
        public object Current => null;
        public bool MoveNext() => !_pred();
        public void Reset() { }
    }
    public sealed class WaitWhile : IEnumerator
    {
        private Func<bool> _pred;
        public WaitWhile(Func<bool> p) { _pred = p; }
        public object Current => null;
        public bool MoveNext() => _pred();
        public void Reset() { }
    }

    public class AsyncOperation
    {
        public bool isDone;
        public float progress;
        public bool allowSceneActivation = true;
        public event Action completed;
    }

    // ============================================================
    // LayerMask / AnimationCurve / Gradient
    // ============================================================
    public struct LayerMask
    {
        public int m_MaskValue;
        public int value { get => m_MaskValue; set => m_MaskValue = value; }
        public static implicit operator int(LayerMask m) => m.m_MaskValue;
        public static implicit operator LayerMask(int i) => new LayerMask { m_MaskValue = i };
        public static string LayerToName(int layer) => "";
        public static int NameToLayer(string name) => 0;
    }

    public struct Keyframe
    {
        public float time, value, inTangent, outTangent, weightedMode;
        public Keyframe(float time, float value) { this.time = time; this.value = value; inTangent = 0; outTangent = 0; weightedMode = 0; }
        public Keyframe(float time, float value, float inT, float outT) { this.time = time; this.value = value; inTangent = inT; outTangent = outT; weightedMode = 0; }
    }

    public class AnimationCurve
    {
        public Keyframe[] keys;
        public int length;
        public WrapMode preWrapMode, postWrapMode;
        public AnimationCurve(params Keyframe[] keys) { this.keys = keys; }
        public static AnimationCurve Linear(float t1, float v1, float t2, float v2) => new AnimationCurve();
        public static AnimationCurve Constant(float t1, float t2, float v) => new AnimationCurve();
        public static AnimationCurve EaseInOut(float t1, float v1, float t2, float v2) => new AnimationCurve();
        public float Evaluate(float t) => 0f;
        public int AddKey(float t, float v) => 0;
        public int MoveKey(int index, Keyframe k) => 0;
    }

    public enum WrapMode { Once = 0, Loop = 2, PingPong = 4, Default = 1, ClampForever = 8 }

    public struct GradientColorKey
    {
        public Color color;
        public float time;
        public GradientColorKey(Color c, float t) { color = c; time = t; }
    }
    public struct GradientAlphaKey
    {
        public float alpha, time;
        public GradientAlphaKey(float a, float t) { alpha = a; time = t; }
    }

    public sealed class Gradient
    {
        public GradientColorKey[] colorKeys;
        public GradientAlphaKey[] alphaKeys;
        public GradientMode mode;
        public Color Evaluate(float t) => Color.white;
        public void SetKeys(GradientColorKey[] c, GradientAlphaKey[] a) { }
    }
    public enum GradientMode { Blend = 0, Fixed = 1, PerceptualBlend = 2 }

    // ============================================================
    // Gizmos
    // ============================================================
    public sealed class Gizmos
    {
        public static Color color;
        public static Matrix4x4 matrix;
        public static void DrawWireSphere(Vector3 c, float r) { }
        public static void DrawSphere(Vector3 c, float r) { }
        public static void DrawLine(Vector3 f, Vector3 t) { }
        public static void DrawRay(Vector3 f, Vector3 d) { }
        public static void DrawWireCube(Vector3 c, Vector3 s) { }
        public static void DrawCube(Vector3 c, Vector3 s) { }
        public static void DrawIcon(Vector3 c, string name) { }
        public static void DrawGUITexture(Rect r, Texture t) { }
    }

    // ============================================================
    // Logger
    // ============================================================
    public interface ILogger { void Log(object o); }
    public interface ILogHandler { void LogException(Exception e, Object ctx); }
}
