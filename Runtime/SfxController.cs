using System.Collections.Generic;
using UnityEngine;

namespace AudioManager.Runtime
{
    /// <summary>
    /// <b>SfxController</b> plays short, one-shot sound effects using a pool of <see cref="AudioSource"/> components.
    /// <para>
    /// Sources are reused from the pool once their clip has finished playing, keeping allocation overhead minimal.
    /// </para>
    /// </summary>
    [AddComponentMenu("AudioManager/SFX Controller")]
    [DisallowMultipleComponent]
    public class SfxController : MonoBehaviour
    {
        [Header("Pool")]
        [Tooltip("Number of AudioSource components pre-created in the pool at startup.")]
        [SerializeField] private int initialPoolSize = 8;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private readonly List<AudioSource> _pool = new();

        /// <summary>Base volume set by the AudioManager volume system.</summary>
        internal float BaseVolume { get; set; } = 1f;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            for (int i = 0; i < initialPoolSize; i++)
                _pool.Add(CreateSource());
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>Play a one-shot SFX clip at the given volume multiplier.</summary>
        public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            var src = GetAvailableSource();
            src.pitch = pitch;
            src.PlayOneShot(clip, BaseVolume * volume);
        }

        /// <summary>
        /// Play a one-shot SFX clip at a world position using <see cref="AudioSource.PlayClipAtPoint"/>.
        /// The volume is scaled by <see cref="BaseVolume"/>.
        /// </summary>
        public void PlayAt(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, BaseVolume * volume);
        }

        /// <summary>Stop all currently playing SFX (returns sources to the pool).</summary>
        public void StopAll()
        {
            foreach (var src in _pool)
            {
                if (src.isPlaying) src.Stop();
            }
        }

        /// <summary>Apply a new base volume. Active one-shots are not retroactively adjusted.</summary>
        internal void ApplyVolume(float effectiveVolume)
        {
            BaseVolume = effectiveVolume;
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private AudioSource GetAvailableSource()
        {
            foreach (var src in _pool)
                if (!src.isPlaying) return src;

            // Pool exhausted — grow it
            var newSrc = CreateSource();
            _pool.Add(newSrc);
            return newSrc;
        }

        private AudioSource CreateSource()
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            return src;
        }
    }
}
