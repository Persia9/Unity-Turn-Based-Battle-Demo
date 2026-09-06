using Domain.Battle;

namespace Application.Messages
{
    public readonly struct TurnChangedMessage
    {
        public TurnOwner TurnOwner { get; }
        public int TurnCount { get; }

        public TurnChangedMessage(TurnOwner turnOwner, int turnCount)
        {
            TurnOwner = turnOwner;
            TurnCount = turnCount;
        }
    }
}