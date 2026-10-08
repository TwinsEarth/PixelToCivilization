using System;
using System.Collections.Generic;

namespace PixelToCivilization.AI.Agents
{
    /// <summary>
    /// 智能体注册表：启动时从 AgentCatalog 批量注册；提供按 id / 类别 / 分组 / 系统实例查询。
    /// 身份只读、按原型共享；实体实例通过 Id 引用。
    /// </summary>
    public class AgentRegistry
    {
        private readonly Dictionary<string, AgentIdentity> _byId = new();
        private readonly Dictionary<AgentKind, List<AgentIdentity>> _byKind = new();
        private readonly Dictionary<string, List<AgentIdentity>> _byGroup = new();
        private readonly Dictionary<Type, string> _systemTypeToId = new();

        public int Count => _byId.Count;
        public IEnumerable<AgentIdentity> All => _byId.Values;

        public void Register(AgentIdentity a)
        {
            if (a == null || string.IsNullOrEmpty(a.Id)) return;
            _byId[a.Id] = a;
            if (!_byKind.TryGetValue(a.Kind, out var kl)) { kl = new List<AgentIdentity>(); _byKind[a.Kind] = kl; }
            if (!kl.Contains(a)) kl.Add(a);
            var gk = a.Group ?? "";
            if (!_byGroup.TryGetValue(gk, out var gl)) { gl = new List<AgentIdentity>(); _byGroup[gk] = gl; }
            if (!gl.Contains(a)) gl.Add(a);
        }

        /// <summary>把一个系统实例的类型绑定到其身份 id（系统级智能体由 Director 按此找到对应系统）</summary>
        public void BindSystem(Type systemType, string id)
        {
            if (systemType != null && !string.IsNullOrEmpty(id)) _systemTypeToId[systemType] = id;
        }

        public AgentIdentity Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _byId.TryGetValue(id, out var a);
            return a;
        }

        public bool Has(string id) => !string.IsNullOrEmpty(id) && _byId.ContainsKey(id);

        public List<AgentIdentity> OfKind(AgentKind kind)
            => _byKind.TryGetValue(kind, out var l) ? new List<AgentIdentity>(l) : new List<AgentIdentity>();

        public List<AgentIdentity> OfGroup(string group)
            => _byGroup.TryGetValue(group ?? "", out var l) ? new List<AgentIdentity>(l) : new List<AgentIdentity>();

        public List<string> GroupNames() => new(_byGroup.Keys);

        /// <summary>由系统实例找到身份（系统级）</summary>
        public AgentIdentity ForSystem(object system)
        {
            if (system == null) return null;
            return _systemTypeToId.TryGetValue(system.GetType(), out var id) ? Get(id) : null;
        }
    }
}
