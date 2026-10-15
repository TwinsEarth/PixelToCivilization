using UnityEngine;

namespace PxC.Core.Services
{
    /// <summary>
    /// 统一对象池：覆盖 GameObject（UI/投射物/视图）与 C# 数据对象。
    /// Rent/Return 必须配对；池内对象按 prefab 或类型分组，禁止在活跃时重复 Rent。
    /// </summary>
    public interface IObjectPool
    {
        // ---- GameObject 池 ----
        /// <summary>租用一个实例（池中没有则创建）。parent 为容器（通常为对象池根节点）。</summary>
        GameObject Rent(GameObject prefab, Transform parent = null);

        /// <summary>归还实例（SetActive(false) 并回到池根）。</summary>
        void Return(GameObject instance);

        /// <summary>预实例化若干对象。</summary>
        void Prewarm(GameObject prefab, int count, Transform parent = null);

        // ---- 数据对象池（C# class，零 GC 复用）----
        T RentData<T>(System.Func<T> factory = null) where T : class, new();
        void ReturnData<T>(T instance) where T : class;

        // ---- 统计 / 清理 ----
        int ActiveCount(GameObject prefab);
        int InactiveCount(GameObject prefab);
        void Clear(GameObject prefab);
        void ClearAll();
    }
}
