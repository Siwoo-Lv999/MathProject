namespace _LumenLib.SoundSystem.Runtime
{
    public interface IAudioService
    {
        void PlaySfx(SoundClipSO clipData, int channel = 0);
        void StopSfx(int channel);
        void PlayBgm(SoundClipSO clipData);
        void StopBgm();
    }
}
