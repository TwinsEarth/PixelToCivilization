using UnityEditor;
using UnityEngine;

namespace PixelToCivilization.EditorTools
{
    /// <summary>
    /// V7.1.0：Resources/Audio 下的实体音效统一按 Vorbis 压缩、内存加载，
    /// 避免 17 条 PCM WAV 以未压缩形式打进 WebGL 的 .data（可显著减小包体与内存峰值）。
    /// </summary>
    public class AudioImportPostprocessor : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Audio/")) return;
            var imp = (AudioImporter)assetImporter;
            var s = imp.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.quality = 0.7f;
            s.preloadAudioData = true;
            imp.defaultSampleSettings = s;
            // 各平台统一用这套设置（WebGL/Standalone）
            imp.SetOverrideSampleSettings("WebGL", s);
            imp.SetOverrideSampleSettings("Standalone", s);
            imp.forceToMono = false;
            imp.ambisonic = false;
        }
    }
}
