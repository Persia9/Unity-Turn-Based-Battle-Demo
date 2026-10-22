using Domain.Battle;

namespace Application.Services
{
    public interface IBattleSession
    {
        BattleState State { get; }
        int Revision { get; }
        void Initialize(BattleUnitSnapshot player, BattleUnitSnapshot enemy);
    }
}