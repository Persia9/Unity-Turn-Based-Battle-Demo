using System;
using Domain.Battle;
using NUnit.Framework;

public sealed class BattleActionRulesTests
{
    private static BattleState Create()
    {
        var state = new BattleState();
        state.Initialize(new BattleUnitSnapshot(1, "Hero", 100, 100, 20),
            new BattleUnitSnapshot(2, "Slime", 140, 140, 10));
        return state;
    }
    private static void AdvanceRound(BattleState state) { state.NextTurn(); state.NextTurn(); }
    [Test] public void NormalAndStrongDamageUseDistinctMultipliers()
    {
        var rules = new BattleSystem(); var normal = Create(); var strong = Create();
        Assert.That(rules.ApplyPlayerAction(normal, BattleAction.Attack, 20).Damage, Is.EqualTo(20));
        Assert.That(rules.ApplyPlayerAction(strong, BattleAction.StrongAttack, 20).Damage, Is.EqualTo(40));
    }
    [Test] public void CooldownBlocksExactlyTwoSubsequentPlayerTurns()
    {
        var state = Create(); var rules = new BattleSystem();
        rules.ApplyPlayerAction(state, BattleAction.StrongAttack, 20);
            state.NextTurn();
            Assert.That(state.StrongAttackCooldown, Is.EqualTo(2));
            state.NextTurn();
        Assert.That(state.StrongAttackCooldown, Is.EqualTo(2));
        Assert.That(state.CanUseStrongAttack, Is.False);
        var hp = state.Enemy.Hp;
        Assert.Throws<InvalidOperationException>(() => rules.ApplyPlayerAction(state, BattleAction.StrongAttack, 20));
        Assert.That(state.Enemy.Hp, Is.EqualTo(hp));
        AdvanceRound(state);
        Assert.That(state.StrongAttackCooldown, Is.EqualTo(1));
        Assert.That(state.CanUseStrongAttack, Is.False);
        AdvanceRound(state);
        Assert.That(state.StrongAttackCooldown, Is.Zero);
        Assert.That(state.CanUseStrongAttack, Is.True);
    }
    [Test] public void GuardHalvesOneHitAndRoundsUp()
    {
        var state = Create(); var rules = new BattleSystem();
        rules.ApplyPlayerAction(state, BattleAction.Defend, 20); state.NextTurn();
        Assert.That(rules.ApplyEnemyAction(state, 15).Damage, Is.EqualTo(8));
        Assert.That(state.IsPlayerDefending, Is.False);
        state.NextTurn();
        rules.ApplyPlayerAction(state, BattleAction.Attack, 20); state.NextTurn();
        Assert.That(rules.ApplyEnemyAction(state, 15).Damage, Is.EqualTo(15));
    }
    [Test] public void EnemyForecastMatchesItsThirdTurnStrongAttack()
    {
        var state = Create(); var rules = new BattleSystem();
        Assert.That(state.EnemyIntent, Is.EqualTo(BattleAction.Attack));
        AdvanceRound(state);
        Assert.That(state.EnemyIntent, Is.EqualTo(BattleAction.Attack));
        AdvanceRound(state);
        Assert.That(state.EnemyIntent, Is.EqualTo(BattleAction.StrongAttack));
        rules.ApplyPlayerAction(state, BattleAction.Defend, 20); state.NextTurn();
        var hit = rules.ApplyEnemyAction(state, 10);
        Assert.That(hit.Action, Is.EqualTo(BattleAction.StrongAttack));
        Assert.That(hit.Damage, Is.EqualTo(15));
        state.NextTurn();
        Assert.That(state.EnemyIntent, Is.EqualTo(BattleAction.Attack));
    }
    [Test] public void DefendingDoesNotStackAcrossTurns()
    {
        var state = Create(); var rules = new BattleSystem();
        for (int i = 0; i < 2; i++)
        {
            rules.ApplyPlayerAction(state, BattleAction.Defend, 20); state.NextTurn();
            Assert.That(rules.ApplyEnemyAction(state, 10).Damage, Is.EqualTo(5));
            state.NextTurn();
        }
        Assert.That(state.Player.Hp, Is.EqualTo(90));
    }
    [Test] public void RestartClearsCooldownGuardAndForecast()
    {
        var state = Create(); var rules = new BattleSystem();
        rules.ApplyPlayerAction(state, BattleAction.StrongAttack, 20); AdvanceRound(state);
        rules.ApplyPlayerAction(state, BattleAction.Defend, 20); AdvanceRound(state);
        state.Initialize(new BattleUnitSnapshot(1, "Hero", 100, 100, 20),
            new BattleUnitSnapshot(2, "Slime", 140, 140, 10));
        Assert.That(state.StrongAttackCooldown, Is.Zero);
        Assert.That(state.IsPlayerDefending, Is.False);
        Assert.That(state.EnemyIntent, Is.EqualTo(BattleAction.Attack));
        Assert.That(state.CanUseStrongAttack, Is.True);
    }
    [Test] public void FinishedBattleRejectsEveryAction()
    {
        var state = Create(); var rules = new BattleSystem();
        state.ApplyDamage(BattleSide.Enemy, 140);
        foreach (BattleAction action in Enum.GetValues(typeof(BattleAction)))
            Assert.That(state.CanPlayerChoose(action), Is.False);
        Assert.Throws<InvalidOperationException>(() => rules.ApplyPlayerAction(state, BattleAction.Defend, 20));
    }
    [Test] public void InvalidPowerDoesNotConsumeGuardOrCooldown()
    {
        var state = Create(); var rules = new BattleSystem();
        Assert.Throws<ArgumentOutOfRangeException>(() => rules.ApplyPlayerAction(state, BattleAction.StrongAttack, -1));
        Assert.That(state.CanUseStrongAttack, Is.True);
        Assert.Throws<OverflowException>(() => rules.ApplyPlayerAction(state, BattleAction.StrongAttack, int.MaxValue));
        Assert.That(state.CanUseStrongAttack, Is.True);
        rules.ApplyPlayerAction(state, BattleAction.Defend, 0); state.NextTurn();
        Assert.Throws<ArgumentOutOfRangeException>(() => rules.ApplyEnemyAction(state, -1));
        Assert.That(state.IsPlayerDefending, Is.True);
    }
}
