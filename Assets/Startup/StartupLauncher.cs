using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR
using System.Linq;

#else
using System.IO;
using HybridCLR;
using Infrastructure.Asset;
#endif

namespace Startup
{
    public sealed class StartupLauncher : MonoBehaviour
    {
        private static StartupLauncher _instance;
        [SerializeField] private StartupScreen _screenPrefab;

        [SerializeField,
         Tooltip("Empty uses installed content only; otherwise the platform directory containing manifest.json.")]
        private string _remoteRoot = "";

        private StartupScreen _screen;
#if !UNITY_EDITOR
        private AssetBundleAssetService _assets;
        private ContentRepository _repository;
        private ContentSelection _selection;
#endif

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _screen = Instantiate(_screenPrefab, transform, false);
            _screen.Show(string.Empty);
        }

        private void Start()
        {
            if (_instance == this) Run(destroyCancellationToken).Forget();
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
#if !UNITY_EDITOR
            _assets?.Dispose();
#endif
            _instance = null;
        }

        private async UniTask Run(CancellationToken token)
        {
            try
            {
                var container = ProjectContext.Instance.Container;
                var ready = new StartupReady();
                container.Bind<ILoadingScreen>().FromInstance(_screen).AsSingle();
                container.Bind<StartupReady>().FromInstance(ready).AsSingle();
                Assembly game;
#if UNITY_EDITOR
                if (!string.IsNullOrWhiteSpace(_remoteRoot))
                    Debug.Log(
                        "Editor uses compiled game code and local assets; test remote code updates with the original IL2CPP player.");
                game = AppDomain.CurrentDomain.GetAssemblies()
                           .FirstOrDefault(x => x.GetName().Name == "BattleDemo.Runtime")
                       ?? Assembly.Load("BattleDemo.Runtime");
#else
                var repository = new ContentRepository(
                    Path.Combine(UnityEngine.Application.streamingAssetsPath, "HotUpdate"),
                    Path.Combine(UnityEngine.Application.persistentDataPath, "HotUpdate"));
                _repository = repository;
                var content = await repository.Select(_remoteRoot, token);
                _selection = content;
                foreach (var metadata in content.Manifest.aotAssemblies)
                {
                    var bytes = await repository.ReadVerified(content, metadata.file, token);
                    var error = RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
                    if (error != LoadImageErrorCode.OK) throw new InvalidDataException($"AOT metadata {metadata.name}: {error}");
                }
                game = null;
                foreach (var assembly in content.Manifest.hotAssemblies)
                {
                    var bytes = await repository.ReadVerified(content, assembly.file, token);
                    var loaded = Assembly.Load(bytes);
                    if (loaded.GetName().Name != assembly.name) throw new InvalidDataException("Assembly identity mismatch.");
                    if (assembly.name == "BattleDemo.Runtime") game = loaded;
                }
                _assets = new AssetBundleAssetService(repository, content);
                container.Bind<IAssetService>().FromInstance(_assets).AsSingle();
                container.Bind<ISceneBundleLoader>().FromInstance(_assets).AsSingle();
#endif
                var entry = game?.GetType("HotUpdate.GameEntry", true)
                                .GetMethod("Start", BindingFlags.Public | BindingFlags.Static)
                            ?? throw new MissingMethodException("HotUpdate.GameEntry.Start");
                var start = (UniTask)entry.Invoke(null, new object[] { container });
                await start;
                await ready.Task.AttachExternalCancellation(token);
#if !UNITY_EDITOR
                try { repository.Commit(content); }
                catch (Exception cacheError) { Debug.LogWarning($"Game started, but active cache pointer could not be saved: {cacheError.Message}"); }
#endif
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception error)
            {
                if (token.IsCancellationRequested) return;
#if !UNITY_EDITOR
                try
                {
                    _repository?.Reject(_selection);
                }
                catch (Exception cacheError)
                {
                    Debug.LogWarning(cacheError.Message);
                }
#endif

                _screen.Fail(error is TargetInvocationException invocation
                    ? invocation.InnerException ?? error
                    : error);
            }
        }
    }
}
