namespace Domain.Battle
{
    public readonly struct BattleUnitSnapshot
    {
        public int CharacterId { get; }
        public string Name { get; }
        public int MaxHp { get; }
        public int Hp { get; }
        public int Attack { get; }
        public bool IsDead => Hp <= 0;

        public BattleUnitSnapshot(int characterId, string name, int maxHp, int hp, int attack)
        {
            CharacterId = characterId;
            Name = name;
            MaxHp = maxHp;
            Hp = hp;
            Attack = attack;
        }
    }
}