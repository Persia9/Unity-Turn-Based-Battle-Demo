using System;

namespace Domain.Battle
{
    public sealed class BattleSystem
    {
        public int CalculateDamage(int attackerAttack)
        {
            if (attackerAttack < 0) throw new ArgumentOutOfRangeException(nameof(attackerAttack));
            return attackerAttack;
        }
        public BattleActionResult ApplyPlayerAction(BattleState state, BattleAction action, int basePower)
        {
            if (!state.CanPlayerChoose(action)) throw new InvalidOperationException("Player action is unavailable.");
            var damage = checked(CalculateDamage(basePower) + state.PlayerDamageBonus);
            if (action == BattleAction.StrongAttack) damage = checked(damage * 2);
            state.SelectPlayerAction(action);
            if (action == BattleAction.Defend) damage = 0;
            else state.ApplyDamage(BattleSide.Enemy, damage);
            return new BattleActionResult(action, BattleSide.Player, BattleSide.Enemy, damage, state.Enemy.Hp);
        }
        public BattleActionResult ApplyEnemyAction(BattleState state, int basePower)
        {
            if (state.TurnCount == 0 || state.IsFinished || state.CurrentTurn != TurnOwner.Enemy)
                throw new InvalidOperationException("Enemy action is unavailable.");
            var action = state.EnemyIntent;
            var damage = CalculateDamage(basePower);
            if (action == BattleAction.StrongAttack) damage = checked(damage * 3);
            if (state.ConsumePlayerDefense()) damage = (int)(((long)damage * state.GuardDamagePercent + 99) / 100);
            state.ApplyDamage(BattleSide.Player, damage);
            return new BattleActionResult(action, BattleSide.Enemy, BattleSide.Player, damage, state.Player.Hp);
        }
    }
}
