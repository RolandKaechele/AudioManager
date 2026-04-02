#if AUDIOMANAGER_DM
using UnityEngine;
using DialogueManager.Runtime;

namespace AudioManager.Runtime
{
    /// <summary>
    /// Optional bridge between AudioManager and DialogueManager.
    /// Enable define <c>AUDIOMANAGER_DM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Hooks <c>DialogueManager.PlayAudioCallback</c> so that per-node audio resources
    /// (voice lines, sound effects) are routed through AudioManager's channel system
    /// instead of requiring a separate AudioSource on the dialogue GameObject.
    /// </para>
    /// </summary>
    [AddComponentMenu("AudioManager/Dialogue Audio Bridge")]
    [DisallowMultipleComponent]
    public class DialogueAudioBridge : MonoBehaviour
    {
        [Tooltip("Channel used for dialogue node audio. Voice for voice lines, Sfx for sound effects.")]
        [SerializeField] private AudioChannelType dialogueAudioChannel = AudioChannelType.Voice;

        private AudioManager _audio;
        private DialogueManager.Runtime.DialogueManager _dialogue;

        private void Awake()
        {
            _audio    = GetComponent<AudioManager>() ?? FindFirstObjectByType<AudioManager>();
            _dialogue = GetComponent<DialogueManager.Runtime.DialogueManager>()
                        ?? FindFirstObjectByType<DialogueManager.Runtime.DialogueManager>();

            if (_audio == null)
            {
                Debug.LogWarning("[DialogueAudioBridge] AudioManager not found in scene.");
                return;
            }
            if (_dialogue == null)
            {
                Debug.LogWarning("[DialogueAudioBridge] DialogueManager not found in scene.");
                return;
            }

            _dialogue.PlayAudioCallback = HandlePlayAudio;
            Debug.Log("[DialogueAudioBridge] Hooked AudioManager into DialogueManager.PlayAudioCallback.");
        }

        private void OnDestroy()
        {
            if (_dialogue != null &&
                _dialogue.PlayAudioCallback == (System.Action<string, bool>)HandlePlayAudio)
                _dialogue.PlayAudioCallback = null;
        }

        private void HandlePlayAudio(string resourceOrId, bool loop)
        {
            if (_audio == null || string.IsNullOrEmpty(resourceOrId)) return;

            switch (dialogueAudioChannel)
            {
                case AudioChannelType.Voice:
                    _audio.PlaySfx(resourceOrId);
                    break;
                case AudioChannelType.Sfx:
                    _audio.PlaySfx(resourceOrId);
                    break;
                case AudioChannelType.Music:
                    _audio.PlayMusic(resourceOrId);
                    break;
                default:
                    _audio.PlaySfx(resourceOrId);
                    break;
            }
        }
    }
}
#else
// AUDIOMANAGER_DM not defined — bridge is inactive.
namespace AudioManager.Runtime
{
    /// <summary>No-op stub. Enable AUDIOMANAGER_DM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("AudioManager/Dialogue Audio Bridge")]
    public class DialogueAudioBridge : UnityEngine.MonoBehaviour
    {
        private void Awake()
        {
            UnityEngine.Debug.Log("[DialogueAudioBridge] DialogueManager integration is disabled. " +
                                  "Add the scripting define AUDIOMANAGER_DM to enable it.");
        }
    }
}
#endif
