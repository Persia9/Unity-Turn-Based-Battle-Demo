using Domain.Battle;

namespace Application.Services
{
    public interface IBattleSession
    {
        BattleState State { get; }
        void Initialize(BattleUnitSnapshot player, BattleUnitSnapshot enemy);
    }
}