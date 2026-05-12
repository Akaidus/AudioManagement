using UnityEngine;

namespace AudioManagement
{
    /// <summary>
    /// Component for a prefab audio source that can be pooled.
    /// Assign this as the audio source prefab in the AudioManager.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class PooledAudioSource : MonoBehaviour
    {
        public AudioSource AudioSource;

        Transform followTarget;
        bool followTargetPosition = false;

        void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // This is to ensure that the AudioSource reference is always assigned in the editor.
            if (AudioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out var audioSource))
                {
                    gameObject.AddComponent<AudioSource>();
                }
                AudioSource = GetComponent<AudioSource>();
            }
        }
#endif

        void LateUpdate()
        {
            if (followTargetPosition)
            {
                transform.position = followTarget.position;
            }
        }

        /// <summary>
        /// Initiates following the target transform. 
        /// The audio source will follow the position of the target transform until StopFollowingTarget is called.
        /// </summary>
        public void FollowTarget(Transform target)
        {
            if (target == null)
            {
                Debug.LogWarning("[PooledAudioSource] FollowTarget called with null target. This will stop the audio source from following any target.", this);
                return;
            }
            followTargetPosition = true;
            followTarget = target;
        }

        /// <summary>
        /// Stops following the target transform.
        /// </summary>
        public void StopFollowingTarget()
        {
            followTarget = null;
            followTargetPosition = false;
        }
    }
}