using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Startup
{
    public sealed class ContentRepository
    {
        private readonly string _packagedRoot;
        private readonly string _cacheRoot;
        private readonly int _timeout;
        private string _activePointer;
        private readonly Func<string, CancellationToken, UniTask<byte[]>> _read;
        private readonly Func<byte[], ContentManifest> _parse;
        private readonly Action<string> _warn;

        public ContentRepository(string packagedRoot, string cacheRoot, int timeout = 15)
            : this(packagedRoot, cacheRoot, timeout, null, null, null)
        {
        }

        internal ContentRepository(string packagedRoot, string cacheRoot, int timeout,
            Func<string, CancellationToken, UniTask<byte[]>> read, Func<byte[], ContentManifest> parse,
            Action<string> warn)
        {
            _packagedRoot = packagedRoot;
            _cacheRoot = cacheRoot;
            _timeout = timeout;
            _read = read ?? Read;
            _parse = parse ?? Parse;
            _warn = warn ?? (message => Debug.LogWarning(message));
        }

        public async UniTask<ContentSelection> Select(string remoteRoot, CancellationToken token)
        {
            var packagedBytes = await _read(Join(_packagedRoot, "manifest.json"), token);
            var baseline = _parse(packagedBytes);
            baseline.Validate(baseline.clientId, baseline.target);
            if (string.IsNullOrEmpty(baseline.clientId)) throw new InvalidDataException("Missing client identity.");
            var cache = Path.Combine(_cacheRoot, baseline.clientId);
            Directory.CreateDirectory(cache);
            _activePointer = Path.Combine(cache, "active.txt");
            var selected = new ContentSelection(baseline, _packagedRoot, null);
            if (File.Exists(_activePointer))
            {
                try
                {
                    var id = File.ReadAllText(_activePointer).Trim();
                    if (id.Length != 64 || !System.Linq.Enumerable.All(id, Uri.IsHexDigit))
                        throw new InvalidDataException("Invalid cached version.");
                    var path = Path.Combine(cache, id);
                    var bytes = File.ReadAllBytes(Path.Combine(path, "manifest.json"));
                    if (ContentManifest.Hash(bytes) != id) throw new InvalidDataException("Cached manifest changed.");
                    var manifest = _parse(bytes);
                    manifest.Validate(baseline.clientId, baseline.target);
                    await VerifyAll(manifest, path, token);
                    selected = new ContentSelection(manifest, path, id);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception error)
                {
                    _warn($"Cached update ignored: {error.Message}");
                }
            }

            if (string.IsNullOrWhiteSpace(remoteRoot)) return selected;
            remoteRoot = remoteRoot.Replace("[BuildTarget]", baseline.target);
            try
            {
                if (!Uri.TryCreate(remoteRoot, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                    throw new InvalidDataException("Remote content URL must use HTTP or HTTPS.");
                var bytes = await _read(Join(remoteRoot, "manifest.json"), token);
                var manifest = _parse(bytes);
                manifest.Validate(baseline.clientId, baseline.target);
                if (manifest.aotAssemblies.Length != baseline.aotAssemblies.Length || manifest.aotAssemblies.Any(a =>
                        !baseline.aotAssemblies.Any(b => b.name == a.name && b.file == a.file &&
                                                         baseline.files.Any(original => original.path == b.file &&
                                                             manifest.files.Any(updated =>
                                                                 updated.path == a.file &&
                                                                 updated.sha256 == original.sha256 &&
                                                                 updated.size == original.size)))))
                    throw new InvalidDataException("Update must use this client's original AOT metadata.");
                var id = ContentManifest.Hash(bytes);
                if (File.Exists(Path.Combine(cache, "rejected-" + id)))
                    throw new InvalidDataException(
                        "This update failed to start previously; waiting for a new version.");
                if (selected.CacheId == id) return selected;
                // Cache directories are immutable versions. The active pointer changes only after game startup succeeds.
                var directory = Path.Combine(cache, id);
                Directory.CreateDirectory(directory);
                foreach (var file in manifest.files)
                {
                    var output = Path.Combine(directory, file.path);
                    if (File.Exists(output))
                    {
                        try
                        {
                            ContentManifest.Verify(file, File.ReadAllBytes(output));
                            continue;
                        }
                        catch (InvalidDataException)
                        {
                        }
                    }

                    var data = await _read(Join(remoteRoot, file.path), token);
                    ContentManifest.Verify(file, data);
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                    File.WriteAllBytes(output + ".tmp", data);
                    if (File.Exists(output)) File.Delete(output);
                    File.Move(output + ".tmp", output);
                }

                File.WriteAllBytes(Path.Combine(directory, "manifest.json"), bytes);
                return new ContentSelection(manifest, directory, id);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                _warn($"Update unavailable, using verified previous/installed content: {error.Message}");
                return selected;
            }
        }

        public void Commit(ContentSelection selection)
        {
            if (selection.CacheId == null) return;
            File.WriteAllText(_activePointer + ".tmp", selection.CacheId);
            if (File.Exists(_activePointer)) File.Replace(_activePointer + ".tmp", _activePointer, null);
            else File.Move(_activePointer + ".tmp", _activePointer);
        }

        public void Reject(ContentSelection selection)
        {
            if (selection?.CacheId == null) return;
            var cache = Path.GetDirectoryName(_activePointer);
            File.WriteAllText(Path.Combine(cache, "rejected-" + selection.CacheId), "Startup failed");
            if (File.Exists(_activePointer) && File.ReadAllText(_activePointer).Trim() == selection.CacheId)
                File.Delete(_activePointer);
        }

        public async UniTask<byte[]> ReadVerified(ContentSelection selection, string relativePath,
            CancellationToken token)
        {
            var file = Array.Find(selection.Manifest.files, x => x.path == relativePath)
                       ?? throw new InvalidDataException($"File not in manifest: {relativePath}");
            var bytes = await _read(Join(selection.Root, relativePath), token);
            ContentManifest.Verify(file, bytes);
            return bytes;
        }

        private async UniTask VerifyAll(ContentManifest manifest, string directory, CancellationToken token)
        {
            foreach (var file in manifest.files)
            {
                token.ThrowIfCancellationRequested();
                ContentManifest.Verify(file, await _read(Join(directory, file.path), token));
            }
        }

        private async UniTask<byte[]> Read(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!path.Contains("://") && !path.StartsWith("jar:", StringComparison.Ordinal))
                return File.ReadAllBytes(path);
            using var request = UnityWebRequest.Get(path);
            request.timeout = _timeout;
            await request.SendWebRequest().WithCancellation(token);
            return request.downloadHandler.data;
        }

        private static ContentManifest Parse(byte[] bytes)
            => JsonUtility.FromJson<ContentManifest>(Encoding.UTF8.GetString(bytes)) ??
               throw new InvalidDataException("Invalid manifest.");

        public static string Join(string root, string relativePath) => root.TrimEnd('/', '\\') + "/" + relativePath;
    }

    public sealed class ContentSelection
    {
        public ContentManifest Manifest { get; }
        public string Root { get; }
        public string CacheId { get; }

        public ContentSelection(ContentManifest manifest, string root, string cacheId)
        {
            Manifest = manifest;
            Root = root;
            CacheId = cacheId;
        }
    }
}
