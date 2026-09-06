using Cysharp.Threading.Tasks;
using Core.Common;
using MasterMemory;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ILogger = Core.Logging.ILogger;

namespace Infrastructure.MasterData
{
    public sealed class AddressableMasterDatabaseProvider : IMasterDatabaseProvider
    {
        private readonly ILogger _logger;

        public MemoryDatabase CoreDb { get; private set; }
        public MemoryDatabase UiDb { get; private set; }
        public MemoryDatabase AudioDb { get; private set; }

        public bool IsCoreReady => CoreDb != null;
        public bool IsUiReady => UiDb != null;
        public bool IsAudioReady => AudioDb != null;

        public AddressableMasterDatabaseProvider(ILogger logger)
        {
            _logger = logger;
        }

        public async UniTask InitializeAsync()
        {
            if (CoreDb != null) return;
            var coreText = await Addressables.LoadAssetAsync<TextAsset>(GameConstants.MasterCoreKey);
            CoreDb = BuildDatabaseAccessor(coreText.bytes);
            _logger.Log("Master Core DB loaded.");

            // LoadOptionalAsync().Forget();
            await LoadOptionalAsync();
        }

        public async UniTask EnsureUiDbAsync()
        {
            if (UiDb != null) return;
            var text = await Addressables.LoadAssetAsync<TextAsset>(GameConstants.MasterUiKey);
            UiDb = BuildDatabaseAccessor(text.bytes);
            _logger.Log("Master UI DB loaded.");
        }

        public async UniTask EnsureAudioDbAsync()
        {
            if (AudioDb != null) return;
            var text = await Addressables.LoadAssetAsync<TextAsset>(GameConstants.MasterAudioKey);
            AudioDb = BuildDatabaseAccessor(text.bytes);
            _logger.Log("Master Audio DB loaded.");
        }

        private async UniTask LoadOptionalAsync()
        {
            await EnsureUiDbAsync();
            await EnsureAudioDbAsync();
        }

        private MemoryDatabase BuildDatabaseAccessor(byte[] bytes)
        {
            var db = new MemoryDatabase(bytes);
            return db;
        }
    }
}