using System;
using UnityEngine;

namespace PxC.Core.Services
{
    /// <summary>
    /// 资源加载句柄。每次加载必须保留并在结束时 Release（配对）。
    /// 内部句柄在 Addressables 后端承载 AsyncOperationHandle，Resources 后端为 null。
    /// </summary>
    public struct AssetLoadHandle
    {
        public int Id;
        public string Address;
        public bool IsValid;
        public bool IsInstance;
        public object Result;
        /// <summary>后端内部句柄（Addressables 为 AsyncOperationHandle 装箱，Resources 为 null）。</summary>
        public object InternalHandle;

        public static readonly AssetLoadHandle Invalid = new AssetLoadHandle
        {
            Id = -1,
            IsValid = false
        };
    }

    /// <summary>
    /// 资源加载器：统一 Addressables 与 Resources 降级。
    /// 引用计数由实现维护，Load/Release 必须镜像，禁止只 Destroy 不 Release。
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>异步加载资产（非实例）。onComplete 带回句柄。</summary>
        AssetLoadHandle LoadAssetAsync(string address, Action<AssetLoadHandle> onComplete = null);

        /// <summary>异步实例化。onComplete 带回 GameObject。</summary>
        AssetLoadHandle InstantiateAsync(string address, Transform parent = null, Action<GameObject> onComplete = null);

        /// <summary>释放句柄（配对加载）。</summary>
        void Release(ref AssetLoadHandle handle);

        /// <summary>当前未释放的活动句柄数（探针/内存 Diff 用）。</summary>
        int LiveHandleCount { get; }

        /// <summary>使用的后端名称（"Addressables" 或 "Resources"）。</summary>
        string BackendName { get; }
    }
}
