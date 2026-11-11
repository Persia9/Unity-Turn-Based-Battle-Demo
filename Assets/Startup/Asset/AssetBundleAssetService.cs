using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Startup;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Infrastructure.Asset
{
    // Bundles stay open for the selected content version; version switching requires restarting the game.
    public sealed class AssetBundleAssetService : IAssetService, ISceneBundleLoader, IDisposable
    {
        private readonly ContentRepository _repository;
        private readonly ContentSelection _content;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly Dictionary<string, AssetBundle> _bundles = new();
        private readonly Dictionary<Object, int> _leases = new();
        private bool _disposed;

        public AssetBundleAssetService(ContentRepository repository, ContentSelection content)
        { _repository = repository; _content = content; }

        public async UniTask<T> Load<T>(string key) where T : Object
        {
            await _gate.WaitAsync();
            try
            {
                ThrowIfDisposed();
                var entry = Array.Find(_content.Manifest.assets, x => x.key == key)
                    ?? throw new ArgumentException($"Unknown asset: {key}");
                var bundle = await EnsureBundle(entry.bundle, new HashSet<string>());
                var request = bundle.LoadAssetAsync<T>(entry.name);
                await request;
                ThrowIfDisposed();
                var asset = request.asset as T ?? throw new InvalidDataException($"Asset type mismatch: {key}");
                _leases.TryGetValue(asset, out var count); _leases[asset] = count + 1;
                return asset;
            }
            finally { if (_disposed) Unload(); _gate.Release(); }
        }

        public async UniTask Release(object assetOrInstance)
        {
            await _gate.WaitAsync();
            try
            {
                if (assetOrInstance is Object asset && _leases.TryGetValue(asset, out var count))
                {
                    if (count > 1) _leases[asset] = count - 1;
                    else _leases.Remove(asset);
                }
            }
            finally { _gate.Release(); }
        }

        public async UniTask LoadScene(string sceneName)
        {
            await _gate.WaitAsync();
            try
            {
                ThrowIfDisposed();
                var scene = Array.Find(_content.Manifest.scenes, x => x.name == sceneName)
                    ?? throw new ArgumentException($"Unknown scene: {sceneName}");
                await EnsureBundle(scene.bundle, new HashSet<string>());
                await SceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Single);
            }
            finally { if (_disposed) Unload(); _gate.Release(); }
        }

        public void Dispose()
        {
            _disposed = true;
            if (_gate.CurrentCount != 0) Unload();
        }

        private async UniTask<AssetBundle> EnsureBundle(string name, HashSet<string> visiting)
        {
            ThrowIfDisposed();
            if (_bundles.TryGetValue(name, out var existing)) return existing;
            if (!visiting.Add(name)) throw new InvalidDataException("Cyclic bundle dependencies.");
            var entry = Array.Find(_content.Manifest.bundles, x => x.name == name)
                ?? throw new InvalidDataException($"Missing bundle: {name}");
            foreach (var dependency in entry.dependencies) await EnsureBundle(dependency, visiting);
            visiting.Remove(name);
            // A verified byte load also supports APK jar paths and avoids opening unchecked cache files.
            var bytes = await _repository.ReadVerified(_content, entry.file, CancellationToken.None);
            var request = AssetBundle.LoadFromMemoryAsync(bytes);
            await request;
            var bundle = request.assetBundle ?? throw new InvalidDataException($"Invalid bundle: {name}");
            if (_disposed) { bundle.Unload(false); ThrowIfDisposed(); }
            _bundles.Add(name, bundle);
            return bundle;
        }

        private void ThrowIfDisposed()
        { if (_disposed) throw new ObjectDisposedException(nameof(AssetBundleAssetService)); }
        private void Unload()
        {
            foreach (var bundle in _bundles.Values) if (bundle != null) bundle.Unload(false);
            _bundles.Clear(); _leases.Clear();
        }
    }
}
