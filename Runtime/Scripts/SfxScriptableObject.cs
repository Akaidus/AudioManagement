using UnityEngine;
using UnityEditor;

namespace AudioManagement
{
    /// <summary>
    /// ScriptableObject for storing sound effect data. 
    /// This allows for easy management of sound effects in the editor, and also allows for easy referencing of sound effects in code.
    /// </summary>
    [CreateAssetMenu(fileName = "_New AudioData", menuName = "Audio Management/Audio Scriptable Object", order = -1000)]
    public class SfxScriptableObject : ScriptableObject
    {
        [Tooltip("The type of sound effect. Used to reference the sound effect in code")] public SfxType sfxType;
        [Tooltip("The data for the sound effect. Used to store the actual audio clip and volume for the sound effect")] public SfxData data;

#if UNITY_EDITOR
        void OnValidate()
        {
            // This will rename the asset to match the SfxType enum value.
            // This is just for organization purposes in the editor, and does not affect the functionality of the asset in any way.
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(this), this.sfxType.ToString());
            };
        }

#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom editor for the SfxScriptableObject.
    /// Draws buttons in the inspector on the SfxScriptableObject for testing the sound effect while in the editor.
    /// </summary>
    [CustomEditor(typeof(SfxScriptableObject))]
    public class ScriptableObjectEditor : Editor
    {
        AudioSource audioSource;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            SfxScriptableObject sfxScriptableObject = (SfxScriptableObject)target;
            if (GUILayout.Button("Play"))
            {
                AudioManager.EditorTestPlay(sfxScriptableObject.sfxType);
            }
            if (GUILayout.Button("Play Random Pitch and Volume"))
            {
                AudioManager.EditorTestPlayRandomPitchAndVolume(sfxScriptableObject.sfxType);
            }
            if (GUILayout.Button("Play Looping"))
            {
                if (audioSource == null)
                    audioSource = AudioManager.EditorTestPlayLooping(sfxScriptableObject.sfxType);
            }
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
#endif
}