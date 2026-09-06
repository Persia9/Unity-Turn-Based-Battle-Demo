namespace Infrastructure.Audio
{
    public interface IAudioService
    {
        void PlayBgm(string eventId);
        void PlaySe(string eventId);
        void StopBgm(string eventId);
    }
}