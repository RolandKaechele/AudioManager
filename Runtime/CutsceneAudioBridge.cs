#if AUDIOMANAGER_CSM
using CutsceneManager.Runtime;
using UnityEngine;

namespace AudioManager.Runtime
{
    /// <summary>
    /// <b>CutsceneAudioBridge</b> connects AudioManager to CutsceneManager without creating a
    /// hard compile-time dependency in either package.
    /// <para>
    /// When <c>AUDIOMANAGER_CSM</c> is defined:
    /// <list type="bullet">
    /// <item>Hooks <c>CutsceneManager.PlayAudioCallback</c> so that <c>PlayAudio</c> cutscene steps
    /// use <see cref="AudioManager"/> for channel-routed, volume-managed playback instead of a raw
    /// <see cref="AudioSource"/>.</item>
    /// <item>Hooks <c>CutsceneManager.StopAudioCallback</c> so that <c>StopAudio</c> cutscene steps
    /// stop the appropriate channel.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("AudioManager/Cutscene Audio Bridge")]
    [DisallowMultipleComponent]
    public class CutsceneAudioBridge : MonoBehaviour
    {
        private AudioManager _audio;
        private CutsceneManager _cutscene;

        [Tooltip("Channel to use for clips played via CutsceneManager PlayAudio steps.")]
        [SerializeField] private AudioChannelType cutsceneAudioChannel = AudioChannelType.Music;

        [Tooltip("If true, crossfade into the cutscene track; if false, play immediately.")]
        [SerializeField] private bool useCrossfadeForMusic = true;

        [SerializeField] private float crossfadeDuration = 0.8f;

        private void Awake()
        {
            _audio    = GetComponent<AudioManager>() ?? FindObjectOfType<AudioManager>();
            _cutscene = GetComponent<CutsceneManager>() ?? FindObjectOfType<CutsceneManager>();

            if (_audio == null)
            {
                Debug.LogWarning("[CutsceneAudioBridge] AudioManager not found in scene.");
                return;
            }

            if (_cutscene == null)
            {
                Debug.LogWarning("[CutsceneAudioBridge] CutsceneManager not found in scene.");
                return;
            }

            // Hook the delegate callbacks on CutsceneManager
            _cutscene.PlayAudioCallback = HandlePlayAudio;
            _cutscene.StopAudioCallback = HandleStopAudio;

            Debug.Log("[CutsceneAudioBridge] Hooked AudioManager into CutsceneManager audio callbacks.");
        }

        private void OnDestroy()
        {
            if (_cutscene == null) return;
            if (_cutscene.PlayAudioCallback == (System.Action<string, bool>)HandlePlayAudio)
                _cutscene.PlayAudioCallback = null;
            if (_cutscene.StopAudioCallback == (System.Action)HandleStopAudio)
                _cutscene.StopAudioCallback = null;
        }

        // -------------------------------------------------------------------------
        // Handlers
        // -------------------------------------------------------------------------

        private void HandlePlayAudio(string resourceOrId, bool loop)
        {
            if (cutsceneAudioChannel == AudioChannelType.Music)
            {
                float fade = useCrossfadeForMusic ? crossfadeDuration : 0f;
                _audio.PlayMusic(resourceOrId, fade);
            }
            else if (cutsceneAudioChannel == AudioChannelType.Ambient)
            {
                _audio.PlayAmbient(new[] { resourceOrId });
            }
            else
            {
                _audio.PlaySfx(resourceOrId);
            }
        }

        private void HandleStopAudio()
        {
            if (cutsceneAudioChannel == AudioChannelType.Music)
                _audio.StopMusic();
            else if (cutsceneAudioChannel == AudioChannelType.Ambient)
                _audio.StopAmbient();
        }
    }
}
#endif
