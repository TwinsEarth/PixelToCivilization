// 仅在安装 Addressables 包后编译。
// 启用步骤：
//   1) 安装包：com.unity.addressables（Package Manager）。
//   2) 在 PxC.Infra.asmdef 的 Assembly References 中添加 Unity.Addressables 与 Unity.ResourceManager。
//   3) 可在 asmdef 的 Version Defines 中添加：Resource com.unity.addressables，Define UNITY_ADDRESSABLES。
// 未安装时本文件不参与编译，宿主自动使用 ResourcesAssetLoader（降级）。
#if UNITY_ADDRESSABLES
using System;
using System.Collections.Generic;
using PxC.Core.Services;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PxC.Infra.Assets
{
    /// <summary>
    /// 资源加载器：Addressables 后端（引用计数）。
    /// 每次 Load/Instantiate 保留 AsyncOperationHandle，结束必须 Release（镜像）。
    /// 禁止只 Destroy 不 Release。
    /// </summary>
    public sealed class AddressablesAssetLoader : IAssetLoader
    {
        private readonly Dictionary<int, AsyncOperationHandle> _handles;
        private int _nextId;

        public AddressablesAssetLoader(int capacity = 64)
        {
            _handles = new Dictionary<int, AsyncOperationHandle>(capacity);
            _nextId = 1;
        }

        public string BackendName => "Addressables";
        public int LiveHandleCount => _handles.Count;

        public AssetLoadHandle LoadAssetAsync(string address, Action<AssetLoadHandle> onComplete = null)
        {
            var op = Addressables.LoadAssetAsync<UnityEngine.Object>(address);
            int id = _nextId++;
            _handles[id] = op;
            op.Completed += h =>
            {
                bool ok = h.IsValid() && h.Status == AsyncOperationStatus.Succeeded;
                onComplete?.Invoke(new AssetLoadHandle
                {
                    Id = id,
                    Address = address,
                    IsValid = ok,
                    IsInstance = false,
                    Result = ok ? h.Result : null,
                    InternalHandle = h
                });
            };
            return new AssetLoadHandle
            {
                Id = id,
                Address = address,
                IsValid = true,
                IsInstance = false,
                InternalHandle = op
            };
        }

        public AssetLoadHandle InstantiateAsync(string address, Transform parent = null, Action<GameObject> onComplete = null)
        {
            var op = Addressables.InstantiateAsync(address, parent);
            int id = _nextId++;
            _handles[id] = op;
            op.Completed += h =>
            {
                bool ok = h.IsValid() && h.Status == AsyncOperationStatus.Succeeded;
                onComplete?.Invoke(ok ? h.Result : null);
            };
            return new AssetLoadHandle
            {
                Id = id,
                Address = address,
                IsValid = true,
                IsInstance = true,
                InternalHandle = op
            };
        }

        public void Release(ref AssetLoadHandle handle)
        {
            if (!handle.IsValid) return;
            if (_handles.TryGetValue(handle.Id, out var op))
            {
                if (op.IsValid()) Addressables.Release(op);
                _handles.Remove(handle.Id);
            }
            handle = AssetLoadHandle.Invalid;
        }
    }
}
#endif
