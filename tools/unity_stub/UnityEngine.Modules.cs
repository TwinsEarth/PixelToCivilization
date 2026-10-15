// UnityEngine stub —— Addressables（仅供 Roslyn 无 Unity 编译验证）
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement.AsyncOperations
{
    public enum AsyncOperationStatus { None = 0, Succeeded = 1, Failed = 2 }

    public struct AsyncOperationHandle : IEquatable<AsyncOperationHandle>
    {
        public bool IsValid => true;
        public bool IsDone => true;
        public float PercentComplete => 1f;
        public AsyncOperationStatus Status => AsyncOperationStatus.Succeeded;
        public Exception OperationException => null;
        public object Result;
        public object m_InternalOp;
        public int m_Version;
        public void Release() { }
        public bool Equals(AsyncOperationHandle o) => false;
        public override bool Equals(object o) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(AsyncOperationHandle a, AsyncOperationHandle b) => false;
        public static bool operator !=(AsyncOperationHandle a, AsyncOperationHandle b) => true;
    }

    public struct AsyncOperationHandle<T> : IEquatable<AsyncOperationHandle<T>>
    {
        public T Result;
        public bool IsValid => true;
        public bool IsDone => true;
        public float PercentComplete => 1f;
        public AsyncOperationStatus Status => AsyncOperationStatus.Succeeded;
        public Exception OperationException => null;
        public object m_InternalOp;
        public int m_Version;
        public void Release() { }
        public static implicit operator AsyncOperationHandle(AsyncOperationHandle<T> h) => new AsyncOperationHandle();
        public bool Equals(AsyncOperationHandle<T> o) => false;
        public override bool Equals(object o) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(AsyncOperationHandle<T> a, AsyncOperationHandle<T> b) => false;
        public static bool operator !=(AsyncOperationHandle<T> a, AsyncOperationHandle<T> b) => true;
    }

    public interface IAsyncOperation
    {
        bool IsDone { get; }
        float PercentComplete { get; }
    }
}

namespace UnityEngine.AddressableAssets
{
    using UnityEngine.ResourceManagement.AsyncOperations;

    public class AssetReference
    {
        public string AssetGUID;
        public object RuntimeKey => AssetGUID;
        public AssetReference(string guid) { AssetGUID = guid; }
        public bool IsValid() => false;
        public bool IsDone => false;
        public AsyncOperationHandle<T> LoadAssetAsync<T>() => new AsyncOperationHandle<T>();
        public void ReleaseAsset() { }
        public AsyncOperationHandle<GameObject> InstantiateAsync(Transform parent = null) => new AsyncOperationHandle<GameObject>();
        public void ReleaseInstance(GameObject obj) { }
        public bool RuntimeKeyIsValid() => false;
    }

    public class AssetReferenceT<T> : AssetReference
    {
        public AssetReferenceT(string guid) : base(guid) { }
    }

    public static class Addressables
    {
        public static AsyncOperationHandle InitializeAsync() => new AsyncOperationHandle();
        public static AsyncOperationHandle<T> LoadAssetAsync<T>(object key) => new AsyncOperationHandle<T>();
        public static AsyncOperationHandle<T> LoadAssetAsync<T>(AssetReference reference) => new AsyncOperationHandle<T>();
        public static AsyncOperationHandle<GameObject> InstantiateAsync(object key, Transform parent = null, bool instantiateInWorldSpace = false) => new AsyncOperationHandle<GameObject>();
        public static AsyncOperationHandle<GameObject> InstantiateAsync(object key, Transform parent) => new AsyncOperationHandle<GameObject>();
        public static AsyncOperationHandle<IList<T>> LoadAssetsAsync<T>(object key, Action<T> callback) => new AsyncOperationHandle<IList<T>>();
        public static void Release(AsyncOperationHandle handle) { }
        public static void Release<T>(AsyncOperationHandle<T> handle) { }
        public static void ReleaseInstance(AsyncOperationHandle handle) { }
        public static void ReleaseInstance(GameObject instance) { }
        public static bool CheckForCatalogUpdates() => false;
    }

    public sealed class AddressablesImpl { }
}

// ============================================================
// UnityEngine.Pool
// ============================================================
namespace UnityEngine.Pool
{
    public interface IObjectPool<T> where T : class
    {
        int CountInactive { get; }
        T Get();
        PooledObject<T> Get(out T v);
        void Release(T element);
        void Clear();
    }

    public struct PooledObject<T> : IDisposable where T : class
    {
        private readonly T m_ToReturn;
        private readonly IObjectPool<T> m_Pool;
        internal PooledObject(T value, IObjectPool<T> pool) { m_ToReturn = value; m_Pool = pool; }
        public void Dispose() => m_Pool?.Release(m_ToReturn);
    }

    public class ObjectPool<T> : IDisposable, IObjectPool<T> where T : class
    {
        private readonly Func<T> m_CreateFunc;
        private readonly Action<T> m_ActionOnGet;
        private readonly Action<T> m_ActionOnRelease;
        private readonly Action<T> m_ActionOnDestroy;
        private readonly int m_MaxSize;
        private readonly Stack<T> m_Stack;
        private int m_CountAll;

