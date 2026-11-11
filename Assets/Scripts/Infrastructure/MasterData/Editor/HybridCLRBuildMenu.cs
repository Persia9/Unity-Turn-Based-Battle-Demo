using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Startup;
using HybridCLR;
using HybridCLR.Commands;
using HybridCLR.Settings;
using Infrastructure.Asset;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Infrastructure.MasterData.Editor
{
    public sealed class HybridCLRBuildMenu : IPreprocessBuildWithReport
    {
        private const string StartupScene = "Assets/Scenes/Startup.unity";
        private static readonly string[] HotNames = { "BattleDemo.Domain", "BattleDemo.Runtime" };
        private static bool _preparing;
        private static string Root => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
        private static BuildTarget Target => EditorUserBuildSettings.activeBuildTarget;
        private static string Baseline => Path.Combine(Root, "Artifacts", "HybridCLR", Target.ToString(), Uri.EscapeDataString(PlayerSettings.bundleVersion), "Baseline");
        private static string Installed => Path.Combine(UnityEngine.Application.streamingAssetsPath, "HotUpdate");
        public int callbackOrder => 10000;

        [MenuItem("Tools/HybridCLR Demo/1 Configure")]
        public static void Configure()
        {
            var settings = HybridCLRSettings.instance;
            settings.enable = true;
            settings.hotUpdateAssemblies = HotNames;
            settings.hotUpdateAssemblyDefinitions = Array.Empty<UnityEditorInternal.AssemblyDefinitionAsset>();
            settings.outputLinkFile = "Startup/Generated/link.xml";
            settings.outputAOTGenericReferenceFile = "HybridCLRGenerated/Editor/AOTGenericReferences.cs";
            settings.hybridclrRepoURL = "https://github.com/focus-creative-games/hybridclr";
            settings.il2cppPlusRepoURL = "https://github.com/focus-creative-games/il2cpp_plus";
            HybridCLRSettings.Save();
            var named = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(Target));
            PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(named, ApiCompatibilityLevel.NET_Unity_4_8);
            EditorUserBuildSettings.development = true;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(StartupScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Configured HybridCLR 9.1.0. Run HybridCLR > Installer once, then prepare the demo client.");
        }

        [MenuItem("Tools/HybridCLR Demo/2 Prepare Client Content")]
        public static void PrepareClient()
        {
            if (File.Exists(Path.Combine(Baseline, "state.json")))
                throw new BuildFailedException("This client baseline already exists. Use Build Code And Resource Update, or increment Player Settings > Version for a new client.");
            Configure();
            if (Target != BuildTarget.StandaloneWindows64 && Target != BuildTarget.Android)
                throw new BuildFailedException("Demo packaging currently supports Windows x64 and Android.");
            ArchiveLegacyBundles();
            _preparing = true;
            try
            {
                PrebuildCommand.GenerateAll();
                MasterDataBuildMenu.BuildFromExcel();
                var content = CreateContent(Guid.NewGuid().ToString("N"), "baseline", null);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                var aotInputs = AotInputs();
                var state = new BuildState
                {
                    clientId = ReadManifest(content).clientId,
                    development = EditorUserBuildSettings.development,
                    aotSignature = AotSignature(aotInputs),
                    aotInputs = aotInputs,
                    manifestHash = ContentManifest.Hash(File.ReadAllBytes(Path.Combine(content, "manifest.json")))
                };
                Directory.CreateDirectory(Baseline);
                CopyTree(content, Path.Combine(Baseline, "Content"));
                CopyTree(content, Installed);
                File.WriteAllText(Path.Combine(Baseline, "state.json"), JsonUtility.ToJson(state, true));
                Publish(content);
                AssetDatabase.Refresh();
                Debug.Log($"HybridCLR client content prepared: {Baseline}");
            }
            finally { _preparing = false; }
        }

        [MenuItem("Tools/HybridCLR Demo/3 Build Demo Player")]
        public static void BuildPlayer()
        {
            ValidateBaseline();
            var output = Target == BuildTarget.Android
                ? Path.Combine(Path.GetDirectoryName(Baseline), "Player", "CubeBattle.apk")
                : Path.Combine(Root, "Artifacts", "Player", "Win64", Uri.EscapeDataString(PlayerSettings.bundleVersion), "CubeBattle.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            CopyTree(Path.Combine(Baseline, "Content"), Installed);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { StartupScene }, target = Target, locationPathName = output,
                options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("HybridCLR demo player build failed.");
            Debug.Log($"HybridCLR demo player: {output}");
        }

        [MenuItem("Tools/HybridCLR Demo/4 Build Code And Resource Update")]
        public static void BuildUpdate()
        {
            var state = ValidateBaseline();
            CompileDllCommand.CompileDll(Target, state.development);
            // Do not silently publish code that requires additional stripped AOT APIs.
            var link = Path.Combine(UnityEngine.Application.dataPath, HybridCLRSettings.instance.outputLinkFile);
            var previousLink = File.ReadAllBytes(link);
            try
            {
                LinkGeneratorCommand.GenerateLinkXml(Target);
                if (!File.ReadAllBytes(link).SequenceEqual(previousLink))
                    throw new BuildFailedException("Update changes the AOT API preservation list. Build a new client version instead.");
            }
            finally { File.WriteAllBytes(link, previousLink); }
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(Target);
            var frozen = ReadManifest(Path.Combine(Baseline, "Content"));
            var supplied = new HashSet<string>(frozen.aotAssemblies.Select(x => x.name + ".dll"));
            if (RequiredAotMetadata().Any(name => !supplied.Contains(name)))
                throw new BuildFailedException("Update needs AOT metadata not included in the original client baseline. Prepare a new client version.");
            MasterDataBuildMenu.BuildFromExcel();
            var version = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            var directory = CreateContent(state.clientId, version, Path.Combine(Baseline, "Content"));
            var output = Path.Combine(Path.GetDirectoryName(Baseline), "Updates", version);
            CopyTree(directory, output);
            Publish(output);
            Debug.Log($"Code/resource update published: {output}. Restart the ORIGINAL player to test; do not rebuild it.");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (_preparing || EditorUserBuildSettings.buildScriptsOnly) return;
            var state = ValidateBaseline();
            if (!File.Exists(Path.Combine(Installed, "manifest.json")) ||
                ContentManifest.Hash(File.ReadAllBytes(Path.Combine(Installed, "manifest.json"))) != state.manifestHash)
                throw new BuildFailedException("Installed content differs from this client baseline. Use Tools > HybridCLR Demo > 3 Build Demo Player.");
            var manifest = ReadManifest(Installed);
            foreach (var file in manifest.files) ContentManifest.Verify(file, File.ReadAllBytes(Path.Combine(Installed, file.path)));
        }

        private static string CreateContent(string clientId, string version, string frozenContent)
        {
            var directory = Path.Combine(Root, "Temp", "HybridCLRPayload", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var nativeOutput = Path.Combine(directory, "native"); Directory.CreateDirectory(nativeOutput);
            var builds = new[]
            {
                new AssetBundleBuild
                {
                    assetBundleName = LocalAssetCatalog.BundleName,
                    assetNames = LocalAssetCatalog.Entries.Select(x => x.Path).ToArray(),
                    addressableNames = LocalAssetCatalog.Entries.Select(x => x.Key.ToLowerInvariant()).ToArray()
                },
                new AssetBundleBuild { assetBundleName = "title_scene", assetNames = new[] { "Assets/Scenes/Title.unity" } },
                new AssetBundleBuild { assetBundleName = "battle_scene", assetNames = new[] { "Assets/Scenes/Battle.unity" } }
            };
            var native = BuildPipeline.BuildAssetBundles(nativeOutput, builds,
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, Target);
            if (native == null) throw new BuildFailedException("Hot-update scene/resource bundle build failed.");
            var payload = Path.Combine(directory, "Content"); Directory.CreateDirectory(payload);
            var manifest = new ContentManifest { clientId = clientId, version = version, target = Target.ToString() };
            var files = new List<PayloadFile>();
            PayloadFile Add(string source, string category, string suffix)
            {
                var bytes = File.ReadAllBytes(source); var hash = ContentManifest.Hash(bytes);
                var relative = category + "/" + hash + suffix;
                var output = Path.Combine(payload, relative); Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output, bytes);
                var record = new PayloadFile { path = relative, size = bytes.LongLength, sha256 = hash };
                if (files.All(x => x.path != relative)) files.Add(record);
                return record;
            }
            manifest.hotAssemblies = HotNames.Select(name => new AssemblyFile
            {
                name = name,
                file = Add(Path.Combine(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(Target), name + ".dll"), "code", ".dll.bytes").path
            }).ToArray();
            if (frozenContent == null)
            {
                var names = RequiredAotMetadata();
                manifest.aotAssemblies = names.Select(name => new AssemblyFile
                {
                    name = Path.GetFileNameWithoutExtension(name),
                    file = Add(Path.Combine(SettingsUtil.GetAssembliesPostIl2CppStripDir(Target), name), "aot", ".dll.bytes").path
                }).ToArray();
            }
            else
            {
                var baseline = ReadManifest(frozenContent);
                manifest.aotAssemblies = baseline.aotAssemblies.Select(a => new AssemblyFile
                {
                    name = a.name, file = Add(Path.Combine(frozenContent, a.file), "aot", ".dll.bytes").path
                }).ToArray();
            }
            manifest.bundles = native.GetAllAssetBundles().OrderBy(x => x).Select(name => new BundleEntry
            {
                name = name, file = Add(Path.Combine(nativeOutput, name), "bundles", ".bundle").path,
                dependencies = native.GetAllDependencies(name)
            }).ToArray();
            manifest.assets = LocalAssetCatalog.Entries.Select(x => new AssetEntry { key = x.Key, name = x.Key.ToLowerInvariant(), bundle = LocalAssetCatalog.BundleName }).ToArray();
            manifest.scenes = new[]
            {
                new SceneEntry { name = "Title", bundle = "title_scene", path = "Assets/Scenes/Title.unity" },
                new SceneEntry { name = "Battle", bundle = "battle_scene", path = "Assets/Scenes/Battle.unity" }
            };
            manifest.files = files.OrderBy(x => x.path).ToArray(); manifest.Validate(clientId, Target.ToString());
            File.WriteAllText(Path.Combine(payload, "manifest.json"), JsonUtility.ToJson(manifest, true));
            return payload;
        }

        private static string[] RequiredAotMetadata()
        {
            var generated = Path.Combine(UnityEngine.Application.dataPath, HybridCLRSettings.instance.outputAOTGenericReferenceFile);
            var source = File.ReadAllText(generated);
            var start = source.IndexOf("PatchedAOTAssemblyList", StringComparison.Ordinal);
            if (start < 0) throw new BuildFailedException("AOT generic reference list is missing. Run Prepare Client Content.");
            var list = source.Substring(start);
            list = list.Substring(0, list.IndexOf("};", StringComparison.Ordinal));
            return Regex.Matches(list, "\"([^\"]+\\.dll)\"").Cast<Match>().Select(x => x.Groups[1].Value).Distinct().ToArray();
        }

        private static BuildState ValidateBaseline()
        {
            var file = Path.Combine(Baseline, "state.json");
            if (!File.Exists(file)) throw new BuildFailedException("Run Tools > HybridCLR Demo > 2 Prepare Client Content first.");
            var state = JsonUtility.FromJson<BuildState>(File.ReadAllText(file));
            if (state.development != EditorUserBuildSettings.development)
                throw new BuildFailedException($"Development Build differs from the baseline. Set it to {state.development} and try again.");
            var inputs = AotInputs();
            if (state.aotSignature != AotSignature(inputs))
            {
                var detail = state.aotInputs == null || state.aotInputs.Length == 0
                    ? "This baseline has no per-file fingerprint record."
                    : "Changed AOT inputs: " + string.Join(", ", state.aotInputs.Except(inputs)
                        .Concat(inputs.Except(state.aotInputs))
                        .Select(input => input.Substring(0, input.LastIndexOf(':'))).Distinct());
                throw new BuildFailedException($"AOT inputs differ from preparation. {detail} Restore the original inputs or increment app Version and prepare a new client.");
            }
            return state;
        }

        private static void ArchiveLegacyBundles()
        {
            var source = Path.GetFullPath(Path.Combine(UnityEngine.Application.streamingAssetsPath, "AssetBundles"));
            if (!Directory.Exists(source)) return;
            var allowedSource = Path.GetFullPath(Path.Combine(Root, "Assets", "StreamingAssets")) + Path.DirectorySeparatorChar;
            var archiveRoot = Path.GetFullPath(Path.Combine(Root, "Artifacts", "LegacyAssetBundles"));
            var destination = Path.GetFullPath(Path.Combine(archiveRoot, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
            if (!source.StartsWith(allowedSource, StringComparison.OrdinalIgnoreCase) ||
                !destination.StartsWith(archiveRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("Unexpected generated bundle directory.");
            Directory.CreateDirectory(archiveRoot);
            Directory.Move(source, destination);
            if (File.Exists(source + ".meta")) File.Move(source + ".meta", destination + ".meta");
            Debug.Log($"Archived old local-only bundles outside the Player assets: {destination}");
        }

        private static string[] AotInputs()
        {
            var paths = Directory.GetFiles(Path.Combine(Root, "Assets", "Startup"), "*", SearchOption.AllDirectories)
                .Concat(ThirdPartySources(Path.Combine(Root, "Assets", "ThirdParty")))
                .Concat(Directory.GetFiles(Path.Combine(Root, "Assets", "Packages"), "*.dll", SearchOption.AllDirectories))
                .Concat(new[] { Path.Combine(Root, StartupScene), Path.Combine(Root, "Assets/Resources/ProjectContext.prefab"),
                    Path.Combine(Root, "Packages/manifest.json"), Path.Combine(Root, "Packages/packages-lock.json"),
                    Path.Combine(Root, "Assets/packages.config"), Path.Combine(Root, "ProjectSettings/ProjectSettings.asset"),
                    Path.Combine(Root, "ProjectSettings/HybridCLRSettings.asset") })
                .OrderBy(x => x, StringComparer.Ordinal);
            return paths.Select(path => path.Substring(Root.Length).Replace('\\', '/') + ":" +
                ContentManifest.Hash(File.ReadAllBytes(path))).ToArray();
        }

        private static string AotSignature(string[] inputs)
            => ContentManifest.Hash(Encoding.UTF8.GetBytes(string.Join("\n", inputs)));

        private static IEnumerable<string> ThirdPartySources(string directory)
        {
            var zenjectExtras = Path.Combine(Root, "Assets", "ThirdParty", "Plugins", "Zenject", "OptionalExtras");
            foreach (var file in Directory.EnumerateFiles(directory))
                if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
                    yield return file;

            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                if (string.Equals(directory, zenjectExtras, StringComparison.OrdinalIgnoreCase) &&
                    (Path.GetFileName(child) == "IntegrationTests" || Path.GetFileName(child) == "UnitTests"))
                    continue;

                foreach (var file in ThirdPartySources(child))
                    yield return file;
            }
        }

        private static ContentManifest ReadManifest(string directory)
            => JsonUtility.FromJson<ContentManifest>(File.ReadAllText(Path.Combine(directory, "manifest.json")));
        private static void Publish(string content)
        {
            var output = Path.Combine(Root, "ServerData", "HybridCLR", Target.ToString()); Directory.CreateDirectory(output);
            var manifest = ReadManifest(content);
            foreach (var file in manifest.files)
            {
                var destination = Path.Combine(output, file.path); Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(Path.Combine(content, file.path), destination, true);
            }
            // Publish the manifest last; hashed files from older releases stay available.
            File.Copy(Path.Combine(content, "manifest.json"), Path.Combine(output, "manifest.json"), true);
        }
        private static void CopyTree(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var path in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var output = Path.Combine(destination, path.Substring(source.Length).TrimStart('/', '\\'));
                Directory.CreateDirectory(Path.GetDirectoryName(output)); File.Copy(path, output, true);
            }
        }
        [Serializable] private sealed class BuildState
        {
            public string clientId;
            public bool development;
            public string aotSignature;
            public string[] aotInputs;
            public string manifestHash;
        }
    }
}
