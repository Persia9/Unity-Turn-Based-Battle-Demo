using Domain.Battle;

namespace Application.Messages
{
    public readonly struct DamageAppliedMessage
    {
        public BattleSide TargetSide { get; }
        public int Damage { get; }
        public int RemainingHp { get; }

        public DamageAppliedMessage(BattleSide targetSide, int damage, int remainingHp)
        {
            TargetSide = targetSide;
            Damage = damage;
            RemainingHp = remainingHp;
        }
    }
}