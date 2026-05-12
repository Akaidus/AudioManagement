using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace AudioManagement
{

    /// <summary>
    /// Enum for defining different types of sound effects.
    /// Used to reference the sound effect in code.
    /// For new SFX types, add a new entry to the SfxType enum and Create a new Scriptable Object of the type "SfxScriptableObject" for it.
    /// </summary>
    public enum SfxType
    {
        TestSound,
    }

    /// <summary>
    /// Class for storing sound effect data.
    /// </summary>
    [Serializable]
    public class SfxData
    {
        public AudioClip[] clips;
        [Range(0, 1)] public float volume;
    }

    /// <summary>
    /// Singleton class for managing audio in the game.
    /// Stores references to audio clips in a dictionary for easy access, and manage a pool of audio sources for playing sound effects.
    /// Various methods for playing sound effects with different options, such as looping, fading, and random pitch/volume variations.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Source Pooling")]
        [SerializeField] PooledAudioSource audioSourcePrefab;
        [SerializeField] int maxAudioSourcesCount = 100;
        [SerializeField] [Tooltip("The interval at which to prune the audio source pool.")] float pruneInterval = 10f;
        Queue<PooledAudioSource> audioSourcePool = new();
        WaitForSeconds waitForSeconds0_1 = new(0.1f);

        [Header("Audio Clips")]
        [SerializeField] SfxScriptableObject[] sfxScriptableObjects;
        Dictionary<SfxType, SfxScriptableObject> sfxDictionary = new();

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

            PopulateDictionary();
            CreateAudioSourcePool();
            pruneRoutine = StartCoroutine(PrunePool());
        }

        void PopulateDictionary()
        {
            sfxDictionary.Clear();
            if (sfxScriptableObjects == null) return;
            if (sfxScriptableObjects.Length <= 0)
            {
                Debug.LogWarning("[Audio Manager] No SfxScriptableObjects assigned in AudioManager inspector. Assign SfxScriptableObjects to populate the audio clip dictionary, else the audio clips will not play.");
                return;
            }
            foreach (var sfx in sfxScriptableObjects)
            {
                if (sfx == null)
                {
                    Debug.LogWarning($"[Audio Manager] Null SfxScriptableObject found at index {sfx} in AudioManager inspector. Assign a valid SfxScriptableObject or remove the entry.");
                    continue;
                }
                if (sfxDictionary.ContainsKey(sfx.sfxType))
                {
                    Debug.LogWarning($"[Audio Manager] Duplicate SfxType {sfx.sfxType} found in AudioManager inspector. Remove or change the duplicate entry.");
                    continue;
                }
                sfxDictionary.Add(sfx.sfxType, sfx);
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
                audioSource.gameObject.name = $"AudioSource_{i}";
#endif
                audioSourcePool.Enqueue(audioSource);
            }
        }

        /// <summary>
        /// Periodically checks the audio source pool and destroys any excess audio sources if the pool size exceeds the specified maximum count.
        /// </summary>
        IEnumerator PrunePool()
        {
            WaitForSeconds wait = new(pruneInterval);
            while (true)
            {
                yield return wait;
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
            audioSource.gameObject.name = $"AudioSource_{audioSourcePool.Count}";
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
        /// Try to get a random audio clip for the specified SFX type. 
        /// If there are no clips assigned for that SFX type, it will log a warning and return false. Otherwise, it will return true and output the randomly selected clip.
        /// </summary>
        bool TryGetClip(SfxType sfxType, out AudioClip clip, out float volume)
        {
            clip = null;
            var sfx = sfxDictionary.GetValueOrDefault(sfxType);
            if (!sfxDictionary.ContainsKey(sfxType))
            {
                Debug.LogWarning($"[Audio Manager] No SFX data found in dictionary for SFX type: {sfxType}. Make sure to assign a SfxScriptableObject for this SFX type in the AudioManager inspector.");
                volume = 0f;
                return false;
            }
            volume = sfx.data.volume;
            if (sfx == null || sfx.data.clips == null || sfx.data.clips.Length == 0)
            {
                Debug.LogWarning($"[Audio Manager] No clips assigned for SFX: {sfxType}");
                return false;
            }

            clip = sfx.data.clips[Random.Range(0, sfx.data.clips.Length)];
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

        /// <summary>
        /// Plays a sound effect at the position of the specified transform.
        /// If followTransform is true, the sound will follow the position of the transform until it finishes playing.
        /// If no transform is specified, the sound will play in 2D space.
        /// </summary>
        public static void Play(SfxType sfx, Transform transform = null, bool followTransform = false)
        {
            if (!instance.TryGetClip(sfx, out var clip, out float volume)) return;

            var pooledAudioSource = instance.GetAudioSource();

            instance.SetAudioSourceTransform(pooledAudioSource, transform, followTransform);

            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.spatialBlend = transform == null ? 0f : 1f;
            pooledAudioSource.AudioSource.volume = volume;
            pooledAudioSource.AudioSource.Play();
            instance.StartCoroutine(instance.ReturnAudioSource(pooledAudioSource));
#if UNITY_EDITOR
            pooledAudioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif
        }

        /// <summary>
        /// Plays a looping sound effect at the position of the specified transform.
        /// To stop the looping sound, call StopLoopSound and pass in the AudioSource returned by this method.
        /// </summary>
        public static AudioSource PlayLooping(SfxType sfx, Transform transform = null, bool followTransform = false)
        {
            if (!instance.TryGetClip(sfx, out var clip, out float volume)) return null;

            var pooledAudioSource = instance.GetAudioSource();

            instance.SetAudioSourceTransform(pooledAudioSource, transform, followTransform);

            pooledAudioSource.AudioSource.loop = true;
            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.spatialBlend = transform == null ? 0f : 1f;
            pooledAudioSource.AudioSource.volume = volume;
            pooledAudioSource.AudioSource.Play();
#if UNITY_EDITOR
            pooledAudioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif
            return pooledAudioSource.AudioSource;
        }

        /// <summary>
        /// Used to stop a looping sound that was started with PlayLoopSound. 
        /// The AudioSource passed in should be the one returned by PlayLoopSound when the looping sound was started.
        /// </summary>
        public static void StopLooping(AudioSource audioSource)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                instance.StartCoroutine(instance.StopSound(audioSource));
            }
        }

        /// <summary>
        /// Plays a sound effect with a fade-in effect over the specified fade time.
        /// </summary>
        public static void PlayFadeIn(SfxType sfx, float fadeTime = 1f, Transform transform = null, bool followTransform = false)
        {
            if (!instance.TryGetClip(sfx, out var clip, out float volume)) return;

            var pooledAudioSource = instance.GetAudioSource();

            instance.SetAudioSourceTransform(pooledAudioSource, transform, followTransform);

            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.spatialBlend = transform == null ? 0f : 1f;
            pooledAudioSource.AudioSource.volume = volume;
            instance.StartCoroutine(instance.PlayFadeCoroutine(pooledAudioSource.AudioSource, fadeTime, 0, pooledAudioSource.AudioSource.volume));
            instance.StartCoroutine(instance.ReturnAudioSource(pooledAudioSource));
#if UNITY_EDITOR
            pooledAudioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif
        }

        /// <summary>
        /// Plays a sound effect with a fade-out effect over the specified fade time.
        /// </summary>
        public static void PlayFadeOut(SfxType sfx, float fadeTime = 1f, Transform transform = null, bool followTransform = false)
        {
            if (!instance.TryGetClip(sfx, out var clip, out float volume)) return;

            var pooledAudioSource = instance.GetAudioSource();

            instance.SetAudioSourceTransform(pooledAudioSource, transform, followTransform);

            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.spatialBlend = transform == null ? 0f : 1f;
            pooledAudioSource.AudioSource.volume = volume;
            instance.StartCoroutine(instance.PlayFadeCoroutine(pooledAudioSource.AudioSource, fadeTime, pooledAudioSource.AudioSource.volume, 0));
            instance.StartCoroutine(instance.ReturnAudioSource(pooledAudioSource));
#if UNITY_EDITOR
            pooledAudioSource.name = $"AudioSource_{sfx}_{clip.name}";
#endif
        }

        /// <summary>
        /// Fades the sound effect's volume from the specified start volume to the target volume over the specified fade time.
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
        /// Plays a sound effect with random pitch and volume variations.
        /// </summary>
        public static void PlayRandomPitchAndVolume(SfxType sfx, float pitchRange = 0.05f, float volumeRange = 0.02f, Transform transform = null, bool followTransform = false)
        {
            if (!instance.TryGetClip(sfx, out var clip, out float volume)) return;

            var pooledAudioSource = instance.GetAudioSource();

            instance.SetAudioSourceTransform(pooledAudioSource, transform, followTransform);

            pooledAudioSource.AudioSource.clip = clip;
            pooledAudioSource.AudioSource.spatialBlend = transform == null ? 0f : 1f;
            pooledAudioSource.AudioSource.pitch = 1 + Random.Range(-pitchRange, pitchRange);
            pooledAudioSource.AudioSource.volume = pooledAudioSource.AudioSource.volume = volume + Random.Range(-volumeRange, volumeRange);
            pooledAudioSource.AudioSource.Play();
            instance.StartCoroutine(instance.ReturnAudioSource(pooledAudioSource));
#if UNITY_EDITOR
            pooledAudioSource.name = $"AudioSource_{sfx}_{clip.name}";
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
            if (gameObject != null)
                gameObject.name = "AudioManager";
            PopulateDictionary();
        }

        /// <summary>
        /// This is a test method that should ONLY be called from the Unity Editor.
        /// </summary>
        public static void EditorTestPlay(SfxType sfx)
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

            if (!instance.TryGetClip(sfx, out var clip, out float volume)) return;

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
        public static void EditorTestPlayRandomPitchAndVolume(SfxType sfx, float pitchRange = 0.035f, float volumeRange = 0.02f)
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

            if (!instance.TryGetClip(sfx, out var clip, out float volume)) return;

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
        public static AudioSource EditorTestPlayLooping(SfxType sfx)
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

            if (!instance.TryGetClip(sfx, out var clip, out float volume)) return null;

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