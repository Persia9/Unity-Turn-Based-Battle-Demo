using MasterMemory;
using MessagePack;

namespace Infrastructure.MasterData.Masters
{
    [MemoryTable("stage_master")]
    [MessagePackObject(true)]
    public sealed class StageMaster
    {
        [PrimaryKey] public int Id { get; set; }
        public int PlayerCharacterId { get; set; }
        public int EnemyCharacterId { get; set; }
        public int EnemyPattern { get; set; }
        public string DisplayName { get; set; }
    }
}