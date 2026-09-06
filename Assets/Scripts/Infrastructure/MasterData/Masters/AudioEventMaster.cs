using MasterMemory;
using MessagePack;

namespace Infrastructure.MasterData.Masters
{
    [MemoryTable("audio_event_master")]
    [MessagePackObject(true)]
    public sealed class AudioEventMaster
    {
        [PrimaryKey] public string Id { get; set; }
        public string EventName { get; set; }
    }
}