namespace Domain.Run
{
    public static class RunRules
    {
        // 三連戦モードをクリアするために必要な勝利数。
        public const int ExpeditionBattleCount = 3;

        // 勝利後、次の戦闘に進む前に回復するHP。最大HPを超えない。
        public const int BetweenBattleRecoveryHp = 20;

        // 攻撃強化を選ぶたびに攻撃力ボーナスに加算する値。
        public const int AttackUpgradeBonus = 5;

        // 体力強化を選ぶたびに増える最大HP。
        public const int VitalityMaxHpIncrease = 25;

        // 体力強化を選んだときに即時回復するHP。
        public const int VitalityHealAmount = 25;

        // 防御強化を選ぶたびに、防御時の被ダメージ割合を下げるポイント数。
        public const int GuardUpgradeReductionPercent = 15;

        // 防御時に受けるダメージの割合（%）の下限。
        public const int MinimumGuardDamagePercent = 20;
    }
}