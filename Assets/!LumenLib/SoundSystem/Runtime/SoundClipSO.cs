using UnityEngine;
using UnityEngine.Serialization;

namespace _LumenLib.SoundSystem.Runtime
{
    [CreateAssetMenu(fileName = "Sound Clip", menuName = "Lib/Audio/Sound Clip")]
    public sealed class SoundClipSO : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("<AudioType>k__BackingField")]
        private AudioType audioType;

        [SerializeField, FormerlySerializedAs("<Clip>k__BackingField")]
        private AudioClip clip;

        [SerializeField, FormerlySerializedAs("<IsLoop>k__BackingField")]
        private bool isLoop;

        [SerializeField, FormerlySerializedAs("<RandomizePitch>k__BackingField")]
        private bool randomizePitch;

        [SerializeField, Range(0f, 1f), FormerlySerializedAs("<RandomPitchModifier>k__BackingField")]
        private float randomPitchModifier = 0.1f;

        [SerializeField, Range(0f, 1f), FormerlySerializedAs("<Volume>k__BackingField")]
        private float volume = 1f;

        [SerializeField, Range(0.1f, 3f), FormerlySerializedAs("<Pitch>k__BackingField")]
        private float pitch = 1f;

        [SerializeField, Min(0f), FormerlySerializedAs("<StartTime>k__BackingField")]
        private float startTime;

        [SerializeField, Min(0f), FormerlySerializedAs("<EndTime>k__BackingField")]
        private float endTime;

        public AudioType AudioType => audioType;
        public AudioClip Clip => clip;
        public bool IsLoop => isLoop;
        public bool RandomizePitch => randomizePitch;
        public float RandomPitchModifier => randomPitchModifier;
        public float Volume => volume;
        public float Pitch => pitch;
        public float StartTime => startTime;
        public float EndTime => endTime;

        public float ResolvedEndTime => clip == null || endTime <= startTime
            ? clip?.length ?? 0f
            : Mathf.Min(endTime, clip.length);

        private void OnValidate()
        {
            if (clip == null)
            {
                startTime = 0f;
                endTime = 0f;
                return;
            }

            startTime = Mathf.Clamp(startTime, 0f, clip.length);
            endTime = endTime <= startTime
                ? clip.length
                : Mathf.Clamp(endTime, startTime, clip.length);
        }
    }
}
