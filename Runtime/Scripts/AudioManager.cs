using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace AudioManagement
{
    /// <summary>
    /// Singleton class for managing audio in the game.
    /// Stores references to audio clips in a dictionary for easy access, and manage a pool of audio sources for playing sound effects.
    /// Various methods for playing sound effects with different options, such as looping, fading, and random pitch/volume variations.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Source Pooling")]
        [SerializeField] PooledAudioSource audioSourcePrefab;
        [SerializeField] int maxAudioSourcesCount = 40;
        [SerializeField] [Tooltip("The interval at which to prune the audio source pool. If 0, pruning is disabled.")] float pruneInterval = 10f;
        Queue<PooledAudioSource> audioSourcePool = new();
        WaitForSeconds waitForSeconds0_1 = new(0.1f);
        WaitForSeconds waitPruneInterval;

        [Header("Audio Library")]
        [SerializeField] AudioLibrary[] audioLibraryScriptableObjects;
        Dictionary<Type, Dictionary<Enum, AudioData>> audioLibraries = new();

        Coroutine pruneRoutine;
        static AudioManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);

            // Clean up any existing audio sources.
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            PopulateAudioLibraries();
            CreateAudioSourcePool();

            if (pruneInterval == 0) return;
            waitPruneInterval = new (pruneInterval);
            pruneRoutine = StartCoroutine(PrunePool());
        }

        /// <summary>
        /// Populates the audioLibraries dictionary by iterating through the assigned AudioLibrary ScriptableObjects, extracting their enum types and associated SfxScriptableObjects, and organizing them into a nested dictionary structure for easy access when playing sound effects.
        /// </summary>
        void PopulateAudioLibraries()
        {
            audioLibraries.Clear();
            if (audioLibraryScriptableObjects == null) return;
            
            foreach (var library in audioLibraryScriptableObjects)
            {
                if (library == null)
                {
                    Debug.LogWarning($"[Audio Manager] Null AudioLibrary ScriptableObject found in AudioManager inspector. Assign a valid AudioLibrary ScriptableObject or remove the entry. " +
                                     $"This could also be a false alert an AudioLibrary ScriptableObject was dragged into the inspector when none existed previously.");
                    continue;
                }

                // Create a dictionary for the current library to store its Audio Entries, using its enum type as the key and the SfxScriptableObject as the value.
                Dictionary<Enum, AudioData> libraryDictionary = new();

                // Find the enum type that matches the library's enumType field by searching through all loaded assemblies and their types.
                Type enumCategory = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.IsEnum && t.Name == library.audioCategory.ToString());
                
                if (enumCategory == null)
                {
                    Debug.LogError($"[Audio Manager] Could not find enum type {library.audioCategory}");
                    continue;
                }

                foreach (var sound in library.audioData)
                {
                    if (sound == null)
                        continue;

                    if (Enum.TryParse(enumCategory, sound.Name, out object value))
                    {
                        sound.enumValue = (Enum)value;
                    }
                    else
                    {
                        Debug.LogWarning($"[Audio Manager] Could not parse enum value from sound name {sound.Name} in library {library.audioCategory}. Make sure the sound name matches an enum value in the library's enum type.");
                        continue;
                    }

                    if (libraryDictionary.ContainsKey((Enum)value))
                    {
                        Debug.LogWarning($"[Audio Manager] Duplicate enumValue {value} in {enumCategory} found in AudioManager inspector. Remove or change the duplicate entry.");
                        continue;
                    }

                    libraryDictionary.Add((Enum)value, sound);
                }
                if(audioLibraries.ContainsKey(enumCategory))
                {
                    Debug.LogWarning($"[Audio Manager] Duplicate category {enumCategory} found in AudioManager inspector. Remove or change the duplicate entry.");
                    continue;
                }
                audioLibraries.Add(enumCategory, libraryDictionary);
                Debug.Log($"[Audio Manager] Library {library.audioCategory} successfully populated with: {string.Join(", ", libraryDictionary.Keys)}");
            }
        }
        

        /// <summary>
        /// Initializes the audio source pool by instantiating a specified number of audio sources from the prefab and adding them to the pool.
        /// </summary>
        void CreateAudioSourcePool()
        {
            for (int i = 0; i < maxAudioSourcesCount; i++)
            {
                var audioSource = Instantiate(audioSourcePrefab, transform);
#if UNITY_EDITOR
                audioSource.gameObject.name = $"AudioSource_{i + 1}";
#endif
                audioSourcePool.Enqueue(audioSource);
            }
        }

        /// <summary>
        /// Periodically checks the audio source pool and destroys any excess audio sources if the pool size exceeds the specified maximum count.
        /// </summary>
        IEnumerator PrunePool()
        {
            while (true)
            {
                yield return waitPruneInterval;
                while (audioSourcePool.Count > maxAudioSourcesCount)
                {
                    var audioSource = audioSourcePool.Dequeue();
                    Destroy(audioSource.gameObject);
                }
            }
        }

        /// <summary>
        /// If there are no available audio sources to play a clip, this method is called to add more audio sources to the pool.
        /// </summary>
        AudioSource AddAudioSource()
        {
            var audioSource = Instantiate(audioSourcePrefab, transform);
#if UNITY_EDITOR
            audioSource.gameObject.name = $"AudioSource_{transform.childCount}";
#endif
            audioSourcePool.Enqueue(audioSource);
            return audioSource.AudioSource;
        }

        /// <summary>
        /// If there are available audio sources in the pool, this method will return one for use. 
        /// If the pool is empty, it will call AddAudioSource to create a new audio source and return it.
        /// </summary>
        PooledAudioSource GetAudioSource()
        {
            if (audioSourcePool.Count == 0)
                AddAudioSource();

            return audioSourcePool.Dequeue();
        }

        /// <summary>
        /// Resets the specified AudioSource to default settings and returns it to the pool for reuse.
        /// </summary>
        IEnumerator ReturnAudioSource(PooledAudioSource pooledAudioSource)
        {
            yield return new WaitWhile(() => pooledAudioSource.AudioSource.isPlaying);
            pooledAudioSource.AudioSource.clip = null;
            pooledAudioSource.AudioSource.loop = false;
            pooledAudioSource.AudioSource.volume = 1f;
            pooledAudioSource.AudioSource.pitch = 1f;
            pooledAudioSource.AudioSource.spatialBlend = 1f;
            pooledAudioSource.AudioSource.transform.SetParent(transform, false);
            pooledAudioSource.StopFollowingTarget();
            audioSourcePool.Enqueue(pooledAudioSource);
        }

        /// <summary>
        /// Try to get a random audio clip for the specified SFX type from the specified audio library.
        /// If there are no clips assigned for that SFX type, it will log a warning and return false. Otherwise, it will return true and output the randomly selected clip.
        /// </summary>
        bool TryGetClip(Enum requestedAudio, out AudioClip clip, out float volume)
        {
            clip = null;
            volume = 0f;

            if(audioSourcePrefab == null)
            {
                Debug.LogWarning($"[Audio Manager] No audio source prefab assigned. Please assign an audio source prefab in the inspector."); 
                return false;
            }

            if(requestedAudio == null || requestedAudio.ToString() == "")
            {
                Debug.LogWarning("[Audio Manager] Requested audio is null or None. Make sure to pass a valid enum value when trying to get a clip, or that a Audio Library has a clip assigned for this audio type.");
                return false;
            }

            Type enumType = requestedAudio.GetType();

            // Find matching library
            if (!audioLibraries.TryGetValue(enumType, out var library))
            {
                Debug.LogWarning($"[Audio Manager] No library found for enum type {enumType.Name}");
                return false;
            }

            // Find sound inside library
            if (!library.TryGetValue(requestedAudio, out var audioData))
            {
                Debug.LogWarning(
                    $"[Audio Manager] No clip found for {requestedAudio}");
                return false;
            }

            if (audioData == null || audioData.audioClips == null || audioData.audioClips.Length == 0)
            {
                Debug.LogWarning($"[Audio Manager] No clips assigned for requested audio: {requestedAudio}");
                return false;
            }

            clip = audioData.audioClips[Random.Range(0, audioData.audioClips.Length)];
            volume = audioData.volume;
            return clip != null;
        }

        /// <summary>
        /// Sets the position of the audio source to the position of the target transform. 
        /// If followTransform is true, it will also set the audio source to follow the target transform's position until StopFollowingTarget is called on the PooledAudioSource.
        /// </summary>
        void SetAudioSourceTransform(PooledAudioSource pooledAudioSource, Transform targetTransform, bool followTransform)
        {
            if (targetTransform == null)
            {
                return;
            }
            pooledAudioSource.transform.position = targetTransform.position;
            if (!followTransform) return;
            pooledAudioSource.FollowTarget(targetTransform);
        }

        /// <summary>
        /// Used to avoid clipping sounds when stopping.
        /// </summary>
        IEnumerator StopSound(AudioSource audioSource)
        {
            if (audioSource == null) yield return null;
            audioSource.volume = 0.0001f; // Avoid clipping sounds when stopping.
            yield return waitForSeconds0_1;
            audioSource.Stop();
            if(audioSource.TryGetComponent<PooledAudioSource>(out var pooledAudioSource))
            {
                instance.StartCoroutine(instance.ReturnAudioSource(pooledAudioSource));
            }
        }

        #region Stop and Pause Methods

        /// <summary>
        /// Used to stop the specified audio source. 
        /// The AudioSource passed in should be the one returned by the initial Play method.
        /// When an audio source is stopped, it is returned to the pool and can be reused for future audio playback requests.
        /// </summary>
        public static void Stop(AudioSource audioSource)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                instance.StartCoroutine(instance.StopSound(audioSource));
            }
        }

        /// <summary>
        /// Used to stop all looping audio of the specified type. 
        /// This will find all currently playing audio sources that are playing the same clip as the requested audio and stop them.
        /// When an audio source is stopped, it is returned to the pool and can be reused for future audio playback requests.
        /// </summary>
        public static void StopAllLoopingOfType(Enum requestedAudio)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;

            var audioSources = instance.audioSourcePool.Where(source => source.AudioSource.clip == clip && source.AudioSource.loop).Select(source => source.AudioSource).ToArray();
            foreach (var source in audioSources)
            {
                instance.StartCoroutine(instance.StopSound(source));
            }
        }

        /// <summary>
        /// Used to stop all currently playing audio sources of the specified type.
        /// When an audio source is stopped, it is returned to the pool and can be reused for future audio playback requests.
        /// </summary>
        public static void StopAllOfType(Enum requestedAudio)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;
            var audioSources = instance.audioSourcePool.Where(source => source.AudioSource.clip == clip).Select(source => source.AudioSource).ToArray();
            foreach (var source in audioSources)
            {
                instance.StartCoroutine(instance.StopSound(source));
            }
        }

        /// <summary>
        /// Used to stop all currently playing audio sources, regardless of type.
        /// When an audio source is stopped, it is returned to the pool and can be reused for future audio playback requests.
        /// </summary>
        public static void StopAll()
        {
            var audioSources = instance.audioSourcePool.Where(source => source.AudioSource.isPlaying).Select(source => source.AudioSource).ToArray();
            foreach (var source in audioSources)
            {
                instance.StartCoroutine(instance.StopSound(source));
            }
        }

        /// <summary>
        /// Used to pause the specified audio source. 
        /// Paused audio sources can be unpaused with different UnPause methods.
        /// Paused audio sources will not be returned to the pool until they are unpaused and finish playing, so use this method with caution to avoid exhausting available audio sources in the pool.
        /// </summary>
        public static void Pause(AudioSource audioSource)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }

        /// <summary>
        /// Used to pause all currently playing looping audio sources of the specified type.
        /// Paused audio sources can be unpaused with different UnPause methods.
        /// Paused audio sources will not be returned to the pool until they are unpaused and finish playing, so use this method with caution to avoid exhausting available audio sources in the pool.
        /// </summary>
        public static void PauseAllLoopingOfType(Enum requestedAudio)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;
            var audioSources = instance.audioSourcePool.Where(source => source.AudioSource.clip == clip && source.AudioSource.loop && source.AudioSource.isPlaying).Select(source => source.AudioSource).ToArray();
            foreach (var source in audioSources)
            {
                source.Pause();
            }
        }

        /// <summary>
        /// Used to pause all currently playing audio sources of the specified type.
        /// Paused audio sources can be unpaused with different UnPause methods.
        /// Paused audio sources will not be returned to the pool until they are unpaused and finish playing, so use this method with caution to avoid exhausting available audio sources in the pool.
        /// </summary>
        public static void PauseAllOfType(Enum requestedAudio)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;
            var audioSources = instance.audioSourcePool.Where(source => source.AudioSource.clip == clip && source.AudioSource.isPlaying).Select(source => source.AudioSource).ToArray();
            foreach (var source in audioSources)
            {
                source.Pause();
            }
        }

        /// <summary>
        /// Used to pause all currently playing audio sources, regardless of type.
        /// Can be unpaused with UnPauseAll to resume playback of all paused audio sources.
        /// Paused audio sources will not be returned to the pool until they are unpaused and finish playing, so use this method with caution to avoid exhausting available audio sources in the pool.
        /// </summary>
        public static void PauseAll()
        {
            var audioSources = instance.audioSourcePool.Where(source => source.AudioSource.isPlaying).Select(source => source.AudioSource).ToArray();
            foreach (var source in audioSources)
            {
                source.Pause();
            }
        }
        
        /// <summary>
        /// Used to unpause the specified audio source if it is currently paused.
        /// </summary>
        public static void UnPause(AudioSource audioSource)
        {
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.UnPause();
            }
        }

        /// <summary>
        /// Used to unpause all currently paused looping audio sources of the specified type.
        /// </summary>
        public static void UnPauseAllLoopingOfType(Enum requestedAudio)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;
            var audioSources = instance.audioSourcePool.Where(source => source.AudioSource.clip == clip && source.AudioSource.loop && !source.AudioSource.isPlaying).Select(source => source.AudioSource).ToArray();
            foreach (var source in audioSources)
            {
                source.UnPause();
            }
        }

        /// <summary>
        /// Used to unpause all currently paused audio sources of the specified type.
        /// </summary>
        public static void UnPauseAllOfType(Enum requestedAudio)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;
            var audioSources = instance.audioSourcePool.Where(source => source.AudioSource.clip == clip && !source.AudioSource.isPlaying).Select(source => source.AudioSource).ToArray();
            foreach (var source in audioSources)
            {
                source.UnPause();
            }
        }

        /// <summary>
        /// Used to unpause all currently paused audio sources, regardless of type.
        /// </summary>
        public static void UnPauseAll()
        {
            var audioSources = instance.audioSourcePool.Where(source => !source.AudioSource.isPlaying).Select(source => source.AudioSource).ToArray();
            foreach (var source in audioSources)
            {
                source.UnPause();
            }
        }
        #endregion

        /// <summary>
        /// Plays the requested audio at the position of the specified transform.
        /// If followTransform is true, the sound will follow the position of the transform until it finishes playing.
        /// If no transform is specified, the sound will play in 2D space.
        /// </summary>
        public static void Play(Enum requestedAudio, Transform transform = null, bool followTransform = false)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;

            var pooledAudioSource = instance.GetAudioSource();

            instance.SetAudioSourceTransform(pooledAudioSource, transform, followTransform);

            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.spatialBlend = transform == null ? 0f : 1f;
            pooledAudioSource.AudioSource.volume = volume;
            pooledAudioSource.AudioSource.Play();
            instance.StartCoroutine(instance.ReturnAudioSource(pooledAudioSource));
