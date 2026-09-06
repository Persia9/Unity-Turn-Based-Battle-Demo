using System;
using Application.Messages;
using Infrastructure.Audio;
using MessagePipe;
using Zenject;

namespace Application.Handlers
{
    public sealed class PlaySeRequestedHandler : IInitializable, IDisposable
    {
        private readonly ISubscriber<PlaySeRequestedMessage> _subscriber;
        private readonly IAudioService _audioService;
        private IDisposable _disposable;

        public PlaySeRequestedHandler(
            ISubscriber<PlaySeRequestedMessage> subscriber,
            IAudioService audioService)
        {
            _subscriber = subscriber;
            _audioService = audioService;
        }

        public void Initialize()
        {
            _disposable = _subscriber.Subscribe(OnReceived);
        }

        private void OnReceived(PlaySeRequestedMessage message)
        {
            _audioService.PlaySe(message.EventId);
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}