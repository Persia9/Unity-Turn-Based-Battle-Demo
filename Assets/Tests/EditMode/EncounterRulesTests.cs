using Domain.Battle;
using NUnit.Framework;

public sealed class EncounterRulesTests
{
    private static BattleState Create(EnemyAttackPattern pattern, int bonus = 0, int guardPercent = 50)
    {
        var state = new BattleState();
        state.Initialize(new BattleUnitSnapshot(1, "Hero", 100, 100, 20),
            new BattleUnitSnapshot(2, "Enemy", 200, 200, 10), pattern, bonus, guardPercent);
        return state;
    }
    [Test] public void FirstEncounterNeverSchedulesStrongAttack()
    {
        var state = Create(EnemyAttackPattern.NormalOnly);
        for(int i = 0; i < 10; i++)
        {
            Assert.That(state.EnemyIntent, Is.EqualTo(BattleAction.Attack));
            state.NextTurn(); state.NextTurn();
        }
    }
    [Test] public void BossForecastShowsTwoConsecutiveStrongAttacks()
    {
        var state = Create(EnemyAttackPattern.BossCycle);
        foreach(var action in new[] { BattleAction.Attack, BattleAction.StrongAttack, BattleAction.StrongAttack,
            BattleAction.Attack, BattleAction.Attack, BattleAction.StrongAttack, BattleAction.StrongAttack, BattleAction.Attack })
        {
            Assert.That(state.EnemyIntent, Is.EqualTo(action)); state.NextTurn(); state.NextTurn();
        }
    }
    [Test] public void AttackUpgradeAppliesBeforeStrongMultiplier()
    {
        var state = Create(EnemyAttackPattern.NormalOnly, bonus: 5);
        Assert.That(new BattleSystem().ApplyPlayerAction(state, BattleAction.StrongAttack, 20).Damage, Is.EqualTo(50));
    }
    [Test] public void ImprovedGuardRoundsUpStrongDamage()
    {
        var state = Create(EnemyAttackPattern.BossCycle, guardPercent: 35); var rules = new BattleSystem();
        state.NextTurn(); state.NextTurn();
        rules.ApplyPlayerAction(state, BattleAction.Defend, 20); state.NextTurn();
        Assert.That(rules.ApplyEnemyAction(state, 10).Damage, Is.EqualTo(11));
    }
}
