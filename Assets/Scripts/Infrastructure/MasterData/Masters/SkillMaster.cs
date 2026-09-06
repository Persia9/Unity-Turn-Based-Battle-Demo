using MasterMemory;
using MessagePack;

namespace Infrastructure.MasterData.Masters
{
    [MemoryTable("skill_master")]
    [MessagePackObject(true)]
    public sealed class SkillMaster
    {
        [PrimaryKey] public int Id { get; set; }
        public string Name { get; set; }
        public int Power { get; set; }
        public string SeEventId { get; set; }
    }
}