#if UNITY_EDITOR
            pooledAudioSource.name = $"AudioSource_{requestedAudio}_{clip.name}";
#endif
        }

        /// <summary>
        /// Plays the requested audio looping at the position of the specified transform.
        /// To stop the looping audio, call StopLoopSound and pass in the AudioSource returned by this method.
        /// </summary>
        public static AudioSource PlayLooping(Enum requestedAudio, Transform transform = null, bool followTransform = false)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return null;

            var pooledAudioSource = instance.GetAudioSource();

            instance.SetAudioSourceTransform(pooledAudioSource, transform, followTransform);

            pooledAudioSource.AudioSource.loop = true;
            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.spatialBlend = transform == null ? 0f : 1f;
            pooledAudioSource.AudioSource.volume = volume;
            pooledAudioSource.AudioSource.Play();
#if UNITY_EDITOR
            pooledAudioSource.name = $"AudioSource_{requestedAudio}_{clip.name}";
#endif
            return pooledAudioSource.AudioSource;
        }

        /// <summary>
        /// Plays the requested audio with a fade-in effect over the specified fade time.
        /// </summary>
        public static void PlayFadeIn(Enum requestedAudio, float fadeTime = 1f, Transform transform = null, bool followTransform = false)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;

            var pooledAudioSource = instance.GetAudioSource();

            instance.SetAudioSourceTransform(pooledAudioSource, transform, followTransform);

            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.spatialBlend = transform == null ? 0f : 1f;
            pooledAudioSource.AudioSource.volume = volume;
            instance.StartCoroutine(instance.PlayFadeCoroutine(pooledAudioSource.AudioSource, fadeTime, 0, pooledAudioSource.AudioSource.volume));
            instance.StartCoroutine(instance.ReturnAudioSource(pooledAudioSource));
