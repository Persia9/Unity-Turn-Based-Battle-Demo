using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Application.Services
{
    public enum ContentUpdateStage { Initializing, CheckingCatalog, UpdatingCatalog, CheckingDownload, Downloading, Completed }

    public readonly struct ContentUpdateProgress
    {
        public readonly ContentUpdateStage Stage;
        public readonly long DownloadedBytes, TotalBytes;
        public ContentUpdateProgress(ContentUpdateStage stage, long downloadedBytes = 0, long totalBytes = 0)
        { Stage = stage; DownloadedBytes = downloadedBytes; TotalBytes = totalBytes; }
    }

    public interface IContentUpdateService
    {
        UniTask UpdateContent(Action<ContentUpdateProgress> progress, CancellationToken cancellationToken);
    }
}
