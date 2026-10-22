using System;
using Domain.Run;
using NUnit.Framework;

public sealed class RunStateTests
{
    [Test] public void VictoryCarriesHpAndCapsRecovery()
    {
        var run = new RunState(); run.Start(); run.CompleteBattle(true, 90, 3);
        Assert.That(run.Hp, Is.EqualTo(100)); Assert.That(run.Wins, Is.EqualTo(1));
        Assert.That(run.TotalDamageTaken, Is.EqualTo(10)); Assert.That(run.TotalTurns, Is.EqualTo(3));
        Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
        Assert.Throws<InvalidOperationException>(() => run.CompleteBattle(true, 90, 3));
    }
    [TestCase(RunUpgrade.Attack)] [TestCase(RunUpgrade.Vitality)] [TestCase(RunUpgrade.Guard)]
    public void UpgradeIsAppliedOnceAndEntersNextRound(RunUpgrade upgrade)
    {
        var run = new RunState(); run.Start(); run.CompleteBattle(true, 40, 4); run.ChooseUpgrade(upgrade);
        Assert.That(run.Round, Is.EqualTo(2)); Assert.That(run.Phase, Is.EqualTo(RunPhase.InBattle));
        Assert.That(run.DamageBonus, Is.EqualTo(upgrade == RunUpgrade.Attack ? 5 : 0));
        Assert.That(run.MaxHp, Is.EqualTo(upgrade == RunUpgrade.Vitality ? 125 : 100));
        Assert.That(run.Hp, Is.EqualTo(upgrade == RunUpgrade.Vitality ? 85 : 60));
        Assert.That(run.GuardDamagePercent, Is.EqualTo(upgrade == RunUpgrade.Guard ? 35 : 50));
        Assert.Throws<InvalidOperationException>(() => run.ChooseUpgrade(upgrade));
    }
    [Test] public void ThirdVictoryClearsRunWithoutAnotherUpgradeOrRecovery()
    {
        var run = new RunState(); run.Start();
        run.CompleteBattle(true, 80, 2); run.ChooseUpgrade(RunUpgrade.Guard);
        run.CompleteBattle(true, 60, 3); run.ChooseUpgrade(RunUpgrade.Guard);
        run.CompleteBattle(true, 40, 4);
        Assert.That(run.Phase, Is.EqualTo(RunPhase.Cleared)); Assert.That(run.Wins, Is.EqualTo(3));
        Assert.That(run.Round, Is.EqualTo(3)); Assert.That(run.Hp, Is.EqualTo(40));
        Assert.That(run.TotalTurns, Is.EqualTo(9)); Assert.That(run.TotalDamageTaken, Is.EqualTo(100));
        Assert.That(run.GuardDamagePercent, Is.EqualTo(20));
        Assert.Throws<InvalidOperationException>(() => run.ChooseUpgrade(RunUpgrade.Attack));
    }
    [Test] public void DefeatStopsRunAndRestartClearsAllProgress()
    {
        var run = new RunState(); run.Start(); run.CompleteBattle(true, 60, 3); run.ChooseUpgrade(RunUpgrade.Attack);
        run.CompleteBattle(false, 0, 4);
        Assert.That(run.Phase, Is.EqualTo(RunPhase.Defeated)); Assert.That(run.Wins, Is.EqualTo(1));
        Assert.That(run.TotalDamageTaken, Is.EqualTo(120));
        run.Start();
        Assert.That(run.Round, Is.EqualTo(1)); Assert.That(run.Hp, Is.EqualTo(100));
        Assert.That(run.Wins, Is.Zero); Assert.That(run.DamageBonus, Is.Zero);
        Assert.That(run.TotalTurns, Is.Zero); Assert.That(run.TotalDamageTaken, Is.Zero);
        Assert.That(run.GuardDamagePercent, Is.EqualTo(50));
    }
    [Test] public void InvalidUpgradeDoesNotAdvanceOrChangeStats()
    {
        var run = new RunState(); run.Start(); run.CompleteBattle(true, 50, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => run.ChooseUpgrade((RunUpgrade)999));
        Assert.That(run.Round, Is.EqualTo(1)); Assert.That(run.Hp, Is.EqualTo(70));
        Assert.That(run.Phase, Is.EqualTo(RunPhase.ChoosingUpgrade));
    }
    [Test] public void InvalidBattleResultDoesNotPolluteTotals()
    {
        var run = new RunState(); run.Start();
        Assert.Throws<ArgumentOutOfRangeException>(() => run.CompleteBattle(true, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => run.CompleteBattle(false, 20, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => run.CompleteBattle(true, 101, 1));
        Assert.That(run.TotalTurns, Is.Zero); Assert.That(run.Phase, Is.EqualTo(RunPhase.InBattle));
    }
}