#if UNITY_EDITOR
            pooledAudioSource.name = $"AudioSource_{requestedAudio}_{clip.name}";
#endif
        }

        /// <summary>
        /// Plays the requested audio with a fade-out effect over the specified fade time.
        /// </summary>
        public static void PlayFadeOut(Enum requestedAudio, float fadeTime = 1f, Transform transform = null, bool followTransform = false)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;

            var pooledAudioSource = instance.GetAudioSource();

            instance.SetAudioSourceTransform(pooledAudioSource, transform, followTransform);

            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.spatialBlend = transform == null ? 0f : 1f;
            pooledAudioSource.AudioSource.volume = volume;
            instance.StartCoroutine(instance.PlayFadeCoroutine(pooledAudioSource.AudioSource, fadeTime, pooledAudioSource.AudioSource.volume, 0));
            instance.StartCoroutine(instance.ReturnAudioSource(pooledAudioSource));
#if UNITY_EDITOR
            pooledAudioSource.name = $"AudioSource_{requestedAudio}_{clip.name}";
#endif
        }

        /// <summary>
        /// Fades the requested audio's volume from the specified start volume to the target volume over the specified fade time.
        /// </summary>
        IEnumerator PlayFadeCoroutine(AudioSource audioSource, float fadeTime, float startVolume, float targetVolume)
        {
            audioSource.volume = startVolume;
            audioSource.Play();
            var t = 0f;
            while (t < fadeTime)
            {
                audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t / fadeTime);
                t += Time.deltaTime;
                yield return null;
            }
            audioSource.volume = targetVolume;
        }

        /// <summary>
        /// Plays the requested audio with random pitch and volume variations.
        /// </summary>
        public static void PlayRandomPitchAndVolume(Enum requestedAudio, float pitchRange = 0.05f, float volumeRange = 0.02f, Transform transform = null, bool followTransform = false)
        {
            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;

            var pooledAudioSource = instance.GetAudioSource();

            instance.SetAudioSourceTransform(pooledAudioSource, transform, followTransform);

            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.spatialBlend = transform == null ? 0f : 1f;
            pooledAudioSource.AudioSource.pitch = 1 + Random.Range(-pitchRange, pitchRange);
            pooledAudioSource.AudioSource.volume = pooledAudioSource.AudioSource.volume = volume + Random.Range(-volumeRange, volumeRange);
            pooledAudioSource.AudioSource.Play();
            instance.StartCoroutine(instance.ReturnAudioSource(pooledAudioSource));
#if UNITY_EDITOR
            pooledAudioSource.name = $"AudioSource_{requestedAudio}_{clip.name}";
#endif
        }

        #region Editor Only
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // -------------------------------------------------------------------------------*******************------------------------------------------------------------------------------
        // -----------------------------------------------------------------------------***** Editor Only *****----------------------------------------------------------------------------
        // -------------------------------------------------------------------------------*******************------------------------------------------------------------------------------
        // --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

