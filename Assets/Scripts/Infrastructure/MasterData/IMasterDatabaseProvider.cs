using MasterMemory;

namespace Infrastructure.MasterData
{
    public interface IMasterDatabaseProvider
    {
        MemoryDatabase CoreDb { get; }
        MemoryDatabase UiDb { get; }
        MemoryDatabase AudioDb { get; }
        bool IsCoreReady { get; }
        bool IsUiReady { get; }
        bool IsAudioReady { get; }
    }
}