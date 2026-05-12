using UnityEngine;

namespace AudioManagement
{
    /// <summary>
    /// Component for playing sound effects. 
    /// Can be used for one-shot sounds, looping sounds, and fading sounds in and out. 
    /// Can also be set to play on impact when colliding with another object. 
    /// Can be used with a target transform to play the sound at a specific position, or can be set to play in 2D space. 
    /// Finally, it has options for randomizing pitch and volume within a specified range.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        [Header("Sound")]
        [SerializeField] SfxScriptableObject sfxSO;

        [Header("Settings")]
        [SerializeField] [Tooltip("Transform to play the sound at. If left empty, the sound will play at this GameObject's position.")] Transform targetTransform;
        [SerializeField] [Tooltip("Whether to follow the target transform.")] bool followTarget = false;
        [SerializeField] [Tooltip("Whether to play the sound on impact.")] bool sfxOnImpact = false;
        [SerializeField] [Tooltip("Whether the sound is 2D.")] bool twoD = false;

        [Header("Randomization")]
        [SerializeField] [Tooltip("Range for random pitch adjustment.")] float pitchRange = 0.1f;
        [SerializeField] [Tooltip("Range for random volume adjustment.")] float volumeRange = 0.1f;

        AudioSource audioSource;

        void Awake()
        {
            if (targetTransform == null)
                targetTransform = transform;

            // If the sound is 2D, we don't need a target transform.
            targetTransform = twoD == true ? null : targetTransform;
        }

        /// <summary>
        /// To be called from an event or something if needed.
        /// </summary>
        public void PlaySound()
        {
            // This is how we play a simple one-shot sound effect.
            // We pass in the SfxType from the SfxScriptableObject to reference the sound effect we want to play.
            // This can either be done by passing in the SfxType directly (SfxType.TestSound), or by passing in the SfxScriptableObject and accessing the SfxType from it like we do here.
            // The sound will play at the position of the target transform if one is assigned, or at the position of this GameObject if no target transform is assigned.
            // The sound will also follow the target transform if followTarget is set to true.
            // If the sound is 2D, the target transform will be ignored and the sound will play as 2D.
            AudioManager.Play(sfxSO.sfxType, targetTransform, followTarget);
        }

        /// <summary>
        /// To be called from an event or something if needed.
        /// </summary>
        public void PlayLoop()
        {
            // This is how we play a looping sound effect.
            // We need to keep track of the audio source so that we can stop it later.
            if (audioSource != null) return; // If we already have an audio source, prevent multiple loops from being played at the same time.
            audioSource = AudioManager.PlayLooping(sfxSO.sfxType, targetTransform, followTarget);
        }

        /// <summary>
        /// To be called from an animation event or something when stopping the looping sound from PlayLoop.
        /// </summary>
        public void StopLoop()
        {
            // This is how we stop a looping sound effect.
            // We need to pass in the audio source that was returned from PlayLoop to stop the correct sound.
            if (audioSource != null)
            {
                AudioManager.StopLooping(audioSource);
                audioSource = null;
            }
        }

        /// <summary>
        /// To be called from an animation event or something if needed.
        /// </summary>
        public void PlayFadeIn(float fadeTime)
        {
            // This is how we play a sound effect with a fade-in.
            // The sound will start at volume 0 and fade in to the volume specified in the SfxData for the sound effect over the specified fadeTime variable.
            AudioManager.PlayFadeIn(sfxSO.sfxType, fadeTime, targetTransform, followTarget);
        }

        /// <summary>
        /// To be called from an animation event or something if needed.
        /// </summary>
        public void PlayFadeOut(float fadeTime)
        {
            // This is how we play a sound effect with a fade-out.
            // The sound will start at the volume specified in the SfxData for the sound effect and fade out to volume 0 over the specified fadeTime variable.
            AudioManager.PlayFadeOut(sfxSO.sfxType, fadeTime, targetTransform, followTarget);
        }

        /// <summary>
        /// To be called from an animation event or something if needed.
        /// </summary>
        public void PlayRandomPitchAndVolume()
        {
            // This is how we play a sound effect with random pitch and volume.
            // The pitch and volume will be randomly adjusted within the specified pitchRange and volumeRange variables.
            AudioManager.PlayRandomPitchAndVolume(sfxSO.sfxType, pitchRange, volumeRange, targetTransform, followTarget);
        }

        /// <summary>
        /// Called when the object or its children collide with another object if sfxOnImpact is true.
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            if (sfxOnImpact)
            {
                AudioManager.Play(sfxSO.sfxType, targetTransform, followTarget);
            }
        }
    }
}