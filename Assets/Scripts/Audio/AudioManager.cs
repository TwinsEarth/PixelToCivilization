using System.Collections.Generic;
using UnityEngine;

namespace PixelToCivilization.Audio
{
    /// <summary>
    /// V7.1.0 环境实体声音系统（第一版：6 种鸟鸣 / 8 种船 / 3 种车）。
    /// 设计：
    /// - 音频全部放在 Resources/Audio，按 Res 名懒加载并缓存；
    /// - 24 个 3D AudioSource 声轨池，空间化（spatialBlend=1、线性衰减）；
    /// - 每类并发上限：鸟 8 / 船 6 / 车 4，超出只播离相机最近者，控算力不吵杂；
    /// - 每 0.25s 扫描一次发声器（与 LOD 刷新同量级），按距离+随机节奏触发；
    /// - WebGL 浏览器自动播放策略：首次点击/按键/触摸后才解锁发声；
    /// - 静音状态持久化（PlayerPrefs pxc_muted），与 UI 声音按钮联动。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        const int POOL = 24;
        const float SCAN = 0.25f;

        // 每类声轨预算
        static readonly int[] Budget = { 8, 6, 4 };
        // 每类两次发声的随机间隔（现实秒）
        static readonly float[] IvMin = { 4.5f, 6f, 5f };
        static readonly float[] IvMax = { 12f, 16f, 13f };
        // 每类基础音量
        static readonly float[] BaseVol = { 0.62f, 0.95f, 0.85f };

        class Voice { public AudioSource Src; public SoundCat Cat; public bool Busy; }
        readonly List<Voice> _voices = new List<Voice>();
        readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        readonly List<EntitySound> _em = new List<EntitySound>();

        Transform _cam;
        float _scanT;
        bool _unlocked;
        bool _muted;

        void Awake()
        {
            I = this;
            _muted = PlayerPrefs.GetInt("pxc_muted", 0) == 1;
        }

        void Start()
        {
            for (int i = 0; i < POOL; i++)
            {
                var go = new GameObject("Voice_" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;          // 全 3D
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 8f;
                s.maxDistance = 120f;
                s.dopplerLevel = 0f;
                s.volume = 1f;
                _voices.Add(new Voice { Src = s, Cat = SoundCat.Bird, Busy = false });
            }
            AudioListener.volume = _muted ? 0f : 1f;
        }

        void Update()
        {
            // WebGL 自动播放策略：首次用户手势解锁
            if (!_unlocked)
            {
                bool gesture = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)
                    || Input.anyKeyDown || Input.touchCount > 0;
                if (gesture) { _unlocked = true; }
            }

            _scanT += Time.unscaledDeltaTime;
            if (_scanT < SCAN) return;
            _scanT = 0f;
            Scan();
        }

        public void SetMuted(bool m)
        {
            _muted = m;
            PlayerPrefs.SetInt("pxc_muted", m ? 1 : 0);
            AudioListener.volume = m ? 0f : 1f;
        }

        public bool IsMuted => _muted;

        AudioClip Clip(string res)
        {
            if (string.IsNullOrEmpty(res)) return null;
            if (_cache.TryGetValue(res, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + res);
            _cache[res] = c;   // 允许 null，避免重复查找缺失资源
            return c;
        }

        void Scan()
        {
            if (_muted || !_unlocked) return;
            if (_cam == null)
            {
                if (Camera.main != null) _cam = Camera.main.transform;
                if (_cam == null) return;
            }

            // 回收已播完声轨
            int[] playing = { 0, 0, 0 };
            foreach (var v in _voices)
            {
                if (v.Busy && !v.Src.isPlaying) { v.Busy = false; }
                if (v.Busy) playing[(int)v.Cat]++;
            }

            var ems = Object.FindObjectsByType<EntitySound>(FindObjectsSortMode.None);
            _em.Clear();
            Vector3 cp = _cam.position;

            // 每类挑出“到点且在听距内”的发声器，按距离升序
            for (int ci = 0; ci < 3; ci++)
            {
                int slots = Budget[ci] - playing[ci];
                if (slots <= 0) continue;

                _em.Clear();
                for (int i = 0; i < ems.Length; i++)
                {
                    var e = ems[i];
                    if (e == null || (int)e.Cat != ci) continue;
                    if (Time.unscaledTime < e.NextPlay) continue;
                    float d = Vector3.Distance(e.transform.position, cp);
                    if (d > e.MaxDistance) continue;
                    if (Clip(e.Res) == null) continue;
                    _em.Add(e);
                }
                // 最近优先，少量随机让同距离不固定
                _em.Sort((a, b) =>
                {
                    float da = Vector3.Distance(a.transform.position, cp) + Random.value * 6f;
                    float db = Vector3.Distance(b.transform.position, cp) + Random.value * 6f;
                    return da.CompareTo(db);
                });

                for (int k = 0; k < _em.Count && slots > 0; k++)
                {
                    var e = _em[k];
                    var voice = FreeVoice();
                    if (voice == null) break;
                    PlayOne(voice, (SoundCat)ci, e, cp);
                    slots--;
                }
            }
        }

        Voice FreeVoice()
        {
            foreach (var v in _voices) if (!v.Busy) return v;
            return null;
        }

        void PlayOne(Voice v, SoundCat cat, EntitySound e, Vector3 cp)
        {
            var clip = Clip(e.Res);
            if (clip == null) return;
            float d = Vector3.Distance(e.transform.position, cp);
            float t = Mathf.Clamp01(d / e.MaxDistance);
            float vol = Mathf.Lerp(1f, 0.06f, t) * e.BaseVolume * BaseVol[(int)cat];

            v.Busy = true; v.Cat = cat;
            v.Src.transform.position = e.transform.position;
            v.Src.clip = clip;
            v.Src.maxDistance = e.MaxDistance;
            v.Src.volume = Mathf.Clamp(vol, 0.02f, 1f);
            v.Src.pitch = Random.Range(0.92f, 1.08f); // 轻微变调，避免同质重复
            v.Src.loop = false;
            v.Src.Play();

            // 安排下一次发声：近处更勤、远处更稀
            float iv = Mathf.Lerp(IvMin[(int)cat], IvMax[(int)cat], t) * Random.Range(0.8f, 1.35f);
            e.NextPlay = Time.unscaledTime + iv;
        }
    }
}
