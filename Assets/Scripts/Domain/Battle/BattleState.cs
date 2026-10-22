using System;

namespace Domain.Battle
{
    public sealed class BattleState
    {
        public BattleUnitSnapshot Player { get; private set; }
        public BattleUnitSnapshot Enemy { get; private set; }
        public TurnOwner CurrentTurn { get; private set; }
        public bool IsFinished { get; private set; }
        public BattleSide? Winner { get; private set; }
        public int TurnCount { get; private set; }
        private int _strongReadyOnTurn;
        public EnemyAttackPattern EnemyPattern { get; private set; }
        public int PlayerDamageBonus { get; private set; }
        public int GuardDamagePercent { get; private set; } = 50;
        public bool IsPlayerDefending { get; private set; }
        public int StrongAttackCooldown => Math.Max(0,
            _strongReadyOnTurn - TurnCount - (CurrentTurn == TurnOwner.Enemy ? 1 : 0));
        public bool CanUseStrongAttack => TurnCount > 0 && !IsFinished && CurrentTurn == TurnOwner.Player && StrongAttackCooldown == 0;
        public BattleAction EnemyIntent
        {
            get
            {
                if (TurnCount <= 0) return BattleAction.Attack;
                var strong = EnemyPattern == EnemyAttackPattern.EveryThirdTurn ? TurnCount % 3 == 0
                    : EnemyPattern == EnemyAttackPattern.BossCycle && (TurnCount % 4 == 2 || TurnCount % 4 == 3);
                return strong ? BattleAction.StrongAttack : BattleAction.Attack;
            }
        }

        public int GetActionCooldown(BattleAction action) => action == BattleAction.StrongAttack ? StrongAttackCooldown : 0;

        public bool CanPlayerChoose(BattleAction action)
        {
            if (TurnCount == 0 || IsFinished || CurrentTurn != TurnOwner.Player) return false;
            return action == BattleAction.Attack || action == BattleAction.Defend ||
                (action == BattleAction.StrongAttack && CanUseStrongAttack);
        }

        internal void SelectPlayerAction(BattleAction action)
        {
            if (!CanPlayerChoose(action)) throw new InvalidOperationException("Player action is unavailable.");
            IsPlayerDefending = action == BattleAction.Defend;
            if (action == BattleAction.StrongAttack) _strongReadyOnTurn = TurnCount + 3;
        }

        internal bool ConsumePlayerDefense()
        {
            var defending = IsPlayerDefending;
            IsPlayerDefending = false;
            return defending;
        }

        public void Initialize(BattleUnitSnapshot player, BattleUnitSnapshot enemy,
            EnemyAttackPattern pattern = EnemyAttackPattern.EveryThirdTurn, int damageBonus = 0, int guardDamagePercent = 50)
        {
            if (player.MaxHp <= 0 || player.Hp <= 0 || player.Hp > player.MaxHp)
                throw new ArgumentOutOfRangeException(nameof(player));
            if (enemy.MaxHp <= 0 || enemy.Hp <= 0 || enemy.Hp > enemy.MaxHp)
                throw new ArgumentOutOfRangeException(nameof(enemy));
            if (!Enum.IsDefined(typeof(EnemyAttackPattern), pattern)) throw new ArgumentOutOfRangeException(nameof(pattern));
            if (damageBonus < 0) throw new ArgumentOutOfRangeException(nameof(damageBonus));
            if (guardDamagePercent < 1 || guardDamagePercent > 100) throw new ArgumentOutOfRangeException(nameof(guardDamagePercent));
            EnemyPattern = pattern; PlayerDamageBonus = damageBonus; GuardDamagePercent = guardDamagePercent;
            Player = player;
            Enemy = enemy;
            CurrentTurn = TurnOwner.Player;
            IsFinished = false;
            Winner = null;
            TurnCount = 1;
            _strongReadyOnTurn = 1;
            IsPlayerDefending = false;
        }

        public void ApplyDamage(BattleSide targetSide, int damage)
        {
            if (TurnCount == 0) throw new InvalidOperationException("Battle is not initialized.");
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (targetSide != BattleSide.Player && targetSide != BattleSide.Enemy)
                throw new ArgumentOutOfRangeException(nameof(targetSide));
            if (IsFinished) throw new InvalidOperationException("Battle already finished.");

            if (targetSide == BattleSide.Player)
            {
                var hp = Math.Max(0, Player.Hp - damage);
                Player = new BattleUnitSnapshot(Player.CharacterId, Player.Name, Player.MaxHp, hp, Player.Attack);
                if (hp <= 0) Finish(BattleSide.Enemy);
            }
            else
            {
                var hp = Math.Max(0, Enemy.Hp - damage);
                Enemy = new BattleUnitSnapshot(Enemy.CharacterId, Enemy.Name, Enemy.MaxHp, hp, Enemy.Attack);
                if (hp <= 0) Finish(BattleSide.Player);
            }
        }

        public void NextTurn()
        {
            if (IsFinished) return;

            CurrentTurn = CurrentTurn == TurnOwner.Player ? TurnOwner.Enemy : TurnOwner.Player;
            if (CurrentTurn == TurnOwner.Player)
            {
                TurnCount++;
            }
        }

        private void Finish(BattleSide winner)
        {
            IsFinished = true;
            Winner = winner;
        }
    }
}
