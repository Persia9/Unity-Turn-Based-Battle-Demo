using System;
using Application.UseCases;
using Core.Logging;
using Cysharp.Threading.Tasks;
using Domain.Run;
using Infrastructure.Localization;
using R3;
using Zenject;

namespace Presentation.Title
{
    public sealed class TitleScreenViewModel : IDisposable
    {
        private readonly Func<RunMode, UniTask> _start;
        private readonly ILocalizationService _localization;
        private readonly ILogger _logger;
        private bool _busy, _opened, _disposed;
        private string _error = string.Empty;

        public sealed class DisplayState
        {
            public string Genre { get; internal set; } = string.Empty;
            public string Subtitle { get; internal set; } = string.Empty;
            public string Tagline { get; internal set; } = string.Empty;
            public string Expedition { get; internal set; } = string.Empty;
            public string Endless { get; internal set; } = string.Empty;
            public string LanguageLabel { get; internal set; } = string.Empty;
            public string Error { get; internal set; } = string.Empty;
            public GameLanguage Language { get; internal set; }
            public bool CanStart { get; internal set; }
        }

        public ReactiveProperty<DisplayState> State { get; } = new(new DisplayState());

        [Inject]
        public TitleScreenViewModel(RunFlowUseCase flow, ILocalizationService localization, ILogger logger)
            : this(flow.StartNewRun, localization, logger)
        {
        }

        internal TitleScreenViewModel(Func<RunMode, UniTask> start, ILocalizationService localization, ILogger logger)
        {
            _start = start;
            _localization = localization;
            _logger = logger;
        }

        public void Open()
        {
            if (_disposed) return;
            if (!_opened)
            {
                _localization.LanguageChanged += Publish;
                _opened = true;
            }

            _error = string.Empty;
            Publish();
        }

        public void Close()
        {
            if (!_opened) return;
            _localization.LanguageChanged -= Publish;
            _opened = false;
        }

        public void ToggleLanguage()
        {
            if (!_disposed && _opened && !_busy) _localization.ToggleLanguage();
        }

        public async UniTask Start(RunMode mode)
        {
            if (_disposed || !_opened || _busy) return;
            _busy = true;
            _error = string.Empty;
            Publish();
            try
            {
                await _start(mode);
            }
            catch (Exception error)
            {
                _logger.Error(error.ToString());
                if (!_disposed) _error = error.Message;
            }
            finally
            {
                _busy = false;
                if (!_disposed) Publish();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            Close();
            _disposed = true;
            State.Dispose();
        }

        private void Publish()
        {
            if (_disposed) return;
            State.Value = new DisplayState
            {
                Genre = _localization.Get("title.genre"), Subtitle = _localization.Get("title.subtitle"),
                Tagline = _localization.Get("title.tagline"), Expedition = _localization.Get("title.start.expedition"),
                Endless = _localization.Get("title.start.endless"),
                LanguageLabel = _localization.Get("language.toggle"),
                Language = _localization.CurrentLanguage, CanStart = !_busy, Error = _error
            };
        }
    }
}