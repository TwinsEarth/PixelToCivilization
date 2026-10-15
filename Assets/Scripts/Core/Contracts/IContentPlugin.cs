using System.Collections.Generic;

namespace PxC.Core.Contracts
{
    /// <summary>
    /// 内容插件：定义数据（ScriptableObject）、资产地址、Addressables 分组。
    /// 数据驱动，便于朝代历史包、场景副本、单位/美术包独立发布。
    /// </summary>
    public interface IContentPlugin : IPlugin
    {
        /// <summary>需要加载的定义/配置地址（可空，纯数据类可返回空）。</summary>
        IReadOnlyList<string> DefinitionAddresses { get; }

        /// <summary>所属 Addressables 分组。</summary>
        IReadOnlyList<string> ContentGroups { get; }

        void OnContentLoaded();
    }
}
