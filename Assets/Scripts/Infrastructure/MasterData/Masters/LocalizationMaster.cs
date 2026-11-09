using MasterMemory;
using MessagePack;

namespace Infrastructure.MasterData.Masters
{
    [MemoryTable("localization_master")]
    [MessagePackObject(true)]
    public sealed class LocalizationMaster
    {
        [PrimaryKey] public string Key { get; set; }
        public string English { get; set; }
        public string Japanese { get; set; }
    }
}