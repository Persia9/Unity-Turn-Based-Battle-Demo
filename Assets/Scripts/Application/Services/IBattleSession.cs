using Domain.Battle;

namespace Application.Services
{
    public interface IBattleSession
    {
        BattleState State { get; }
        int Revision { get; }
        void Initialize(BattleUnitSnapshot player, BattleUnitSnapshot enemy,
            EnemyAttackPattern pattern = EnemyAttackPattern.EveryThirdTurn, int damageBonus = 0, int guardDamagePercent = 50);
    }
}