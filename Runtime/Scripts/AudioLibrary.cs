using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AudioManagement
{
    /// <summary>
    /// ScriptableObject for storing a library of audio data. 
    /// This allows for easy management of audio clips in the editor, and also allows for easy referencing of audio clips in code.
    /// Each library is associated with an AudioCategory enum value, which is used to reference the library in code. 
    /// The enum value is automatically generated based on the name of the library asset, but can be manually set if needed. 
    /// The enum value is used to reference the library in code, and the audio entries in the library are represented as enum values in a separate enum that is generated based on the entries in the library.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Audio Management/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Tooltip("The name of the library. This is used to added an enum value that represents this library. Scripts/Enum values cannot contain unsafe characters and will be sanitized.")] public string Name;
        [HideInInspector]public AudioCategory audioCategory;
        [Tooltip("The audio data entries in the library.")] public List<AudioData> audioData = new();

#if UNITY_EDITOR
        void OnValidate()
        {
            // This will rename the asset to match the enum type.
            // This is just for organization purposes in the editor, and does not affect the functionality of the asset in any way.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    UnityEditor.AssetDatabase.RenameAsset(UnityEditor.AssetDatabase.GetAssetPath(this), $"AudioLibrary_{MakeSafe(Name)}");
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
                if(regex.IsMatch(c.ToString()))
                    continue;
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
            }

            return sb.ToString();
        }
#endif
    }
}