using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Messages;
using Application.Services;
using Application.UseCases;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using Infrastructure.MasterData.Masters;
using Infrastructure.MasterData.Repositories;
using NUnit.Framework;
using MessagePipe;

public sealed class BattleUseCaseTests
{
    private sealed class Recorder<T> : IPublisher<T>
    {
        public readonly List<T> Messages = new();
        public Action<T> OnPublish;
        public void Publish(T message) { Messages.Add(message); OnPublish?.Invoke(message); }
    }
    private sealed class Characters : ICharacterRepository
    {
        public CharacterMaster Get(int id) => new CharacterMaster { Id = id, DefaultSkillId = id };
    }
    private sealed class Skills : ISkillRepository
    {
        public bool Fail;
        public SkillMaster Get(int id)
        {
            if (Fail) throw new InvalidOperationException("Missing skill");
            return new SkillMaster { Id = id, Power = id == 1 ? 20 : 15 };
        }
    }
    private sealed class ControlledDelay : ITurnDelay
    {
        public int Calls;
        public readonly UniTaskCompletionSource Completion = new();
        // Deliberately ignore cancellation: the UseCase must also check after awaiting.
        public UniTask Wait(CancellationToken cancellationToken) { Calls++; return Completion.Task; }
    }
    private sealed class Fixture
    {
        public readonly BattleSession Session = new();
        public readonly Skills Skills = new();
        public readonly Recorder<DamageAppliedMessage> Damage = new();
        public readonly Recorder<TurnChangedMessage> Turns = new();
        public readonly Recorder<BattleFinishedMessage> Finish = new();
        public readonly List<string> Order = new();
        public readonly AttackUseCase Attack;
        public Fixture(int enemyHp = 80, int playerHp = 100)
        {
            Restart(enemyHp, playerHp);
            Damage.OnPublish = _ => Order.Add("damage");
            Turns.OnPublish = _ => Order.Add("turn");
            Finish.OnPublish = _ => Order.Add("finish");
            Attack = new AttackUseCase(Session, new BattleSystem(), Skills, new Characters(), Damage, Turns, Finish);
        }
        public void Restart(int enemyHp = 80, int playerHp = 100) => Session.Initialize(
            new BattleUnitSnapshot(1, "Hero", 100, playerHp, 20),
            new BattleUnitSnapshot(2, "Slime", 80, enemyHp, 15));
    }

