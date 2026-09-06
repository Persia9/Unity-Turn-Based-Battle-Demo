using Domain.Battle;

namespace Application.Services
{
    public sealed class BattleSession : IBattleSession
    {
        public BattleState State { get; } = new();

        public void Initialize(BattleUnitSnapshot player, BattleUnitSnapshot enemy)
        {
            State.Initialize(player, enemy);
        }
    }
}