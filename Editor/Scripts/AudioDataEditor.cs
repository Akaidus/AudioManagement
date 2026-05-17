using UnityEditor;
using UnityEngine;

namespace AudioManagement
{
    /// <summary>
    /// Custom editor for the SfxScriptableObject.
    /// Draws buttons in the inspector on the SfxScriptableObject for testing the sound effect while in the editor.
    /// </summary>
    [CustomEditor(typeof(AudioData))]
    public class AudioDataEditor : Editor
    {
        AudioSource audioSource;
        [Tooltip("This ONLY applies to audio played through the button here. The amount of pitch variation for the sound effect.")] float pitchVariation = 0f;
        [Tooltip("This ONLY applies to audio played through the button here. The amount of volume variation for the sound effect.")] float volumeVariation = 0f;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            AudioData sfxScriptableObject = (AudioData)target;

            GUILayout.Space(20);

            if (GUILayout.Button("Play"))
            {
                AudioManager.EditorTestPlay(sfxScriptableObject.enumValue);
            }

            GUILayout.Space(30);

            pitchVariation = EditorGUILayout.Slider("Pitch Variation", pitchVariation, 0f, 3f);
            volumeVariation = EditorGUILayout.Slider("Volume Variation", volumeVariation, 0f, 1f);

            if (GUILayout.Button("Play Random Pitch and Volume"))
            {
                AudioManager.EditorTestPlayRandomPitchAndVolume(sfxScriptableObject.enumValue, pitchVariation, volumeVariation);
            }
            
            GUILayout.Space(20);

            if (GUILayout.Button("Play Looping"))
            {
                if (audioSource == null)
                    audioSource = AudioManager.EditorTestPlayLooping(sfxScriptableObject.enumValue);
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Stop Looping"))
            {
                if (audioSource != null)
                {
                    AudioManager.EditorTestStopLooping(audioSource);
                    audioSource = null;
                }
            }
        }
    }
}
