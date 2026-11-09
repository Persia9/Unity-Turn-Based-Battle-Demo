using MasterMemory;

namespace Infrastructure.MasterData
{
    public interface IMasterDatabaseProvider
    {
        MemoryDatabase CoreDb { get; }
        MemoryDatabase UiDb { get; }
        MemoryDatabase LocalizationDb { get; }
        bool IsCoreReady { get; }
        bool IsUiReady { get; }
        bool IsLocalizationReady { get; }
    }
}