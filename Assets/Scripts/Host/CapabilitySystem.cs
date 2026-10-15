using System;
using System.Collections.Generic;
using PxC.Core.Capabilities;
using PxC.Core.Contracts;

namespace PxC.Host
{
    /// <summary>
    /// 能力系统：零默认原则。插件按 RequiredCapabilities 申请，授权后才能调用敏感 API。
    /// Tier-0/1 内置能力在装配时 Grant；Tier-3 须经用户确认（由 UI 调用 Grant）。
    /// 运行时通过 HasCapability/CheckCapability 校验，越权拒绝并计数。
    /// </summary>
    public sealed class CapabilitySystem
    {
        private struct GrantedCap
        {
            public Capability Capability;
            public string Scope;
        }

        private readonly Dictionary<string, List<GrantedCap>> _granted;
        private readonly Dictionary<string, int> _violations;
        private Action<string> _logger;

        public CapabilitySystem(int capacity = 32)
        {
            _granted = new Dictionary<string, List<GrantedCap>>(capacity);
            _violations = new Dictionary<string, int>(capacity);
        }

        public void SetLogger(Action<string> logger) => _logger = logger;

        /// <summary>授予一组能力（装配/用户同意后）。</summary>
        public void Grant(string pluginId, IEnumerable<CapabilityClaim> claims)
        {
            if (claims == null) return;
            foreach (var claim in claims)
                Grant(pluginId, claim.Capability, claim.Scope);
        }

        public void Grant(string pluginId, Capability capability, string scope = null)
        {
            if (!_granted.TryGetValue(pluginId, out var list))
            {
                list = new List<GrantedCap>(4);
                _granted[pluginId] = list;
            }
            // 避免重复授予
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Capability == capability &&
                    string.Equals(list[i].Scope, scope, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            list.Add(new GrantedCap { Capability = capability, Scope = scope });
        }

        /// <summary>撤销插件全部能力（卸载/停用）。</summary>
        public void Revoke(string pluginId) => _granted.Remove(pluginId);

        /// <summary>撤销插件的单个能力；scope 为 null 时撤销该能力的所有 scope。</summary>
        public void Revoke(string pluginId, Capability capability, string scope = null)
        {
            if (!_granted.TryGetValue(pluginId, out var list)) return;
            list.RemoveAll(g => g.Capability == capability &&
                (scope == null || string.Equals(g.Scope, scope, StringComparison.OrdinalIgnoreCase)));
        }

        public bool HasCapability(string pluginId, Capability capability, string scope = null)
        {
            if (!_granted.TryGetValue(pluginId, out var list)) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Capability != capability) continue;
                // 授权 scope 为空=不限制；否则需匹配
                if (string.IsNullOrEmpty(list[i].Scope)) return true;
                if (string.Equals(list[i].Scope, scope, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>校验并记录越权尝试（安全失败）。</summary>
        public bool CheckCapability(string pluginId, Capability capability, string scope = null)
        {
            if (HasCapability(pluginId, capability, scope)) return true;
            _violations.TryGetValue(pluginId, out var n);
            _violations[pluginId] = n + 1;
            _logger?.Invoke($"[Capability] 拒绝越权: {pluginId} -> {capability} scope={scope}");
            return false;
        }

        public int ViolationCount(string pluginId) =>
            _violations.TryGetValue(pluginId, out var n) ? n : 0;

        public void Clear()
        {
            _granted.Clear();
            _violations.Clear();
        }
    }
}
