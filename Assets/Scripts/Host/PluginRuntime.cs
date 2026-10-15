using System;
using System.Collections.Generic;
using PxC.Core.Contracts;
using PxC.Core.Events;
using PxC.Core.Persistence;
using PxC.Core.Services;
using PxC.Core.World;
using UnityEngine;

namespace PxC.Host
{
    /// <summary>插件生命周期状态。</summary>
    public enum PluginState
    {
        Discovered = 0,
        Registered = 1,
        Initialized = 2,
        Started = 3,
        Paused = 4,
        Stopped = 5,
        Faulted = 6
    }

    /// <summary>
    /// 插件运行时：发现注册、依赖解析（DAG/Kahn 拓扑 + 环检测）、生命周期驱动、
    /// 能力授予、故障隔离、存档段收集/恢复。不含业务规则。
    /// </summary>
    public sealed class PluginRuntime
    {
        private readonly Dictionary<string, IPlugin> _plugins;
        private readonly Dictionary<string, PluginState> _states;
        private readonly Dictionary<string, int> _faults;
        private readonly HashSet<string> _disabled;

        private readonly IEventBus _events;
        private readonly IServiceLocator _services;
        private readonly ChunkedWorld _world;
        private readonly CapabilitySystem _capability;
        private readonly Action<string> _logger;
        private readonly int _hostVersion;

        private List<IPlugin> _sorted;
        private List<ISimPlugin> _sim;
        private List<IViewPlugin> _view;
        private List<IContentPlugin> _content;

        private const int MaxConsecutiveFaults = 5;

        public PluginRuntime(
            IEventBus events,
            IServiceLocator services,
            ChunkedWorld world,
            CapabilitySystem capability,
            int hostVersion,
            Action<string> logger)
        {
            _events = events;
            _services = services;
            _world = world;
            _capability = capability;
            _hostVersion = hostVersion;
            _logger = logger;

            _plugins = new Dictionary<string, IPlugin>(64);
            _states = new Dictionary<string, PluginState>(64);
            _faults = new Dictionary<string, int>(64);
            _disabled = new HashSet<string>();
        }

        public IReadOnlyList<ISimPlugin> SimPlugins => _sim;
        public IReadOnlyList<IViewPlugin> ViewPlugins => _view;
        public IReadOnlyList<IContentPlugin> ContentPlugins => _content;
        public int PluginCount => _plugins.Count;

