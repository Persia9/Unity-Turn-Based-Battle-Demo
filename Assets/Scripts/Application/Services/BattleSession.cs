using Domain.Battle;

namespace Application.Services
{
    public sealed class BattleSession : IBattleSession
    {
        public int Revision { get; private set; }
        public BattleState State { get; } = new();

        public void Initialize(BattleUnitSnapshot player, BattleUnitSnapshot enemy,
            EnemyAttackPattern pattern = EnemyAttackPattern.EveryThirdTurn, int damageBonus = 0, int guardDamagePercent = 50)
        {
            State.Initialize(player, enemy, pattern, damageBonus, guardDamagePercent);
            Revision++;
        }
    }
}