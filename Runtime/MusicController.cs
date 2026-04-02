using System;
using System.Collections;
using UnityEngine;

namespace AudioManager.Runtime
{
    /// <summary>
    /// <b>MusicController</b> manages background music playback with smooth crossfading.
    /// <para>
    /// Uses two <see cref="AudioSource"/> components to crossfade seamlessly between tracks.
    /// Attach to the same GameObject as <see cref="AudioManager"/> or any persistent manager.
    /// </para>
    /// </summary>
    [AddComponentMenu("AudioManager/Music Controller")]
    [DisallowMultipleComponent]
    public class MusicController : MonoBehaviour
    {
        [Header("Crossfade")]
        [Tooltip("Default crossfade duration when not specified per-track.")]
        [SerializeField] private float defaultCrossfadeDuration = 1.5f;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _active;     // currently playing source
        private Coroutine   _fadeRoutine;

        /// <summary>The <see cref="AudioClip"/> currently scheduled to play (or playing).</summary>
        public AudioClip CurrentClip => _active != null ? _active.clip : null;

        /// <summary>True while a music track is playing.</summary>
        public bool IsPlaying => _active != null && _active.isPlaying;

        /// <summary>Target base volume applied by <see cref="AudioManager"/> volume scaling.</summary>
        internal float BaseVolume { get; set; } = 1f;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            _sourceA = gameObject.AddComponent<AudioSource>();
            _sourceB = gameObject.AddComponent<AudioSource>();
            _sourceA.playOnAwake = false;
            _sourceB.playOnAwake = false;
            _active = _sourceA;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Play a clip immediately (no crossfade), stopping any currently playing track.
        /// </summary>
        public void Play(AudioClip clip, bool loop = true, float volume = 1f)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            float effective = BaseVolume * volume;
            _active.Stop();
            _active.clip   = clip;
            _active.loop   = loop;
            _active.volume = effective;
            _active.Play();
        }

        /// <summary>
        /// Crossfade from the currently playing track to <paramref name="clip"/> over <paramref name="duration"/> seconds.
        /// If nothing is playing, the new clip starts immediately with a fade-in.
        /// </summary>
        public void CrossfadeTo(AudioClip clip, bool loop = true, float volume = 1f, float duration = -1f)
        {
            if (clip == null) { Stop(); return; }
            if (duration < 0f) duration = defaultCrossfadeDuration;

            // If same clip is already playing, do nothing
            if (_active.isPlaying && _active.clip == clip) return;

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(CrossfadeRoutine(clip, loop, volume, duration));
        }

        /// <summary>Fade out and stop the currently playing track.</summary>
        public void Stop(float fadeDuration = -1f)
        {
            if (!_active.isPlaying) return;
            if (fadeDuration < 0f) fadeDuration = defaultCrossfadeDuration;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeOutRoutine(_active, fadeDuration));
        }

        /// <summary>Immediately stop without fade.</summary>
        public void StopImmediate()
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _sourceA.Stop();
            _sourceB.Stop();
        }

        /// <summary>Update volume on the active source when settings change.</summary>
        internal void ApplyVolume(float effectiveVolume)
        {
            BaseVolume = effectiveVolume;
            if (_active != null && _active.isPlaying)
                _active.volume = effectiveVolume;
        }

        // -------------------------------------------------------------------------
        // Coroutines
        // -------------------------------------------------------------------------

        private IEnumerator CrossfadeRoutine(AudioClip clip, bool loop, float targetVolume, float duration)
        {
            AudioSource outgoing = _active;
            AudioSource incoming = (outgoing == _sourceA) ? _sourceB : _sourceA;
            _active = incoming;

            float effective = BaseVolume * targetVolume;
            incoming.clip   = clip;
            incoming.loop   = loop;
            incoming.volume = 0f;
            incoming.Play();

            float elapsed = 0f;
            float startVolume = outgoing.volume;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                incoming.volume = effective * t;
                outgoing.volume = startVolume * (1f - t);
                yield return null;
            }
            incoming.volume = effective;
            outgoing.Stop();
            outgoing.clip = null;
        }

        private IEnumerator FadeOutRoutine(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }
            source.Stop();
            source.clip = null;
        }
    }
}
