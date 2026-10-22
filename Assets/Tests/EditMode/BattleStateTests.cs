using System;
using Domain.Battle;
using NUnit.Framework;

public sealed class BattleStateTests
{
    private static BattleState Create()
    {
        var state = new BattleState();
        state.Initialize(new BattleUnitSnapshot(1, "Hero", 100, 100, 20),
            new BattleUnitSnapshot(2, "Slime", 80, 80, 15));
        return state;
    }
    [Test] public void TurnsAlternateAndCountRounds()
    {
        var state = Create();
        state.NextTurn();
        Assert.That(state.CurrentTurn, Is.EqualTo(TurnOwner.Enemy));
        Assert.That(state.TurnCount, Is.EqualTo(1));
        state.NextTurn();
        Assert.That(state.CurrentTurn, Is.EqualTo(TurnOwner.Player));
        Assert.That(state.TurnCount, Is.EqualTo(2));
    }
    [TestCase(BattleSide.Enemy, BattleSide.Player)]
    [TestCase(BattleSide.Player, BattleSide.Enemy)]
    public void LethalDamageClampsHpAndDeterminesWinner(BattleSide target, BattleSide winner)
    {
        var state = Create();
        state.ApplyDamage(target, int.MaxValue);
        Assert.That(state.IsFinished, Is.True);
        Assert.That(state.Winner, Is.EqualTo(winner));
        Assert.That(target == BattleSide.Player ? state.Player.Hp : state.Enemy.Hp, Is.Zero);
        state.NextTurn();
        Assert.That(state.CurrentTurn, Is.EqualTo(TurnOwner.Player));
        Assert.Throws<InvalidOperationException>(() => state.ApplyDamage(target, 1));
    }
    [Test] public void NegativeDamageDoesNotHeal()
    {
        var state = Create();
        Assert.Throws<ArgumentOutOfRangeException>(() => state.ApplyDamage(BattleSide.Player, -1));
        Assert.That(state.Player.Hp, Is.EqualTo(100));
    }
    [Test] public void RestartResetsAllBattleState()
    {
        var state = Create();
        state.ApplyDamage(BattleSide.Enemy, 80);
        state.Initialize(new BattleUnitSnapshot(1, "Hero", 100, 100, 20),
            new BattleUnitSnapshot(2, "Slime", 80, 80, 15));
        Assert.That(state.IsFinished, Is.False);
        Assert.That(state.Winner, Is.Null);
        Assert.That(state.Enemy.Hp, Is.EqualTo(80));
        Assert.That(state.TurnCount, Is.EqualTo(1));
    }
    [Test] public void CannotDamageBeforeInitialization()
        => Assert.Throws<InvalidOperationException>(() => new BattleState().ApplyDamage(BattleSide.Enemy, 1));
}