    [Test] public void NormalAttackPublishesDamageThenTurn()
    {
        var f = new Fixture();
        Assert.That(f.Attack.ExecutePlayerAttack(), Is.True);
        Assert.That(f.Session.State.Enemy.Hp, Is.EqualTo(60));
        Assert.That(f.Damage.Messages[0].TargetSide, Is.EqualTo(BattleSide.Enemy));
        Assert.That(f.Damage.Messages[0].RemainingHp, Is.EqualTo(60));
        Assert.That(f.Order, Is.EqualTo(new[] { "damage", "turn" }));
        Assert.That(f.Finish.Messages, Is.Empty);
    }
    [Test] public void WrongTurnAndRepeatedInputDoNothing()
    {
        var f = new Fixture();
        Assert.That(f.Attack.ExecuteEnemyAttack(), Is.False);
        f.Attack.ExecutePlayerAttack();
        Assert.That(f.Attack.ExecutePlayerAttack(), Is.False);
        Assert.That(f.Damage.Messages.Count, Is.EqualTo(1));
        Assert.That(f.Session.State.Enemy.Hp, Is.EqualTo(60));
    }
    [Test] public void VictoryPublishesFinishOnceAndRejectsAllFurtherAttacks()
    {
        var f = new Fixture(enemyHp: 20);
        f.Attack.ExecutePlayerAttack();
        Assert.That(f.Attack.ExecutePlayerAttack(), Is.False);
        Assert.That(f.Attack.ExecuteEnemyAttack(), Is.False);
        Assert.That(f.Order, Is.EqualTo(new[] { "damage", "finish" }));
        Assert.That(f.Finish.Messages.Count, Is.EqualTo(1));
        Assert.That(f.Finish.Messages[0].Winner, Is.EqualTo(BattleSide.Player));
        Assert.That(f.Session.State.Player.Hp, Is.EqualTo(100));
    }
    [Test] public void DefeatPublishesFinishOnceWithoutAdvancingTurn()
    {
        var f = new Fixture(playerHp: 15);
        f.Attack.ExecutePlayerAttack();
        f.Attack.ExecuteEnemyAttack();
        Assert.That(f.Attack.ExecuteEnemyAttack(), Is.False);
        Assert.That(f.Finish.Messages.Count, Is.EqualTo(1));
        Assert.That(f.Finish.Messages[0].Winner, Is.EqualTo(BattleSide.Enemy));
        Assert.That(f.Session.State.TurnCount, Is.EqualTo(1));
    }
    [Test] public void SynchronousSubscriberCannotReenterAttack()
    {
        var f = new Fixture();
        f.Damage.OnPublish = _ => { f.Attack.ExecutePlayerAttack(); f.Attack.ExecuteEnemyAttack(); };
        f.Attack.ExecutePlayerAttack();
        Assert.That(f.Damage.Messages.Count, Is.EqualTo(1));
        Assert.That(f.Session.State.Player.Hp, Is.EqualTo(100));
    }
    [Test] public void MissingSkillDoesNotDamageAndAllowsRetry()
    {
        var f = new Fixture(); f.Skills.Fail = true;
        Assert.Throws<InvalidOperationException>(() => f.Attack.ExecutePlayerAttack());
        Assert.That(f.Damage.Messages, Is.Empty);
        Assert.That(f.Session.State.CurrentTurn, Is.EqualTo(TurnOwner.Player));
        f.Skills.Fail = false;
        Assert.That(f.Attack.ExecutePlayerAttack(), Is.True);
    }
    [Test] public void UninitializedBattleIgnoresInput()
    {
        var f = new Fixture();
        var attack = new AttackUseCase(new BattleSession(), new BattleSystem(), f.Skills,
            new Characters(), f.Damage, f.Turns, f.Finish);
        Assert.That(attack.ExecutePlayerAttack(), Is.False);
        Assert.That(f.Damage.Messages, Is.Empty);
    }
    [Test] public void RestartResetsStateAndAllowsAnotherFinish()
    {
        var f = new Fixture(enemyHp: 20); f.Attack.ExecutePlayerAttack();
        var revision = f.Session.Revision; f.Restart(enemyHp: 20);
        Assert.That(f.Session.Revision, Is.GreaterThan(revision));
        Assert.That(f.Session.State.IsFinished, Is.False);
        Assert.That(f.Session.State.Winner, Is.Null);
        Assert.That(f.Session.State.TurnCount, Is.EqualTo(1));
        f.Attack.ExecutePlayerAttack();
        Assert.That(f.Finish.Messages.Count, Is.EqualTo(2));
    }
    [Test] public async Task FullTurnRunsOneEnemyActionDespiteRepeatedClick()
    {
        var f = new Fixture(); var delay = new ControlledDelay();
        using var flow = new PlayPlayerTurnUseCase(f.Attack, f.Session, delay);
        var pending = flow.Execute(CancellationToken.None);
        await flow.Execute(CancellationToken.None);
        Assert.That(delay.Calls, Is.EqualTo(1));
        Assert.That(f.Damage.Messages.Count, Is.EqualTo(1));
        delay.Completion.TrySetResult(); await pending;
        Assert.That(f.Damage.Messages.Count, Is.EqualTo(2));
        Assert.That(f.Session.State.Player.Hp, Is.EqualTo(85));
        Assert.That(f.Session.State.CurrentTurn, Is.EqualTo(TurnOwner.Player));
    }
    [Test] public async Task ClosingViewCancelsEnemyAction()
    {
        var f = new Fixture(); var delay = new ControlledDelay();
        using var cancellation = new CancellationTokenSource();
        using var flow = new PlayPlayerTurnUseCase(f.Attack, f.Session, delay);
        var pending = flow.Execute(cancellation.Token);
        cancellation.Cancel(); delay.Completion.TrySetResult(); await pending;
        Assert.That(f.Damage.Messages.Count, Is.EqualTo(1));
        Assert.That(f.Session.State.Player.Hp, Is.EqualTo(100));
    }
    [Test] public async Task DisposingSceneCancelsEnemyActionAndRejectsNewInput()
    {
        var f = new Fixture(); var delay = new ControlledDelay();
        var flow = new PlayPlayerTurnUseCase(f.Attack, f.Session, delay);
        var pending = flow.Execute(CancellationToken.None);
        flow.Dispose(); delay.Completion.TrySetResult(); await pending;
        await flow.Execute(CancellationToken.None);
        Assert.That(f.Damage.Messages.Count, Is.EqualTo(1));
    }
    [Test] public async Task OldDelayedActionCannotAttackRestartedBattle()
    {
        var f = new Fixture(); var delay = new ControlledDelay();
        using var flow = new PlayPlayerTurnUseCase(f.Attack, f.Session, delay);
        var pending = flow.Execute(CancellationToken.None);
        f.Restart();
        // Make the new battle reach the enemy turn; a turn-only guard would fail here.
        f.Attack.ExecutePlayerAttack();
        delay.Completion.TrySetResult(); await pending;
        Assert.That(f.Session.State.Player.Hp, Is.EqualTo(100));
        Assert.That(f.Damage.Messages.Count, Is.EqualTo(2));
    }
    [Test] public async Task LethalPlayerAttackDoesNotScheduleEnemy()
    {
        var f = new Fixture(enemyHp: 20); var delay = new ControlledDelay();
        using var flow = new PlayPlayerTurnUseCase(f.Attack, f.Session, delay);
        await flow.Execute(CancellationToken.None);
        Assert.That(delay.Calls, Is.Zero);
        Assert.That(f.Finish.Messages.Count, Is.EqualTo(1));
    }
    [Test] public async Task PreCancelledInputDoesNotAttack()
    {
        var f = new Fixture(); var delay = new ControlledDelay();
        using var flow = new PlayPlayerTurnUseCase(f.Attack, f.Session, delay);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await flow.Execute(cancellation.Token);
        Assert.That(f.Damage.Messages, Is.Empty);
    }
}

