using System.Collections.Generic;
using R3;
using Domain.Battle;
using Presentation.Battle.Commands;

namespace Presentation.Battle
{
    public sealed class BattleViewModel : System.IDisposable
    {
        public ReactiveProperty<int> PlayerHp { get; } = new(0);
        public ReactiveProperty<int> PlayerMaxHp { get; } = new(0);
        public ReactiveProperty<int> EnemyHp { get; } = new(0);
        public ReactiveProperty<int> EnemyMaxHp { get; } = new(0);
        public ReactiveProperty<int> TurnCount { get; } = new(1);
        public ReactiveProperty<string> TurnText { get; } = new(string.Empty);
        public ReactiveProperty<bool> CanPlayerAct { get; } = new(true);
        public ReactiveProperty<string> LogText { get; } = new(string.Empty);

        public ReactiveProperty<bool> CanStrongAttack { get; } = new(false);
        public ReactiveProperty<int> StrongCooldown { get; } = new(0);
        public ReactiveProperty<string> EnemyIntentText { get; } = new(string.Empty);
        public ReactiveProperty<string> DefenseText { get; } = new(string.Empty);

        public ReactiveProperty<int> CommandsVersion { get; } = new(0);
        private readonly Dictionary<BattleAction, BattleCommandState> _commands = new();
        public BattleCommandState GetCommandState(BattleAction action)
            => _commands.TryGetValue(action, out var value) ? value : default;

        public void RefreshCommands(BattleState state)
        {
            CanPlayerAct.Value = state.TurnCount > 0 && !state.IsFinished && state.CurrentTurn == TurnOwner.Player;
            CanStrongAttack.Value = state.CanUseStrongAttack;
            StrongCooldown.Value = state.StrongAttackCooldown;
            EnemyIntentText.Value = state.IsFinished ? "Battle finished" : state.EnemyIntent == BattleAction.StrongAttack
                ? "Enemy next: STRONG ATTACK x3 - Consider defending!"
                : "Enemy next: Normal attack";
            foreach (BattleAction action in System.Enum.GetValues(typeof(BattleAction)))
                _commands[action] = new BattleCommandState(state.CanPlayerChoose(action), state.GetActionCooldown(action));
            CommandsVersion.Value++;
            DefenseText.Value = state.IsPlayerDefending
                ? $"Guard active: next hit takes {state.GuardDamagePercent}% damage"
                : $"Defend: reduce the next enemy hit by {100 - state.GuardDamagePercent}%";
        }

        private readonly List<string> _logs = new();

        public void AppendLog(string log)
        {
            _logs.Add(log);
            if (_logs.Count > 40) _logs.RemoveAt(0);
            LogText.Value = string.Join("\n", _logs);
        }
        public void Dispose()
        {
            PlayerHp.Dispose(); PlayerMaxHp.Dispose(); EnemyHp.Dispose(); EnemyMaxHp.Dispose();
            TurnCount.Dispose(); TurnText.Dispose(); CanPlayerAct.Dispose(); LogText.Dispose();
            CommandsVersion.Dispose(); _commands.Clear();
            CanStrongAttack.Dispose(); StrongCooldown.Dispose(); EnemyIntentText.Dispose(); DefenseText.Dispose();
        }
    }
}