        // ----------------------------------------------------------------
        // 发现
        // ----------------------------------------------------------------
        public void Discover(IPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));
            if (string.IsNullOrEmpty(plugin.Id))
                throw new ArgumentException("插件 Id 不能为空");
            if (_plugins.ContainsKey(plugin.Id))
                throw new ArgumentException($"重复的插件 Id: {plugin.Id}");
            _plugins[plugin.Id] = plugin;
            _states[plugin.Id] = PluginState.Discovered;
        }

        // ----------------------------------------------------------------
        // 依赖解析 + 拓扑排序（Kahn，含环检测）
        // ----------------------------------------------------------------
        public bool BuildOrder(out List<string> errors)
        {
            errors = new List<string>();

            // 1. 校验依赖存在性/版本
            foreach (var kv in _plugins)
            {
                var p = kv.Value;
                if (p.MinHostVersion > _hostVersion)
                    errors.Add($"插件 {p.Id} 需要宿主版本 {p.MinHostVersion}，当前 {_hostVersion}");
                if (p.Dependencies == null) continue;
                for (int i = 0; i < p.Dependencies.Count; i++)
                {
                    var dep = p.Dependencies[i];
                    if (!_plugins.TryGetValue(dep.Id, out var target))
                    {
                        if (!dep.Optional)
                            errors.Add($"插件 {p.Id} 依赖缺失: {dep.Id}");
                        continue;
                    }
                    if (target.Version < dep.MinVersion)
                        errors.Add($"插件 {p.Id} 依赖 {dep.Id} 版本 {dep.MinVersion}，当前 {target.Version}");
                }
            }
            if (errors.Count > 0) return false;

            // 2. 入度（仅计必选依赖）
            var indegree = new Dictionary<string, int>(_plugins.Count);
            var dependents = new Dictionary<string, List<string>>(_plugins.Count);
            foreach (var kv in _plugins)
            {
                indegree[kv.Key] = 0;
                dependents[kv.Key] = new List<string>();
            }
            foreach (var kv in _plugins)
            {
                var p = kv.Value;
                if (p.Dependencies == null) continue;
                for (int i = 0; i < p.Dependencies.Count; i++)
                {
                    var dep = p.Dependencies[i];
                    if (dep.Optional || !_plugins.ContainsKey(dep.Id)) continue;
                    indegree[kv.Key]++;
                    dependents[dep.Id].Add(kv.Key);
                }
            }

            // 3. Kahn
            var queue = new Queue<string>();
            foreach (var kv in indegree)
                if (kv.Value == 0) queue.Enqueue(kv.Key);

            _sorted = new List<IPlugin>(_plugins.Count);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                _sorted.Add(_plugins[id]);
                var list = dependents[id];
                for (int i = 0; i < list.Count; i++)
                {
                    var d = list[i];
                    indegree[d]--;
                    if (indegree[d] == 0) queue.Enqueue(d);
                }
            }

            if (_sorted.Count != _plugins.Count)
            {
                errors.Add("检测到插件依赖环，无法构建加载顺序");
                foreach (var kv in indegree)
                    if (kv.Value > 0) errors.Add($"环中节点: {kv.Key}");
                return false;
            }

            // 4. 分类缓存
            _sim = new List<ISimPlugin>(_sorted.Count);
            _view = new List<IViewPlugin>(_sorted.Count);
            _content = new List<IContentPlugin>(_sorted.Count);
            for (int i = 0; i < _sorted.Count; i++)
            {
                if (_sorted[i] is ISimPlugin s) _sim.Add(s);
                if (_sorted[i] is IViewPlugin v) _view.Add(v);
                if (_sorted[i] is IContentPlugin c) _content.Add(c);
            }
            return true;
        }

        // ----------------------------------------------------------------
        // 生命周期（按拓扑顺序）
        // ----------------------------------------------------------------
        public bool InitializeAll()
        {
            bool allOk = true;
            for (int i = 0; i < _sorted.Count; i++)
            {
                var p = _sorted[i];
                if (!DependenciesReady(p))
                {
                    _states[p.Id] = PluginState.Faulted;
                    allOk = false;
                    continue;
                }
                // 授予能力
                _capability.Grant(p.Id, p.RequiredCapabilities);
                var ctx = new PluginContext(p.Id, _events, _services, _world, _capability, _logger);
                if (!SafeCall(p, () => p.OnRegister(ctx), PluginState.Registered)) { allOk = false; continue; }
                if (!SafeCall(p, p.OnInitialize, PluginState.Initialized)) { allOk = false; continue; }
            }
            return allOk;
        }

        public void StartAll()
        {
            for (int i = 0; i < _sim.Count; i++)
            {
                var p = _sim[i];
                if (_states[p.Id] == PluginState.Initialized || _states[p.Id] == PluginState.Paused)
                    SafeCall(p, () => { }, PluginState.Started);
            }
            for (int i = 0; i < _view.Count; i++)
            {
                var p = _view[i];
                SafeCall(p, p.OnViewReady, PluginState.Started);
            }
            for (int i = 0; i < _content.Count; i++)
            {
                var p = _content[i];
                SafeCall(p, p.OnContentLoaded, PluginState.Started);
            }
        }

        /// <summary>逐插件 Tick（GameLoop 已做算力/预算过滤，本方法只调用并隔离故障）。</summary>
        public void TickSim(ISimPlugin p, float simDt)
        {
            if (!IsLive(p.Id)) return;
            if (!SafeCall(p, () => p.OnTick(simDt), PluginState.Started))
                TryDisable(p);
        }

        public void TickYear(int gameYear)
        {
            for (int i = 0; i < _sim.Count; i++)
            {
                var p = _sim[i];
                if (IsLive(p.Id)) SafeCall(p, () => p.OnYear(gameYear), PluginState.Started);
            }
        }

        public void TickEra(int newEra, int oldEra)
        {
            for (int i = 0; i < _sim.Count; i++)
            {
                var p = _sim[i];
                if (IsLive(p.Id)) SafeCall(p, () => p.OnEra(newEra, oldEra), PluginState.Started);
            }
        }

        public void TickDynasty(int idx, int gameYear)
        {
            for (int i = 0; i < _sim.Count; i++)
            {
                var p = _sim[i];
                if (IsLive(p.Id)) SafeCall(p, () => p.OnDynasty(idx, gameYear), PluginState.Started);
            }
        }

        public void TickView(float realDt)
        {
            for (int i = 0; i < _view.Count; i++)
            {
                var p = _view[i];
                if (IsLive(p.Id)) SafeCall(p, () => p.OnViewTick(realDt), PluginState.Started);
            }
        }

        /// <summary>卸载全部（逆拓扑顺序，OnShutdown 必须镜像 OnInitialize）。</summary>
        public void ShutdownAll()
        {
            for (int i = _view.Count - 1; i >= 0; i--)
            {
                var p = _view[i];
                SafeCall(p, p.OnViewRelease, PluginState.Stopped);
            }
            for (int i = _sorted.Count - 1; i >= 0; i--)
            {
                var p = _sorted[i];
                SafeCall(p, p.OnShutdown, PluginState.Stopped);
                _capability.Revoke(p.Id);
            }
        }

        // ----------------------------------------------------------------
        // 存档段
        // ----------------------------------------------------------------
        public void CollectSegments(SaveData data)
        {
            for (int i = 0; i < _sorted.Count; i++)
            {
                var p = _sorted[i];
                ISaveSegment seg = null;
                SafeCall(p, () => seg = p.GetSaveSegment(), _states[p.Id]);
                if (seg == null) continue;
                string json = JsonUtility.ToJson(seg);
                data.SetSegment(p.Id, seg.SegmentVersion, json);
            }
        }

        public void RestoreSegments(SaveData data)
        {
            for (int i = 0; i < _sorted.Count; i++)
            {
                var p = _sorted[i];
                var blob = data.GetSegment(p.Id);
                if (blob == null) continue;
                ISaveSegment seg = null;
                SafeCall(p, () => seg = p.GetSaveSegment(), _states[p.Id]);
                if (seg == null) continue;
                // 段版本迁移由插件内部处理；这里先反序列化
                JsonUtility.FromJsonOverwrite(blob.Json, seg);
                SafeCall(p, () => p.RestoreSaveSegment(seg, data.Version), _states[p.Id]);
            }
        }

        // ----------------------------------------------------------------
        // 内部
        // ----------------------------------------------------------------
        private bool DependenciesReady(IPlugin p)
        {
            if (p.Dependencies == null) return true;
            for (int i = 0; i < p.Dependencies.Count; i++)
            {
                var d = p.Dependencies[i];
                if (d.Optional) continue;
                if (!_states.TryGetValue(d.Id, out var st) ||
                    (st != PluginState.Initialized && st != PluginState.Started))
                    return false;
            }
            return true;
        }

        private bool IsLive(string id) =>
            !_disabled.Contains(id) &&
            _states.TryGetValue(id, out var st) &&
            (st == PluginState.Started || st == PluginState.Initialized);

        private bool SafeCall(IPlugin p, Action action, PluginState onSuccess)
        {
            try
            {
                action();
                if (onSuccess != PluginState.Discovered) _states[p.Id] = onSuccess;
                ResetFault(p.Id);
                return true;
            }
            catch (Exception e)
            {
                AddFault(p.Id);
                _states[p.Id] = PluginState.Faulted;
                _logger?.Invoke($"[PluginRuntime] {p.Id} 执行异常: {e.GetType().Name}: {e.Message}");
                if (_faults[p.Id] >= MaxConsecutiveFaults)
                {
                    _disabled.Add(p.Id);
                    _capability.Revoke(p.Id);
                    _logger?.Invoke($"[PluginRuntime] 插件连续故障达到上限，已隔离: {p.Id}");
                }
                return false;
            }
        }

        private void TryDisable(IPlugin p)
        {
            if (_faults.TryGetValue(p.Id, out var n) && n >= MaxConsecutiveFaults)
            {
                _disabled.Add(p.Id);
                _capability.Revoke(p.Id);
                _logger?.Invoke($"[PluginRuntime] 已隔离高频故障插件: {p.Id}");
            }
        }

        private void AddFault(string id)
        {
            _faults.TryGetValue(id, out var n);
            _faults[id] = n + 1;
        }
        private void ResetFault(string id) => _faults[id] = 0;
    }
}