        public int CountAll { get; private set; }
        public int CountActive => CountAll - CountInactive;
        public int CountInactive => m_Stack.Count;

        public ObjectPool(Func<T> createFunc,
            Action<T> actionOnGet = null,
            Action<T> actionOnRelease = null,
            Action<T> actionOnDestroy = null,
            bool collectionCheck = true,
            int defaultCapacity = 10,
            int maxSize = 10000)
        {
            m_CreateFunc = createFunc;
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
            m_ActionOnDestroy = actionOnDestroy;
            m_MaxSize = maxSize;
            m_Stack = new Stack<T>(defaultCapacity);
        }

        public T Get()
        {
            T element;
            if (m_Stack.Count == 0)
            {
                element = m_CreateFunc();
                CountAll++;
            }
            else
            {
                element = m_Stack.Pop();
            }
            m_ActionOnGet?.Invoke(element);
            return element;
        }

        public PooledObject<T> Get(out T v) => new PooledObject<T>(v = Get(), this);

        public void Release(T element)
        {
            m_ActionOnRelease?.Invoke(element);
            if (m_Stack.Count < m_MaxSize)
                m_Stack.Push(element);
            else
                m_ActionOnDestroy?.Invoke(element);
        }

        public void Clear()
        {
            while (m_Stack.Count > 0)
                m_ActionOnDestroy?.Invoke(m_Stack.Pop());
        }

        public void Dispose() => Clear();
    }

    public class CollectionPool<TCollection, TItem> : IDisposable
        where TCollection : ICollection<TItem>, new()
    {
        public void Dispose() { }
        public void Release(TCollection element) { }
        public TCollection Get() => new TCollection();
    }
}

// ============================================================
// UnityEngine.Events
// ============================================================
namespace UnityEngine.Events
{
    public class UnityEvent
    {
        public void AddListener(Action call) { }
        public void RemoveListener(Action call) { }
        public void Invoke() { }
        public void RemoveAllListeners() { }
    }

    public class UnityEvent<T>
    {
        public void AddListener(Action<T> call) { }
        public void RemoveListener(Action<T> call) { }
        public void Invoke(T arg) { }
        public void RemoveAllListeners() { }
    }
}

// ============================================================
// SceneManagement
// ============================================================
namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single = 0, Additive = 1 }
    public enum LoadSceneParameters { }

    public struct Scene
    {
        public string name;
        public string path;
        public int buildIndex;
        public bool isLoaded;
        public bool IsValid() => buildIndex >= 0;
        public int rootCount;
        public GameObject[] GetRootGameObjects() => new GameObject[0];
    }

    public static class SceneManager
    {
        public static int sceneCount;
        public static int sceneCountInBuildSettings;
        public static event Action<Scene, LoadSceneMode> sceneLoaded;
        public static event Action<Scene> sceneUnloaded;
        public static event Action<Scene, Scene> activeSceneChanged;
        public static Scene GetActiveScene() => new Scene { name = "Main", buildIndex = 0, isLoaded = true };
        public static Scene GetSceneAt(int index) => GetActiveScene();
        public static Scene GetSceneByName(string name) => GetActiveScene();
        public static Scene GetSceneByPath(string path) => GetActiveScene();
        public static void SetActiveScene(Scene scene) { }
        public static AsyncOperation LoadSceneAsync(string name, LoadSceneMode mode = LoadSceneMode.Single) => new AsyncOperation();
        public static AsyncOperation LoadSceneAsync(string name) => new AsyncOperation();
        public static AsyncOperation LoadSceneAsync(int buildIndex) => new AsyncOperation();
        public static Scene LoadScene(string name) => GetActiveScene();
        public static AsyncOperation UnloadSceneAsync(Scene scene) => new AsyncOperation();
        public static void CreateScene(string name) { }
        public static int GetSceneCount() => 1;
    }
}

// ============================================================
// UnityEngine.Profiling
// ============================================================
namespace UnityEngine.Profiling
{
    public sealed class Profiler
    {
        public static bool enabled;
        public static bool enableBinaryLog;
        public static string logFile;
        public static bool supported;
        public static int usedHeapSizeLong;
        public static int GetTotalAllocatedMemoryLong() => 0;
        public static int GetTotalUnusedReservedMemoryLong() => 0;
        public static int GetTotalReservedMemoryLong() => 0;
        public static int GetMonoUsedSizeLong() => 0;
        public static int GetAllocatedMemoryForGraphicsDriver() => 0;
        public static void BeginSample(string name) { }
        public static void BeginSample(string name, Object target) { }
        public static void EndSample() { }
        public static void BeginThreadProfiling(string group, string name) { }
        public static void EndThreadProfiling() { }
    }

    public sealed class Recorder
    {
        public bool enabled;
        public long lastFrameValue;
        public static Recorder Get(string name) => new Recorder();
    }
}
