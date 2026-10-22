using Domain.Battle;

namespace Application.Messages
{
    public readonly struct BattleActionResolvedMessage
    {
        public BattleActionResult Result { get; }
        public BattleActionResolvedMessage(BattleActionResult result) => Result = result;
    }
}
