using UnityEngine;
using PixelToCivilization.Core;

namespace PixelToCivilization.Systems
{
    /// <summary>
    /// V9.6.1 战场战时广播系统：以传令兵口吻向整个作战系统广播 战报 / 指令 / 双方战损。
    /// 语音通道（WebGL）：index.html 注入 window.pxcSpeak → 浏览器 speechSynthesis 中文 TTS；
    /// Editor/其他平台：仅 Debug.Log（不打断开发）。
    /// 文本通道：编年史（kind=war/good/bad）+ 顶部滚动信息条（TickerBar 从编年史取重要事件）。
    /// 战损统计：会话内累计 敌方击毁数 / 我方损失数，供"战况定期播报"。
    /// 节流：战报 6s / 战损 8s / 指令即时（真实时间 unscaledTime，防高倍速刷屏）。
    /// </summary>
    public class WarBroadcastSystem : GameSystemBase
    {
        /// <summary>会话累计：击毁敌方单位数（战果）</summary>
        public int EnemyKills;
        /// <summary>会话累计：我方损失数</summary>
        public int SelfLosses;

        /// <summary>
        /// V9.8.3 全局语音开关：WebGL 默认静音。speechSynthesis 的 cancel/speak 是 Chrome 已知渲染进程崩溃源
        /// （用户「集结令连续插旗 5 次必弹窗卡死」根因），因此 WebGL 构建默认不调用浏览器 TTS；
        /// 需要语音的玩家可在设置面板开启（未来接入 UI 开关），开启后仍受 2.5s 节流+140 字截断+JS try/catch 保护。
        /// Editor/非 WebGL 平台不受影响（仅 Debug.Log）。
        /// </summary>
        public static bool VoiceEnabled = false;

        float _reportCd, _lossCd, _situationCd;
        const float ReportGap = 6f;
        const float LossGap = 8f;
        const float SituationGap = 30f;

        public override void Init(GameManager gm) { base.Init(gm); }

        /// <summary>通用广播：写编年史 + 语音（kind: good/bad/info）</summary>
        public void Broadcast(string text, string kind)
        {
            if (string.IsNullOrEmpty(text)) return;
            GM.AddEvent(kind == "loss" ? "bad" : kind == "kill" ? "good" : "info", "📢 " + text);
            Speak(text);
        }

        /// <summary>指令广播（集结令/投送/编队）：即时、无节流</summary>
        public void Command(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            GM.AddEvent("info", "📢 " + text);
            Speak(text);
        }

        /// <summary>
        /// V9.8.3 静默指令广播：只写编年史/横幅，不触发语音。
        /// 集结令插旗链路（可高频连点）是 speechSynthesis 崩溃源，一律走静默通道；
        /// 战报/战损仍走 Command/Report（受 VoiceEnabled 全局开关保护）。
        /// </summary>
        public void SilentCommand(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            GM.AddEvent("info", "📢 " + text);
        }

        /// <summary>战报（交战/战况/攻防）：节流 6s</summary>
        public void Report(string text)
        {
            if (Time.unscaledTime < _reportCd) return;
            _reportCd = Time.unscaledTime + ReportGap;
            GM.AddEvent("info", "📢 " + text);
            Speak(text);
        }

        /// <summary>战损广播：ours=true 我方损失 / false 敌方战果；计数无条件累计，广播节流 8s</summary>
        public void Loss(string text, bool ours)
        {
            if (ours) SelfLosses++; else EnemyKills++;
            if (Time.unscaledTime < _lossCd) return;
            _lossCd = Time.unscaledTime + LossGap;
            GM.AddEvent(ours ? "bad" : "good", "📢 " + text);
            Speak(text);
        }

        /// <summary>传令兵语音：WebGL → 浏览器 TTS；Editor → Log。
        /// V9.8.2 加固：真实时间 2.5s 节流 + 截断 140 字（与 JS 侧节流双保险，防高频插旗触发 speechSynthesis 崩溃）。</summary>
        float _speakCd;
        const float SpeakGap = 2.5f;
        public void Speak(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (Time.unscaledTime < _speakCd) return;
            _speakCd = Time.unscaledTime + SpeakGap;
            if (text.Length > 140) text = text.Substring(0, 140);
#if UNITY_WEBGL && !UNITY_EDITOR
            // V9.8.3 根治：WebGL 默认静音（speechSynthesis 是 Chrome 已知渲染进程崩溃源，玩家实测连插旗必崩）。
            // VoiceEnabled 默认 false：默认不调用浏览器 TTS，杜绝崩溃；未来设置面板开启后仍受节流+截断+JS try/catch 保护。
            if (!VoiceEnabled) return;
            try
            {
                // decodeURIComponent 方案：中文/单引号/换行全部转 ASCII 安全传输
                Application.ExternalEval("window.pxcSpeak(decodeURIComponent('" + System.Uri.EscapeDataString(text) + "'))");
            }
            catch (System.Exception e) { Debug.LogWarning("[WarVoice] " + e.GetType().Name + ": " + e.Message); }
#else
            Debug.Log("[WarVoice·传令兵] " + text);
#endif
        }

        public override void Tick(float dt)
        {
            // 战况定期播报：真实时间 30s 一次，仅当有战损时才报（防开局空报）
            if (Time.unscaledTime < _situationCd) return;
            _situationCd = Time.unscaledTime + SituationGap;
            if (EnemyKills + SelfLosses <= 0) return;
            Report("报——战况：我军击毁敌 " + EnemyKills + " 个单位，损失 " + SelfLosses + "。");
        }

        /// <summary>新局重置（StartNewGame 调用，防上一局战损残留）</summary>
        public void ResetRuntime()
        {
            EnemyKills = 0; SelfLosses = 0;
            _reportCd = 0f; _lossCd = 0f; _situationCd = 0f;
        }
    }
}
