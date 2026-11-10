using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Infrastructure.Asset
{
    public sealed class AssetBundleAssetService : IAssetService, IDisposable
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly Dictionary<Object, int> _leases = new();
        private AssetBundle _bundle;
        private bool _disposed;

        public async UniTask<T> Load<T>(string key) where T : Object
        {
            var entry = LocalAssetCatalog.Get(key);
            await _gate.WaitAsync();
            try
            {
                if (_disposed) throw new ObjectDisposedException(nameof(AssetBundleAssetService));
                T asset;
#if UNITY_EDITOR
                // Editor ではビルド済み Bundle を必要とせず、同じ対応表から読み込む。
                asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(entry.Path);
#else
                if (_bundle == null) _bundle = await LoadBundle();
                if (_disposed)
                {
                    UnloadBundle();
                    throw new ObjectDisposedException(nameof(AssetBundleAssetService));
                }
                var request = _bundle.LoadAssetAsync<T>(entry.Key.ToLowerInvariant());
                await request;
                if (_disposed) throw new ObjectDisposedException(nameof(AssetBundleAssetService));
                asset = request.asset as T;
#endif
                if (asset == null) throw new InvalidOperationException($"Asset '{key}' could not be loaded as {typeof(T).Name}.");
                _leases.TryGetValue(asset, out var count);
                _leases[asset] = count + 1;
                return asset;
            }
            catch
            {
                if (_leases.Count == 0) UnloadBundle();
                throw;
            }
            finally { _gate.Release(); }
        }

        public async UniTask Release(object assetOrInstance)
        {
            await _gate.WaitAsync();
            try
            {
                if (assetOrInstance is not Object asset || !_leases.TryGetValue(asset, out var count)) return;
                if (count > 1) _leases[asset] = count - 1;
                else _leases.Remove(asset);
                if (_leases.Count == 0) UnloadBundle();
            }
            finally { _gate.Release(); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _leases.Clear();
            // 非同期ロード中は完了側で解放し、処理中の Bundle を破棄しない。
            if (_gate.CurrentCount != 0) UnloadBundle();
        }

        private static async UniTask<AssetBundle> LoadBundle()
        {
            var path = Path.Combine(UnityEngine.Application.streamingAssetsPath,
                LocalAssetCatalog.DirectoryName, LocalAssetCatalog.BundleName).Replace('\\', '/');
            if (path.Contains("://") || path.StartsWith("jar:"))
            {
                using var request = UnityWebRequestAssetBundle.GetAssetBundle(path);
                await request.SendWebRequest();
                return DownloadHandlerAssetBundle.GetContent(request)
                    ?? throw new IOException($"Could not load AssetBundle: {path}");
            }
            var load = AssetBundle.LoadFromFileAsync(path);
            await load;
            return load.assetBundle ?? throw new IOException($"AssetBundle missing or invalid: {path}. Build the player again or run Tools/AssetBundles/Build Local Bundles.");
        }

        private void UnloadBundle()
        {
            // 参照済みオブジェクトを強制破棄せず、Bundle のコンテナのみ解放する。
            if (_bundle != null) _bundle.Unload(false);
            _bundle = null;
        }
    }
}
