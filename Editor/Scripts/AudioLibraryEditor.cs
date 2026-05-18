using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditor.Compilation;
using System.Collections.Generic;

namespace AudioManagement
{
    /// <summary>
    /// Custom editor for the AudioLibrary ScriptableObject.
    /// Draws a button in the inspector on the AudioLibrary ScriptableObject for re-generating the enum that represents the audio entries in the library.
    /// </summary>
    [CustomEditor(typeof(AudioLibrary))]
    public class AudioLibraryEditor : Editor
    {
        string compilationMessage = "";
        AudioLibrary library;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);
            library = (AudioLibrary)target;

            // Adds the library to the enum that represents the audio categories. 
            if (GUILayout.Button($"Add Library to Category Enum"))
            {
                AddLibraryToCategories();
            }

            // Re-generates the enum based on the current entries in the audio data list on the library.
            if (GUILayout.Button($"Re-Gererate Library Enum"))
            {
                ReGenerateLibraryEnum(library, library.Name);
            }
        }

        /// <summary>
        /// Adds the library to the enum that represents the audio categories. 
        /// This is done by creating a new enum file if it doesn't exist, or by adding a new value to the existing enum file if it does exist. 
        /// The enum value is based on the name of the library asset, and is sanitized to be safe for use as an enum value (spaces are replaced with underscores, special characters are removed). 
        /// This allows for easy referencing of the library in code by using the enum value. 
        /// The enum file is created in the "Packages/com.akaidus.audiomanagement/Runtime/Scripts/Enums" folder, and is named after the type of audio category (e.g. "SoundEffect.cs" for a sound effect library). 
        /// After adding the library to the enum, the method will trigger a recompilation of the scripts to ensure that the new enum value is recognized in code and assigns the enum value to the library.
        /// </summary>
        public void AddLibraryToCategories()
        {
            if (string.IsNullOrWhiteSpace(library.Name))
            {
                Debug.LogWarning($"[Audio Library] Library {library.Name} has an empty name and will not be added to categories.");
                return;
            }

            var libraryName = MakeSafe(library.Name);

            string enumName = library.audioCategory.GetType().Name;
            string path = $"Packages/com.akaidus.audiomanagement/Runtime/Scripts/Enums/{enumName}.cs";

            // Create enum file if it doesn't exist
            if (!File.Exists(path))
            {
                var sb = new StringBuilder();

                sb.AppendLine($"public enum {enumName}");
                sb.AppendLine("{");
                sb.AppendLine($"    {libraryName}");
                sb.AppendLine("}");

                File.WriteAllText(path, sb.ToString());
            }
            else
            {
                string fileContent = File.ReadAllText(path);

                // Prevent duplicates
                if (fileContent.Contains($"{libraryName}"))
                {
                    Debug.Log($"[Audio Library] {libraryName} already exists in {enumName}");
                    return;
                }

                // Insert value before closing brace
                int insertIndex = fileContent.LastIndexOf('}');

                if (insertIndex < 0)
                {
                    Debug.LogError($"Invalid enum file: {path}");
                    return;
                }

                string newEntry = $"    {library.Name},\n";

                fileContent = fileContent.Insert(insertIndex, newEntry);

                File.WriteAllText(path, fileContent);
            }

            library.Name = libraryName;

            string libraryPath = $"Packages/com.akaidus.audiomanagement/Runtime/Scripts/Enums/{libraryName}.cs";

            var libSb = new StringBuilder();
            libSb = libSb.AppendLine($"public enum {libraryName}");
            libSb = libSb.AppendLine("{");
            libSb = libSb.AppendLine($"    None,");
            libSb = libSb.AppendLine("}");

            File.WriteAllText(libraryPath, libSb.ToString());

            AssetDatabase.Refresh();

            CompilationPipeline.compilationFinished += OnCompilationFinishedAfterAddingLibrary;
        }

        /// <summary>
        /// Re-generates the enum that represents the audio entries in the library based on the current entries in the sounds list on the library.
        /// </summary>
        public void ReGenerateLibraryEnum(AudioLibrary library, string libraryName)
        {
            HashSet<string> added = new();
            List<string> entries = new();

            foreach (var sound in library.audioData)
            {
                if (sound == null)
                {
                    Debug.LogWarning($"[Audio Library] Found a null sound in the library, skipping...");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(sound.name))
                {
                    Debug.LogWarning($"[Audio Library] Sound {sound.name} has an empty name, skipping...");
                    continue;
                }

                sound.Name = MakeSafe(sound.Name);

                if (!added.Add(sound.Name))
                    continue;

                entries.Add(sound.Name);
            }
            library.audioData.RemoveAll(s => s == null);

            StringBuilder sb = new();

            sb.AppendLine($"public enum {libraryName}");
            sb.AppendLine("{");

            if (entries.Count == 0)
            {
                sb.AppendLine($"    None,");
            }

            foreach (var e in entries)
            {
                sb.AppendLine($"    {e},");
            }

            sb.AppendLine("}");

            var path = $"Packages/com.akaidus.audiomanagement/Runtime/Scripts/Enums/{libraryName}.cs";

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Could not find path for {libraryName}");
                return;
            }

            File.WriteAllText(path, sb.ToString());

            Debug.Log($"[Audio Library] Library contains: {string.Join(", ", entries)}");

            AssetDatabase.Refresh();

            compilationMessage = entries.Count > 0 ? $"[Audio Library] Done adding sounds {string.Join(", ", entries)} to {libraryName}!" : $"[Audio Library] {libraryName} now has no sounds!";

            CompilationPipeline.compilationFinished += OnCompilationFinishedAfterReGeneratingLibraryEnum;
        }

        /// <summary>
        /// Sets the audio category of the library based on the enum value that matches the name of the library asset.
        /// </summary>
        public void SetCategory()
        {
            Type category = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.IsEnum && t.Name == library.audioCategory.ToString());

            if (Enum.TryParse(category, library.Name, out object value))
            {
                library.audioCategory = (AudioCategory)value;
            }
            else
            {
                Debug.LogWarning($"[Audio Manager] Could not parse enum value from library name {library.Name} in {library.audioCategory.GetType()}.");
            }
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

        void OnCompilationFinishedAfterAddingLibrary(object ctx)
        {
            SetCategory();
            Debug.Log($"[Audio Library] Done adding library {library.Name} to category {library.audioCategory.GetType().Name}!");
            CompilationPipeline.compilationFinished -= OnCompilationFinishedAfterAddingLibrary;
        }

        void OnCompilationFinishedAfterReGeneratingLibraryEnum(object ctx)
        {
            Debug.Log(compilationMessage);
            CompilationPipeline.compilationFinished -= OnCompilationFinishedAfterReGeneratingLibraryEnum;
        }
    }
}
