using Cysharp.Threading.Tasks;
using MasterMemory;
using ILogger = Core.Logging.ILogger;

namespace Infrastructure.MasterData
{
    public sealed class AddressableMasterDatabaseProvider : IMasterDatabaseProvider
    {
        private readonly MasterDatabaseInitialization<MemoryDatabase> _initialization;
        public MemoryDatabase CoreDb => _initialization.CoreDb;
        public MemoryDatabase UiDb => _initialization.UiDb;
        public bool IsCoreReady => CoreDb != null;
        public bool IsUiReady => UiDb != null;
        public AddressableMasterDatabaseProvider(ILogger logger, IMasterDatabaseLoader loader)
            => _initialization = new MasterDatabaseInitialization<MemoryDatabase>(loader.Load, logger);
        public UniTask InitializeAsync() => _initialization.InitializeAsync();
        public UniTask EnsureUiDbAsync() => InitializeAsync();
    }
}
