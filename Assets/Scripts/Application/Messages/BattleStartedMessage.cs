namespace Application.Messages
{
    public readonly struct BattleStartedMessage
    {
        public int PlayerCharacterId { get; }
        public int EnemyCharacterId { get; }

        public BattleStartedMessage(int playerCharacterId, int enemyCharacterId)
        {
            PlayerCharacterId = playerCharacterId;
            EnemyCharacterId = enemyCharacterId;
        }
    }
}