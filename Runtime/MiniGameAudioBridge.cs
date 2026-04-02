#if AUDIOMANAGER_MGM
using UnityEngine;
using MiniGameManager.Runtime;

namespace AudioManager.Runtime
{
    /// <summary>
    /// Optional bridge between AudioManager and MiniGameManager.
    /// Enable define <c>AUDIOMANAGER_MGM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Plays configured audio tracks or SFX clips when a mini-game starts, completes, or is aborted.
    /// All ids are <c>Resources/Audio/</c>-relative paths or AudioManager track ids.
    /// </para>
    /// </summary>
    [AddComponentMenu("AudioManager/Mini Game Audio Bridge")]
    [DisallowMultipleComponent]
    public class MiniGameAudioBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Header("Mini-game start")]
        [Tooltip("Music track id or path to play when a mini-game starts. Leave empty to skip.")]
        [SerializeField] private string startMusicId = string.Empty;
        [Tooltip("SFX id or path to play when a mini-game starts.")]
        [SerializeField] private string startSfxId = string.Empty;

        [Header("Mini-game complete")]
        [Tooltip("Music track id or path to play on mini-game completion. Leave empty to skip.")]
        [SerializeField] private string completeMusicId = string.Empty;
        [Tooltip("SFX id or path to play on mini-game completion.")]
        [SerializeField] private string completeSfxId = string.Empty;

        [Header("Mini-game abort")]
        [Tooltip("SFX id or path to play when a mini-game is aborted.")]
        [SerializeField] private string abortSfxId = string.Empty;

        [Header("Options")]
        [Tooltip("Crossfade duration in seconds when switching music tracks.")]
        [SerializeField] private float musicFadeDuration = 0.5f;

        // ─── References ──────────────────────────────────────────────────────────
        private AudioManager _audio;
        private MiniGameManager.Runtime.MiniGameManager _mgr;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _audio = GetComponent<AudioManager>() ?? FindFirstObjectByType<AudioManager>();
            _mgr   = GetComponent<MiniGameManager.Runtime.MiniGameManager>()
                     ?? FindFirstObjectByType<MiniGameManager.Runtime.MiniGameManager>();

            if (_audio == null) Debug.LogWarning("[MiniGameAudioBridge] AudioManager not found.");
            if (_mgr   == null) Debug.LogWarning("[MiniGameAudioBridge] MiniGameManager not found.");
        }

        private void OnEnable()
        {
            if (_mgr != null)
            {
                _mgr.OnMiniGameStarted   += OnStarted;
                _mgr.OnMiniGameCompleted += OnCompleted;
                _mgr.OnMiniGameAborted   += OnAborted;
            }
        }

        private void OnDisable()
        {
            if (_mgr != null)
            {
                _mgr.OnMiniGameStarted   -= OnStarted;
                _mgr.OnMiniGameCompleted -= OnCompleted;
                _mgr.OnMiniGameAborted   -= OnAborted;
            }
        }

        // ─── Handlers ────────────────────────────────────────────────────────────
        private void OnStarted(string id)
        {
            if (_audio == null) return;
            if (!string.IsNullOrEmpty(startMusicId)) _audio.PlayMusic(startMusicId, musicFadeDuration);
            if (!string.IsNullOrEmpty(startSfxId))   _audio.PlaySfx(startSfxId);
        }

        private void OnCompleted(MiniGameResult result)
        {
            if (_audio == null) return;
            if (!string.IsNullOrEmpty(completeMusicId)) _audio.PlayMusic(completeMusicId, musicFadeDuration);
            if (!string.IsNullOrEmpty(completeSfxId))   _audio.PlaySfx(completeSfxId);
        }

        private void OnAborted(string id)
        {
            if (_audio == null) return;
            if (!string.IsNullOrEmpty(abortSfxId)) _audio.PlaySfx(abortSfxId);
        }
    }
}
#else
namespace AudioManager.Runtime
{
    /// <summary>No-op stub. Enable AUDIOMANAGER_MGM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("AudioManager/Mini Game Audio Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MiniGameAudioBridge : UnityEngine.MonoBehaviour { }
}
#endif
