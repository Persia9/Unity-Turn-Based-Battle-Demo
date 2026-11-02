using System.Collections.Generic;
using Application.Services;
using R3;
using Domain.Battle;
using Presentation.Battle.Commands;

namespace Presentation.Battle
{
    public sealed class BattleViewModel : System.IDisposable
    {
        private readonly ILocalizationService _localization;

        public BattleViewModel(ILocalizationService localization = null)
        {
            _localization = localization;
        }
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
            EnemyIntentText.Value = Text(state.IsFinished ? "battle.finished" : state.EnemyIntent == BattleAction.StrongAttack
                ? "battle.intent.strong"
                : "battle.intent.normal");
            foreach (BattleAction action in System.Enum.GetValues(typeof(BattleAction)))
                _commands[action] = new BattleCommandState(state.CanPlayerChoose(action), state.GetActionCooldown(action));
            CommandsVersion.Value++;
            DefenseText.Value = state.IsPlayerDefending
                ? Text("battle.guard.active", state.GuardDamagePercent)
                : Text("battle.guard.inactive", 100 - state.GuardDamagePercent);
        }

        public string ActionLabel(BattleAction action) => Text(action switch
        {
            BattleAction.Attack => "action.attack",
            BattleAction.Defend => "action.defend",
            BattleAction.StrongAttack => "action.strong",
            _ => "action.unassigned"
        });

        public string CooldownLabel(BattleAction action, int cooldown)
            => Text("action.cooldown", ActionLabel(action), cooldown);

        private string Text(string key, params object[] arguments)
        {
            if (_localization != null) return _localization.Get(key, arguments);
            return key switch
            {
                "battle.finished" => "Battle finished",
                "battle.intent.strong" => "Enemy next: STRONG ATTACK x3 - Consider defending!",
                "battle.intent.normal" => "Enemy next: Normal attack",
                "battle.guard.active" => $"Guard active: next hit takes {arguments[0]}% damage",
                "battle.guard.inactive" => $"Defend: reduce the next enemy hit by {arguments[0]}%",
                "action.attack" => "Attack",
                "action.defend" => "Defend",
                "action.strong" => "Strong attack x2",
                "action.cooldown" => $"{arguments[0]} (CD {arguments[1]})",
                _ => "Unassigned"
            };
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
