using System;
using SceneFlow.Battle;
using Application.Services;
using Application.UseCases;
using Core.Logging;
using Cysharp.Threading.Tasks;
using Domain.Run;
using Infrastructure.Localization;
using R3;
using Zenject;

namespace Presentation.Battle.Result
{
    public sealed class ResultPopupViewModel : IDisposable
    {
        private readonly RunSession _run;
        private readonly ILocalizationService _localization;
        private readonly ILogger _logger;
        private readonly Func<RunUpgrade, UniTask> _choose;
        private readonly Func<RunMode, UniTask> _retry;
        private readonly Func<UniTask> _return;
        private ResultPopupContext _context;
        private Summary _summary;
        private RunUpgrade? _selected;
        private string _error = string.Empty;
        private bool _busy, _opened, _disposed;

        public sealed class DisplayState
        {
            public string Title { get; internal set; }
            public string Heading { get; internal set; }
            public string Progress { get; internal set; }
            public string Statistics { get; internal set; }
            public string Hp { get; internal set; }
            public string Attack { get; internal set; }
            public string Guard { get; internal set; }
            public string Guidance { get; internal set; }
            public string Error { get; internal set; }
            public string Retry { get; internal set; }
            public string Return { get; internal set; }
            public string AttackUpgrade { get; internal set; }
            public string VitalityUpgrade { get; internal set; }
            public string GuardUpgrade { get; internal set; }
            public GameLanguage Language { get; internal set; }
            public bool ShowUpgrades { get; internal set; }
            public bool CanInteract { get; internal set; }
            public RunUpgrade? SelectedUpgrade { get; internal set; }

            public bool CanChoose(RunUpgrade upgrade) => CanInteract && ShowUpgrades &&
                                                         (!SelectedUpgrade.HasValue || SelectedUpgrade == upgrade);

            public string UpgradeLabel(RunUpgrade upgrade) => upgrade switch
            {
                RunUpgrade.Attack => AttackUpgrade,
                RunUpgrade.Vitality => VitalityUpgrade,
                RunUpgrade.Guard => GuardUpgrade,
                _ => string.Empty
            };
        }

        public ReactiveProperty<DisplayState> State { get; } = new(new DisplayState());

        [Inject]
        public ResultPopupViewModel(RunSession run, ReturnToTitleUseCase returnToTitle,
            BattleSceneEntryPoint battleScene, ILocalizationService localization, ILogger logger)
            : this(run, battleScene.ChooseUpgrade, battleScene.Retry, returnToTitle.Execute, localization, logger)
        {
        }

        internal ResultPopupViewModel(RunSession run, Func<RunUpgrade, UniTask> choose,
            Func<RunMode, UniTask> retry, Func<UniTask> returnToTitle, ILocalizationService localization,
            ILogger logger)
        {
            _run = run;
            _choose = choose;
            _retry = retry;
            _return = returnToTitle;
            _localization = localization;
            _logger = logger;
        }

        public void Open(ResultPopupContext context)
        {
            if (_disposed) return;
            if (!_opened)
            {
                _localization.LanguageChanged += Publish;
                _opened = true;
            }

            if (_busy)
            {
                Publish();
                return;
            }

            _context = context;
            _summary = new Summary(_run.State);
            _selected = null;
            _error = string.Empty;
            Publish();
        }

        public void Close()
        {
            if (!_opened) return;
            _localization.LanguageChanged -= Publish;
            _opened = false;
        }

        public UniTask Choose(RunUpgrade upgrade)
        {
            if (!CanExecute || !State.Value.CanChoose(upgrade)) return UniTask.CompletedTask;
            _selected = upgrade;
            return Execute(() => _choose(upgrade), "result.load_upgrade_failed");
        }

        public UniTask Retry() => CanExecute && !State.Value.ShowUpgrades
            ? Execute(() => _retry(_run.State.Mode), "result.load_retry_failed")
            : UniTask.CompletedTask;

        public UniTask ReturnToTitle() => CanExecute ? Execute(_return, null) : UniTask.CompletedTask;

        public void Dispose()
        {
            if (_disposed) return;
            Close();
            _disposed = true;
            State.Dispose();
        }

        private bool CanExecute => !_disposed && _opened && !_busy;

        private async UniTask Execute(Func<UniTask> action, string errorKey)
        {
            _busy = true;
            _error = string.Empty;
            Publish();
            try
            {
                await action();
            }
            catch (Exception error)
            {
                _logger.Error(error.ToString());
                if (!_disposed) _error = errorKey == null ? error.Message : _localization.Get(errorKey);
            }
            finally
            {
                _busy = false;
                if (!_disposed) Publish();
            }
        }

        private void Publish()
        {
            if (_disposed) return;
            var run = _summary;
            var endless = run.Mode == RunMode.Endless;
            var upgrades = run.Phase == RunPhase.ChoosingUpgrade || _selected.HasValue;
            var heading = upgrades
                ? _localization.Get(endless ? "result.round_clear.endless" : "result.round_clear", run.Round)
                : run.Phase == RunPhase.Cleared && _context.IsWin
                    ? _localization.Get("result.cleared")
                    : _localization.Get(endless ? "result.failed.endless" : "result.failed");
            State.Value = new DisplayState
            {
                Title = _localization.Get(endless ? "result.title.endless" : "result.title"),
                Heading = heading.ToUpperInvariant(),
                Progress = $"{_localization.Get("result.progress")}   " +
                           $"{_localization.Get(endless ? "result.wins.endless" : "result.wins", run.Wins)}   •   " +
                           _localization.Get(endless ? "result.reached.endless" : "result.reached", run.Round),
                Statistics =
                    $"{_localization.Get("result.turns")}  {run.TotalTurns}      {_localization.Get("result.damage")}  {run.TotalDamageTaken}",
                Hp = $"HP  {run.Hp} / {run.MaxHp}", Attack = $"ATK  +{run.DamageBonus}",
                Guard = $"GUARD  {100 - run.GuardDamagePercent}%",
                Guidance = _localization.Get(upgrades ? "result.guidance.upgrade" :
                    endless ? "result.guidance.retry.endless" : "result.guidance.retry"),
                Error = _error, Language = _localization.CurrentLanguage,
                Retry = _localization.Get(endless ? "result.retry.endless" : "result.retry"),
                Return = _localization.Get("result.return"),
                AttackUpgrade = _localization.Get("result.attack"),
                VitalityUpgrade = _localization.Get("result.vitality"),
                GuardUpgrade = _localization.Get("result.guard"),
                ShowUpgrades = upgrades, CanInteract = !_busy, SelectedUpgrade = _selected
            };
        }

        private readonly struct Summary
        {
            public readonly RunMode Mode;
            public readonly RunPhase Phase;
            public readonly int Round, Wins, TotalTurns, TotalDamageTaken, Hp, MaxHp, DamageBonus, GuardDamagePercent;

            public Summary(RunState state)
            {
                Mode = state.Mode;
                Phase = state.Phase;
                Round = state.Round;
                Wins = state.Wins;
                TotalTurns = state.TotalTurns;
                TotalDamageTaken = state.TotalDamageTaken;
                Hp = state.Hp;
                MaxHp = state.MaxHp;
                DamageBonus = state.DamageBonus;
                GuardDamagePercent = state.GuardDamagePercent;
            }
        }
    }
}