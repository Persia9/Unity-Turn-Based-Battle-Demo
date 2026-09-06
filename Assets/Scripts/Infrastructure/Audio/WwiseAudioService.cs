using Core.Logging;
using Infrastructure.MasterData.Repositories;

namespace Infrastructure.Audio
{
    public sealed class WwiseAudioService : IAudioService
    {
        private readonly IAudioEventRepository _audioEventRepository;
        private readonly ILogger _logger;

        public WwiseAudioService(IAudioEventRepository audioEventRepository, ILogger logger)
        {
            _audioEventRepository = audioEventRepository;
            _logger = logger;
        }

        public void PlayBgm(string eventId)
        {
            var evt = _audioEventRepository.Get(eventId);
#if AK_WWISE
            AkSoundEngine.PostEvent(evt.EventName, UnityEngine.Camera.main.gameObject);
#else
            _logger.Log($"[MockWwise] PlayBgm: {evt.EventName}");
#endif
        }

        public void PlaySe(string eventId)
        {
            var evt = _audioEventRepository.Get(eventId);
#if AK_WWISE
            AkSoundEngine.PostEvent(evt.EventName, UnityEngine.Camera.main.gameObject);
#else
            _logger.Log($"[MockWwise] PlaySe: {evt.EventName}");
#endif
        }

        public void StopBgm(string eventId)
        {
            var evt = _audioEventRepository.Get(eventId);
#if AK_WWISE
            AkSoundEngine.ExecuteActionOnEvent(evt.EventName, AkActionOnEventType.AkActionOnEventType_Stop, UnityEngine.Camera.main.gameObject, 0, AkCurveInterpolation.AkCurveInterpolation_Linear);
#else
            _logger.Log($"[MockWwise] StopBgm: {evt.EventName}");
#endif
        }
    }
}