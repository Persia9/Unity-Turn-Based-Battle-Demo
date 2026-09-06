using MasterMemory;
using MessagePack;

namespace Infrastructure.MasterData.Masters
{
    [MemoryTable("ui_master")]
    [MessagePackObject(true)]
    public sealed class UIMaster
    {
        [PrimaryKey] public string Id { get; set; }
        public string PrefabKey { get; set; }
        public int Layer { get; set; }
        public int Type { get; set; }
        public bool Cache { get; set; }
        public bool Singleton { get; set; }
        public bool BlockInput { get; set; }
    }
}