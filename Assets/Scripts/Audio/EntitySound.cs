using UnityEngine;

namespace PixelToCivilization.Audio
{
    /// <summary>声音类别：决定并发声轨预算与触发节奏（V7.1.0）。</summary>
    public enum SoundCat { Bird = 0, Ship = 1, Cart = 2 }

    /// <summary>
    /// V7.1.0 实体发声器：挂在鸟、船、车的视图根节点上。
    /// AudioManager 每 0.25s 扫描一次，按“离相机最近优先 + 每类声轨预算”随机触发，
    /// Res 对应 Resources/Audio 下的音频名（不含扩展名）。
    /// </summary>
    public class EntitySound : MonoBehaviour
    {
        public SoundCat Cat = SoundCat.Bird;
        public string Res = null;          // 例如 bird_0_songbird / ship_cannon_ship / cart_large_cart
        public float MaxDistance = 70f;    // 超过此距离不再发声
        public float BaseVolume = 1f;
        [HideInInspector] public float NextPlay; // 下次允许发声的 unscaledTime
    }
}
