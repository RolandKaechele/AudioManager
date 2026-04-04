using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace AudioManager.Runtime
{
    /// <summary>
    /// <b>AudioManager</b> is the central orchestrator for all runtime audio.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Load <see cref="AudioTrackData"/> and <see cref="AudioPlaylistData"/> from
    /// <c>Resources/Audio/</c> and an optional external folder on disk.</item>
    /// <item>Route playback to <see cref="MusicController"/>, <see cref="AmbientController"/>,
    /// and <see cref="SfxController"/>.</item>
    /// <item>Persist and restore per-channel volume settings via PlayerPrefs.</item>
    /// <item>Expose delegate hooks consumed by <c>CutsceneAudioBridge</c> and
    /// <c>MapLoaderAudioBridge</c>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add to a persistent manager GameObject. Attach (or let auto-resolve)
    /// <see cref="MusicController"/>, <see cref="AmbientController"/>, and <see cref="SfxController"/>.
    /// Place track/playlist JSON files in <c>Assets/Resources/Audio/</c>.
    /// </para>
    /// </summary>
    [AddComponentMenu("AudioManager/Audio Manager")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class AudioManager : SerializedMonoBehaviour
#else
    public class AudioManager : MonoBehaviour
#endif
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("Sub-controllers (auto-resolved if not assigned)")]
        [SerializeField] private MusicController   musicController;
        [SerializeField] private AmbientController ambientController;
        [SerializeField] private SfxController     sfxController;

        [Header("Loaded tracks (read-only)")]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [SerializeField] private List<string> loadedTrackIds = new();

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>Fired when a music track starts. Parameter: track id (or resource path if anonymous).</summary>
        public event Action<string> OnMusicStarted;
        /// <summary>Fired when music stops.</summary>
        public event Action OnMusicStopped;
        /// <summary>Fired when a playlist advances to a new track. Parameter: track id.</summary>
        public event Action<string> OnTrackChanged;

        // -------------------------------------------------------------------------
        // Delegate hooks for bridge components
        // -------------------------------------------------------------------------

        /// <summary>
        /// Optional callback invoked by <see cref="PlayMusic(string, float)"/> so that bridge
        /// components can intercept or augment music playback.
        /// Signature: (resourcePath, loop, fadeDuration).
        /// If set, the default AudioSource playback is bypassed.
        /// </summary>
        public Action<string, bool, float> PlayMusicOverride;

        /// <summary>
        /// Optional callback invoked by <see cref="StopMusic(float)"/>.
        /// If set, the default stop behaviour is bypassed.
        /// </summary>
        public Action<float> StopMusicOverride;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------

        private readonly Dictionary<string, AudioTrackData>    _tracks    = new();
        private readonly Dictionary<string, AudioPlaylistData> _playlists = new();
        private AudioSettings _settings = new();

        /// <summary>Current persistent volume/mute settings.</summary>
        public AudioSettings Settings => _settings;

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (musicController   == null) musicController   = GetComponent<MusicController>()   ?? gameObject.AddComponent<MusicController>();
            if (ambientController == null) ambientController = GetComponent<AmbientController>() ?? gameObject.AddComponent<AmbientController>();
            if (sfxController     == null) sfxController     = GetComponent<SfxController>()     ?? gameObject.AddComponent<SfxController>();

            _settings.Load();
            ApplyAllVolumes();
            LoadAllTracks();
        }

        private void OnApplicationQuit() => _settings.Save();

        // -------------------------------------------------------------------------
        // Track loading
        // -------------------------------------------------------------------------

        /// <summary>
        /// Reloads all track and playlist JSON files from <c>Resources/Audio/</c> and the
        /// optional external <c>Audio/</c> folder (<c>persistentDataPath/Audio/</c>).
        /// </summary>
        public void LoadAllTracks()
        {
            _tracks.Clear();
            _playlists.Clear();
            loadedTrackIds.Clear();

            // Load from Resources/Audio
            var trackAssets    = Resources.LoadAll<TextAsset>("Audio/Tracks");
            var playlistAssets = Resources.LoadAll<TextAsset>("Audio/Playlists");

            foreach (var asset in trackAssets)    RegisterTrackJson(asset.text);
            foreach (var asset in playlistAssets) RegisterPlaylistJson(asset.text);

            // Load from persistentDataPath/Audio (mods / runtime overrides)
            string externalDir = Path.Combine(Application.persistentDataPath, "Audio");
            if (Directory.Exists(externalDir))
            {
                foreach (var file in Directory.GetFiles(externalDir, "*.json", SearchOption.AllDirectories))
                {
                    try { RegisterTrackJson(File.ReadAllText(file)); }
                    catch (Exception e) { Debug.LogWarning($"[AudioManager] Failed to parse {file}: {e.Message}"); }
                }
            }

            loadedTrackIds.AddRange(_tracks.Keys);
            Debug.Log($"[AudioManager] Loaded {_tracks.Count} tracks, {_playlists.Count} playlists.");
        }

        // -------------------------------------------------------------------------
        // Music API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Play a music track by resource path (or track id if a matching track is registered).
        /// Crossfades from the currently playing track if one is active.
        /// </summary>
        /// <param name="resourceOrId">Resources-relative path or registered track id.</param>
        /// <param name="fadeDuration">Crossfade seconds. -1 uses the track's configured value or default.</param>
        public void PlayMusic(string resourceOrId, float fadeDuration = -1f)
        {
            if (string.IsNullOrEmpty(resourceOrId)) return;

            if (PlayMusicOverride != null)
            {
                PlayMusicOverride(resourceOrId, true, fadeDuration);
                return;
            }

            AudioTrackData track = ResolveTrack(resourceOrId);
            float fade = fadeDuration >= 0f ? fadeDuration
                       : (track != null ? track.fadeInDuration : 1.5f);

            AudioClip clip = LoadClip(track?.resource ?? resourceOrId);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] Music clip not found: {resourceOrId}");
                return;
            }

            float vol = track?.volume ?? 1f;
            musicController.ApplyVolume(_settings.EffectiveVolume(AudioChannelType.Music));
            musicController.CrossfadeTo(clip, track?.loop ?? true, vol, fade);
            OnMusicStarted?.Invoke(track?.id ?? resourceOrId);
        }

        /// <summary>Fade out and stop the currently playing music.</summary>
        public void StopMusic(float fadeDuration = -1f)
        {
            if (StopMusicOverride != null) { StopMusicOverride(fadeDuration); return; }
            musicController.Stop(fadeDuration);
            OnMusicStopped?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Ambient API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Start playing one or more ambient layers by resource path or track id.
        /// Any previously playing ambients are stopped first.
        /// </summary>
        public void PlayAmbient(string[] resourcesOrIds, float fadeDuration = -1f)
        {
            if (resourcesOrIds == null || resourcesOrIds.Length == 0) { StopAmbient(fadeDuration); return; }

            ambientController.ApplyVolume(_settings.EffectiveVolume(AudioChannelType.Ambient));

            var clips = new List<AudioClip>();
            foreach (var rid in resourcesOrIds)
            {
                AudioTrackData t = ResolveTrack(rid);
                AudioClip clip = LoadClip(t?.resource ?? rid);
                if (clip != null) clips.Add(clip);
                else Debug.LogWarning($"[AudioManager] Ambient clip not found: {rid}");
            }
            ambientController.Play(clips.ToArray(), fadeDuration);
        }

        /// <summary>Stop all ambient layers.</summary>
        public void StopAmbient(float fadeDuration = -1f) => ambientController.Stop(fadeDuration);

        // -------------------------------------------------------------------------
        // SFX API
        // -------------------------------------------------------------------------

        /// <summary>Play a one-shot SFX by resource path or track id.</summary>
        public void PlaySfx(string resourceOrId, float volume = 1f, float pitch = 1f)
        {
            AudioTrackData track = ResolveTrack(resourceOrId);
            string path = track?.resource ?? resourceOrId;
            AudioClip clip = LoadClip(path);
            if (clip == null) { Debug.LogWarning($"[AudioManager] SFX clip not found: {resourceOrId}"); return; }
            sfxController.ApplyVolume(_settings.EffectiveVolume(AudioChannelType.Sfx));
            sfxController.Play(clip, (track?.volume ?? 1f) * volume, pitch);
        }

        /// <summary>Play a one-shot SFX at a world position.</summary>
        public void PlaySfxAt(string resourceOrId, Vector3 position, float volume = 1f)
        {
            AudioTrackData track = ResolveTrack(resourceOrId);
            string path = track?.resource ?? resourceOrId;
            AudioClip clip = LoadClip(path);
            if (clip == null) { Debug.LogWarning($"[AudioManager] SFX clip not found: {resourceOrId}"); return; }
            sfxController.ApplyVolume(_settings.EffectiveVolume(AudioChannelType.Sfx));
            sfxController.PlayAt(clip, position, (track?.volume ?? 1f) * volume);
        }

        // -------------------------------------------------------------------------
        // Volume API
        // -------------------------------------------------------------------------

        /// <summary>Set the volume (0–1) for a given channel and persist to PlayerPrefs.</summary>
        public void SetVolume(AudioChannelType channel, float volume)
        {
            volume = Mathf.Clamp01(volume);
            switch (channel)
            {
                case AudioChannelType.Music:   _settings.musicVolume   = volume; break;
                case AudioChannelType.Ambient: _settings.ambientVolume = volume; break;
                case AudioChannelType.Sfx:     _settings.sfxVolume     = volume; break;
                case AudioChannelType.Voice:   _settings.voiceVolume   = volume; break;
            }
            ApplyAllVolumes();
            _settings.Save();
        }

        /// <summary>Set the master volume (0–1) and persist to PlayerPrefs.</summary>
        public void SetMasterVolume(float volume)
        {
            _settings.masterVolume = Mathf.Clamp01(volume);
            ApplyAllVolumes();
            _settings.Save();
        }

        /// <summary>Mute or unmute a channel.</summary>
        public void SetMute(AudioChannelType channel, bool muted)
        {
            switch (channel)
            {
                case AudioChannelType.Music:   _settings.musicMuted   = muted; break;
                case AudioChannelType.Ambient: _settings.ambientMuted = muted; break;
                case AudioChannelType.Sfx:     _settings.sfxMuted     = muted; break;
                case AudioChannelType.Voice:   _settings.voiceMuted   = muted; break;
            }
            ApplyAllVolumes();
            _settings.Save();
        }

        /// <summary>Return the current effective (scaled) volume for a channel.</summary>
        public float GetVolume(AudioChannelType channel) => _settings.EffectiveVolume(channel);

        // -------------------------------------------------------------------------
        // Track / Playlist query
        // -------------------------------------------------------------------------

        /// <summary>Return all registered track IDs.</summary>
        public IReadOnlyList<string> GetTrackIds() => loadedTrackIds;

        /// <summary>Return the <see cref="AudioTrackData"/> for a given id, or null.</summary>
        public AudioTrackData GetTrack(string id) => _tracks.TryGetValue(id, out var t) ? t : null;

        /// <summary>Return the <see cref="AudioPlaylistData"/> for a given id, or null.</summary>
        public AudioPlaylistData GetPlaylist(string id) => _playlists.TryGetValue(id, out var p) ? p : null;

        // -------------------------------------------------------------------------
        // Internal helpers
        // -------------------------------------------------------------------------

        private void ApplyAllVolumes()
        {
            musicController?.ApplyVolume(_settings.EffectiveVolume(AudioChannelType.Music));
            ambientController?.ApplyVolume(_settings.EffectiveVolume(AudioChannelType.Ambient));
            sfxController?.ApplyVolume(_settings.EffectiveVolume(AudioChannelType.Sfx));
        }

        private void RegisterTrackJson(string json)
        {
            try
            {
                var track = JsonUtility.FromJson<AudioTrackData>(json);
                if (track != null && !string.IsNullOrEmpty(track.id))
                {
                    track.rawJson = json;
                    _tracks[track.id] = track;
                }
            }
            catch (Exception e) { Debug.LogWarning($"[AudioManager] Failed to parse track JSON: {e.Message}"); }
        }

        private void RegisterPlaylistJson(string json)
        {
            try
            {
                var playlist = JsonUtility.FromJson<AudioPlaylistData>(json);
                if (playlist != null && !string.IsNullOrEmpty(playlist.id))
                    _playlists[playlist.id] = playlist;
            }
            catch (Exception e) { Debug.LogWarning($"[AudioManager] Failed to parse playlist JSON: {e.Message}"); }
        }

        private AudioTrackData ResolveTrack(string idOrResource)
        {
            return _tracks.TryGetValue(idOrResource, out var t) ? t : null;
        }

        private static AudioClip LoadClip(string resource)
        {
            if (string.IsNullOrEmpty(resource)) return null;
            return Resources.Load<AudioClip>(resource);
        }
    }
}
