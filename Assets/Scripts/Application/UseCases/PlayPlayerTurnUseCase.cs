using System;
using System.Threading;
using Application.Services;
using Cysharp.Threading.Tasks;
using Domain.Battle;

namespace Application.UseCases
{
    // Scene scoped orchestration. Views submit intent; they do not decide enemy behavior.
    public sealed class PlayPlayerTurnUseCase : IDisposable
    {
        private readonly AttackUseCase _attack;
        private readonly IBattleSession _session;
        private readonly ITurnDelay _delay;
        private readonly CancellationTokenSource _lifetime = new();
        private bool _busy;
        private bool _disposed;

        public PlayPlayerTurnUseCase(AttackUseCase attack, IBattleSession session, ITurnDelay delay)
        { _attack = attack; _session = session; _delay = delay; }

        public UniTask Execute(CancellationToken cancellationToken) => Execute(BattleAction.Attack, cancellationToken);

        public async UniTask Execute(BattleAction action, CancellationToken cancellationToken)
        {
            if (_disposed || _busy || cancellationToken.IsCancellationRequested) return;
            _busy = true;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            var token = linked.Token;
            var revision = _session.Revision;
            try
            {
                if (!_attack.ExecutePlayerAction(action)) return;
                if (_session.State.IsFinished) return;
                await _delay.Wait(token);
                token.ThrowIfCancellationRequested();
                // An old delayed action must never affect a newly initialized battle.
                if (_session.Revision != revision) return;
                _attack.ExecuteEnemyAttack();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            finally { _busy = false; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
    }
}
