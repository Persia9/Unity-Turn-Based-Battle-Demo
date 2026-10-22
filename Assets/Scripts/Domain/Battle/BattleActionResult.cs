namespace Domain.Battle
{
    public readonly struct BattleActionResult
    {
        public BattleAction Action { get; }
        public BattleSide Actor { get; }
        public BattleSide Target { get; }
        public int Damage { get; }
        public int RemainingHp { get; }
        public BattleActionResult(BattleAction action, BattleSide actor, BattleSide target, int damage, int remainingHp)
        { Action = action; Actor = actor; Target = target; Damage = damage; RemainingHp = remainingHp; }
    }
}
