#if AUDIOMANAGER_VID
using UnityEngine;
using VideoManager.Runtime;

namespace AudioManager.Runtime
{
    /// <summary>
    /// Optional bridge between AudioManager and VideoManager.
    /// Enable define <c>AUDIOMANAGER_VID</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Automatically stops (or ducks) background music when a video begins and restores it
    /// when the video ends, preventing audio conflicts during video playback.
    /// </para>
    /// <para>
    /// <b>Behaviour:</b>
    /// <list type="bullet">
    /// <item>On <see cref="VideoManager.OnVideoStarted"/>: stops music with <see cref="stopFadeDuration"/>.</item>
    /// <item>On <see cref="VideoManager.OnVideoCompleted"/> or <see cref="VideoManager.OnVideoStopped"/>:
    ///       re-plays the last track (if recorded) with <see cref="resumeFadeDuration"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("AudioManager/Video Manager Bridge")]
    [DisallowMultipleComponent]
    public class VideoManagerBridge : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────────
        [Tooltip("Fade duration in seconds when stopping music as a video starts.")]
        [SerializeField] private float stopFadeDuration   = 0.5f;

        [Tooltip("Fade duration in seconds when resuming music after a video ends.")]
        [SerializeField] private float resumeFadeDuration = 1f;

        [Tooltip("When true, the last playing music track is automatically resumed after the video ends.")]
        [SerializeField] private bool  resumeAfterVideo   = true;

        // ─── References ───────────────────────────────────────────────────────
        private AudioManager _audio;
        private VideoManager.Runtime.VideoManager _video;

        private string _lastTrack;

        // ─── Unity ────────────────────────────────────────────────────────────
        private void Awake()
        {
            _audio = GetComponent<AudioManager>() ?? FindFirstObjectByType<AudioManager>();
            _video = GetComponent<VideoManager.Runtime.VideoManager>()
                     ?? FindFirstObjectByType<VideoManager.Runtime.VideoManager>();

            if (_audio == null) Debug.LogWarning("[AudioManager/VideoManagerBridge] AudioManager not found.");
            if (_video == null) Debug.LogWarning("[AudioManager/VideoManagerBridge] VideoManager not found.");
        }

        private void OnEnable()
        {
            if (_audio != null) _audio.OnMusicStarted += CacheTrack;

            if (_video != null)
            {
                _video.OnVideoStarted   += OnVideoStarted;
                _video.OnVideoCompleted += OnVideoEnded;
                _video.OnVideoStopped   += OnVideoEnded;
            }
        }

        private void OnDisable()
        {
            if (_audio != null) _audio.OnMusicStarted -= CacheTrack;

            if (_video != null)
            {
                _video.OnVideoStarted   -= OnVideoStarted;
                _video.OnVideoCompleted -= OnVideoEnded;
                _video.OnVideoStopped   -= OnVideoEnded;
            }
        }

        // ─── Handlers ─────────────────────────────────────────────────────────
        private void CacheTrack(string trackId) => _lastTrack = trackId;

        private void OnVideoStarted(string _videoId)
        {
            if (_audio != null)
                _audio.StopMusic(stopFadeDuration);
        }

        private void OnVideoEnded(string _videoId)
        {
            if (_audio != null && resumeAfterVideo && !string.IsNullOrEmpty(_lastTrack))
                _audio.PlayMusic(_lastTrack, resumeFadeDuration);
        }
    }
}
#else
namespace AudioManager.Runtime
{
    /// <summary>No-op stub. Enable AUDIOMANAGER_VID in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("AudioManager/Video Manager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class VideoManagerBridge : UnityEngine.MonoBehaviour { }
}
#endif
