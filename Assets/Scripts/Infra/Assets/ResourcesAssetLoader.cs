using System;
using System.Collections.Generic;
using PxC.Core.Services;
using UnityEngine;

namespace PxC.Infra.Assets
{
    /// <summary>
    /// 资源加载器：Resources 后端（降级）。
    /// 每个活动句柄（无论资产还是实例）都持有一次资产引用；Release 时减计数，
    /// 归零卸载资产。保证 Load/Release 镜像。
    /// </summary>
    public sealed class ResourcesAssetLoader : IAssetLoader
    {
        private sealed class AssetEntry
        {
            public UnityEngine.Object Asset;
            public int Count;
        }

        private sealed class HandleEntry
        {
            public string Address;
            public bool IsInstance;
            public UnityEngine.Object Result;
        }

        private readonly Dictionary<string, AssetEntry> _assets;
        private readonly Dictionary<int, HandleEntry> _handles;
        private int _nextId;

        public ResourcesAssetLoader(int capacity = 64)
        {
            _assets = new Dictionary<string, AssetEntry>(capacity);
            _handles = new Dictionary<int, HandleEntry>(capacity);
            _nextId = 1;
        }

        public string BackendName => "Resources";
        public int LiveHandleCount => _handles.Count;

        public AssetLoadHandle LoadAssetAsync(string address, Action<AssetLoadHandle> onComplete = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                onComplete?.Invoke(AssetLoadHandle.Invalid);
                return AssetLoadHandle.Invalid;
            }
            if (!_assets.TryGetValue(address, out var entry))
            {
                var asset = Resources.Load(address);
                if (asset == null)
                {
                    onComplete?.Invoke(AssetLoadHandle.Invalid);
                    return AssetLoadHandle.Invalid;
                }
                entry = new AssetEntry { Asset = asset, Count = 0 };
                _assets[address] = entry;
            }
            entry.Count++;
            int id = _nextId++;
            _handles[id] = new HandleEntry { Address = address, IsInstance = false, Result = entry.Asset };
            var handle = new AssetLoadHandle
            {
                Id = id,
                Address = address,
                IsValid = true,
                IsInstance = false,
                Result = entry.Asset
            };
            onComplete?.Invoke(handle);
            return handle;
        }

        public AssetLoadHandle InstantiateAsync(string address, Transform parent = null, Action<GameObject> onComplete = null)
        {
            var assetHandle = LoadAssetAsync(address, null);
            if (!assetHandle.IsValid || !(assetHandle.Result is GameObject prefab))
            {
                if (assetHandle.IsValid) { var h = assetHandle; Release(ref h); }
                onComplete?.Invoke(null);
                return AssetLoadHandle.Invalid;
            }
            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            // 手柄从"资产"切换为"实例"
            _handles[assetHandle.Id].IsInstance = true;
            _handles[assetHandle.Id].Result = instance;
            var handle = new AssetLoadHandle
            {
                Id = assetHandle.Id,
                Address = address,
                IsValid = true,
                IsInstance = true,
                Result = instance
            };
            onComplete?.Invoke(instance);
            return handle;
        }

        public void Release(ref AssetLoadHandle handle)
        {
            if (!handle.IsValid) return;
            if (!_handles.TryGetValue(handle.Id, out var entry))
            {
                handle = AssetLoadHandle.Invalid;
                return;
            }

            if (entry.IsInstance && entry.Result != null)
                UnityEngine.Object.Destroy(entry.Result);

            if (_assets.TryGetValue(entry.Address, out var asset))
            {
                asset.Count--;
                if (asset.Count <= 0)
                {
                    // GameObject prefab 由 UnloadUnusedAssets 回收；非 GameObject 可直接卸载
                    if (asset.Asset != null && !(asset.Asset is GameObject))
                        Resources.UnloadAsset(asset.Asset);
                    _assets.Remove(entry.Address);
                }
            }

            _handles.Remove(handle.Id);
            handle = AssetLoadHandle.Invalid;
        }
    }
}
