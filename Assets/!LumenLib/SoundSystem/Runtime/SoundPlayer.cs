using System;
using System.Collections;
using _LumenLib.PoolingSystem.Runtime;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace _LumenLib.SoundSystem.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundPlayer : MonoBehaviour, IPoolable
    {
        [SerializeField] private PoolItemSO poolItem;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup musicMixerGroup;

        private AudioSource _audioSource;
        private Coroutine _finishRoutine;

        public PoolItemSO Item => poolItem;
        public GameObject GameObject => gameObject;
        public event Action<SoundPlayer> SoundFinished;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public bool Play(SoundClipSO clipData)
        {
            if (clipData == null || clipData.Clip == null)
            {
                Debug.LogWarning("[SoundPlayer] A valid SoundClipSO is required.", this);
                return false;
            }

            StopFinishRoutine();

            _audioSource.outputAudioMixerGroup = clipData.AudioType == AudioType.Music
                ? musicMixerGroup
                : sfxMixerGroup;
            _audioSource.volume = clipData.Volume;
            _audioSource.pitch = ResolvePitch(clipData);
            _audioSource.clip = clipData.Clip;
            _audioSource.loop = clipData.IsLoop;
            _audioSource.time = Mathf.Clamp(clipData.StartTime, 0f, clipData.Clip.length);
            _audioSource.Play();

            if (!clipData.IsLoop)
            {
                float duration = (clipData.ResolvedEndTime - _audioSource.time) /
                                 Mathf.Max(Mathf.Abs(_audioSource.pitch), 0.01f);
                _finishRoutine = StartCoroutine(FinishAfter(Mathf.Max(0f, duration)));
            }

            return true;
        }

        public void Stop()
        {
            StopFinishRoutine();
            _audioSource.Stop();
        }

        public void ResetItem()
        {
            gameObject.SetActive(true);
            StopFinishRoutine();
            _audioSource.Stop();
            _audioSource.clip = null;
            SoundFinished = null;
        }

        private static float ResolvePitch(SoundClipSO clipData)
        {
            if (!clipData.RandomizePitch)
                return clipData.Pitch;

            return Mathf.Clamp(
                clipData.Pitch + Random.Range(-clipData.RandomPitchModifier, clipData.RandomPitchModifier),
                0.1f,
                3f);
        }

        private IEnumerator FinishAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);

            _finishRoutine = null;
            _audioSource.Stop();
            SoundFinished?.Invoke(this);
        }

        private void StopFinishRoutine()
        {
            if (_finishRoutine == null)
                return;

            StopCoroutine(_finishRoutine);
            _finishRoutine = null;
        }
    }
}
