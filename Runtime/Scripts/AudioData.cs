using System;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;

namespace AudioManagement
{
    /// <summary>
    /// ScriptableObject for storing audio data. 
    /// This allows for easy management of audio clips in the editor, and also allows for easy referencing of audio clips in code.
    /// </summary>
    [CreateAssetMenu(fileName = "_NewAudioData", menuName = "Audio Management/Audio Data", order = -1000)]
    public class AudioData : ScriptableObject
    {
        [Tooltip("The name of the audio entry. This is used to added an enum value that represents this audio entry. Enum values cannot contain unsafe characters and will be sanitized.")] public string Name;
        [Tooltip("The audio clips associated with this audio entry. If multiple clips are provided, one will be randomly selected when the audio is played. If a new clip is added after the Audio Library enum is re-generated, it will be automatically included in the selection.")]  public AudioClip[] audioClips;
        [Tooltip("The volume of the audio.")] [Range(0, 1)] public float volume;
        [HideInInspector] [Tooltip("The enum value that represents this audio entry, based on the Name variable.")] public Enum enumValue;

#if UNITY_EDITOR
        void OnValidate()
        {
            // This will rename the asset to match the SfxType enum value.
            // This is just for organization purposes in the editor, and does not affect the functionality of the asset in any way.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    UnityEditor.AssetDatabase.RenameAsset(UnityEditor.AssetDatabase.GetAssetPath(this), MakeSafe(Name));
                }
            };
        }

        /// <summary>
        /// Takes a string and sanitizes it to make it safe to use as an enum name by replacing spaces with underscores and removing any non-alphanumeric characters (except for underscores).
        /// </summary>
        string MakeSafe(string input)
        {
            input = input.Replace(" ", "_");
            Regex regex = new("[^a-zA-Z0-9_]");
            StringBuilder sb = new();

            foreach (var c in input)
            {
                if (regex.IsMatch(c.ToString()))
                    continue;
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
            }

            return sb.ToString();
        }
#endif
    }
}