#if UNITY_EDITOR
        void OnValidate()
        {
            // This will rename the GameObject to "AudioManager" for organization purposes in the editor, and does not affect the functionality of the GameObject in any way.
            if (gameObject != null)
                gameObject.name = "AudioManager";

            // Whenever something is changed in the inspector, we want to re-populate the audio libraries to ensure that any changes to the assigned AudioLibrary ScriptableObjects are reflected in the audioLibraries dictionary.
            PopulateAudioLibraries();
        }

        /// <summary>
        /// This is a test method that should ONLY be called from the Unity Editor.
        /// </summary>
        public static void EditorTestPlay(Enum requestedAudio)
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AudioManager>();
                if (instance == null)
                {
                    Debug.LogError("[Audio Manager (EDITOR)] No AudioManager in scene!");
                    return;
                }
            }

            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;

            var pooledAudioSource = instance.GetAudioSource();

            pooledAudioSource.AudioSource.spatialBlend = 0f;
            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.volume = volume;
            pooledAudioSource.AudioSource.Play();
            Debug.Log("[Audio Manager (EDITOR)] Playing clip: " + clip.name);
            instance.StartCoroutine(instance.EditorTestRemoveAudioSource(pooledAudioSource.AudioSource, clip.length));
        }

        /// <summary>
        /// This is a test method that should ONLY be called from the Unity Editor.
        /// </summary>
        public static void EditorTestPlayRandomPitchAndVolume(Enum requestedAudio, float pitchRange = 0.035f, float volumeRange = 0.02f)
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AudioManager>();
                if (instance == null)
                {
                    Debug.LogError("[Audio Manager (EDITOR)] No AudioManager in scene!");
                    return;
                }
            }

            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return;

            var pooledAudioSource = instance.GetAudioSource();

            pooledAudioSource.AudioSource.spatialBlend = 0f;
            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.pitch = 1 + Random.Range(-pitchRange, pitchRange);
            pooledAudioSource.AudioSource.volume = pooledAudioSource.AudioSource.volume = volume + Random.Range(-volumeRange, volumeRange);
            pooledAudioSource.AudioSource.Play();
            Debug.Log("[Audio Manager (EDITOR)] Playing clip: " + clip.name);
            instance.StartCoroutine(instance.EditorTestRemoveAudioSource(pooledAudioSource.AudioSource, clip.length));
        }

        /// <summary>
        /// This is a test method that should ONLY be called from the Unity Editor.
        /// </summary>
        public static AudioSource EditorTestPlayLooping(Enum requestedAudio)
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AudioManager>();
                if (instance == null)
                {
                    Debug.LogError("[Audio Manager (EDITOR)] No AudioManager in scene!");
                    return null;
                }
            }

            if (!instance.TryGetClip(requestedAudio, out var clip, out float volume)) return null;

            var pooledAudioSource = instance.GetAudioSource();

            pooledAudioSource.AudioSource.spatialBlend = 0f;
            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.volume = volume;
            pooledAudioSource.AudioSource.loop = true;
            pooledAudioSource.AudioSource.Play();
            Debug.Log("[Audio Manager (EDITOR)] Playing clip: " + clip.name);
            return pooledAudioSource.AudioSource;
        }

        /// <summary>
        /// This is a test method that should ONLY be called from the Unity Editor.
        /// </summary>
        public static void EditorTestStopLooping(AudioSource source)
        {
            if (source != null && source.isPlaying)
            {
                instance.StartCoroutine(instance.EditorTestRemoveAudioSource(source, 0f));
            }
        }

        IEnumerator EditorTestRemoveAudioSource(AudioSource audioSource, float delay)
        {
            yield return new WaitForSeconds(delay);
            audioSource.volume = 0.0001f;
            yield return waitForSeconds0_1;
            audioSource.Stop();
            DestroyImmediate(audioSource.gameObject);
        }
#endif
#endregion
    }
}