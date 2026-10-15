using System;
using System.Collections.Generic;

namespace PxC.Core.Persistence
{
    /// <summary>单个插件段在全局存档中的 JSON 块（JsonUtility 不支持 Dictionary，故用 List）。</summary>
    [Serializable]
    public sealed class PluginSegmentBlob
    {
        public string PluginId;
        public int SegmentVersion;
        public string Json;
    }

    /// <summary>
    /// 全局存档 DTO。存档与运行时数据分离；包含版本字段用于迁移。
    /// 每插件的存档段以 JSON 字符串形式存于 Segments，实现分块存储与独立版本迁移。
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>全局存档版本（SaveVersion=0）。</summary>
        public int Version = 0;

        public string CreatedAt;
        public string UpdatedAt;
        public string BuildVersion;

        // ---- 核心全局状态 ----
        public int GameYear;
        public float Day;
        public int Population;
        public int FactionCount;

        // ---- 插件段 ----
        public List<PluginSegmentBlob> Segments = new List<PluginSegmentBlob>();

        public void SetSegment(string pluginId, int segmentVersion, string json)
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                if (Segments[i].PluginId == pluginId)
                {
                    Segments[i].SegmentVersion = segmentVersion;
                    Segments[i].Json = json;
                    return;
                }
            }
            Segments.Add(new PluginSegmentBlob
            {
                PluginId = pluginId,
                SegmentVersion = segmentVersion,
                Json = json
            });
        }

        /// <summary>直接放入一个段（按 PluginId 去重覆盖）。</summary>
        public void SetSegment(PluginSegmentBlob blob)
        {
            if (blob == null) return;
            for (int i = 0; i < Segments.Count; i++)
            {
                if (Segments[i].PluginId == blob.PluginId)
                {
                    Segments[i] = blob;
                    return;
                }
            }
            Segments.Add(blob);
        }

        public PluginSegmentBlob GetSegment(string pluginId)
        {
            for (int i = 0; i < Segments.Count; i++)
                if (Segments[i].PluginId == pluginId) return Segments[i];
            return null;
        }
    }
}
