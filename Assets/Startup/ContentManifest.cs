using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Startup
{
    [Serializable]
    public sealed class ContentManifest
    {
        public int format = 1;
        public string clientId;
        public string version;
        public string target;
        public PayloadFile[] files = Array.Empty<PayloadFile>();
        public AssemblyFile[] hotAssemblies = Array.Empty<AssemblyFile>();
        public AssemblyFile[] aotAssemblies = Array.Empty<AssemblyFile>();
        public BundleEntry[] bundles = Array.Empty<BundleEntry>();
        public AssetEntry[] assets = Array.Empty<AssetEntry>();
        public SceneEntry[] scenes = Array.Empty<SceneEntry>();

        public void Validate(string expectedClient, string expectedTarget)
        {
            if (hotAssemblies == null || aotAssemblies == null || bundles == null || assets == null || scenes == null || format != 1 || string.IsNullOrWhiteSpace(version) || clientId != expectedClient || target != expectedTarget)
                throw new InvalidDataException("Content is incompatible with this client or platform.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files ?? throw new InvalidDataException("Missing files"))
            {
                ValidatePath(file.path);
                if (!names.Add(file.path) || file.size < 0 || file.sha256 == null || file.sha256.Length != 64 || !System.Linq.Enumerable.All(file.sha256, Uri.IsHexDigit))
                    throw new InvalidDataException("Invalid or duplicate content file.");
            }
            var assemblies = new HashSet<string>();
            foreach (var assembly in hotAssemblies)
                if (!names.Contains(assembly.file) || !assemblies.Add(assembly.name)) throw new InvalidDataException("Invalid assembly entry.");
            if (hotAssemblies.Length != 2 || hotAssemblies[0].name != "BattleDemo.Domain" || hotAssemblies[1].name != "BattleDemo.Runtime")
                throw new InvalidDataException("Unexpected hot-update assembly order.");
            foreach (var assembly in aotAssemblies)
                if (!names.Contains(assembly.file)) throw new InvalidDataException("Missing AOT metadata.");
            var bundleNames = new HashSet<string>();
            foreach (var bundle in bundles)
                if (!bundleNames.Add(bundle.name) || !names.Contains(bundle.file)) throw new InvalidDataException("Invalid bundle entry.");
            foreach (var bundle in bundles)
                foreach (var dependency in bundle.dependencies)
                    if (!bundleNames.Contains(dependency) || dependency == bundle.name) throw new InvalidDataException("Unknown bundle dependency.");
            var keys = new HashSet<string>();
            foreach (var asset in assets)
                if (!keys.Add(asset.key) || !bundleNames.Contains(asset.bundle) || string.IsNullOrEmpty(asset.name)) throw new InvalidDataException("Invalid asset mapping.");
            keys.Clear();
            foreach (var scene in scenes)
                if (!keys.Add(scene.name) || !bundleNames.Contains(scene.bundle) || !scene.path.StartsWith("Assets/Scenes/", StringComparison.Ordinal) || !scene.path.EndsWith(".unity", StringComparison.Ordinal))
                    throw new InvalidDataException("Invalid scene mapping.");
            if (!keys.Contains("Title") || !keys.Contains("Battle")) throw new InvalidDataException("Required scenes missing.");
        }

        public static void ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.StartsWith("/") || path.Contains("\\") || path.Contains(":") || path.Contains("?") || path.Contains("#") || path.Contains("%") || path.IndexOfAny(new[] { '\0', '\r', '\n' }) >= 0)
                throw new InvalidDataException("Invalid content path.");
            foreach (var part in path.Split('/'))
                if (part == ".." || part == "." || part.Length == 0) throw new InvalidDataException("Invalid content path.");
        }

        public static string Hash(byte[] data)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }
        public static void Verify(PayloadFile file, byte[] data)
        {
            if (data.LongLength != file.size || !string.Equals(Hash(data), file.sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Content verification failed: {file.path}");
        }
    }
    [Serializable] public sealed class PayloadFile { public string path; public string sha256; public long size; }
    [Serializable] public sealed class AssemblyFile { public string name; public string file; }
    [Serializable] public sealed class BundleEntry { public string name; public string file; public string[] dependencies = Array.Empty<string>(); }
    [Serializable] public sealed class AssetEntry { public string key; public string bundle; public string name; }
    [Serializable] public sealed class SceneEntry { public string name; public string bundle; public string path; }
}
