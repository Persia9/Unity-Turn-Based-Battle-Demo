using System;

namespace Domain.Run
{
    public enum RunPhase { NotStarted, InBattle, ChoosingUpgrade, Cleared, Defeated }
    public enum RunUpgrade { Attack, Vitality, Guard }

    public sealed class RunState
    {
        public const int BattleCount = 3;
        public const int RecoveryHp = 20;
        public RunPhase Phase { get; private set; }
        public int Round { get; private set; }
        public int Wins { get; private set; }
        public int MaxHp { get; private set; }
        public int Hp { get; private set; }
        public int DamageBonus { get; private set; }
        public int GuardDamagePercent { get; private set; }
        public int TotalTurns { get; private set; }
        public int TotalDamageTaken { get; private set; }
        private int _battleStartingHp;

        public void Start(int maxHp = 100)
        {
            if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp));
            Phase = RunPhase.InBattle; Round = 1; Wins = 0;
            MaxHp = Hp = _battleStartingHp = maxHp;
            DamageBonus = 0; GuardDamagePercent = 50; TotalTurns = TotalDamageTaken = 0;
        }
        public void CompleteBattle(bool won, int remainingHp, int turns)
        {
            if (Phase != RunPhase.InBattle) throw new InvalidOperationException("Run is not in battle.");
            if (remainingHp < 0 || remainingHp > _battleStartingHp || turns < 1 || won != (remainingHp > 0))
                throw new ArgumentOutOfRangeException(nameof(remainingHp));
            TotalTurns += turns; TotalDamageTaken += _battleStartingHp - remainingHp; Hp = remainingHp;
            if (!won) { Phase = RunPhase.Defeated; return; }
            Wins++;
            if (Round == BattleCount) { Phase = RunPhase.Cleared; return; }
            Hp = Math.Min(MaxHp, Hp + RecoveryHp);
            Phase = RunPhase.ChoosingUpgrade;
        }
        public void ChooseUpgrade(RunUpgrade upgrade)
        {
            if (Phase != RunPhase.ChoosingUpgrade) throw new InvalidOperationException("No upgrade is pending.");
            if (!Enum.IsDefined(typeof(RunUpgrade), upgrade)) throw new ArgumentOutOfRangeException(nameof(upgrade));
            switch (upgrade)
            {
                case RunUpgrade.Attack: DamageBonus += 5; break;
                case RunUpgrade.Vitality: MaxHp += 25; Hp = Math.Min(MaxHp, Hp + 25); break;
                case RunUpgrade.Guard: GuardDamagePercent = Math.Max(20, GuardDamagePercent - 15); break;
            }
            Round++; _battleStartingHp = Hp; Phase = RunPhase.InBattle;
        }
    }
}
