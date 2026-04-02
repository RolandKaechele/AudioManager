using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioManager.Runtime
{
    // -------------------------------------------------------------------------
    // Channel type
    // -------------------------------------------------------------------------

    /// <summary>
    /// Logical audio channel categories used for independent volume control.
    /// </summary>
    public enum AudioChannelType
    {
        /// <summary>Background music tracks.</summary>
        Music,
        /// <summary>Looping ambient/environmental layers.</summary>
        Ambient,
        /// <summary>Short one-shot sound effects.</summary>
        Sfx,
        /// <summary>Voiced dialogue lines.</summary>
        Voice
    }

    // -------------------------------------------------------------------------
    // AudioTrackData
    // -------------------------------------------------------------------------

    /// <summary>
    /// Describes a single audio track that can be loaded and played at runtime.
    /// Authored in JSON and stored in <c>Resources/Audio/</c>.
    /// </summary>
    [Serializable]
    public class AudioTrackData
    {
        /// <summary>Unique identifier referenced by playlists and event scripts.</summary>
        public string id;

        /// <summary>
        /// Path to the <see cref="AudioClip"/> relative to a <c>Resources/</c> folder (without extension).
        /// </summary>
        public string resource;

        /// <summary>Playback volume multiplier (0–1).</summary>
        public float volume = 1f;

        /// <summary>Pitch multiplier. 1 = normal.</summary>
        public float pitch = 1f;

        /// <summary>Whether the track loops.</summary>
        public bool loop = true;

        /// <summary>Target <see cref="AudioChannelType"/> for routing and volume control.</summary>
        public AudioChannelType channel = AudioChannelType.Music;

        /// <summary>Duration in seconds of the fade-in. 0 = instant.</summary>
        public float fadeInDuration = 0f;

        /// <summary>Duration in seconds of the fade-out. 0 = instant.</summary>
        public float fadeOutDuration = 0f;

        /// <summary>Optional human-readable label (used in the Inspector and Editor).</summary>
        public string label;

        /// <summary>Raw JSON stored during deserialisation (non-serialised).</summary>
        [NonSerialized] public string rawJson;
    }

    // -------------------------------------------------------------------------
    // AudioPlaylistData
    // -------------------------------------------------------------------------

    /// <summary>
    /// An ordered or shuffled list of <see cref="AudioTrackData"/> IDs played in sequence.
    /// </summary>
    [Serializable]
    public class AudioPlaylistData
    {
        /// <summary>Unique identifier for this playlist.</summary>
        public string id;

        /// <summary>Human-readable name shown in Editor UI.</summary>
        public string label;

        /// <summary>Ordered list of <see cref="AudioTrackData"/> IDs.</summary>
        public List<string> trackIds = new();

        /// <summary>Randomise playback order each time the playlist starts.</summary>
        public bool shuffle = false;

        /// <summary>Restart from the beginning after the last track ends.</summary>
        public bool loop = true;

        /// <summary>Crossfade duration in seconds between consecutive tracks.</summary>
        public float crossfadeDuration = 1.5f;
    }

    // -------------------------------------------------------------------------
    // AudioSettings
    // -------------------------------------------------------------------------

    /// <summary>
    /// Persisted volume and mute settings for each <see cref="AudioChannelType"/>.
    /// Saved to and loaded from PlayerPrefs automatically by <see cref="AudioManager"/>.
    /// </summary>
    [Serializable]
    public class AudioSettings
    {
        public float masterVolume = 1f;
        public float musicVolume  = 1f;
        public float ambientVolume = 0.6f;
        public float sfxVolume    = 1f;
        public float voiceVolume  = 1f;

        public bool masterMuted  = false;
        public bool musicMuted   = false;
        public bool ambientMuted = false;
        public bool sfxMuted     = false;
        public bool voiceMuted   = false;

        // PlayerPrefs keys
        internal const string KeyPrefix = "AM_";

        /// <summary>Save all values to PlayerPrefs.</summary>
        public void Save()
        {
            PlayerPrefs.SetFloat(KeyPrefix + "master",  masterVolume);
            PlayerPrefs.SetFloat(KeyPrefix + "music",   musicVolume);
            PlayerPrefs.SetFloat(KeyPrefix + "ambient", ambientVolume);
            PlayerPrefs.SetFloat(KeyPrefix + "sfx",     sfxVolume);
            PlayerPrefs.SetFloat(KeyPrefix + "voice",   voiceVolume);
            PlayerPrefs.SetInt(KeyPrefix + "masterMute",  masterMuted  ? 1 : 0);
            PlayerPrefs.SetInt(KeyPrefix + "musicMute",   musicMuted   ? 1 : 0);
            PlayerPrefs.SetInt(KeyPrefix + "ambientMute", ambientMuted ? 1 : 0);
            PlayerPrefs.SetInt(KeyPrefix + "sfxMute",     sfxMuted     ? 1 : 0);
            PlayerPrefs.SetInt(KeyPrefix + "voiceMute",   voiceMuted   ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Load all values from PlayerPrefs (uses current values as defaults).</summary>
        public void Load()
        {
            masterVolume  = PlayerPrefs.GetFloat(KeyPrefix + "master",  masterVolume);
            musicVolume   = PlayerPrefs.GetFloat(KeyPrefix + "music",   musicVolume);
            ambientVolume = PlayerPrefs.GetFloat(KeyPrefix + "ambient", ambientVolume);
            sfxVolume     = PlayerPrefs.GetFloat(KeyPrefix + "sfx",     sfxVolume);
            voiceVolume   = PlayerPrefs.GetFloat(KeyPrefix + "voice",   voiceVolume);
            masterMuted  = PlayerPrefs.GetInt(KeyPrefix + "masterMute",  0) == 1;
            musicMuted   = PlayerPrefs.GetInt(KeyPrefix + "musicMute",   0) == 1;
            ambientMuted = PlayerPrefs.GetInt(KeyPrefix + "ambientMute", 0) == 1;
            sfxMuted     = PlayerPrefs.GetInt(KeyPrefix + "sfxMute",     0) == 1;
            voiceMuted   = PlayerPrefs.GetInt(KeyPrefix + "voiceMute",   0) == 1;
        }

        /// <summary>Returns the effective linear volume for a given channel (master × channel, 0 when muted).</summary>
        public float EffectiveVolume(AudioChannelType channel)
        {
            if (masterMuted) return 0f;
            float ch = channel switch
            {
                AudioChannelType.Music   => musicMuted   ? 0f : musicVolume,
                AudioChannelType.Ambient => ambientMuted ? 0f : ambientVolume,
                AudioChannelType.Sfx     => sfxMuted     ? 0f : sfxVolume,
                AudioChannelType.Voice   => voiceMuted   ? 0f : voiceVolume,
                _                        => 1f
            };
            return masterVolume * ch;
        }
    }
}
