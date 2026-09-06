using Domain.Battle;

namespace Application.Messages
{
    public readonly struct BattleFinishedMessage
    {
        public BattleSide Winner { get; }

        public BattleFinishedMessage(BattleSide winner)
        {
            Winner = winner;
        }
    }
}