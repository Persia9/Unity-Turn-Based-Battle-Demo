namespace Domain.Run
{
    public static class RunRules
    {
        public const int ExpeditionBattleCount = 3;
        public const int BetweenBattleRecoveryHp = 20;
        public const int AttackUpgradeBonus = 5;
        public const int VitalityMaxHpIncrease = 25;
        public const int VitalityHealAmount = 25;
        // Percentage points subtracted from the damage taken while defending.
        public const int GuardUpgradeReductionPercent = 15;
        public const int MinimumGuardDamagePercent = 20;
    }
}
