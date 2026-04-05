#if AUDIOMANAGER_DOTWEEN
using System;
using UnityEngine;
using DG.Tweening;

namespace AudioManager.Runtime
{
    /// <summary>
    /// Optional bridge that replaces the coroutine-based music crossfade inside
    /// <see cref="MusicController"/> with DOTween-driven volume tweens, providing
    /// precise easing control over all music transitions.
    /// Enable define <c>AUDIOMANAGER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Sets <see cref="AudioManager.PlayMusicOverride"/> and
    /// <see cref="AudioManager.StopMusicOverride"/> so that all music play/stop requests
    /// are routed through this bridge instead of the default <see cref="MusicController"/>
    /// coroutine implementation.
    /// </para>
    /// </summary>
    [AddComponentMenu("AudioManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenAudioBridge : MonoBehaviour
    {
        [Header("Crossfade")]
        [Tooltip("Default crossfade duration used when the caller does not specify one (≤0).")]
        [SerializeField] private float defaultCrossfadeDuration = 1.2f;

        [Tooltip("DOTween ease applied to music crossfades.")]
        [SerializeField] private Ease crossfadeEase = Ease.InOutSine;

        [Header("Volume")]
        [Tooltip("Target volume for the incoming music track (0–1). " +
                 "Apply AudioManager volume scaling separately via AudioManager.Settings.")]
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 1f;

        // -------------------------------------------------------------------------

        private AudioManager _audio;

        /// <summary>Source A — one of two sources used for crossfading.</summary>
        private AudioSource _srcA;

        /// <summary>Source B — the other crossfade source.</summary>
        private AudioSource _srcB;

        /// <summary>Currently active (playing) source.</summary>
        private AudioSource _active;

        private void Awake()
        {
            _audio = GetComponent<AudioManager>() ?? FindFirstObjectByType<AudioManager>();
            if (_audio == null)
            {
                Debug.LogWarning("[AudioManager/DotweenAudioBridge] AudioManager not found.");
                return;
            }

            // Dedicated AudioSources on this bridge's GameObject, separate from MusicController.
            _srcA                = gameObject.AddComponent<AudioSource>();
            _srcB                = gameObject.AddComponent<AudioSource>();
            _srcA.playOnAwake    = false;
            _srcB.playOnAwake    = false;
            _srcA.spatialBlend   = 0f;
            _srcB.spatialBlend   = 0f;
            _active              = _srcA;
        }

        private void OnEnable()
        {
            if (_audio == null) return;
            _audio.PlayMusicOverride = HandlePlayMusic;
            _audio.StopMusicOverride = HandleStopMusic;
        }

        private void OnDisable()
        {
            if (_audio == null) return;
            if (_audio.PlayMusicOverride == (Action<string, bool, float>)HandlePlayMusic)
                _audio.PlayMusicOverride = null;
            if (_audio.StopMusicOverride == (Action<float>)HandleStopMusic)
                _audio.StopMusicOverride = null;
        }

        private void OnDestroy()
        {
            DOTween.Kill(_srcA);
            DOTween.Kill(_srcB);
        }

        // -------------------------------------------------------------------------

        private void HandlePlayMusic(string resourcePath, bool loop, float fadeDuration)
        {
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager/DotweenAudioBridge] AudioClip not found at Resources/{resourcePath}");
                return;
            }

            float duration = fadeDuration > 0f ? fadeDuration : defaultCrossfadeDuration;

            var next = _active == _srcA ? _srcB : _srcA;

            next.clip   = clip;
            next.loop   = loop;
            next.volume = 0f;
            next.Play();

            DOTween.Kill(_active);
            DOTween.Kill(next);

            var outgoing = _active;
            _active.DOFade(0f, duration)
                   .SetEase(crossfadeEase)
                   .OnComplete(() => outgoing.Stop());

            next.DOFade(musicVolume, duration).SetEase(crossfadeEase);

            _active = next;
        }

        private void HandleStopMusic(float fadeDuration)
        {
            if (_active == null || !_active.isPlaying) return;

            float duration = fadeDuration > 0f ? fadeDuration : defaultCrossfadeDuration;

            DOTween.Kill(_active);
            var src = _active;
            src.DOFade(0f, duration)
               .SetEase(crossfadeEase)
               .OnComplete(() => src.Stop());
        }
    }
}
#else
namespace AudioManager.Runtime
{
    /// <summary>No-op stub — enable define <c>AUDIOMANAGER_DOTWEEN</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("AudioManager/DOTween Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DotweenAudioBridge : UnityEngine.MonoBehaviour { }
}
#endif
