using Application.Messages;
using Infrastructure.Audio;
using MessagePipe;

namespace Application.UseCases
{
    public sealed class StartTitleUseCase
    {
        private readonly IAudioService _audioService;
        private readonly IPublisher<PlayBgmRequestedMessage> _publisher;

        public StartTitleUseCase(IAudioService audioService, IPublisher<PlayBgmRequestedMessage> publisher)
        {
            _audioService = audioService;
            _publisher = publisher;
        }

        public void Execute()
        {
            _publisher.Publish(new PlayBgmRequestedMessage("bgm_title"));
            _audioService.PlayBgm("bgm_title");
        }
    }
}