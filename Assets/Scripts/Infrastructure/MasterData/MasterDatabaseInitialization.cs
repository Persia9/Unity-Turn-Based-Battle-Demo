using System;
using System.Threading;
using Core.Common;
using Cysharp.Threading.Tasks;
using ILogger = Core.Logging.ILogger;

namespace Infrastructure.MasterData
{
    // The same initialization policy is used for real databases and controlled test loaders.
    // No Unity or generated database types are required to verify retries and concurrent calls.
    public sealed class MasterDatabaseInitialization<TDatabase> where TDatabase : class
    {
        private readonly Func<string, UniTask<TDatabase>> _load;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        public TDatabase CoreDb { get; private set; }
        public TDatabase UiDb { get; private set; }
        public MasterDatabaseInitialization(Func<string, UniTask<TDatabase>> load, ILogger logger)
        { _load = load; _logger = logger; }

        public async UniTask InitializeAsync()
        {
            await _gate.WaitAsync();
            try
            {
                if (CoreDb == null) CoreDb = await Load(GameConstants.MasterCoreKey);
                if (UiDb == null) UiDb = await Load(GameConstants.MasterUiKey);
            }
            finally { _gate.Release(); }
        }
        private async UniTask<TDatabase> Load(string key)
        {
            try
            {
                var database = await _load(key);
                if (database == null) throw new InvalidOperationException("Loader returned no database.");
                _logger.Log($"Master database loaded: {key}");
                return database;
            }
            catch (Exception error) { throw new MasterDataLoadException(key, error); }
        }
    }
}
