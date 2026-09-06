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

        public void Initialize(BattleUnitSnapshot player, BattleUnitSnapshot enemy)
        {
            Player = player;
            Enemy = enemy;
            CurrentTurn = TurnOwner.Player;
            IsFinished = false;
            Winner = null;
            TurnCount = 1;
        }

        public void ApplyDamage(BattleSide targetSide, int damage)
        {
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