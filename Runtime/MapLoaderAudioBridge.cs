using System.Collections.Generic;
using UnityEngine;

namespace AudioManager.Runtime
{
    /// <summary>
    /// <b>MapLoaderAudioBridge</b> connects AudioManager to MapLoaderFramework without creating a
    /// hard compile-time dependency in either package.
    /// <para>
    /// When <c>AUDIOMANAGER_MLF</c> is defined:
    /// <list type="bullet">
    /// <item>Subscribes to <c>MapLoaderFramework.OnChapterChanged</c> and automatically crossfades
    /// to the new chapter's background music and ambient layers as defined in each
    /// <c>MapData.audio</c> field.</item>
    /// <item>Exposes <see cref="PlayAudioForMap"/> so other systems can feed a MapData manually.</item>
    /// </list>
    /// </para>
    /// <para>Without the scripting symbol this component compiles as a no-op stub.</para>
    /// </summary>
    [AddComponentMenu("AudioManager/Map Loader Audio Bridge")]
    [DisallowMultipleComponent]
    public class MapLoaderAudioBridge : MonoBehaviour
    {
#if AUDIOMANAGER_MLF
        private AudioManager _audio;
        private MapLoaderFramework.Runtime.MapLoaderFramework _framework;

        [Tooltip("Crossfade duration in seconds when the chapter changes music.")]
        [SerializeField] private float musicCrossfadeDuration = 1.5f;

        [Tooltip("If true, stop ambient sounds when the chapter has no ambient data.")]
        [SerializeField] private bool stopAmbientOnNoData = true;

        private void Awake()
        {
            _audio      = GetComponent<AudioManager>() ?? FindObjectOfType<AudioManager>();
            _framework  = GetComponent<MapLoaderFramework.Runtime.MapLoaderFramework>()
                          ?? FindObjectOfType<MapLoaderFramework.Runtime.MapLoaderFramework>();

            if (_audio == null)
            {
                Debug.LogWarning("[MapLoaderAudioBridge] AudioManager not found in scene.");
                return;
            }

            if (_framework != null)
            {
                // OnMapLoaded covers all load paths: LoadMap(), LoadChapter() and warp events.
                _framework.OnMapLoaded += OnMapLoaded;
            }
            else
            {
                Debug.LogWarning("[MapLoaderAudioBridge] MapLoaderFramework not found — map audio automation disabled.");
            }
        }

        private void OnDestroy()
        {
            if (_framework != null)
                _framework.OnMapLoaded -= OnMapLoaded;
        }

        // -------------------------------------------------------------------------
        // Map loaded handler
        // -------------------------------------------------------------------------

        private void OnMapLoaded(MapLoaderFramework.Runtime.MapData mapData)
        {
            if (_audio == null || mapData == null) return;
            PlayAudioForMap(mapData);
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Crossfade to the background music and ambient layers defined in <paramref name="mapData"/>.
        /// Call this directly when you want to drive audio from a specific map rather than relying
        /// on the chapter-change event.
        /// </summary>
        public void PlayAudioForMap(MapLoaderFramework.Runtime.MapData mapData)
        {
            if (_audio == null || mapData == null) return;

            // Background music — prefer audio.backgroundMusic, fall back to legacy music string
            string bgm = mapData.audio?.backgroundMusic;
            if (string.IsNullOrEmpty(bgm)) bgm = mapData.music;
            if (!string.IsNullOrEmpty(bgm))
                _audio.PlayMusic(bgm, musicCrossfadeDuration);

            // Ambient layers
            if (mapData.audio?.ambientSounds != null && mapData.audio.ambientSounds.Count > 0)
            {
                _audio.PlayAmbient(mapData.audio.ambientSounds.ToArray());
            }
            else if (stopAmbientOnNoData)
            {
                _audio.StopAmbient();
            }
        }

        /// <summary>Immediately stop all music and ambient layers.</summary>
        public void StopAll()
        {
            _audio?.StopMusic();
            _audio?.StopAmbient();
        }

#else
        // No-op stub when AUDIOMANAGER_MLF is not defined

        private void Awake()
        {
            Debug.Log("[MapLoaderAudioBridge] MapLoaderFramework integration is disabled. " +
                      "Add the scripting define AUDIOMANAGER_MLF to enable it.");
        }

        public void PlayAudioForMap(object mapData)
        {
            Debug.LogWarning("[MapLoaderAudioBridge] PlayAudioForMap called but AUDIOMANAGER_MLF is not defined.");
        }

        public void StopAll()
        {
            Debug.LogWarning("[MapLoaderAudioBridge] StopAll called but AUDIOMANAGER_MLF is not defined.");
        }
#endif
    }
}
