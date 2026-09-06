using System;
using Application.Messages;
using Infrastructure.Audio;
using MessagePipe;
using Zenject;

namespace Application.Handlers
{
    public sealed class PlayBgmRequestedHandler : IInitializable, IDisposable
    {
        private readonly ISubscriber<PlayBgmRequestedMessage> _subscriber;
        private readonly IAudioService _audioService;
        private IDisposable _disposable;

        public PlayBgmRequestedHandler(
            ISubscriber<PlayBgmRequestedMessage> subscriber,
            IAudioService audioService)
        {
            _subscriber = subscriber;
            _audioService = audioService;
        }

        public void Initialize()
        {
            _disposable = _subscriber.Subscribe(OnReceived);
        }

        private void OnReceived(PlayBgmRequestedMessage message)
        {
            _audioService.PlayBgm(message.EventId);
        }

        public void Dispose()
        {
            
            _disposable?.Dispose();
        }
    }
}