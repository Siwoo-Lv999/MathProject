using UnityEngine;
using Services = _LumenLib.ServiceLocator.ServiceLocator;

namespace _LumenLib.SoundSystem.Runtime
{
    public sealed class NullAudioService : IAudioService
    {
        public static NullAudioService Instance { get; } = new();

        private NullAudioService()
        {
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterDefault()
        {
            if (!Services.TryGet<IAudioService>(out _))
                Services.Register<IAudioService>(Instance);
        }

        public void PlaySfx(SoundClipSO clipData, int channel = 0)
        {
        }

        public void StopSfx(int channel)
        {
        }

        public void PlayBgm(SoundClipSO clipData)
        {
        }

        public void StopBgm()
        {
        }
    }
}
