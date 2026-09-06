using MasterMemory;
using MessagePack;

namespace Infrastructure.MasterData.Masters
{
    [MemoryTable("character_master")]
    [MessagePackObject(true)]
    public sealed class CharacterMaster
    {
        [PrimaryKey] public int Id { get; set; }
        public string Name { get; set; }
        public int MaxHp { get; set; }
        public int Attack { get; set; }
        public int DefaultSkillId { get; set; }
    }
}