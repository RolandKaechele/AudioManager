using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AudioManager.Runtime
{
    /// <summary>
    /// <b>AmbientController</b> plays one or more looping ambient sound layers simultaneously.
    /// <para>
    /// Each ambient layer gets its own <see cref="AudioSource"/> component, created on demand and returned
    /// to an internal pool on stop. Volume changes apply to all active layers proportionally.
    /// </para>
    /// </summary>
    [AddComponentMenu("AudioManager/Ambient Controller")]
    [DisallowMultipleComponent]
    public class AmbientController : MonoBehaviour
    {
        [Header("Fade")]
        [Tooltip("Seconds to fade in/out ambient layers.")]
        [SerializeField] private float defaultFadeDuration = 1.0f;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private readonly List<AudioSource> _activeSources = new();
        private readonly List<AudioSource> _pool          = new();
        private Coroutine _fadeRoutine;

        /// <summary>True when at least one ambient layer is playing.</summary>
        public bool IsPlaying => _activeSources.Count > 0 && _activeSources[0].isPlaying;

        /// <summary>Base volume set by the AudioManager volume system.</summary>
        internal float BaseVolume { get; set; } = 0.6f;

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Start playing the supplied ambient clips, fading in over <paramref name="fadeDuration"/> seconds.
        /// Any currently playing ambients are stopped first.
        /// </summary>
        public void Play(AudioClip[] clips, float fadeDuration = -1f)
        {
            if (fadeDuration < 0f) fadeDuration = defaultFadeDuration;
            StopImmediate();

            foreach (var clip in clips)
            {
                if (clip == null) continue;
                var src = GetOrCreateSource();
                src.clip   = clip;
                src.loop   = true;
                src.volume = 0f;
                src.Play();
                _activeSources.Add(src);
            }

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeInRoutine(fadeDuration));
        }

        /// <summary>Fade out and stop all ambient layers.</summary>
        public void Stop(float fadeDuration = -1f)
        {
            if (_activeSources.Count == 0) return;
            if (fadeDuration < 0f) fadeDuration = defaultFadeDuration;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeOutRoutine(fadeDuration));
        }

        /// <summary>Immediately stop all ambient layers without fade.</summary>
        public void StopImmediate()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            foreach (var src in _activeSources)
            {
                src.Stop();
                src.clip = null;
                _pool.Add(src);
            }
            _activeSources.Clear();
        }

        /// <summary>Apply a new base volume to all currently playing layers.</summary>
        internal void ApplyVolume(float effectiveVolume)
        {
            BaseVolume = effectiveVolume;
            foreach (var src in _activeSources)
                src.volume = effectiveVolume;
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private AudioSource GetOrCreateSource()
        {
            if (_pool.Count > 0)
            {
                var pooled = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                return pooled;
            }
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }

        private IEnumerator FadeInRoutine(float duration)
        {
            float elapsed = 0f;
            float target = BaseVolume;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float v = Mathf.Clamp01(elapsed / duration) * target;
                foreach (var src in _activeSources) src.volume = v;
                yield return null;
            }
            foreach (var src in _activeSources) src.volume = target;
        }

        private IEnumerator FadeOutRoutine(float duration)
        {
            float startVolume = _activeSources.Count > 0 ? _activeSources[0].volume : BaseVolume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float v = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                foreach (var src in _activeSources) src.volume = v;
                yield return null;
            }
            StopImmediate();
        }
    }
}
