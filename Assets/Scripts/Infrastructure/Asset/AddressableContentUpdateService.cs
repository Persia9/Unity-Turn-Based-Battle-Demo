using System;
using System.Threading;
using Application.Services;
using Core.Common;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Infrastructure.Asset
{
    public sealed class AddressableContentUpdateService : IContentUpdateService
    {
        public async UniTask UpdateContent(Action<ContentUpdateProgress> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress(new ContentUpdateProgress(ContentUpdateStage.Initializing));
            var initialize = Addressables.InitializeAsync(false);
            try { await Wait(initialize, cancellationToken); }
            finally { if (initialize.IsValid()) Addressables.Release(initialize); }

            progress(new ContentUpdateProgress(ContentUpdateStage.CheckingCatalog));
            var check = Addressables.CheckForCatalogUpdates(false);
            try
            {
                await Wait(check, cancellationToken);
                if (check.Result.Count > 0)
                {
                    progress(new ContentUpdateProgress(ContentUpdateStage.UpdatingCatalog));
                    var update = Addressables.UpdateCatalogs(check.Result, false);
                    try { await Wait(update, cancellationToken); }
                    finally { if (update.IsValid()) Addressables.Release(update); }
                }
            }
            finally { if (check.IsValid()) Addressables.Release(check); }

            progress(new ContentUpdateProgress(ContentUpdateStage.CheckingDownload));
            long bytes;
            var size = Addressables.GetDownloadSizeAsync(GameConstants.RemoteContentLabel);
            try { await Wait(size, cancellationToken); bytes = size.Result; }
            finally { if (size.IsValid()) Addressables.Release(size); }
            if (bytes > 0)
            {
                var download = Addressables.DownloadDependenciesAsync(GameConstants.RemoteContentLabel, false);
                try
                {
                    var nextReport = 0f;
                    while (!download.IsDone)
                    {
                        if (!cancellationToken.IsCancellationRequested && Time.realtimeSinceStartup >= nextReport)
                        {
                            var status = download.GetDownloadStatus();
                            progress(new ContentUpdateProgress(ContentUpdateStage.Downloading, status.DownloadedBytes, status.TotalBytes));
                            nextReport = Time.realtimeSinceStartup + 0.1f;
                        }
                        // Addressables owns the native operation. Drain it before releasing its handle.
                        await UniTask.Yield();
                    }
                    Check(download);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                finally { if (download.IsValid()) Addressables.Release(download); }
            }
            cancellationToken.ThrowIfCancellationRequested();
            progress(new ContentUpdateProgress(ContentUpdateStage.Completed, bytes, bytes));
        }

        private static async UniTask Wait(AsyncOperationHandle handle, CancellationToken token)
        {
            while (!handle.IsDone) await UniTask.Yield();
            Check(handle);
            token.ThrowIfCancellationRequested();
        }

        private static void Check(AsyncOperationHandle handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw handle.OperationException ?? new InvalidOperationException("Addressables content operation failed.");
        }
    }
}
