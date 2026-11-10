using System;
using System.Threading;
using Application.Services;
using Core.Common;
using Core.Logging;
using Cysharp.Threading.Tasks;
using Infrastructure.Scene;
using R3;
using Zenject;

namespace Presentation.Bootstrap
{
    public sealed class BootstrapViewModel : IDisposable
    {
        private const int InitialRetryDelaySeconds = 2;
        private const int MaximumRetryDelaySeconds = 15;
        private readonly IContentUpdateService _content;
        private readonly ISceneService _scenes;
        private readonly ILogger _logger;
        private readonly Func<TimeSpan, CancellationToken, UniTask> _waitForRetry;
        private readonly CancellationTokenSource _lifetime = new();
        private bool _busy, _disposed;
        public ReactiveProperty<string> Status { get; } = new("Preparing resources...");
        public ReactiveProperty<string> Progress { get; } = new(string.Empty);
        public ReactiveProperty<string> Error { get; } = new(string.Empty);
        public ReactiveProperty<float> DownloadProgress { get; } = new(0f);

        [Inject]
        public BootstrapViewModel(IContentUpdateService content, ISceneService scenes, ILogger logger)
            : this(content, scenes, logger, WaitForRetry) { }

        internal BootstrapViewModel(IContentUpdateService content, ISceneService scenes, ILogger logger,
            Func<TimeSpan, CancellationToken, UniTask> waitForRetry)
        { _content = content; _scenes = scenes; _logger = logger; _waitForRetry = waitForRetry; }

        public async UniTask Run(CancellationToken token)
        {
            if (_busy || _disposed) return;
            _busy = true;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
            var operationToken = linked.Token;
            var retryDelay = InitialRetryDelaySeconds;
            try
            {
                while (!_disposed)
                {
                    operationToken.ThrowIfCancellationRequested();
                    Error.Value = string.Empty;
                    Progress.Value = string.Empty;
                    DownloadProgress.Value = 0f;
                    try
                    {
                        await _content.UpdateContent(Report, operationToken);
                        operationToken.ThrowIfCancellationRequested();
                        if (_disposed) return;
                        Status.Value = "Opening title...";
                        await _scenes.LoadScene(GameConstants.TitleSceneName);
                        return;
                    }
                    catch (OperationCanceledException) when (operationToken.IsCancellationRequested) { throw; }
                    catch (Exception error)
                    {
                        operationToken.ThrowIfCancellationRequested();
                        if (_disposed) return;
                        _logger.Error(error.ToString());
                        Status.Value = $"Update failed. Retrying automatically in {retryDelay}s...";
                        Error.Value = "Check your network connection or the resource server.";
                        await _waitForRetry(TimeSpan.FromSeconds(retryDelay), operationToken);
                        retryDelay = Math.Min(retryDelay * 2, MaximumRetryDelaySeconds);
                    }
                }
            }
            catch (OperationCanceledException) when (operationToken.IsCancellationRequested) { }
            finally { _busy = false; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; _lifetime.Cancel(); _lifetime.Dispose();
            Status.Dispose(); Progress.Dispose(); Error.Dispose(); DownloadProgress.Dispose();
        }

        private void Report(ContentUpdateProgress value)
        {
            if (_disposed) return;
            Status.Value = value.Stage switch
            {
                ContentUpdateStage.Initializing => "Initializing Addressables...",
                ContentUpdateStage.CheckingCatalog => "Checking for resource updates...",
                ContentUpdateStage.UpdatingCatalog => "Updating resource catalog...",
                ContentUpdateStage.CheckingDownload => "Checking download size...",
                ContentUpdateStage.Downloading => "Downloading resources...",
                ContentUpdateStage.Completed => "Resources ready",
                _ => "Preparing resources..."
            };
            Progress.Value = value.TotalBytes > 0
                ? $"{value.DownloadedBytes / 1048576f:F1} / {value.TotalBytes / 1048576f:F1} MB"
                : string.Empty;
            DownloadProgress.Value = value.Stage == ContentUpdateStage.Completed ? 1f
                : value.TotalBytes > 0 ? Math.Max(0f, Math.Min(1f, (float)value.DownloadedBytes / value.TotalBytes)) : 0f;
        }

        private static UniTask WaitForRetry(TimeSpan duration, CancellationToken token)
            => UniTask.Delay(duration, ignoreTimeScale: true, cancellationToken: token);
    }
}
