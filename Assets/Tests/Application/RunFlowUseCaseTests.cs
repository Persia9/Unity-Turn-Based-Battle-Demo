using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Messages;
using Application.Services;
using Application.UseCases;
using Cysharp.Threading.Tasks;
using Domain.Battle;
using Domain.Run;
using Infrastructure.MasterData.Masters;
using Infrastructure.MasterData.Repositories;
using Infrastructure.Scene;
using MessagePipe;
using NUnit.Framework;

public sealed class RunFlowUseCaseTests
{
    private sealed class Publisher<T> : IPublisher<T>
    {
        public readonly List<T> Messages = new();
        public void Publish(T message) => Messages.Add(message);
    }
    private sealed class Stages : IStageRepository
    {
        public StageMaster Get(int id) => new StageMaster { Id = id, PlayerCharacterId = 1, EnemyCharacterId = id + 1, EnemyPattern = id - 1 };
    }
    private sealed class Characters : ICharacterRepository
    {
        public CharacterMaster Get(int id) => new CharacterMaster { Id = id, Name = "Unit" + id,
            MaxHp = id == 1 ? 100 : id == 2 ? 80 : id == 3 ? 130 : 150, Attack = id == 1 ? 20 : id == 2 ? 8 : 10, DefaultSkillId = id };
    }
    private sealed class Skills : ISkillRepository
    {
        public SkillMaster Get(int id) => new SkillMaster { Id = id, Power = id == 1 ? 20 : id == 2 ? 8 : 10 };
    }
    private sealed class Scenes : ISceneService
    {
        public bool Fail; public int Calls;
        public UniTaskCompletionSource Pending;
        public UniTask LoadScene(string sceneName)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("Simulated scene load failure");
            return Pending != null ? Pending.Task : UniTask.CompletedTask;
        }
    }
    private sealed class Fixture
    {
        public readonly RunSession Run = new(); public readonly BattleSession Battle = new();
        public readonly Scenes Scenes = new(); public readonly Publisher<BattleStartedMessage> Started = new();
        public readonly RunFlowUseCase Flow; public readonly AttackUseCase Attack;
        public Fixture()
        {
            Flow = new RunFlowUseCase(Run, Battle, new Stages(), new Characters(), Scenes, Started);
            Attack = new AttackUseCase(Battle, new BattleSystem(), new Skills(), new Characters(),
                new Publisher<DamageAppliedMessage>(), new Publisher<TurnChangedMessage>(),
                new Publisher<BattleFinishedMessage>(), new Publisher<BattleActionResolvedMessage>());
        }
        public void Play(params BattleAction[] actions)
        {
            foreach(var action in actions)
            {
                Assert.That(Attack.ExecutePlayerAction(action), Is.True);
                if (!Battle.State.IsFinished) Attack.ExecuteEnemyAttack();
            }
            Assert.That(Battle.State.Winner, Is.EqualTo(BattleSide.Player));
            Assert.That(Flow.CompleteBattle(), Is.True);
        }
    }
    [Test] public async Task NewRunInitializesFirstEncounterAndPublishesStart()
    {
        var f = new Fixture(); await f.Flow.StartNewRun();
        Assert.That(f.Battle.State.Enemy.MaxHp, Is.EqualTo(80));
        Assert.That(f.Battle.State.EnemyPattern, Is.EqualTo(EnemyAttackPattern.NormalOnly));
        Assert.That(f.Started.Messages.Count, Is.EqualTo(1)); Assert.That(f.Run.State.Round, Is.EqualTo(1));
    }
    [Test] public async Task DuplicateFinishIsIgnoredAndUpgradeCarriesHp()
    {
        var f = new Fixture(); await f.Flow.StartNewRun();
        f.Battle.State.ApplyDamage(BattleSide.Player, 40); f.Battle.State.ApplyDamage(BattleSide.Enemy, 80);
        Assert.That(f.Flow.CompleteBattle(), Is.True); Assert.That(f.Flow.CompleteBattle(), Is.False);
        await f.Flow.ChooseUpgrade(RunUpgrade.Attack);
        Assert.That(f.Battle.State.Player.Hp, Is.EqualTo(80));
        Assert.That(f.Battle.State.PlayerDamageBonus, Is.EqualTo(5));
        Assert.That(f.Battle.State.Enemy.MaxHp, Is.EqualTo(130));
        Assert.That(f.Battle.State.StrongAttackCooldown, Is.Zero);
        Assert.That(f.Battle.State.IsPlayerDefending, Is.False);
    }
    [Test] public async Task FailedNextSceneCanRetryWithoutDoubleUpgrade()
    {
        var f = new Fixture(); await f.Flow.StartNewRun();
        f.Battle.State.ApplyDamage(BattleSide.Enemy, 80); f.Flow.CompleteBattle(); f.Scenes.Fail = true;
        try { await f.Flow.ChooseUpgrade(RunUpgrade.Vitality); Assert.Fail("Expected failure"); }
        catch (InvalidOperationException) { }
        Assert.That(f.Run.State.Round, Is.EqualTo(2)); Assert.That(f.Run.State.MaxHp, Is.EqualTo(125));
        f.Scenes.Fail = false; await f.Flow.ChooseUpgrade(RunUpgrade.Vitality);
        Assert.That(f.Run.State.Round, Is.EqualTo(2)); Assert.That(f.Run.State.MaxHp, Is.EqualTo(125));
        Assert.That(f.Battle.State.Player.MaxHp, Is.EqualTo(125)); Assert.That(f.Started.Messages.Count, Is.EqualTo(2));
    }
    [Test] public async Task ConcurrentStartDoesNotLoadTwice()
    {
        var f = new Fixture(); f.Scenes.Pending = new UniTaskCompletionSource();
        var pending = f.Flow.StartNewRun(); await f.Flow.StartNewRun();
        Assert.That(f.Scenes.Calls, Is.EqualTo(1)); f.Scenes.Pending.TrySetResult(); await pending;
        Assert.That(f.Started.Messages.Count, Is.EqualTo(1));
    }
    [Test] public async Task UpgradeCannotSkipAnUnfinishedBattle()
    {
        var f = new Fixture(); await f.Flow.StartNewRun();
        try { await f.Flow.ChooseUpgrade(RunUpgrade.Attack); Assert.Fail("Expected rejection"); }
        catch (InvalidOperationException) { }
        Assert.That(f.Run.State.Round, Is.EqualTo(1)); Assert.That(f.Scenes.Calls, Is.EqualTo(1));
    }
    [Test] public async Task ThreeEncounterStrategyClearsRunAndRecordsTotals()
    {
        var f = new Fixture(); await f.Flow.StartNewRun();
        f.Play(BattleAction.StrongAttack, BattleAction.Attack, BattleAction.Attack);
        Assert.That(f.Run.State.Hp, Is.EqualTo(100)); await f.Flow.ChooseUpgrade(RunUpgrade.Attack);
        f.Play(BattleAction.StrongAttack, BattleAction.Attack, BattleAction.Defend, BattleAction.StrongAttack, BattleAction.Attack);
        Assert.That(f.Run.State.Hp, Is.EqualTo(75)); await f.Flow.ChooseUpgrade(RunUpgrade.Attack);
        f.Play(BattleAction.StrongAttack, BattleAction.Defend, BattleAction.Defend, BattleAction.StrongAttack, BattleAction.Attack);
        Assert.That(f.Run.State.Phase, Is.EqualTo(RunPhase.Cleared));
        Assert.That(f.Run.State.Hp, Is.EqualTo(25)); Assert.That(f.Run.State.Wins, Is.EqualTo(3));
        Assert.That(f.Run.State.TotalTurns, Is.EqualTo(13)); Assert.That(f.Run.State.TotalDamageTaken, Is.EqualTo(111));
        Assert.That(f.Flow.CompleteBattle(), Is.False);
        await f.Flow.StartNewRun();
        Assert.That(f.Run.State.TotalTurns, Is.Zero); Assert.That(f.Run.State.DamageBonus, Is.Zero);
        Assert.That(f.Battle.State.Player.Hp, Is.EqualTo(100)); Assert.That(f.Run.State.Round, Is.EqualTo(1));
    }
    [Test] public async Task DefeatCanRestartFreshFromFirstBattle()
    {
        var f = new Fixture(); await f.Flow.StartNewRun();
        f.Battle.State.ApplyDamage(BattleSide.Player, int.MaxValue); f.Flow.CompleteBattle();
        Assert.That(f.Run.State.Phase, Is.EqualTo(RunPhase.Defeated));
        Assert.That(f.Run.State.TotalDamageTaken, Is.EqualTo(100));
        await f.Flow.StartNewRun();
        Assert.That(f.Run.State.Phase, Is.EqualTo(RunPhase.InBattle));
        Assert.That(f.Run.State.TotalDamageTaken, Is.Zero); Assert.That(f.Run.State.Wins, Is.Zero);
    }
    [Test] public async Task GuardUpgradeAffectsRealBattleDamage()
    {
        var f = new Fixture(); await f.Flow.StartNewRun();
        f.Battle.State.ApplyDamage(BattleSide.Enemy, 80); f.Flow.CompleteBattle();
        await f.Flow.ChooseUpgrade(RunUpgrade.Guard);
        f.Attack.ExecutePlayerAction(BattleAction.Defend); f.Attack.ExecuteEnemyAttack();
        Assert.That(f.Battle.State.GuardDamagePercent, Is.EqualTo(35));
        Assert.That(f.Battle.State.Player.Hp, Is.EqualTo(96));
    }

    [TestCase(RunUpgrade.Attack, RunUpgrade.Attack)]
    [TestCase(RunUpgrade.Attack, RunUpgrade.Vitality)]
    [TestCase(RunUpgrade.Attack, RunUpgrade.Guard)]
    [TestCase(RunUpgrade.Vitality, RunUpgrade.Attack)]
    [TestCase(RunUpgrade.Vitality, RunUpgrade.Vitality)]
    [TestCase(RunUpgrade.Vitality, RunUpgrade.Guard)]
    [TestCase(RunUpgrade.Guard, RunUpgrade.Attack)]
    [TestCase(RunUpgrade.Guard, RunUpgrade.Vitality)]
    [TestCase(RunUpgrade.Guard, RunUpgrade.Guard)]
    public async Task EveryUpgradeCombinationHasAWinningForecastStrategy(RunUpgrade first, RunUpgrade second)
    {
        var f = new Fixture(); await f.Flow.StartNewRun();
        for (int round = 1; round <= 3; round++)
        {
            for (int step = 0; step < 30 && !f.Battle.State.IsFinished; step++)
            {
                var state = f.Battle.State; var power = 20 + state.PlayerDamageBonus;
                var action = state.Enemy.Hp <= power ? BattleAction.Attack
                    : state.CanUseStrongAttack && state.Enemy.Hp <= power * 2 ? BattleAction.StrongAttack
                    : state.EnemyIntent == BattleAction.StrongAttack ? BattleAction.Defend
                    : state.CanUseStrongAttack ? BattleAction.StrongAttack : BattleAction.Attack;
                Assert.That(f.Attack.ExecutePlayerAction(action), Is.True);
                if (!state.IsFinished) f.Attack.ExecuteEnemyAttack();
            }
            Assert.That(f.Battle.State.Winner, Is.EqualTo(BattleSide.Player), $"Failed at battle {round}");
            Assert.That(f.Flow.CompleteBattle(), Is.True);
            if (round < 3) await f.Flow.ChooseUpgrade(round == 1 ? first : second);
        }
        Assert.That(f.Run.State.Phase, Is.EqualTo(RunPhase.Cleared));
        Assert.That(f.Run.State.Hp, Is.GreaterThan(0));
    }
}
