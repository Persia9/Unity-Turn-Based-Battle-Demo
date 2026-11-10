using System.Collections.Generic;
using Application.Services;
using R3;
using Domain.Battle;
using Infrastructure.Localization;
using Presentation.Battle.Commands;
using System;
using System.Threading;
using Application.UseCases;
using Core.Logging;
using Cysharp.Threading.Tasks;
using Domain.Run;
using Zenject;

namespace Presentation.Battle
{
    public sealed class BattleViewModel : IDisposable
    {
        private const int MaxLogEntries = 40;

        private readonly ILocalizationService _localization;
        private readonly IBattleSession _session;
        private readonly RunSession _run;
        private readonly PlayPlayerTurnUseCase _playTurn;
        private readonly ILogger _logger;
        private CompositeDisposable _displaySubscriptions;
        private CancellationTokenSource _turnCancellation;
        private bool _opened, _disposed;
        private readonly Dictionary<BattleAction, BattleCommandState> _commands = new();

        private readonly List<string> _logs = new();

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
        public ReactiveProperty<string> RunText { get; } = new(string.Empty);
        public ReactiveProperty<string> PlayerHpText { get; } = new(string.Empty);
        public ReactiveProperty<string> EnemyHpText { get; } = new(string.Empty);
        public ReactiveProperty<GameLanguage> Language { get; } = new(GameLanguage.English);

        public ReactiveProperty<int> CommandsVersion { get; } = new(0);

        [Inject]
        public BattleViewModel(ILocalizationService localization, IBattleSession session,
            RunSession run, PlayPlayerTurnUseCase playTurn, ILogger logger)
        {
            _localization = localization;
            _session = session;
            _run = run;
            _playTurn = playTurn;
            _logger = logger;
        }

        public void Open(CancellationToken lifetime)
        {
            if (_disposed) return;
            Close();
            _opened = true;
            _turnCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
            var state = _session.State;
            PlayerMaxHp.Value = state.Player.MaxHp;
            PlayerHp.Value = state.Player.Hp;
            EnemyMaxHp.Value = state.Enemy.MaxHp;
            EnemyHp.Value = state.Enemy.Hp;
            TurnCount.Value = state.TurnCount;
            _displaySubscriptions = new CompositeDisposable();
            PlayerHp.Subscribe(_ => RefreshHp()).AddTo(_displaySubscriptions);
            PlayerMaxHp.Subscribe(_ => RefreshHp()).AddTo(_displaySubscriptions);
            EnemyHp.Subscribe(_ => RefreshHp()).AddTo(_displaySubscriptions);
            EnemyMaxHp.Subscribe(_ => RefreshHp()).AddTo(_displaySubscriptions);
            _localization.LanguageChanged += RefreshTexts;
            RefreshTexts();
            AppendLog(Text("battle.start"));
        }

        public void Close()
        {
            if (_opened) _localization.LanguageChanged -= RefreshTexts;
            _opened = false;
            _displaySubscriptions?.Dispose();
            _displaySubscriptions = null;
            _turnCancellation?.Cancel();
            _turnCancellation?.Dispose();
            _turnCancellation = null;
        }

        public async UniTask ExecuteAction(BattleAction action)
        {
            if (_disposed || !_opened || _turnCancellation == null) return;
            var token = _turnCancellation.Token;
            try
            {
                await _playTurn.Execute(action, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception error)
            {
                _logger.Error(error.ToString());
                if (!_disposed) AppendLog(error.Message);
            }
        }

        public BattleCommandState GetCommandState(BattleAction action)
            => _commands.TryGetValue(action, out var value) ? value : default;

        public void RefreshCommands(BattleState state)
        {
            CanPlayerAct.Value = state.TurnCount > 0 && !state.IsFinished && state.CurrentTurn == TurnOwner.Player;
            CanStrongAttack.Value = state.CanUseStrongAttack;
            StrongCooldown.Value = state.StrongAttackCooldown;
            EnemyIntentText.Value = Text(state.IsFinished
                ? "battle.finished"
                : state.EnemyIntent == BattleAction.StrongAttack
                    ? "battle.intent.strong"
                    : "battle.intent.normal");
            foreach (BattleAction action in System.Enum.GetValues(typeof(BattleAction)))
                _commands[action] =
                    new BattleCommandState(state.CanPlayerChoose(action), state.GetActionCooldown(action));
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

        public void AppendLog(string log)
        {
            _logs.Add(log);
            if (_logs.Count > MaxLogEntries) _logs.RemoveAt(0);
            LogText.Value = string.Join("\n", _logs);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Close();
            PlayerHp.Dispose();
            PlayerMaxHp.Dispose();
            EnemyHp.Dispose();
            EnemyMaxHp.Dispose();
            TurnCount.Dispose();
            TurnText.Dispose();
            CanPlayerAct.Dispose();
            LogText.Dispose();
            CommandsVersion.Dispose();
            _commands.Clear();
            CanStrongAttack.Dispose();
            StrongCooldown.Dispose();
            EnemyIntentText.Dispose();
            DefenseText.Dispose();
            RunText.Dispose();
            PlayerHpText.Dispose();
            EnemyHpText.Dispose();
            Language.Dispose();
        }

        private void RefreshTexts()
        {
            if (_disposed || !_opened) return;
            var state = _session.State;
            var run = _run.State;
            var enemyKey = state.Enemy.CharacterId switch
            {
                2 => "character.slime", 3 => "character.sentinel", 4 => "character.warden", _ => null
            };
            RunText.Value = Text(run.Mode == RunMode.Endless ? "battle.run.endless" : "battle.run", run.Round,
                enemyKey == null ? state.Enemy.Name : Text(enemyKey), run.DamageBonus, 100 - run.GuardDamagePercent);
            TurnText.Value = Text("battle.turn", state.TurnCount,
                Text(state.CurrentTurn == TurnOwner.Player ? "side.player" : "side.enemy"));
            Language.Value = _localization.CurrentLanguage;
            RefreshHp();
            RefreshCommands(state);
        }

        private void RefreshHp()
        {
            if (!_opened || _disposed) return;
            var state = _session.State;
            PlayerHpText.Value = Text("battle.hero_hp", PlayerHp.Value, PlayerMaxHp.Value,
                state.Player.BasePower + state.PlayerDamageBonus);
            EnemyHpText.Value = Text("battle.enemy_hp", EnemyHp.Value, EnemyMaxHp.Value, state.Enemy.BasePower);
        }

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
    }
}
