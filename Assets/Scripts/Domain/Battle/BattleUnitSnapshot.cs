namespace Domain.Battle
{
    public readonly struct BattleUnitSnapshot
    {
        public int CharacterId { get; }
        public string Name { get; }
        public int MaxHp { get; }
        public int Hp { get; }
        public int BasePower { get; }
        public bool IsDead => Hp <= 0;

        public BattleUnitSnapshot(int characterId, string name, int maxHp, int hp, int basePower)
        {
            CharacterId = characterId;
            Name = name;
            MaxHp = maxHp;
            Hp = hp;
            BasePower = basePower;
        }
    }
}
