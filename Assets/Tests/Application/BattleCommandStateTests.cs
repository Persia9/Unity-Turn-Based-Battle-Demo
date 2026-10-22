using Domain.Battle;
using NUnit.Framework;
using Presentation.Battle;

public sealed class BattleCommandStateTests
{
    private static BattleState Create()
    {
        var state = new BattleState();
        state.Initialize(new BattleUnitSnapshot(1, "Hero", 100, 100, 20),
            new BattleUnitSnapshot(2, "Slime", 140, 140, 10));
        return state;
    }
    [Test] public void BeforeInitializationEveryCommandIsDisabled()
    {
        using var vm = new BattleViewModel();
        foreach (BattleAction action in System.Enum.GetValues(typeof(BattleAction)))
            Assert.That(vm.GetCommandState(action).CanExecute, Is.False);
    }
    [Test] public void PlayerTurnMakesAllInitialCommandsAvailable()
    {
        using var vm = new BattleViewModel(); vm.RefreshCommands(Create());
        foreach (BattleAction action in System.Enum.GetValues(typeof(BattleAction)))
            Assert.That(vm.GetCommandState(action).CanExecute, Is.True);
    }
    [Test] public void CooldownOnlyDisablesTheAssignedStrongAction()
    {
        var state = Create(); new BattleSystem().ApplyPlayerAction(state, BattleAction.StrongAttack, 20);
        state.NextTurn(); state.NextTurn();
        using var vm = new BattleViewModel(); vm.RefreshCommands(state);
        Assert.That(vm.GetCommandState(BattleAction.StrongAttack).CanExecute, Is.False);
        Assert.That(vm.GetCommandState(BattleAction.StrongAttack).Cooldown, Is.EqualTo(2));
        Assert.That(vm.GetCommandState(BattleAction.Attack).CanExecute, Is.True);
        Assert.That(vm.GetCommandState(BattleAction.Defend).CanExecute, Is.True);
    }
    [Test] public void EnemyTurnDisablesEveryCommand()
    {
        var state = Create(); state.NextTurn();
        using var vm = new BattleViewModel(); vm.RefreshCommands(state);
        foreach (BattleAction action in System.Enum.GetValues(typeof(BattleAction)))
            Assert.That(vm.GetCommandState(action).CanExecute, Is.False);
    }
    [Test] public void FinishedBattleDisablesEveryCommand()
    {
        var state = Create(); state.ApplyDamage(BattleSide.Enemy, 140);
        using var vm = new BattleViewModel(); vm.RefreshCommands(state);
        foreach (BattleAction action in System.Enum.GetValues(typeof(BattleAction)))
            Assert.That(vm.GetCommandState(action).CanExecute, Is.False);
    }
    [Test] public void RefreshAfterRestartClearsOldCooldownState()
    {
        var state = Create(); var rules = new BattleSystem();
        rules.ApplyPlayerAction(state, BattleAction.StrongAttack, 20); state.NextTurn();
        using var vm = new BattleViewModel(); vm.RefreshCommands(state);
        Assert.That(vm.GetCommandState(BattleAction.StrongAttack).CanExecute, Is.False);
        vm.RefreshCommands(Create());
        Assert.That(vm.GetCommandState(BattleAction.StrongAttack).CanExecute, Is.True);
        Assert.That(vm.GetCommandState(BattleAction.StrongAttack).Cooldown, Is.Zero);
    }
    [Test] public void UnknownActionFailsClosed()
    {
        using var vm = new BattleViewModel(); vm.RefreshCommands(Create());
        Assert.That(vm.GetCommandState((BattleAction)999).CanExecute, Is.False);
    }
}
