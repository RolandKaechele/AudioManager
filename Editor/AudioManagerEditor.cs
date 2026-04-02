#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AudioManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="AudioManager.Runtime.AudioManager"/>.
    /// Adds runtime test controls and a volume slider panel.
    /// </summary>
    [CustomEditor(typeof(AudioManager.Runtime.AudioManager))]
    public class AudioManagerEditor : UnityEditor.Editor
    {
        private string _testTrackId = "";
        private string _testSfxId  = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mgr = (AudioManager.Runtime.AudioManager)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use runtime controls.", MessageType.Info);
                return;
            }

            // Volume sliders
            EditorGUILayout.LabelField("Volume", EditorStyles.miniBoldLabel);
            DrawVolumeSlider(mgr, AudioManager.Runtime.AudioChannelType.Music,   "Music");
            DrawVolumeSlider(mgr, AudioManager.Runtime.AudioChannelType.Ambient, "Ambient");
            DrawVolumeSlider(mgr, AudioManager.Runtime.AudioChannelType.Sfx,     "SFX");
            DrawVolumeSlider(mgr, AudioManager.Runtime.AudioChannelType.Voice,   "Voice");

            EditorGUILayout.Space(4);

            // Test music
            EditorGUILayout.LabelField("Test Music", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _testTrackId = EditorGUILayout.TextField("Track ID / Resource", _testTrackId);
            if (GUILayout.Button("Play", GUILayout.Width(60)))
                mgr.PlayMusic(_testTrackId);
            if (GUILayout.Button("Stop", GUILayout.Width(60)))
                mgr.StopMusic();
            EditorGUILayout.EndHorizontal();

            // Test SFX
            EditorGUILayout.LabelField("Test SFX", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _testSfxId = EditorGUILayout.TextField("SFX ID / Resource", _testSfxId);
            if (GUILayout.Button("Play", GUILayout.Width(60)))
                mgr.PlaySfx(_testSfxId);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Reload tracks
            if (GUILayout.Button("Reload All Tracks"))
                mgr.LoadAllTracks();
        }

        private void DrawVolumeSlider(AudioManager.Runtime.AudioManager mgr,
                                      AudioManager.Runtime.AudioChannelType channel,
                                      string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(60));
            float current = mgr.GetVolume(channel);
            float next = EditorGUILayout.Slider(current, 0f, 1f);
            if (!Mathf.Approximately(current, next))
                mgr.SetVolume(channel, next);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
