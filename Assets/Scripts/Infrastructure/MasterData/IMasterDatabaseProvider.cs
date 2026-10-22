using MasterMemory;

namespace Infrastructure.MasterData
{
    public interface IMasterDatabaseProvider
    {
        MemoryDatabase CoreDb { get; }
        MemoryDatabase UiDb { get; }
        bool IsCoreReady { get; }
        bool IsUiReady { get; }
    }
}